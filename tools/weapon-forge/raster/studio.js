import {PREFIXES,prefixMapURL,prefixSurface} from './prefixes.js';
import {ASSETS,PRESETS,presetRecipe} from './manifest.js';
import {validateRasterRecipe,renderRaster,pngBlob,loadRasterAsset,pixelCanvas} from './core.js';
import {zipFiles} from './zip.js';
const $=id=>document.getElementById(id),storageKey='df-painted-forge-v1';
let asset=ASSETS[0],previewRevision=0;
const sessions=new Map();
let recipe=presetRecipe(),ground='parchment',revision=0,scheduled=false,compare=false;
const status=message=>$('status').textContent=message;
try{const saved=localStorage.getItem(storageKey);if(saved)recipe=validateRasterRecipe(JSON.parse(saved));}catch{status('Saved colors could not be restored. Using the original painting.');}
try{for(const value of JSON.parse(localStorage.getItem(storageKey+'-weapons')||'[]')){const restored=validateRasterRecipe(value);sessions.set(restored.asset,restored);}}catch{/* Last selected legacy recipe is still available. */}
sessions.set(recipe.asset,structuredClone(recipe));
asset=ASSETS.find(a=>a.id===recipe.asset);
const save=()=>{try{sessions.set(recipe.asset,structuredClone(recipe));localStorage.setItem(storageKey,JSON.stringify(recipe));localStorage.setItem(storageKey+'-weapons',JSON.stringify([...sessions.values()]));}catch{status('Browser storage unavailable. Download your color recipe to keep it.');}};
function controls(){
 $('layers').innerHTML=asset.regions.map((name,i)=>{const layer=recipe.layers[name],label=(asset.regionLabels||['Metal head','Leather grip','Metal fittings'])[i];return `<div class="layer"><div class="layer-top"><label for="color-${name}">${label}</label><div class="swatch"><input id="color-${name}" type="color" value="${layer.color}" data-color="${name}" aria-label="${label} color"><input type="text" value="${layer.color}" data-hex="${name}" aria-label="${label} hex color" maxlength="7" spellcheck="false"></div></div><label class="strength" for="strength-${name}"><span>Tint amount</span><output id="amount-${name}">${layer.strength}%</output></label><input id="strength-${name}" type="range" min="0" max="100" value="${layer.strength}" data-strength="${name}" aria-label="${label} tint amount"></div>`;}).join('');
 $('prefixSelect').value=recipe.prefix;$('prefixSelect').disabled=asset.supportsPrefixes===false;
 $('assetSelect').value=asset.id;$('assetName').textContent=asset.name;$('hero').setAttribute('aria-label',asset.name+' painted preview');
 $('background').value=recipe.background;$('transparent').checked=recipe.transparent;
}
function labelPreset(id){document.querySelectorAll('[data-preset]').forEach(el=>el.setAttribute('aria-pressed',el.dataset.preset===id));$('variantName').textContent=(recipe.prefix!=='none'?PREFIXES.find(p=>p.id===recipe.prefix).name+' · ':'')+(PRESETS.find(p=>p.id===id)?.name||'Custom colors');}
function paintGround(){const stage=$('stage');stage.classList.toggle('checker',ground==='checker');stage.style.backgroundColor=ground==='dark'?'#202628':recipe.background;}
async function draw(){const stamp=++revision,currentAsset=asset;try{
 const temp=document.createElement('canvas');
 if($('showMask').checked){const {mask}=await loadRasterAsset(currentAsset);temp.width=512;temp.height=768;temp.getContext('2d').drawImage(pixelCanvas(new Uint8ClampedArray(mask),currentAsset.width,currentAsset.height),0,0,512,768);}
 else await renderRaster(temp,compare?presetRecipe(PRESETS[0],asset):structuredClone(recipe));
 if(stamp!==revision)return;
 for(const id of ['hero','small','medium']){const c=$(id),ctx=c.getContext('2d');ctx.clearRect(0,0,c.width,c.height);ctx.imageSmoothingEnabled=true;ctx.imageSmoothingQuality='high';ctx.drawImage(temp,0,0,c.width,c.height);}
 const detail=$('prefixDetail');detail.hidden=recipe.prefix==='none';
 if(!detail.hidden){const surface=prefixSurface(currentAsset.id,recipe.prefix);
 const ctx=detail.getContext('2d'),side=Math.max(...surface.size)*1.55;ctx.clearRect(0,0,detail.width,detail.height);ctx.imageSmoothingEnabled=true;ctx.imageSmoothingQuality='high';ctx.drawImage(temp,(surface.center[0]-side/2)/2,(surface.center[1]-side/2)/2,side/2,side/2,0,0,detail.width,detail.height);}
 }catch(e){status('Could not render painting: '+e.message);}}
