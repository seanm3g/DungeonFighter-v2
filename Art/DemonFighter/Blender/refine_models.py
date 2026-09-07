"""Deterministic gothic detail pass. Follow with export_detailed.py for runtime assets.

The original progression kit remains the editable blockout. This builds a separate
detailed kit, preserving collection IDs, world placement and rigid rig-part tags.
"""
import bpy, bmesh, math, random, sys, json, os
from pathlib import Path
from mathutils import Vector, Matrix

HERE = Path(__file__).resolve().parent
OUT = HERE.parents[2] / 'GameData/Visuals/DemonFighter'
VERIFY = HERE.parent / 'Verification'
os.environ['OPTIX_CACHE_PATH']=str(VERIFY/'render-cache')
rng = random.Random(1977)
bpy.ops.wm.open_mainfile(filepath=str(HERE / 'progression-kit.blend'))
bpy.context.preferences.filepaths.save_version = 0
scene = bpy.context.scene
scene.cycles.samples = 24
scene.cycles.device = 'CPU'
scene.render.use_persistent_data = True
try:
    prefs = bpy.context.preferences.addons['cycles'].preferences
    prefs.compute_device_type = 'OPTIX'; prefs.get_devices()
    if any(d.type == 'OPTIX' for d in prefs.devices):
        for d in prefs.devices: d.use = d.type == 'OPTIX'
        scene.cycles.device = 'GPU'
except (TypeError, RuntimeError): pass

palette = {
    'Ink / charcoal': (.02,.024,.025), 'Weathered basalt': (.16,.155,.135),
    'Pale forged steel': (.23,.25,.24), 'Old bone': (.46,.41,.30),
    'Crimson cloth': (.19,.012,.023), 'Sulfur enamel': (.39,.27,.085),
    'Sulfur fire': (1,.37,.045), 'Portal crimson': (.46,.003,.011),
    'Forest venom': (.15,.21,.085), 'Drowned spectral teal': (.07,.22,.23),
    'Glacial slate': (.23,.29,.32), 'Molten crimson': (.38,.026,.018),
    'Dead bark': (.115,.071,.035), 'Tomb ochre': (.32,.22,.085),
}
for mat in bpy.data.materials:
    if not mat.use_nodes: continue
    nodes=mat.node_tree.nodes; links=mat.node_tree.links
    bs=nodes.get('Principled BSDF')
    if not bs: continue
    base=palette.get(mat.name, tuple(bs.inputs['Base Color'].default_value)[:3])
    bs.inputs['Base Color'].default_value=(*base,1)
    for link in list(bs.inputs['Base Color'].links): links.remove(link)
    metal=mat.name in ['Pale forged steel','Sulfur enamel','Glacial slate','Tomb ochre']
    bs.inputs['Metallic'].default_value=.72 if metal else 0
    bs.inputs['Roughness'].default_value=.43 if metal else .88
    noise=nodes.new('ShaderNodeTexNoise'); noise.name='Weathering grain'; noise.inputs['Scale'].default_value=22 if metal else 9
    noise.inputs['Detail'].default_value=4
    ramp=nodes.new('ShaderNodeValToRGB'); ramp.name='Gothic patina'
    ramp.color_ramp.elements[0].position=.2; ramp.color_ramp.elements[0].color=(*(v*.28 for v in base),1)
    ramp.color_ramp.elements[1].position=.8; ramp.color_ramp.elements[1].color=(*base,1)
    links.new(noise.outputs['Fac'],ramp.inputs[0]); links.new(ramp.outputs[0],bs.inputs['Base Color'])
    fine=nodes.new('ShaderNodeTexNoise'); fine.inputs['Scale'].default_value=120 if metal else 65
    bump=nodes.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.27
    bump.inputs['Distance'].default_value=.012 if metal else .025
    links.new(fine.outputs['Fac'],bump.inputs['Height']); links.new(bump.outputs[0],bs.inputs['Normal'])
    if mat.name in ['Sulfur fire','Portal crimson','Molten crimson','Drowned spectral teal','Sulfur enamel']:
        bs.inputs['Emission Color'].default_value=(*base,1)
        bs.inputs['Emission Strength'].default_value=2 if mat.name=='Sulfur fire' else .7 if mat.name=='Portal crimson' else .12
        if mat.name=='Portal crimson':
            links.new(ramp.outputs[0],bs.inputs['Emission Color'])

