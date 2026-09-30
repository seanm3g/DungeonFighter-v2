import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {SUFFIX_CATALOG,SUFFIX_DETAILS,itemName} from './model.js';
import {ITEM_CATALOG,ITEM_DETAILS,FAMILIES} from './model.js';
import {SHAPES} from './equipment.js';
import {WEAPONS,PREFIXES,SUFFIXES,MATERIALS,QUALITIES,QUALITY_GRIP,MATERIAL_FITTINGS,randomRecipe,validateRecipe,FIELDS} from './model.js';
import {renderWeapon,GEOMETRY,prefixAnchor} from './renderer.js';
const base={version:2,weapon:'sword',material:'steel',prefix:'none',suffix:'none',grip:'umber',fittings:'iron',quality:'standard',seed:132};
test('every named catalog row has a renderer, preserves its identity and exports finite art',()=>{
 const sources=['Weapons','Armor','Charms','Consumables'];
 const sourceRows=sources.flatMap(source=>JSON.parse(readFileSync(new URL(`../../GameData/${source}.json`,import.meta.url),'utf8')).filter(i=>i.name||i.displayName).map(i=>({name:i.name||i.displayName,source})));
 assert.deepEqual(ITEM_CATALOG.map(({name,source})=>({name,source})),sourceRows);
 assert.equal(new Set(ITEM_CATALOG.map(i=>i.id)).size,ITEM_CATALOG.length);
 for(const item of ITEM_CATALOG){
  assert.ok(SHAPES[item.shape]||GEOMETRY[item.shape],item.id);
  const recipe=validateRecipe({...base,weapon:item.family,item:item.id,suffix:'burden-of-kin',prefix:'flaming'});
  assert.ok(itemName(recipe).includes(item.name));
  assert.deepEqual(validateRecipe(JSON.parse(JSON.stringify(recipe))),recipe);
  for(const suffix of item.affixable?SUFFIXES:['none']){
   const svg=renderWeapon({...recipe,suffix},{distress:0});
   assert.ok(svg.startsWith('<svg '));assert.ok(!/NaN|undefined|Infinity/.test(svg),item.id+'/'+suffix);
  }
  const worn=renderWeapon(recipe,{distress:35,background:true});
  assert.ok(worn.includes('<mask'));assert.ok(worn.includes('fill="black"'));
 }
});
test('family selection, base locks and consumable rules remain valid',()=>{
 for(const family of FAMILIES){
  const r=randomRecipe(family,'catalog/'+family);
  assert.equal(ITEM_DETAILS[r.item].family,family);
  for(let i=0;i<30;i++)assert.equal(randomRecipe(family,i,{item:true},r).item,r.item);
  if(family==='consumable'){assert.equal(r.prefix,'none');assert.equal(r.suffix,'none');}
 }
 assert.throws(()=>validateRecipe({...base,item:ITEM_CATALOG.find(i=>i.family==='head').id}));
 assert.throws(()=>validateRecipe({...base,item:'missing'}));
 assert.equal(randomRecipe('sword',22,{item:true},base).item,undefined);
});
test('each equipment silhouette responds visually to every prefix',()=>{
 const representatives=[...new Map(ITEM_CATALOG.filter(i=>i.affixable).map(i=>[i.shape,i])).values()];
 const art=r=>renderWeapon(r,{distress:0}).replace(/aria-label="[^"]*"/g,'').replace(/<title>.*?<\/title>/g,'').replace(/w\d+-/g,'ID-');
 for(const item of representatives){
  const r={...base,weapon:item.family,item:item.id};const plain=art(r);
  for(const prefix of PREFIXES.filter(p=>p!=='none'))assert.notEqual(art({...r,prefix}),plain,item.shape+'/'+prefix);
 }
});
test('forge suffixes cover the game catalog with correct naming and deterministic rolls',()=>{
 const source=JSON.parse(readFileSync(new URL('../../GameData/StatBonuses.json',import.meta.url),'utf8'));
 assert.deepEqual(SUFFIX_CATALOG.map(s=>[s.name,s.rarity]),source.map(s=>[s.Name,s.Rarity]));
 assert.equal(new Set(SUFFIXES).size,SUFFIXES.length);
 assert.equal(itemName({...base,suffix:'burden-of-kin'}),'Steel Sword burden of kin');
 assert.equal(itemName({...base,suffix:'dire-wolf'}),'Steel Sword of the Dire Wolf');
 const rolled=new Set(Array.from({length:10000},(_,i)=>randomRecipe('sword',i).suffix));
 for(const s of SUFFIX_CATALOG){assert.ok(s.motif);assert.ok(rolled.has(s.id),s.id);assert.ok(SUFFIX_DETAILS[s.id]);}
});
test('magical prefixes originate at the grip joint and extend into the working body',()=>{
 for(const weapon of WEAPONS){
  const a=prefixAnchor(weapon),g=GEOMETRY[weapon];
  assert.equal(a.joint,g.joint);
  assert.ok(a.y<g.joint && a.y>g.tip+(g.joint-g.tip)*.5);
  assert.ok(a.y+25*a.scale<g.joint-5);
  assert.ok(50*a.scale<=(weapon==='dagger'?14:40));
  if(weapon!=='dagger')assert.ok(a.scale>prefixAnchor('dagger').scale*1.7);
  for(const prefix of ['flaming','poisonous','charged','enchanted','blessed']){
   const svg=renderWeapon({...base,weapon,prefix},{distress:0});
   assert.ok(svg.includes('data-prefix-anchor="joint"'));
   assert.ok(svg.includes('data-prefix-finish="embossed"'));
   assert.ok(!svg.includes('#813631'));
  }
 }
});
test('grips and fittings are derived from affixes, including legacy recipe migration',()=>{
 for(const quality of QUALITIES)for(const material of Object.keys(MATERIALS)){
  const r=validateRecipe({...base,version:1,quality,material,grip:'random old value',fittings:'random old value'});
  assert.equal(r.version,2);assert.equal(r.grip,QUALITY_GRIP[quality]);assert.equal(r.fittings,MATERIAL_FITTINGS[material]);
 }
 for(let i=0;i<100;i++){const r=randomRecipe('sword',i);assert.equal(r.grip,QUALITY_GRIP[r.quality]);assert.equal(r.fittings,MATERIAL_FITTINGS[r.material]);}
});
test('same seed and recipe reproduce the same item and artwork',()=>{
 for(const weapon of WEAPONS){assert.deepEqual(randomRecipe(weapon,'vault'),randomRecipe(weapon,'vault'));assert.equal(renderWeapon({...base,weapon}),renderWeapon({...base,weapon}));}
});
test('locked components survive hundreds of rerolls without stopping unlocked parts',()=>{
 const locks={material:true,suffix:true,quality:true},seen=new Set();
 for(let i=0;i<200;i++){const r=randomRecipe('sword',i,locks,base);for(const k of Object.keys(locks))assert.equal(r[k],base[k]);seen.add(r.prefix);}
 assert.ok(seen.size>10);
 const locked=randomRecipe('sword',19,Object.fromEntries(FIELDS.map(k=>[k,true])),base);for(const k of FIELDS)assert.equal(locked[k],base[k]);
});
test('all weapon, material, prefix and suffix combinations produce finite complete SVGs',()=>{
 for(const weapon of WEAPONS)for(const material of Object.keys(MATERIALS))for(const prefix of PREFIXES)for(const suffix of SUFFIXES){
   const svg=renderWeapon({...base,weapon,material,prefix,suffix},{distress:0});
   assert.ok(svg.startsWith('<svg '));assert.ok(svg.endsWith('</svg>'));assert.ok(!/NaN|undefined|Infinity/.test(svg));
 }
});
test('every motif and prefix changes drawn geometry or paint, not just metadata',()=>{
 const artwork=r=>renderWeapon(r,{distress:0}).replace(/aria-label="[^"]*"/g,'').replace(/<title>.*?<\/title>/g,'').replace(/w\d+-/g,'ID-');
 for(const weapon of WEAPONS){const baseline=artwork({...base,weapon});
  for(const prefix of PREFIXES.filter(x=>x!=='none')) assert.notEqual(artwork({...base,weapon,prefix}),baseline,`${weapon}/${prefix}`);
  for(const suffix of SUFFIXES.filter(x=>x!=='none')) assert.notEqual(artwork({...base,weapon,suffix}),baseline,`${weapon}/${suffix}`);
  for(const quality of QUALITIES)assert.ok(!/NaN|undefined/.test(renderWeapon({...base,weapon,quality})));
 }
});
test('finish is independent from recipe and material settings',()=>{
 const recipe={...base};const clean=renderWeapon(recipe,{distress:0}),worn=renderWeapon(recipe,{distress:25});assert.notEqual(clean,worn);assert.deepEqual(recipe,base);
 assert.ok(!clean.includes('fill="#8d8271"'));assert.ok(renderWeapon(recipe,{background:true}).includes('fill="#8d8271"'));
 assert.ok(worn.includes('<mask'));assert.ok(worn.includes('fill="black"'));
});
test('import validation rejects unknown values and injection and strips arbitrary data',()=>{
 for(const bad of [{...base,version:99},{...base,weapon:'axe'},{...base,suffix:'<script>'},{...base,seed:Infinity},{...base,seed:-1}])assert.throws(()=>validateRecipe(bad));
 assert.deepEqual(validateRecipe({...base,extra:'discard'}),base);
});
