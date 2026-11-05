#!/usr/bin/env python3
"""Shard runner shared by the sim scripts: local DF.dll processes plus optional slots on the DGX Spark over SSH.

Each shard takes whichever slot is free, so the faster machine simply takes more shards. Spark slots run the
linux-arm64 build in ~/df/app (republished automatically when Code/ source changed since the last publish) against an
upload of this session's scratch GameData, so a Spark shard sees exactly the data a local shard does. With
DF_GAMEDATA_DIR set, seeded shards are bit-identical across the two machines (checked 2026-11-04, 80 shards).

  pool = SimPool(root, work, data_dir, dll, local_slots=8, spark_slots=18)
  pool.start()
  host = pool.run(['DUNGEONSIM', '--patch', patch, ..., '--json', out], out, log_path, timeout=1800)
  pool.close()

Paths inside `work` (patch copies, the --json output) are mapped to the Spark session folder; inputs are uploaded once.
Setup: `ssh spark` must work without a password (see ~/.ssh/config).
"""
import hashlib, os, queue, random, shlex, subprocess, tarfile, threading, time

# GameData folders headless sims never read (UI art, audio, logs): left out of every scratch GameData copy. ItemArt alone
# is ~220 MB, and copying it was ~5 s of every dungeon-sim.py call.
SIM_SKIP = {'Audio', 'Video', 'CombatLogs', 'LabSnapshots', 'TuningRuns', 'ItemArt', 'RoomArt', 'StatusArt'}
# Also not to be copied off this PC (credentials).
UPLOAD_SKIP = SIM_SKIP | {'secrets', 'SheetsOAuthToken'}


SPARK_DEFAULT_SLOTS = 18


def spark_slots_arg(value):
    """--spark value: an int, or 'auto' (the default): SPARK_DEFAULT_SLOTS when `ssh <DF_SPARK_HOST or spark>` answers
    within a few seconds, else 0. DF_SPARK=0 forces local; DF_SPARK=N forces N. The probe runs once per process."""
    env = os.environ.get('DF_SPARK')
    if str(value).lower() != 'auto':
        return int(value)
    if env not in (None, '', 'auto'):
        return int(env)
    global _SPARK_PROBE
    if _SPARK_PROBE is None:
        host = os.environ.get('DF_SPARK_HOST', 'spark')
        try:
            ok = subprocess.run(['ssh', '-o', 'BatchMode=yes', '-o', 'ConnectTimeout=4', host, 'true'],
                                capture_output=True, timeout=10).returncode == 0
        except Exception:
            ok = False
        _SPARK_PROBE = SPARK_DEFAULT_SLOTS if ok else 0
        print(f'Spark: {"reachable, " + str(_SPARK_PROBE) + " slots" if ok else "not reachable, local only"} (--spark auto)', flush=True)
    return _SPARK_PROBE


_SPARK_PROBE = None


def add_args(ap):
    """The pool's CLI flags, shared by every script that uses it."""
    ap.add_argument('--spark', type=spark_slots_arg, default='auto',
                    help="shards to run at once on the DGX Spark: auto (default: %d if `ssh spark` answers, else 0), 0 = local only, or N" % SPARK_DEFAULT_SLOTS)
    ap.add_argument('--spark-host', default='spark', help='ssh host of the Spark (an ~/.ssh/config alias)')


def source_hash(root):
    """Hash of the game source the Spark build is made from (Code/ minus build output and tests)."""
    code = os.path.join(root, 'Code')
    h = hashlib.sha1()
    for d, dirs, files in os.walk(code):
        rel = os.path.relpath(d, code).replace(os.sep, '/')
        dirs[:] = sorted(x for x in dirs if not (rel == '.' and x in ('bin', 'obj', 'Tests')))
        for f in sorted(files):
            if f.endswith(('.cs', '.csproj', '.axaml', '.xaml', '.json', '.props', '.targets')):
                p = os.path.join(d, f)
                h.update(os.path.relpath(p, code).replace(os.sep, '/').encode())
                with open(p, 'rb') as fh:
                    h.update(fh.read())
    return h.hexdigest()[:16]


