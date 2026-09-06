"""Run with Blender 5.1: blender --background --python build_assets.py.

Deterministic first-pass models; no downloaded meshes or textures. Outputs are
relative to this script, so the asset kit can be rebuilt on another machine.
"""
import bpy
import math
import random
import json
import struct
from array import array
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'GameData/Visuals/DemonFighter'
SOURCE = Path(__file__).resolve().parent
OUT.mkdir(parents=True, exist_ok=True)
random.seed(17)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for collection in list(bpy.data.collections):
    if collection.name != 'Collection':
        bpy.data.collections.remove(collection)

def material(name, color, emission=0, roughness=.8, grain=False):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    nodes = m.node_tree.nodes
    bs = nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*color, 1)
    bs.inputs['Roughness'].default_value = roughness
    if emission:
        bs.inputs['Emission Color'].default_value = (*color, 1)
        bs.inputs['Emission Strength'].default_value = emission
    if grain:
        noise = nodes.new('ShaderNodeTexNoise')
        noise.inputs['Scale'].default_value = 95
        ramp = nodes.new('ShaderNodeValToRGB')
        ramp.color_ramp.elements[0].position = .38
        ramp.color_ramp.elements[0].color = (*[c*.18 for c in color], 1)
        ramp.color_ramp.elements[1].position = .6
        ramp.color_ramp.elements[1].color = (*color, 1)
        m.node_tree.links.new(noise.outputs['Fac'], ramp.inputs[0])
        m.node_tree.links.new(ramp.outputs[0], bs.inputs['Base Color'])
    return m

ink = material('Ink / charcoal', (.025,.029,.028), grain=True)
stone = material('Weathered basalt', (.16,.18,.16), grain=True)
steel = material('Pale forged steel', (.40,.46,.40), roughness=.5, grain=True)
bone = material('Old bone', (.8,.77,.53), grain=True)
red = material('Crimson cloth', (.65,.008,.045), grain=True)
acid = material('Sulfur enamel', (.78,.94,.006), emission=.25, grain=True)
glow = material('Portal crimson', (.8,.003,.025), emission=2)
eyes = material('Sulfur fire', (.8,1,.005), emission=3)
current = None
groups = {}

def group(name):
    global current
    current = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(current)
    groups[name] = current

def finish(obj, name, mat):
    obj.name = name
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    current.objects.link(obj)
    obj.data.materials.append(mat)
    return obj

def cube(name, loc, scale, mat, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = finish(bpy.context.object, name, mat)
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Chipped edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        obj.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL')
    return obj

def ico(name, loc, scale, mat, subdivisions=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1, location=loc)
    obj = finish(bpy.context.object, name, mat)
    obj.scale = scale
    return obj

def rod(name, a, b, r1, r2, mat, vertices=6):
    d = Vector(b)-Vector(a)
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=r1, radius2=r2,
                                    depth=d.length, location=(Vector(a)+Vector(b))/2)
    obj = finish(bpy.context.object, name, mat)
    obj.rotation_euler = d.to_track_quat('Z','Y').to_euler()
    return obj

def mesh(name, verts, faces, mat):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    current.objects.link(obj)
    obj.data.materials.append(mat)
    return obj

group('Arena')
cube('Diorama plinth', (0,0,-.38), (12,7,.65), ink, .18)
for x in range(-6,6):
    for y in range(-3,4):
        tile=cube('Broken flagstone', (x+.48,y-.35,random.uniform(-.06,.01)),
                  (.94,.94,.16),stone,.07)
        tile.rotation_euler.z=random.uniform(-.035,.035)
for x in [-5.2,5.2]:
    for y in [-1.6,2.4]:
        cube('Pillar foot',(x,y,.2),(.95,.95,.4),ink,.09)
        for z in range(5 if y > 0 else 1):
            cube('Column course',(x,y,.7+z*.65),(.58,.58,.6),stone,.06)
        top = 3.7 if y > 0 else 1.05
        cube('Column crown',(x,y,top),(.95,.9,.26),steel,.05)
        rod('Sulfur candle',(x,y,top+.1),(x,y,top+.52),.12,.02,eyes)
for i in range(24):
    x=random.uniform(-5.5,5.5)
    y=random.choice([random.uniform(2.7,3),random.uniform(-3,-2.5)])
    ico('Rubble',(x,y,.12),(.24,.2,.24),stone)

