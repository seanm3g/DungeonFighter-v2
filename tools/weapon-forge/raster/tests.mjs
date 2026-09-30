import test from 'node:test';
import assert from 'node:assert/strict';
import {ASSETS,PRESETS,presetRecipe} from './manifest.js';
import {validateRasterRecipe,tintPixels,isolateLayer} from './core.js';
import {zipFiles} from './zip.js';
const asset=ASSETS[0],base=new Uint8ClampedArray([70,65,60,255,40,25,20,200,150,100,50,255,0,0,0,0]),mask=new Uint8ClampedArray([255,0,0,255,0,255,0,255,0,0,255,255,0,0,0,0]);
test('original recipe reproduces every painted pixel and alpha exactly',()=>assert.deepEqual(tintPixels(base,mask,presetRecipe(),asset),base));
test('head color changes only the head and leaves fractional alpha untouched',()=>{const r=presetRecipe();r.layers.head={color:'#a24488',strength:100};const out=tintPixels(base,mask,r,asset);assert.notDeepEqual(out.slice(0,3),base.slice(0,3));assert.deepEqual(out.slice(4),base.slice(4));for(let i=3;i<base.length;i+=4)assert.equal(out[i],base[i]);});
test('light colors preserve highlight variation without clipping white',()=>{const r=presetRecipe(PRESETS.find(p=>p.id==='bone'));const b=new Uint8ClampedArray([170,170,170,255,210,210,210,255]),m=new Uint8ClampedArray([255,0,0,255,255,0,0,255]);const out=tintPixels(b,m,r,asset);assert.ok(out[0]<out[4]);assert.ok(out[4]<255);});
test('three masked layer alphas partition the painting exactly',()=>{const layers=[0,1,2].map(i=>isolateLayer(base,mask,i));for(let p=0;p<base.length;p+=4){assert.equal(layers.reduce((s,l)=>s+l[p+3],0),base[p+3]);for(const layer of layers)if(layer[p+3])assert.deepEqual(layer.slice(p,p+3),base.slice(p,p+3));}});
test('all presets serialize and validate without depending on browser state',()=>{for(const p of PRESETS){const r=presetRecipe(p);assert.deepEqual(validateRasterRecipe(JSON.parse(JSON.stringify(r))),r);assert.deepEqual(tintPixels(base,mask,r,asset),tintPixels(base,mask,r,asset));}});
test('invalid imports fail before reaching the renderer',()=>{for(const bad of [{...presetRecipe(),asset:'missing'},{...presetRecipe(),version:99},{...presetRecipe(),background:'url(evil)'}])assert.throws(()=>validateRasterRecipe(bad));const bad=presetRecipe();bad.layers.grip.strength=NaN;assert.throws(()=>validateRasterRecipe(bad));assert.throws(()=>tintPixels(base,mask.slice(4),presetRecipe(),asset));});
test('ZIP has valid local sizes, CRC, central directory offsets and binary payload',async()=>{const blob=await zipFiles({'hello.txt':'123456789','pixel.bin':new Blob([new Uint8Array([0,255,128])])}),data=new Uint8Array(await blob.arrayBuffer()),v=new DataView(data.buffer);assert.equal(v.getUint32(0,true),0x04034b50);assert.equal(v.getUint32(14,true),0xcbf43926);assert.equal(v.getUint32(18,true),9);const end=data.length-22;assert.equal(v.getUint32(end,true),0x06054b50);assert.equal(v.getUint16(end+10,true),2);const central=v.getUint32(end+16,true);assert.equal(v.getUint32(central,true),0x02014b50);assert.equal(v.getUint32(central+42,true),0);const second=central+46+'hello.txt'.length,offset=v.getUint32(second+42,true);assert.equal(v.getUint32(offset,true),0x04034b50);assert.deepEqual([...data.slice(offset+30+'pixel.bin'.length,offset+33+'pixel.bin'.length)],[0,255,128]);});

test('every item has independent valid recipes for all eight studies',()=>{assert.equal(new Set(ASSETS.map(a=>a.id)).size,11);for(const a of ASSETS)for(const p of PRESETS){const r=presetRecipe(p,a);assert.equal(r.asset,a.id);assert.deepEqual(validateRasterRecipe(JSON.parse(JSON.stringify(r))),r);if(!p.strength)assert.deepEqual(tintPixels(base,mask,r,a),base);}});

import {PREFIXES,shadePrefix} from './prefixes.js';
test('legacy recipes migrate to no prefix and invalid affixes are rejected',()=>{const old=presetRecipe();delete old.prefix;assert.equal(validateRasterRecipe(old).prefix,'none');for(const prefix of ['unknown','../flaming',null,{}])assert.throws(()=>validateRasterRecipe({...old,prefix}));});
test('all 160 painted color/prefix combinations retain their recipe identity',()=>{for(const a of ASSETS.filter(a=>a.supportsPrefixes!==false))for(const p of PRESETS)for(const affix of PREFIXES.slice(1)){const r={...presetRecipe(p,a),prefix:affix.id};assert.deepEqual(validateRasterRecipe(JSON.parse(JSON.stringify(r))),r);}});
test('relief preserves alpha, untouched paint, and transparent negative spaces',()=>{const b=new Uint8ClampedArray([100,80,60,255,100,80,60,255,10,20,30,0,40,30,20,128]),r=new Uint8ClampedArray([20,20,20,255,220,220,220,0,200,200,200,255,220,220,220,128]);const out=shadePrefix(b,r);assert.ok(out[0]<b[0]);assert.deepEqual(out.slice(4,12),b.slice(4,12));for(let i=3;i<b.length;i+=4)assert.equal(out[i],b[i]);assert.throws(()=>shadePrefix(b,r.slice(4)));});
test('separate prefix layer reconstructs the flattened result on opaque host surfaces',()=>{const b=new Uint8ClampedArray([110,80,60,255,180,120,80,255]),r=new Uint8ClampedArray([20,20,20,255,220,220,220,128]),flat=shadePrefix(b,r),layer=shadePrefix(b,r,{layerOnly:true});for(let i=0;i<b.length;i+=4)for(let c=0;c<3;c++){const composed=Math.round(layer[i+c]*layer[i+3]/255+b[i+c]*(1-layer[i+3]/255));assert.ok(Math.abs(composed-flat[i+c])<=1);}});

test('opaque stamped faces replace underlying brushwork instead of showing it through',()=>{const b=new Uint8ClampedArray([50,40,30,255,190,152,114,255]),r=new Uint8ClampedArray([95,95,95,255,95,95,95,255]);const out=shadePrefix(b,r),layer=shadePrefix(b,r,{layerOnly:true});assert.deepEqual(out.slice(0,4),out.slice(4,8));assert.equal(layer[3],255);assert.equal(layer[7],255);assert.deepEqual(out,layer);});

test('protected ivory stays unchanged and remains in exported shell layer',()=>{
 const b=new Uint8ClampedArray([210,180,120,200]),m=new Uint8ClampedArray([0,0,0,255]);
 assert.deepEqual(tintPixels(b,m,presetRecipe(PRESETS[6]),asset),b);
 assert.deepEqual(isolateLayer(b,m,0),b);assert.equal(isolateLayer(b,m,1)[3],0);
});
test('armor rejects weapon affixes before rendering',()=>{
 for(const a of ASSETS.filter(a=>a.supportsPrefixes===false))assert.throws(()=>validateRasterRecipe({...presetRecipe(PRESETS[0],a),prefix:PREFIXES[1].id}));
});