stone=bpy.data.materials['Weathered basalt']; steel=bpy.data.materials['Pale forged steel']
bone=bpy.data.materials['Old bone']; red=bpy.data.materials['Crimson cloth']
gold=bpy.data.materials['Sulfur enamel']; ink=bpy.data.materials['Ink / charcoal']

def mesh(name, verts, faces, mat, col, part='torso'):
    data=bpy.data.meshes.new(name); data.from_pydata(verts,[],faces); data.update()
    obj=bpy.data.objects.new(name,data); col.objects.link(obj); data.materials.append(mat)
    obj['rig_part']=part
    return obj

def cube(name, loc, scale, mat, col, bevel=.035):
    verts=[(loc[0]+x*scale[0]/2,loc[1]+y*scale[1]/2,loc[2]+z*scale[2]/2) for x,y,z in [(-1,-1,-1),(-1,-1,1),(-1,1,-1),(-1,1,1),(1,-1,-1),(1,-1,1),(1,1,-1),(1,1,1)]]
    obj=mesh(name,verts,[(0,4,6,2),(1,3,7,5),(0,1,5,4),(2,6,7,3),(0,2,3,1),(4,5,7,6)],mat,col)
    mod=obj.modifiers.new('Worn edges','BEVEL'); mod.width=bevel; mod.segments=2
    return obj

def orb(name, loc, scale, mat, col, part='torso'):
    data=bpy.data.meshes.new(name); bm=bmesh.new()
    bmesh.ops.create_icosphere(bm,subdivisions=2,radius=1)
    bm.to_mesh(data); bm.free()
    obj=bpy.data.objects.new(name,data); col.objects.link(obj)
    obj.location=loc; obj.scale=scale; data.materials.append(mat); obj['rig_part']=part
    return obj

def rod(name, a, b, radius, mat, col, end=.005, part='torso'):
    a,b=Vector(a),Vector(b); delta=b-a
    turn=delta.to_track_quat('Z','Y').to_matrix(); verts=[]
    for z,r in [(0,radius),(delta.length,end)]:
        for i in range(10): verts.append(a+turn@Vector((r*math.cos(i*math.tau/10),r*math.sin(i*math.tau/10),z)))
    faces=[tuple(range(9,-1,-1)),tuple(range(10,20))]+[(i,(i+1)%10,(i+1)%10+10,i+10) for i in range(10)]
    obj=mesh(name,verts,faces,mat,col,part)
    return obj

def part_for(obj, x):
    if 'rig_part' in obj: return obj['rig_part']
    name=obj.name.lower(); center=sum((obj.matrix_world@v.co for v in obj.data.vertices),Vector())/len(obj.data.vertices)
    if any(s in name for s in ['boot','greave','knee','thigh','hoof','foot','shin','femur','hock']): return 'legL' if center.x<x else 'legR'
    if any(s in name for s in ['helm','visor','skull','eye','socket','horn','nasal','mouth','tooth','head','ear','fang']): return 'head'
    if any(s in name for s in ['arm','gauntlet','glove','sword','wing','knuckle','talon']): return 'left' if center.x<x else 'right'
    return 'torso' if center.z>1.55 else 'root'

