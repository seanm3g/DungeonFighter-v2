#!/usr/bin/env python3
"""Local balance dashboard: start dungeon sims, bo-solves and balance scans (locally and/or on the DGX Spark) from a page.

  python Scripts/balance-dashboard.py [port]      # default 8765, then open http://localhost:8765

Serves on 127.0.0.1 only. Shows the Spark's state (reachable, load, free memory, running DF shards, vLLM container,
whether its build matches Code/), the active balance patch and whether Code/bin/BalanceTools is older than the source.
Jobs run the same scripts as the command line (Scripts/dungeon-sim.py, bo-solve.py, balance-scan.py) with
--tools Code/bin/BalanceTools; one sim job at a time since each one fills every slot. Logs and result JSON go to
GameData/TuningRuns/dash-<id>.*, so they outlive the server.

"Running now" lists every balance script running on this PC, also ones started from a shell or another session: a
balance-loop measure shows seeds done of total and an ETA, a dungeon-sim its shards done (counted in its dsim-* temp
folder). The Spark card lists its running shards by level and class. "Results vs targets" reads a balance-loop run
folder's per-seed sim JSONs (so a running measure fills in seed by seed) against balance-targets.json.

"Balance layers" puts every layer the sims measure on one page, each from the newest result file of its kind under
GameData/TuningRuns (pick another per tab): an overview scorecard; difficulty curve and class parity (balance-loop);
class identity, action/combo use and scan findings incl. per-enemy (balance-scan JSON); material balance (forced-set
grid, SD/range over each class's viable sets: own three materials + class-less, with natural worn share); affix
balance (forced-affix clear delta vs a stripped reference, dead/dominant flags, natural adoption); build diversity
and fight dynamics (diversity-report JSON, optionally vs a second report). The Fun report / Material grid / Affix
grid jobs produce those files (a grid job is one sim per variant, run as a chain of steps).
"""
import glob, json, os, re, subprocess, sys, tempfile, threading, time
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


PROFILE_PATH = os.path.join(ROOT, 'GameData', 'PatchProfile.json')


def patch_base(name):
    """The patch an overlay builds on ("base"), or None."""
    try:
        with open(os.path.join(PATCH_DIR, name + '.json'), encoding='utf-8-sig') as fh:
            return json.load(fh).get('base')
    except (OSError, ValueError, AttributeError):
        return None


def set_active_patch(name):
    with open(PROFILE_PATH, encoding='utf-8-sig') as fh:
        profile = json.load(fh)
    profile['activeBalancePatch'] = name
    with open(PROFILE_PATH, 'w', encoding='utf-8') as fh:
        json.dump(profile, fh, indent=2)


_VERSIONED = re.compile(r'^(.*?)(\d+)([a-z]?)$')
SEEDS_PATH = os.path.join(RUNS, 'balance-loop', 'dash-seed-base.json')


def _next_seed_base():
    """Fresh seeds per dashboard iteration: 1000 apart from 30000, above every seed the scripts default to."""
    try:
        with open(SEEDS_PATH, encoding='utf-8') as fh:
            base = int(json.load(fh)['next'])
    except (OSError, ValueError, KeyError):
        base = 30000
    os.makedirs(os.path.dirname(SEEDS_PATH), exist_ok=True)
    with open(SEEDS_PATH, 'w', encoding='utf-8') as fh:
        json.dump({'next': base + 1000}, fh)
    return base


def iteration_plan(mode, p):
    """The next balance-loop iteration on the active patch: 'measure' (held-out measure vs the patch it builds on) or
    'chain' (health -> damage -> class -> damage solves writing the next patch versions, then a held-out measure).
    Returns (steps, description); seeds are fresh each time."""
    patch = active_patch()
    if patch not in patches():
        raise ValueError(f'active patch {patch} has no file in GameData/Patches/Balance')
    ref = patch_base(patch) or next((x for x in patches() if x != patch), '')
    stamp = datetime.now().strftime('%Y-%m-%d-%H%M')
    steps = []
    if tools_state()['stale']:
        steps.append(build_tools_args())
    base = _next_seed_base()
    levels = _str(p, 'levels')
    if levels and not all(x.strip().isdigit() for x in levels.split(',') if x.strip()):
        raise ValueError('levels must be a comma list of numbers')
    lv = ['--levels', levels] if levels else []
    if mode == 'chain':
        m = _VERSIONED.match(patch)
        if not m:
            raise ValueError(f'cannot number the next patch after {patch} (expected a name ending in a number)')
        prefix, first = m.group(1), int(m.group(2)) + 1
        existing = set(patches())
        while any(f'{prefix}{first + i}' in existing for i in range(4)):
            first += 1
        run = f'iter-chain-{patch}-{stamp}'
        holdout = ','.join(str(base + 901 + k) for k in range(4))
        steps.append([PYTHON, '-u', 'Scripts/balance-chain.py', '--start', patch, '--first', str(first), '--prefix', prefix,
                      '--ref', patch, '--run', run, '--tools', TOOLS, '--seed-base', str(base), '--holdout-seeds', holdout] + lv)
        desc = f'solve chain on {patch} -> {prefix}{first}.. (seeds {base}+), run {run}'
    else:
        run = f'iter-{patch}-{stamp}'
        seeds = ','.join(str(base + 1 + k) for k in range(4))
        a = [PYTHON, '-u', 'Scripts/balance-loop.py', 'measure', '--patch', patch, '--seeds', seeds, '--run', run,
             '--tools', TOOLS] + lv
        if ref:
            a += ['--ref', ref]
        if p.get('spark') not in (None, ''):
            a += ['--spark', str(_int(p, 'spark', 0, 0, 40))]
        steps.append(a)
        desc = f'measure {patch}' + (f' vs {ref}' if ref else '') + f' on seeds {seeds}, run {run}'
    return steps, desc


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
               'printf "%s\\n" "$(cat ~/df/app/.source-hash 2>/dev/null)"; '
               # One line per running shard: seconds running, then its DF arguments (level, class, bot, seed).
               'ps -o etimes=,args= -C DF 2>/dev/null | grep DUNGEONSIM || true')

TARGETS_PATH = os.path.join(ROOT, 'GameData', 'TuningProfiles', 'balance-targets.json')
LOOP_DIR = os.path.join(RUNS, 'balance-loop')
SIM_SCRIPTS = ('balance-loop.py', 'balance-chain.py', 'feature-test.py', 'dungeon-calibrate.py', 'enemy-env-report.py', 'dungeon-sim.py', 'bo-solve.py', 'balance-scan.py',
               'surrogate-data.py', 'median-curve-solve.py')
CLASSES = ['Barbarian', 'Warrior', 'Rogue', 'Wizard']
MIDPOINTS = [3, 8, 13, 18, 23, 28, 33, 38, 43, 48, 53, 58, 63, 68, 73]


def _arg(argv, flag, default=None):
    """Value after `flag` in an argv list (also --flag=value)."""
    for i, a in enumerate(argv):
        if a == flag and i + 1 < len(argv):
            return argv[i + 1]
        if a.startswith(flag + '='):
            return a.split('=', 1)[1]
    return default


def _shard_info(args):
    """Level / class / bot from one DUNGEONSIM command line."""
    argv = args.split()
    return {'level': _arg(argv, '--level'), 'cls': _arg(argv, '--classes'), 'bot': _arg(argv, '--bot', 'rules')}


def spark_shards(lines):
    rows = []
    for ln in lines:
        ln = ln.strip()
        if not ln or 'DUNGEONSIM' not in ln:
            continue
        secs, _, args = ln.partition(' ')
        try:
            rows.append({**_shard_info(args), 'secs': int(secs)})
        except ValueError:
            continue
    groups = {}
    for r in rows:
        g = groups.setdefault((r['level'], r['cls']), {'level': r['level'], 'cls': r['cls'], 'n': 0, 'oldest': 0})
        g['n'] += 1
        g['oldest'] = max(g['oldest'], r['secs'])
    out = sorted(groups.values(), key=lambda g: (int(g['level'] or 0), g['cls'] or ''))
    return {'count': len(rows), 'groups': out, 'bots': sorted({r['bot'] for r in rows})}


# ---------------------------------------------------------------- runs started anywhere (shell, other sessions)

_local_cache = {'at': 0.0, 'data': None}


