#!/usr/bin/env python3
"""Solve combat.materialConvertPercentByKeyword (percent converts) with BUILDLAB: for each mono material build, the
gain from the class strip (step 1) to the full set (step 3) at L30/60/90 (long fights) is pushed toward --target
(default 1.30) by scaling that currency's % per unit. Currencies that don't respond in single fights (TROPHY mints on
kill, LENS needs Focus procs) run away, so cap them by hand afterwards.

  python Scripts/convert-tune.py 3 1.30   (edits the patch named in PATCH below)
"""
import json,subprocess,sys,os,glob,statistics
from concurrent.futures import ThreadPoolExecutor
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SP=os.path.join(ROOT,'GameData','TuningRuns','convert-tune')
DLL=os.path.join(ROOT,os.environ.get('TOOLS','Code/bin/BalanceTools'),'DF.dll')
PATCH=os.path.join(ROOT,'GameData','Patches','Balance',os.environ.get('PATCH','overnight-v2')+'.json')
BUILDS={'B1':'RAGE','B3':'CRISIS','B5':'TRIUMPH','W1':'BEAT','W3':'DRAG','W5':'STREAK','R1':'GRAZE','R3':'TROPHY','R5':'SWING','Z1':'CHAIN','Z3':'LENS','Z5':'CONTACT'}
LEVELS=[30,60,90]
TARGET=float(sys.argv[2]) if len(sys.argv)>2 else 1.30
def run(b,l,tag):
  out=f'{SP}/runs/conv/{tag}-{b}-{l}.json'
  subprocess.run(['dotnet',DLL,'BUILDLAB','--builds',b,'--level',str(l),'--encounters','25','--patch',PATCH,'--long','4','--max-step','3','--json',out],cwd=ROOT,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
  rows=json.load(open(out)); r={x['Step']:x for x in rows}
  return b,l,r[1]['KillTime']/r[3]['KillTime']
def main():
  iters=int(sys.argv[1]) if len(sys.argv)>1 else 3
  os.makedirs(SP+'/runs/conv',exist_ok=True)
  for it in range(iters):
    p=json.load(open(PATCH,encoding='utf-8')); tab=p['combat']['materialConvertPercentByKeyword']
    with ThreadPoolExecutor(18) as ex: res=list(ex.map(lambda a: run(a[0],a[1],f'it{it}'),[(b,l) for b in BUILDS for l in LEVELS]))
    g={}
    for b,l,v in res: g.setdefault(b,[]).append(v)
    print(f'--- iter {it}')
    for b,kw in BUILDS.items():
      m=statistics.mean(g[b]); old=tab[kw]
      ratio=(TARGET-1)/max(0.02,m-1)
      ratio=min(3.0,max(0.33,ratio))**0.7
      tab[kw]=round(old*ratio,3)
      print(f'{b} {kw:8} gains {[round(x,2) for x in g[b]]} mean {m:.2f}  pct {old} -> {tab[kw]}')
    json.dump(p,open(PATCH,'w',encoding='utf-8'),indent=2,ensure_ascii=False)
main()
