#!/usr/bin/env python3
"""Local balance dashboard: start dungeon sims, bo-solves and balance scans (locally and/or on the DGX Spark) from a page.

  python Scripts/balance-dashboard.py [port]      # default 8765, then open http://localhost:8765

Serves on 127.0.0.1 only. Shows the Spark's state (reachable, load, free memory, running DF shards, vLLM container,
whether its build matches Code/), the active balance patch and whether Code/bin/BalanceTools is older than the source.
Jobs run the same scripts as the command line (Scripts/dungeon-sim.py, bo-solve.py, balance-scan.py) with
--tools Code/bin/BalanceTools; one sim job at a time since each one fills every slot. Logs and result JSON go to
GameData/TuningRuns/dash-<id>.*, so they outlive the server.
"""
import json, os, subprocess, sys, tempfile, threading, time
from datetime import datetime
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse, parse_qs

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)
from simpool import source_hash  # noqa: E402

TOOLS = os.path.join('Code', 'bin', 'BalanceTools')
RUNS = os.path.join(ROOT, 'GameData', 'TuningRuns')
PATCH_DIR = os.path.join(ROOT, 'GameData', 'Patches', 'Balance')
SPARK_HOST = os.environ.get('DF_SPARK_HOST', 'spark')
VLLM = 'quizzical_bell'
PYTHON = sys.executable or 'python'

JOBS = {}
JOBS_LOCK = threading.Lock()
_counter = 0
_spark_cache = {'at': 0.0, 'data': None}
_spark_lock = threading.Lock()


# ---------------------------------------------------------------- helpers

def active_patch():
    try:
        with open(os.path.join(ROOT, 'GameData', 'PatchProfile.json'), encoding='utf-8-sig') as fh:
            return json.load(fh).get('activeBalancePatch') or 'default'
    except (OSError, ValueError):
        return 'default'


def patches():
    try:
        files = [f for f in os.listdir(PATCH_DIR) if f.endswith('.json')]
    except OSError:
        return []
    files.sort(key=lambda f: os.path.getmtime(os.path.join(PATCH_DIR, f)), reverse=True)
    return [f[:-5] for f in files]


def newest_source_mtime():
    code = os.path.join(ROOT, 'Code')
    newest = 0.0
    for d, dirs, files in os.walk(code):
        rel = os.path.relpath(d, code)
        dirs[:] = [x for x in dirs if not (rel == '.' and x in ('bin', 'obj', 'Tests'))]
        for f in files:
            if f.endswith(('.cs', '.csproj', '.axaml')):
                newest = max(newest, os.path.getmtime(os.path.join(d, f)))
    return newest


def tools_state():
    dll = os.path.join(ROOT, TOOLS, 'DF.dll')
    if not os.path.isfile(dll):
        return {'exists': False, 'stale': True, 'built': None}
    built = os.path.getmtime(dll)
    return {'exists': True, 'stale': newest_source_mtime() > built + 1,
            'built': datetime.fromtimestamp(built).strftime('%Y-%m-%d %H:%M')}


def ssh(command, timeout=15):
    return subprocess.run(['ssh', '-o', 'BatchMode=yes', '-o', 'ConnectTimeout=10', SPARK_HOST, command],
                          capture_output=True, text=True, timeout=timeout)


SPARK_PROBE = ('nproc; cat /proc/loadavg; free -m | awk \'/^Mem:/{print $2, $7}\'; '
               'pgrep -fc "^./app/DF " || true; '
               f'printf "%s\\n" "$(docker ps --filter name={VLLM} --format "{{{{.Status}}}}" 2>/dev/null | head -1)"; '
               'printf "%s\\n" "$(cat ~/df/app/.source-hash 2>/dev/null)"')


def _probe():
    """One status call, retried once: while sims fan out 12-18 ssh connects, sshd's MaxStartups drops or stalls some
    (simpool retries for the same reason). Returns (result or None, short error)."""
    err = ''
    for attempt in range(2):
        try:
            r = ssh(SPARK_PROBE, timeout=20)
        except subprocess.TimeoutExpired:
            err = 'no reply within 20 s'
            continue
        except OSError as ex:
            return None, str(ex)
        if r.returncode == 0:
            return r, ''
        err = (r.stderr or '').strip().splitlines()[-1:] or [f'ssh exit {r.returncode}']
        err = err[0][-200:]
        if r.returncode != 255:  # 255 = connection problem, worth a retry; anything else won't change
            break
        time.sleep(1)
    return None, err


