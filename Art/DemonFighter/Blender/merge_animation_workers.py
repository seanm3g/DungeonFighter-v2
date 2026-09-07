"""Merge disjoint animation export workers into one editable rig kit and manifest."""
import bpy,json
from pathlib import Path
HERE=Path(__file__).resolve().parent
TEMP=HERE.parent/'Verification/animation-workers'
OUT=HERE.parents[2]/'GameData/Visuals/DemonFighter/Animations'
manifest={'version':1,'canvasWidth':800,'canvasHeight':450,'actors':{}}
base=HERE/('detailed-kit.blend' if (HERE/'detailed-kit.blend').exists() else 'progression-kit.blend')
bpy.ops.wm.open_mainfile(filepath=str(base if base.exists() else HERE/'roster-kit.blend'))
bpy.context.preferences.filepaths.save_version=0
for i in range(4):
 data=json.loads((TEMP/f'clips-{i}.json').read_text())
 assert not set(data['actors']) & set(manifest['actors'])
 with bpy.data.libraries.load(str(TEMP/f'kit-{i}.blend'),link=False) as (src,dst):
  names=[name for name in src.collections if name.lower() in data['actors']]
 for name in names:
  old=bpy.data.collections.get(name)
  if old:
   for obj in list(old.objects): bpy.data.objects.remove(obj,do_unlink=True)
   bpy.data.collections.remove(old)
 with bpy.data.libraries.load(str(TEMP/f'kit-{i}.blend'),link=False) as (src,dst):
  dst.collections=names
  dst.actions=[name for name in src.actions if name.split('/')[0] in names]
 for c in dst.collections:
  bpy.context.scene.collection.children.link(c)
  c.hide_render=c.name not in ['Fighter','Bat']
 for action in dst.actions: action.use_fake_user=True
 manifest['actors'].update(data['actors'])
expected=set(json.loads((OUT.parent/'enemies.json').read_text()).values())|{'fighter','demon'}
if base.exists():
 expected|={f'fighter-{weapon}-{armor}' for weapon in ['sword','dagger','mace','wand','unarmed'] for armor in ['cloth','plate']}
assert set(manifest['actors'])==expected
bpy.data.collections['Arena'].hide_render=False
bpy.data.collections['Portal'].hide_render=False
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'animated-kit.blend'))
(OUT/'clips.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('MERGED',len(manifest['actors']),'animated actors',flush=True)