function requestDraw(){revision++;if(scheduled)return;scheduled=true;requestAnimationFrame(()=>{scheduled=false;draw();});}
function changed(){labelPreset(null);save();requestDraw();}
$('prefixSelect').innerHTML=PREFIXES.map(p=>`<option value="${p.id}">${p.name}</option>`).join('');
$('prefixSelect').onchange=e=>{recipe.prefix=e.target.value;save();requestDraw();buildPresets();};
$('assetSelect').innerHTML=ASSETS.map(a=>`<option value="${a.id}">${a.name}</option>`).join('');
controls();paintGround();
const initialPreset=PRESETS.find(p=>JSON.stringify(presetRecipe(p,asset).layers)===JSON.stringify(recipe.layers));labelPreset(initialPreset?.id);
$('layers').addEventListener('input',e=>{
 const color=e.target.dataset.color,strength=e.target.dataset.strength;
 if(color){recipe.layers[color].color=e.target.value;recipe.layers[color].strength=100;document.querySelector(`[data-hex="${color}"]`).value=e.target.value;$('strength-'+color).value=100;$('amount-'+color).value='100%';changed();}
 if(strength){recipe.layers[strength].strength=Number(e.target.value);$('amount-'+strength).value=e.target.value+'%';changed();}
});
$('layers').addEventListener('change',e=>{const name=e.target.dataset.hex;if(!name)return;if(!/^#[\da-f]{6}$/i.test(e.target.value)){e.target.setCustomValidity('Use a six-digit hex color such as #7f929e.');e.target.reportValidity();return;}e.target.setCustomValidity('');recipe.layers[name]={color:e.target.value.toLowerCase(),strength:100};$('color-'+name).value=recipe.layers[name].color;$('strength-'+name).value=100;$('amount-'+name).value='100%';changed();});
function applyPreset(p){recipe={...presetRecipe(p,asset),prefix:recipe.prefix,background:recipe.background,transparent:recipe.transparent};controls();labelPreset(p.id);save();requestDraw();}
$('reset').onclick=()=>{recipe.prefix='none';applyPreset(PRESETS[0]);buildPresets();};
document.querySelectorAll('[data-ground]').forEach(b=>b.onclick=()=>{ground=b.dataset.ground;paintGround();});
$('background').oninput=e=>{recipe.background=e.target.value;ground='parchment';paintGround();save();};
$('transparent').onchange=e=>{recipe.transparent=e.target.checked;save();};
$('showMask').onchange=requestDraw;
$('compare').onpointerdown=e=>{e.preventDefault();compare=true;requestDraw();};
const endCompare=()=>{if(compare){compare=false;requestDraw();}};
window.addEventListener('pointerup',endCompare);window.addEventListener('pointercancel',endCompare);window.addEventListener('blur',endCompare);
$('compare').onkeydown=e=>{if([' ','Enter'].includes(e.key)){e.preventDefault();compare=true;requestDraw();}};$('compare').onkeyup=endCompare;$('compare').onblur=endCompare;
function download(blob,name){const url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download=name;a.click();setTimeout(()=>URL.revokeObjectURL(url),10000);}
async function exporting(button,fn){button.disabled=true;status('Preparing export…');try{await fn();status('Download prepared.');}catch(e){status('Export failed: '+e.message);}finally{button.disabled=false;}}
$('exportPng').onclick=e=>exporting(e.target,async()=>{const snapshot=structuredClone(recipe),canvas=document.createElement('canvas');await renderRaster(canvas,snapshot,{width:Number($('resolution').value),background:!snapshot.transparent});download(await pngBlob(canvas),`${snapshot.asset}-${snapshot.prefix}-${canvas.width}.png`);});
$('saveRecipe').onclick=()=>download(new Blob([JSON.stringify(recipe,null,2)],{type:'application/json'}),recipe.asset+'-colors.json');
$('loadRecipe').onclick=()=>$('recipeFile').click();
$('recipeFile').onchange=async e=>{try{const file=e.target.files[0];if(!file)return;if(file.size>20000)throw Error('Color recipe exceeds 20 KB.');const next=validateRasterRecipe(JSON.parse(await file.text()));recipe=next;asset=ASSETS.find(a=>a.id===recipe.asset);buildPresets();controls();labelPreset(null);paintGround();save();requestDraw();status('Color recipe loaded.');}catch(error){status(error.message);}finally{e.target.value='';}};
$('exportLayers').onclick=e=>exporting(e.target,async()=>{
 const snapshot=structuredClone(recipe),asset=ASSETS.find(a=>a.id===snapshot.asset),files={};
 for(let i=0;i<asset.regions.length;i++){const c=document.createElement('canvas');await renderRaster(c,snapshot,{width:asset.width,layer:i});files[`${asset.regions[i]}.png`]=await pngBlob(c);}
 for(const [name,url] of [['base.png',asset.base],['regions.png',asset.mask]]){const response=await fetch(url);if(!response.ok)throw Error('Missing asset '+name);files[name]=await response.blob();}
 if(snapshot.prefix!=='none'){
  const c=document.createElement('canvas');await renderRaster(c,snapshot,{width:asset.width,layer:'prefix'});files['prefix.png']=await pngBlob(c);
  const response=await fetch(prefixMapURL(asset,snapshot.prefix));if(!response.ok)throw Error('Missing prefix map');files['prefix-relief.png']=await response.blob();
  files['prefix.json']=JSON.stringify({id:snapshot.prefix,map:'prefix-relief.png',surface:prefixSurface(asset.id,snapshot.prefix)},null,2);
 }
 files['recipe.json']=JSON.stringify(snapshot,null,2);files['asset.json']=JSON.stringify({...asset,base:'base.png',mask:'regions.png'},null,2);
 files['README.txt']='Canvas: 1024 x 1536. Composite head.png, grip.png and fittings.png at (0,0), source-over, then prefix.png if included. prefix-relief.png is the neutral runtime relief map; prefix.png already inherits the chosen material colors. Regions RGB: red=head, green=grip, blue=fittings. Black mask pixels preserve original pigment and are included in head.png (ivory horns). All layers have alpha. base.png is the untinted painting. recipe.json records tint choices. Use the repository raster/core.js for runtime tinting. Color changes do not alter silhouette or physically change the material.';
 download(await zipFiles(files),asset.id+'-layers.zip');
});
$('exportAtlas').onclick=e=>exporting(e.target,async()=>{
 const asset=ASSETS.find(a=>a.id===recipe.asset),prefix=recipe.prefix;
 const frameWidth=256,frameHeight=384,padding=2,strideX=frameWidth+padding*2,strideY=frameHeight+padding*2;
 const atlas=document.createElement('canvas');atlas.width=strideX*4;atlas.height=strideY*2;const ctx=atlas.getContext('2d'),frames={};
 for(let i=0;i<PRESETS.length;i++){const preset=PRESETS[i],c=document.createElement('canvas');await renderRaster(c,{...presetRecipe(preset,asset),prefix},{width:frameWidth});const x=(i%4)*strideX+padding,y=Math.floor(i/4)*strideY+padding;ctx.drawImage(c,x,y);frames[preset.id]={frame:{x,y,w:frameWidth,h:frameHeight},rotated:false,trimmed:false,sourceSize:{w:frameWidth,h:frameHeight},pivot:{x:asset.pivot[0],y:asset.pivot[1]},recipe:{...presetRecipe(preset,asset),prefix}};}
 const meta={image:asset.id+'-atlas.png',size:{w:atlas.width,h:atlas.height},scale:'1',padding,asset:asset.id};download(await zipFiles({[asset.id+'-atlas.png']:await pngBlob(atlas),[asset.id+'-atlas.json']:JSON.stringify({frames,meta},null,2)}),asset.id+'-atlas.zip');
});
async function buildPresets(){
 const stamp=++previewRevision,current=asset,prefix=recipe.prefix;
 $('presets').innerHTML=PRESETS.map(p=>`<button class="preset" data-preset="${p.id}" aria-pressed="false"><canvas width="256" height="384" aria-label="${p.name} preview"></canvas><span>${p.name}</span></button>`).join('');
 const canvases=[...$('presets').querySelectorAll('canvas')];
 const selected=PRESETS.find(p=>JSON.stringify(presetRecipe(p,current).layers)===JSON.stringify(recipe.layers));labelPreset(selected?.id);
 try{for(let i=0;i<PRESETS.length;i++){if(stamp!==previewRevision)return;await renderRaster(canvases[i],{...presetRecipe(PRESETS[i],current),prefix},{width:256});}if(stamp===previewRevision)status(current.name+' ready. Colors are saved in this browser.');}catch(e){if(stamp===previewRevision)status('Could not load painting: '+e.message);}
}
$('presets').onclick=e=>{const b=e.target.closest('[data-preset]');if(b)applyPreset(PRESETS.find(p=>p.id===b.dataset.preset));};
$('assetSelect').onchange=e=>{sessions.set(asset.id,structuredClone(recipe));asset=ASSETS.find(a=>a.id===e.target.value);recipe=sessions.get(asset.id)||presetRecipe(PRESETS[0],asset);compare=false;controls();paintGround();save();requestDraw();buildPresets();};
draw();buildPresets();
