#!/usr/bin/env python3
"""Re-solve enemySystem.healthCurve and damageCurve anchors after hero-side changes.

Health pass: FUNDMATRIX (typical gear) per anchor level x weapon against near-harmless enemies (damage x0.05), so hero
turns measure kill speed; each health anchor is scaled by (HP needed for 11 turns / current)^0.9. Measuring against
normal enemies breaks at lethal levels (L75 / L100 targets): short losses read as "kills too fast" and the anchor runs
away. Damage pass: WRSOLVE per level against the balance-check targets. Repeat 2 passes, then run balance-check.ps1.

  python Scripts/curve-solve.py --patch overnight-v2 --passes 2 --tools Code/bin/BalanceTools
"""
import json,subprocess,re,sys,os,argparse
from concurrent.futures import ThreadPoolExecutor
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ap=argparse.ArgumentParser(); ap.add_argument('--patch',default='overnight-v2'); ap.add_argument('--passes',type=int,default=2)
ap.add_argument('--tools',default='Code/bin/BalanceTools')
ARGS=ap.parse_args()
DLL=os.path.join(ROOT,ARGS.tools,'DF.dll')
PATCH=os.path.join(ROOT,'GameData','Patches','Balance',ARGS.patch+'.json')
E="Goblin,Rats,Wraith,Wolf,Bear,Lich,Fire Elemental,Zombie,Wight,Glass Shard Golem,Guardian Scarab,Vigil Alpha"
WR={10:99.1,20:97.1,35:93.6,50:89.4,75:79.2,100:1.5}
HL=[1,5,10,20,35,50,75,100]
def load(): return json.load(open(PATCH,encoding='utf-8'))
def save(d): json.dump(d,open(PATCH,'w',encoding='utf-8'),indent=2,ensure_ascii=False)
def run(args):
  r=subprocess.run(['dotnet',DLL]+args,cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='ignore'); return r.stdout
def fm(level,weapon):
  # Near-harmless enemies (damage x0.05) so hero turns measure kill speed, not how fast the hero dies: at lethal levels
  # (L75 / L100 targets) short losses otherwise read as "kills too fast" and the HP anchor runs away.
  out=run(['FUNDMATRIX','--patch',ARGS.patch,'--gear','typical','--level',str(level),'--weapons',weapon,'--encounters','40','--seed','1','--enemies',E,'--label',f'curve-L{level}-{weapon}','--set','enemySystem.damageCurve.baseMultiplier=0.05'])
  m=re.search(r'\(x([0-9.]+)\)',out); t=re.search(r'hero turns[^0-9]*([0-9.]+)',out,re.I)
  return level,weapon,float(m.group(1)) if m else None
def health_pass():
  with ThreadPoolExecutor(24) as ex: res=list(ex.map(lambda a: fm(*a),[(l,w) for l in HL for w in ['Mace','Sword','Dagger','Wand']]))
  d=load(); anchors={a['level']:a for a in d['enemySystem']['healthCurve']['anchors']}
  import statistics
  for l in HL:
    ks=[k for (lv,w,k) in res if lv==l and k]
    if not ks: continue
    k=statistics.mean(ks); old=anchors[l]['multiplier']; new=round(old*k**0.9,3)
    anchors[l]['multiplier']=new; print(f'health L{l}: x{k:.2f} {old} -> {new}',flush=True)
  save(d)
def wr_pass():
  def one(l):
    out=run(['WRSOLVE','--patch',ARGS.patch,'--level',str(l),'--target',str(WR[l]),'--encounters','30','--steps','8','--enemies',E])
    m=re.search(r'RESULT level \d+ multiplier ([0-9.]+) WR ([0-9.]+)% turns ([0-9.]+)',out)
    return l,float(m.group(1)),m.group(2),m.group(3)
  with ThreadPoolExecutor(6) as ex: res=list(ex.map(one,WR))
  d=load(); anchors={a['level']:a for a in d['enemySystem']['damageCurve']['anchors']}
  for l,m,wr,t in res:
    print(f'damage L{l}: {anchors[l]["multiplier"]} -> {m:.3f}  (WR {wr}% turns {t})',flush=True); anchors[l]['multiplier']=round(m,3)
  save(d)
for it in range(ARGS.passes):
  print('== pass',it,flush=True); health_pass(); wr_pass()
