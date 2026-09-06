"""Build reusable low-poly enemy families; exact roster coverage is checked before export."""
import bpy, json, math
from pathlib import Path
from mathutils import Vector, Matrix

HERE=Path(__file__).resolve().parent
OUT=HERE.parents[2]/'GameData/Visuals/DemonFighter'
bpy.ops.wm.open_mainfile(filepath=str(HERE/'enemy-kit.blend'))
bpy.context.preferences.filepaths.save_version=0
groups={
 'goblin':'aaaSTARTER|Goblin|Overflow Imp',
 'spider':'Spider|Amber Trapper|Living Wall Crawler',
 'wolf':'Wolf|Territorial Hound|Alpha Wolf|Vigil Alpha|Hedge Hound|Maze Runner|Valley Prowler|Mirage Hunter',
 'firehound':'Cinder Hound|Magma Beast|Tribute Beast',
 'flame':'Fire Elemental|Ember Spirit|Reignited Wisp|Flame Drifter|Island Drifter',
 'lavagolem':'Lava Golem|Soot Golem|Throne Effigy',
 'golem':'Glass Shard Golem|Stone Colossus|Summit Titan|Rock Lobber|Amber Sentinel',
 'bat':'Bat', 'skeleton':'Skeleton',
 'zombie':'Zombie|Ghoul|Tomb Creeper|Mummy|Sarcopha Gus|Toxic Drinker|Cave Lurcher',
 'wraith':"Wraith|Wight|Shadow Wraith|Ancestor's Shade|Deserter Shade|Altitude Wraith|Torch Wraith|Loop Shade|Shattered Reflection|Pollen Wraith|Ruin Wraith|Current Rider|Hollow Drifter|Shelf Specter|Mist Phantom|Undertow Shade|Smog Lurker",
 'bear':'Bear|Cursed Bear', 'treant':'Treant|Scorched Treant', 'boar':'Boar|Thorn Boar',
 'lizard':'Salamander|Magma Creeper|Lava Spitter|Mud Lurker|Cold Creeper|Frost Creeper|Sinkhole Lurker',
 'lich':'Lich|Burial Lich|Bone Spreader|Throne Revenant|Depth Drainer',
 'hunter':'Sandstorm Flanker|Glare Stalker|Edge Stalker|Charwood Stalker|Echo Stalker|Reed Stalker|Ink Stalker',
 'snake':'Sand Serpent|Rattlesnake|Slick Eel',
 'beetle':'Dune Roller|Guardian Scarab|Glow Mite',
 'scorpion':'Sun Lance Scorpion',
 'leech':'Restorative Leech|Blood Feeder|Crystal Leech',
 'knight':'Poison Dart Sentinel|Bridge Warden|Temple Knight|Pilar Watcher|Royal Flame Guard|Mound Warden|Duplicate|The Other|Fourecourt Guard|Tide Sentinel|Aqueduct Warden',
 'mage':'Sun Priest|Storm Caller|Seal Caster|Chronicle Watcher|Pressure Monk|Still Gazer|Cascade Rider',
 'bird':'Hawk|Ashwing Harpy', 'rat':'Scavenger Rat', 'toad':'Cave Ember Toad',
 'spore':'Vent Spore', 'tortoise':'Slag Tortoise', 'crab':'Molten Crab',
 'sprite':'Moon Blessed|Guile Sprite|Root Sprite|Vent Sprite',
 'mimic':'Dead End Mimic|Fog Mimic|Instinct Eater',
 'swarm':'Nectar Drone|Mosquito Swarm', 'vine':'Animated Vine', 'fish':'Lion Fish'
}
catalog={name:family for family,names in groups.items() for name in names.split('|')}
roster=json.loads((HERE.parents[2]/'GameData/Enemies.json').read_text(encoding='utf-8-sig'))
assert set(e['name'] for e in roster)==set(catalog), 'Roster mapping must be exhaustive'
(OUT/'enemies.json').write_text(json.dumps(catalog,indent=2)+'\n')

def material(name,color,glow=0):
 m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*color,1)
 p.inputs['Roughness'].default_value=.8
 p.inputs['Emission Color'].default_value=(*color,1); p.inputs['Emission Strength'].default_value=glow
 return m