group('Portal')
# Front-facing arched aperture and individual masonry voussoirs.
verts=[(-1.32,2.48,.12),(1.32,2.48,.12),(1.32,2.48,2.6)]
verts += [(1.32*math.cos(t),2.48,2.6+1.32*math.sin(t)) for t in [i*math.pi/16 for i in range(17)]]
verts += [(-1.32,2.48,.12)]
mesh('Red veil',verts,[tuple(range(len(verts)))],glow)
for side in [-1,1]:
    for i in range(5):
        cube('Gate pier',(side*1.62,2.5,.36+i*.54),(.6,.72,.49),stone,.065)
    cube('Gate base',(side*1.62,2.5,.14),(.95,1,.28),ink,.04)
for i in range(13):
    t=(i+.5)*math.pi/13
    o=cube('Arch stone',(1.62*math.cos(t),2.5,2.6+1.62*math.sin(t)),(.46,.75,.6),stone,.045)
    o.rotation_euler.y=math.pi/2-t
for x in [-1.1,-.78,-.4,0,.4,.78,1.1]:
    z=2.6+math.sqrt(1.32**2-x*x)
    rod('Upper fang',(x,2.03,z),(x*.92,2.02,z-(.65 if abs(x)>.7 else .34)),.17,.015,bone)
for x in [-1.1,-.75,.75,1.1]:
    rod('Lower fang',(x,2.05,.1),(x*.9,2.05,.65),.14,0,bone)
for z, width in [(.03,3.5),(-.01,4.2)]:
    cube('Threshold',(0,1.95 if width<4 else 1.55,z),(width,.6,.22),ink,.04)
ico('Gate skull',(0,2.05,4.35),(.48,.26,.5),bone,2)
for x in [-.18,.18]:
    ico('Gate eye',(x,1.805,4.38),(.14,.045,.13),ink)

def fighter():
    group('Fighter')
    x=-3
    def p(a): return (a[0]+x,a[1],a[2])
    for side in [-1,1]:
        foot=(side*.38,-.03,.2)
        knee=(side*.34,0,.85)
        hip=(side*.23,.06,1.45)
        cube('Armored boot',p(foot),(.42,.65,.33),ink,.07)
        rod('Greave',p(foot),p(knee),.18,.23,steel)
        ico('Knee',p(knee),(.23,.23,.2),acid)
        rod('Thigh',p(knee),p(hip),.20,.26,ink)
    ico('Waist',p((0,0,1.46)),(.42,.27,.28),ink)
    ico('Breastplate',p((0,0,1.95)),(.57,.35,.65),steel,1)
    ico('Sulfur breast sigil',p((0,-.305,2.03)),(.18,.035,.3),acid)
    for side in [-1,1]:
        shoulder=(side*.6,0,2.24)
        elbow=(side*.76,-.08,1.82)
        hand=(side*.66,-.26,1.45)
        ico('Pauldron',p(shoulder),(.38,.4,.28),steel)
        rod('Arm',p(shoulder),p(elbow),.18,.15,ink)
        rod('Gauntlet',p(elbow),p(hand),.20,.15,steel)
        ico('Glove',p(hand),(.17,.16,.18),ink)
    ico('Closed helm',p((0,0,2.76)),(.32,.3,.46),steel)
    cube('Visor slit',p((0,-.269,2.78)),(.40,.035,.075),ink)
    cube('Visor ember',p((0,-.29,2.78)),(.25,.016,.025),eyes)
    mesh('Torn crimson mantle',[p(v) for v in [(-.43,.22,2.45),(.43,.22,2.45),(.6,.53,1.3),(.65,.85,.3),(.22,.79,.5),(-.1,.93,.22),(-.5,.82,.43),(-.7,.53,1.25)]],[(0,1,2,3,4,5,6,7)],red)
    rod('Sword grip',p((.66,-.26,1.18)),p((.66,-.26,1.7)),.065,.065,red)
    rod('Sword crossguard',p((.29,-.26,1.45)),p((1.03,-.26,1.45)),.065,.065,acid)
    mesh('Broad sword',[p(v) for v in [(.55,-.26,1.46),(.77,-.26,1.46),(.84,-.26,.38),(.66,-.26,.05),(.48,-.26,.38),(.66,-.36,.55)]],[(0,1,5),(1,2,5),(2,3,5),(3,4,5),(4,0,5),(0,4,3,2,1)],steel)