def spark_status(force=False):
    with _spark_lock:
        if not force and _spark_cache['data'] and time.time() - _spark_cache['at'] < 8:
            return _spark_cache['data']
        t0 = time.time()
        r, err = _probe()
        if r is None:
            last = _spark_cache.get('good')
            if last and time.time() - last['at'] < 180:
                # A missed reply while the box is busy isn't "offline": keep the last reading, flagged.
                data = {**last['data'], 'stale': True, 'error': err,
                        'staleSeconds': round(time.time() - last['at'])}
            else:
                data = {'reachable': False, 'error': err}
            _spark_cache.update(at=time.time(), data=data)
            return data
        else:
            lines = r.stdout.split('\n') + [''] * 8
            cores = int(lines[0] or 0)
            load = [float(x) for x in lines[1].split()[:3]] if lines[1] else [0, 0, 0]
            total, avail = (int(x) for x in (lines[2].split() + ['0', '0'])[:2])
            remote_hash = lines[5].strip()
            data = {'reachable': True, 'cores': cores, 'load': load, 'memTotalMb': total, 'memAvailMb': avail,
                    'dfShards': int(lines[3] or 0), 'vllm': lines[4].strip() or None,
                    'buildHash': remote_hash or None, 'buildCurrent': remote_hash == source_hash(ROOT),
                    'pingMs': round((time.time() - t0) * 1000)}
        _spark_cache.update(at=time.time(), data=data, good={'at': time.time(), 'data': data})
        return data


# ---------------------------------------------------------------- jobs

class Job:
    def __init__(self, kind, args, json_path):
        global _counter
        _counter += 1
        stamp = datetime.now().strftime('%Y%m%d-%H%M%S')
        self.id = f'{kind}-{_counter}'
        self.kind, self.args, self.json_path = kind, args, json_path
        self.log_path = os.path.join(RUNS, f'dash-{self.id}-{stamp}.log')
        self.lines, self.started, self.ended = [], time.time(), None
        self.exit_code, self.cancelled, self.proc = None, False, None

    @property
    def status(self):
        if self.cancelled:
            return 'cancelled'
        if self.exit_code is None:
            return 'running'
        return 'done' if self.exit_code == 0 else 'failed'

    def summary(self):
        return {'id': self.id, 'kind': self.kind, 'status': self.status, 'command': ' '.join(self.args),
                'elapsed': round((self.ended or time.time()) - self.started), 'log': self.log_path,
                'json': self.json_path if self.json_path and os.path.isfile(self.json_path) else None,
                'started': datetime.fromtimestamp(self.started).strftime('%H:%M:%S')}


def start_job(kind, args, json_path=None, exclusive=True):
    with JOBS_LOCK:
        if exclusive:
            busy = [j for j in JOBS.values() if j.status == 'running']
            if busy:
                return None, f'{busy[0].id} is still running. Wait for it or cancel it first.'
        os.makedirs(RUNS, exist_ok=True)
        job = Job(kind, args, json_path)
        JOBS[job.id] = job
    env = dict(os.environ, PYTHONIOENCODING='utf-8', PYTHONUNBUFFERED='1')
    flags = getattr(subprocess, 'CREATE_NEW_PROCESS_GROUP', 0) | getattr(subprocess, 'CREATE_NO_WINDOW', 0)
    job.proc = subprocess.Popen(args, cwd=ROOT, env=env, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE,
                                stderr=subprocess.STDOUT, text=True, encoding='utf-8', errors='replace',
                                creationflags=flags)

    def pump():
        with open(job.log_path, 'w', encoding='utf-8') as log:
            log.write('$ ' + ' '.join(args) + '\n')
            for line in job.proc.stdout:
                line = line.rstrip('\r\n')
                job.lines.append(line)
                if len(job.lines) > 20000:
                    del job.lines[:5000]
                log.write(line + '\n')
                log.flush()
        job.exit_code = job.proc.wait()
        job.ended = time.time()

    threading.Thread(target=pump, daemon=True).start()
    return job, None


