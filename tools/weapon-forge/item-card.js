import {ITEM_CATALOG,validateRecipe} from './model.js';
import {renderWeapon} from './renderer.js';
const $=id=>document.getElementById(id);
// Deliberately authored display fixtures: these are not calculated gameplay rolls.
const fixtures={
 sword:{family:'sword',name:'Keen Steel Sword',suffix:'of the Raven',material:'steel',prefix:'keen',animal:'raven',type:'Sword · Weapon',main:'Attack damage',amount:'24',delta:'+6',secondary:'1.12×<br>Attack speed',stats:[['Strength','+4'],['Agility','+2'],['Critical chance','+5%']],effect:'Steel · Material effect',text:'A measured strike rewards a precise hand. Material-trigger description appears here.',req:'Requires 12 STR · 8 AGI',quality:'Masterwork'},
 chest:{family:'chest',name:'Reinforced Iron Armor',suffix:'of the Cave Bear',material:'iron',prefix:'reinforced',animal:'cave-bear',type:'Armor · Chest',main:'Armor',amount:'38',delta:'+9',secondary:'Chest<br>Equipment slot',stats:[['Strength','+3'],['Maximum health','+20'],['Damage reduction','+4%']],effect:'Iron · Material effect',text:'Built to withstand the next blow. Material-trigger description appears here.',req:'Requires 14 STR',quality:'Masterwork'},
 charm:{family:'charm',name:'Silver Charm',suffix:'of the Raven',material:'silver',prefix:'blessed',animal:'raven',type:'Accessory · Charm',main:'Intelligence',amount:'+8',delta:'+3',secondary:'Charm<br>Equipment slot',stats:[['Technique','+3'],['Maximum mana','+15'],['Spell power','+5%']],effect:'Silver · Material effect',text:'A quiet talisman with a watchful eye. Material-trigger description appears here.',req:'Requires 10 INT',quality:'Masterwork'}
};
let selected='sword';
const equipped=new Set();
function draw(){const d=fixtures[selected];const item=ITEM_CATALOG.find(i=>i.family===d.family&&i.shape===d.family)||ITEM_CATALOG.find(i=>i.family===d.family);
 const recipe=validateRecipe({version:2,weapon:d.family,item:item.id,material:d.material,quality:'masterwork',prefix:d.prefix,suffix:d.animal,seed:132});
 $('art').innerHTML=renderWeapon(recipe,{uid:'item-card-'+selected,distress:12});$('art').setAttribute('aria-label',d.name+' '+d.suffix);
 $('item-name').textContent=d.name+' '+d.suffix;$('item-type').textContent=d.type;$('craft').textContent=d.quality+' quality · '+d.material[0].toUpperCase()+d.material.slice(1);
 $('main-label').textContent=d.main;$('main-value').textContent=d.amount;$('main-delta').textContent=d.delta;$('main-delta').hidden=!$('compare').checked;$('main-delta').setAttribute('aria-label',d.delta+' compared to equipped');$('secondary').innerHTML=d.secondary;
 $('stats').innerHTML=d.stats.map(([k,v])=>`<dt>${k}</dt><dd>${v}</dd>`).join('');$('affix-label').textContent=d.effect;$('affix-text').textContent=d.text;$('requirements').textContent=d.req;
 $('equip').textContent=equipped.has(selected)?'Unequip item ↗':'Equip item ↗';$('status').textContent=equipped.has(selected)?'Equipped in this preview.':'';
 document.querySelectorAll('[data-item]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.item===selected)));
}
document.querySelectorAll('[data-item]').forEach(b=>b.addEventListener('click',()=>{selected=b.dataset.item;draw();}));$('compare').addEventListener('change',draw);$('equip').addEventListener('click',()=>{equipped.has(selected)?equipped.delete(selected):equipped.add(selected);draw();});draw();