class SimPool:
    def __init__(self, root, work, data_dir, dll, local_slots, spark_slots=0, host='spark', log=print):
        self.root, self.work, self.data_dir, self.dll = root, os.path.abspath(work), data_dir, dll
        self.local_slots, self.spark_slots, self.host, self.log = max(0, local_slots), max(0, spark_slots), host, log
        self.base = ''      # absolute ~/df on the Spark (paths go through shlex.quote, so no "~"); set by start()
        self.remote = ''    # this session's folder under base/runs
        self.counts = {'local': 0, 'spark': 0}
        self.retries = 0   # ssh calls retried after a dropped connection
        self._slots = queue.Queue()
        self._uploaded = set()
        self._lock = threading.Lock()

    @property
    def slots(self):
        return self.local_slots + self.spark_slots

    def describe(self):
        return f'{self.local_slots} local' + (f' + {self.spark_slots} on {self.host}' if self.spark_slots else '')

    # ------------------------------------------------------------------ setup

    def _ssh(self, command, attempts=5, **kw):
        """One ssh call, retried when ssh itself fails (exit 255). A round opens up to one connection per slot at once,
        and sshd's default MaxStartups (10:30:100) drops some of those handshakes ("kex_exchange_identification",
        "Connection closed by ... port 22"); before this retry those shards were silently dropped (2026-11-04)."""
        stdin = kw.get('stdin')
        for attempt in range(attempts):
            r = subprocess.run(['ssh', '-o', 'BatchMode=yes', '-o', 'ConnectTimeout=10', self.host, command], **kw)
            if r.returncode != 255 or attempt == attempts - 1:
                return r
            with self._lock:
                self.retries += 1
            time.sleep(random.uniform(0.3, 1.5) * (attempt + 1))
            if stdin is not None and hasattr(stdin, 'seek'):
                stdin.seek(0)
        return r

    def start(self):
        if self.spark_slots:
            try:
                self._start_remote()
            except Exception as e:
                self.log(f'WARNING: Spark unavailable ({e}); running {self.local_slots} local slots only')
                self.spark_slots = 0
        if self.slots == 0:
            raise SystemExit('No sim slots (local 0, Spark off).')
        self._warn_stale_local_dll()
        for _ in range(self.spark_slots):
            self._slots.put('spark')
        for _ in range(self.local_slots):
            self._slots.put('local')

    def _start_remote(self):
        r = self._ssh('echo $HOME; cat ~/df/app/.source-hash 2>/dev/null; find ~/df/runs -mindepth 1 -maxdepth 1 -mmin +1440 '
                      '-exec rm -rf {} + 2>/dev/null; true', capture_output=True, text=True, timeout=30)
        if r.returncode != 0:
            raise RuntimeError((r.stderr or 'ssh failed').strip().splitlines()[-1])
        lines = r.stdout.split()
        self.base = lines[0] + '/df'
        self.remote = f'{self.base}/runs/{os.environ.get("COMPUTERNAME", "pc").lower()}-{os.getpid()}-{int(time.time())}'
        want = source_hash(self.root)
        if (lines[1] if len(lines) > 1 else '') != want:
            self._publish(want)
        self._upload_tree(self.data_dir, f'{self.remote}/GameData')

    def _publish(self, stamp):
        out = os.path.join(self.root, 'Code', 'bin', 'SparkPublish')
        self.log(f'Publishing linux-arm64 build for {self.host} (source {stamp})...')
        r = subprocess.run(['dotnet', 'publish', os.path.join(self.root, 'Code', 'Code.csproj'), '-c', 'Release', '-r', 'linux-arm64',
                            '--self-contained', '-p:PublishSingleFile=false', '-o', out,
                            f'-p:BaseIntermediateOutputPath={os.path.join(self.work, "obj-arm64")}{os.sep}'],
                           cwd=self.root, capture_output=True, text=True)
        if r.returncode != 0:
            raise RuntimeError('linux-arm64 publish failed: ' + r.stdout[-1500:])
        # Swap the folder in whole: shards from other sessions keep their open files.
        t = time.time()
        sent = self._stage_app(out)
        self.log(f'  uploaded {sent} to {self.host} in {time.time() - t:.0f} s')
        self._ssh(f'chmod +x {self.base}/app.new/DF && echo {stamp} > {self.base}/app.new/.source-hash && '
                  f'rm -rf {self.base}/app.old && (mv {self.base}/app {self.base}/app.old 2>/dev/null; true) && '
                  f'mv {self.base}/app.new {self.base}/app && rm -rf {self.base}/app.old', check=True, timeout=120)

    def _stage_app(self, out):
        """Builds base/app.new from the publish folder `out`, sending only files whose content differs from base/app.
        app.new starts as a hard-link copy of app; changed files are extracted with --unlink-first so the links break
        and app's copies stay untouched for shards still running on it. A code change usually touches DF.dll and a
        few files, not the ~340 MB the full upload sent (~3 min). Returns a short description of what was sent."""
        local = {}
        for d, _, files in os.walk(out):
            for f in files:
                p = os.path.join(d, f)
                with open(p, 'rb') as fh:
                    local[os.path.relpath(p, out).replace(os.sep, '/')] = hashlib.sha1(fh.read()).hexdigest()
        app, new = f'{self.base}/app', f'{self.base}/app.new'
        r = self._ssh(f'cd {app} 2>/dev/null && find . -type f ! -name .source-hash -print0 | xargs -0 -r sha1sum',
                      capture_output=True, text=True, timeout=300)
        remote = {}
        for line in (r.stdout if r.returncode == 0 else '').splitlines():
            h, _, rel = line.partition('  ')
            remote[rel[2:] if rel.startswith('./') else rel] = h
        if not remote:
            self._upload_tree(out, new)
            return f'all {len(local)} files (no previous build)'
        changed = sorted(rel for rel, h in local.items() if remote.get(rel) != h)
        removed = sorted(rel for rel in remote if rel not in local)
        # Hard-link copy, then drop files the new build no longer has (names over stdin, NUL separated).
        r = self._ssh(f'rm -rf {new} && cp -al {app} {new} && rm -f {new}/.source-hash && cd {new} && xargs -0 -r rm -f --',
                      input=b'\0'.join(x.encode() for x in removed), capture_output=True, timeout=300)
        if r.returncode != 0:
            raise RuntimeError(f'staging {new} failed: {r.stderr.decode("utf-8", "replace")[-500:]}')
        size = sum(os.path.getsize(os.path.join(out, rel)) for rel in changed)
        if changed:
            p = subprocess.Popen(['ssh', '-o', 'BatchMode=yes', self.host, f'tar -C {new} --unlink-first -xzf -'], stdin=subprocess.PIPE)
            with tarfile.open(fileobj=p.stdin, mode='w|gz') as tar:
                for rel in changed:
                    tar.add(os.path.join(out, rel), arcname=rel)
            p.stdin.close()
            if p.wait() != 0:
                raise RuntimeError(f'upload of changed files to {self.host}:{new} failed')
        return f'{len(changed)} changed file(s), {size / 1e6:.1f} MB; {len(removed)} removed; {len(local) - len(changed)} reused'

    def _upload_tree(self, local_dir, remote_dir):
        p = subprocess.Popen(['ssh', '-o', 'BatchMode=yes', self.host, f'rm -rf {remote_dir} && mkdir -p {remote_dir} && tar -C {remote_dir} -xzf -'],
                             stdin=subprocess.PIPE)
        with tarfile.open(fileobj=p.stdin, mode='w|gz') as tar:
            for name in sorted(os.listdir(local_dir)):
                if name in UPLOAD_SKIP or name.startswith('character_'):
                    continue
                tar.add(os.path.join(local_dir, name), arcname=name)
        p.stdin.close()
        if p.wait() != 0:
            raise RuntimeError(f'upload to {self.host}:{remote_dir} failed')

    def _warn_stale_local_dll(self):
        """Spark shards run the current source; a local DLL older than the source would make the two disagree."""
        if not self.spark_slots or not self.local_slots or not os.path.exists(self.dll):
            return
        built = os.path.getmtime(self.dll)
        code = os.path.join(self.root, 'Code')
        for d, dirs, files in os.walk(code):
            dirs[:] = [x for x in dirs if not (d == code and x in ('bin', 'obj', 'Tests'))]
            if any(f.endswith('.cs') and os.path.getmtime(os.path.join(d, f)) > built for f in files):
                self.log(f'WARNING: {self.dll} is older than Code/ source; local shards run older code than Spark shards. Rebuild it.')
                return

    # ------------------------------------------------------------------ shards

    def _remote_path(self, path):
        rel = os.path.relpath(os.path.abspath(path), self.work)
        return f'{self.remote}/{rel.replace(os.sep, "/")}'

    def _in_work(self, arg):
        try:
            return os.path.commonpath([os.path.abspath(arg), self.work]) == self.work
        except ValueError:
            return False

    def _upload_file(self, path):
        with self._lock:
            if path in self._uploaded:
                return
            remote = self._remote_path(path)
            with open(path, 'rb') as fh:
                r = self._ssh(f'mkdir -p {shlex.quote(os.path.dirname(remote))} && cat > {shlex.quote(remote)}', stdin=fh, timeout=60)
                if r.returncode != 0:
                    raise RuntimeError(f'upload of {path} to {self.host} failed (exit {r.returncode})')
            self._uploaded.add(path)

    def run(self, args, out_path, log_path, timeout=1800):
        """Runs one shard (DF arguments after the executable) on a free slot; returns the host name it ran on.
        The caller reads out_path afterwards, as it would for a local run (missing on failure)."""
        slot = self._slots.get()
        try:
            if slot == 'local':
                with open(log_path, 'w', encoding='utf-8') as log:
                    try:
                        subprocess.run(['dotnet', self.dll] + args, cwd=self.root, stdout=log, stderr=subprocess.STDOUT,
                                       env=dict(os.environ, DF_GAMEDATA_DIR=self.data_dir), timeout=timeout)
                    except subprocess.TimeoutExpired:
                        log.write(f'\nKilled after {timeout} s\n')
            else:
                self._run_remote(args, out_path, log_path, timeout)
            with self._lock:
                self.counts[slot] += 1
            return slot
        finally:
            self._slots.put(slot)

    def _run_remote(self, args, out_path, log_path, timeout):
        remote_args = []
        for a in args:
            if self._in_work(a):
                if os.path.isfile(a):
                    self._upload_file(a)
                a = self._remote_path(a)
            remote_args.append(a)
        rlog = self._remote_path(log_path)
        rout = self._remote_path(out_path)
        cmd = (f'cd {self.base} && mkdir -p {shlex.quote(os.path.dirname(rout))} && '
               f'DF_GAMEDATA_DIR={self.remote}/GameData timeout -k 10 {timeout} ./app/DF '
               + ' '.join(shlex.quote(a) for a in remote_args)
               + f' > {shlex.quote(rlog)} 2>&1; rc=$?; [ -f {shlex.quote(rout)} ] && cat {shlex.quote(rout)}; '
               f'[ $rc -ne 0 ] && tail -40 {shlex.quote(rlog)} >&2; rm -f {shlex.quote(rout)}; exit $rc')
        with open(log_path, 'w', encoding='utf-8') as log:
            log.write(f'[{self.host}] {" ".join(remote_args)}\n')
            log.flush()
            try:
                r = self._ssh(cmd, capture_output=True, timeout=timeout + 60)
            except subprocess.TimeoutExpired:
                log.write(f'\nssh killed after {timeout + 60} s\n')
                return
            log.write(r.stderr.decode('utf-8', 'replace'))
        if r.stdout:
            with open(out_path, 'wb') as fh:
                fh.write(r.stdout)

    def close(self):
        if self.spark_slots:
            self._ssh(f'rm -rf {self.remote}', capture_output=True, timeout=60)
