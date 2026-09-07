"""Rigid-bone prototype rigs and cropped animation atlases. Blender 5.1.
Rebuild base/enemy kits first. Every clip has fixed trim bounds and timing metadata.
"""
import bpy, math, json, sys, os
from pathlib import Path
from array import array
from mathutils import Vector, Matrix
from bpy_extras.object_utils import world_to_camera_view

SOURCE=Path(__file__).resolve().parent
CACHE=SOURCE.parent/'Verification/render-cache'
CACHE.mkdir(parents=True,exist_ok=True)
os.environ['OPTIX_CACHE_PATH']=str(CACHE)
OUT=SOURCE.parents[2]/'GameData/Visuals/DemonFighter/Animations'
OUT.mkdir(parents=True,exist_ok=True)
base=next(SOURCE/name for name in ['detailed-kit.blend','progression-kit.blend','roster-kit.blend'] if (SOURCE/name).exists())
bpy.ops.wm.open_mainfile(filepath=str(base))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
scene.render.resolution_x=800; scene.render.resolution_y=450
scene.cycles.samples=16
scene.render.use_persistent_data=True
try:
    preferences=bpy.context.preferences.addons['cycles'].preferences
    preferences.compute_device_type='OPTIX'; preferences.get_devices()
    gpu=[d for d in preferences.devices if d.type=='OPTIX']
    if gpu:
        for d in preferences.devices: d.use=d.type=='OPTIX'
        scene.cycles.device='GPU'
except (TypeError,RuntimeError):
    scene.cycles.device='CPU'
scene.render.film_transparent=True
clips={'idle':8,'attack':10,'hit':6,'cast':10,'guard':6,'evade':6,'death':8,'victory':6}
manifest={'version':1,'canvasWidth':800,'canvasHeight':450,'actors':{}}
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
shard=int(args[0]) if args else 0
shards=int(args[1]) if args else 1
resume='resume' in args
only_new='only-new' in args
if only_new: manifest=json.loads((OUT/'clips.json').read_text())
TEMP=SOURCE.parent/'Verification/animation-workers'
if shards>1: TEMP.mkdir(parents=True,exist_ok=True)

def bone(rig,name,head,tail,parent=None):
    b=rig.data.edit_bones.new(name); b.head=head; b.tail=tail
    if parent: b.parent=rig.data.edit_bones[parent]

def pose(rig,actor,clip,t):
    for b in rig.pose.bones:
        b.rotation_mode='XYZ'; b.rotation_euler=(0,0,0); b.location=(0,0,0)
    root=rig.pose.bones['root']; torso=rig.pose.bones['torso']
    head=rig.pose.bones['head']; left=rig.pose.bones['left']; right=rig.pose.bones['right']
    wave=math.sin(t*math.tau)
    motion=bpy.data.collections[actor].get('motion','fly' if actor=='Bat' else 'humanoid')
    facing=1 if actor.startswith('Fighter') else -1
    def curve(keys):
        for (a,x),(b,y) in zip(keys,keys[1:]):
            if t<=b:
                u=max(0,(t-a)/(b-a)); return x+(y-x)*u
        return keys[-1][1]
    drive=curve([(0,0),(1/3,-.35),(5/9,1),(.7,.65),(1,0)])
    lift=math.sin(t*math.pi)
    if clip=='idle':
        root.location.y=(.15 if motion in ['fly','float'] else .035)*wave
        torso.rotation_euler.y=.055*wave
        head.rotation_euler.x=.035*math.sin(t*math.tau+.7)
        if motion=='fly':
            left.rotation_euler.z=.9*wave; right.rotation_euler.z=-.9*wave
    elif clip=='attack':
        root.location.x=facing*(.85 if motion!='heavy' else .45)*drive
        root.location.y=curve([(0,0),(.3,-.13),(.45,.2),(.6,0),(1,0)])
        torso.rotation_euler.z=-facing*.32*drive
        torso.rotation_euler.y=.45*drive
        head.rotation_euler.z=facing*.15*drive
        right.rotation_euler.x=-1.9*drive; left.rotation_euler.x=-.8*drive
        if motion in ['humanoid','heavy']:
            right.rotation_euler.z=.9*drive
            left.rotation_euler.z=-.25*drive
        rig.pose.bones['legL'].rotation_euler.x=.5*drive
        rig.pose.bones['legR'].rotation_euler.x=-.4*drive
        if motion in ['fly','float']:
            root.location.y=.35*lift; root.rotation_euler.z=-facing*.45*drive
            left.rotation_euler.z=1.1*drive; right.rotation_euler.z=-1.1*drive
        elif motion in ['pounce','scuttle','serpent']:
            root.rotation_euler.z=-facing*.22*drive
            left.rotation_euler.x=.65*drive; right.rotation_euler.x=-.65*drive
        elif motion=='heavy':
            left.rotation_euler.x=-2*drive
    elif clip=='hit':
        recoil=curve([(0,.65),(.2,1),(.5,-.2),(1,0)])
        root.location.x=-facing*.35*recoil
        torso.rotation_euler.z=facing*.35*recoil
        head.rotation_euler.z=-facing*.3*recoil
        left.rotation_euler.x=.6*recoil; right.rotation_euler.x=.4*recoil
    elif clip in ['cast','victory']:
        spell=curve([(0,0),(1/3,.65),(5/9,1),(.7,.8),(1,0)]) if clip=='cast' else min(1,t*3)
        left.rotation_euler.x=-2.2*spell; right.rotation_euler.x=-1.7*spell
        torso.rotation_euler.y=.25*spell
        root.location.y=.22*spell; head.rotation_euler.x=-.25*spell
    elif clip=='guard':
        left.rotation_euler.x=-1.3*lift; right.rotation_euler.x=-1*lift
        root.location.y=-.16*lift; root.location.x=-facing*.18*lift
    elif clip=='evade':
        root.rotation_euler.z=facing*.5*lift
        root.location.x=-facing*.8*lift; root.location.y=.2*lift
    elif clip=='death':
        fall=curve([(0,0),(.22,.15),(.65,1),(.8,.92),(1,1)])
        root.rotation_euler.z=-facing*1.5*fall
        root.location.y=-.22*fall
        root.location.x=-facing*.35*fall

