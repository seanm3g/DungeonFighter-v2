#!/usr/bin/env python3
"""Balance patch helpers shared by the sim scripts (same rules as Code/Config/BalancePatchOverlay.cs and
EnemyDamageCurveConfig.MultiplierAt).

- Overlays: a patch may hold "base": "<patch name>" plus only what it changes. resolve() merges the chain (objects merge
  key by key, case-insensitive; arrays and scalars replace). write_patch() stores an overlay as its differences.
- Curves (enemySystem.damageCurve / healthCurve / packSizeCurve, classBalance.<class>.damageCurve): "bands"
  [{from, to, multiplier}] (step values; one per 5-level region band) take precedence over the older "anchors".
- Region bands are levels 1-5, 6-10, ...: region_band(L) gives (lo, hi).

  python Scripts/balancepatch.py resolve fastbot-v20                 (print the merged patch)
  python Scripts/balancepatch.py curve fastbot-v20 damage            (level, multiplier per band)
  python Scripts/balancepatch.py migrate fastbot-v18 fastbot-v20     (old layout -> current; behaviour identical)
"""
import json, math, os, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PATCH_DIR = os.path.join(ROOT, 'GameData', 'Patches', 'Balance')
CURVES = {'damage': ('enemySystem', 'damageCurve'), 'health': ('enemySystem', 'healthCurve'), 'pack': ('enemySystem', 'packSizeCurve')}
CLASSES = ['barbarian', 'warrior', 'rogue', 'wizard']


# ---------------------------------------------------------------- files and overlays

def patch_path(name_or_path, patch_dir=None):
    if os.path.isfile(name_or_path):
        return os.path.abspath(name_or_path)
    d = patch_dir or PATCH_DIR
    return os.path.join(d, name_or_path if name_or_path.endswith('.json') else name_or_path + '.json')


def read_raw(path):
    raw = open(path, 'rb').read()
    return json.loads(raw.decode('utf-8-sig'))


def _key(obj, key):
    return next((k for k in obj if k.lower() == key.lower()), None)


def merge(target, src):
    for k, v in src.items():
        tk = _key(target, k) or k
        if isinstance(v, dict) and isinstance(target.get(tk), dict):
            merge(target[tk], v)
        else:
            target[tk] = v
    return target


def resolve(name_or_path, patch_dir=None, _depth=0):
    """The patch with every base merged in (no "base" key)."""
    if _depth > 16:
        raise SystemExit(f'balance patch base chain too deep at {name_or_path}')
    path = patch_path(name_or_path, patch_dir)
    d = read_raw(path)
    bk = _key(d, 'base')
    if bk is None or not d[bk]:
        return d
    base = resolve(patch_path(d.pop(bk), os.path.dirname(path)), os.path.dirname(path), _depth + 1)
    return merge(base, d)


def chain_depth(name_or_path, patch_dir=None):
    """How many bases sit under this patch (0 for a whole patch)."""
    n, path = 0, patch_path(name_or_path, patch_dir)
    while True:
        d = read_raw(path)
        bk = _key(d, 'base')
        if bk is None or not d[bk] or n > 64:
            return n
        n, path = n + 1, patch_path(d[bk], os.path.dirname(path))


def base_name(name_or_path, patch_dir=None):
    d = read_raw(patch_path(name_or_path, patch_dir))
    bk = _key(d, 'base')
    return d[bk] if bk else None


def diff(full, base):
    if isinstance(full, dict) and isinstance(base, dict):
        out = {}
        for k, v in full.items():
            bk = _key(base, k)
            d = diff(v, base[bk]) if bk is not None else v
            if bk is None or d is not None:
                out[k] = d if bk is not None else v
        return out or None
    if isinstance(full, (int, float)) and isinstance(base, (int, float)) and not isinstance(full, bool) and float(full) == float(base):
        return None
    return None if full == base else full


def dumps(d):
    return json.dumps(d, indent=2, ensure_ascii=False).replace('\n', '\r\n')


