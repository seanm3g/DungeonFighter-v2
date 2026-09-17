"""Index the reviewed pixel-art atlases without modifying their original pixels.

Run with Pillow installed after editing an atlas/layout. Catalog order is preserved;
extra generated variants are skipped explicitly. This writes clipping geometry,
anatomical anchors, JSON and the preview's data script only; it never calls an
image model, modifies image pixels, or changes game catalog data.
"""
import json
from collections import deque
from pathlib import Path
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'Code/UI/Avalonia/Assets/ArtLab'

# (row edges, column edges per row, selected zero-based columns per row)
def edges(count, step, start=0):
    return [round(start + i * step) for i in range(count + 1)]

LAYOUTS = {
    'head': ([0,170,340,510,680,851], [edges(10,161.9)]*5, [list(range(10))]*5),
    'chest': ([0,202,405,598,782,971], [edges(12,134.9)]*5,
              [[0,1,2,3,4,5,7,8,9,11], [0,1,2,3,4,6,7,8,10,11],
               [0,1,2,3,4,5,6,8,10,11], [0,1,2,3,4,5,6,8,9,11], [0,1,2,3,5,6,7,8,9,10]]),
    'legs': ([0,191,371,550,741,971], [edges(11,135.5)]*5,
             [list(range(10)), list(range(10)), [0,1,2,3,4,5,6,7,8,10], list(range(10)), list(range(10))]),
    'feet': ([0,200,385,575,754,971],
             [edges(11,134), [0,150,290,435,584,730,885,1050,1180,1300,1460,1619]] + [edges(10,161.9)]*3,
             [[0,1,2,3,4,5,6,7,9,10], [0,1,2,3,4,5,6,7,9,10]] + [list(range(10))]*3),
    'sword': ([0,179,370,574,762,971],
              [[0,150,300,450,600,755,910,1060,1210,1350,1490,1619],
               [0,157,307,462,605,758,920,1070,1250,1440,1619],
               [0,158,308,464,609,758,920,1070,1250,1440,1619],
               [0,150,305,450,620,760,920,1080,1300,1460,1619],
               [0,150,310,460,615,770,920,1090,1280,1460,1619]],
              [[0,1,2,3,4,5,6,7,8,10]] + [list(range(10))]*4),
    'dagger': ([0,157,321,486,650,813,971], [edges(10,161.9)]*6,
               [list(range(10))]*5 + [[0]]),
    'mace': ([0,182,365,550,748,971], [edges(11,147.18)]*4 + [edges(8,177)],
             [list(range(11)), [0,1,3,4,5,6,7,8,9], [0,1,2,3,4,5,6,7,9,10], [0,1,2,3,4,5,6,7,9,10], list(range(8))]),
    'wand': ([0,163,326,489,655,830,971], [edges(10,156)]*6,
             [list(range(10))]*5 + [[0]])
}

armor = json.loads((ROOT / 'GameData/Armor.json').read_text(encoding='utf-8-sig'))
weapons = json.loads((ROOT / 'GameData/Weapons.json').read_text(encoding='utf-8-sig'))
manifest = []
base = Image.open(ASSETS / 'equipment-pixel-base.png').getchannel('A')

def leg_bounds(y, side):
    lo,hi = (128,198) if side == 0 else (202,272)
    xs = [x for x in range(lo,hi) if base.getpixel((int(x*base.width/400),int(y*base.height/600)))>128]
    return (min(xs)-4,max(xs)+5) if xs else ((132,180) if side==0 else (220,268))

