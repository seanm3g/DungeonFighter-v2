import {MATERIALS,GRIPS,FITTINGS,ITEM_DETAILS,SUFFIX_DETAILS,rng,hash,validateRecipe,itemName} from './model.js';
import {renderEquipment} from './equipment.js';
import {weaponPaint} from './weapon-paint.js';
import {MACE_BODY,maceHead,wrappedGrip,roundedCollar} from './mace-art.js';
import {mixPaint} from './paint-light.js';
const ink='#252326';
const p=(d,fill,extra='')=>`<path d="${d}" fill="${fill}" ${extra}/>`;
const line=(d,color,width=2)=>p(d,'none',`stroke="${color}" stroke-width="${width}" stroke-linecap="round" stroke-linejoin="round"`);
const circ=(x,y,r,fill,extra='')=>`<circle cx="${x}" cy="${y}" r="${r}" fill="${fill}" ${extra}/>`;
export const GEOMETRY={
  sword:{joint:271,end:354,tip:37,body:'M130 37 L153 91 L151 266 L136 276 L110 269 L107 94 Z',light:'M130 40 L121 101 L120 257 L111 267 L108 94 Z',shadow:'M131 48 L152 92 L150 265 L132 274 L132 106 Z'},
  dagger:{joint:247,end:319,tip:99,body:'M130 99 L149 149 L147 238 L131 251 L112 239 L111 150 Z',light:'M130 102 L122 154 L121 234 L113 239 L112 150 Z',shadow:'M131 105 L148 149 L146 238 L131 250 L132 157 Z'},
  mace:{joint:179,end:351,tip:52,body:MACE_BODY,light:'M127 62 L138 60 L123 166 L116 173Z',shadow:'M149 60 L153 64 L144 137 L137 172 L127 174Z'},
  wand:{joint:258,end:350,tip:60,body:'M130 60 C151 60 155 88 139 98 L137 110 L139 250 Q139 264 130 265 Q120 264 121 250 L123 110 L121 98 C104 88 109 60 130 60 Z',light:'M127 64 C114 67 113 84 122 91 L127 88 C120 77 124 69 130 65 Z M124 108 L128 108 L128 252 Q127 260 123 256 Z',shadow:'M136 63 C153 70 151 88 139 98 L137 110 L139 250 Q138 263 131 264 L132 111 L133 94 C145 82 145 72 136 63 Z'}
};
function motif(name,accent,pale,dark) {
 name=SUFFIX_DETAILS[name]?.motif||name;
 const art={
  strength:p('M-9 9 V-3 L-5-10 H5 L9-3 V9Z',accent),
  shell:line('M-11 7 Q-16-12 0-12 Q16-12 11 7 L0 12Z M0-10 V9 M-8-7 L-2 9 M8-7 L2 9',accent,2),
  insect:circ(0,0,7,accent)+line('M0-12 V12 M-6-5 L-12-9 M6-5 L12-9 M-6 0 H-12 M6 0 H12 M-6 5 L-12 10 M6 5 L12 10',accent,2),
  tentacle:line('M0-11 V4 Q-9 16-11 5 M0 0 Q12-2 10 9 M0 5 Q5 15 8 12',accent,3),
  wave:line('M-12-5 Q-6-12 0-5 T12-5 M-12 3 Q-6-4 0 3 T12 3 M-8 10 H8',accent,2.5),
  scale:line('M0-12 L10-2 L0 12 L-10-2Z M-6-2 L0 4 L6-2 M-3-6 L0-3 L3-6',accent,2),
  moon:p('M5-12 A13 13 0 1 0 8 11 Q-7 6 5-12Z',accent),
  sun:circ(0,0,6,accent)+line('M0-13 V-9 M0 9 V13 M-13 0 H-9 M9 0 H13 M-9-9 L-6-6 M6 6 L9 9 M9-9 L6-6 M-6 6 L-9 9',accent,2),
  chain:line('M-8-2 C-17-12-2-17 1-7 L4 0 M8 2 C17 12 2 17-1 7 L-4 0 M-4-5 L4 5',accent,3),
  kin:line('M-10-6 L-4-12 L2-6 L-4 1Z M-2-6 L4-12 L10-6 L4 1Z M-4 1 V9 H4 V1',accent,2.5),
  heart:p('M0 11 C-23-4-7-18 0-7 C7-18 23-4 0 11Z',accent),
  eye:line('M-12 0 Q0-13 12 0 Q0 13-12 0Z',accent,2)+circ(0,0,4,accent),
  hand:line('M-8 2 V-5 M-4 1 V-10 M0 1 V-12 M4 1 V-9 M8 0 V-4 L7 8 L-3 12 L-11 3',accent,2.5),
  hourglass:line('M-8-11 H8 L-6 11 H8 L-8-11 M-8 11 H8',accent,2),
  stitch:line('M-10-9 L10 9 M-9-2 L-2-9 M-4 4 L4-4 M2 9 L9 2',accent,2.5),
  bone:line('M-7-8 L7 8 M-11-7 L-7-11 M3 9 L9 3',accent,4),
  raven:p('M-8 10 Q-8-4 6-12 Q13-4 0 7 Z',accent)+line('M-6 8 L6-9 M-2 2 L6 0 M0-2 L7-5',dark,1.5),
  cobra:line('M-7 8 C12 12 9-1-2 0 C-13 2-8-10 3-8 L8-5',accent,4)+circ(6,-7,1,pale),
  wolf:p('M-10-8 L-3-3 L0-10 L4-3 L10-8 L6 5 L0 12 L-6 5 Z',accent)+line('M-6 0 L-2 2 M2 2 L6 0',dark,2),
  ram:line('M-2 7 C-16 7-13-12-4-8 C3-3-8 1-8-4 M2 7 C16 7 13-12 4-8 C-3-3 8 1 8-4',accent,3),
  protection:p('M-9-9 L0-12 L9-8 L7 4 L0 12 L-7 5 Z',accent)+p('M0-8 L6-6 L4 3 L0 8 Z',dark),
  vitality:p('M0-12 C13-6 9 6 0 12 C-9 6-13-6 0-12Z','#8a3439')+p('M-2-8 L-5-2 L-2 1 L1-7Z','#c3866b'),
  killing:p('M0-12 C4-3 12 3 6 10 C-5 15-13 4 0-12Z','#883e39'),
  accuracy:circ(0,0,7,'none',`stroke="${accent}" stroke-width="2"`)+line('M0-13 V13 M-13 0 H13',accent,1.5),
  intelligence:p('M-12 0 Q0-12 12 0 Q0 12-12 0Z',accent)+circ(0,0,4,dark),
  rejuvenation:p('M-7 11 Q-13-7 9-12 Q13 8-7 11Z','#7e8955')+line('M-6 9 L7-9',pale,1.5),
  power:p('M-11 9 L-6-10 L6-10 L11 9 L0 4Z',accent),
  precision:line('M0-12 L8 0 L0 12 L-8 0Z M0-5 L3 0 L0 5 L-3 0Z',accent,2),
  technique:p('M-9 11 L-4-5 Q2-15 11-12 Q13-3 3 4Z',accent)+line('M-11 13 L7-8',dark,2),
  agility:line('M-10 5 L0-5 L10 5 M-7 11 L0 4 L7 11',accent,2),
  ferocity:p('M-9-9 L-1-6 L-4 11Z M2-6 L10-9 L5 11Z',pale),
  fleeting:line('M-10-6 L10-10 L-3 1 M-9 5 L10 2 L0 11',accent,2),
  none:circ(0,0,3,accent)
 };
 return art[name]||art.none;
}
function suffixAdornment(r,g,f) {
 const detail=SUFFIX_DETAILS[r.suffix];
 if(!detail || detail.rarity==='Common')return '';
 const symbol=detail.motif, [_,base,shadow,light]=f;
 const y=g.joint+15;
 let art='';
 if(detail.rarity==='Rare') {
   // A hanging seal makes poetic suffixes readable without altering the quality-owned wrap.
   art=line('M11 0 Q33 8 28 28',shadow,5)+line('M11 0 Q33 8 28 28',light,2);
   art+=p('M28 23 L43 36 L28 53 L13 36Z',shadow,`stroke="${base}" stroke-width="3"`);
   art+=`<g transform="translate(28 37) scale(.78)">${motif(r.suffix,light,light,shadow)}</g>`;
   if(symbol==='kin'||symbol==='chain')art+=line('M-12 0 Q-27 10-18 23 Q-7 28-8 15 Q-10 8-18 15 Q-28 23-18 34',light,3);
   else if(symbol==='stitch')art+=line('M-19 2 V34 M-25 7 L-13 13 M-25 19 L-13 25',light,3);
   else art+=p('M-13 0 L-23 4 L-19 35 L-12 27 L-6 32 L-7 6Z',shadow,`stroke="${base}" stroke-width="2"`);
 } else {
   const designs={
    raven:'M-10 0 L-34-16 L-28 1 L-21 0 L-24 10 L-14 5 M10 0 L34-16 L28 1 L21 0 L24 10 L14 5',
    wolf:'M-10 0 L-27-9 L-24 12 L-16 6 M10 0 L27-9 L24 12 L16 6',
    ram:'M-10 0 C-42-22-40 24-20 18 C-8 14-23 0-26 9 M10 0 C42-22 40 24 20 18 C8 14 23 0 26 9',
    cobra:'M-10 0 C-37-10-34 17-20 17 C-7 17-13 33-26 29 M10 0 C37-10 34 17 20 17 C7 17 13 33 26 29',
    shell:'M-11 0 Q-36-14-31 12 L-14 20 M11 0 Q36-14 31 12 L14 20 M-14 2 L-29 10 M14 2 L29 10',
    insect:'M-10 0 L-27-8 L-31-2 M10 0 L27-8 L31-2 M-12 6 L-30 10 L-33 18 M12 6 L30 10 L33 18',
    tentacle:'M-10 0 C-40-13-39 30-21 24 Q-9 17-23 12 M10 0 C40-13 39 30 21 24 Q9 17 23 12',
    wave:'M-10 0 Q-31-11-32 8 Q-23-1-15 14 M10 0 Q31-11 32 8 Q23-1 15 14',
    scale:'M-10 0 L-27-10 L-22 3 L-30 8 L-16 17 M10 0 L27-10 L22 3 L30 8 L16 17'
   };
   const d=designs[symbol]||designs.wolf;
   art=line(d,ink,7)+line(d,base,5)+line(d,light,1.5);
 }
 const scale=r.weapon==='dagger'?.65:1;
 return `<g data-suffix-adornment="${symbol}" transform="translate(130 ${y}) scale(${scale})">${art}</g>`;
}
function fittings(r,g,f) {
 const [_,base,shadow,light]=f;
 let body='';
 if(['sword','dagger'].includes(r.weapon)) {
   const j=g.joint, sweep=['swift','acrobatic','ancient'].includes(r.prefix);
   const w=['light','featherweight','nimble'].includes(r.prefix)?30:40;
   body=p(sweep?`M${130-w} ${j-10} Q110 ${j+7} 130 ${j-1} Q150 ${j+7} ${130+w} ${j-10} L${130+w-3} ${j+6} Q147 ${j+12} 130 ${j+8} Q108 ${j+12} ${133-w} ${j+6}Z`:`M${130-w} ${j-4} L168 ${j-4} L170 ${j+6} L132 ${j+8} L91 ${j+5}Z`,base,`stroke="${ink}" stroke-width="2.5"`);
   body+=line(`M${134-w} ${j-2} L160 ${j-1}`,light,3)+p(`M126 ${j-5} L139 ${j-3} L142 ${j+7} L130 ${j+12} L121 ${j+4}Z`,shadow);
 } else {
   const slim=['light','featherweight','nimble'].includes(r.prefix),left=slim?120:116,right=slim?141:146;
   body=roundedCollar(g.joint-4,left,right,f,{p,line});
   if(r.prefix==='ancient')body+=p(`M114 ${g.joint-5} Q130 ${g.joint-19} 148 ${g.joint-5} L145 ${g.joint+2} Q131 ${g.joint-5} 117 ${g.joint+2}Z`,base,`stroke="${ink}" stroke-width="2"`);
 }
 if(r.weapon==='dagger') body=`<g transform="translate(130 ${g.joint}) scale(.72 1) translate(-130 -${g.joint})">${body}</g>`;
 body+=roundedCollar(g.end-26,114,146,f,{p,line});
 const hasSuffix=r.suffix!=='none';
 const endcap=p(`M114 ${g.end-12} Q129 ${g.end-18} 146 ${g.end-11} L145 ${g.end+3} Q137 ${g.end+18} 124 ${g.end+16} Q111 ${g.end+12} 112 ${g.end}Z`,shadow,`stroke="${ink}" stroke-width="2"`)+p(`M115 ${g.end-10} Q126 ${g.end-15} 136 ${g.end-10} L140 ${g.end+1} L132 ${g.end+9} L120 ${g.end+6} L114 ${g.end-1}Z`,base)+p(`M117 ${g.end-8} L124 ${g.end-11} L130 ${g.end-7} L126 ${g.end-1} L119 ${g.end}Z`,light)+`<g transform="translate(130 ${g.end}) scale(${hasSuffix?1.05:.8})">${hasSuffix?motif(r.suffix,light,'#d9cba9',shadow):''}</g>`;
 body+=r.weapon==='dagger'?`<g transform="translate(130 ${g.end}) scale(.72) translate(-130 -${g.end})">${endcap}</g>`:endcap;
 if(['reinforced','sturdy'].includes(r.prefix)) body+=p(`M112 ${g.joint-21} L149 ${g.joint-20} L148 ${g.joint-7} L113 ${g.joint-8}Z`,shadow,`stroke="${ink}" stroke-width="2"`)+line(`M116 ${g.joint-19} L145 ${g.joint-18}`,light,2)+circ(119,g.joint-13,2,base)+circ(142,g.joint-12,2,base);
 if(r.prefix==='balanced') {const wide=['sword','dagger'].includes(r.weapon);body+=circ(wide?100:120,g.joint+1,3,light)+circ(wide?160:140,g.joint+1,3,light);}
 return body+suffixAdornment(r,g,f);
}
export function prefixAnchor(weapon) {
 const g=GEOMETRY[weapon];
 const scale=weapon==='wand'?.48:weapon==='dagger'?.27:weapon==='mace'?.8:.64;
 return {x:130,y:weapon==='mace'?119:g.joint-7-25*scale,scale,joint:g.joint};
}
function effect(r,g,m) {
 const {x,y,scale}=g.crest||prefixAnchor(r.weapon);
 const crest={
  flaming:'M-9 18 C-20 5-7-3-4-16 Q-3-4 2-3 Q8-9 4-25 C24-7 22 7 12 18 Q1 24-9 18Z M-3 15 Q-7 7 2 0 Q13 12 4 17',
  poisonous:'M0-24 C-4-10-16 0-14 11 C-11 26 12 26 14 11 C16 0 4-10 0-24Z M-7 8 Q-4 14 0 14',
  charged:'M4-25 L-15 2 L-1 2 L-7 23 L16-7 L3-7 L12-25Z',
  enchanted:'M0-24 L13-11 L0 2 L-13-11Z M0 2 V23 M-11 10 L0 18 L11 10',
  blessed:'M0-10 A10 10 0 1 1-.1-10 M0-16 V-24 M0 16 V24 M-16 0 H-24 M16 0 H24 M-12-12 L-17-17 M12 12 L17 17 M12-12 L17-17 M-12 12 L-17 17'
 }[r.prefix];
 if(['precise','refined','keen'].includes(r.prefix)) return line(`M125 ${g.joint+5} L125 ${g.joint-14} L${r.weapon==='wand'?125:122} ${g.joint-(g.joint-g.tip)*.6}`,m[3],r.prefix==='keen'?3:1.4);
 if(!crest)return '';
 // Shallow cast relief: visible face, tucked lower edge and a thin lit lip.
 // Material still owns the entire crest, including magical prefixes.
 return `<g data-prefix-anchor="joint" data-prefix-finish="embossed" transform="translate(${x} ${y}) scale(${scale})"><g transform="translate(1.4 2)" opacity=".9">${p(crest,m[2],`stroke="${m[2]}" stroke-width="3" stroke-linejoin="round"`)}</g>${p(crest,mixPaint(m[1],m[3],.62),`stroke="${m[3]}" stroke-width="1.15" stroke-linejoin="round"`)}<g transform="translate(.6 1)" opacity=".28">${line(crest,m[2],.8)}</g></g>`;
}
function quality(r,g,f) {
 const y=g.joint+25;
 switch(r.quality){
  case 'broken':return p(`M139 ${y} Q156 ${y+9} 149 ${y+23} L141 ${y+13}Z`,GRIPS[r.grip][1],`stroke="${ink}" stroke-width="1.5"`);
  case 'battle-scarred':return p(`M117 ${y} H143 V${y+10} H117Z`,f[2])+line(`M120 ${y+3} H140`,f[3],2);
  case 'second-hand':return p(`M118 ${y} L142 ${y+3} L141 ${y+11} L118 ${y+8}Z`,'#80735e');
  case 'perfect':case 'masterwork':return line(`M119 ${g.joint+13} H142 M119 ${g.joint+17} H142`,f[3],1.4);
  case 'heirloom':return line(`M115 ${g.joint-3} Q130 ${g.joint-15} 145 ${g.joint-3}`,f[3],2);
  case 'cosmic':return p(`M130 ${g.joint-10} L136 ${g.joint-3} L130 ${g.joint+3} L124 ${g.joint-3}Z`,'#b2a9bc');
  default:return '';
 }
}
export function renderWeapon(input,{uid='weapon',distress=12,background=false}={}) {
 const checked=validateRecipe(input),baseItem=ITEM_DETAILS[checked.item];
 if(baseItem&&!['sword','mace','dagger','wand'].includes(baseItem.shape))return renderEquipment(checked,{uid,distress,background},{p,line,circ,motif,effect,suffixAdornment,hash,rng,MATERIALS,FITTINGS,GRIPS,ITEM_DETAILS,SUFFIX_DETAILS,itemName});
 const r=validateRecipe(input),g=GEOMETRY[r.weapon],m=MATERIALS[r.material],f=FITTINGS[r.fittings],gr=GRIPS[r.grip];
 const id='w'+hash(uid+JSON.stringify(r));
 const outline=`stroke="${ink}" stroke-width="2.3" stroke-linejoin="round"`;
 const heightScale=r.prefix==='short'?.89:r.prefix==='long'?1.055:1;
 const widthScale=r.prefix==='heavy'?1.14:r.prefix==='brutal'?1.09:1;
 const transform=`translate(130 ${g.joint}) scale(${widthScale} ${heightScale}) translate(-130 -${g.joint})`;
 let grip=wrappedGrip(g,gr,{p,line});
 const start=g.joint+13;
 if(r.quality==='worn')grip+=p(`M121 ${start} H129 V${g.end-15} H121Z`,'#b5a18a','opacity=".18"');
 if(['swift','nimble','acrobatic'].includes(r.prefix))grip+=line(`M120 ${start+12} L141 ${start+4} M120 ${start+21} L141 ${start+13}`,f[1],3);
 if(r.prefix==='featherweight')grip+=line(`M120 ${start+7} L141 ${start+2}`,'#c7b99c',4);
 let body=p(g.body,m[1],outline)+p(g.shadow,r.prefix==='hardened'?ink:m[2])+p(g.light,m[3]);
 if(r.weapon==='mace')body=maceHead(r.prefix==='hardened'?[m[0],m[1],ink,m[3]]:m,f,{p,line});
 if(['sword','dagger'].includes(r.weapon))body+=line(`M130 ${g.tip+60} L129 ${g.joint-14}`,m[3],1.2)+`<g clip-path="url(#${id}-body)">${p(`M109 ${g.tip+63} L118 ${g.tip+59} L118 ${g.joint-8} L110 ${g.joint-4}Z`,'#aa9165','opacity=".16"')}</g>`;
 if(['sword','dagger'].includes(r.weapon)){
  // A recessed fuller has a shaded lip, while the ground bevel keeps a few
  // discontinuous bright sections instead of an uninterrupted white stripe.
  body+=p(`M134 ${g.tip+64} L138 ${g.tip+72} L137 ${g.joint-22} L133 ${g.joint-12}Z`,mixPaint(m[1],m[2],.65))
   +line(`M133 ${g.tip+67} L132 ${g.joint-24}`,mixPaint(m[1],m[3],.42),.8)
   +line(`M111 ${g.joint-31} L110 ${g.joint-17} M110 ${g.tip+89} L110 ${g.tip+106}`,m[3],.85)
   +p(`M113 ${g.joint-10} Q129 ${g.joint-5} 148 ${g.joint-10} L145 ${g.joint-2} L118 ${g.joint}Z`,m[2]);
 }
 if(['crystal','glass'].includes(r.material))body+=`<g clip-path="url(#${id}-body)">${p(`M130 ${g.tip+26} L141 ${g.tip+53} L130 ${g.joint-19} L136 ${g.tip+76}Z`,m[3],'opacity=".6"')}</g>`;
 if(r.prefix==='serrated') {
   if(['sword','dagger'].includes(r.weapon)) {const edge=r.weapon==='dagger'?137:145;for(let y=g.tip+75;y<g.joint-20;y+=24)body+=p(`M${edge} ${y} L${edge+5} ${y+9} L${edge} ${y+13}Z`,m[2],`stroke="${ink}" stroke-width="1.3"`);}
   else body+=line(`M116 ${g.joint-17} l5-5 5 5 5-5 5 5 5-5`,m[3],2);
 }
 const random=rng(r.seed), brush=[];
 // Broad, low-opacity marks preserve painted planes; no simulated material roughness.
 for(let i=0;i<12;i++){let x=108+random()*48,y=g.tip+random()*(g.joint-g.tip);brush.push(p(`M${x} ${y} l${5+random()*7} -3 -2 7 -8 2Z`,i%3?m[3]:m[2],`opacity=".09"`));}
 body+=`<g clip-path="url(#${id}-body)">${brush.join('')}</g>`;
 if(r.weapon==='wand') {
   // Reusable round setting, open scrollwork and curved bands distinguish the rod from a blade.
   body+=circ(130,80,23,'none',`stroke="${ink}" stroke-width="7"`)+circ(130,80,23,'none',`stroke="${f[1]}" stroke-width="4"`);
   body+=line('M110 89 C94 105 109 121 124 110 M150 89 C166 105 151 121 136 110',ink,6)+line('M110 89 C94 105 109 121 124 110 M150 89 C166 105 151 121 136 110',f[1],3);
   body+=line('M111 71 Q118 56 134 58',f[3],2);
   body+=p('M118 108 Q130 114 142 108 L141 119 Q130 125 119 119Z',f[1],outline)+line('M121 112 Q130 117 139 112',f[3],2);
   for(const y of [157,205])body+=p(`M122 ${y} Q130 ${y+5} 138 ${y} L138 ${y+6} Q130 ${y+11} 122 ${y+6}Z`,f[1])+line(`M123 ${y+1} Q129 ${y+5} 136 ${y+2}`,f[3],1.5);
 }
 let marks='';const amount=Math.max(0,Math.min(35,Number(distress)||0));
 const noise=rng('print'+r.seed);
 for(let i=0;i<(amount>0?180:0);i++){const x=104+noise()*53,y=45+noise()*325,w=.7+noise()*2.8,h=.5+noise()*1.4;marks+=p(`M${x.toFixed(2)} ${y.toFixed(2)} l${w.toFixed(2)} -1 -1 ${h.toFixed(2)} -${w.toFixed(2)} 1Z`,'black',`opacity="${((amount/35)*(.25+noise()*.35)).toFixed(3)}"`);}
 // Pigment dropout is an alpha mask on the finished art, including fittings. Transparent exports retain it.
 const raw=grip+`<g transform="${transform}">${body}</g>`+fittings(r,g,f)+quality(r,g,f)+effect(r,g,m);
 const components=weaponPaint(raw,{id,m,q:gr,f,seed:r.seed},{p,rng});
 const item=r.weapon==='dagger'?`<g transform="translate(130 215) scale(.9) translate(-130 -215)">${components}</g>`:components;
 const title=itemName(r).replaceAll('&','&amp;');
 return `<svg xmlns="http://www.w3.org/2000/svg" width="280" height="420" viewBox="0 0 280 420" role="img" aria-label="${title}"><title>${title}</title><defs><clipPath id="${id}-body">${p(g.body,'white')}</clipPath><mask id="${id}-wear" maskUnits="userSpaceOnUse" x="0" y="0" width="280" height="420"><rect width="280" height="420" fill="white"/>${marks}</mask></defs>${background?'<rect width="280" height="420" fill="#8d8271"/>':''}<g transform="rotate(21 138 207)"><g mask="url(#${id}-wear)">${item}</g></g></svg>`;
}
