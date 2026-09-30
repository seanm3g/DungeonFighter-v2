import {SUFFIX_CATALOG} from './suffix-catalog.js';
import {ITEM_CATALOG} from './item-catalog.js';
export {ITEM_CATALOG};
export const ITEM_DETAILS=Object.fromEntries(ITEM_CATALOG.map(i=>[i.id,i]));
export const FAMILIES=['sword','mace','dagger','wand','head','chest','legs','feet','charm','consumable'];
export {SUFFIX_CATALOG};
export const SUFFIX_DETAILS=Object.fromEntries(SUFFIX_CATALOG.map(s=>[s.id,s]));
export const VERSION = 2;
export const WEAPONS = ['sword','mace','dagger','wand'];
export const MATERIALS = {
  wood:['Wood','#6b5942','#302c28','#ac9167'],leather:['Leather','#6d5142','#30282a','#ac8262'],cloth:['Cloth','#696558','#343438','#a8a08b'],
  iron: ['Iron','#50504c','#25282b','#a59f8f'], steel:['Steel','#727777','#30363d','#d5cfbc'],
  bronze:['Bronze','#987543','#44382b','#c1a471'], gold:['Gold','#ad8848','#51402c','#d2b77e'],
  silver:['Silver','#949c9c','#454b57','#e3dfcb'], mithril:['Mithril','#647c87','#303e50','#c9d8d5'],
  bone:['Bone','#c2b392','#6e6252','#e5d9ba'], glass:['Glass','#698780','#303f40','#c0d0b7'],
  obsidian:['Obsidian','#38343e','#191b24','#8d879e'], shadow:['Shadow','#302e3d','#171721','#6f687d'],
  willow:['Willow','#655940','#302c26','#aa9874'], crystal:['Crystal','#8b809b','#4b465e','#d3c7d9'],
  stone:['Stone','#79766a','#3c3c38','#bdb5a0'], celestial:['Celestial','#a6a9a3','#4e535f','#e0d6b4'],
  strange:['Strange','#786076','#3d303f','#b49eaa'], unknown:['Unknown','#626367','#33323a','#a6a597']
};
export const GRIPS = {umber:['Umber','#513c30','#292525','#806148'],oxblood:['Oxblood','#713637','#302125','#a55d50'],ink:['Ink','#343437','#202023','#626164'],olive:['Olive','#5a6044','#30362c','#8d936b'],ivory:['Ivory','#b5a688','#615b50','#d0c2a2']};
export const FITTINGS = {bronze:['Bronze','#957443','#483a29','#c7a367'],iron:['Iron','#606364','#292e34','#a5a99f'],bone:['Bone','#b9a988','#5e5242','#d9cbae'],silver:['Silver','#a3a5a0','#474b53','#d8d5c5']};
export const PREFIXES = ['none','reinforced','balanced','precise','swift','featherweight','hardened','light','heavy','short','long','blessed','enchanted','charged','acrobatic','ancient','keen','flaming','poisonous','serrated','refined','nimble','brutal','sturdy'];
export const SUFFIXES = ['none',...SUFFIX_CATALOG.map(s=>s.id)];
export const QUALITIES = ['standard','broken','battle-scarred','worn','second-hand','preowned','like-new','new','perfect','masterwork','heirloom','cosmic'];
export const FIELDS = ['material','quality','prefix','suffix'];
export const QUALITY_GRIP = {standard:'umber',broken:'umber','battle-scarred':'oxblood',worn:'umber','second-hand':'olive',preowned:'umber','like-new':'ink',new:'ink',perfect:'ink',masterwork:'oxblood',heirloom:'ivory',cosmic:'ivory'};
export const MATERIAL_FITTINGS = {wood:'bronze',leather:'iron',cloth:'bone',iron:'iron',steel:'iron',bronze:'bronze',gold:'bronze',silver:'silver',mithril:'silver',bone:'bone',glass:'silver',obsidian:'iron',shadow:'iron',willow:'bronze',crystal:'silver',stone:'iron',celestial:'silver',strange:'bronze',unknown:'iron'};
export const title = s => s.replaceAll('-', ' ').replace(/\b\w/g, c=>c.toUpperCase());
export function hash(s) {let h=2166136261; for(const c of String(s)) h=Math.imul(h^c.charCodeAt(0),16777619);return h>>>0;}
export function rng(seed) {let a=hash(seed);return ()=>{a+=0x6d2b79f5;let t=a;t=Math.imul(t^t>>>15,t|1);t^=t+Math.imul(t^t>>>7,t|61);return ((t^t>>>14)>>>0)/4294967296;};}
const options = {weapon:FAMILIES,material:Object.keys(MATERIALS),prefix:PREFIXES,suffix:SUFFIXES,grip:Object.keys(GRIPS),fittings:Object.keys(FITTINGS),quality:QUALITIES};
export function validateRecipe(value) {
  if (!value || ![1,VERSION].includes(value.version)) throw Error('This recipe version is not supported.');
  const clean={version:VERSION};
  if(value.item!==undefined){const item=ITEM_DETAILS[value.item];if(!item||item.family!==value.weapon)throw Error('Invalid base item for this family.');clean.item=item.id;}
  if(!WEAPONS.includes(value.weapon)&&!clean.item)throw Error('Choose a base item for this family.');
  for(const key of ['weapon',...FIELDS]) {if(!options[key].includes(value[key])) throw Error(`Invalid ${key} in recipe.`);clean[key]=value[key];}
  if(clean.weapon==='consumable')Object.assign(clean,{prefix:'none',suffix:'none',quality:'standard',material:ITEM_DETAILS[clean.item].shape==='potion'?'glass':'wood'});
  clean.grip=QUALITY_GRIP[clean.quality];clean.fittings=MATERIAL_FITTINGS[clean.material];
  if(!Number.isSafeInteger(value.seed) || value.seed<0 || value.seed>4294967295) throw Error('Invalid recipe seed.');
  clean.seed=value.seed;return clean;
}
export function randomRecipe(weapon,seed,locked={},source={}) {
  const random=rng(seed), pick=a=>a[Math.floor(random()*a.length)];
  const recipe={version:VERSION,weapon,seed:hash(seed)};
  recipe.item=source.item && (locked.item||source.keepItem) && ITEM_DETAILS[source.item]?.family===weapon ? source.item:pick(ITEM_CATALOG.filter(i=>i.family===weapon)).id;
  if(locked.item && source.weapon===weapon && !source.item && WEAPONS.includes(weapon))delete recipe.item;
  for(const field of FIELDS) recipe[field]=locked[field] && source[field] ? source[field] : pick(options[field]);
  if(weapon==='consumable')Object.assign(recipe,{prefix:'none',suffix:'none',quality:'standard',material:ITEM_DETAILS[recipe.item].shape==='potion'?'glass':'wood'});
  return validateRecipe(recipe);
}
export function itemName(r) {
  if(r.weapon==='consumable')return ITEM_DETAILS[r.item].name;
  const quality = ['standard','preowned'].includes(r.quality)?'':title(r.quality)+' ';
  const suffix = r.suffix==='none'?'':` ${SUFFIX_DETAILS[r.suffix].name}`;
  return `${quality}${r.prefix==='none'?'':title(r.prefix)+' '}${MATERIALS[r.material][0]} ${ITEM_DETAILS[r.item]?.name||title(r.weapon)}${suffix}`;
}
export const EFFECTS = new Set(['flaming','poisonous','charged','enchanted','blessed']);
export function describe(input) {
  const r=validateRecipe(input);
  const item=ITEM_DETAILS[r.item];
  if(r.weapon==='consumable')return [`Base → ${item.name}`,`Purpose → ${item.effect||'Provision'}`,'Consumables retain their catalog identity; equipment affixes do not apply.','Print distress → Wear on the illustration only'];
  const part={sword:'blade',dagger:'blade',mace:'head',wand:'focus',head:'shell',chest:'body',legs:'panels',feet:'upper',charm:'setting',consumable:'vessel'}[r.weapon];
  const edits={none:'Original construction',flaming:`Flame from the grip into the ${part}`,poisonous:`Olive channel from the grip into the ${part}`,charged:`Charge from the grip into the ${part}`,enchanted:`Runes from the grip into the ${part}`,blessed:`Blessing seal at the grip–${part} junction`,serrated:['sword','dagger'].includes(r.weapon)?'Shallow teeth on the cutting edge':'Sawtooth collar motif',reinforced:'Reinforcement collar at the joint',sturdy:'Broad support collar',heavy:'Broader working body',short:'Shorter working body',long:'Longer working body',ancient:'Archaic curved fittings',brutal:'Squared working-body treatment',keen:'Unbroken pale edge',hardened:'Deepened working-body shadow',balanced:'Paired balanced fittings',precise:'Straight narrow inset line',refined:'Fine planar border',swift:'Swept fittings and angled wrap',acrobatic:'Curved fittings and paired wraps',nimble:'Compact fittings',light:'Lighter, slimmer fittings',featherweight:'Slim fittings and pale binding'};
  return [`Material → ${MATERIALS[r.material][0]} ${part} + ${FITTINGS[r.fittings][0]} fittings`,`Quality → ${title(r.quality)} · ${GRIPS[r.grip][0]} ${(!item||["sword","mace","dagger","wand","club","hammer","morningstar","flail","cleaver","saber","rapier","polearm","scythe","kris","hook","fang","staff"].includes(item.shape))?"wrap":"binding / trim"}`,EFFECTS.has(r.prefix)?`Prefix → Embossed ${title(r.prefix)} crest at the attachment point`:`Prefix → ${edits[r.prefix]}`,r.suffix==='none'?'Suffix → Plain setting':`Suffix → ${SUFFIX_DETAILS[r.suffix].name} · ${SUFFIX_DETAILS[r.suffix].rarity} · ${title(SUFFIX_DETAILS[r.suffix].motif)} emblem + ${SUFFIX_DETAILS[r.suffix].rarity==="Rare"?"hanging seal":SUFFIX_DETAILS[r.suffix].rarity==="Uncommon"?"ornament":"setting"}`];
}
export const COMBINATIONS = Object.keys(MATERIALS).length*PREFIXES.length*SUFFIXES.length*QUALITIES.length;

