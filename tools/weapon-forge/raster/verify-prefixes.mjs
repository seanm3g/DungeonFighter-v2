// Real asset checks; optional offline sharp dependency, no runtime dependencies.
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {ASSETS} from './manifest.js';
import {PREFIXES,PREFIX_SURFACES,prefixMapURL,shadePrefix} from './prefixes.js';
const sharp=createRequire(import.meta.url)(process.argv[2]||'sharp');
const path=s=>fileURLToPath(new URL('../'+s,import.meta.url));
let checked=0;
for(const a of ASSETS.filter(a=>a.supportsPrefixes!==false)){
 const base=await sharp(path(a.base)).raw().toBuffer(),mask=await sharp(path(a.mask)).raw().toBuffer();
 for(const prefix of PREFIXES.slice(1)){
  const {data:map,info}=await sharp(path(prefixMapURL(a,prefix.id))).raw().toBuffer({resolveWithObject:true});
  assert.deepEqual([info.width,info.height,info.channels],[a.width,a.height,4]);
  let marked=0,changed=0;const out=shadePrefix(base,map),layer=shadePrefix(base,map,{layerOnly:true});
  for(let i=0;i<base.length;i+=4){
   assert.equal(out[i+3],base[i+3]);
   if(!map[i+3]){for(let c=0;c<4;c++)assert.equal(out[i+c],base[i+c]);continue;}
   marked++;assert.equal(base[i+3],255);assert.equal(mask[i+PREFIX_SURFACES[a.id].region],255);
   for(let c=0;c<3;c++){
    if(out[i+c]!==base[i+c])changed++;
    const composed=Math.round(layer[i+c]*layer[i+3]/255+base[i+c]*(1-layer[i+3]/255));assert.ok(Math.abs(composed-out[i+c])<=1);
   }
  }
  assert.ok(marked>100&&changed>100);checked++;
 }
}
console.log(`${checked} prefix maps: correct dimensions, visible relief, exact silhouette, correct host regions and layer reconstruction.`);