def demon():
    group('Demon')
    x=3
    def p(a): return (a[0]+x,a[1],a[2])
    for s in [-1,1]:
        foot=(s*.46,-.08,.14); ankle=(s*.46,.2,.55); knee=(s*.36,-.04,1.1); hip=(s*.23,.08,1.62)
        ico('Cloven hoof',p(foot),(.32,.42,.22),ink)
        rod('Hock',p(foot),p(ankle),.13,.15,bone)
        rod('Shin',p(ankle),p(knee),.12,.21,bone)
        rod('Femur',p(knee),p(hip),.14,.22,bone)
        shoulder=(s*.68,.05,2.55); elbow=(s*.94,-.02,2.02); wrist=(s*.92,-.26,1.45)
        rod('Upper arm',p(shoulder),p(elbow),.19,.14,bone)
        rod('Forearm',p(elbow),p(wrist),.2,.1,bone)
        ico('Knuckle',p(wrist),(.19,.14,.22),ink)
        for f in range(3):
            a=(s*(.8+f*.11),-.3,1.44)
            rod('Talon',p(a),p((a[0]+s*.09,-.44,1.08)),.055,0,bone)
        ico('Spiked shoulder',p(shoulder),(.34,.29,.3),ink)
        for k in range(3):
            rod('Shoulder spike',p((s*(.52+k*.13),0,2.66)),p((s*(.64+k*.23),0,3.04-k*.08)),.105,0,bone)
    ico('Pelvis',p((0,0,1.6)),(.36,.25,.35),ink)
    rod('Spine',p((0,.07,1.6)),p((0,.07,2.78)),.14,.15,bone)
    ico('Black rib core',p((0,0,2.25)),(.44,.25,.55),ink)
    for z,w in [(1.95,.29),(2.13,.39),(2.31,.46),(2.49,.48)]:
        for s in [-1,1]:
            rod('Rib',p((s*.06,-.29,z-.08)),p((s*w,-.19,z+.06)),.075,.085,bone)
    ico('Demon skull',p((0,0,3.05)),(.35,.27,.43),bone,2)
    for s in [-1,1]:
        ico('Eye socket',p((s*.15,-.24,3.13)),(.145,.06,.13),ink)
        ico('Burning eye',p((s*.15,-.287,3.12)),(.07,.016,.037),eyes)
        points=[(s*.24,.02,3.3),(s*.48,.05,3.57),(s*.60,.08,3.91),(s*.49,.08,4.24)]
        for i in range(3):
            rod('Swept horn',p(points[i]),p(points[i+1]),[.19,.14,.08][i],[.14,.08,0][i],bone)
    ico('Nasal cavity',p((0,-.26,2.99)),(.072,.038,.10),ink)
    cube('Mouth',p((0,-.235,2.86)),(.3,.045,.105),ink)
    for t in range(5):
        rod('Tooth',p((-.13+t*.065,-.275,2.92)),p((-.13+t*.065,-.275,2.81)),.026,.006,bone)
    mesh('Ragged loincloth',[p(v) for v in [(-.28,-.22,1.72),(.28,-.22,1.72),(.37,-.25,.7),(.1,-.27,.9),(-.13,-.27,.58),(-.32,-.25,.9)]],[(0,1,2,3,4,5)],red)

fighter()
demon()
# Turn the combatants inward while retaining their three-quarter silhouettes.
for name, x, angle in [('Fighter',-3,55),('Demon',3,-45)]:
    transform = Matrix.Translation((x,0,0)) @ Matrix.Rotation(math.radians(angle),4,'Z') @ Matrix.Translation((-x,0,0))
    for obj in groups[name].objects:
        obj.matrix_world = transform @ obj.matrix_world
