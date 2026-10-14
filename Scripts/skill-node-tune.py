#!/usr/bin/env python3
"""Per-node skill power solver (BUILDLAB add-one). Each skill node listed by a BUILD_CATALOG ladder
(GameData/TuningProfiles/build-ladders.json) is priced alone on top of its build's set + mods step: offense from long fights
(--long 4), defense from normal fights. Targets: 5-rank nodes ~0.8% per point, 1-rank rules x1.04, tier-7 masteries
x1.06, capstones x1.08. Asymmetric: only clear outliers (> target x1.15) are cut (at most halved per pass), weak but
live nodes are raised (at most x3), nodes with no response are reported, not changed (cutting moderate nodes lets
integer effects round to 0, after which they never respond). Writes SkillTrees.json and skill-tree-templates.json.

  TOOLS=Code/bin/BalanceTools python Scripts/skill-node-tune.py pass1
"""
import json,subprocess,os,sys,glob,collections,math
from concurrent.futures import ThreadPoolExecutor
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SP=os.path.join(ROOT,'GameData','TuningRuns','skill-node-tune')
DLL=os.path.join(ROOT,os.environ.get('TOOLS','Code/bin/BalanceTools'),'DF.dll')
TREES=ROOT+'/GameData/SkillTrees.json'; TEMPL=ROOT+'/GameData/TuningProfiles/skill-tree-templates.json'
BUILDS=os.environ.get("BUILDS","B1 B2 B3 B4 B5 B6 W1 W2 W3 W4 W5 R1 R2 R3 R4 R5 R6 Z1 Z2 Z3 Z4 Z5 Z6").replace(",", " ").split()
LEVELS=[int(x) for x in os.environ.get('LEVELS','30,60').split(',')]
PER_POINT=float(os.environ.get('PER_POINT','0.008'))
def run(b,l,mode,tag):
  out=f'{SP}/runs/st/{tag}-{mode}-{b}-{l}.json'
  args=['dotnet',DLL,'BUILDLAB','--builds',b,'--level',str(l),'--encounters','24','--patch',os.environ.get('PATCH','overnight-v2'),'--aoo-base','4','--aoo-upto','6','--max-step','4','--json',out]
  if mode=='off': args+=['--long','4']
  subprocess.run(args,cwd=ROOT,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
  try: return json.load(open(out))
  except Exception: return []
def load_trees():
  raw=open(TREES,'rb').read(); return json.loads(raw.decode('utf-8-sig')),raw
def save_json(path,data,raw,ascii_):
  out=json.dumps(data,indent=2,ensure_ascii=ascii_)
  if b'\r\n' in raw: out=out.replace('\n','\r\n')
  bom=raw[:3]==b'\xef\xbb\xbf'
  tail='\r\n' if raw.endswith(b'\r\n') else ('\n' if raw.endswith(b'\n') else '')
  open(path,'w',encoding='utf-8-sig' if bom else 'utf-8',newline='').write(out+tail)
def main(tag):
  os.makedirs(SP+'/runs/st',exist_ok=True)
  jobs=[(b,l,m) for b in BUILDS for l in LEVELS for m in ('off','def')]
  with ThreadPoolExecutor(int(os.environ.get('WORKERS','23'))) as ex: res=list(ex.map(lambda j: (j,run(j[0],j[1],j[2],tag)),jobs))
  off=collections.defaultdict(list); dfn=collections.defaultdict(list)
  for (b,l,m),rows in res:
    for r in rows:
      if r['Step']!=-2: continue
      n=r['Label'][4:]
      (off if m=='off' else dfn)[(n,b,l)].append(r['StepGain'] if m=='off' else r['DefenseGain'])
  data,raw=load_trees(); nodes={n['id']:(t['classKey'],n) for t in data['trees'] for n in t['nodes']}
  tl_raw=open(TEMPL,'rb').read(); tl=json.loads(tl_raw.decode('utf-8-sig'))
  vals=collections.defaultdict(list)
  for (n,b,l),o in off.items():
    d=dfn.get((n,b,l),[1.0])
    vals[n].append(o[0]*d[0])
  report=[]
  for n,vs in sorted(vals.items()):
    cls,node=nodes[n]
    v=max(vs)  # best home build
    pts=max(1,node.get('cost',1))*max(1,node.get('maxRank',1))
    tier=node.get('tier',0)
    rank=max(1,node.get('maxRank',1))
    if tier==9: target=1.08
    elif tier==7: target=1.06
    elif rank==1: target=1.04
    else: target=(1+PER_POINT)**pts
    p=node.get('power',1.0)
    if 'ACTION:' in (node.get('effect') or '') or tier==0:
      report.append((n,v,target,p,p,'unlock/root')); continue
    if v<=1.006:
      report.append((n,v,target,p,p,'no response')); continue
    # Asymmetric: cut only clear outliers (at most halve per pass), lift weak-but-live nodes, leave the rest.
    # Cutting moderate nodes risks integer effects (round(rank x power)) rounding to 0, after which they never respond.
    if v > target*1.15:
      ratio=max(0.5,((target-1)/(v-1))**0.8)
    elif v < target:
      ratio=min(3.0,((target-1)/max(0.003,v-1))**0.8)
    else:
      report.append((n,v,target,p,p,'in band')); continue
    newp=round(p*ratio,5)
    node['power']=newp
    t=tl['nodes'].get(n)
    if t:
      scale=tl['classScale'].get(cls,1.0) if t[2] else 1.0
      t[0]=round(newp/scale,5) if scale else newp
    report.append((n,v,target,p,newp,'tuned'))
  save_json(TREES,data,raw,True); save_json(TEMPL,tl,tl_raw,False)
  for n,v,t,p,np_,s in report: print(f'{n:18} value {v:5.3f} target {t:5.3f} power {p:.4f} -> {np_:.4f}  {s}')
  json.dump(report,open(f'{SP}/runs/st/{tag}-report.json','w'))
if __name__=='__main__': main(sys.argv[1])