def write_patch(path, full, base=None, patch_dir=None):
    """Write a patch: whole, or as an overlay of `base` (only its differences). An overlay whose base chain is already
    at the 16-base cap (BalancePatchOverlay.MaxDepth) is written whole instead, so the game can still load it."""
    if base and chain_depth(base, patch_dir or os.path.dirname(os.path.abspath(path))) >= 16:
        print(f'note: {base} is {16} bases deep; writing {os.path.basename(path)} flattened', flush=True)
        base = None
    if base:
        d = diff(full, resolve(base, patch_dir or os.path.dirname(os.path.abspath(path)))) or {}
        full = {'base': base, **d}
    open(path, 'w', encoding='utf-8', newline='').write(dumps(full))


def variant(base, tag, edit, patch_dir=None):
    """Write `<base>-<tag>.json` next to base: an overlay holding whatever edit(full_dict) changes. Returns its name."""
    full = resolve(base, patch_dir)
    edit(full)
    name = f'{os.path.splitext(os.path.basename(patch_path(base, patch_dir)))[0]}-{tag}'
    write_patch(patch_path(name, patch_dir), full, base=base, patch_dir=patch_dir)
    return name


# ---------------------------------------------------------------- curves

def region_band(level):
    lo = 5 * ((level - 1) // 5) + 1
    return lo, lo + 4


def get_curve(d, which, cls=None):
    if cls:
        cb = d[_key(d, 'classBalance')]
        return cb[_key(cb, cls)].setdefault('damageCurve', {})
    sec, name = CURVES[which]
    s = d[_key(d, sec)]
    return s.setdefault(_key(s, name) or name, {})


def _anchor_value(anchors, level):
    pts = sorted((a['level'], a['multiplier']) for a in anchors if a.get('multiplier', 0) > 0)
    if not pts:
        return 1.0
    if level <= pts[0][0]:
        return pts[0][1]
    if level >= pts[-1][0]:
        return pts[-1][1]
    for (l0, m0), (l1, m1) in zip(pts, pts[1:]):
        if level <= l1:
            t = (level - l0) / max(1, l1 - l0)
            return math.exp(math.log(m0) + t * (math.log(m1) - math.log(m0)))
    return pts[-1][1]


def _band_value(bands, level):
    best, nearest = None, None
    for b in bands:
        if b['from'] <= level <= b['to']:
            return b['multiplier']
        dist = b['from'] - level if level < b['from'] else level - b['to']
        if best is None or dist < best:
            best, nearest = dist, b
    return nearest['multiplier'] if nearest else 1.0


def value_at(curve, level):
    """Curve multiplier at a level (bands, else anchors, else growth), x baseMultiplier; 1.0 when disabled."""
    if not curve or not curve.get('enabled', False):
        return 1.0
    base = max(0.0, curve.get('baseMultiplier', 1.0))
    if curve.get('bands'):
        return base * _band_value(curve['bands'], level)
    if curve.get('anchors'):
        return base * _anchor_value(curve['anchors'], level)
    exp = curve.get('exponent', 1.0) or 1.0
    return base * (1.0 + curve.get('growthPerLevel', 0.0) * math.pow(max(0, level - 1), exp))


def band_value(curve, level):
    """The stored step value (without baseMultiplier) of the band holding `level`."""
    if curve.get('bands'):
        return _band_value(curve['bands'], level)
    return _anchor_value(curve.get('anchors') or [], level) if curve.get('anchors') else 1.0


def to_bands(curve, max_level=100):
    """Exact step form of an anchor curve: consecutive levels with the same value share a band."""
    if not curve or curve.get('bands') or not curve.get('anchors'):
        return curve
    vals = [_anchor_value(curve['anchors'], L) for L in range(1, max_level + 1)]
    bands = []
    for L, v in enumerate(vals, start=1):
        if bands and abs(bands[-1]['multiplier'] - v) <= 1e-12 * max(1.0, abs(v)):
            bands[-1]['to'] = L
        else:
            bands.append({'from': L, 'to': L, 'multiplier': v})
    curve['bands'] = bands
    curve.pop('anchors', None)
    return curve


def set_band(curve, level, value):
    """Set the step value of the region band holding `level` (splitting existing bands if needed)."""
    lo, hi = region_band(level)
    return set_range(curve, lo, hi, value)


def set_range(curve, lo, hi, value):
    """Set levels lo..hi to one step value (splitting existing bands if needed)."""
    curve.setdefault('enabled', True)
    to_bands(curve)
    bands = curve.setdefault('bands', [])
    if not bands:
        bands.append({'from': 1, 'to': 100, 'multiplier': 1.0})
    out = []
    for b in bands:
        if b['to'] < lo or b['from'] > hi:
            out.append(b); continue
        if b['from'] < lo: out.append({'from': b['from'], 'to': lo - 1, 'multiplier': b['multiplier']})
        if b['to'] > hi: out.append({'from': hi + 1, 'to': b['to'], 'multiplier': b['multiplier']})
    out.append({'from': lo, 'to': hi, 'multiplier': round(value, 6)})
    curve['bands'] = sorted(out, key=lambda b: b['from'])
    return curve


def scale_band(curve, level, factor):
    return set_band(curve, level, band_value(curve, level) * factor)


# ---------------------------------------------------------------- migration (2026-11-05 layout)

DEAD_SECTIONS = ['enemyScaling', 'enemyBalance', 'enemyBaseline', 'enemyArchetypes', 'enemyDPS']


def migrate(d):
    """Old layout -> current: drop dead enemy sections, globalMultipliers -> attributeScale / armorScale (health folded into
    the health curve base), every curve's anchors -> exact bands. Game behaviour is unchanged."""
    for s in DEAD_SECTIONS:
        k = _key(d, s)
        if k: d.pop(k)
    es = d[_key(d, 'enemySystem')]
    gk = _key(es, 'globalMultipliers')
    if gk:
        g = es.pop(gk)
        es['attributeScale'] = g.get('damageMultiplier', 1.0) or 1.0
        es['armorScale'] = g.get('armorMultiplier', 1.0) or 1.0
        h = g.get('healthMultiplier', 1.0) or 1.0
        if abs(h - 1.0) > 1e-12:
            hc = get_curve(d, 'health')
            if not hc.get('enabled'):
                hc.update({'enabled': True, 'growthPerLevel': 0, 'baseMultiplier': 1.0, 'anchors': [], 'bands': []})
            hc['baseMultiplier'] = hc.get('baseMultiplier', 1.0) * h
    for which in CURVES:
        to_bands(get_curve(d, which))
    cb = d.get(_key(d, 'classBalance') or 'classBalance', {})
    for c in CLASSES:
        ck = _key(cb, c)
        if ck and isinstance(cb[ck], dict) and cb[ck].get('damageCurve'):
            to_bands(cb[ck]['damageCurve'])
    return d


if __name__ == '__main__':
    cmd = sys.argv[1] if len(sys.argv) > 1 else ''
    if cmd == 'resolve':
        print(json.dumps(resolve(sys.argv[2]), indent=2))
    elif cmd == 'curve':
        d = resolve(sys.argv[2]); c = get_curve(d, sys.argv[3], sys.argv[4] if len(sys.argv) > 4 else None)
        for L in range(3, 101, 5):
            print(f'L{region_band(L)[0]:>3}-{region_band(L)[1]:<3} {value_at(c, L):.4f}')
    elif cmd == 'migrate':
        src, dst = sys.argv[2], sys.argv[3]
        d = migrate(resolve(src))
        note = f' {dst} (2026-11-05): {src} migrated to the current layout (bands, attributeScale/armorScale, no legacy enemy sections); same game behaviour.'
        d['description'] = (d.get('description') or '').rstrip() + note
        write_patch(patch_path(dst), d)
        print('wrote', patch_path(dst))
    else:
        print(__doc__)
