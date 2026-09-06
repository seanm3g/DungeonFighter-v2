"""Check generated atlas content, trim safety and decoded memory without launching the game."""
import bpy,json,hashlib,struct
from array import array
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'GameData/Visuals/DemonFighter/Animations'
manifest=json.loads((OUT/'clips.json').read_text())
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Art/DemonFighter/Blender/animated-kit.blend'))
report={}; total=0
for actor,clips in manifest['actors'].items():
 size=0
 glb=(OUT.parent/(actor+'.glb')).read_bytes()
 assert glb[:4]==b'glTF' and struct.unpack_from('<I',glb,8)[0]==len(glb),(actor,'GLB header')
 chunk=struct.unpack_from('<I',glb,12)[0]
 model=json.loads(glb[20:20+chunk]); assert model.get('meshes'),(actor,'empty GLB')
 assert any(o.name.lower()==actor+' rig' for o in bpy.data.objects),(actor,'missing editable rig')
 assert set(clips)=={'idle','attack','hit','cast','guard','evade','death','victory'}
 for name,c in clips.items():
  assert any(a.name.lower()==actor+'/'+name for a in bpy.data.actions),(actor,name,'missing Blender action')
  im=bpy.data.images.load(str(OUT/c['file']),check_existing=False)
  w,h,n=c['frameWidth'],c['frameHeight'],c['frames']; width=w*n
  assert tuple(im.size)==(width,h),(actor,name,'dimensions')
  pixels=array('f',[0])*len(im.pixels); im.pixels.foreach_get(pixels)
  hashes=set()
  for frame in range(n):
   buf=array('f')
   for row in range(h):
    start=(row*width+frame*w)*4; buf.extend(pixels[start:start+w*4])
    assert pixels[start+3]<.01 and pixels[start+(w-1)*4+3]<.01,(actor,name,'side clipping')
   assert max(buf[3::4])>.1,(actor,name,'empty frame')
   hashes.add(hashlib.sha256(buf.tobytes()).hexdigest())
  assert all(pixels[x*4+3]<.01 and pixels[((h-1)*width+x)*4+3]<.01 for x in range(width)),(actor,name,'vertical clipping')
  assert len(hashes)>2,(actor,name,'animation frozen')
  size+=width*h*4; total+=1; bpy.data.images.remove(im)
 report[actor]={'decodedMiB':round(size/1048576,2),'clips':len(clips)}
report['summary']={'actors':len(manifest['actors']),'atlases':total,
 'maxActorMiB':max(v['decodedMiB'] for v in report.values()),
 'checks':'dimensions, nonempty frames, distinct poses, transparent trim borders'}
(ROOT/'Art/DemonFighter/Verification/atlas-validation.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report['summary']),flush=True)
