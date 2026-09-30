import {paintLight,mixPaint} from './paint-light.js';
import {paintSurface} from './paint-surface.js';

// Shared weapon paint, applied to explicit construction faces before print distress.
// Object-bounding-box gradients restart on each component, rather than washing
// the complete illustration with one diagonal ramp.
export function weaponPaint(art,{id,m,q,f,seed,natural=false,mirrored=false},{p,rng}){
 const light=paintLight(m),defs=[],fills=new Map();
 const soft=['Wood','Willow','Leather','Cloth'].includes(m[0]);
 const mineral=['Bone','Stone'].includes(m[0]);
 const ramp=(name,color,stops)=>{
  if(fills.has(color))return;
  const key=`${id}-weapon-${name}`;
  defs.push(`<linearGradient id="${key}" x1="${mirrored?1:0}" y1=".12" x2="${mirrored?0:1}" y2=".65">${stops.map(([at,c])=>`<stop offset="${at}" stop-color="${c}"/>`).join('')}</linearGradient>`);
  fills.set(color,`url(#${key})`);
 };
 if(!natural){
 ramp('face',m[1],[[0,mixPaint(m[1],m[3],soft?.13:.24)],[.32,mixPaint(m[1],m[3],.09)],[.68,m[1]],[1,mixPaint(m[1],m[2],soft?.35:.58)]]);
 ramp('bevel',m[3],[[0,soft?mixPaint(m[3],m[1],.4):mineral?m[3]:light.key],[.24,mixPaint(m[3],m[1],soft?.45:.12)],[.7,light.mid],[1,m[1]]]);
 ramp('side',m[2],[[0,mixPaint(m[2],m[1],.12)],[.35,m[2]],[.82,light.deep],[1,mixPaint(light.deep,m[2],.2)]]);
 ramp('binding',q[1],[[0,mixPaint(q[1],q[3],.35)],[.27,q[1]],[.72,q[2]],[1,mixPaint(q[2],'#18191c',.25)]]);
 ramp('wrap-shadow',q[2],[[0,mixPaint(q[2],q[1],.2)],[.5,q[2]],[1,mixPaint(q[2],'#16171b',.32)]]);
 ramp('hardware',f[1],[[0,f[1]],[.2,f[3]],[.36,mixPaint(f[3],f[1],.6)],[.62,f[1]],[.88,f[2]],[1,mixPaint(f[2],'#17191c',.25)]]);
 ramp('hardware-edge',f[3],[[0,mixPaint(f[3],'#ead7ae',.2)],[.36,f[3]],[1,f[1]]]);
 }else{
  const colors=[...new Set([...art.matchAll(/fill="(#[0-9a-fA-F]{6})"/g)].map(x=>x[1]))];
  for(const [i,color] of colors.entries())ramp('natural-'+i,color,[[0,mixPaint(color,'#e5d1a8',.12)],[.3,color],[1,mixPaint(color,'#24232a',.25)]]);
 }
 const silhouette=art.replace(/<defs>[\s\S]*?<\/defs>/g,'').replace(/\bid="[^"]*"/g,'').replace(/fill="(?!none)[^"]*"/g,'fill="white"').replace(/stroke="(?!none)[^"]*"/g,'stroke="white"');
 art=art.replace(/fill="(#[0-9a-fA-F]{6})"/g,(all,c)=>fills.has(c)?`fill="${fills.get(c)}"`:all);
 // Quieter contours let adjacent painted values carry the interior drawing.
 art=art.replace(/stroke-width="2\.[1235]"/g,'stroke-width="1.65"');
 const random=rng('weapon-brush-'+seed);let brush='';
 // Uneven clustered pigment, with broad quiet areas between marks. Muted
 // light and shadow glazes leave the authored plane values in control.
 for(let i=0;i<46;i++){
  const x=45+random()*165,y=55+random()*310,w=5+random()*15,h=4+random()*12;
  const n=v=>v.toFixed(1);
  brush+=p(`M${n(x)} ${n(y)} l${n(w*.65)} -2 ${n(w*.35)} ${n(h*.25)} -2 ${n(h*.35)} ${n(-w*.25)} ${n(h*.4)} ${n(-w*.75)} -2 2 ${n(-h*.6)}Z`,i%3?'#c9b99b':'#201f21',`opacity="${(.035+random()*.085).toFixed(3)}"`);
  if(i%4===0)brush+=p(`M${n(x+2)} ${n(y+2)} l${n(w*.6)} -1 -2 3 -${n(w*.5)} 1Z`,'#d3c3a5','opacity=".055"');
 }
 // Long, sparse strokes support the grain of shafts and blade faces.
 for(let i=0;i<(natural||soft?0:9);i++){
  const x=93+random()*68,y=80+random()*240;
  brush+=p(`M${x.toFixed(1)} ${y.toFixed(1)} l.7 -2 -.6 ${(5+random()*12).toFixed(1)} -.7 2Z`,light.key,'opacity=".09"');
 }
 const surface=paintSurface(id,seed,{soft,natural});
 return `<defs>${defs.join('')}${surface.defs}<mask id="${id}-weapon-paint-mask" maskUnits="userSpaceOnUse" x="0" y="0" width="280" height="420">${silhouette}</mask></defs><g data-weapon-finish="sculpted-paint" filter="url(#${surface.key})">${art}<g mask="url(#${id}-weapon-paint-mask)" pointer-events="none">${brush}</g></g>`;
}