ink=bpy.data.materials['Ink / charcoal']; bone=bpy.data.materials['Old bone']
red=bpy.data.materials['Crimson cloth']; eye=bpy.data.materials['Sulfur fire']
green=material('Forest venom',(.24,.34,.065)); blue=material('Drowned spectral teal',(.08,.38,.42),.2)
stone=material('Glacial slate',(.3,.38,.4)); fire=material('Molten crimson',(.8,.012,.055),.5)
wood=material('Dead bark',(.19,.12,.06)); gold=material('Tomb ochre',(.5,.34,.075))

def finish(o,name,mat,part='torso'):
 o.name=name; o['rig_part']=part
 for old in list(o.users_collection): old.objects.unlink(o)
 c.objects.link(o); o.data.materials.append(mat)
 return o
def orb(name,p,s,mat,part='torso'):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=(3+p[0],p[1],p[2]))
 o=finish(bpy.context.object,name,mat,part); o.scale=s; return o
def rod(name,a,b,r,mat,part='torso',end=.015):
 a=Vector((3+a[0],a[1],a[2])); b=Vector((3+b[0],b[1],b[2])); d=b-a
 bpy.ops.mesh.primitive_cone_add(vertices=6,radius1=r,radius2=end,depth=d.length,location=(a+b)/2)
 o=finish(bpy.context.object,name,mat,part); o.rotation_euler=d.to_track_quat('Z','Y').to_euler(); return o
def eyes(x,y,z,spread=.18):
 for s in [-1,1]: orb('Eye',(x+s*spread,y,z),(.08,.05,.07),eye,'head')
