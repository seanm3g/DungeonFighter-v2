import {paintLight,mixPaint} from './paint-light.js';

// Material-colored washes within existing facets; no glow or surface damage.
export function focusFinish(art,{id,shape,m,q,f,seed,narrow=false}, {p,rng}){
 const light=paintLight(m),defs=[],fills=new Map();
 const gradient=(name,color,stops)=>{
  if(fills.has(color))return;
  const key=`${id}-focus-${name}`;
  defs.push(`<linearGradient id="${key}" gradientUnits="userSpaceOnUse" x1="${narrow?116:70}" y1="85" x2="${narrow?148:196}" y2="315">${stops.map(([at,value])=>`<stop offset="${at}%" stop-color="${value}"/>`).join('')}</linearGradient>`);
  fills.set(color,`url(#${key})`);
 };
 gradient('body',m[1],[[0,light.mid],[32,m[1]],[78,mixPaint(m[1],m[2],.35)],[100,m[2]]]);
 gradient('light',m[3],[[0,light.key],[45,m[3]],[100,light.mid]]);
 gradient('shadow',m[2],[[0,m[2]],[64,light.deep],[100,light.bounce]]);
 gradient('grip',q[1],[[0,q[3]],[30,q[1]],[78,q[2]],[100,q[1]]]);
 gradient('fittings',f[1],[[0,f[3]],[30,f[1]],[75,f[2]],[100,f[1]]]);
 art=art.replace(/fill="(#[0-9a-fA-F]{6})"/g,(whole,color)=>fills.has(color)?`fill="${fills.get(color)}"`:whole);
 const random=rng('focus-paint-'+seed);let texture='';
 for(let i=0;i<(narrow?100:145);i++){
  const x=(narrow?113:48)+random()*(narrow?33:165),y=60+random()*275,w=1.5+random()*(narrow?3:8),h=1+random()*4;
  texture+=p(`M${x.toFixed(1)} ${y.toFixed(1)} l${w.toFixed(1)} -1 1 ${h.toFixed(1)} -${(w*.75).toFixed(1)} 2Z`,i%3?light.key:light.deep,`opacity="${(.025+random()*.055).toFixed(3)}"`);
 }
 return `<defs>${defs.join('')}<clipPath id="${id}-focus-paint">${p(shape,'white')}</clipPath></defs>${art}<g data-focus-finish="painted-mottle" clip-path="url(#${id}-focus-paint)">${texture}</g>`;
}
