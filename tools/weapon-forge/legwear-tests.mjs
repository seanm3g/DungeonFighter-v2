import test from 'node:test';
import assert from 'node:assert/strict';
import {ITEM_CATALOG,MATERIALS,PREFIXES,SUFFIXES,validateRecipe} from './model.js';
import {legwearProfile} from './legwear.js';
import {renderWeapon} from './renderer.js';
const rows=ITEM_CATALOG.filter(i=>i.family==='legs');
const base={version:2,weapon:'legs',material:'steel',quality:'standard',prefix:'none',suffix:'none',seed:132};
test('legwear catalog maps to thirteen constructions with appropriate coverage',()=>{
 assert.equal(rows.length,50);
 assert.equal(new Set(rows.map(i=>legwearProfile(i).style)).size,13);
 for(const [name,style] of [['Knee Guards','knee-guards'],['shinguards','shin-guards'],['tassets','tassets'],['wraps','wraps'],['Full Greaves','full-plate'],['Linked Greaves','mail-greaves']])assert.equal(legwearProfile(rows.find(i=>i.name===name)).style,style);
});
test('each legwear construction responds to prefixes and renders every material and suffix',()=>{
 const reps=[...new Map(rows.map(i=>[legwearProfile(i).style,i])).values()];
 const clean=s=>s.replace(/aria-label="[^"]*"/g,'').replace(/<title>.*?<\/title>/g,'').replace(/w\d+-/g,'ID-');
 const render=r=>{const svg=renderWeapon(validateRecipe(r),{distress:0});assert.ok(!/NaN|undefined|Infinity/.test(svg));return svg;};
 for(const item of reps){
  const r={...base,item:item.id};const plain=clean(render(r));
  for(const prefix of PREFIXES.filter(x=>x!=='none'))assert.notEqual(clean(render({...r,prefix})),plain,item.name+'/'+prefix);
  for(const material of Object.keys(MATERIALS))render({...r,material});
  for(const suffix of SUFFIXES)render({...r,suffix});
 }
 for(const item of rows)assert.match(render({...base,item:item.id}),new RegExp(`data-legwear-style="${legwearProfile(item).style}"`));
});
test('paired legwear puts suffix attachments on both pieces, trousers use one waist attachment',()=>{
 for(const item of rows){
  const svg=renderWeapon({...base,item:item.id,suffix:'raven'});
  const paired=!['trousers','leggings','breeches','reinforced-breeches'].includes(legwearProfile(item).style);
  assert.equal(svg.includes('data-attachment="greaves-pair"'),paired,item.name);
 }
});
