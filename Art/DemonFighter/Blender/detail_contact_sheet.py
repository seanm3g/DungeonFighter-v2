"""Arrange rendered actor cutouts at inspection size; no changes to runtime images."""
import bpy,json,math
from array import array
from pathlib import Path
HERE=Path(__file__).resolve().parent
OUT=HERE.parents[2]/'GameData/Visuals/DemonFighter'
VERIFY=HERE.parent/'Verification'
names=sorted(json.loads((VERIFY/'model-detail-audit.json').read_text()))
cell=240; cols=8; rows=math.ceil(len(names)/cols); width=cols*cell; height=rows*cell
sheet=array('f',[.035,.04,.045,1])*(width*height)
for index,name in enumerate(names):
    im=bpy.data.images.load(str(OUT/(name.lower()+'-layer.png')),check_existing=False)
    src=array('f',[0])*len(im.pixels); im.pixels.foreach_get(src)
    sw,sh=im.size; xs=[]; ys=[]
    for y in range(sh):
        for x in range(sw):
            if src[(y*sw+x)*4+3]>.02: xs.append(x); ys.append(y)
    x0,x1,y0,y1=min(xs),max(xs)+1,min(ys),max(ys)+1
    scale=min((cell-28)/(x1-x0),(cell-28)/(y1-y0)); w=int((x1-x0)*scale); h=int((y1-y0)*scale)
    dx=(index%cols)*cell+(cell-w)//2; dy=height-(index//cols+1)*cell+(cell-h)//2
    for y in range(h):
        for x in range(w):
            a=((y0+int(y/scale))*sw+x0+int(x/scale))*4; b=((dy+y)*width+dx+x)*4
            alpha=src[a+3]
            for ch in range(3): sheet[b+ch]=src[a+ch]*alpha+sheet[b+ch]*(1-alpha)
    bpy.data.images.remove(im)
im=bpy.data.images.new('Detailed roster contact sheet',width=width,height=height,alpha=True)
im.pixels.foreach_set(sheet); im.filepath_raw=str(VERIFY/'detailed-roster.png'); im.file_format='PNG'; im.save()
(VERIFY/'detailed-roster-index.json').write_text(json.dumps({'columns':cols,'orderLeftToRightTopToBottom':names},indent=2)+'\n')
print('CONTACT SHEET COMPLETE',flush=True)
