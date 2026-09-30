import {ITEM_CATALOG,FAMILIES,MATERIALS,validateRecipe} from './model.js';
import {renderWeapon} from './renderer.js';
import {legwearProfile} from './legwear.js';
import {headwearProfile} from './headwear.js';
import {footwearProfile} from './footwear.js';
const $=id=>document.getElementById(id);
$('family').innerHTML=FAMILIES.map(f=>`<option>${f}</option>`).join('');$('family').value='chest';
$('material').innerHTML=Object.keys(MATERIALS).map(m=>`<option value="${m}">${MATERIALS[m][0]}</option>`).join('');$('material').value='steel';
function draw(){
 const rows=new Map();
 for(const item of ITEM_CATALOG.filter(i=>i.family===$('family').value)){
  const key=item.family==='legs'?legwearProfile(item).style:item.family==='head'?headwearProfile(item).style:item.family==='feet'?footwearProfile(item).style:['charm','consumable'].includes(item.family)?item.name:item.shape;
  if(!rows.has(key))rows.set(key,item);
 }
 $('grid').innerHTML=[...rows].map(([key,item],i)=>{
  const r=validateRecipe({version:2,weapon:item.family,item:item.id,material:$('material').value,quality:'standard',prefix:'none',suffix:'none',seed:132});
  const opts={distress:Number($('finish').value)};
  return `<article><div class="art">${renderWeapon(r,{...opts,uid:'plain'+i})}${renderWeapon({...r,prefix:'flaming',suffix:'raven'},{...opts,uid:'affix'+i})}</div><div class="labels"><span>Base</span><span>${item.affixable?'Flaming · Raven':'Catalog identity'}</span></div><div class="caption"><h2>${item.name}</h2><small>${key.replaceAll('-',' ')}</small></div></article>`;
 }).join('');
}
for(const id of ['family','material','finish'])$(id).onchange=draw;draw();