def fitted_parts(sheet, name, box, mask, target):
    """Fit garment landmarks rather than treating apparel as a scaled inventory icon.
    Source is local to the sprite bounds; each target is on the shared doll canvas."""
    w,h=box[2:]; parts=[]
    def part(source,destination,layer="front"):
        parts.append(dict(Layer=layer,Source=[round(v,4) for v in source],Target=[round(v,4) for v in destination]))
    if sheet == 'head' and name in ('veil','headdress'):
        # One continuous drape behind the skull and shoulders. Only the brow
        # ornament and outside fabric edges cross in front; never the face.
        part([0,0,w,h],[140,24,120,156],"rear")
        part([0,0,w,.32*h],[148,24,104,38])
        part([0,.32*h,.18*w,.68*h],[140,62,20,118])
        part([.82*w,.32*h,.18*w,.68*h],[240,62,20,118])
    elif sheet == 'chest':
        coat = any(word in name for word in ('coat','jacket','shirt','jerkin','wrap','vest','tabard','surcoat','mantle'))
        width=184 if coat else 200
        # Neck, shoulder seam, belt, hem. The old uniform stretch put the belt
        # above the waist and left bare shoulders protruding above sleeves.
        src=[0,.20*h,.72*h,h]; dst=[108,144,272,320 if coat else 304]
        for i in range(3): part([0,src[i],w,src[i+1]-src[i]],[(400-width)/2,dst[i],width,dst[i+1]-dst[i]])
    elif sheet == 'legs':
        if 'tasset' in name:
            part([0,0,w,h],[132,272,136,120])
            return parts
        if 'breeches' in name:
            part([0,0,w,h],[132,272,136,140])
            return parts
        part([0,0,w,.5*h],[132,272,136,112])
        # Imported greaves include drawn feet: stop at the ankle so they do not
        # produce a second pair of shoes above the real boots.
        for side in range(2):
            for y in range(384,520,4):
                sy=(.5+(y-384)/136*.39)*h
                sh=.39*h*4/136
                dl,dr=leg_bounds(y+2,side)
                center=(dl+dr)/2
                # Keep a constant shin width and source region; only follow the
                # leg center. Rescaling each silhouette row distorts metal plates.
                part([0 if side==0 else .64*w,sy,.36*w,sh],[center-26,y,52,4])
        # Only actual center panels get a tabard layer. Copying trouser inner
        # seams into this area creates an unwanted third strip between the legs.
        panel=mask.crop((int(.47*w),int(.55*h),int(.53*w),int(.82*h)))
        if any(word in name for word in ('greave','plate','stomper')) and sum(v>0 for v in panel.get_flattened_data()) > panel.width*panel.height*.3:
            part([.36*w,.5*h,.28*w,.39*h],[180.96,384,38.08,87.36])
    elif sheet == 'feet':
        for side in range(2):
            bounds=mask.crop((int(side*w/2),0,int((side+1)*w/2),h)).getbbox()
            if not bounds: continue
            sx=int(side*w/2)+bounds[0]; sy=bounds[1]; sw=bounds[2]-bounds[0]; sh=bounds[3]-bounds[1]
            dw=56; dh=min(132,round(sh/sw*dw/4)*4)
            if name in ('sandals','shoes','sneakers','slippers','courtly shoes','brogues','moccassins'): dh=min(dh,44)
            # Follow the shin above the ankle, transitioning to the foot center.
            for offset in range(0,dh,4):
                height=min(4,dh-offset); y=564-dh+offset
                dl,dr=leg_bounds(min(y+height/2,519),side)
                blend=max(0,min(1,(536-y)/20))
                center=(140 if side==0 else 264)*(1-blend)+(dl+dr)/2*blend
                part([sx,sy+sh*offset/dh,sw,sh*height/dh],[center-dw/2,y,dw,height])
    else:
        part([0,0,w,h],target)
    return parts

OPEN_HEADS = {'cowl','hood','chainhood','ironhood','linked hood','plated hood','warhelf'}

