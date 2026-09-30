import {ITEM_CATALOG,validateRecipe} from './model.js';
import {legwearProfile} from './legwear.js';
import {renderWeapon} from './renderer.js';
const groups=new Map();
for(const item of ITEM_CATALOG.filter(i=>i.family==='legs')){
 const {style}=legwearProfile(item);
 if(!groups.has(style))groups.set(style,[]);
 groups.get(style).push(item);
}
document.getElementById('grid').innerHTML=[...groups].map(([style,items],i)=>{
 const item=items[0],r=validateRecipe({version:2,weapon:'legs',item:item.id,material:'steel',quality:'standard',prefix:'none',suffix:'none',seed:132});
 return `<article><div class="art">${renderWeapon(r,{uid:'leg-review-'+i,distress:0})}</div><div class="caption"><span class="index">${String(i+1).padStart(2,'0')} / ${items.length} CATALOG ENTRIES</span><h2>${style.replaceAll('-',' ')}</h2><p>${items.map(x=>x.name).join(' · ')}</p></div></article>`;
}).join('');