def legs(n,width,z,mat):
 for i in range(n):
  s=-1 if i%2==0 else 1; y=(i//2-(n/2-1)/2)*.55
  part='left' if s<0 else 'right'
  rod('Leg',(s*width*.5,y,z),(s*width,y-.12,.5),.13,mat,part)
  rod('Foot',(s*width,y-.12,.5),(s*width*1.25,y-.35,.15),.1,mat,part)

for family in groups:
 if family in ['bat','skeleton']: continue
 c=bpy.data.collections.new(family.title()); bpy.context.scene.collection.children.link(c)
 c['family']=family
 mat=green
 if family in ['firehound','lavagolem','flame','crab','lizard']: mat=fire
 if family in ['wraith','sprite','fish','mage']: mat=blue
 if family in ['golem','knight']: mat=stone
 if family in ['bear','boar','rat','treant','vine']: mat=wood
 if family in ['lich','zombie']: mat=bone
 if family in ['beetle','scorpion','tortoise']: mat=gold
 if family in ['wolf','firehound','bear','boar','rat','lizard','toad','tortoise']:
  c['motion']='pounce'; c['head_z']=1.45
  bulky=family in ['bear','boar','toad','tortoise']
  orb('Body',(0,.25,1),(.75 if bulky else .44,1,.75 if family=='bear' else .55),mat)
  orb('Head',(0,-.7,1.3),(.48,.48,.45),mat,'head')
  orb('Muzzle',(0,-1.06,1.16),(.31,.35,.22),ink,'head'); eyes(0,-1.02,1.48,.25)
  for s in [-1,1]:
   for y in [-.4,.85]:
    part='left' if s<0 else 'right'
    orb('Haunch',(s*.46,y,.85),(.25,.3,.4),mat,part)
    rod('Foreleg',(s*.46,y,.85),(s*.5,y-.05,.22),.16,mat,part,end=.12)
    orb('Paw',(s*.5,y-.18,.16),(.23,.33,.15),ink,part)
  if family in ['wolf','firehound','rat']:
   for s in [-1,1]: rod('Ear',(s*.3,-.64,1.55),(s*.38,-.56,2.1),.2,mat,'head')
  if family in ['boar','bear']:
   for s in [-1,1]: rod('Tusk',(s*.25,-1.17,1),(s*.38,-1.22,1.6),.13,bone,'head')
  if family=='bear':
   for s in [-1,1]: orb('Round ear',(s*.32,-.65,1.67),(.18,.13,.2),mat,'head')
  if family=='boar':
   for i in range(6): rod('Razor bristle',(0,-.1+i*.22,1.4),(0,-.05+i*.22,1.95),.12,ink)
  if family=='lizard':
   for i in range(7): rod('Dorsal spine',(0,-.1+i*.22,1.35),(0,-.05+i*.22,1.85),.11,bone)
  if family=='toad':
   for s in [-1,1]: orb('Hind thigh',(s*.6,.7,.55),(.42,.5,.4),mat,'left' if s<0 else 'right')
  if family=='tortoise': orb('Armored shell',(0,.35,1.3),(.86,1.05,.7),ink)
  if family not in ['bear','boar','toad','tortoise']: rod('Tail',(0,1,1),(0,2.25,.8),.22,mat,'torso')
 elif family in ['spider','scorpion','beetle','crab']:
  c['motion']='scuttle'; c['head_z']=1.1
  orb('Carapace',(0,.2,1),(.7,.85,.55),mat); orb('Head',(0,-.65,1),(.4,.4,.35),ink,'head')
  eyes(0,-.99,1.15); legs(8,1.1,.95,mat)
  for s in [-1,1]: rod('Mandible',(s*.22,-.8,.9),(s*.33,-1.3,.75),.12,bone,'head')
  if family in ['scorpion','crab']:
   for s in [-1,1]:
    orb('Pincer',(s*1,-.9,1.1),(.4,.45,.24),mat,'left' if s<0 else 'right')
    rod('Pincer tip',(s*1.2,-1,1.1),(s*.95,-1.6,1.1),.17,bone,'left' if s<0 else 'right')
  if family=='scorpion':
   rod('Tail base',(0,.8,1),(0,1.3,2),.25,mat)
   rod('Tail arch',(0,1.3,2),(0,.4,2.65),.2,mat)
   rod('Stinger',(0,.4,2.65),(0,-.2,2.15),.17,bone,'head')
 elif family in ['snake','leech','vine']:
  c['motion']='serpent'; c['head_z']=2
  for i in range(14):
   t=i/13; orb('Coiled segment',(math.sin(t*math.tau)*(.2 if family=='leech' else .65),math.cos(t*math.tau)*.5, .25+t*1.8),(.37 if family=='leech' else .27,.28,.3),mat,'torso' if i<9 else 'head')
  orb('Head',(0,.5,2.2),(.4,.36,.27),mat,'head'); eyes(0,.18,2.25)
  for s in [-1,1]: rod('Fang',(s*.18,.15,2.1),(s*.2,.1,1.7),.08,bone,'head')
  if family=='vine':
   for i in range(6): rod('Leaf',(0,0,.45+i*.27),((-1 if i%2 else 1)*.8,-.2,.9+i*.27),.22,green)
 elif family in ['wraith','flame','sprite','spore']:
  c['motion']='float'; c['head_z']=2.3
  orb('Spirit core',(0,0,2),(.52,.4,.7),mat)
  orb('Head',(0,-.05,2.65),(.37,.3,.4),ink,'head'); eyes(0,-.34,2.68)
  for i in range(7):
   a=i*math.tau/7
   rod('Trailing mote',(math.cos(a)*.42,math.sin(a)*.3,1.8),(math.cos(a)*.85,math.sin(a)*.6,.6+(i%2)*.35),.19,mat)
  if family=='spore': orb('Fungal cap',(0,0,2.9),(1,.7,.4),red,'head')
  for s in [-1,1]: rod('Spirit tendril',(s*.35,0,2.3),(s*.95,-.2,2.8),.22,mat,'left' if s<0 else 'right')
 elif family=='fish':
  c['motion']='fly'; c['head_z']=2
  orb('Fish body',(0,0,2),(.4,.95,.5),mat)
  orb('Fish head',(0,-.65,2),(.42,.4,.4),mat,'head'); eyes(0,-.96,2.15,.25)
  for s in [-1,1]:
   rod('Tail fin',(0,.7,2),(s*.65,1.5,2),.24,red,'left' if s<0 else 'right')
   for i in range(5): rod('Pectoral spine',(s*.25,-.05,2),(s*(.95-i*.1),.1+i*.17,1.6),.09,bone,'left' if s<0 else 'right')
  for i in range(7): rod('Dorsal spine',(0,-.4+i*.17,2.2),(0,-.5+i*.22,2.95),.08,bone)
 elif family in ['bird','swarm']:
  c['motion']='fly'; c['head_z']=2.3
  orb('Body',(0,0,2),(.35,.5,.45),mat); orb('Head',(0,-.4,2.4),(.3,.3,.3),mat,'head'); eyes(0,-.68,2.45)
  rod('Beak',(0,-.5,2.3),(0,-1.05,2.28),.17,bone,'head')
  for s in [-1,1]:
   for i in range(5): rod('Wing feather',(s*.23,0,2.15),(s*(1.5-i*.16),.15+i*.18,2.6-i*.17),.18,bone if family=='bird' else mat,'left' if s<0 else 'right')
  if family=='swarm':
   for i in range(9): orb('Swarm mote',(math.sin(i*2.4)*1.1,math.cos(i)*.7,1.3+(i%3)*.5),(.13,.18,.12),gold,'left' if i%2 else 'right')
 elif family=='mimic':
  c['motion']='pounce'; c['head_z']=1.8
  orb('Chest body',(0,0,.85),(.9,.6,.65),wood)
  orb('Open lid',(0,.28,1.9),(.95,.35,.38),wood,'head')
  orb('Maw',(0,-.44,1.4),(.75,.12,.52),ink,'head')
  for i in range(7):
   for z in [.95,1.9]: rod('Tooth',(-.6+i*.2,-.6,z),(-.6+i*.2,-.65,1.4),.085,bone,'head')
  eyes(0,-.1,2.1,.45)
 else:
  c['motion']='heavy' if family in ['golem','lavagolem','treant'] else 'humanoid'
  c['head_z']=2.5
  heavy=c['motion']=='heavy'; w=.8 if heavy else .48
  orb('Torso',(0,0,1.9),(w,.42,.7),mat)
  orb('Head',(0,-.04,2.9),(.38,.34,.45),mat,'head'); eyes(0,-.35,2.94)
  for s in [-1,1]:
   part='left' if s<0 else 'right'
   orb('Shoulder',(s*w,0,2.35),(.35,.35,.38),mat,part)
   rod('Arm',(s*w,0,2.3),(s*(w+.15),-.2,1.35),.25,mat,part,end=.13)
   orb('Hand',(s*(w+.15),-.2,1.3),(.23,.23,.28),mat,part)
   rod('Leg',(s*.3,0,1.45),(s*.4,0,.25),.23,mat,'legL' if s<0 else 'legR',end=.15)
   orb('Boot',(s*.4,-.15,.2),(.24,.4,.2),ink,'legL' if s<0 else 'legR')
  if family in ['mage','lich']:
   rod('Robe',(0,0,.15),(0,0,2.4),.8,red,end=.35)
   rod('Staff',(.85,-.2,.1),(.85,-.2,3.6),.08,bone,'right')
   orb('Staff jewel',(.85,-.2,3.6),(.22,.22,.3),eye,'right')
  if family in ['knight','hunter','goblin']:
   rod('Blade',(.7,-.2,1.35),(.8,-.3,3.1),.16,bone,'right')
   if family=='knight': orb('Shield',(-.7,-.48,1.75),(.45,.13,.65),red,'left')
  if family in ['treant','goblin','lich']:
   for s in [-1,1]:
    rod('Crown branch',(s*.25,0,3.1),(s*.8,.05,3.8),.16,wood if family=='treant' else bone,'head')
   if family=='treant':
    for s in [-1,1]:
     rod('Forked bough',(s*.55,0,3.4),(s*1.1,.15,3.6),.13,wood,'head')
     rod('Branch hand',(s*.85,0,1.5),(s*1.4,0,2.6),.16,wood,'left' if s<0 else 'right')
     for i in range(3): rod('Root',(s*.25,0,.4),(s*(.6+i*.2),-.4+i*.4,.08),.16,wood,'root')
  if family=='zombie': orb('Exposed wound',(.22,-.4,1.9),(.25,.1,.35),red)
 bpy.context.view_layer.update()
 angle=-65 if c.get('motion') in ['pounce','scuttle'] or family=='fish' else -30
 turn=Matrix.Translation((3,0,0)) @ Matrix.Rotation(math.radians(angle),4,'Z') @ Matrix.Translation((-3,0,0))
 for obj in c.objects: obj.matrix_world=turn @ obj.matrix_world

scene=bpy.context.scene; scene.cycles.samples=8; scene.render.film_transparent=True
for c0 in bpy.data.collections:
 if c0.name!='Lighting': c0.hide_render=True
for family in groups:
 if family in ['bat','skeleton']: continue
 c=bpy.data.collections[family.title()]; c.hide_render=False
 scene.render.filepath=str(OUT/(family+'-layer.png')); bpy.ops.render.render(write_still=True)
 bpy.ops.object.select_all(action='DESELECT')
 for o in c.objects: o.select_set(True)
 bpy.ops.export_scene.gltf(filepath=str(OUT/(family+'.glb')),export_format='GLB',use_selection=True)
 c.hide_render=True
 print('FAMILY COMPLETE',family,flush=True)
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'roster-kit.blend'))
print('ROSTER COMPLETE',len(catalog),len(groups),flush=True)
