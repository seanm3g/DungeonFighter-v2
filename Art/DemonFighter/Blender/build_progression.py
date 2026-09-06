"""Equipment silhouettes and biome stages using the established camera and kit."""
import bpy,math,json,os
from pathlib import Path
from mathutils import Vector,Matrix
HERE=Path(__file__).resolve().parent
OUT=HERE.parents[2]/'GameData/Visuals/DemonFighter'
arena_map={'Ashen land':'volcanic','Den':'forest','Dense Brush':'forest','Desert':'desert',
 'Fire Temple':'volcanic','Lava Cave':'volcanic','Laybrinth':'forest','Mirror Pond':'wetland',
 'Mountains':'mountain','Pyramids':'desert','Spring':'wetland','Water Temple':'wetland'}
roster=json.loads((HERE.parents[2]/'GameData/Enemies.json').read_text(encoding='utf-8-sig'))
(OUT/'arenas.json').write_text(json.dumps({e['name']:arena_map[e['biome']] for e in roster if e['biome'] in arena_map},indent=2)+'\n')
bpy.ops.wm.open_mainfile(filepath=str(HERE/'roster-kit.blend'))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene; scene.cycles.samples=8; scene.render.use_persistent_data=True
steel=bpy.data.materials['Pale forged steel']; ink=bpy.data.materials['Ink / charcoal']
red=bpy.data.materials['Crimson cloth']; eye=bpy.data.materials['Sulfur fire']
cloth=bpy.data.materials['Dead bark']; bone=bpy.data.materials['Old bone']
turn=Matrix.Translation((-3,0,0))@Matrix.Rotation(math.radians(55),4,'Z')@Matrix.Translation((3,0,0))
def primitive(name,loc,scale,mat,c):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,location=loc)
 obj=bpy.context.object; obj.name=name; obj.scale=scale
 for old in list(obj.users_collection): old.objects.unlink(obj)
 c.objects.link(obj); obj.data.materials.append(mat)
 bpy.context.view_layer.update(); obj.matrix_world=turn@obj.matrix_world
 return obj
for weapon in ['sword','dagger','mace','wand','unarmed']:
 for armor in ['cloth','plate']:
  name='Fighter-'+weapon+'-'+armor
  c=bpy.data.collections.new(name); scene.collection.children.link(c); c['presentation_actor']=True
  for source in bpy.data.collections['Fighter'].objects:
   if source.type!='MESH': continue
   if weapon=='unarmed' and 'sword' in source.name.lower(): continue
   if weapon in ['mace','wand'] and ('Broad sword' in source.name or 'crossguard' in source.name): continue
   if armor=='cloth' and ('Pauldron' in source.name or 'breast sigil' in source.name): continue
   obj=source.copy(); obj.data=source.data.copy(); c.objects.link(obj)
   if armor=='cloth' and any(k in obj.name for k in ['Breastplate','Gauntlet','Greave','Closed helm']):
    obj.data.materials.clear(); obj.data.materials.append(cloth)
   if weapon=='dagger' and 'Broad sword' in obj.name:
    # Shorten blade in original local model coordinates, before its world turn.
    for v in obj.data.vertices: v.co.z=1.46+(v.co.z-1.46)*.48
  if weapon in ['mace','wand']:
   obj=primitive('Sword '+weapon+' head',(-2.34,-.26,.55 if weapon=='mace' else 1.95),
     (.26,.26,.34) if weapon=='mace' else (.12,.12,.24),steel if weapon=='mace' else eye,c)
   obj['rig_part']='right'
   obj=primitive('Sword shaft',(-2.34,-.26,1.12),(.06,.06,.72),cloth,c); obj['rig_part']='right'
  for col in bpy.data.collections:
   if col.name!='Lighting': col.hide_render=col!=c
  scene.render.film_transparent=True; scene.render.filepath=str(OUT/(name.lower()+'-layer.png'))
  bpy.ops.render.render(write_still=True)
  bpy.ops.object.select_all(action='DESELECT')
  for obj in c.objects: obj.select_set(True)
  bpy.ops.export_scene.gltf(filepath=str(OUT/(name.lower()+'.glb')),export_format='GLB',use_selection=True)
  c.hide_render=True

# Stages share footprint/camera, but each has different materials, light and props.
for col in bpy.data.collections:
 if col.name!='Lighting': col.hide_render=col.name not in ['Arena','Portal']
original={m.name:tuple(m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value)
 for m in bpy.data.materials if m.use_nodes and m.node_tree.nodes.get('Principled BSDF')}
themes={'forest':(.12,.22,.07),'volcanic':(.22,.035,.025),'desert':(.48,.29,.10),
 'mountain':(.28,.34,.42),'wetland':(.055,.20,.18),'cavern':(.10,.10,.22),'crypt':(.17,.19,.14)}
for theme,color in themes.items():
 for name,base in original.items(): bpy.data.materials[name].node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=base
 for name in ['Weathered basalt','Dead bark']:
  if name in bpy.data.materials: bpy.data.materials[name].node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*color,1)
 props=bpy.data.collections.new('Stage props '+theme); scene.collection.children.link(props)
 for side in [-1,1]:
  for i in range(3):
   loc=(side*(5.3-i*.28),2+i*.35,.4+i*.2)
   bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,location=loc)
   obj=bpy.context.object; obj.scale=(.4,.4,1.2 if theme in ['forest','cavern','mountain'] else .3)
   for old in list(obj.users_collection): old.objects.unlink(obj)
   props.objects.link(obj)
   obj.data.materials.append(bpy.data.materials['Forest venom'] if theme=='forest' else steel if theme in ['cavern','mountain'] else cloth)
 scene.render.film_transparent=False; scene.render.filepath=str(OUT/('arena-'+theme+'.png')); bpy.ops.render.render(write_still=True)
 props.hide_render=True
for name,base in original.items(): bpy.data.materials[name].node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=base
scene.render.film_transparent=True
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'progression-kit.blend'))
print('PROGRESSION ASSETS COMPLETE',flush=True)
