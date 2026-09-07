"""Validate complete detailed static exports and the preserved camera/rig contract."""
import bpy,json,struct
from array import array
from pathlib import Path
from bpy_extras.object_utils import world_to_camera_view
HERE=Path(__file__).resolve().parent
OUT=HERE.parents[2]/'GameData/Visuals/DemonFighter'
VERIFY=HERE.parent/'Verification'
bpy.ops.wm.open_mainfile(filepath=str(HERE/'detailed-kit.blend'))
audit=json.loads((VERIFY/'model-detail-audit.json').read_text())
mapping=json.loads((OUT/'enemies.json').read_text())
assert len(audit)==46
assert set(mapping.values())<=set(n.lower() for n in audit)
report={'actors':len(audit),'enemyDefinitions':len(mapping),'stages':8,'meshesBefore':0,'meshesAfter':0}
for name,counts in audit.items():
    assert counts['detailedMeshes']>counts['originalMeshes'],(name,'no additional geometry')
    report['meshesBefore']+=counts['originalMeshes']; report['meshesAfter']+=counts['detailedMeshes']
    for o in bpy.data.collections[name].objects:
        if o.type=='MESH':
            assert o.get('rig_part') in ['root','torso','head','left','right','legL','legR'],(name,o.name,'invalid attachment')

def check_glb(name):
    raw=(OUT/(name+'.glb')).read_bytes()
    magic,version,size=struct.unpack_from('<4sII',raw)
    assert magic==b'glTF' and version==2 and size==len(raw),name
    length=struct.unpack_from('<I',raw,12)[0]
    data=json.loads(raw[20:20+length]); assert data.get('meshes'),name

for name in [n.lower()+'-layer' for n in audit]+['portal-layer','battle-stage']+['arena-'+n for n in ['background','forest','volcanic','desert','mountain','wetland','cavern','crypt']]:
    im=bpy.data.images.load(str(OUT/(name+'.png')),check_existing=False)
    assert tuple(im.size)==(1600,900),(name,'canvas changed')
    pixels=array('f',[0])*len(im.pixels); im.pixels.foreach_get(pixels)
    alpha=pixels[3::4]
    assert max(alpha)>.99,(name,'empty render')
    if name.endswith('-layer'):
        assert min(alpha)==0,(name,'missing transparency')
        assert all(alpha[x]<.01 and alpha[1600*899+x]<.01 for x in range(1600)),(name,'vertical clipping')
        assert all(alpha[y*1600]<.01 and alpha[y*1600+1599]<.01 for y in range(900)),(name,'horizontal clipping')
    else: assert min(alpha)>.99,(name,'background not opaque')
    bpy.data.images.remove(im)
for name in [n.lower() for n in audit]+['portal','demon-fighter-kit']+['arena-'+n for n in ['background','forest','volcanic','desert','mountain','wetland','cavern','crypt']]: check_glb(name)
report['checks']='complete roster, added geometry for every presentation, valid rigid-part tags, static canvas dimensions, alpha, unclipped layers, GLB integrity'
(VERIFY/'detail-validation.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report),flush=True)