def cancel_job(job):
    if job.status != 'running':
        return 'already finished'
    job.cancelled = True
    if os.name == 'nt':
        subprocess.run(['taskkill', '/PID', str(job.proc.pid), '/T', '/F'], capture_output=True)
    else:
        job.proc.kill()
    # Shards already handed to the Spark keep running after their ssh dies; stop them too.
    if 'spark' in ' '.join(job.args):
        try:
            ssh('pkill -f "^./app/DF " || true', timeout=10)
        except (subprocess.TimeoutExpired, OSError):
            pass
    return 'cancelled'


def _int(p, key, default, lo=0, hi=10_000):
    try:
        return max(lo, min(hi, int(p.get(key, default))))
    except (TypeError, ValueError):
        return default


def _str(p, key):
    v = str(p.get(key) or '').strip()
    return v or None


def build_command(kind, p):
    """Validated form fields -> (script args, json path). Values go to argv lists, never a shell."""
    stamp = datetime.now().strftime('%Y%m%d-%H%M%S')
    out = os.path.join(RUNS, f'dash-{kind}-{stamp}.json')
    a = [PYTHON, '-u']
    spark, local = _int(p, 'spark', 0, 0, 40), p.get('local')
    patch = _str(p, 'patch')
    if patch and patch not in patches():
        raise ValueError(f'unknown patch {patch}')
    if kind == 'dungeon-sim':
        a += ['Scripts/dungeon-sim.py', '--levels', _str(p, 'levels') or '10',
              '--shards', str(_int(p, 'shards', 6, 1, 200)), '--runs', str(_int(p, 'runs', 2, 1, 50)),
              '--dungeons', str(_int(p, 'dungeons', 6, 1, 100)), '--seed', str(_int(p, 'seed', 1, 0, 10**9))]
        if _str(p, 'classes'):
            a += ['--classes', _str(p, 'classes')]
        if p.get('regionMix'):
            a.append('--region-mix')
    elif kind == 'bo-solve':
        a += ['Scripts/bo-solve.py', '--level', str(_int(p, 'level', 8, 1, 100)), '--seed', str(_int(p, 'seed', 11, 0, 10**9)),
              '--scope', 'level' if p.get('scope') == 'level' else 'band', '--max-rounds', str(_int(p, 'maxRounds', 12, 1, 50))]
        if _str(p, 'target'):
            a += ['--target', str(float(p['target']))]
    elif kind == 'balance-scan':
        a += ['Scripts/balance-scan.py', '--skip-build', '--heroes', str(_int(p, 'heroes', 2, 1, 20))]
        if p.get('dungeonLevels') is not None:
            a += ['--dungeon-levels', str(p['dungeonLevels']).strip()]
    else:
        raise ValueError(f'unknown job kind {kind}')
    for lv in (_str(p, 'levels'), _str(p, 'dungeonLevels')):
        if lv and not all(x.strip().isdigit() for x in lv.split(',') if x.strip()):
            raise ValueError('levels must be a comma list of numbers')
    if patch:
        a += ['--patch', patch]
    for anchor in (_str(p, 'anchors') or '').replace(';', ',').split(','):
        if anchor.strip() and kind != 'balance-scan':
            a += ['--anchor', anchor.strip()]
    if spark:
        a += ['--spark', str(spark)]
    if local not in (None, ''):
        a += ['--parallel', str(_int(p, 'local', 0, 0, 64))]
    a += ['--tools', TOOLS, '--json', out]
    return a, out


# ---------------------------------------------------------------- http

