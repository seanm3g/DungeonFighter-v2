import bpy
from pathlib import Path
HERE=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(HERE/'animated-kit.blend'))
bpy.context.preferences.filepaths.save_version=0
for c in list(bpy.data.collections):
 if c.name.startswith('Fighter-'):
  for obj in list(c.objects): bpy.data.objects.remove(obj,do_unlink=True)
  bpy.data.collections.remove(c)
for action in list(bpy.data.actions):
 if action.name.startswith('Fighter-'): bpy.data.actions.remove(action)
with bpy.data.libraries.load(str(HERE/'progression-animated.blend'),link=False) as (src,dst):
 dst.collections=[n for n in src.collections if n.startswith('Fighter-')]
 dst.actions=[n for n in src.actions if n.startswith('Fighter-')]
for c in dst.collections:
 bpy.context.scene.collection.children.link(c); c.hide_render=True
for action in dst.actions: action.use_fake_user=True
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'animated-kit.blend'))
print('EQUIPMENT RIGS MERGED',flush=True)