actors=['Fighter','Demon','Skeleton','Bat']+[c.name for c in bpy.data.collections if 'family' in c or 'presentation_actor' in c]
audit={}
for name in actors:
    col=bpy.data.collections[name]; originals=[o for o in col.objects if o.type=='MESH']
    x=-3 if name.startswith('Fighter') else 3
    for obj in originals:
        obj['rig_part']=part_for(obj,x)
        label=obj.name.lower(); mat=obj.data.materials[0] if obj.data.materials else stone
        # Chamfered edges read as metal and bone rather than primitive triangles.
        if len(obj.data.polygons)>4 and not any(s in label for s in ['eye','socket','visor','mote']):
            mod=obj.modifiers.new('Hand worn edges','BEVEL'); mod.width=.018; mod.segments=2
        shell=any(s in label for s in ['breastplate','pauldron','gauntlet','greave','helm','carapace','shell','torso','shoulder','chest body','open lid'])
        organic=name.lower() in ['wolf','bear','boar','rat','firehound','bird','bat','treant','vine','lizard','toad','snake','leech','fish','spore']
        eligible=shell or any(s in label for s in ['spirit core','swarm mote']) or (organic and any(s in label for s in ['body','head','haunch','coiled segment','cap','wing']))
        if not eligible: continue
        # Inset overlapping surface plates / scales / fur tufts, in world space.
        verts=[]; faces=[]
        polys=list(obj.data.polygons)
        for poly in polys[::max(1,len(polys)//18)]:
            points=[obj.matrix_world@obj.data.vertices[i].co for i in poly.vertices]
            center=sum(points,Vector())/len(points)
            normal=(points[1]-points[0]).cross(points[2]-points[0]).normalized()
            if normal.length<.5: continue
            start=len(verts)
            for p in points: verts.append(center+(p-center)*.78+normal*.018)
            tip=center+normal*(.095 if organic else .045)
            if organic: tip.z+=.075
            verts.append(tip)
            for i in range(len(points)): faces.append((start+i,start+(i+1)%len(points),start+len(points)))
        if verts: mesh('Layered '+obj.name,verts,faces,mat,col,obj['rig_part'])
        # Small metal fasteners follow the same part as their plate.
        if shell and not organic:
            for poly in polys[::max(1,len(polys)//4)][:4]:
                p=obj.matrix_world@poly.center
                orb('Forged rivet '+obj.name,p,(.025,.025,.025),gold,col,obj['rig_part'])

    if name.startswith('Fighter'):
        turn=Matrix.Translation((-3,0,0))@Matrix.Rotation(math.radians(55),4,'Z')@Matrix.Translation((3,0,0))
        # Replace the flat single polygon cape with a pleated, uneven torn hem.
        for obj in originals:
            if 'mantle' in obj.name.lower(): bpy.data.objects.remove(obj,do_unlink=True)
        verts=[]; faces=[]; nx=13; ny=9
        for j in range(ny):
            t=j/(ny-1)
            for i in range(nx):
                u=i/(nx-1); width=.48+.19*t
                z=2.42-2.05*t + (0 if j<ny-1 else (.16 if i%3==0 else -.035))
                verts.append(turn@Vector((-3+(u*2-1)*width,.24+t*.69+math.sin(u*math.pi*8)*(.035+.065*t),z)))
        for j in range(ny-1):
            for i in range(nx-1):
                k=j*nx+i; faces.append((k,k+1,k+1+nx,k+nx))
        cape=mesh('Pleated torn crimson mantle',verts,faces,red,col)
        solid=cape.modifiers.new('Cloth thickness','SOLIDIFY'); solid.thickness=.012
        for s in [-1,1]:
            for i in range(3):
                # Overlapping hip lames give the torso a more human armored outline.
                z=1.52-i*.15
                o=orb('Articulated hip lame',(-3+s*(.28+i*.045),-.14,z),(.24,.28,.13),steel if 'cloth' not in name else red,col,'torso')
                bpy.context.view_layer.update(); o.matrix_world=turn@o.matrix_world
        # Ridged overlapping shoulder armor, rather than a single faceted sphere.
        if 'cloth' not in name:
            for s in [-1,1]:
                for i in range(3):
                    cx=-3+s*(.46+i*.12); z=2.37-i*.07
                    v=[(cx-.16,-.28,z),(cx+.16,-.28,z),(cx+.20,.15,z+.025),(cx-.20,.15,z+.025),(cx,-.04,z+.15)]
                    o=mesh('Ridged shoulder lame',[turn@Vector(p) for p in v],[(0,1,4),(1,2,4),(2,3,4),(3,0,4)],steel,col,'left' if s<0 else 'right')
        if 'unarmed' not in name and 'wand' not in name and 'mace' not in name:
            for s in [-1,1]:
                o=rod('Sword engraved guard',(-2.34,-.26,1.45),(-2.34+s*.36,-.26,1.56),.043,gold,col,end=.018,part='right')
                bpy.context.view_layer.update(); o.matrix_world=turn@o.matrix_world
            o=orb('Sword inset garnet',(-2.34,-.31,1.46),(.055,.025,.07),red,col,'right')
            bpy.context.view_layer.update(); o.matrix_world=turn@o.matrix_world
        for s in [-1,1]:
            for i in range(3):
                o=rod('Collar fluting',(-3+s*(.13+i*.105),-.3,2.4),(-3+s*(.18+i*.11),-.31,2.14),.018,gold,col,end=.012)
                bpy.context.view_layer.update(); o.matrix_world=turn@o.matrix_world
    if name in ['Demon','Skeleton','Lich']:
        angle=-45 if name in ['Demon','Skeleton'] else -30
        turn=Matrix.Translation((3,0,0))@Matrix.Rotation(math.radians(angle),4,'Z')@Matrix.Translation((-3,0,0))
        for s in [-1,1]:
            for i in range(4):
                o=rod('Serrated scapula',(3+s*(.42+i*.12),.06,2.55),(3+s*(.51+i*.16),.1,2.86+(.3 if i==2 else 0)),.10,bone,col,part='torso')
                bpy.context.view_layer.update(); o.matrix_world=turn@o.matrix_world
        if name=='Demon':
            for s in [-1,1]:
                points=[(3+s*.38,.25,2.58),(3+s*.82,.4,3.32),(3+s*1.53,.54,3.63),(3+s*1.38,.62,2.83),(3+s*1.62,.62,2.27),(3+s*1.02,.50,2.54),(3+s*.84,.42,1.94),(3+s*.50,.30,2.24)]
                mesh('Torn demon wing membrane',[turn@Vector(p) for p in points],[(0,1,2),(0,2,3),(0,3,4),(0,4,5),(0,5,6),(0,6,7)],red,col,'torso')
                for i in [1,2,4,6]:
                    o=rod('Demon wing strut',points[0],points[i],.065,bone,col,end=.018)
                    bpy.context.view_layer.update(); o.matrix_world=turn@o.matrix_world
                for i in range(4):
                    o=orb('Demon spinal armor',(3+s*.25,-.11,1.4+i*.25),(.28,.25,.13),bone,col)
                    bpy.context.view_layer.update(); o.matrix_world=turn@o.matrix_world
    audit[name]={'originalMeshes':len(originals),'detailedMeshes':sum(o.type=='MESH' for o in col.objects)}
    print('REFINED',name,audit[name],flush=True)

arena=bpy.data.collections['Arena']; portal=bpy.data.collections['Portal']
# Broken rear architecture stays outside the actors' established battle footprint.
for side in [-1,1]:
    for column in range(3):
        x=side*(2.75+column*1.05); y=3.4+column*.12; height=4.4-column*.55
        for z in [i*.46+.18 for i in range(int(height/.46))]:
            o=cube('Ruined ashlar buttress',(x+rng.uniform(-.035,.035),y,z),(.56,.68,.43),stone,arena)
            o.rotation_euler.z=rng.uniform(-.025,.025)
        cube('Carved column plinth',(x,y,.03),(.88,.94,.24),stone,arena)
        for dx in [-.20,.20]: rod('Buttress vertical moulding',(x+dx,y-.37,.3),(x+dx,y-.37,height-.35),.045,stone,arena,end=.045)
    # Folded hanging standard, with a visibly broken lower edge.
    x=side*3.55; verts=[]; faces=[]
    for j in range(9):
        for i in range(7):
            verts.append((x+(i-3)*.13,3.05+math.sin(i*1.6)*.065,4.25-j*.35-(.16*(i%3) if j==8 else 0)))
    for j in range(8):
        for i in range(6):
            k=j*7+i; faces.append((k,k+1,k+8,k+7))
    mesh('Tattered cathedral standard',verts,faces,red,arena)
    rod('Banner pole',(x-.55,3.08,4.33),(x+.55,3.08,4.33),.045,gold,arena,end=.045)
    for i in range(18):
        o=orb('Fallen masonry',(side*rng.uniform(4.3,5.6),rng.uniform(-2.6,3),.09),(.16+rng.random()*.18,.14+rng.random()*.2,.10+rng.random()*.14),stone,arena)
        o.rotation_euler=(rng.random(),rng.random(),rng.random())
# Additional receding stairs and a monumental skull crown.
for i in range(3): cube('Worn sanctuary stair',(0,1.3+i*.26,-.035+i*.07),(4.65-i*.28,.50,.17),stone,portal)
orb('Portal skull brow',(0,2.16,4.4),(.83,.40,.52),bone,portal)
for side in [-1,1]:
    orb('Deep portal eye socket',(side*.31,1.79,4.45),(.21,.065,.16),ink,portal)
    rod('Skull crown horn',(side*.55,2.24,4.55),(side*.87,2.3,4.98),.23,stone,portal)
    rod('Angled skull brow',(side*.08,1.73,4.62),(side*.56,1.82,4.50),.12,bone,portal,end=.16)
    rod('Skull cheek buttress',(side*.62,1.95,4.4),(side*.46,1.82,4.08),.15,bone,portal,end=.075)
    for i in range(4):
        x=side*(.17+i*.19)
        rod('Skull crown tooth',(x,1.88,4.13),(x*.96,1.85,3.8-(.20 if i==2 else 0)),.095,bone,portal)
    for i in range(6):
        x=side*(1.64+rng.uniform(-.09,.09)); z=.5+i*.55
        orb('Gate chipped relief',(x,2.03,z),(.24,.15,.24),stone,portal)
rod('Skull nasal ridge',(0,1.76,4.36),(0,1.71,4.14),.12,ink,portal)
# Hairline fissures across the floor are geometry, visible in GLB as well as renders.
for i in range(48):
    x=rng.uniform(-4.9,4.9); y=rng.uniform(-2.4,1.1)
    rod('Floor fissure',(x,y,.045),(x+rng.uniform(-.28,.28),y+rng.uniform(.12,.4),.047),.009,ink,arena,end=.005)

for obj in bpy.data.objects:
    if obj.type=='LIGHT':
        if 'Acid' in obj.name: obj.data.color=(1,.64,.32); obj.data.energy=850
        elif 'Bone' in obj.name: obj.data.color=(.86,.90,1); obj.data.energy=850
        elif 'Crimson' in obj.name: obj.data.color=(1,.015,.035); obj.data.energy=180
scene.view_settings.view_transform='AgX'
scene.view_settings.look='AgX - Medium High Contrast'
scene.view_settings.exposure=-.25

def visible(names):
    for col in bpy.data.collections:
        col.hide_render=col.name not in names and col.name!='Lighting'
        col.hide_viewport=False

visible(['Arena','Portal','Fighter','Demon'])
scene.render.film_transparent=False
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'detailed-kit.blend'))
(VERIFY/'model-detail-audit.json').write_text(json.dumps(audit,indent=2)+'\n')
scene.render.filepath=str(VERIFY/'gothic-detail-preview.png')
bpy.ops.render.render(write_still=True)