class Handler(BaseHTTPRequestHandler):
    def log_message(self, *a):
        pass

    def _send(self, code, body, ctype='application/json'):
        data = body if isinstance(body, bytes) else (json.dumps(body) if ctype == 'application/json' else body).encode()
        self.send_response(code)
        self.send_header('Content-Type', ctype + '; charset=utf-8')
        self.send_header('Cache-Control', 'no-store')
        self.end_headers()
        self.wfile.write(data)

    def _body(self):
        n = int(self.headers.get('Content-Length') or 0)
        return json.loads(self.rfile.read(n) or b'{}') if n else {}

    def do_GET(self):
        u = urlparse(self.path)
        q = parse_qs(u.query)
        if u.path == '/':
            return self._send(200, PAGE, 'text/html')
        if u.path == '/api/status':
            with JOBS_LOCK:
                jobs = [j.summary() for j in sorted(JOBS.values(), key=lambda j: -j.started)]
            return self._send(200, {'patch': active_patch(), 'patches': patches(), 'tools': tools_state(),
                                    'jobs': jobs, 'cores': os.cpu_count()})
        if u.path == '/api/spark':
            return self._send(200, spark_status(force='force' in q))
        if u.path.startswith('/api/jobs/'):
            job = JOBS.get(u.path.split('/')[3])
            if not job:
                return self._send(404, {'error': 'no such job'})
            since = int((q.get('since') or ['0'])[0])
            total = len(job.lines)
            since = min(max(0, since), total)
            return self._send(200, {**job.summary(), 'lines': job.lines[since:], 'next': total})
        self._send(404, {'error': 'not found'})

    def do_POST(self):
        # Same-origin only: a page on another site must not start jobs on this PC.
        origin = self.headers.get('Origin')
        if origin and urlparse(origin).hostname not in ('localhost', '127.0.0.1'):
            return self._send(403, {'error': 'cross-origin request refused'})
        u = urlparse(self.path)
        try:
            body = self._body()
            if u.path == '/api/run':
                args, out = build_command(body.get('kind'), body.get('params') or {})
                job, err = start_job(body['kind'], args, out)
                return self._send(200 if job else 409, {'job': job.summary()} if job else {'error': err})
            if u.path == '/api/build':
                # Own obj folder outside Code/ (inside it the default compile glob would pick up obj/ sources), so
                # this build never fights an IDE build or a running DF.exe over Code/obj.
                obj = os.path.join(tempfile.gettempdir(), 'df-balancetools-obj') + os.sep
                job, err = start_job('build', ['dotnet', 'build', 'Code/Code.csproj', '-c', 'Debug', '-o', TOOLS,
                                               f'-p:BaseIntermediateOutputPath={obj}'])
                return self._send(200 if job else 409, {'job': job.summary()} if job else {'error': err})
            if u.path.startswith('/api/jobs/') and u.path.endswith('/cancel'):
                job = JOBS.get(u.path.split('/')[3])
                return self._send(200, {'result': cancel_job(job)} if job else {'error': 'no such job'})
            if u.path == '/api/spark/vllm':
                action = 'stop' if body.get('action') == 'stop' else 'start'
                r = ssh(f'docker {action} {VLLM}', timeout=90)
                spark_status(force=True)
                return self._send(200, {'ok': r.returncode == 0, 'output': (r.stdout + r.stderr).strip()[-400:]})
            if u.path == '/api/spark/kill':
                r = ssh('pkill -f "^./app/DF " ; echo done', timeout=15)
                spark_status(force=True)
                return self._send(200, {'ok': r.returncode == 0})
        except (ValueError, KeyError) as ex:
            return self._send(400, {'error': str(ex)})
        except subprocess.TimeoutExpired:
            return self._send(504, {'error': 'Spark did not answer in time'})
        self._send(404, {'error': 'not found'})


