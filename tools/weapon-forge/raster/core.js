import {PREFIXES,prefixMapURL,shadePrefix} from './prefixes.js';
import {ASSETS} from './manifest.js';
export function validateRasterRecipe(value){
 if(!value||value.version!==1||!ASSETS.some(a=>a.id===value.asset))throw Error('Unsupported painted asset or recipe version.');
 const prefix=value.prefix===undefined?'none':value.prefix;
 if(!PREFIXES.some(p=>p.id===prefix))throw Error('Unsupported painted prefix.');
 const asset=ASSETS.find(a=>a.id===value.asset),layers={};
 if(asset.supportsPrefixes===false&&prefix!=='none')throw Error('Painted prefixes are available for weapons only.');
 for(const name of asset.regions){const layer=value.layers?.[name];if(!layer||!/^#[\da-f]{6}$/i.test(layer.color)||!Number.isFinite(layer.strength)||layer.strength<0||layer.strength>100)throw Error('Invalid '+name+' color or tint amount.');layers[name]={color:layer.color.toLowerCase(),strength:layer.strength};}
 if(!/^#[\da-f]{6}$/i.test(value.background))throw Error('Invalid background color.');
 return {version:1,asset:asset.id,prefix,layers,background:value.background.toLowerCase(),transparent:value.transparent!==false};
}
const rgb=hex=>[1,3,5].map(i=>parseInt(hex.slice(i,i+2),16));
export function tintPixels(base,mask,recipe,asset){
 if(base.length!==mask.length||base.length%4)throw Error('Painting and material mask dimensions differ.');
 const output=new Uint8ClampedArray(base),layers=asset.regions.map(name=>({rgb:rgb(recipe.layers[name].color),amount:recipe.layers[name].strength/100}));
 for(let i=0;i<base.length;i+=4){if(!base[i+3]||!(mask[i]||mask[i+1]||mask[i+2]))continue;
  const region=mask[i]>=mask[i+1]&&mask[i]>=mask[i+2]?0:mask[i+1]>=mask[i+2]?1:2;
  const {rgb:color,amount}=layers[region];if(!amount)continue;
  const lum=.2126*base[i]+.7152*base[i+1]+.0722*base[i+2],anchor=asset.referenceLuma[region];
  // A continuous two-part value curve holds black and white fixed. Bright
  // ivory colors retain brush variation instead of clipping highlights flat.
  for(let c=0;c<3;c++){const tinted=lum<=anchor?color[c]*lum/anchor:color[c]+(255-color[c])*(lum-anchor)/(255-anchor);output[i+c]=Math.round(base[i+c]*(1-amount)+tinted*amount);}
 }
 return output;
}
export function isolateLayer(pixels,mask,index){const out=new Uint8ClampedArray(pixels);for(let i=0;i<out.length;i+=4)out[i+3]=Math.round(out[i+3]*(index===0&&!(mask[i]||mask[i+1]||mask[i+2])?1:mask[i+index]/255));return out;}
const cache=new Map();
export async function loadRasterAsset(asset){
 if(!cache.has(asset.id))cache.set(asset.id,(async()=>{
  const read=async url=>{const image=new Image();image.src=url;await image.decode();if(image.naturalWidth!==asset.width||image.naturalHeight!==asset.height)throw Error('Incorrect image dimensions: '+url);const canvas=document.createElement('canvas');canvas.width=asset.width;canvas.height=asset.height;const ctx=canvas.getContext('2d',{willReadFrequently:true});ctx.drawImage(image,0,0);return ctx.getImageData(0,0,canvas.width,canvas.height).data;};
  const [base,mask]=await Promise.all([read(asset.base),read(asset.mask)]);return {base,mask};
 })().catch(e=>{cache.delete(asset.id);throw e;}));return cache.get(asset.id);
}
const reliefCache=new Map();
export async function loadPrefixMap(asset,prefix){
 const key=asset.id+':'+prefix;
 if(!reliefCache.has(key))reliefCache.set(key,(async()=>{
  const image=new Image();image.src=prefixMapURL(asset,prefix);await image.decode();
  if(image.naturalWidth!==asset.width||image.naturalHeight!==asset.height)throw Error('Incorrect prefix dimensions.');
  const canvas=document.createElement('canvas');canvas.width=asset.width;canvas.height=asset.height;
  const ctx=canvas.getContext('2d',{willReadFrequently:true});ctx.drawImage(image,0,0);return ctx.getImageData(0,0,canvas.width,canvas.height).data;
 })().catch(e=>{reliefCache.delete(key);throw e;}));
 return reliefCache.get(key);
}
export function pixelCanvas(data,w,h){const c=document.createElement('canvas');c.width=w;c.height=h;c.getContext('2d').putImageData(new ImageData(data,w,h),0,0);return c;}
export async function renderRaster(canvas,recipe,{width=512,background=false,layer=null}={}){
 recipe=validateRasterRecipe(recipe);const asset=ASSETS.find(a=>a.id===recipe.asset),{base,mask}=await loadRasterAsset(asset);
 let pixels=tintPixels(base,mask,recipe,asset);
 if(recipe.prefix!=='none'&&(layer===null||layer==='prefix'))pixels=shadePrefix(pixels,await loadPrefixMap(asset,recipe.prefix),{layerOnly:layer==='prefix'});
 else if(layer==='prefix')pixels=new Uint8ClampedArray(pixels.length);
 if(Number.isInteger(layer))pixels=isolateLayer(pixels,mask,layer);
 const painted=pixelCanvas(pixels,asset.width,asset.height);canvas.width=width;canvas.height=Math.round(width*asset.height/asset.width);
 const ctx=canvas.getContext('2d');ctx.imageSmoothingEnabled=true;ctx.imageSmoothingQuality='high';if(background){ctx.fillStyle=recipe.background;ctx.fillRect(0,0,canvas.width,canvas.height);}ctx.drawImage(painted,0,0,canvas.width,canvas.height);return canvas;
}
export const pngBlob=canvas=>new Promise((resolve,reject)=>canvas.toBlob(blob=>blob?resolve(blob):reject(Error('PNG export failed.')),'image/png'));