def coverage(image, name, sheet, initial=None, openings=True):
    """Build clipping geometry, never rewrite artwork. Flood only the connected
    neutral checker backdrop; isolated silver highlights remain intact."""
    w, h = image.size
    if initial is not None and not (sheet=='chest' or (sheet=='legs' and name.lower()=='stompers') or (sheet=='head' and name.lower() in OPEN_HEADS)):
        return initial
    rgb = list(image.convert('RGB').get_flattened_data())
    background = bytearray(0 if v else 1 for v in initial.get_flattened_data()) if initial is not None else bytearray(w*h)
    # Weapon sheets use a darker checker than the armor sheets. Both checker
    # tones must be traversable or the pale squares remain disconnected islands.
    floor = 110 if sheet in ('sword','dagger','mace','wand') else 130
    eligible = bytearray(min(p)>floor and max(p)-min(p)<38 for p in rgb)
    seal_outline = initial is None and sheet in ('sword','dagger','mace','wand')
    if seal_outline:
        # Seal single-pixel antialiased breaks in dark weapon outlines before
        # tracing the backdrop. This keeps neutral silver blade interiors solid.
        gate=Image.frombytes('L',(w,h),bytes(255 if p else 0 for p in eligible)).filter(ImageFilter.MinFilter(3))
        eligible=bytearray(v>0 for v in gate.get_flattened_data())
    seeds=[]
    if initial is None:
        seeds = [x for x in range(w)] + [(h-1)*w+x for x in range(w)]
        seeds += [y*w for y in range(h)] + [y*w+w-1 for y in range(h)]
    if openings and sheet == 'head' and name.lower() in OPEN_HEADS:
        seeds.append(round(h*.6)*w+round(w*.5))
    if openings and sheet == 'legs' and name.lower()=='stompers':
        seeds.extend(y*w+x for y in range(round(h*.55),round(h*.9))
                     for x in range(round(w*.4),round(w*.6)))
    if openings and sheet == 'chest':
        # Collar heights differ by row. Seed only the narrow center of the neck
        # area, above the breastplate/shirt: never flood silver chest highlights.
        for y in range(round(h*.08),round(h*.21)):
            seeds.append(y*w+round(w*.5))
    queue = deque(s for s in seeds if eligible[s])
    for s in queue:
        background[s]=1
    while queue:
        p=queue.popleft(); x,y=p%w,p//w
        for xx,yy in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
            if 0<=xx<w and 0<=yy<h:
                n=yy*w+xx
                if eligible[n] and not background[n]:
                    background[n]=1; queue.append(n)
    mask=Image.frombytes('L',(w,h),bytes(0 if b else 255 for b in background))
    if seal_outline: mask=mask.filter(ImageFilter.MinFilter(3))
    return mask

def runs_for(mask):
    """Coalesce identical scanline spans into clip rectangles."""
    w,h=mask.size; pixels=mask.tobytes(); result=[]; active={}
    for y in range(h):
        spans=[]; x=0
        while x<w:
            if not pixels[y*w+x]: x+=1; continue
            start=x
            while x<w and pixels[y*w+x]: x+=1
            spans.append((start,x-start))
        next_active={}
        for span in spans:
            if span in active:
                rect=active.pop(span); rect[3]+=1
            else: rect=[span[0],y,span[1],1]
            next_active[span]=rect
        result.extend(active.values()); active=next_active
    result.extend(active.values())
    return result

def substantial_bounds(mask):
    """Ignore tiny neighboring fragments, retaining paired boots and detached details."""
    w, h = mask.size
    pixels = bytearray(mask.tobytes())
    components = []
    for seed in range(len(pixels)):
        if not pixels[seed]:
            continue
        queue = deque([seed])
        pixels[seed] = 0
        count = 0
        members = []
        left = right = seed % w
        top = bottom = seed // w
        while queue:
            p = queue.popleft()
            members.append(p)
            x, y = p % w, p // w
            count += 1
            left, right, top, bottom = min(left,x), max(right,x), min(top,y), max(bottom,y)
            for yy in range(max(0,y-1),min(h,y+2)):
                for xx in range(max(0,x-1),min(w,x+2)):
                    n = yy*w+xx
                    if pixels[n]:
                        pixels[n] = 0
                        queue.append(n)
        components.append((count,left,top,right+1,bottom+1,members))
    if not components:
        return None
    biggest = max(c[0] for c in components)
    kept = [c for c in components if c[0] >= biggest * .08]
    # Bounds alone do not remove neighboring fragments inside the retained box.
    clean=bytearray(w*h)
    for c in kept:
        for p in c[5]: clean[p]=255
    mask.frombytes(bytes(clean))
    return min(c[1] for c in kept),min(c[2] for c in kept),max(c[3] for c in kept),max(c[4] for c in kept)