PAGE = r"""<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Balance Runs</title>
<style>
:root{--bg:#f6f5f1;--panel:#fff;--fg:#1d1c1a;--muted:#6b6862;--line:#e2dfd8;--accent:#c4602d;--ok:#2e8b57;--warn:#c98a10;--bad:#c0392b;--code:#f1efe9}
@media (prefers-color-scheme:dark){:root:not([data-theme="light"]){--bg:#1a1917;--panel:#23221f;--fg:#ecebe6;--muted:#9c988f;--line:#36342f;--accent:#e07a45;--ok:#4cbb7f;--warn:#e0a83a;--bad:#e2604f;--code:#1d1c19}}
*{box-sizing:border-box} [hidden]{display:none!important}
body{margin:0;background:var(--bg);color:var(--fg);font:14px/1.45 system-ui,-apple-system,"Segoe UI",sans-serif}
main{max-width:1180px;margin:0 auto;padding:20px 16px 40px}
h1{font-size:20px;margin:0 0 2px} h2{font-size:15px;margin:0 0 10px}
.sub{color:var(--muted);margin-bottom:18px}
.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:14px}
.card{background:var(--panel);border:1px solid var(--line);border-radius:10px;padding:14px}
.stats{display:grid;grid-template-columns:repeat(auto-fit,minmax(110px,1fr));gap:10px;margin-bottom:10px}
.stat .k{color:var(--muted);font-size:12px} .stat .v{font-size:18px;font-weight:600}
.dot{display:inline-block;width:9px;height:9px;border-radius:50%;margin-right:6px;vertical-align:middle;background:var(--muted)}
.dot.ok{background:var(--ok)} .dot.warn{background:var(--warn)} .dot.bad{background:var(--bad)}
.tabs{display:flex;gap:6px;margin-bottom:12px;flex-wrap:wrap}
.tabs button{background:none;border:1px solid var(--line);color:var(--fg);padding:5px 12px;border-radius:999px;cursor:pointer}
.tabs button.on{background:var(--fg);color:var(--bg);border-color:var(--fg)}
form{display:grid;grid-template-columns:repeat(auto-fill,minmax(130px,1fr));gap:10px}
label{display:flex;flex-direction:column;font-size:12px;color:var(--muted);gap:3px}
label.chk{flex-direction:row;align-items:center;gap:6px;color:var(--fg)}
input,select{font:inherit;padding:6px 8px;border:1px solid var(--line);border-radius:6px;background:var(--bg);color:var(--fg);min-width:0}
.actions{grid-column:1/-1;display:flex;gap:8px;align-items:center;flex-wrap:wrap}
.btn{font:inherit;padding:7px 14px;border-radius:7px;border:1px solid var(--line);background:var(--panel);color:var(--fg);cursor:pointer}
.btn.primary{background:var(--accent);border-color:var(--accent);color:#fff;font-weight:600}
.btn:disabled{opacity:.5;cursor:default}
.hint{color:var(--muted);font-size:12px}
.msg{font-size:13px} .msg.bad{color:var(--bad)} .msg.warn{color:var(--warn)}
table{width:100%;border-collapse:collapse;font-size:13px}
td,th{text-align:left;padding:6px 6px;border-bottom:1px solid var(--line);white-space:nowrap}
tr.sel td{background:var(--code)} tr[data-id]{cursor:pointer}
.st-running{color:var(--warn)} .st-done{color:var(--ok)} .st-failed,.st-cancelled{color:var(--bad)}
pre{background:var(--code);border:1px solid var(--line);border-radius:8px;padding:10px;margin:0;height:420px;overflow:auto;font:12px/1.4 ui-monospace,Consolas,monospace;white-space:pre-wrap;word-break:break-word}
.row{display:flex;gap:8px;align-items:center;flex-wrap:wrap;margin-bottom:8px}
.cmd{font:12px ui-monospace,Consolas,monospace;color:var(--muted);word-break:break-all}
.tablewrap{overflow-x:auto}
</style></head><body><main>
<h1>Balance runs</h1>
<div class="sub">Starts the DUNGEONSIM scripts on this PC and the DGX Spark. Active patch <b id="patch">—</b></div>

<div class="grid">
 <section class="card">
  <div class="row" style="justify-content:space-between"><h2 style="margin:0"><span id="sdot" class="dot"></span>DGX Spark</h2>
   <button class="btn" id="srefresh">Refresh</button></div>
  <div class="stats">
   <div class="stat"><div class="k">Status</div><div class="v" id="s-state">—</div></div>
   <div class="stat"><div class="k">Load (1 min)</div><div class="v" id="s-load">—</div></div>
   <div class="stat"><div class="k">Free memory</div><div class="v" id="s-mem">—</div></div>
   <div class="stat"><div class="k">DF shards running</div><div class="v" id="s-df">—</div></div>
  </div>
  <div class="msg" id="s-build"></div>
  <div class="msg" id="s-vllm"></div>
  <div class="row" style="margin-top:10px">
   <button class="btn" id="vllm-btn" hidden></button>
   <button class="btn" id="kill-btn" hidden>Stop Spark shards</button>
  </div>
 </section>
 <section class="card">
  <h2>This PC</h2>
  <div class="stats">
   <div class="stat"><div class="k">Cores</div><div class="v" id="p-cores">—</div></div>
   <div class="stat"><div class="k">Sim tools built</div><div class="v" id="p-built" style="font-size:14px">—</div></div>
  </div>
  <div class="msg" id="p-tools"></div>
  <div class="row" style="margin-top:10px"><button class="btn" id="build-btn">Build sim tools</button>
   <span class="hint">dotnet build → Code/bin/BalanceTools</span></div>
 </section>
</div>

<section class="card" style="margin-top:14px">
 <div class="tabs" id="tabs">
  <button data-k="dungeon-sim" class="on">Dungeon sim</button><button data-k="bo-solve">Bo-solve</button><button data-k="balance-scan">Balance scan</button>
 </div>
 <form id="f-dungeon-sim" data-k="dungeon-sim">
  <label>Levels<input name="levels" value="8,13,18"></label>
  <label>Shards / class<input name="shards" type="number" value="8" min="1"></label>
  <label>Heroes / shard<input name="runs" type="number" value="2" min="1"></label>
  <label>Dungeons / hero<input name="dungeons" type="number" value="6" min="1"></label>
  <label>Seed<input name="seed" type="number" value="1" min="0"></label>
  <label>Classes<input name="classes" placeholder="all"></label>
  <label>Anchors<input name="anchors" placeholder="L=mult,…"></label>
  <label class="chk"><input type="checkbox" name="regionMix">Region mix</label>
 </form>
 <form id="f-bo-solve" data-k="bo-solve" hidden>
  <label>Level (band midpoint)<input name="level" type="number" value="13" min="1"></label>
  <label>Target clear %<input name="target" placeholder="from anchors"></label>
  <label>Scope<select name="scope"><option>band</option><option>level</option></select></label>
  <label>Seed<input name="seed" type="number" value="11" min="0"></label>
  <label>Max rounds<input name="maxRounds" type="number" value="12" min="1"></label>
  <label>Other anchors<input name="anchors" placeholder="L=mult,…"></label>
 </form>
 <form id="f-balance-scan" data-k="balance-scan" hidden>
  <label>Heroes / class / level<input name="heroes" type="number" value="2" min="1"></label>
  <label>Dungeon levels<input name="dungeonLevels" value="8,18,33,48"></label>
 </form>
 <form id="common" style="margin-top:10px">
  <label>Patch<select name="patch" id="patch-sel"></select></label>
  <label>Spark slots<input name="spark" type="number" value="18" min="0" max="40"></label>
  <label>Local slots<input name="local" type="number" placeholder="script default" min="0"></label>
  <div class="actions"><button class="btn primary" id="run-btn" type="button">Run</button>
   <span class="msg" id="run-msg"></span></div>
  <div class="hint actions" id="slot-hint"></div>
 </form>
</section>

<section class="card" style="margin-top:14px">
 <h2>Jobs</h2>
 <div class="tablewrap"><table><thead><tr><th>Job</th><th>Status</th><th>Started</th><th>Time</th><th></th></tr></thead><tbody id="jobs"><tr><td colspan="5" class="hint">No jobs yet.</td></tr></tbody></table></div>
 <div id="logbox" hidden style="margin-top:12px">
  <div class="row" style="justify-content:space-between"><b id="log-title"></b>
   <span><button class="btn" id="cancel-btn">Cancel</button></span></div>
  <div class="cmd" id="log-cmd" style="margin-bottom:6px"></div>
  <pre id="log"></pre>
  <div class="hint" id="log-files" style="margin-top:6px"></div>
 </div>
</section>
</main>
<script>
const $ = s => document.querySelector(s);
let kind = 'dungeon-sim', selected = null, logNext = 0, spark = null;
const api = async (path, body) => {
  const r = await fetch(path, body === undefined ? {} : {method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify(body)});
  const j = await r.json().catch(() => ({error:'bad response'}));
  if (!r.ok && !j.error) j.error = r.statusText;
  return j;
};
const fmtTime = s => s < 60 ? s + ' s' : s < 3600 ? Math.floor(s/60) + ' min ' + (s%60) + ' s' : (s/3600).toFixed(1) + ' h';
const fields = form => { const o = {}; for (const el of form.elements) { if (!el.name) continue; o[el.name] = el.type === 'checkbox' ? el.checked : el.value; } return o; };

document.querySelectorAll('#tabs button').forEach(b => b.onclick = () => {
  kind = b.dataset.k;
  document.querySelectorAll('#tabs button').forEach(x => x.classList.toggle('on', x === b));
  document.querySelectorAll('form[data-k]').forEach(f => f.hidden = f.dataset.k !== kind);
  slotHint();
});

function slotHint() {
  const s = +document.querySelector('#common [name=spark]').value || 0;
  let t = s ? `Shards go to ${s} Spark slots` : 'Local only';
  if (kind === 'bo-solve' && s) t += ' (bo-solve keeps 0 local slots with the Spark unless you set some: small rounds straggle on this PC)';
  if (kind === 'balance-scan' && s) t += '; BUILDLAB median builds still run here';
  if (s && spark && !spark.reachable) t += '. The Spark is not reachable right now.';
  else if (s && spark && spark.vllm && spark.vllm.startsWith('Up')) t += '. vLLM is holding the Spark\'s memory: stop it first for big batches.';
  $('#slot-hint').textContent = t + '.';
}
document.querySelector('#common [name=spark]').oninput = slotHint;

async function loadSpark(force) {
  if (!spark || force) $('#s-state').textContent = '…';
  spark =await api('/api/spark' + (force ? '?force=1' : ''));
  const dot = $('#sdot'); dot.className = 'dot ' + (spark.reachable ? (spark.dfShards ? 'warn' : 'ok') : 'bad');
  if (!spark.reachable) {
    $('#s-state').textContent = 'Offline'; ['#s-load','#s-mem','#s-df'].forEach(s => $(s).textContent = '—');
    $('#s-build').textContent = spark.error ? `No answer: ${spark.error}. Is it powered on and does "ssh spark" work from this PC?` : ''; $('#s-build').className = 'msg bad';
    $('#s-vllm').textContent = ''; $('#vllm-btn').hidden = $('#kill-btn').hidden = true; slotHint(); return;
  }
  if (spark.stale) dot.className = 'dot warn';
  $('#s-state').textContent = spark.stale ? 'No reply' : spark.dfShards ? 'Busy' : 'Idle';
  $('#s-load').textContent = `${spark.load[0].toFixed(1)} / ${spark.cores}`;
  $('#s-mem').textContent = `${(spark.memAvailMb/1024).toFixed(0)} / ${(spark.memTotalMb/1024).toFixed(0)} GB`;
  $('#s-df').textContent = spark.dfShards;
  const b = $('#s-build');
  if (!spark.buildHash) { b.textContent = 'No sim build on the Spark yet: the first Spark run publishes one (~1 min).'; b.className = 'msg warn'; }
  else if (!spark.buildCurrent) { b.textContent = 'Spark build is behind Code/: the next Spark run republishes it (~50 s).'; b.className = 'msg warn'; }
  else { b.textContent = 'Spark build matches Code/.'; b.className = 'msg hint'; }
  if (spark.stale) { b.textContent = `Last check got no answer (${spark.error}); showing the reading from ${spark.staleSeconds} s ago. Usually ssh congestion while shards start; it retries every 15 s.`; b.className = 'msg warn'; }
  const up = spark.vllm && spark.vllm.startsWith('Up');
  $('#s-vllm').textContent = up ? `vLLM (Odysseus) running: ${spark.vllm}. It holds ~113 GB.` : 'vLLM (Odysseus) stopped.';
  $('#s-vllm').className = 'msg ' + (up ? 'warn' : 'hint');
  const vb = $('#vllm-btn'); vb.hidden = false; vb.textContent = up ? 'Stop vLLM' : 'Start vLLM'; vb.dataset.action = up ? 'stop' : 'start';
  $('#kill-btn').hidden = !spark.dfShards;
  slotHint();
}
$('#srefresh').onclick = () => loadSpark(true);
$('#vllm-btn').onclick = async e => {
  const a = e.target.dataset.action;
  if (!confirm(a === 'stop' ? 'Stop the Odysseus vLLM container on the Spark?' : 'Start the Odysseus vLLM container? It takes ~113 GB of the Spark\'s memory.')) return;
  e.target.disabled = true; const r = await api('/api/spark/vllm', {action:a}); e.target.disabled = false;
  if (!r.ok) alert('docker ' + a + ' failed: ' + (r.output || r.error || ''));
  loadSpark(true);
};
$('#kill-btn').onclick = async () => { if (confirm('Kill every DF shard running on the Spark?')) { await api('/api/spark/kill', {}); loadSpark(true); } };

async function loadStatus() {
  const s = await api('/api/status');
  $('#patch').textContent = s.patch;
  const sel = $('#patch-sel');
  if (sel.options.length !== s.patches.length + 1) {
    const cur = sel.value; sel.innerHTML = '';
    const def = document.createElement('option'); def.value = ''; def.textContent = s.patch + ' (active)'; sel.appendChild(def);
    for (const p of s.patches) { const o = document.createElement('option'); o.value = p; o.textContent = p; sel.appendChild(o); }
    sel.value = cur;
  }
  $('#p-cores').textContent = s.cores;
  $('#p-built').textContent = s.tools.built || 'not built';
  const t = $('#p-tools');
  t.textContent = !s.tools.exists ? 'No DF.dll in Code/bin/BalanceTools: build before running.' : s.tools.stale ? 'Source changed since the last build: runs would use old code. Build first.' : 'Up to date with Code/.';
  t.className = 'msg ' + (s.tools.stale ? 'warn' : 'hint');
  const busy = s.jobs.some(j => j.status === 'running');
  $('#run-btn').disabled = $('#build-btn').disabled = busy;
  const tb = $('#jobs');
  if (!s.jobs.length) return;
  tb.innerHTML = '';
  for (const j of s.jobs) {
    const tr = document.createElement('tr'); tr.dataset.id = j.id; if (j.id === selected) tr.className = 'sel';
    for (const [txt, cls] of [[j.id], [j.status, 'st-' + j.status], [j.started], [fmtTime(j.elapsed)], [j.json ? 'result JSON' : '', 'hint']]) {
      const td = document.createElement('td'); td.textContent = txt; if (cls) td.className = cls; tr.appendChild(td);
    }
    tr.onclick = () => selectJob(j.id);
    tb.appendChild(tr);
  }
  if (!selected && s.jobs.length) selectJob(s.jobs[0].id);
}

function selectJob(id) {
  if (selected !== id) { selected = id; logNext = 0; $('#log').textContent = ''; }
  document.querySelectorAll('#jobs tr').forEach(tr => tr.classList.toggle('sel', tr.dataset.id === id));
  $('#logbox').hidden = false; pollLog();
}

async function pollLog() {
  if (!selected) return;
  const j = await api(`/api/jobs/${selected}?since=${logNext}`);
  if (j.error) return;
  $('#log-title').textContent = `${j.id} · ${j.status} · ${fmtTime(j.elapsed)}`;
  $('#log-cmd').textContent = j.command;
  $('#cancel-btn').hidden = j.status !== 'running';
  if (j.lines.length) {
    const pre = $('#log'), atEnd = pre.scrollTop + pre.clientHeight >= pre.scrollHeight - 20;
    pre.textContent += j.lines.join('\n') + '\n'; logNext = j.next;
    if (atEnd) pre.scrollTop = pre.scrollHeight;
  }
  $('#log-files').textContent = 'Log: ' + j.log + (j.json ? '   ·   Result: ' + j.json : '');
}
$('#cancel-btn').onclick = async () => { if (selected && confirm('Cancel ' + selected + '?')) { await api(`/api/jobs/${selected}/cancel`, {}); loadStatus(); } };

$('#run-btn').onclick = async () => {
  const params = {...fields(document.querySelector(`form[data-k="${kind}"]`)), ...fields($('#common'))};
  const m = $('#run-msg'); m.className = 'msg'; m.textContent = 'Starting…';
  const r = await api('/api/run', {kind, params});
  if (r.error) { m.className = 'msg bad'; m.textContent = r.error; return; }
  m.textContent = 'Started ' + r.job.id; selected = null; await loadStatus(); selectJob(r.job.id);
};
$('#build-btn').onclick = async () => {
  const r = await api('/api/build', {});
  if (r.error) { alert(r.error); return; }
  selected = null; await loadStatus(); selectJob(r.job.id);
};

loadStatus(); loadSpark(false);
setInterval(loadStatus, 3000); setInterval(pollLog, 1500); setInterval(() => loadSpark(false), 15000);
</script></body></html>
"""


def main():
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8765
    srv = ThreadingHTTPServer(('127.0.0.1', port), Handler)
    print(f'Balance dashboard on http://localhost:{port}  (repo {ROOT})', flush=True)
    try:
        srv.serve_forever()
    except KeyboardInterrupt:
        pass


if __name__ == '__main__':
    main()
