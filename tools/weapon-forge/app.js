import {FAMILIES as WEAPONS,ITEM_CATALOG,ITEM_DETAILS,MATERIALS,GRIPS,FITTINGS,PREFIXES,SUFFIXES,SUFFIX_CATALOG,SUFFIX_DETAILS,QUALITIES,FIELDS,title,randomRecipe,itemName,describe,validateRecipe,COMBINATIONS} from './model.js';
import {renderWeapon} from './renderer.js';
const $=id=>document.getElementById(id);
const familyName={sword:'Swords',mace:'Maces',dagger:'Daggers',wand:'Wands & focuses',head:'Headwear',chest:'Body armor',legs:'Legwear',feet:'Footwear',charm:'Charms',consumable:'Consumables'};
let collection=[],selected=0,distress=12,locks={},pins=new Set(),shuffleSerial=0;
let backgroundDefault='#918572',backgrounds={},backgroundPalette=['#918572','#333d42','#b7ac92'],paletteSlots={},usePalette=false;
const validColor=c=>typeof c==='string'&&/^#[0-9a-f]{6}$/i.test(c);
const backgroundKey=i=>collection[i].weapon+'/'+collection.slice(0,i).filter(r=>r.weapon===collection[i].weapon).length;
const backgroundAt=i=>usePalette?backgroundPalette[(paletteSlots[backgroundKey(i)]??i)%backgroundPalette.length]:(backgrounds[backgroundKey(i)]||backgroundDefault);
function randomPaletteIndex(){return crypto.getRandomValues(new Uint32Array(1))[0]%backgroundPalette.length;}
function drawPalette(){
 $('backgroundPalette').innerHTML=backgroundPalette.map((color,i)=>`<div class="palette-swatch"><input type="color" value="${color}" data-palette="${i}" aria-label="Palette color ${i+1}"><input type="text" value="${color}" data-palette-hex="${i}" aria-label="Palette hex ${i+1}" maxlength="7" spellcheck="false"><button data-remove-palette="${i}" aria-label="Remove palette color ${i+1}" ${backgroundPalette.length===1?'disabled':''}>×</button></div>`).join('');
 $('usePalette').checked=usePalette;
}
function enablePalette(){usePalette=true;$('usePalette').checked=true;}
function paintBackgrounds(){document.querySelectorAll('[data-index]').forEach(card=>{card.querySelector('.art-ground').style.background=backgroundAt(Number(card.dataset.index));});$('hero').style.background=backgroundAt(selected);$('backgroundItem').value=backgroundAt(selected);saveSession();}
function exportArt(){let svg=renderWeapon(collection[selected],{uid:'export',distress});if(!$('transparent').checked)svg=svg.replace('</defs>','</defs><rect width="280" height="420" fill="'+backgroundAt(selected)+'"/>');return svg;}
const selectOptions={material:Object.keys(MATERIALS),prefix:PREFIXES,suffix:SUFFIXES,grip:Object.keys(GRIPS),fittings:Object.keys(FITTINGS),quality:QUALITIES};
const optionName=(field,key)=>field==='suffix'?(SUFFIX_DETAILS[key]?.name||'None'):({material:MATERIALS,grip:GRIPS,fittings:FITTINGS}[field]?.[key]?.[0]||title(key));
function optionsHtml(field){const option=key=>`<option value="${key}">${optionName(field,key)}</option>`;return field!=='suffix'?selectOptions[field].map(option).join(''):option('none')+['Common','Uncommon','Rare'].map(rarity=>`<optgroup label="${rarity}">${SUFFIX_CATALOG.filter(s=>s.rarity===rarity).map(s=>option(s.id)).join('')}</optgroup>`).join('');}
const notices={};
function notify(message){$('notice').textContent=message;$('notice').classList.add('show');clearTimeout(notices.timer);notices.timer=setTimeout(()=>$('notice').classList.remove('show'),3500);}
function saveSession(){try{localStorage.setItem('df-forge-v1',JSON.stringify({collection,selected,distress,backgroundDefault,backgrounds,backgroundPalette,paletteSlots,usePalette,locks,pins:[...pins],family:$('family').value,seed:$('seed').value,count:$('count').value}));}catch{}}
function makeCollection(seed,keepPinned=false){
 const previous=collection,source=previous[selected]||{},n=Number($('count').value);collection=[];
 for(const weapon of WEAPONS) for(let i=0;i<n;i++){
   const oldIndex=previous.findIndex((r,index)=>r.weapon===weapon && previous.slice(0,index).filter(x=>x.weapon===weapon).length===i);
   const old=previous[oldIndex];
   collection.push(keepPinned && pins.has(oldIndex)?old:randomRecipe(weapon,`${seed}/${weapon}/${i}`,locks,source));
 }
 if(!keepPinned)pins.clear();
 selected=Math.max(0,collection.findIndex(r=>r.weapon===$('family').value));drawGallery();drawInspector();saveSession();
}
function cardHtml(r,i){return `<button class="item-card${selected===i?' selected':''}" data-index="${i}" aria-label="Inspect ${itemName(r)}" aria-pressed="${selected===i}"><div class="art-ground" style="background:${backgroundAt(i)}"><span class="card-number">${String(i%Number($('count').value)+1).padStart(2,'0')}</span>${pins.has(i)?'<span class="pin-label">PINNED</span>':''}${renderWeapon(r,{uid:'card'+i,distress})}</div><div class="card-text"><strong>${itemName(r)}</strong><small>${ITEM_DETAILS[r.item]?.name||title(r.weapon)} · ${r.weapon==='consumable'?'Provision':(ITEM_DETAILS[r.item]?.tier?'Tier '+ITEM_DETAILS[r.item].tier:ITEM_DETAILS[r.item]?.rarity||'Original study')}</small></div></button>`;}
function drawGallery(){
 $('gallery').innerHTML=WEAPONS.filter(w=>$('family').value==='all'||w===$('family').value).map(weapon=>`<section class="weapon-section"><div class="section-heading"><h2>${familyName[weapon]}<small>${ITEM_CATALOG.filter(i=>i.family===weapon).length} BASE ITEMS</small></h2><button class="quiet" data-shuffle="${weapon}">↻ Shuffle family</button></div><div class="cards">${collection.map((r,i)=>r.weapon===weapon?cardHtml(r,i):'').join('')}</div></section>`).join('');
}
function drawInspector(){const r=collection[selected];
 if($('baseItem').dataset.family!==r.weapon){$('baseItem').innerHTML=(['sword','mace','dagger','wand'].includes(r.weapon)?'<option value="">Original study</option>':'')+ITEM_CATALOG.filter(i=>i.family===r.weapon).map(i=>`<option value="${i.id}">${i.name}${i.tier?" · Tier "+i.tier:i.rarity?" · "+i.rarity:""}</option>`).join('');$('baseItem').dataset.family=r.weapon;}
 $('baseItem').value=r.item||'';$('lockItem').setAttribute('aria-pressed',!!locks.item);$('lockItem').textContent=locks.item?'▣':'□';
 $('hero').style.background=backgroundAt(selected);$('backgroundItem').value=backgroundAt(selected);
 $('hero').innerHTML=renderWeapon(r,{uid:'hero',distress});$('weaponType').textContent=r.weapon.toUpperCase();$('itemName').textContent=itemName(r);$('selectedNumber').textContent=`${selected+1} / ${collection.length}`;
 $('recipeSummary').innerHTML=describe(r).map(s=>`<li>${s}</li>`).join('');$('pin').textContent=pins.has(selected)?'Unpin':'Pin';$('pin').setAttribute('aria-pressed',pins.has(selected));
 if(!$('controls').children.length) $('controls').innerHTML=FIELDS.map(field=>`<div class="control-row"><label for="select-${field}">${title(field)}</label><select id="select-${field}" data-field="${field}">${optionsHtml(field)}</select><button class="lock" data-lock="${field}"></button></div>`).join('');
 for(const field of FIELDS){$('select-'+field).value=r[field];$('select-'+field).disabled=r.weapon==='consumable';const button=document.querySelector(`[data-lock="${field}"]`);button.disabled=r.weapon==='consumable';button.setAttribute('aria-pressed',!!locks[field]);button.setAttribute('aria-label',`${locks[field]?'Unlock':'Lock'} ${field}`);button.title=button.getAttribute('aria-label');button.textContent=locks[field]?'▣':'□';}
}
function refresh(){collection=collection.map(validateRecipe);drawGallery();drawInspector();saveSession();}
function shuffleIndices(indices){const nonce=crypto.getRandomValues(new Uint32Array(1))[0];let changed=0;for(const i of indices){if(pins.has(i))continue;collection[i]=randomRecipe(collection[i].weapon,`${$('seed').value}/${++shuffleSerial}/${nonce}/${i}`,locks,collection[i]);changed++;}refresh();notify(changed?`Shuffled ${changed} ${changed===1?'item':'items'}. Locked components retained.`:'All selected items are pinned. Unpin to shuffle.');}
function download(blob,name){const url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download=name;a.click();setTimeout(()=>URL.revokeObjectURL(url),10000);}
function filename(ext){return itemName(collection[selected]).toLowerCase().replace(/[^a-z0-9]+/g,'-')+'.'+ext;}
$('gallery').addEventListener('click',e=>{const c=e.target.closest('[data-index]');if(c){selected=Number(c.dataset.index);refresh();return;}const shuffle=e.target.closest('[data-shuffle]');if(shuffle)shuffleIndices(collection.map((r,i)=>r.weapon===shuffle.dataset.shuffle?i:-1).filter(i=>i>=0));});
$('controls').addEventListener('change',e=>{const field=e.target.dataset.field;if(!field)return;collection[selected]={...collection[selected],[field]:e.target.value};refresh();});
$('controls').addEventListener('click',e=>{const btn=e.target.closest('[data-lock]');if(!btn)return;locks[btn.dataset.lock]=!locks[btn.dataset.lock];drawInspector();saveSession();});
$('pin').onclick=()=>{pins.has(selected)?pins.delete(selected):pins.add(selected);refresh();};
$('family').innerHTML='<option value="all">All families</option>'+WEAPONS.map(w=>`<option value="${w}">${familyName[w]}</option>`).join('');$('family').value='sword';
$('family').onchange=()=>{if($('family').value!=='all')selected=collection.findIndex(r=>r.weapon===$('family').value);refresh();};
$('baseItem').onchange=()=>{collection[selected]={...collection[selected],item:$('baseItem').value};if(!collection[selected].item)delete collection[selected].item;refresh();};
$('lockItem').onclick=()=>{locks.item=!locks.item;drawInspector();saveSession();};
$('shuffleSelected').onclick=()=>shuffleIndices([selected]);
$('shuffleAll').onclick=()=>shuffleIndices(collection.map((_,i)=>i));
$('replay').onclick=()=>{locks={};makeCollection($('seed').value);notify('Seed replayed. Pins and component locks cleared for an exact replay.');};
$('seed').addEventListener('keydown',e=>{if(e.key==='Enter')$('replay').click();});
$('count').onchange=()=>{pins.clear();makeCollection($('seed').value);};
$('distress').oninput=e=>{distress=Number(e.target.value);$('distressValue').value=distress+'%';refresh();};
$('applyBackground').onclick=()=>{usePalette=false;drawPalette();backgroundDefault=$('backgroundAll').value;backgrounds={};paintBackgrounds();};
$('randomBackgrounds').onclick=()=>{enablePalette();collection.forEach((_,i)=>paletteSlots[backgroundKey(i)]=randomPaletteIndex());paintBackgrounds();};
$('resetBackgrounds').onclick=()=>{usePalette=false;drawPalette();backgroundDefault='#918572';backgrounds={};$('backgroundAll').value=backgroundDefault;paintBackgrounds();};
$('backgroundItem').onchange=e=>{if(usePalette){let index=backgroundPalette.indexOf(e.target.value);if(index<0){index=backgroundPalette.push(e.target.value)-1;drawPalette();}paletteSlots[backgroundKey(selected)]=index;}else backgrounds[backgroundKey(selected)]=e.target.value;paintBackgrounds();};
$('randomBackgroundItem').onclick=()=>{enablePalette();paletteSlots[backgroundKey(selected)]=randomPaletteIndex();paintBackgrounds();};
$('usePalette').onchange=e=>{usePalette=e.target.checked;paintBackgrounds();};
$('addPaletteColor').onclick=()=>{backgroundPalette.push($('backgroundAll').value);drawPalette();saveSession();};
$('backgroundPalette').oninput=e=>{const hex=e.target.dataset.paletteHex;if(hex!==undefined){if(validColor(e.target.value)){e.target.setCustomValidity('');backgroundPalette[Number(hex)]=e.target.value.toLowerCase();$('backgroundPalette').querySelector('[data-palette="'+hex+'"]').value=e.target.value;paintBackgrounds();}return;}const index=e.target.dataset.palette;if(index===undefined)return;backgroundPalette[Number(index)]=e.target.value;$('backgroundPalette').querySelector('[data-palette-hex="'+index+'"]').value=e.target.value;paintBackgrounds();};
$('backgroundPalette').onchange=e=>{const index=e.target.dataset.paletteHex;if(index===undefined)return;if(!validColor(e.target.value)){e.target.setCustomValidity('Enter a six-digit hex color, such as #918572.');e.target.reportValidity();return;}e.target.setCustomValidity('');backgroundPalette[Number(index)]=e.target.value.toLowerCase();drawPalette();paintBackgrounds();};
$('backgroundPalette').onclick=e=>{const button=e.target.closest('[data-remove-palette]');if(!button||backgroundPalette.length===1)return;const index=Number(button.dataset.removePalette);backgroundPalette.splice(index,1);for(const key of Object.keys(paletteSlots)){const old=paletteSlots[key];paletteSlots[key]=old===index?0:old>index?old-1:old;}drawPalette();paintBackgrounds();};
$('exportSvg').onclick=()=>{download(new Blob([exportArt()],{type:'image/svg+xml'}),filename('svg'));notify('SVG saved with its reusable vector layers.');};
$('exportPng').onclick=async()=>{
 const button=$('exportPng');button.disabled=true;
 try{const svg=exportArt();const url=URL.createObjectURL(new Blob([svg],{type:'image/svg+xml'}));const img=new Image();try{img.src=url;await img.decode();const canvas=document.createElement('canvas');canvas.width=840;canvas.height=1260;canvas.getContext('2d').drawImage(img,0,0,840,1260);const blob=await new Promise(resolve=>canvas.toBlob(resolve,'image/png'));if(!blob)throw Error('Could not create image.');download(blob,filename('png'));notify('PNG saved at 840 × 1260.');}finally{URL.revokeObjectURL(url);}}catch(error){notify(error.message);}finally{button.disabled=false;}
};
$('exportRecipe').onclick=()=>download(new Blob([JSON.stringify({recipe:collection[selected],finish:{distress,background:backgroundAt(selected)}},null,2)],{type:'application/json'}),filename('json'));
$('importRecipe').onclick=()=>$('recipeFile').click();
$('recipeFile').onchange=async e=>{try{const file=e.target.files[0];if(!file)return;if(file.size>20000)throw Error('Recipe file is too large.');const data=JSON.parse(await file.text());const recipe=validateRecipe(data.recipe);let target=selected;if(recipe.weapon!==collection[selected].weapon)target=collection.findIndex(r=>r.weapon===recipe.weapon);collection[target]=recipe;selected=target;if(validColor(data.finish?.background)){backgrounds[backgroundKey(target)]=data.finish.background;if(usePalette){let index=backgroundPalette.indexOf(data.finish.background);if(index<0)index=backgroundPalette.push(data.finish.background)-1;paletteSlots[backgroundKey(target)]=index;drawPalette();}}if(Number.isFinite(data.finish?.distress)){distress=Math.max(0,Math.min(35,data.finish.distress));$('distress').value=distress;$('distressValue').value=distress+'%';}$('family').value=recipe.weapon;refresh();notify('Recipe loaded.');}catch(error){notify(`Could not load recipe: ${error.message}`);}finally{e.target.value='';}};
$('referenceBtn').onclick=()=>$('referenceDialog').showModal();$('closeReference').onclick=()=>$('referenceDialog').close();
$('combinationCount').textContent=COMBINATIONS.toLocaleString();
try{
 const saved=JSON.parse(localStorage.getItem('df-forge-v1'));
 if(saved && [4,6,10].includes(Number(saved.count)) && [4,WEAPONS.length].includes(saved.collection.length/Number(saved.count))){
  collection=saved.collection.map(validateRecipe);const n=Number(saved.count),oldFamilies=WEAPONS.slice(0,collection.length/n);
  if(!oldFamilies.every((w,j)=>collection.slice(j*n,(j+1)*n).every(r=>r.weapon===w)))throw Error('Invalid saved collection');
  for(const weapon of WEAPONS.slice(oldFamilies.length))for(let i=0;i<n;i++)collection.push(randomRecipe(weapon,saved.seed+'/'+weapon+'/'+i));
  if(Array.isArray(saved.backgroundPalette)){const colors=saved.backgroundPalette.filter(validColor).slice(0,64);if(colors.length)backgroundPalette=colors;}usePalette=saved.usePalette===true;paletteSlots=Object.fromEntries(Object.entries(saved.paletteSlots||{}).filter(([key,value])=>/^[a-z]+\/\d+$/.test(key)&&Number.isInteger(value)&&value>=0&&value<backgroundPalette.length));
  backgroundDefault=validColor(saved.backgroundDefault)?saved.backgroundDefault:'#918572';backgrounds=Object.fromEntries(Object.entries(saved.backgrounds||{}).filter(([key,color])=>/^[a-z]+\/\d+$/.test(key)&&validColor(color)));$('backgroundAll').value=backgroundDefault;
  selected=Math.max(0,Math.min(collection.length-1,Number(saved.selected)||0));
  distress=Math.max(0,Math.min(35,Number(saved.distress)||0));
  locks=Object.fromEntries([...FIELDS,'item'].map(f=>[f,!!saved.locks?.[f]]));
  pins=new Set((saved.pins||[]).filter(i=>Number.isInteger(i)&&i>=0&&i<collection.length));
  $('seed').value=String(saved.seed||'ashen-vault').slice(0,64);$('count').value=saved.count;
  $('distress').value=distress;$('distressValue').value=distress+'%';
  $('family').value=saved.family==='all'?'all':collection[selected].weapon;
 }
}catch{collection=[];}
drawPalette();
if(collection.length){drawGallery();drawInspector();}else makeCollection($('seed').value);