def local_shards():
    """DUNGEONSIM shard processes on this PC (dotnet DF.dll / DF.exe), grouped like the Spark's."""
    if time.time() - _local_cache['at'] < 4 and _local_cache['data'] is not None:
        return _local_cache['data']
    lines = []
    if os.name == 'nt':
        ps = ("Get-CimInstance Win32_Process -Filter \"Name like 'dotnet%' or Name like 'DF%'\" | "
              "Where-Object { $_.CommandLine -like '*DUNGEONSIM*' } | ForEach-Object { "
              "@(([int]((Get-Date) - $_.CreationDate).TotalSeconds), $_.CommandLine) -join ' ' }")
        try:
            r = subprocess.run(['powershell', '-NoProfile', '-NonInteractive', '-Command', ps], capture_output=True,
                               text=True, timeout=20, creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
            lines = r.stdout.splitlines()
        except (subprocess.TimeoutExpired, OSError):
            lines = []
    else:
        r = subprocess.run(['ps', '-eo', 'etimes=,args='], capture_output=True, text=True)
        lines = [ln for ln in r.stdout.splitlines() if 'DUNGEONSIM' in ln]
    data = {**spark_shards(lines), 'cores': os.cpu_count()}
    _local_cache.update(at=time.time(), data=data)
    return data


_proc_cache = {'at': 0.0, 'data': []}
_proc_lock = threading.Lock()


def _list_processes():
    """(pid, ppid, started epoch, command line) for every python process running one of the balance scripts."""
    out = []
    if os.name == 'nt':
        ps = ("Get-CimInstance Win32_Process -Filter \"Name like 'python%'\" | ForEach-Object { "
              "@($_.ProcessId, $_.ParentProcessId, ([DateTimeOffset]$_.CreationDate).ToUnixTimeSeconds(), "
              "$_.CommandLine) -join [char]9 }")
        try:
            r = subprocess.run(['powershell', '-NoProfile', '-NonInteractive', '-Command', ps], capture_output=True,
                               text=True, timeout=20, creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
        except (subprocess.TimeoutExpired, OSError):
            return out
        for ln in r.stdout.splitlines():
            parts = ln.split('\t', 3)
            if len(parts) == 4 and parts[0].isdigit():
                out.append((int(parts[0]), int(parts[1] or 0), int(parts[2] or 0), parts[3]))
    else:
        r = subprocess.run(['ps', '-eo', 'pid=,ppid=,etimes=,args='], capture_output=True, text=True)
        now = time.time()
        for ln in r.stdout.splitlines():
            parts = ln.split(None, 3)
            if len(parts) == 4 and 'python' in parts[3]:
                out.append((int(parts[0]), int(parts[1]), int(now - int(parts[2])), parts[3]))
    return [p for p in out if any(s in p[3] for s in SIM_SCRIPTS) and 'balance-dashboard' not in p[3]]


def _dsim_dirs():
    tmp = tempfile.gettempdir()
    try:
        names = [n for n in os.listdir(tmp) if n.startswith('dsim-')]
    except OSError:
        return []
    dirs = []
    for n in names:
        d = os.path.join(tmp, n)
        try:
            dirs.append((os.path.getctime(d), d))
        except OSError:
            pass
    return sorted(dirs)


def _dsim_progress(proc_started, argv):
    """Shard progress of one running dungeon-sim.py: its temp work folder (dsim-*, made right after start) holds a
    .log per started shard and a .json per finished one."""
    cands = [d for t, d in _dsim_dirs() if proc_started - 5 <= t <= proc_started + 120]
    if not cands:
        return None
    d = cands[0]
    try:
        names = os.listdir(d)
    except OSError:
        return None
    started = sum(1 for n in names if n.endswith('.log'))
    done = sum(1 for n in names if n.endswith('.json') and not n.startswith('patch'))
    levels = [x for x in (_arg(argv, '--levels', '10') or '').split(',') if x.strip()]
    classes = [x for x in (_arg(argv, '--classes') or '').split(',') if x.strip()] or CLASSES
    total = len(levels) * len(classes) * int(_arg(argv, '--shards', '6') or 6)
    if '--solve' in argv or total <= 0:
        total = None
    # Rate from the first shard start, so the Spark publish (~35-50 s) before it doesn't skew the ETA.
    firsts = [os.path.getmtime(os.path.join(d, n)) for n in names if n.endswith('.log')]
    elapsed = time.time() - (min(firsts) if firsts else os.path.getctime(d))
    eta =round(elapsed * (total - done) / done) if total and done else None
    return {'done': done, 'running': max(0, started - done), 'total': total, 'eta': eta, 'workDir': d}


def _batch_info(argv):
    """dungeon-sim.py --batch NAME:I/N (sim I of N run back to back), or None."""
    m = re.match(r'^(.*?):(\d+)/(\d+)$', _arg(argv, '--batch') or '')
    if not m:
        return None
    return {'name': m.group(1), 'index': int(m.group(2)), 'count': int(m.group(3))}


def _loop_run_dir(argv, started):
    name = _arg(argv, '--run') or datetime.fromtimestamp(started).strftime('%Y-%m-%d')
    return os.path.join(LOOP_DIR, name)


def _loop_progress(argv, started):
    """balance-loop.py measure: seeds finished (one dungeon-sim JSON per seed and patch) and the time each took."""
    d = _loop_run_dir(argv, started)
    cmd = next((a for a in argv if a in ('measure', 'solve', 'botcheck', 'export')), None)
    info = {'cmd': cmd, 'runDir': d, 'events': []}
    log = os.path.join(d, 'log.txt')
    if os.path.isfile(log):
        with open(log, encoding='utf-8', errors='replace') as fh:
            lines = fh.read().splitlines()
        info['events'] = [ln[:220] for ln in lines[-8:]]
        times = []
        for ln in lines:
            if '  exit ' in ln:
                try:
                    times.append(int(ln.split('  exit ', 1)[1].split()[1].rstrip('s')))
                except (IndexError, ValueError):
                    pass
        info['simSeconds'] = times
    if cmd == 'measure':
        seeds = (_arg(argv, '--seeds') or '7101,7102,7103,7104').split(',')
        patches = [p for p in (_arg(argv, '--patch'), _arg(argv, '--ref')) if p]
        total = len(seeds) * len(patches)
        done = sum(1 for p in patches for s in seeds if os.path.isfile(os.path.join(d, f'measure-{p}-s{s}.json')))
        info.update(total=total, done=done)
    return info


def external_runs():
    with _proc_lock:
        if time.time() - _proc_cache['at'] < 4:
            return _proc_cache['data']
        procs = _list_processes()
        with JOBS_LOCK:
            dash_pids = {j.proc.pid for j in JOBS.values() if j.proc and j.status == 'running'}
        pids = {p[0] for p in procs}
        runs = []
        for pid, ppid, started, cmdline in procs:
            if ppid in pids:
                continue  # a child of another listed script (balance-loop -> dungeon-sim): shown under its parent
            argv = cmdline.replace('"', '').split()
            script = next((s for s in SIM_SCRIPTS if s in cmdline), '?')
            r = {'pid': pid, 'script': script, 'started': datetime.fromtimestamp(started).strftime('%H:%M:%S'),
                 'elapsed': round(time.time() - started), 'command': cmdline[cmdline.find('Scripts'):][:400],
                 'fromDashboard': pid in dash_pids or ppid in dash_pids, 'patch': _arg(argv, '--patch')}
            if script in ('balance-loop.py',):
                r['loop'] = _loop_progress(argv, started)
            child = next((c for c in procs if c[1] == pid and 'dungeon-sim.py' in c[3]), None)
            sim = (child[2], child[3]) if child else ((started, cmdline) if script == 'dungeon-sim.py' else None)
            if sim:
                sargv = sim[1].replace('"', '').split()
                r['sim'] = {'seed': _arg(sargv, '--seed'), 'levels': _arg(sargv, '--levels'), 'bot': _arg(sargv, '--bot', 'synergy'),
                            'shards': _arg(sargv, '--shards'), 'spark': _arg(sargv, '--spark'),
                            'parallel': _arg(sargv, '--parallel'), 'featuresOff': _arg(sargv, '--features-off'),
                            'elapsed': round(time.time() - sim[0]),
                            'progress': _dsim_progress(sim[0], sargv), 'batch': _batch_info(sargv)}
            runs.append(r)
        _proc_cache.update(at=time.time(), data=runs)
        return runs


# ---------------------------------------------------------------- results vs targets

def _interp(pts, level):
    pts = sorted(pts)
    if level <= pts[0][0]:
        return pts[0][1]
    for (l0, c0), (l1, c1) in zip(pts, pts[1:]):
        if level <= l1:
            return c0 + (level - l0) / (l1 - l0) * (c1 - c0)
    return pts[-1][1]


def _targets():
    try:
        with open(TARGETS_PATH, encoding='utf-8-sig') as fh:
            t = json.load(fh)
    except (OSError, ValueError):
        return None
    clear = [(a['level'], float(a['clear'])) for a in t.get('realClearAnchors', [])]
    turns = [(a['level'], float(a['turns'])) for a in t.get('heroTurnsAnchors', [])]
    turns_max = max((p[0] for p in turns), default=0)
    return {'clear': lambda L: _interp(clear, L) if clear else None,
            'turns': lambda L: _interp(turns, L) if turns and L <= turns_max else float(t.get('heroTurnsTarget', 11)),
            'parity': float(t.get('classParityMaxSpread', 10)), 'clearTol': float(t.get('realClearTolerance', 5) or 5)}


def _mean(v):
    v = [x for x in v if x is not None]
    return sum(v) / len(v) if v else None


def _se(v):
    v = [x for x in v if x is not None]
    if len(v) < 2:
        return None
    m = sum(v) / len(v)
    return (sum((x - m) ** 2 for x in v) / (len(v) - 1)) ** 0.5 / len(v) ** 0.5


def loop_runs():
    """balance-loop run folders, newest first, with what each holds."""
    try:
        names = [n for n in os.listdir(LOOP_DIR) if os.path.isdir(os.path.join(LOOP_DIR, n))]
    except OSError:
        return []
    names.sort(key=lambda n: os.path.getmtime(os.path.join(LOOP_DIR, n)), reverse=True)
    return names[:30]


def run_results(name):
    """Per patch in a balance-loop run: level rows averaged over the seed JSONs present so far (a running measure
    shows its finished seeds), vs the targets."""
    d = os.path.join(LOOP_DIR, os.path.basename(name or ''))
    if not name or not os.path.isdir(d):
        return {'error': 'no such run'}
    T = _targets()
    by_patch = {}
    for f in sorted(glob.glob(os.path.join(d, '*-s*.json'))):
        base = os.path.basename(f)[:-5]
        m = re.match(r'^(measure|diag|confirm|botcheck|check)-(.+)-s(\d+)$', base)
        if not m:
            continue
        tag, patch, seed = m.groups()
        try:
            with open(f, encoding='utf-8') as fh:
                rows = json.load(fh)
        except (OSError, ValueError):
            continue
        key = f'{patch} ({tag})' if tag != 'measure' else patch
        p = by_patch.setdefault(key, {'patch': patch, 'tag': tag, 'seeds': [], 'levels': {}})
        p['seeds'].append(seed)
        for r in rows if isinstance(rows, list) else []:
            o = p['levels'].setdefault(r['level'], {'clear': [], 'acts': [], 'cls': {c: [] for c in CLASSES}, 'dungeons': 0, 'dun': {}})
            for t, v in (r.get('perDungeon') or {}).items():
                if v.get('dungeons'):
                    dd = o['dun'].setdefault(t, [0.0, 0])
                    dd[0] += v['clear'] * v['dungeons']; dd[1] += v['dungeons']
            o['clear'].append(100 * r['clear'])
            o['acts'].append(r.get('heroActionsPerFight'))
            o['dungeons'] += r.get('dungeons') or 0
            for c, v in (r.get('perClassClear') or {}).items():
                if v is not None and c in o['cls']:
                    o['cls'][c].append(100 * v)
    out = []
    for key, p in by_patch.items():
        levels = []
        for L, o in sorted(p['levels'].items()):
            cm = {c: round(_mean(v), 1) for c, v in o['cls'].items() if v}
            tgt = T['clear'](L) if T else None
            clear = _mean(o['clear'])
            levels.append({'level': L, 'target': round(tgt, 1) if tgt is not None else None, 'clear': round(clear, 1),
                           'clearSE': round(_se(o['clear']), 1) if _se(o['clear']) is not None else None,
                           'gap': round(clear - tgt, 1) if tgt is not None else None,
                           'heroActions': round(_mean(o['acts']), 2) if _mean(o['acts']) is not None else None,
                           'turnsTarget': round(T['turns'](L), 1) if T else None, 'classes': cm,
                           'spread': round(max(cm.values()) - min(cm.values()), 1) if len(cm) > 1 else None,
                           'dungeons': o['dungeons'],
                           'perDungeon': {t: round(100 * k / n, 1) for t, (k, n) in sorted(o['dun'].items()) if n}})
        gaps = [abs(r['gap']) for r in levels if r['gap'] is not None]
        spreads = [r['spread'] for r in levels if r['spread'] is not None]
        tgaps = [abs(r['heroActions'] - r['turnsTarget']) for r in levels if r['heroActions'] is not None and r['turnsTarget']]
        summary = {'meanClearGap': round(_mean(gaps), 2) if gaps else None,
                   'meanClassSpread': round(_mean(spreads), 1) if spreads else None,
                   'bandsOverParity': sum(1 for s in spreads if T and s > T['parity']),
                   'meanTurnsGap': round(_mean(tgaps), 2) if tgaps else None}
        out.append({'key': key, 'patch': p['patch'], 'seeds': p['seeds'], 'levels': levels, 'summary': summary})
    final = [f for f in sorted(glob.glob(os.path.join(d, 'measure-*.json'))) if not re.search(r'-s\d+\.json$', f)]
    return {'run': os.path.basename(d), 'patches': out, 'parity': T['parity'] if T else None,
            'clearTol': T['clearTol'] if T else None, 'finished': [os.path.basename(f) for f in final]}


# ---------------------------------------------------------------- balance layers

# Result files under GameData/TuningRuns, by what layer of balance they measure. Classified by file name (cheap: the
# folder holds GBs of raw shards) and, for grids, by their variant names.
_layer_cache = {'at': 0.0, 'data': None}
_SKIP_DIRS = ('raw', 'dsim-', 'paired', 'surrogate', 'affix-sweep', 'material-sweep')


def _classify(name):
    n = name.lower()
    if not n.endswith('.json'):
        return None
    # These two name their seed (-s3302): check them before the per-seed rule below.
    if n.startswith('feature-test'):
        return 'featureTest'
    if n.startswith('enemy-env-report'):
        return 'enemyEnv'
    if re.search(r'-s\d+\.json$', n):
        return None
    if 'scan' in n:
        return 'scan'
    if 'report' in n:
        return 'report'
    if 'grid' in n:
        return 'grid'
    return None


def layer_sources(force=False):
    if not force and _layer_cache['data'] and time.time() - _layer_cache['at'] < 10:
        return _layer_cache['data']
    out = {'scan': [], 'report': [], 'materialGrid': [], 'affixGrid': [], 'featureTest': [], 'enemyEnv': []}
    for dirpath, dirs, files in os.walk(RUNS):
        depth = os.path.relpath(dirpath, RUNS).count(os.sep)
        dirs[:] = [] if depth >= 2 else [d for d in dirs if not d.lower().startswith(_SKIP_DIRS)]
        for f in files:
            kind = _classify(f)
            if not kind:
                continue
            path = os.path.join(dirpath, f)
            rel = os.path.relpath(path, RUNS).replace(os.sep, '/')
            if kind == 'grid':
                try:
                    with open(path, encoding='utf-8') as fh:
                        g = json.load(fh)
                    names = {v for lv in g.values() for v in (lv.get('variants') or {})}
                except (OSError, ValueError, AttributeError):
                    continue
                kind = 'affixGrid' if any(v.startswith(('mod-', 'stat-')) for v in names) else 'materialGrid'
            out[kind].append({'path': rel, 'mtime': os.path.getmtime(path),
                              'when': datetime.fromtimestamp(os.path.getmtime(path)).strftime('%m-%d %H:%M')})
    for v in out.values():
        v.sort(key=lambda x: -x['mtime'])
    _layer_cache.update(at=time.time(), data=out)
    return out


def layer_file(rel):
    """One result file under TuningRuns, for the layer views. Scan JSON drops its raw rows (large, unused here)."""
    path = os.path.realpath(os.path.join(RUNS, rel or ''))
    if not path.startswith(os.path.realpath(RUNS) + os.sep) or not path.endswith('.json') or not os.path.isfile(path):
        return {'error': 'no such result file'}
    if os.path.getsize(path) > 25_000_000:
        return {'error': 'file too large for the dashboard'}
    with open(path, encoding='utf-8-sig') as fh:
        data = json.load(fh)
    if isinstance(data, dict):
        data.pop('raw', None)
    return {'path': rel, 'data': data}


def material_info():
    """Material -> owning class ('' = class-less), from MaterialBuilds.json."""
    try:
        with open(os.path.join(ROOT, 'GameData', 'MaterialBuilds.json'), encoding='utf-8-sig') as fh:
            rows = json.load(fh)
    except (OSError, ValueError):
        return {}
    return {r['material']: (r.get('class') or '').title() for r in rows if r.get('material')}


def affix_names():
    """Adjective prefixes and stat suffixes that can drop, for the affix grid form."""
    mods, stats = [], []
    try:
        with open(os.path.join(ROOT, 'GameData', 'Modifications.json'), encoding='utf-8-sig') as fh:
            mods = sorted({m['Name'] for m in json.load(fh) if m.get('Name') and m.get('prefixCategory') == 'ADJECTIVE'})
    except (OSError, ValueError, AttributeError):
        pass
    try:
        with open(os.path.join(ROOT, 'GameData', 'StatBonuses.json'), encoding='utf-8-sig') as fh:
            stats = sorted({s.get('StatType') or s.get('statType') for s in json.load(fh) if (s.get('StatType') or s.get('statType'))})
    except (OSError, ValueError, AttributeError):
        pass
    return {'mods': mods, 'stats': stats}


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
                    'pingMs': round((time.time() - t0) * 1000), 'shards': spark_shards(lines[6:])}
        _spark_cache.update(at=time.time(), data=data, good={'at': time.time(), 'data': data})
        return data


# ---------------------------------------------------------------- jobs

class Job:
    def __init__(self, kind, args, json_path):
        global _counter
        _counter += 1
        stamp = datetime.now().strftime('%Y%m%d-%H%M%S')
        self.id = f'{kind}-{_counter}'
        self.kind, self.json_path = kind, json_path
        # A job is one command or a chain of them (a grid is one sim per variant, then the grid report).
        self.steps = args if args and isinstance(args[0], list) else [args]
        self.args, self.step = self.steps[0], 0
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
        return {'id': self.id, 'kind': self.kind, 'status': self.status, 'command': ' && '.join(' '.join(a) for a in self.steps),
                'elapsed': round((self.ended or time.time()) - self.started), 'log': self.log_path,
                'step': self.step, 'steps': len(self.steps),
                'json': self.json_path if self.json_path and os.path.isfile(self.json_path) else None,
                'started': datetime.fromtimestamp(self.started).strftime('%H:%M:%S')}


def start_job(kind, args, json_path=None, exclusive=True, note=None):
    with JOBS_LOCK:
        if exclusive:
            busy = [j for j in JOBS.values() if j.status == 'running']
            if busy:
                return None, f'{busy[0].id} is still running. Wait for it or cancel it first.'
        os.makedirs(RUNS, exist_ok=True)
        job = Job(kind, args, json_path)
        if note:
            job.lines.append(note)
        JOBS[job.id] = job
    env = dict(os.environ, PYTHONIOENCODING='utf-8', PYTHONUNBUFFERED='1')
    flags = getattr(subprocess, 'CREATE_NEW_PROCESS_GROUP', 0) | getattr(subprocess, 'CREATE_NO_WINDOW', 0)
    def pump():
        with open(job.log_path, 'w', encoding='utf-8') as log:
            log.writelines(line + '\n' for line in job.lines)  # the note, if any
            for i, step in enumerate(job.steps):
                if job.cancelled:
                    break
                job.step, job.args = i + 1, step
                head = '$ ' + ' '.join(step) + (f'   [step {i + 1} of {len(job.steps)}]' if len(job.steps) > 1 else '')
                job.lines.append(head)
                log.write(head + '\n')
                job.proc = subprocess.Popen(step, cwd=ROOT, env=env, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE,
                                            stderr=subprocess.STDOUT, text=True, encoding='utf-8', errors='replace',
                                            creationflags=flags)
                for line in job.proc.stdout:
                    line = line.rstrip('\r\n')
                    job.lines.append(line)
                    if len(job.lines) > 20000:
                        del job.lines[:5000]
                    log.write(line + '\n')
                    log.flush()
                code = job.proc.wait()
                if code != 0:
                    job.exit_code = code
                    break
        if job.exit_code is None:
            job.exit_code = 0 if not job.cancelled else 1
        job.ended = time.time()

    threading.Thread(target=pump, daemon=True).start()
    return job, None


def cancel_job(job):
    if job.status != 'running':
        return 'already finished'
    job.cancelled = True
    if job.proc is None:
        pass
    elif os.name == 'nt':
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


def build_tools_args():
    # Own obj folder outside Code/ (inside it the default compile glob would pick up obj/ sources), so this build never
    # fights an IDE build or a running DF.exe over Code/obj.
    obj = os.path.join(tempfile.gettempdir(), 'df-balancetools-obj') + os.sep
    return ['dotnet', 'build', 'Code/Code.csproj', '-c', 'Debug', '-o', TOOLS, f'-p:BaseIntermediateOutputPath={obj}']


def _int(p, key, default, lo=0, hi=10_000):
    try:
        return max(lo, min(hi, int(p.get(key, default))))
    except (TypeError, ValueError):
        return default


def _str(p, key):
    v = str(p.get(key) or '').strip()
    return v or None


_LAYER_KINDS = ('fun-report', 'material-grid', 'affix-grid')
_NAME = re.compile(r'^[A-Za-z][A-Za-z0-9 _-]{0,40}$')


def build_layer_command(kind, p, stamp):
    """Jobs that fill the balance-layer views: a natural run kept raw + diversity report, or one forced-variant sim
    per material / affix then the grid. Same sim flags as dungeon-sim."""
    levels = _str(p, 'levels') or '18,38,58'
    if not all(x.strip().isdigit() for x in levels.split(',') if x.strip()):
        raise ValueError('levels must be a comma list of numbers')
    patch = _str(p, 'patch')
    if patch and patch not in patches():
        raise ValueError(f'unknown patch {patch}')
    tail = ['--levels', levels, '--shards', str(_int(p, 'shards', 4, 1, 200)), '--runs', str(_int(p, 'runs', 2, 1, 50)),
            '--dungeons', str(_int(p, 'dungeons', 6, 1, 100)), '--seed', str(_int(p, 'seed', 1, 0, 10**9)), '--tools', TOOLS]
    if patch:
        tail += ['--patch', patch]
    if p.get('spark') not in (None, ''):
        tail += ['--spark', str(_int(p, 'spark', 0, 0, 40))]  # 0 = local only (the scripts default to auto)
    if p.get('local') not in (None, ''):
        tail += ['--parallel', str(_int(p, 'local', 0, 0, 64))]
    sim = [PYTHON, '-u', 'Scripts/dungeon-sim.py']
    rep = [PYTHON, '-u', 'Scripts/diversity-report.py']
    if kind == 'fun-report':
        raw = os.path.join(RUNS, f'dash-fun-raw-{stamp}')
        out = os.path.join(RUNS, f'dash-fun-report-{stamp}.json')
        return [sim + tail + ['--keep', raw, '--json', os.path.join(RUNS, f'dash-fun-sim-{stamp}.json')],
                rep + ['report', raw, '--json', out]], out
    folder = os.path.join(RUNS, f'dash-{kind}-{stamp}')
    if kind == 'material-grid':
        names = [n.strip() for n in (_str(p, 'variants') or ','.join(material_info())).split(',') if n.strip()]
        variants = [('none', [])] + [(n, ['--material', n]) for n in names]
    else:
        names = [n.strip() for n in (_str(p, 'variants') or '').split(',') if n.strip()]
        if not names:
            raise ValueError('list the affixes to price, e.g. mod:Heavy, stat:CRIT')
        variants = [('mod-strip', ['--affix', 'mod:-']), ('stat-strip', ['--affix', 'stat:-'])]
        for n in names:
            fam, _, name = n.partition(':')
            if fam not in ('mod', 'stat') or not name:
                raise ValueError(f'{n}: affixes are mod:Name or stat:TYPE')
            # Priced against a stripped reference: mod:X replaces every adjective, stat:-X keeps only that suffix.
            variants.append((f'{fam}-{name}', ['--affix', f'mod:{name}' if fam == 'mod' else f'stat:-{name}']))
    for name, _ in variants:
        if not _NAME.match(name.replace('-', '')):
            raise ValueError(f'bad variant name {name}')
    steps, pairs = [], []
    for name, extra in variants:
        path = os.path.join(folder, f'{name}.json')
        steps.append(sim + tail + extra + ['--json', path])
        pairs.append(f'{name}={path}')
    out = os.path.join(RUNS, f'dash-{kind}-{stamp}.json')
    steps.append(rep + ['grid', *pairs, '--json', out])
    os.makedirs(folder, exist_ok=True)
    return steps, out


def build_command(kind, p):
    """Validated form fields -> (script args, json path). Values go to argv lists, never a shell."""
    stamp = datetime.now().strftime('%Y%m%d-%H%M%S')
    if kind in _LAYER_KINDS:
        return build_layer_command(kind, p, stamp)
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
            ps = patches()
            return self._send(200, {'patch': active_patch(), 'patches': ps, 'newest': ps[0] if ps else None,
                                    'patchBase': patch_base(active_patch()), 'tools': tools_state(),
                                    'jobs': jobs, 'cores': os.cpu_count()})
        if u.path == '/api/external':
            try:
                return self._send(200, {'runs': external_runs(), 'pc': local_shards()})
            except Exception as ex:  # a half-written run folder must not blank the page
                return self._send(500, {'error': f'{type(ex).__name__}: {ex}'})
        if u.path == '/api/loopruns':
            return self._send(200, {'runs': loop_runs()})
        if u.path == '/api/results':
            return self._send(200, run_results((q.get('run') or [''])[0]))
        if u.path == '/api/spark':
            return self._send(200, spark_status(force='force' in q))
        if u.path == '/api/layers/sources':
            return self._send(200, {'sources': layer_sources(force='force' in q), 'materials': material_info(),
                                    'affixes': affix_names()})
        if u.path == '/api/layers/file':
            try:
                return self._send(200, layer_file((q.get('path') or [''])[0]))
            except (OSError, ValueError) as ex:
                return self._send(500, {'error': f'{type(ex).__name__}: {ex}'})
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
                job, err = start_job('build', build_tools_args())
                return self._send(200 if job else 409, {'job': job.summary()} if job else {'error': err})
            if u.path == '/api/update':
                # Newest patch file -> active patch, then sim tools and the Spark build brought up to date with Code/.
                newest = (patches() or [None])[0]
                if not newest:
                    return self._send(400, {'error': 'no patches in GameData/Patches/Balance'})
                old = active_patch()
                note = f'Active balance patch: {old} -> {newest}' if old != newest else f'Active balance patch already {newest}'
                job, err = start_job('update', [build_tools_args(), [PYTHON, '-u', 'Scripts/simpool.py', 'publish']], note=note)
                if not job:
                    return self._send(409, {'error': err})
                set_active_patch(newest)
                return self._send(200, {'job': job.summary(), 'patch': newest, 'previous': old})
            if u.path == '/api/iterate':
                busy = [j.id for j in JOBS.values() if j.status == 'running']
                if busy:  # before iteration_plan, which takes a fresh seed block
                    return self._send(409, {'error': f'{busy[0]} is still running. Wait for it or cancel it first.'})
                steps, desc = iteration_plan('chain' if body.get('mode') == 'chain' else 'measure', body.get('params') or {})
                job, err = start_job('iterate', steps, note='Next iteration: ' + desc)
                return self._send(200 if job else 409, {'job': job.summary(), 'desc': desc} if job else {'error': err})
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
.bar{height:8px;background:var(--code);border:1px solid var(--line);border-radius:999px;overflow:hidden;margin:6px 0 4px}
.bar>i{display:block;height:100%;background:var(--accent)}
.ext{border-top:1px solid var(--line);padding:10px 0} .ext:first-child{border-top:0;padding-top:0}
.events{font:12px/1.4 ui-monospace,Consolas,monospace;color:var(--muted);white-space:pre-wrap;word-break:break-all;margin-top:6px}
td.num,th.num{text-align:right;font-variant-numeric:tabular-nums}
.good{color:var(--ok)} .near{color:var(--warn)} .off{color:var(--bad);font-weight:600}
.chips{display:flex;gap:6px;flex-wrap:wrap;margin-top:6px}
.chip{border:1px solid var(--line);border-radius:999px;padding:2px 8px;font-size:12px;background:var(--code)}
.layer-tabs button{font-size:13px}
.lv-row{display:flex;gap:10px;align-items:center;flex-wrap:wrap;margin-bottom:10px}
.lv-row label{flex-direction:row;align-items:center;gap:6px}
td.hm{text-align:right;font-variant-numeric:tabular-nums}
td.dim{color:var(--muted);opacity:.55}
.sev{display:inline-block;min-width:36px;text-align:right;font-variant-numeric:tabular-nums}
.ov-status{font-weight:600}
.legend{display:flex;gap:12px;flex-wrap:wrap;color:var(--muted);font-size:12px;margin-top:8px}
.sw{display:inline-block;width:12px;height:12px;border-radius:3px;vertical-align:-2px;margin-right:4px}
th[title],td[title]{cursor:help}
.now{background:var(--panel);border:1px solid var(--line);border-radius:12px;padding:16px;margin-bottom:16px}
.now-head{display:flex;gap:10px;align-items:baseline;flex-wrap:wrap;margin-bottom:12px} .now-head h2{margin:0;font-size:17px}
.devices{display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:10px;margin-bottom:14px}
.dev{border:1px solid var(--line);border-radius:10px;padding:10px 12px;background:var(--code)}
.dev-top{display:flex;justify-content:space-between;align-items:center;gap:8px}
.dev-name{font-weight:600} .dev-big{font-size:22px;font-weight:700;font-variant-numeric:tabular-nums}
.dev-sub{color:var(--muted);font-size:12px}
.meter{height:6px;background:var(--bg);border-radius:999px;overflow:hidden;margin:6px 0 2px} .meter>i{display:block;height:100%;background:var(--ok)}
.meter.hot>i{background:var(--warn)}
.run{border:1px solid var(--line);border-radius:12px;padding:14px 16px;margin-top:10px;display:grid;grid-template-columns:minmax(0,1fr) auto;gap:4px 18px;align-items:start}
.run-title{font-size:16px;font-weight:700} .run-sub{color:var(--muted);font-size:13px}
.eta{text-align:right} .eta .big{font-size:30px;font-weight:800;line-height:1.1;font-variant-numeric:tabular-nums;color:var(--accent)}
.eta .when{color:var(--muted);font-size:13px}
.run .bigbar{grid-column:1/-1;height:12px;background:var(--code);border:1px solid var(--line);border-radius:999px;overflow:hidden;margin:8px 0 2px;position:relative}
.run .bigbar>i{display:block;height:100%;background:var(--accent);transition:width .6s}
.run .bigbar>b{position:absolute;inset:0;display:block;height:100%;background:color-mix(in srgb,var(--accent) 35%,transparent)}
.run .meta{grid-column:1/-1;display:flex;gap:6px;flex-wrap:wrap;margin-top:4px}
.pill{border:1px solid var(--line);border-radius:999px;padding:2px 9px;font-size:12px;background:var(--code)}
.pill.dev-spark{border-color:color-mix(in srgb,var(--accent) 50%,var(--line))} .pill.dev-pc{border-color:color-mix(in srgb,var(--ok) 50%,var(--line))}
.run details{grid-column:1/-1;margin-top:6px} .run summary{cursor:pointer;color:var(--muted);font-size:12px}
.steps{grid-column:1/-1;display:flex;gap:3px;margin-top:6px} .steps i{flex:1;height:5px;border-radius:3px;background:var(--line)}
.steps i.done{background:var(--ok)} .steps i.cur{background:var(--accent)}
@media (max-width:560px){.run{grid-template-columns:1fr}.eta{text-align:left}}
</style></head><body><main>
<h1>Balance runs</h1>
<div class="sub">Starts the DUNGEONSIM scripts on this PC and the DGX Spark. Active patch <b id="patch">—</b></div>

<section class="now" id="now">
 <div class="now-head"><h2>Running now</h2><span class="hint">every balance script on this PC, wherever it was started · refreshes every 5 s</span></div>
 <div class="devices" id="devices"></div>
 <div id="ext"><div class="hint">Nothing running.</div></div>
</section>

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
 <h2>Patch &amp; next iteration</h2>
 <div class="stats">
  <div class="stat"><div class="k">Active patch</div><div class="v" id="it-active" style="font-size:15px">—</div></div>
  <div class="stat"><div class="k">Newest patch file</div><div class="v" id="it-newest" style="font-size:15px">—</div></div>
  <div class="stat"><div class="k">Builds on</div><div class="v" id="it-base" style="font-size:15px">—</div></div>
 </div>
 <div class="row"><button class="btn" id="update-btn">Update to newest patch</button>
  <span class="hint" id="update-hint">Activates the newest patch, rebuilds the sim tools and republishes the Spark build.</span></div>
 <div class="row" style="margin-top:6px">
  <button class="btn primary" id="iter-btn">Run next iteration</button>
  <select id="iter-mode"><option value="measure">Measure (held-out, vs base patch)</option><option value="chain">Solve chain → next patches</option></select>
  <input id="iter-levels" placeholder="levels: all band midpoints" style="width:210px">
  <span class="msg" id="iter-msg"></span></div>
 <div class="hint" id="iter-hint"></div>
</section>



<section class="card" style="margin-top:14px">
 <div class="row" style="justify-content:space-between"><h2 style="margin:0">Results vs targets</h2>
  <label style="flex-direction:row;align-items:center;gap:6px">Run <select id="run-sel"></select></label></div>
 <div id="res"><div class="hint">No balance-loop runs yet.</div></div>
</section>

<section class="card" style="margin-top:14px">
 <div class="row" style="justify-content:space-between"><h2 style="margin:0">Balance layers</h2>
  <button class="btn" id="lay-refresh">Rescan result files</button></div>
 <div class="hint" style="margin-bottom:10px">Every layer the sims measure, each read from the newest result file of its kind in GameData/TuningRuns. Pick another file per tab; fill a missing layer with the Fun report / Material grid / Affix grid jobs below.</div>
 <div class="tabs layer-tabs" id="lay-tabs">
  <button data-l="overview" class="on">Overview</button><button data-l="classes">Classes</button><button data-l="materials">Materials</button>
  <button data-l="affixes">Affixes</button><button data-l="builds">Build diversity</button><button data-l="fights">Fight dynamics</button><button data-l="findings">Scan findings</button><button data-l="features">Enemy &amp; room features</button><button data-l="enemies">Enemies</button><button data-l="environments">Environments</button>
 </div>
 <div id="lay-body"><div class="hint">Loading…</div></div>
</section>

<section class="card" style="margin-top:14px">
 <div class="tabs" id="tabs">
  <button data-k="dungeon-sim" class="on">Dungeon sim</button><button data-k="bo-solve">Bo-solve</button><button data-k="balance-scan">Balance scan</button>
  <button data-k="fun-report">Fun report</button><button data-k="material-grid">Material grid</button><button data-k="affix-grid">Affix grid</button>
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
 <form id="f-fun-report" data-k="fun-report" hidden>
  <label>Levels<input name="levels" value="18,38,58"></label>
  <label>Shards / class<input name="shards" type="number" value="8" min="1"></label>
  <label>Heroes / shard<input name="runs" type="number" value="2" min="1"></label>
  <label>Dungeons / hero<input name="dungeons" type="number" value="6" min="1"></label>
  <label>Seed<input name="seed" type="number" value="1" min="0"></label>
  <div class="hint actions">Natural play with raw fight rows kept, then diversity-report: build diversity, adoption, choice and fight-dynamics layers. Compare two reports only at equal heroes and seeds (effective counts grow with sample size).</div>
 </form>
 <form id="f-material-grid" data-k="material-grid" hidden>
  <label>Levels<input name="levels" value="18,38,58"></label>
  <label>Shards / class<input name="shards" type="number" value="4" min="1"></label>
  <label>Heroes / shard<input name="runs" type="number" value="2" min="1"></label>
  <label>Dungeons / hero<input name="dungeons" type="number" value="6" min="1"></label>
  <label>Seed<input name="seed" type="number" value="1" min="0"></label>
  <label style="grid-column:span 2">Materials<input name="variants" placeholder="all (from MaterialBuilds.json)"></label>
  <div class="hint actions">One forced-material sim per material plus an unforced one (none), same seed, then the grid. ~19 sims.</div>
 </form>
 <form id="f-affix-grid" data-k="affix-grid" hidden>
  <label>Levels<input name="levels" value="18,38,58"></label>
  <label>Shards / class<input name="shards" type="number" value="4" min="1"></label>
  <label>Heroes / shard<input name="runs" type="number" value="2" min="1"></label>
  <label>Dungeons / hero<input name="dungeons" type="number" value="6" min="1"></label>
  <label>Seed<input name="seed" type="number" value="1" min="0"></label>
  <label style="grid-column:1/-1">Affixes<input name="variants" id="affix-variants" placeholder="mod:Heavy, mod:Keen, stat:CRIT, …"></label>
  <div class="hint actions" id="affix-hint">Each affix forced on every drop, priced against a stripped reference on the same seed (mod:- for prefixes, stat:- for suffixes). Two extra sims for the references.</div>
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
  if (kind === 'material-grid' || kind === 'affix-grid') t += '. Runs one sim per variant in a row';
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
  $('#run-btn').disabled = $('#build-btn').disabled = $('#iter-btn').disabled = $('#update-btn').disabled = busy;
  $('#it-active').textContent = s.patch; $('#it-newest').textContent = s.newest || '—'; $('#it-base').textContent = s.patchBase || '—';
  const behind = s.newest && s.newest !== s.patch;
  $('#it-newest').className = 'v' + (behind ? ' near' : '');
  $('#update-hint').textContent = (behind ? `${s.newest} is newer than the active ${s.patch}. ` : 'Active patch is the newest. ') +
    'Activates the newest patch, rebuilds the sim tools and republishes the Spark build.';
  iterHint(s);
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
  const id = selected, since = logNext;
  const j = await api(`/api/jobs/${id}?since=${since}`);
  // Two polls can overlap (the timer and selectJob): drop an answer another poll already applied.
  if (j.error || id !== selected || since !== logNext) return;
  $('#log-title').textContent = `${j.id} · ${j.status}${j.steps > 1 ? ` · step ${j.step} of ${j.steps}` : ''} · ${fmtTime(j.elapsed)}`;
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
let lastStatus = null;
function iterHint(s) {
  if (s) lastStatus = s; s = lastStatus; if (!s) return;
  const chain = $('#iter-mode').value === 'chain';
  $('#iter-hint').textContent = (chain
    ? `balance-chain.py on ${s.patch}: health → damage → class → damage solves, each writing the next ${s.patch.replace(/\d+[a-z]?$/, '')}N patch only when it beats noise, then a held-out measure. Hours. Activate the result with "Update to newest patch".`
    : `balance-loop.py measure on ${s.patch}${s.patchBase ? ' vs ' + s.patchBase : ''}, 4 fresh seeds. Shows up in Results vs targets as iter-${s.patch}-….`)
    + (s.tools.stale ? ' Sim tools are behind Code/, so it builds them first.' : '');
}
$('#iter-mode').onchange = () => iterHint();
$('#update-btn').onclick = async () => {
  const s = lastStatus;
  if (!confirm(`Make ${s && s.newest || 'the newest patch'} the active balance patch (GameData/PatchProfile.json), rebuild the sim tools and republish the Spark build?`)) return;
  const r = await api('/api/update', {});
  if (r.error) { alert(r.error); return; }
  selected = null; await loadStatus(); selectJob(r.job.id);
};
$('#iter-btn').onclick = async () => {
  const mode = $('#iter-mode').value, m = $('#iter-msg');
  if (mode === 'chain' && !confirm('Start a solve chain? It runs for hours and writes new patch files (not activated).')) return;
  m.className = 'msg'; m.textContent = 'Starting…';
  const r = await api('/api/iterate', {mode, params: {levels: $('#iter-levels').value, spark: document.querySelector('#common [name=spark]').value}});
  if (r.error) { m.className = 'msg bad'; m.textContent = r.error; return; }
  m.textContent = 'Started: ' + r.desc; selected = null; await loadStatus(); selectJob(r.job.id);
};
$('#build-btn').onclick = async () => {
  const r = await api('/api/build', {});
  if (r.error) { alert(r.error); return; }
  selected = null; await loadStatus(); selectJob(r.job.id);
};

const esc = s => String(s ?? '').replace(/[&<>"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));
const pct = (a, b) => b ? Math.min(100, Math.round(100 * a / b)) : 0;
// Wall-clock time <secs> from now, e.g. "14:32".
const clockAt = secs => new Date(Date.now() + 1000 * secs).toLocaleTimeString([], {hour: '2-digit', minute: '2-digit'});

// ---- Running now: device tiles + one card per run, ETA front and center.
let extData = null;

function deviceTile(name, dot, big, sub, load, cores, groups) {
  const frac = cores ? Math.min(1, load / cores) : 0;
  return `<div class="dev"><div class="dev-top"><span class="dev-name"><span class="dot ${dot}"></span>${esc(name)}</span><span class="dev-big">${big}</span></div>` +
    `<div class="dev-sub">${sub}</div>` +
    (cores ? `<div class="meter ${frac > 0.85 ? 'hot' : ''}"><i style="width:${Math.round(100 * frac)}%"></i></div><div class="dev-sub">load ${load.toFixed(1)} of ${cores} cores</div>` : '') +
    (groups && groups.length ? '<div class="chips">' + groups.map(g => `<span class="chip">L${esc(g.level)} ${esc(g.cls || 'all')} ×${g.n}</span>`).join('') + '</div>' : '') + '</div>';
}

function renderDevices() {
  const box = $('#devices'); if (!box) return;
  let h = '';
  if (!spark) h += deviceTile('DGX Spark', '', '…', 'checking…');
  else if (!spark.reachable) h += deviceTile('DGX Spark', 'bad', '—', esc(spark.error ? 'offline: ' + spark.error : 'offline'));
  else {
    const sh = spark.shards || {count: 0, groups: []};
    h += deviceTile('DGX Spark', spark.stale ? 'warn' : sh.count ? 'warn' : 'ok', `${sh.count} <span class="dev-sub">shards</span>`,
      (sh.count ? `running · bot ${esc((sh.bots || []).join(', '))}` : 'idle') + (spark.buildCurrent ? ' · build matches Code/' : ' · build behind Code/'),
      spark.load[0], spark.cores, sh.groups);
  }
  const pc = extData && extData.pc;
  if (pc) h += deviceTile('This PC', pc.count ? 'warn' : 'ok', `${pc.count} <span class="dev-sub">shards</span>`,
    (pc.count ? `running · bot ${esc((pc.bots || []).join(', '))}` : 'idle') + ` · ${pc.cores} cores`, null, null, pc.groups);
  box.innerHTML = h;
}

// Where a sim's shards run: Spark slots and/or local slots.
function simDevices(S) {
  if (!S) return '';
  const pills = [];
  const sp = S.spark, par = S.parallel;
  if (sp == null || sp !== '0') pills.push(`<span class="pill dev-spark">DGX Spark · ${sp == null || sp === 'auto' ? 'auto' : esc(sp)} slots</span>`);
  if (par == null || par !== '0') pills.push(`<span class="pill dev-pc">This PC · ${par == null ? 'all cores' : esc(par)} slots</span>`);
  return pills.join('');
}

function variantLabel(off) {
  if (off == null || off === '') return 'all features on';
  if (off === 'all') return 'all features off';
  return 'no ' + off.split(',').join(', no ');
}

function runTitle(x) {
  const B = x.sim && x.sim.batch;
  if (B) {
    const m = /^feature-test-s(\d+)$/.exec(B.name), dc = /^dungeon-calibrate-(.+)$/.exec(B.name), er = /^enemy-env-report-(.+)$/.exec(B.name);
    return m ? `Enemy & room feature test · seed ${m[1]}` : dc ? `Dungeon calibration → ${dc[1]}` : er ? `Enemy & environment report · ${er[1]}` : `Batch ${B.name}`;
  }
  if (x.script === 'balance-loop.py') return `Balance loop · ${x.loop && x.loop.cmd ? x.loop.cmd : ''}`;
  if (x.script === 'dungeon-sim.py') return `Dungeon sim · seed ${x.sim ? x.sim.seed : '?'}`;
  if (x.script === 'dungeon-calibrate.py') return 'Dungeon calibration';
  return x.script;
}

function renderRun(x) {
  const L = x.loop, S = x.sim, P = S && S.progress, B = S && S.batch;
  let frac = null, eta = null, step = '', steps = '';
  if (B && B.count) {
    const simElapsed = P && P.eta != null ? (S.elapsed != null ? S.elapsed : x.elapsed) : null;
    const per = simElapsed != null ? simElapsed + P.eta : null;
    frac = (B.index - 1 + (P && P.total ? P.done / P.total : 0)) / B.count;
    eta = P && P.eta != null ? P.eta + per * Math.max(0, B.count - B.index) : null;
    step = `Sim ${B.index} of ${B.count}` + ` · ${esc(variantLabel(S.featuresOff))}` + (per ? ` · ~${fmtTime(Math.round(per))} per sim` : '');
    steps = '<div class="steps">' + Array.from({length: B.count}, (_, i) => `<i class="${i + 1 < B.index ? 'done' : i + 1 === B.index ? 'cur' : ''}"></i>`).join('') + '</div>';
  } else if (L && L.total) {
    const t = L.simSeconds || [], per = t.length ? t.reduce((a, c) => a + c, 0) / t.length : null;
    frac = (L.done + (P && P.total ? P.done / P.total : 0)) / L.total;
    if (P && P.eta != null) eta = P.eta + (per ? per * Math.max(0, L.total - L.done - 1) : 0); else if (per) eta = per * (L.total - L.done);
    step = `Seed sims ${L.done} of ${L.total}` + (S ? ` · now seed ${esc(S.seed)}` : '') + (per ? ` · ~${fmtTime(Math.round(per))} per seed` : '');
  } else if (P && P.total) {
    frac = P.done / P.total; eta = P.eta;
    step = S ? esc(variantLabel(S.featuresOff)) : '';
  }
  const pctTxt = frac != null ? Math.round(100 * frac) + '%' : '';
  let h = `<div class="run"><div><div class="run-title">${esc(runTitle(x))}</div>` +
    `<div class="run-sub">${step}${step ? ' · ' : ''}${x.fromDashboard ? 'dashboard job' : 'started outside the dashboard'} · since ${esc(x.started)} (${fmtTime(x.elapsed)})</div></div>` +
    `<div class="eta">${eta != null ? `<div class="big">${fmtTime(Math.round(eta))}</div><div class="when">left · done around <b>${clockAt(eta)}</b></div>`
      : `<div class="big" style="font-size:20px">estimating…</div><div class="when">${P && P.running ? 'first shards running' : 'publishing / starting shards'}</div>`}</div>`;
  if (frac != null) h += `<div class="bigbar" title="${pctTxt}"><i style="width:${Math.min(100, 100 * frac).toFixed(1)}%"></i></div>`;
  h += steps;
  h += '<div class="meta">' + (pctTxt ? `<span class="pill"><b>${pctTxt}</b> overall</span>` : '') +
    (P ? `<span class="pill">shards ${P.done}${P.total ? ' / ' + P.total : ''} done · ${P.running} running</span>` : '') +
    (P && P.eta != null && (B || (L && L.total)) ? `<span class="pill">this sim ~${fmtTime(P.eta)} (${clockAt(P.eta)})</span>` : '') +
    simDevices(S) +
    (S ? `<span class="pill">L ${esc(S.levels)}</span><span class="pill">bot ${esc(S.bot)}</span><span class="pill">${esc(S.shards)} shards/class</span>` : '') +
    (x.patch ? `<span class="pill">patch ${esc(x.patch)}</span>` : '') + '</div>';
  h += `<details><summary>Command and log · pid ${x.pid}</summary><div class="cmd">${esc(x.command)}</div>` +
    (L && L.events && L.events.length ? `<div class="events">${esc(L.events.slice(-4).join('\n'))}</div>` : '') + '</details>';
  return h + '</div>';
}

function renderSparkShards() { renderDevices(); }

async function loadExternal() {
  const r = await api('/api/external');
  const box = $('#ext');
  if (r.error) { box.innerHTML = `<div class="msg bad">Could not list running scripts: ${esc(r.error)}</div>`; return; }
  extData = r; renderDevices();
  if (!r.runs || !r.runs.length) { box.innerHTML = '<div class="hint">Nothing running. Start a job below or run a script from a terminal; it shows up here.</div>'; return; }
  box.innerHTML = r.runs.map(renderRun).join('');
}

let resRun = null;
async function loadRuns() {
  const r = await api('/api/loopruns');
  const sel = $('#run-sel');
  if ((r.runs || []).join() !== [...sel.options].map(o => o.value).join()) {
    const cur = sel.value; sel.innerHTML = '';
    for (const n of r.runs || []) { const o = document.createElement('option'); o.value = o.textContent = n; sel.appendChild(o); }
    sel.value = r.runs.includes(cur) ? cur : (r.runs[0] || '');
  }
  resRun = sel.value; loadResults();
}
$('#run-sel').onchange = () => { resRun = $('#run-sel').value; loadResults(); };

function clsCell(v, tgt, tol) {
  if (v == null) return '<td class="num hint">—</td>';
  const g = v - tgt, c = Math.abs(g) <= tol ? 'good' : Math.abs(g) <= 2 * tol ? 'near' : 'off';
  return `<td class="num ${c}" title="${g >= 0 ? '+' : ''}${g.toFixed(1)} vs target">${v.toFixed(1)}</td>`;
}

// The band's three dungeons: clear % each, coloured against the level's target.
function dungeonCells(L, tol) {
  const d = L.perDungeon || {}; const ks = Object.keys(d);
  if (!ks.length) return '<span class="hint">—</span>';
  return ks.map(t => { const g = L.target != null ? d[t] - L.target : 0;
    const c = Math.abs(g) <= tol ? 'good' : Math.abs(g) <= 2 * tol ? 'near' : 'off';
    return `<span class="chip" title="${esc(t)}: ${d[t]}% clear">${esc(t)} <b class="${c}">${d[t].toFixed(0)}</b></span>`; }).join(' ');
}

async function loadResults() {
  if (!resRun) return;
  const r = await api('/api/results?run=' + encodeURIComponent(resRun));
  const box = $('#res');
  if (r.error) { box.innerHTML = `<div class="hint">${esc(r.error)}</div>`; return; }
  if (!r.patches.length) { box.innerHTML = '<div class="hint">No seed finished yet in this run. Rows appear as each seed\'s sim completes.</div>'; return; }
  const tol = r.clearTol || 4, par = r.parity || 10;
  box.innerHTML = r.patches.map(p => {
    const s = p.summary;
    let h = `<div style="margin-top:10px"><b>${esc(p.key)}</b> <span class="hint">· seeds ${esc(p.seeds.join(', '))}${r.finished.length ? '' : ' (partial: averages of the seeds done so far)'}</span></div>`;
    h += `<div class="chips"><span class="chip">mean |clear gap| <b>${s.meanClearGap ?? '—'}</b> pts</span><span class="chip">mean class spread <b>${s.meanClassSpread ?? '—'}</b> (limit ${par})</span>` +
      `<span class="chip">bands over parity <b>${s.bandsOverParity}</b> of ${p.levels.length}</span><span class="chip">mean |actions gap| <b>${s.meanTurnsGap ?? '—'}</b></span></div>`;
    h += '<div class="tablewrap" style="margin-top:8px"><table><thead><tr><th>Level</th><th class="num">Target</th><th class="num">Clear</th><th class="num">Gap</th>' +
      '<th class="num">Hero acts</th><th class="num">Acts target</th><th class="num">Barb</th><th class="num">War</th><th class="num">Rogue</th><th class="num">Wiz</th><th class="num">Spread</th><th>By dungeon</th><th class="num">Dungeons</th></tr></thead><tbody>';
    for (const L of p.levels) {
      const gc = Math.abs(L.gap) <= tol ? 'good' : Math.abs(L.gap) <= 2 * tol ? 'near' : 'off';
      const ad = L.heroActions != null && L.turnsTarget ? Math.abs(L.heroActions - L.turnsTarget) : 0;
      const ac = ad <= 1.5 ? '' : ad <= 3 ? 'near' : 'off';
      const sc = L.spread == null ? '' : L.spread <= par ? 'good' : L.spread <= 1.5 * par ? 'near' : 'off';
      h += `<tr><td>L${L.level}</td><td class="num">${L.target ?? '—'}</td><td class="num">${L.clear}${L.clearSE != null ? '<span class="hint"> ±' + L.clearSE + '</span>' : ''}</td>` +
        `<td class="num ${gc}">${L.gap > 0 ? '+' : ''}${L.gap}</td><td class="num ${ac}">${L.heroActions ?? '—'}</td><td class="num">${L.turnsTarget ?? '—'}</td>` +
        ['Barbarian','Warrior','Rogue','Wizard'].map(c => clsCell(L.classes[c], L.target, tol)).join('') +
        `<td class="num ${sc}">${L.spread ?? '—'}</td><td>${dungeonCells(L, tol)}</td><td class="num hint">${L.dungeons}</td></tr>`;
    }
    return h + '</tbody></table></div>';
  }).join('') + `<div class="hint" style="margin-top:8px">Green within ±${tol} pts of the clear target (realClearTolerance), amber within ±${2*tol}, red beyond. Spread is best minus worst class (limit ${par}). Hero acts amber/red past 1.5/3 actions off target.</div>`;
}

// ------------------------------------------------------------ balance layers
const CLS = ['Barbarian', 'Warrior', 'Rogue', 'Wizard'];
const CLS_SHORT = {Barbarian:'Barb', Warrior:'War', Rogue:'Rogue', Wizard:'Wiz', all:'All'};
// Thresholds: balance-scan defaults (close-call band, one-shot cap, length CV floor, p90 cap, class distance floor).
const LIM = {closeLo: 5, closeHi: 15, oneShot: 25, lengthCv: 0.25, p90: 2.5, classDist: 0.25, matGood: 10, matNear: 20,
             affixDead: 2, affixStrong: 15};
const PCT_KEYS = new Set(['clear','offclass_share','classless_share','set_rate','set4_rate','contest_share','close_call','one_shot',
  'miss_streak','combo_share','crit_share','convert_share','strip_completion']);
const METRIC = {
  clear: ['Dungeon clear', 'share of dungeons cleared'],
  material_eff: ['Material variety', 'effective number of materials worn (inverse Simpson): higher = more materials in use'],
  offclass_share: ['Off-class material share', 'worn pieces of another class\'s material'],
  classless_share: ['Class-less material share', 'worn pieces of a class-less material (Stone, Wood, Cloth, …)'],
  set_rate: ['Set bonus active', 'heroes with at least a 2-piece set'],
  set4_rate: ['4-piece set', 'heroes with a 4+ piece set'],
  set_eff: ['Set variety', 'effective number of different sets heroes run'],
  affix_eff: ['Affix variety', 'effective number of affix lines worn'],
  stat_eff: ['Suffix variety', 'effective number of stat suffixes worn'],
  mod_eff: ['Prefix variety', 'effective number of adjective prefixes worn'],
  act_eff: ['Action-bonus variety', 'effective number of action bonus lines worn'],
  affixes_per_piece: ['Affixes per piece', 'mean affix lines on a worn piece'],
  strip_eff: ['Combo strip variety', 'effective number of distinct combo strips'],
  skill_eff: ['Skill variety', 'effective number of skill picks'],
  build_eff: ['Distinct builds', 'effective number of whole-build fingerprints (saturates: almost every hero is unique)'],
  build_distance: ['Build distance', 'mean Jaccard distance between two heroes\' builds: 0 = clones, 1 = nothing shared'],
  slot_contest: ['Slot contest', 'runner-up item value / best item value when gear is offered: near 1 = close calls'],
  contest_share: ['Contested choices', 'gear choices where the runner-up is within 10%'],
  upgrades_per_hero: ['Upgrades per hero', 'gear swaps per hero per run'],
  fights: ['Fights', 'fights in the sample'],
  close_call: ['Close calls', 'fights won under 15% HP (target band 5–15%)'],
  lead_changes: ['Lead changes', 'times the HP lead flips per fight'],
  length_cv: ['Length variation', 'fight-length coefficient of variation (floor 0.25: every fight the same length is dull)'],
  p90_ratio: ['Long-fight tail', 'p90 / median fight length (cap 2.5)'],
  one_shot: ['One-shot fights', 'fights won in one hero action (cap 25%)'],
  miss_streak: ['Miss streaks', 'fights with 4+ misses in a row'],
  action_eff: ['Action variety', 'effective number of hero actions used per fight'],
  dot_mix: ['Damage-source mix', 'effective number of damage sources (direct, DoT, convert, …)'],
  status_eff: ['Status variety', 'effective number of status effects applied'],
  enemy_move_eff: ['Enemy move variety', 'effective number of enemy actions seen per fight'],
  combo_share: ['Combo share', 'hero actions that advance a combo'],
  crit_share: ['Crit share', 'hits that crit'],
  convert_share: ['Convert share', 'damage from material convert actions'],
  strip_completion: ['Strip completion', 'fights where the hero finished the whole combo strip'],
};
const BUILD_KEYS = ['material_eff','classless_share','offclass_share','set_rate','set4_rate','set_eff','affix_eff','stat_eff','mod_eff',
  'act_eff','affixes_per_piece','strip_eff','skill_eff','build_eff','build_distance','slot_contest','contest_share','upgrades_per_hero'];
const FIGHT_KEYS = ['close_call','lead_changes','length_cv','p90_ratio','one_shot','miss_streak','action_eff','dot_mix','status_eff',
  'enemy_move_eff','combo_share','crit_share','convert_share','strip_completion','fights'];

let layerTab = 'overview', layerSrc = null, layerMeta = null, layerPick = {}, layerLevel = {}, layerOpt = {viable: true, compare: ''};
const fileCache = new Map();

async function layerFile(path) {
  if (!path) return null;
  const key = path + '@' + ((layerSrc && Object.values(layerSrc).flat().find(x => x.path === path) || {}).mtime || '');
  if (fileCache.has(key)) return fileCache.get(key);
  const r = await api('/api/layers/file?path=' + encodeURIComponent(path));
  const v = r.error ? {error: r.error} : r.data;
  fileCache.set(key, v);
  return v;
}

const fmtV = (k, v) => {
  if (v == null || (typeof v === 'number' && isNaN(v))) return '—';
  if (typeof v !== 'number') return esc(v);
  if (PCT_KEYS.has(k)) return (100 * v).toFixed(1) + '%';
  return Math.abs(v) >= 100 ? v.toFixed(0) : Math.abs(v) >= 10 ? v.toFixed(1) : v.toFixed(2);
};
const mtitle = k => esc((METRIC[k] || [k, ''])[1]);
const mname = k => esc((METRIC[k] || [k])[0]);
const avg = a => { a = a.filter(x => x != null && !isNaN(x)); return a.length ? a.reduce((x, y) => x + y, 0) / a.length : null; };
const sd = a => { a = a.filter(x => x != null); if (a.length < 2) return null; const m = avg(a); return Math.sqrt(a.reduce((s, x) => s + (x - m) ** 2, 0) / (a.length - 1)); };
// Diverging cell tint: green above, red below, strength by |d| / scale.
const tint = (d, scale) => {
  if (d == null || isNaN(d)) return '';
  const a = Math.min(1, Math.abs(d) / scale) * 0.55;
  return `background:color-mix(in srgb, ${d >= 0 ? 'var(--ok)' : 'var(--bad)'} ${Math.round(a * 100)}%, transparent)`;
};
const status = (cls, text) => `<span class="ov-status ${cls}">${esc(text)}</span>`;

function srcSelect(kind, label) {
  const list = (layerSrc && layerSrc[kind]) || [];
  if (!list.length) return `<span class="hint">No ${label} file yet.</span>`;
  const cur = layerPick[kind] || list[0].path;
  return `<label>${label} <select data-src="${kind}">` + list.map(x => `<option value="${esc(x.path)}"${x.path === cur ? ' selected' : ''}>${esc(x.path)} · ${esc(x.when)}</option>`).join('') + '</select></label>';
}
const picked = kind => layerPick[kind] || ((layerSrc && layerSrc[kind] && layerSrc[kind][0]) || {}).path;

function levelChips(id, levels) {
  if (!levels.length) return '';
  const cur = layerLevel[id] && levels.includes(layerLevel[id]) ? layerLevel[id] : levels[0];
  layerLevel[id] = cur;
  return '<span class="tabs" style="margin:0">' + levels.map(L => `<button data-lvl="${id}:${L}" class="${L === cur ? 'on' : ''}">L${esc(L)}</button>`).join('') + '</span>';
}

// Viable sets for a class: its own three materials plus every class-less one.
const viable = (mat, cls) => {
  const owner = (layerMeta.materials || {})[mat];
  return mat === 'none' || owner === '' || owner === undefined || owner === cls;
};

function materialStats(grid) {
  // Per level: per class SD and range of clear over viable sets (pts), the number the material solve balanced.
  const out = {};
  for (const [L, lv] of Object.entries(grid || {})) {
    const per = {};
    for (const c of CLS) {
      const vals = Object.entries(lv.variants || {}).filter(([m, r]) => m !== 'none' && viable(m, c) && r[c] != null).map(([, r]) => 100 * r[c]);
      if (vals.length > 1) per[c] = {sd: sd(vals), range: Math.max(...vals) - Math.min(...vals), n: vals.length};
    }
    out[L] = per;
  }
  return out;
}

function affixDeltas(grid) {
  // variant -> level -> {cls: delta pts} vs its family's stripped reference (or the unforced 'none' run).
  const out = {};
  for (const [L, lv] of Object.entries(grid || {})) {
    const V = lv.variants || {};
    for (const [name, r] of Object.entries(V)) {
      if (name === 'none' || name.endsWith('-strip')) continue;
      const fam = name.startsWith('mod-') ? 'mod' : name.startsWith('stat-') ? 'stat' : '';
      const base = V[fam + '-strip'] || V.none;
      if (!base) continue;
      const d = {};
      for (const c of [...CLS, 'all']) if (r[c] != null && base[c] != null) d[c] = 100 * (r[c] - base[c]);
      (out[name] = out[name] || {})[L] = {d, ref: V[fam + '-strip'] ? fam + '-strip' : 'none'};
    }
  }
  return out;
}

async function renderOverview(box) {
  const rows = [];
  // Curve: newest balance-loop run.
  const runs = (await api('/api/loopruns')).runs || [];
  if (runs.length) {
    const r = await api('/api/results?run=' + encodeURIComponent(runs[0]));
    const p = (r.patches || [])[0];
    if (p) {
      const s = p.summary, tol = r.clearTol || 4, par = r.parity || 10;
      const st = s.meanClearGap == null ? ['hint', 'no data'] : s.meanClearGap <= tol / 2 ? ['good', 'on target'] : s.meanClearGap <= tol ? ['near', 'close'] : ['off', 'off target'];
      rows.push(['Difficulty curve', `balance-loop/${runs[0]} · ${p.key}`, `mean |clear gap| <b>${s.meanClearGap ?? '—'}</b> pts · hero actions gap ${s.meanTurnsGap ?? '—'}`, st]);
      const ss = s.meanClassSpread == null ? ['hint', '—'] : s.meanClassSpread <= par ? ['good', 'within parity'] : s.meanClassSpread <= 1.5 * par ? ['near', 'over parity'] : ['off', 'far over'];
      rows.push(['Class parity (curve)', `same run`, `mean class spread <b>${s.meanClassSpread ?? '—'}</b> pts (limit ${par}) · ${s.bandsOverParity} of ${p.levels.length} levels over`, ss]);
    }
  } else rows.push(['Difficulty curve', '—', 'no balance-loop run yet', ['hint', 'missing']]);

  const scan = await layerFile(picked('scan'));
  if (scan && scan.metrics) {
    const m = scan.metrics, cc = CLS.map(c => m['class clear ' + c]).filter(x => x != null);
    const spread = cc.length ? Math.max(...cc) - Math.min(...cc) : null, dist = m['closest class pair distance'];
    rows.push(['Class identity', picked('scan'), `scan class clear spread <b>${spread?.toFixed(1) ?? '—'}</b> pts · closest class pair distance <b>${dist ?? '—'}</b> (floor ${LIM.classDist})`,
      dist == null ? ['hint', '—'] : dist >= LIM.classDist ? ['good', 'distinct'] : ['off', 'classes play alike']]);
    const fails = (scan.findings || []).filter(f => f.status === 'FAIL'), en = fails.filter(f => /enemy/i.test(f.area));
    rows.push(['Enemies & scan checks', picked('scan'), `<b>${fails.length}</b> failing checks, <b>${en.length}</b> of them per enemy` + (en[0] ? ` · worst ${esc(en[0].subject)} L${esc(en[0].level)}` : ''),
      fails.length === 0 ? ['good', 'clean'] : fails.length <= 5 ? ['near', 'some'] : ['off', 'many']]);
  } else rows.push(['Class identity / enemies', '—', 'no balance-scan file yet', ['hint', 'missing']]);

  const mg = await layerFile(picked('materialGrid'));
  if (mg && !mg.error) {
    const st = materialStats(mg), sds = [], rgs = [];
    for (const per of Object.values(st)) for (const v of Object.values(per)) { sds.push(v.sd); rgs.push(v.range); }
    const msd = avg(sds), mr = avg(rgs);
    rows.push(['Material balance', picked('materialGrid'), `forced-set clear, viable sets per class: mean SD <b>${msd?.toFixed(1) ?? '—'}</b> pts · mean range <b>${mr?.toFixed(1) ?? '—'}</b> pts`,
      msd == null ? ['hint', '—'] : msd <= LIM.matGood ? ['good', 'level'] : msd <= LIM.matNear ? ['near', 'uneven'] : ['off', 'lopsided']]);
  } else rows.push(['Material balance', '—', 'no material grid yet', ['hint', 'missing']]);

  const ag = await layerFile(picked('affixGrid'));
  if (ag && !ag.error) {
    const D = affixDeltas(ag); let dead = 0, strong = 0, n = 0;
    for (const lv of Object.values(D)) { const a = avg(Object.values(lv).map(x => x.d.all)); if (a == null) continue; n++; if (Math.abs(a) < LIM.affixDead) dead++; if (a > LIM.affixStrong) strong++; }
    rows.push(['Affix balance', picked('affixGrid'), `${n} affixes priced · <b>${dead}</b> dead (|Δ| < ${LIM.affixDead} pts) · <b>${strong}</b> dominant (> +${LIM.affixStrong})`,
      n === 0 ? ['hint', '—'] : dead + strong === 0 ? ['good', 'all pull weight'] : (dead + strong) / n <= 0.25 ? ['near', 'a few outliers'] : ['off', 'many outliers']]);
  } else rows.push(['Affix balance', '—', 'no affix grid yet', ['hint', 'missing']]);

  const rep = await layerFile(picked('report'));
  if (rep && rep.levels) {
    const L = Object.values(rep.levels).map(x => x.all);
    const g = k => avg(L.map(x => x[k]));
    const cl = g('classless_share');
    rows.push(['Build diversity', picked('report'), `material variety <b>${fmtV('material_eff', g('material_eff'))}</b> · class-less wear <b>${fmtV('classless_share', cl)}</b> · skill variety <b>${fmtV('skill_eff', g('skill_eff'))}</b> · strip variety <b>${fmtV('strip_eff', g('strip_eff'))}</b>`,
      cl == null ? ['hint', '—'] : cl >= 0.1 ? ['good', 'varied'] : cl >= 0.05 ? ['near', 'narrow'] : ['off', 'home sets only']]);
    const cc = 100 * (g('close_call') || 0), os_ = 100 * (g('one_shot') || 0), cv = g('length_cv'), p9 = g('p90_ratio');
    const bad = [cc < LIM.closeLo || cc > LIM.closeHi, os_ > LIM.oneShot, cv != null && cv < LIM.lengthCv, p9 != null && p9 > LIM.p90].filter(Boolean).length;
    rows.push(['Fight dynamics', picked('report'), `close calls <b>${cc.toFixed(1)}%</b> (band ${LIM.closeLo}–${LIM.closeHi}) · one-shots <b>${os_.toFixed(1)}%</b> · length CV <b>${fmtV('length_cv', cv)}</b> · lead changes <b>${fmtV('lead_changes', g('lead_changes'))}</b>`,
      bad === 0 ? ['good', 'lively'] : bad === 1 ? ['near', '1 check off'] : ['off', bad + ' checks off']]);
  } else rows.push(['Build diversity / fights', '—', 'no diversity report yet (run Fun report)', ['hint', 'missing']]);

  enemyEnvOverviewRows(rows, await layerFile(picked('enemyEnv')), picked('enemyEnv'));

  box.innerHTML = '<div class="tablewrap"><table><thead><tr><th>Layer</th><th>Status</th><th>Headline</th><th>Source</th></tr></thead><tbody>' +
    rows.map(([n, src, h, [c, t]]) => `<tr><td><b>${esc(n)}</b></td><td>${status(c, t)}</td><td style="white-space:normal">${h}</td><td class="hint" style="white-space:normal">${esc(src)}</td></tr>`).join('') +
    '</tbody></table></div><div class="hint" style="margin-top:8px">Each row reads the newest file of its kind (or the one picked on its tab). Values from different files can come from different patches and seeds: check the source column.</div>';
}

function renderMaterials(box, grid, rep) {
  if (!grid || grid.error) { box.innerHTML = '<div class="lv-row">' + srcSelect('materialGrid', 'Grid') + '</div><div class="hint">Run a Material grid job to fill this layer.</div>'; return; }
  const levels = Object.keys(grid), L = (levelChips('mat', levels), layerLevel.mat);
  const V = grid[L].variants || {}, st = materialStats(grid)[L] || {}, none = V.none;
  const owner = layerMeta.materials || {};
  const adopt = rep && rep.levels && rep.levels[L] ? rep.levels[L].adoption || {} : {};
  const mats = Object.keys(V).filter(m => m !== 'none').sort((a, b) => (owner[a] || 'zz').localeCompare(owner[b] || 'zz') || a.localeCompare(b));
  let h = '<div class="lv-row">' + srcSelect('materialGrid', 'Grid') + levelChips('mat', levels) +
    `<label class="chk"><input type="checkbox" id="opt-viable" ${layerOpt.viable ? 'checked' : ''}>Viable sets only</label></div>`;
  h += '<div class="chips" style="margin-bottom:8px">' + CLS.map(c => { const s = st[c]; if (!s) return '';
    const cl = s.sd <= LIM.matGood ? 'good' : s.sd <= LIM.matNear ? 'near' : 'off';
    return `<span class="chip">${c}: SD <b class="${cl}">${s.sd.toFixed(1)}</b> · range ${s.range.toFixed(1)} pts over ${s.n} sets</span>`; }).join('') + '</div>';
  h += '<div class="tablewrap"><table><thead><tr><th>Material</th><th>Class</th>' + CLS.map(c => `<th class="num" title="clear % with every piece forced to this material; tint = vs the class's viable mean">${CLS_SHORT[c]}</th>`).join('') +
    '<th class="num">All</th><th class="num" title="hero actions per fight">Acts</th><th class="num" title="from the picked diversity report: worn / offered item passes in natural play">Worn share</th><th class="num">Offered</th></tr></thead><tbody>';
  const mean = {}; for (const c of CLS) mean[c] = avg(mats.filter(m => viable(m, c)).map(m => V[m][c] != null ? 100 * V[m][c] : null));
  for (const m of mats) {
    const r = V[m], a = adopt['mat:' + m];
    h += `<tr><td><b>${esc(m)}</b></td><td class="hint">${esc(owner[m] || 'class-less')}</td>` + CLS.map(c => {
      const ok = viable(m, c), v = r[c];
      if (v == null || (!ok && layerOpt.viable)) return '<td class="hm dim">·</td>';
      return `<td class="hm ${ok ? '' : 'dim'}" style="${ok ? tint(100 * v - mean[c], 20) : ''}" title="${(100 * v - mean[c]).toFixed(1)} vs ${c} viable mean">${(100 * v).toFixed(0)}</td>`;
    }).join('') + `<td class="hm">${r.all != null ? (100 * r.all).toFixed(0) : '—'}</td><td class="hm hint">${r.acts != null ? r.acts.toFixed(1) : '—'}</td>` +
      `<td class="hm">${a ? (100 * a.worn_share).toFixed(0) + '%' : '—'}</td><td class="hm hint">${a ? a.offered : '—'}</td></tr>`;
  }
  if (none) h += `<tr><td><i>none (unforced)</i></td><td></td>` + CLS.map(c => `<td class="hm hint">${none[c] != null ? (100 * none[c]).toFixed(0) : '—'}</td>`).join('') +
    `<td class="hm hint">${none.all != null ? (100 * none.all).toFixed(0) : '—'}</td><td class="hm hint">${none.acts != null ? none.acts.toFixed(1) : '—'}</td><td></td><td></td></tr>`;
  h += '</tbody></table></div><div class="legend"><span><i class="sw" style="background:color-mix(in srgb,var(--ok) 55%,transparent)"></i>above the class\'s viable-set mean</span><span><i class="sw" style="background:color-mix(in srgb,var(--bad) 55%,transparent)"></i>below</span>' +
    `<span>SD good ≤ ${LIM.matGood}, uneven ≤ ${LIM.matNear} pts</span><span>Worn share: from ${rep ? esc(picked('report')) : 'no report'}</span></div>`;
  box.innerHTML = h;
}

function renderAffixes(box, grid, rep) {
  if (!grid || grid.error) { box.innerHTML = '<div class="lv-row">' + srcSelect('affixGrid', 'Grid') + '</div><div class="hint">Run an Affix grid job to fill this layer.</div>'; return; }
  const levels = Object.keys(grid); levelChips('aff', levels); const L = layerLevel.aff;
  const D = affixDeltas(grid), adopt = rep && rep.levels && rep.levels[L] ? rep.levels[L].adoption || {} : {};
  const names = Object.keys(D).filter(n => D[n][L]).sort((a, b) => (D[b][L].d.all ?? -99) - (D[a][L].d.all ?? -99));
  let h = '<div class="lv-row">' + srcSelect('affixGrid', 'Grid') + levelChips('aff', levels) + '</div>';
  h += '<div class="tablewrap"><table><thead><tr><th>Affix</th><th class="hint">vs</th>' + [...CLS, 'all'].map(c => `<th class="num" title="clear pts vs the stripped reference">Δ ${CLS_SHORT[c]}</th>`).join('') +
    '<th>Verdict</th><th class="num" title="natural play, from the picked diversity report">Worn share</th><th class="num">Offered</th></tr></thead><tbody>';
  for (const n of names) {
    const {d, ref} = D[n][L], all = d.all, label = n.replace(/^mod-/, 'mod:').replace(/^stat-/, 'stat:');
    const a = adopt[label];
    const verdict = all == null ? '' : Math.abs(all) < LIM.affixDead ? status('off', 'dead') : all > LIM.affixStrong ? status('near', 'dominant') : all < 0 ? status('off', 'hurts') : status('good', 'pulls weight');
    h += `<tr><td><b>${esc(label)}</b></td><td class="hint">${esc(ref)}</td>` + [...CLS, 'all'].map(c => d[c] == null ? '<td class="hm dim">·</td>' :
      `<td class="hm" style="${tint(d[c], 20)}">${d[c] > 0 ? '+' : ''}${d[c].toFixed(0)}</td>`).join('') +
      `<td>${verdict}</td><td class="hm">${a ? (100 * a.worn_share).toFixed(0) + '%' : '—'}</td><td class="hm hint">${a ? a.offered : '—'}</td></tr>`;
  }
  h += `</tbody></table></div><div class="legend"><span>Δ = clear pts with the affix on every drop minus the stripped reference on the same seed</span><span>dead: |Δ all| &lt; ${LIM.affixDead}</span><span>dominant: Δ all &gt; +${LIM.affixStrong}</span></div>`;
  // Natural adoption of affixes the grid did not price.
  const rows = Object.entries(adopt).filter(([k, r]) => /^(mod|stat|act):/.test(k) && r.offered >= 30).sort((a, b) => b[1].worn_share - a[1].worn_share);
  if (rows.length) h += '<h2 style="margin-top:14px">Natural adoption (L' + esc(L) + ')</h2><div class="hint" style="margin-bottom:6px">How often the bot wears an affix when it is offered. Low worn share with plenty offered = players will see it and skip it.</div><div class="chips">' +
    rows.map(([k, r]) => `<span class="chip" title="offered ${r.offered} item passes">${esc(k)} <b class="${r.worn_share < 0.05 ? 'off' : r.worn_share < 0.12 ? 'near' : 'good'}">${(100 * r.worn_share).toFixed(0)}%</b></span>`).join('') + '</div>';
  box.innerHTML = h;
}

// ---- Enemy / environment layers (enemy-env-report-*.json from Scripts/enemy-env-report.py).
const FLAG = {deadly: ['off', 'deadly'], soft: ['near', 'soft'], hard: ['off', 'too hard'], easy: ['near', 'too easy'], ok: ['good', 'ok']};
const flagCell = f => f ? status(...(FLAG[f] || ['hint', f])) : '';

function costTable(rows, label, opts = {}) {
  if (!rows || !rows.length) return '<div class="hint">No rows.</div>';
  const scale = opts.scale || 8, lim = opts.limit;
  let t = `<div class="tablewrap"><table><thead><tr><th>${label}</th>${opts.extraHead || ''}<th class="num">Fights</th>` +
    `<th class="num" title="HP lost per fight minus the level's average fight, pts of max HP">Δ HP cost</th>` +
    `<th class="num" title="hero deaths per fight">Death %</th><th class="num" title="death rate / the level's death rate">× level</th>` +
    `<th class="num">Hero acts</th><th class="num">Enemy acts</th>${opts.flags ? '<th>Read</th>' : ''}</tr></thead><tbody>`;
  for (const r of rows) {
    t += `<tr><td><b>${esc(r.name)}</b></td>${opts.extra ? opts.extra(r) : ''}<td class="hm hint">${r.n}</td>` +
      `<td class="hm" style="${tint(-r.hp, scale)}">${r.hp > 0 ? '+' : ''}${r.hp.toFixed(1)}</td><td class="hm">${r.death.toFixed(1)}</td>` +
      `<td class="hm">${r.deathRatio != null ? r.deathRatio.toFixed(2) : '—'}</td><td class="hm">${r.heroActs.toFixed(1)}</td><td class="hm">${r.enemyActs.toFixed(1)}</td>` +
      (opts.flags ? `<td>${flagCell(r.flag)}</td>` : '') + '</tr>';
  }
  return t + '</tbody></table></div>' + (lim != null ? `<div class="legend"><span>flagged past ±${lim} pts HP cost, or deaths ≥ the death-ratio limit</span></div>` : '');
}

function renderEnemies(box, rep) {
  if (!rep || rep.error) { box.innerHTML = '<div class="lv-row">' + srcSelect('enemyEnv', 'Report') + '</div><div class="hint">No enemy/environment report yet: python Scripts/enemy-env-report.py --patch &lt;patch&gt;</div>'; return; }
  const S = rep.summary, L = rep.limits;
  const view = layerOpt.enemyView || 'outliers';
  let h = '<div class="lv-row">' + srcSelect('enemyEnv', 'Report') +
    `<span class="tabs" style="margin:0">${['outliers', 'all'].map(v => `<button data-ev="${v}" class="${v === view ? 'on' : ''}">${v === 'outliers' ? 'Flagged only' : 'All enemies'}</button>`).join('')}</span></div>`;
  h += `<div class="hint" style="margin-bottom:8px">${esc(rep.note)}</div><div class="chips">` +
    `<span class="chip">enemies priced <b>${S.enemiesPriced}</b> (≥ ${L.minFights} fights)</span><span class="chip">deadly <b class="${S.enemiesDeadly ? 'off' : 'good'}">${S.enemiesDeadly}</b></span>` +
    `<span class="chip">soft <b>${S.enemiesSoft}</b></span><span class="chip">deadly Demons <b class="${S.bossesDeadly ? 'near' : 'good'}">${S.bossesDeadly}</b></span>` +
    `<span class="chip">prefix outliers <b>${S.prefixOutliers}</b></span><span class="chip">dungeon affix outliers <b>${S.dungeonAffixOutliers}</b></span>` +
    `<span class="chip">Demon reached phase 2 <b>${S.demonPhaseRate ?? '—'}%</b></span></div>`;
  const en = view === 'all' ? rep.enemies : rep.enemies.filter(r => r.flag !== 'ok');
  h += `<h2 style="margin-top:14px">Enemies ${view === 'all' ? '' : '(flagged)'}</h2>` + costTable(en, 'Enemy', {flags: true, limit: L.enemyHp, scale: 12,
    extraHead: '<th>Home</th><th>Kit</th>', extra: r => `<td class="hint">${esc(r.home || '')}</td><td class="hint" style="white-space:normal">${esc((r.kit || []).map(k => k.replace('ENEMY ', '')).join(', '))}</td>`});
  h += '<h2 style="margin-top:14px">Demons (vs other boss fights at the level)</h2>' + costTable(view === 'all' ? rep.bosses : rep.bosses.filter(r => r.flag !== 'ok'), 'Demon', {flags: true, scale: 15});
  h += '<h2 style="margin-top:14px">Demon phase</h2>' + costTable(rep.demon, 'Phase');
  h += '<h2 style="margin-top:14px">Prefixes</h2>' + costTable(rep.prefixes, 'Prefix', {flags: true, limit: L.affixHp});
  h += '<h2 style="margin-top:14px">Region affixes (rank = region cycle)</h2>' + costTable(rep.regionAffixes, 'Region affix');
  h += '<h2 style="margin-top:14px">Dungeon affixes (Rare enemies, Lairs)</h2>' + costTable(rep.dungeonAffixes, 'Dungeon affix', {flags: true, limit: L.affixHp});
  box.innerHTML = h;
  box.querySelectorAll('[data-ev]').forEach(b => b.onclick = () => { layerOpt.enemyView = b.dataset.ev; renderEnemies(box, rep); });
}

function renderEnvironments(box, rep) {
  if (!rep || rep.error) { box.innerHTML = '<div class="lv-row">' + srcSelect('enemyEnv', 'Report') + '</div><div class="hint">No enemy/environment report yet: python Scripts/enemy-env-report.py --patch &lt;patch&gt;</div>'; return; }
  const S = rep.summary, L = rep.limits;
  let h = '<div class="lv-row">' + srcSelect('enemyEnv', 'Report') + '</div>' + `<div class="hint" style="margin-bottom:8px">${esc(rep.note)}</div><div class="chips">` +
    `<span class="chip">mean dungeon spread in a band <b class="${S.meanDungeonSpread == null ? '' : S.meanDungeonSpread <= L.dungeonGap ? 'good' : S.meanDungeonSpread <= 2 * L.dungeonGap ? 'near' : 'off'}">${S.meanDungeonSpread ?? '—'}</b> pts</span>` +
    `<span class="chip">dungeons off their band <b class="${S.dungeonsOff ? 'off' : 'good'}">${S.dungeonsOff}</b> of 12</span><span class="chip">room affix outliers <b>${S.roomAffixOutliers}</b></span></div>`;
  // Band × dungeon grid.
  const lv = rep.levels || {};
  if (Object.keys(lv).length) {
    h += '<h2 style="margin-top:14px">Clear % by dungeon (tint = vs its band mean)</h2><div class="tablewrap"><table><thead><tr><th>Level</th><th class="num">Band</th><th>Dungeons</th><th class="num">Spread</th></tr></thead><tbody>';
    for (const [Lv, r] of Object.entries(lv)) {
      h += `<tr><td>L${esc(Lv)}</td><td class="hm">${r.band.toFixed(0)}</td><td>` + Object.entries(r.dungeons).map(([t, d]) =>
        `<span class="chip" style="${tint(d.clear - r.band, 20)}" title="${d.n} dungeons">${esc(t)} <b>${d.clear.toFixed(0)}</b></span>`).join(' ') +
        `</td><td class="hm ${r.spread <= L.dungeonGap ? 'good' : r.spread <= 2 * L.dungeonGap ? 'near' : 'off'}">${r.spread.toFixed(0)}</td></tr>`;
    }
    h += '</tbody></table></div>';
  }
  if (rep.dungeons && rep.dungeons.length) {
    h += '<h2 style="margin-top:14px">Dungeons vs their band (all levels)</h2><div class="tablewrap"><table><thead><tr><th>Dungeon</th><th>Region</th><th class="num">Mean gap</th><th class="num">Worst level</th><th class="num">Dungeons run</th><th>Read</th></tr></thead><tbody>' +
      rep.dungeons.map(r => `<tr><td><b>${esc(r.name)}</b></td><td class="hint">${esc(r.region)}</td><td class="hm" style="${tint(r.gap, 15)}">${r.gap > 0 ? '+' : ''}${r.gap.toFixed(1)}</td>` +
        `<td class="hm">${r.worstGap > 0 ? '+' : ''}${r.worstGap.toFixed(0)}</td><td class="hm hint">${r.dungeons}</td><td>${flagCell(r.flag)}</td></tr>`).join('') +
      `</tbody></table></div><div class="legend"><span>gap = dungeon clear minus its band's mean, pts; flagged past ±${L.dungeonGap}</span></div>`;
  }
  h += '<h2 style="margin-top:14px">Room affixes</h2>' + costTable(rep.rooms, 'Room affix', {flags: true, limit: L.affixHp});
  h += '<h2 style="margin-top:14px">Terrain tags</h2>' + costTable(rep.terrain, 'Terrain');
  h += '<h2 style="margin-top:14px">Wandering rooms and enemies</h2>' + costTable(rep.wander, 'Kind');
  box.innerHTML = h;
}

function enemyEnvOverviewRows(rows, rep, src) {
  if (!rep || rep.error || !rep.summary) { rows.push(['Enemies / environments', '—', 'no enemy/environment report yet', ['hint', 'missing']]); return; }
  const S = rep.summary, L = rep.limits;
  rows.push(['Enemy balance', src, `<b>${S.enemiesDeadly}</b> deadly · <b>${S.enemiesSoft}</b> soft of ${S.enemiesPriced} enemies · ${S.bossesDeadly} deadly Demons · ${S.prefixOutliers} prefix outliers`,
    S.enemiesDeadly === 0 && S.prefixOutliers === 0 ? ['good', 'even'] : S.enemiesDeadly <= 5 ? ['near', 'a few outliers'] : ['off', 'many outliers']]);
  rows.push(['Environment balance', src, `dungeon spread in a band <b>${S.meanDungeonSpread ?? '—'}</b> pts (limit ${L.dungeonGap}) · <b>${S.dungeonsOff}</b> dungeons off their band · ${S.roomAffixOutliers} room affix outliers`,
    S.meanDungeonSpread == null ? ['hint', 'no per-dungeon data'] : S.dungeonsOff === 0 && S.meanDungeonSpread <= L.dungeonGap ? ['good', 'even'] : S.dungeonsOff <= 2 ? ['near', 'uneven'] : ['off', 'lopsided']]);
}

// Enemy / room affix feature test (feature-test-*.json from the paired --features-off runs, same seed).
function renderFeatures(box, ft) {
  if (!ft || ft.error) { box.innerHTML = '<div class="lv-row">' + srcSelect('featureTest', 'Test') + '</div><div class="hint">No feature test yet: run the paired dungeon-sim --features-off variants and write feature-test-*.json.</div>'; return; }
  const levels = Object.keys(ft.levels || {}); levelChips('feat', levels); const L = layerLevel.feat;
  const lv = ft.levels[L] || {}, on = lv.on || {};
  let h = '<div class="lv-row">' + srcSelect('featureTest', 'Test') + levelChips('feat', levels) + '</div>';
  h += `<div class="hint" style="margin-bottom:6px">${esc(ft.note || '')}</div>`;
  h += '<h2>Clear % with one feature switched off (Δ vs all on, same seed)</h2><div class="tablewrap"><table><thead><tr><th>Variant</th>' +
    [...CLS, 'all'].map(c => `<th class="num">${CLS_SHORT[c]}</th>`).join('') + '<th class="num">Death %</th><th class="num">Hero acts</th><th class="num">Dungeons</th><th>Read</th></tr></thead><tbody>';
  const order = ['on', 'off', ...Object.keys(lv).filter(k => k !== 'on' && k !== 'off')];
  for (const v of order) {
    const r = lv[v]; if (!r) continue;
    const isOn = v === 'on';
    const cell = c => { if (r[c] == null) return '<td class="hm dim">·</td>'; const val = 100 * r[c];
      if (isOn) return `<td class="hm">${val.toFixed(0)}</td>`; const d = val - 100 * (on[c] ?? 0); return `<td class="hm" style="${tint(d, 15)}">${d > 0 ? '+' : ''}${d.toFixed(1)}</td>`; };
    const dAll = isOn ? null : 100 * ((r.all ?? 0) - (on.all ?? 0));
    const read = isOn ? 'reference' : v === 'off' ? 'every feature off' : dAll == null ? '' : Math.abs(dAll) < 3 ? status('good', 'small (<3 pts)') : dAll > 0 ? status('near', `costs the hero ${dAll.toFixed(0)} pts`) : status('off', `helps the hero ${(-dAll).toFixed(0)} pts`);
    h += `<tr><td><b>${esc(isOn ? 'all on' : v === 'off' ? 'all off' : 'no ' + v)}</b></td>` + [...CLS, 'all'].map(cell).join('') +
      `<td class="hm">${r.death != null ? (100 * r.death).toFixed(1) : '—'}</td><td class="hm">${r.acts != null ? r.acts.toFixed(1) : '—'}</td><td class="hm hint">${r.dungeons ?? '—'}</td><td>${read}</td></tr>`;
  }
  h += '</tbody></table></div><div class="legend"><span>Δ = clear pts with that feature off minus all on. Positive = the feature makes dungeons harder.</span></div>';
  const occ = ft.occurrence || {};
  h += '<h2 style="margin-top:14px">How often each feature shows up (all on, every level)</h2><div class="chips">' +
    Object.entries(occ).map(([k, v]) => `<span class="chip">${esc(k)} <b>${esc(String(v))}</b></span>`).join('') + '</div>';
  const costTable = (title, rows, label) => {
    if (!rows || !rows.length) return '';
    let t = `<h2 style="margin-top:14px">${title}</h2><div class="tablewrap"><table><thead><tr><th>${label}</th><th class="num">Fights</th><th class="num" title="HP lost per fight, minus the level's average (pts of max HP)">Δ HP lost</th><th class="num">Hero death %</th><th class="num">Hero acts</th></tr></thead><tbody>`;
    for (const r of rows) t += `<tr><td><b>${esc(r.name)}</b></td><td class="hm hint">${r.n}</td><td class="hm" style="${tint(-r.hp, 8)}">${r.hp > 0 ? '+' : ''}${r.hp.toFixed(1)}</td><td class="hm">${r.death.toFixed(1)}</td><td class="hm">${r.acts.toFixed(1)}</td></tr>`;
    return t + '</tbody></table></div>';
  };
  h += costTable('Enemy prefixes (non-boss fights)', ft.prefixCost, 'Prefix');
  h += costTable('Room affixes', ft.roomCost, 'Room affix');
  h += costTable('Demon fights', ft.demonCost, 'Phase');
  if (ft.dungeons && ft.dungeons.length) {
    h += '<h2 style="margin-top:14px">Per dungeon (home fights, all on vs all off)</h2><div class="tablewrap"><table><thead><tr><th>Dungeon</th><th class="num">Fights</th><th class="num">HP lost on</th><th class="num">HP lost off</th><th class="num">Δ</th><th class="num">Death on</th><th class="num">Death off</th><th class="num">Acts on</th><th class="num">Acts off</th></tr></thead><tbody>';
    for (const r of ft.dungeons) h += `<tr><td><b>${esc(r.name)}</b></td><td class="hm hint">${r.n}</td><td class="hm">${r.hpOn.toFixed(1)}</td><td class="hm">${r.hpOff.toFixed(1)}</td><td class="hm" style="${tint(-(r.hpOn - r.hpOff), 6)}">${r.hpOn - r.hpOff > 0 ? '+' : ''}${(r.hpOn - r.hpOff).toFixed(1)}</td><td class="hm">${r.deathOn.toFixed(1)}</td><td class="hm">${r.deathOff.toFixed(1)}</td><td class="hm">${r.actsOn.toFixed(1)}</td><td class="hm">${r.actsOff.toFixed(1)}</td></tr>`;
    h += '</tbody></table></div>';
  }
  box.innerHTML = h;
}

function metricTable(rep, cmp, keys, L) {
  const lv = rep.levels[L], lc = cmp && cmp.levels ? cmp.levels[L] : null;
  let h = '<div class="tablewrap"><table><thead><tr><th>Metric</th>' + [...CLS, 'all'].map(c => `<th class="num">${CLS_SHORT[c]}</th>`).join('') + (lc ? '<th class="num" title="all classes: this report minus the comparison">Δ all</th>' : '') + '</tr></thead><tbody>';
  for (const k of keys) {
    if (!lv.all || !(k in lv.all)) continue;
    const vals = [...CLS, 'all'].map(c => lv[c] ? lv[c][k] : null);
    let cell = (c, v) => `<td class="hm">${fmtV(k, v)}</td>`;
    if (k === 'close_call') cell = (c, v) => `<td class="hm ${v == null ? '' : 100 * v < LIM.closeLo || 100 * v > LIM.closeHi ? 'off' : 'good'}">${fmtV(k, v)}</td>`;
    if (k === 'one_shot') cell = (c, v) => `<td class="hm ${v != null && 100 * v > LIM.oneShot ? 'off' : ''}">${fmtV(k, v)}</td>`;
    if (k === 'length_cv') cell = (c, v) => `<td class="hm ${v != null && v < LIM.lengthCv ? 'off' : ''}">${fmtV(k, v)}</td>`;
    if (k === 'p90_ratio') cell = (c, v) => `<td class="hm ${v != null && v > LIM.p90 ? 'off' : ''}">${fmtV(k, v)}</td>`;
    let d = '';
    if (lc) { const a = lv.all[k], b = lc.all ? lc.all[k] : null;
      d = typeof a === 'number' && typeof b === 'number' ? `<td class="hm ${a > b ? 'good' : a < b ? 'off' : ''}">${a - b > 0 ? '+' : ''}${PCT_KEYS.has(k) ? (100 * (a - b)).toFixed(1) : (a - b).toFixed(2)}</td>` : '<td class="hm dim">·</td>'; }
    h += `<tr><td title="${mtitle(k)}">${mname(k)}</td>` + [...CLS, 'all'].map((c, i) => cell(c, vals[i])).join('') + d + '</tr>';
  }
  return h + '</tbody></table></div>';
}

function compareSelect() {
  const list = (layerSrc && layerSrc.report) || [];
  return `<label>Compare with <select id="cmp-sel"><option value="">—</option>` + list.filter(x => x.path !== picked('report')).map(x =>
    `<option value="${esc(x.path)}"${x.path === layerOpt.compare ? ' selected' : ''}>${esc(x.path)}</option>`).join('') + '</select></label>';
}

function renderReportLayer(box, rep, cmp, keys, extra) {
  if (!rep || !rep.levels) { box.innerHTML = '<div class="lv-row">' + srcSelect('report', 'Report') + '</div><div class="hint">Run a Fun report job to fill this layer.</div>'; return; }
  const levels = Object.keys(rep.levels); levelChips('rep', levels); const L = layerLevel.rep;
  let h = '<div class="lv-row">' + srcSelect('report', 'Report') + compareSelect() + levelChips('rep', levels) + '</div>';
  if (cmp && cmp.levels) h += '<div class="hint" style="margin-bottom:6px">Δ is only meaningful at equal heroes per class and the same seeds: effective counts grow with sample size.</div>';
  h += metricTable(rep, cmp, keys, L);
  if (extra) h += extra(rep.levels[L], L);
  box.innerHTML = h + '<div class="hint" style="margin-top:6px">Hover a metric for what it measures. Effective counts are inverse Simpson: N = N things used evenly.</div>';
}

function topSets(lv) {
  return '<h2 style="margin-top:14px">Top sets by class</h2><div class="tablewrap"><table><tbody>' + CLS.map(c =>
    `<tr><td><b>${c}</b></td><td style="white-space:normal">${((lv[c] || {}).top_sets || []).map(([m, n]) => `<span class="chip">${esc(m)} ${n}</span>`).join(' ')}</td></tr>`).join('') + '</tbody></table></div>';
}

function scanPacing(scan) {
  if (!scan || !scan.metrics) return '';
  const m = scan.metrics, lv = [...new Set(Object.keys(m).map(k => (k.match(/^close calls L(\d+)$/) || [])[1]).filter(Boolean))].sort((a, b) => a - b);
  if (!lv.length) return '';
  const keys = [['close calls', 'close calls', '%'], ['dungeon close calls', 'dungeon close calls', '%'], ['one-shot fights', 'one-shot fights', '%'], ['lead changes', 'lead changes', ''],
                ['fight length cv', 'fight length cv', ''], ['fight length p90/p50', 'p90 / p50', ''], ['miss streak 4+', 'miss streak 4+', '%'], ['dungeon clear', 'dungeon clear', '%'], ['bot hero acts', 'hero actions / fight', '']];
  return `<h2 style="margin-top:14px">Pacing from the balance scan</h2><div class="hint" style="margin-bottom:6px">${esc(picked('scan'))} · small sample, gross misses only</div><div class="tablewrap"><table><thead><tr><th>Metric</th>` +
    lv.map(L => `<th class="num">L${L}</th>`).join('') + '</tr></thead><tbody>' + keys.map(([k, n, u]) => `<tr><td>${esc(n)}</td>` +
    lv.map(L => { const v = m[`${k} L${L}`]; return `<td class="hm">${v == null ? '—' : v + u}</td>`; }).join('') + '</tr>').join('') + '</tbody></table></div>';
}

function renderClasses(box, scan) {
  if (!scan || !scan.metrics) { box.innerHTML = '<div class="lv-row">' + srcSelect('scan', 'Scan') + '</div><div class="hint">Run a Balance scan to fill this layer.</div>'; return; }
  const m = scan.metrics;
  let h = '<div class="lv-row">' + srcSelect('scan', 'Scan') + `<span class="hint">patch ${esc(scan.patch)}</span></div>`;
  const cc = CLS.map(c => m['class clear ' + c]);
  h += '<div class="chips" style="margin-bottom:10px">' + CLS.map((c, i) => `<span class="chip">${c} clear <b>${cc[i] ?? '—'}%</b></span>`).join('') +
    `<span class="chip">closest class pair distance <b class="${m['closest class pair distance'] >= LIM.classDist ? 'good' : 'off'}">${m['closest class pair distance'] ?? '—'}</b> (floor ${LIM.classDist})</span></div>`;
  const ids = [...new Set(Object.keys(m).filter(k => k.startsWith('identity ')).map(k => k.split(' ').slice(2).join(' ')))];
  if (ids.length) {
    h += '<h2>Class identity</h2><div class="hint" style="margin-bottom:6px">How each class plays: share or z-like lean per trait. A distinct column per class is the goal.</div><div class="tablewrap"><table><thead><tr><th>Trait</th>' + CLS.map(c => `<th class="num">${CLS_SHORT[c]}</th>`).join('') + '</tr></thead><tbody>';
    for (const t of ids) {
      const vals = CLS.map(c => m[`identity ${c} ${t}`]), mu = avg(vals), s = sd(vals) || 1;
      h += `<tr><td>${esc(t)}</td>` + vals.map(v => `<td class="hm" style="${v == null ? '' : tint((v - mu) / s, 2)}">${v ?? '—'}</td>`).join('') + '</tr>';
    }
    h += '</tbody></table></div>';
  }
  const lv = [...new Set(Object.keys(m).map(k => (k.match(/^effective actions L(\d+) /) || [])[1]).filter(Boolean))].sort((a, b) => a - b);
  const rows = [['effective actions', 'Effective actions'], ['action breadth', 'Action breadth'], ['slots used', 'Combo slots used'], ['strip completion', 'Strip completion %'],
                ['step advance', 'Step advance %'], ['late slot damage', 'Late-slot damage %'], ['breaks miss', 'Strip breaks on miss %'], ['hero tec', 'Hero TEC'], ['hero int', 'Hero INT']];
  if (lv.length) {
    h += '<h2 style="margin-top:14px">Action and combo use by class</h2><div class="tablewrap"><table><thead><tr><th>Metric</th><th>Level</th>' + CLS.map(c => `<th class="num">${CLS_SHORT[c]}</th>`).join('') + '</tr></thead><tbody>';
    for (const [k, n] of rows) for (const L of lv) {
      const vals = CLS.map(c => m[`${k} L${L} ${c}`]);
      if (vals.every(v => v == null)) continue;
      h += `<tr><td>${esc(n)}</td><td class="hint">L${L}</td>` + vals.map(v => `<td class="hm">${v ?? '—'}</td>`).join('') + '</tr>';
    }
    h += '</tbody></table></div>';
  }
  const sig = Object.keys(m).filter(k => k.startsWith('signature action share'));
  if (sig.length) h += '<div class="chips" style="margin-top:8px">' + sig.map(k => `<span class="chip" title="one action's share of a class's actions: high = one-button class">${esc(k.replace('signature action share ', ''))} <b>${m[k]}</b></span>`).join('') + '</div>';
  box.innerHTML = h;
}

function renderFindings(box, scan) {
  if (!scan || !scan.findings) { box.innerHTML = '<div class="lv-row">' + srcSelect('scan', 'Scan') + '</div><div class="hint">Run a Balance scan to fill this layer.</div>'; return; }
  const areas = [...new Set(scan.findings.map(f => f.area))];
  const cur = layerOpt.area && areas.includes(layerOpt.area) ? layerOpt.area : '';
  const rows = scan.findings.filter(f => !cur || f.area === cur);
  let h = '<div class="lv-row">' + srcSelect('scan', 'Scan') + `<label>Area <select id="area-sel"><option value="">all (${scan.findings.length})</option>` +
    areas.map(a => `<option${a === cur ? ' selected' : ''}>${esc(a)}</option>`).join('') + '</select></label></div>';
  h += '<div class="tablewrap"><table><thead><tr><th class="num">Severity</th><th>Status</th><th>Area</th><th>Level</th><th>Subject</th><th>Value</th><th>Target</th><th>Detail</th></tr></thead><tbody>' +
    rows.map(f => `<tr title="${esc(f.focus || '')}"><td class="num"><span class="sev">${f.severity != null ? (+f.severity).toFixed(1) : ''}</span></td><td class="${f.status === 'FAIL' ? 'off' : f.status === 'WARN' ? 'near' : 'good'}">${esc(f.status)}</td>` +
      `<td>${esc(f.area)}</td><td>${f.level != null ? 'L' + esc(f.level) : ''}</td><td>${esc(f.subject)}</td><td>${esc(f.value)}</td><td class="hint">${esc(f.target)}</td><td class="hint" style="white-space:normal">${esc(f.detail)}</td></tr>`).join('') +
    '</tbody></table></div><div class="hint" style="margin-top:6px">Hover a row for the command that focuses on it.</div>';
  box.innerHTML = h;
}

async function renderLayers() {
  const box = $('#lay-body');
  if (!layerSrc) {
    const r = await api('/api/layers/sources');
    layerSrc = r.sources; layerMeta = r;
    const aff = r.affixes || {};
    $('#affix-hint').textContent += ` Prefixes: ${(aff.mods || []).join(', ')}. Suffixes: ${(aff.stats || []).join(', ')}.`;
  }
  const scrollY = window.scrollY;
  try {
    if (layerTab === 'overview') await renderOverview(box);
    else if (layerTab === 'materials') renderMaterials(box, await layerFile(picked('materialGrid')), await layerFile(picked('report')));
    else if (layerTab === 'affixes') renderAffixes(box, await layerFile(picked('affixGrid')), await layerFile(picked('report')));
    else if (layerTab === 'builds') renderReportLayer(box, await layerFile(picked('report')), await layerFile(layerOpt.compare), BUILD_KEYS, topSets);
    else if (layerTab === 'fights') { const scan = await layerFile(picked('scan')); renderReportLayer(box, await layerFile(picked('report')), await layerFile(layerOpt.compare), FIGHT_KEYS, () => scanPacing(scan)); }
    else if (layerTab === 'classes') renderClasses(box, await layerFile(picked('scan')));
    else if (layerTab === 'findings') renderFindings(box, await layerFile(picked('scan')));
    else if (layerTab === 'features') renderFeatures(box, await layerFile(picked('featureTest')));
    else if (layerTab === 'enemies') renderEnemies(box, await layerFile(picked('enemyEnv')));
    else if (layerTab === 'environments') renderEnvironments(box, await layerFile(picked('enemyEnv')));
  } catch (ex) { box.innerHTML = `<div class="msg bad">Could not render this layer: ${esc(ex.message)}</div>`; }
  window.scrollTo(0, scrollY);
}

document.querySelectorAll('#lay-tabs button').forEach(b => b.onclick = () => {
  layerTab = b.dataset.l;
  document.querySelectorAll('#lay-tabs button').forEach(x => x.classList.toggle('on', x === b));
  renderLayers();
});
$('#lay-body').addEventListener('change', e => {
  const t = e.target;
  if (t.dataset.src) layerPick[t.dataset.src] = t.value;
  else if (t.id === 'opt-viable') layerOpt.viable = t.checked;
  else if (t.id === 'cmp-sel') layerOpt.compare = t.value;
  else if (t.id === 'area-sel') layerOpt.area = t.value;
  renderLayers();
});
$('#lay-body').addEventListener('click', e => {
  const b = e.target.closest('button[data-lvl]');
  if (!b) return;
  const [id, L] = b.dataset.lvl.split(':'); layerLevel[id] = L; renderLayers();
});
$('#lay-refresh').onclick = async () => { const r = await api('/api/layers/sources?force=1'); layerSrc = r.sources; layerMeta = r; fileCache.clear(); renderLayers(); };
// New result files from finished jobs show up without a reload.
setInterval(async () => {
  const r = await api('/api/layers/sources');
  if (!r.sources) return;
  const sig = s => JSON.stringify(Object.values(s).map(v => v.map(x => x.path + x.mtime)));
  if (!layerSrc || sig(r.sources) !== sig(layerSrc)) { layerSrc = r.sources; layerMeta = r; if (layerTab === 'overview' || !document.querySelector('#lay-body select:focus')) renderLayers(); }
}, 20000);
renderLayers();

const _loadSpark = loadSpark;
loadSpark = async f => { await _loadSpark(f); renderSparkShards(); };
loadStatus(); loadSpark(false); loadExternal(); loadRuns();
setInterval(loadStatus, 3000); setInterval(pollLog, 1500); setInterval(() => loadSpark(false), 15000);
setInterval(loadExternal, 5000); setInterval(loadRuns, 10000);
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
