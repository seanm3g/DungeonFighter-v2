const entries = window.EQUIPMENT_SPRITES, assets = '../../Code/UI/Avalonia/Assets/ArtLab/';
const slots = ['head', 'chest', 'legs', 'feet', 'weapon'];
const names = {head:'Head',chest:'Body',legs:'Legs',feet:'Feet',weapon:'Weapon'};
const equipment = {}, images = {};
const slotOf = e => ['head','chest','legs','feet'].includes(e.Sheet) ? e.Sheet : 'weapon';
const hero = document.querySelector('#hero'), ctx = hero.getContext('2d');
// Preserve the authored detail; do not crush the composite to a 100px thumbnail.
hero.width = 400; hero.height = 600;
ctx.imageSmoothingEnabled = false;


function drawSprite(c, e, target, fit = false, wearing = false, layer = "front") {
    let [x,y,w,h] = target;
    const [sx,sy,sw,sh] = e.Source;
    if (fit) { const k = Math.min(w/sw,h/sh); x += (w-sw*k)/2; y += (h-sh*k)/2; w = sw*k; h = sh*k; }
    function part(dx,dy,dw,dh,region=[0,0,sw,sh]) {
        const [px,py,pw,ph]=region;
        c.save(); c.beginPath();
        for (const [rx,ry,rw,rh] of e.Coverage) {
            const l=Math.max(rx,px),t=Math.max(ry,py),r=Math.min(rx+rw,px+pw),b=Math.min(ry+rh,py+ph);
            if(r>l&&b>t)c.rect(dx+(l-px)/pw*dw,dy+(t-py)/ph*dh,(r-l)/pw*dw,(b-t)/ph*dh);
        }
        c.clip(); c.drawImage(images[e.Sheet],sx+px,sy+py,pw,ph,dx,dy,dw,dh); c.restore();
    }
    if (wearing && e.Parts?.length) {
        for(const p of e.Parts)if((p.Layer || "front")===layer)part(...p.Target,p.Source);
    } else part(x,y,w,h);
}

function drawBase() {
    if (equipment.head?.HideHair) {
        ctx.save(); ctx.beginPath(); ctx.rect(0,0,400,600); ctx.rect(136,12,128,104); ctx.clip('evenodd');
        ctx.drawImage(images.base,0,0,400,600); ctx.restore();
        ctx.save(); ctx.beginPath(); ctx.rect(176,64,48,52); ctx.clip();
        ctx.drawImage(images.base,0,0,400,600); ctx.restore();
    } else ctx.drawImage(images.base,0,0,400,600);
}

function render() {
    ctx.clearRect(0,0,400,600);
    for (const slot of slots) if(equipment[slot]) drawSprite(ctx,equipment[slot],equipment[slot].Target,false,true,"rear");
    drawBase();
    // Boot shafts go behind leg armor and trouser cuffs.
    for (const slot of ['feet','legs','chest','head','weapon']) {
        if (equipment[slot]) drawSprite(ctx,equipment[slot],equipment[slot].Target,false,true);
    }
    if (equipment.weapon) {
        ctx.save(); ctx.beginPath(); ctx.rect(280,312,16,12); ctx.clip();
        ctx.drawImage(images.base,0,0,400,600); ctx.restore();
    }
    document.querySelector('#loadout').textContent = slots.map(s=>equipment[s]?.Name).filter(Boolean).join(' · ') || 'Unarmored base character';
    for (const s of slots) document.querySelector('#slot-'+s).value = equipment[s] ? entries.indexOf(equipment[s]) : '';
    document.querySelectorAll('.card').forEach(b=>b.classList.toggle('active',equipment[slotOf(entries[b.dataset.index])]===entries[b.dataset.index]));
}

function catalog() {
    const q = document.querySelector('#search').value.toLowerCase(), root = document.querySelector('#catalog');
    root.replaceChildren();
    entries.forEach((e,i)=>{
        if (!(e.Name+' '+e.Sheet+' tier '+e.Tier).toLowerCase().includes(q)) return;
        const b = document.createElement('button'); b.className='card'; b.dataset.index=i;
        const c = document.createElement('canvas'); c.width=96; c.height=100; c.style.width='96px'; c.style.height='100px';
        const cx = c.getContext('2d'); cx.imageSmoothingEnabled=false;
        drawSprite(cx,e,[4,4,88,92],true);
        b.append(c,document.createTextNode(e.Name));
        const sub = document.createElement('small'); sub.textContent=names[slotOf(e)]+' · Tier '+e.Tier; b.append(sub);
        b.onclick = ()=>{equipment[slotOf(e)]=e;render()}; root.append(b);
    });
    render();
}

Promise.all(['base',...new Set(entries.map(e=>e.Sheet))].map(key=>new Promise((resolve,reject)=>{
    const img=new Image(); img.onload=()=>{images[key]=img;resolve()}; img.onerror=reject;
    img.src=assets+'equipment-pixel-'+key+'.png';
}))).then(()=>{
    for (const s of slots) {
        const wrap=document.createElement('div'),label=document.createElement('label'),select=document.createElement('select');
        label.htmlFor=select.id='slot-'+s; label.textContent=names[s]; select.add(new Option('Unequipped',''));
        entries.forEach((e,i)=>{if(slotOf(e)===s)select.add(new Option(e.Name+' · Tier '+e.Tier,i))});
        select.onchange=()=>{equipment[s]=select.value===''?null:entries[select.value];render()};
        wrap.append(label,select);document.querySelector('#controls').append(wrap);
    }
    for (const [s,name] of Object.entries({head:'Veil',chest:'Duelist Coat',legs:"Knight's Greaves",feet:'Pelt Boots',weapon:'Mace'}))
        equipment[s]=entries.find(e=>slotOf(e)===s&&e.Name===name);
    catalog(); window.spritesReady=true;
}).catch(()=>document.querySelector('#loadout').textContent='Unable to load artwork. Keep this file with its data script and project assets.');
document.querySelector('#clear').onclick=()=>{slots.forEach(s=>equipment[s]=null);render()};
document.querySelector('#random').onclick=()=>{
    slots.forEach(s=>{const es=entries.filter(e=>slotOf(e)===s);equipment[s]=es[Math.floor(Math.random()*es.length)]});render();
};
document.querySelector('#search').oninput=catalog;
