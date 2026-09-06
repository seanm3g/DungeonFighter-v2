"""Build Bat and Skeleton using the existing stage camera and materials."""
import bpy
import math
from pathlib import Path
from mathutils import Vector, Matrix

SOURCE = Path(__file__).resolve().parent
OUT = SOURCE.parents[2] / 'GameData/Visuals/DemonFighter'
bpy.ops.wm.open_mainfile(filepath=str(SOURCE / 'demon-fighter-kit.blend'))
bpy.context.preferences.filepaths.save_version = 0
bone = bpy.data.materials['Old bone']
ink = bpy.data.materials['Ink / charcoal']
red = bpy.data.materials['Crimson cloth']
eyes = bpy.data.materials['Sulfur fire']

def collection(name):
    c = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(c)
    return c

def finish(obj, name, mat, c):
    obj.name = name
    for old in list(obj.users_collection): old.objects.unlink(obj)
    c.objects.link(obj)
    obj.data.materials.append(mat)
    return obj

def sphere(name, loc, scale, mat, c):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1, location=loc)
    obj = finish(bpy.context.object, name, mat, c)
    obj.scale = scale
    return obj

def rod(name, a, b, r, mat, c):
    d = Vector(b)-Vector(a)
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=r, radius2=.008, depth=d.length,
        location=(Vector(a)+Vector(b))/2)
    obj = finish(bpy.context.object, name, mat, c)
    obj.rotation_euler = d.to_track_quat('Z','Y').to_euler()

skeleton = collection('Skeleton')
for obj in bpy.data.collections['Demon'].objects:
    if any(obj.name.startswith(n) for n in ['Swept horn','Shoulder spike','Spiked shoulder','Ragged loincloth','Cloven hoof']):
        continue
    duplicate = obj.copy()
    duplicate.data = obj.data.copy()
    skeleton.objects.link(duplicate)
    duplicate.name = 'Skeleton / ' + obj.name
turn = Matrix.Translation((3,0,0)) @ Matrix.Rotation(math.radians(-45),4,'Z') @ Matrix.Translation((-3,0,0))
for side in [-1,1]:
    foot = sphere('Skeleton foot',(3+side*.46,-.08,.14),(.17,.34,.13),bone,skeleton)
    bpy.context.view_layer.update()
    foot.matrix_world = turn @ foot.matrix_world
    shoulder = sphere('Skeleton shoulder',(3+side*.68,.05,2.55),(.21,.21,.23),bone,skeleton)
    bpy.context.view_layer.update()
    shoulder.matrix_world = turn @ shoulder.matrix_world

bat = collection('Bat')
def p(x,y,z): return (3+x,y,z)
sphere('Bat body',p(0,0,2.15),(.3,.25,.57),ink,bat)
sphere('Bat head',p(0,-.03,2.68),(.34,.28,.29),ink,bat)
for side in [-1,1]:
    rod('Pointed ear',p(side*.2,0,2.79),p(side*.36,0,3.33),.19,red,bat)
    sphere('Bat eye',p(side*.14,-.265,2.7),(.075,.025,.055),eyes,bat)
    rod('Bat fang',p(side*.1,-.27,2.51),p(side*.09,-.3,2.3),.055,bone,bat)
    rod('Bat claw',p(side*.15,0,1.78),p(side*.24,-.12,1.54),.07,bone,bat)
    verts = [p(side*x,y,z) for x,y,z in [(0.18,.03,2.46),(.75,0,3.1),(1.8,.1,3.38),
        (1.5,.02,2.77),(1.52,.02,2.42),(1.08,-.06,2.47),(.94,-.08,2.02),(.61,-.12,2.16),(.27,-.02,1.87)]]
    data = bpy.data.meshes.new('Scalloped wing membrane')
    data.from_pydata(verts,[],[(0,1,2),(0,2,3),(0,3,4),(0,4,5),(0,5,6),(0,6,7),(0,7,8)])
    data.update()
    obj = bpy.data.objects.new('Crimson wing',data)
    bat.objects.link(obj)
    data.materials.append(red)
    for end in [1,2,4,6,8]:
        rod('Wing finger',verts[0],verts[end],.045,bone,bat)

scene=bpy.context.scene
scene.render.film_transparent=True
for name in ['Arena','Portal','Fighter','Demon','Skeleton','Bat']:
    bpy.data.collections[name].hide_render=True
for c in [bat,skeleton]:
    c.hide_render=False
    scene.render.filepath=str(OUT/(c.name.lower()+'-layer.png'))
    bpy.ops.render.render(write_still=True)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in c.objects: obj.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(OUT/(c.name.lower()+'.glb')),export_format='GLB',use_selection=True)
    c.hide_render=True
bat.hide_render=False
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'enemy-kit.blend'))
print('BAT AND SKELETON COMPLETE')