actors=['Fighter','Demon','Skeleton','Bat']+[c.name for c in bpy.data.collections if 'family' in c or 'presentation_actor' in c]
if only_new: actors=[a for a in actors if a.startswith('Fighter-')]
for actor in actors[shard::shards]:
    for col in bpy.data.collections:
        if col.name!='Lighting':
            col.hide_render=col.name!=actor
            col.hide_viewport=col.name!=actor
    c=bpy.data.collections[actor]
    objects=[o for o in c.objects if o.type=='MESH']
    x=-3 if actor.startswith('Fighter') else 3
    motion=c.get('motion','fly' if actor=='Bat' else 'humanoid')
    data=bpy.data.armatures.new(actor+' rig')
    rig=bpy.data.objects.new(actor+' rig',data)
    c.objects.link(rig)
    bpy.context.view_layer.objects.active=rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bone(rig,'root',(x,0,0),(x,0,1))
    body_z=1 if motion in ['pounce','scuttle'] else 1.5
    bone(rig,'torso',(x,0,body_z),(x,0,body_z+.9),'root')
    hz=c.get('head_z',2.55)
    neck_y=-.4 if motion in ['pounce','scuttle'] or actor=='Fish' else 0
    bone(rig,'head',(x,neck_y,hz),(x,neck_y,hz+.65),'torso')
    joint_z=1 if c.get('motion') in ['pounce','scuttle'] else 2.35
    bone(rig,'left',(x-.45,0,joint_z),(x-.45,0,joint_z-.75),'torso')
    bone(rig,'right',(x+.45,0,joint_z),(x+.45,0,joint_z-.75),'torso')
    bone(rig,'legL',(x-.3,0,1.4),(x-.3,0,.4),'root')
    bone(rig,'legR',(x+.3,0,1.4),(x+.3,0,.4),'root')
    if actor=='Bat':
        for name,s in [('left',-1),('right',1)]:
            b=rig.data.edit_bones[name]; b.head=(x+s*.18,.03,2.46); b.tail=(x+s*.18,.03,1.71)
    elif actor in ['Bird','Swarm']:
        for name,s in [('left',-1),('right',1)]:
            b=rig.data.edit_bones[name]; b.head=(x+s*.23,0,2.15); b.tail=(x+s*.23,0,1.4)
    elif motion in ['humanoid','heavy']:
        width=.6 if actor.startswith('Fighter') else .68 if actor in ['Demon','Skeleton'] else .8 if motion=='heavy' else .48
        shoulder_z=2.24 if actor.startswith('Fighter') else 2.55 if actor in ['Demon','Skeleton'] else 2.35
        for name,s in [('left',-1),('right',1)]:
            b=rig.data.edit_bones[name]; b.head=(x+s*width,0,shoulder_z); b.tail=(x+s*width,0,shoulder_z-.75)
    angle=55 if actor.startswith('Fighter') else -45 if actor in ['Demon','Skeleton'] else 0 if actor=='Bat' else -65 if motion in ['pounce','scuttle'] or actor=='Fish' else -30
    turn=Matrix.Translation((x,0,0)) @ Matrix.Rotation(math.radians(angle),4,'Z') @ Matrix.Translation((-x,0,0))
    for b in rig.data.edit_bones:
        if b.name!='root': b.transform(turn)
    bpy.ops.object.mode_set(mode='OBJECT')
    for obj in objects:
        name=obj.name.lower()
        center=sum((obj.matrix_world@v.co for v in obj.data.vertices),Vector())/len(obj.data.vertices)
        target='root'
        if 'rig_part' in obj:
            target=obj['rig_part']
            if target in ['left','right'] and (motion=='pounce' or actor=='Fish'): target='torso'
        elif any(n in name for n in ['boot','greave','knee','thigh','hoof','foot','shin','femur','hock']): target='legL' if center.x<x else 'legR'
        elif any(n in name for n in ['helm','visor','skull','eye','socket','horn','nasal','mouth','tooth','head','ear','fang']): target='head'
        elif any(n in name for n in ['arm','gauntlet','glove','sword','wing','knuckle','talon']): target='left' if center.x<x else 'right'
        elif center.z>1.55: target='torso'
        vg=obj.vertex_groups.new(name=target)
        vg.add(list(range(len(obj.data.vertices))),1,'REPLACE')
        mod=obj.modifiers.new('Rigid bone animation','ARMATURE'); mod.object=rig
    actor_meta={}
    for clip,count in clips.items():
        rig.animation_data_clear()
        # Save authored clips as actions, with exact samples used for rendering.
        action=bpy.data.actions.new(actor+'/'+clip); action.use_fake_user=True
        rig.animation_data_create(); rig.animation_data.action=action
        points=[]
        for i in range(count):
            t=i/count if clip=='idle' else i/(count-1)
            scene.frame_set(i+1); pose(rig,actor,clip,t)
            for b in rig.pose.bones:
                b.keyframe_insert('rotation_euler',frame=i+1)
                b.keyframe_insert('location',frame=i+1)
            bpy.context.view_layer.update()
            dg=bpy.context.evaluated_depsgraph_get()
            for obj in objects:
                evaluated=obj.evaluated_get(dg)
                for v in evaluated.data.vertices:
                    p=world_to_camera_view(scene,scene.camera,evaluated.matrix_world@v.co)
                    points.append((p.x*800,(1-p.y)*450))
        x0=max(0,math.floor(min(p[0] for p in points))-6)
        y0=max(0,math.floor(min(p[1] for p in points))-6)
        x1=min(800,math.ceil(max(p[0] for p in points))+6)
        y1=min(450,math.ceil(max(p[1] for p in points))+6)
        w=x1-x0; h=y1-y0
        filename=actor.lower()+'-'+clip+'.png'
        metadata={'file':filename,'frames':count,'frameWidth':w,'frameHeight':h,
            'x':x0,'y':y0,'fps':12,'loop':clip=='idle','impactFrame':count//2 if clip in ['attack','cast'] else None}
        cached=OUT/filename
        if resume and cached.exists() and cached.stat().st_mtime>max(base.stat().st_mtime,Path(__file__).stat().st_mtime):
            im=bpy.data.images.load(str(cached),check_existing=False)
            valid=tuple(im.size)==(w*count,h); bpy.data.images.remove(im)
            if valid:
                actor_meta[clip]=metadata
                print('CLIP RESUMED',actor,clip,flush=True)
                continue
        pixels=array('f',[0])*(w*count*h*4)
        scene.render.use_border=True; scene.render.use_crop_to_border=True
        # Render only the union crop, preserving the original stage camera.
        # Half-pixel nudges avoid floating-point truncation at integer borders.
        scene.render.border_min_x=(x0+.001)/800; scene.render.border_max_x=(x1+.001)/800
        scene.render.border_min_y=(450-y1+.001)/450; scene.render.border_max_y=(450-y0+.001)/450
        for i in range(count):
            scene.frame_set(i+1)
            scene.render.filepath=str(OUT/(actor.lower()+'-'+clip+'-working-'+str(i)+'.png'))
            bpy.ops.render.render(write_still=True)
            im=bpy.data.images.load(scene.render.filepath,check_existing=False)
            assert tuple(im.size)==(w,h),(actor,clip,tuple(im.size),(w,h))
            buf=array('f',[0])*len(im.pixels); im.pixels.foreach_get(buf)
            for row in range(h):
                src=row*w*4
                dst=(row*w*count+i*w)*4
                pixels[dst:dst+w*4]=buf[src:src+w*4]
            bpy.data.images.remove(im)
            Path(scene.render.filepath).unlink(missing_ok=True)
        atlas=bpy.data.images.new(actor+' '+clip,width=w*count,height=h,alpha=True)
        atlas.pixels.foreach_set(pixels)
        filename=actor.lower()+'-'+clip+'.png'
        atlas.save_render(str(OUT/filename),scene=scene)
        bpy.data.images.remove(atlas)
        actor_meta[clip]=metadata
        print('CLIP COMPLETE',actor,clip,flush=True)
    manifest['actors'][actor.lower()]=actor_meta
    rig.animation_data.action=bpy.data.actions[actor+'/idle']
    scene.frame_set(1)
    c.hide_render=True
bpy.data.collections['Fighter'].hide_render=False
bpy.data.collections['Bat'].hide_render=False
bpy.data.collections['Arena'].hide_render=False
bpy.data.collections['Portal'].hide_render=False
for col in bpy.data.collections: col.hide_viewport=False
scene.render.use_border=False; scene.render.use_crop_to_border=False
bpy.ops.wm.save_as_mainfile(filepath=str(TEMP/f'kit-{shard}.blend' if shards>1 else SOURCE/('progression-animated.blend' if only_new else 'animated-kit.blend')))
(TEMP/f'clips-{shard}.json' if shards>1 else OUT/'clips.json').write_text(json.dumps(manifest,indent=2)+'\n')
(OUT/'frame-working.png').unlink(missing_ok=True)
print('ANIMATION EXPORT COMPLETE',flush=True)
