"""Export the reviewed detailed kit without rerunning model generation."""
import bpy, json, os
from pathlib import Path
HERE=Path(__file__).resolve().parent
OUT=HERE.parents[2]/'GameData/Visuals/DemonFighter'
os.environ['OPTIX_CACHE_PATH']=str(HERE.parent/'Verification/render-cache')
bpy.ops.wm.open_mainfile(filepath=str(HERE/'detailed-kit.blend'))
scene=bpy.context.scene
scene.cycles.device='CPU'
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences
    prefs.compute_device_type='OPTIX'; prefs.get_devices()
    if any(d.type=='OPTIX' for d in prefs.devices):
        for d in prefs.devices: d.use=d.type=='OPTIX'
        scene.cycles.device='GPU'
except (TypeError,RuntimeError): pass
def visible(names):
    for c in bpy.data.collections:
        c.hide_render=c.name not in names and c.name!='Lighting'
        c.hide_viewport=False
def glb(filename,names):
    bpy.ops.object.select_all(action='DESELECT')
    for name in names:
        for o in bpy.data.collections[name].objects:
            if o.type=='MESH': o.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(OUT/filename),export_format='GLB',use_selection=True)
visible(['Arena','Portal','Fighter','Demon'])
scene.render.film_transparent=False
scene.render.filepath=str(OUT/'battle-stage.png'); bpy.ops.render.render(write_still=True)
glb('demon-fighter-kit.glb',['Arena','Portal','Fighter','Demon'])
actors=['Fighter','Demon','Skeleton','Bat']+[c.name for c in bpy.data.collections if 'family' in c or 'presentation_actor' in c]
for name in actors+['Portal']:
    visible([name]); scene.render.film_transparent=True
    scene.render.filepath=str(OUT/(name.lower()+'-layer.png')); bpy.ops.render.render(write_still=True)
    glb(name.lower()+'.glb',[name]); print('DETAILED ACTOR',name,flush=True)
themes={'background':(.16,.155,.135),'forest':(.12,.17,.085),'volcanic':(.15,.065,.045),
        'desert':(.28,.22,.12),'mountain':(.18,.22,.24),'wetland':(.085,.16,.14),'cavern':(.12,.105,.18),'crypt':(.16,.155,.135)}
stone=bpy.data.materials['Weathered basalt']; ramp=stone.node_tree.nodes['Gothic patina'].color_ramp
for theme,color in themes.items():
    names=['Arena','Portal']+(['Stage props '+theme] if theme!='background' else [])
    visible(names); scene.render.film_transparent=False
    ramp.elements[0].color=(*(v*.28 for v in color),1); ramp.elements[1].color=(*color,1)
    stone.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*color,1)
    scene.render.filepath=str(OUT/('arena-'+theme+'.png')); bpy.ops.render.render(write_still=True)
    glb('arena-'+theme+'.glb',names); print('DETAILED ARENA',theme,flush=True)
manifest=json.loads((OUT/'manifest.json').read_text())
manifest.update(name='Demon Fighter / Gothic ruined sanctuary',version=2)
manifest['notes']=[
    'Detailed layered geometry and procedural weathering authored in detailed-kit.blend.',
    'All actor layers and stages share the original 1600x900 orthographic camera.',
    'Animated runtime atlas frames use an 800x450 canvas; see Animations/clips.json.',
    'GLB exports preserve detailed geometry and base PBR colors; Blender procedural grain is rendered into PNGs, not baked into GLB textures.',
    '46 actor presentations share rigid-part animation rigs; all 127 enemy definitions map through enemies.json.'
]
manifest.pop('verifiedAssets',None)
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('DETAILED EXPORT COMPLETE',flush=True)
