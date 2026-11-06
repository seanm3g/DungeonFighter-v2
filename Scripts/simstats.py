"""Statistics shared by the balance scripts, so every tool reports intervals the same way.

Sim results are clustered: a shard's (or hero's) dungeons share builds and seeds, so they are not independent. Rates get
a t interval over cluster rates, never narrower than the Wilson interval on the pooled count. Comparisons between two
patches are paired: both run on the same seeds, and the per-seed differences carry the noise.
"""
import math, statistics

T95 = {1: 12.71, 2: 4.30, 3: 3.18, 4: 2.78, 5: 2.57, 6: 2.45, 7: 2.36, 8: 2.31, 9: 2.26, 10: 2.23, 15: 2.13, 20: 2.09, 30: 2.04}


def t95(df):
    """Two-sided 95% t quantile (table, rounded up to the next listed df; 1.96 beyond 30)."""
    if df <= 0:
        return float('inf')
    return T95.get(df) or next((v for d, v in sorted(T95.items()) if d >= df), 1.96)


def wilson(k, n, z=1.96):
    """95% Wilson interval for k successes in n (as fractions)."""
    if n == 0:
        return 0.0, 1.0
    p = k / n
    den = 1 + z * z / n
    c = (p + z * z / (2 * n)) / den
    h = z * math.sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / den
    return max(0.0, c - h), min(1.0, c + h)


def cluster_interval(units, z=1.96):
    """95% interval for a rate from (hits, n) per cluster (shard or hero): t interval over cluster rates, widened to at
    least the Wilson interval on the pooled count."""
    k = sum(c for c, _ in units)
    n = sum(d for _, d in units)
    lo, hi = wilson(k, n, z)
    rates = [c / d for c, d in units if d]
    if len(rates) >= 2 and n:
        mean = k / n
        sd = math.sqrt(sum((r - mean) ** 2 for r in rates) / (len(rates) - 1))
        half = t95(len(rates) - 1) * sd / math.sqrt(len(rates))
        lo, hi = min(lo, max(0.0, mean - half)), max(hi, min(1.0, mean + half))
    return lo, hi


def mean_se(xs):
    """Mean and standard error (5.0 when there is a single value: unknown, treat as wide)."""
    xs = list(xs)
    if not xs:
        return float('nan'), float('inf')
    if len(xs) == 1:
        return xs[0], 5.0
    return statistics.mean(xs), statistics.stdev(xs) / math.sqrt(len(xs))


def paired_gap_change(base, cand, target):
    """Per-seed change in |value - target| (candidate minus base) for paired runs: (mean, SE). Negative = closer."""
    return mean_se(abs(b - target) - abs(a - target) for a, b in zip(base, cand))


def beats_noise(change, se, min_gain=1.0, k=2.0):
    """True when a paired gap change is an improvement larger than both min_gain and k standard errors."""
    return -change > max(min_gain, k * se)