for sheet, (ys, xs, selections) in LAYOUTS.items():
    items = [i for i in armor if i['slot'] == sheet] if sheet in ('head','chest','legs','feet') else [i for i in weapons if i['type'].lower() == sheet]
    im = Image.open(ASSETS / f'equipment-pixel-{sheet}.png')
    # Isolate the backdrop before slicing cells. Otherwise a cell boundary
    # crossing a blade becomes a flood-fill seed that erases the blade itself.
    atlas_mask = coverage(im,'',sheet,openings=False)
    boxes = []
    masks = []
    for row, columns in enumerate(selections):
        for col in columns:
            # Dagger source has unwanted thin cell rules; exclude their boundary pixels.
            inset = 4 if sheet == 'dagger' else 0
            cell = (max(0,xs[row][col]+inset), ys[row]+inset,
                    min(im.width,xs[row][col+1]-inset), min(im.height,ys[row+1]-inset))
            item = items[len(boxes)]
            mask = coverage(im.crop(cell),item['name'],sheet,initial=atlas_mask.crop(cell))
            bbox = substantial_bounds(mask)
            assert bbox, f'{sheet}: empty cell {row},{col}'
            # The atlas has a pale antialiased matte outside its dark outline.
            # Inset the clipping mesh one source texel, leaving pixels untouched.
            mask = mask.filter(ImageFilter.MinFilter(3))
            boxes.append([cell[0]+bbox[0],cell[1]+bbox[1],bbox[2]-bbox[0],bbox[3]-bbox[1]])
            fitted_mask=mask.crop(bbox)
            if sheet=='chest':
                # Collar openings must be seeded relative to the garment, not
                # the atlas cell whose padding varies from item to item.
                fitted_mask=coverage(im.crop((cell[0]+bbox[0],cell[1]+bbox[1],cell[0]+bbox[2],cell[1]+bbox[3])),item['name'],sheet,initial=fitted_mask)
            masks.append(fitted_mask)
    assert len(items) == len(boxes), (sheet,len(items),len(boxes))
    for index, (item, box) in enumerate(zip(items, boxes)):
        target = {'head':[152,24,96,108], 'chest':[108,116,184,184],
                  'legs':[124,276,152,240], 'feet':[112,464,180,100],
                  'sword':[252,260,72,180], 'mace':[252,160,72,180],
                  'dagger':[264,308,48,96], 'wand':[252,282,72,72]}[sheet]
        name = item['name'].lower()
        if sheet == 'head':
            if name in ('band','circlet'):
                target = [156,48,88,24]
            elif name in ('cap','skullcap','visored cap','war cap'):
                target = [152,20,96,48]
            elif name == 'hat':
                target = [136,4,128,64]
            elif 'crown' in name:
                target = [148,4,104,68]
            elif name in ('neckguard','throat guard'):
                target = [156,104,88,40]
            elif name in OPEN_HEADS:
                target = [148,20,104,108]
        elif sheet == 'feet' and name in ('sandals','shoes','sneakers','slippers','courtly shoes','brogues','moccassins'):
            target = [112,528,180,36]
        grip = []
        if sheet in ('sword','mace','dagger','wand'):
            down_swords = {'broadsword','longsword','shortsword','bastard sword','claymore','greatsword','destroyer','crystal stiletto','dominion','doomstrike','eternal blade','fatebringer','ghost sword','oblivion sword','soulreaver','twinblade','worldcleave'}
            grip = [.5, .17 if sheet=='dagger' or (sheet=='sword' and name in down_swords) else .88 if sheet in ('mace','sword') else .5]
            if sheet=='wand' and name in ('staff','rod','stick','sanctum rod','prong'): grip[1]=.8
            gy=min(box[3]-1,round(grip[1]*box[3]))
            handle=[x for x in range(box[2]) if masks[index].getpixel((x,gy))]
            if handle: grip[0]=(min(handle)+max(handle)+1)/2/box[2]
            k=min(target[2]/box[2],target[3]/box[3]); width=round(box[2]*k/4)*4; height=round(box[3]*k/4)*4
            target=[round((288-grip[0]*width)/4)*4, round((316-grip[1]*height)/4)*4,width,height]
        manifest.append(dict(Sheet=sheet,Name=item['name'],Tier=item['tier'],CatalogIndex=index,
            Source=box,Target=target,Coverage=runs_for(masks[index]),
            HideHair=sheet=='head' and name not in ('band','circlet','neckguard','throat guard','veil','headdress'),
            SplitFeet=sheet=='feet',Grip=grip,
            Parts=fitted_parts(sheet,name,box,masks[index],target)))
out = json.dumps(manifest, indent=2)
(ASSETS / 'equipment-sprites.json').write_text(out,encoding='utf-8')
preview = ROOT / 'Documentation/ArtLab/equipment-sprites-data.js'
preview.write_text('window.EQUIPMENT_SPRITES = '+out+';\n',encoding='utf-8')
print(f'Indexed {len(manifest)} pixel sprites with silhouette coverage and anatomical anchors.')