group('Lighting')
def area(name, loc, color, power, size, target):
    data=bpy.data.lights.new(name,'AREA'); data.energy=power; data.color=color; data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data); current.objects.link(obj); obj.location=loc
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
area('Bone key',(-3,-6,8),(1,.91,.65),650,4,(0,0,1))
area('Acid rim',(4,3,6),(.75,1,.025),850,3,(0,0,1.5))
area('Crimson gate',(0,2,3),(1,.008,.035),250,3,(0,-2,1))
scene=bpy.context.scene
scene.render.engine='CYCLES'
scene.cycles.samples=24
scene.cycles.use_denoising=True
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.006,.008,.007,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.3
scene.view_settings.view_transform='Standard'
scene.render.image_settings.file_format='PNG'
scene.render.image_settings.color_mode='RGBA'
scene.render.resolution_percentage=100
bpy.context.preferences.filepaths.save_version=0
bpy.ops.object.camera_add(location=(4,-20,9))
camera=bpy.context.object
camera.name='Orthographic battle camera'
camera.rotation_euler=(Vector((0,.4,1.5))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO'; camera.data.ortho_scale=15
scene.camera=camera
scene.render.resolution_x=1600; scene.render.resolution_y=900
scene.render.film_transparent=False
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'demon-fighter-kit.blend'))
bpy.ops.object.select_all(action='DESELECT')
for name in ['Arena','Portal','Fighter','Demon']:
    for obj in groups[name].objects: obj.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(OUT/'demon-fighter-kit.glb'),export_format='GLB',use_selection=True)
scene.render.filepath=str(OUT/'battle-stage.png')
bpy.ops.render.render(write_still=True)
# Background plate and aligned individual layers share one camera and canvas.
for name in ['Fighter','Demon']: groups[name].hide_render=True
scene.render.filepath=str(OUT/'arena-background.png')
bpy.ops.render.render(write_still=True)
scene.render.film_transparent=True
for name in ['Arena','Portal','Fighter','Demon']: groups[name].hide_render=True
for name in ['Fighter','Demon','Portal']:
    groups[name].hide_render=False
    scene.render.filepath=str(OUT/(name.lower()+'-layer.png'))
    bpy.ops.render.render(write_still=True)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in groups[name].objects: obj.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(OUT/(name.lower()+'.glb')),export_format='GLB',use_selection=True)
    groups[name].hide_render=True
manifest = {
    'name': 'Demon Fighter / Fanged Gate prototype',
    'version': 1,
    'canvas': {'width': 1600, 'height': 900, 'projection': 'orthographic'},
    'composite': 'battle-stage.png',
    'background': 'arena-background.png',
    'combatantLayers': ['fighter-layer.png', 'demon-layer.png'],
    'optionalPortalLayer': 'portal-layer.png',
    'notes': [
        'Draw background and combatant layers into the same destination rectangle.',
        'Portal is already present in the background; the separate portal is for other compositions.',
        'Transparent layers omit cast ground shadows; the composite has full scene lighting.',
        'GLB files retain scene placement. Blender units are meters; glTF exporter converts to Y-up.',
        'Procedural grain is Blender-only; GLB exports use base material colors.',
        'Static prototype models: no skeletal rig, animation clips, or live combat integration.'
    ],
    'verifiedAssets': []
}
for filename in ['battle-stage.png','arena-background.png','fighter-layer.png','demon-layer.png','portal-layer.png']:
    im = bpy.data.images.load(str(OUT/filename), check_existing=False)
    assert tuple(im.size) == (1600,900), filename
    pixels = array('f', [0]) * len(im.pixels)
    im.pixels.foreach_get(pixels)
    alpha = pixels[3::4]
    low, high = min(alpha), max(alpha)
    if '-layer' in filename:
        assert low == 0 and high > .99, 'Missing transparency or subject: '+filename
    else:
        assert low > .99, 'Background must be opaque: '+filename
    manifest['verifiedAssets'].append({'file': filename, 'alphaRange': [low,high]})
    bpy.data.images.remove(im)
for filename in ['demon-fighter-kit.glb','fighter.glb','demon.glb','portal.glb']:
    data = (OUT/filename).read_bytes()
    magic, version, length = struct.unpack_from('<4sII',data)
    assert magic == b'glTF' and version == 2 and length == len(data), filename
    json_length, chunk_type = struct.unpack_from('<II',data,12)
    assert chunk_type == 0x4E4F534A
    gltf = json.loads(data[20:20+json_length])
    assert len(gltf.get('meshes',[])) > 0, filename
    manifest['verifiedAssets'].append({'file': filename, 'meshes': len(gltf['meshes']), 'bytes': length})
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
print('DEMON FIGHTER ASSET KIT COMPLETE:', OUT)
