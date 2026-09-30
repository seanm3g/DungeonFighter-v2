import {weaponPaint} from './weapon-paint.js';
import {rng} from './model.js';
export function footwearProfile(item){
 const name=item.name.toLowerCase();
 const style=/sandal/.test(name)?'sandals':/binding|wrap|chausses/.test(name)?'wrapped':/plate|ironshod|ironfoot|ironstep|greave|guard|legplate/.test(name)?'plate':/scale/.test(name)?'scale':/mail/.test(name)?'mail':/pelt/.test(name)?'fur':item.shape==='shoes'?'shoes':'boots';
 return {style,top:style==='sandals'?224:style==='shoes'?201:style==='wrapped'?154:87,anchorY:style==='sandals'||style==='shoes'?259:223};
}
export function footwearArt(item,m,q,{p,line,id='footwear',seed=132}){
 const {style,top}=footwearProfile(item),outline='stroke="#302e2b" stroke-width="2.2" stroke-linejoin="round"';
 let art='';
 for(let side=0;side<2;side++){
  const low=top>190;
  let shoe=p('M87 '+top+' Q111 '+(top-5)+' 135 '+(top+1)+' L135 '+(low?229:175)+' Q130 237 138 248 L126 279 Q120 299 90 300 Q65 302 55 286 Q48 271 65 253 L83 230 Q94 216 90 '+(low?top+5:197)+'Z',m[1],outline);
  if(!low){
   shoe+=p(side===0?'M119 '+top+' L135 '+(top+1)+' L135 175 Q130 197 138 248 L126 279 L116 287 L115 260 L115 234 Q110 216 113 188Z':'M87 '+(top+2)+' L99 '+top+' L102 188 Q108 215 97 236 L80 260 Q66 277 72 293 L58 286 Q51 272 65 253 L83 230 Q94 216 90 197Z',m[2]);
   shoe+=p(side===0?'M91 '+(top+4)+' L105 '+(top+2)+' L107 187 Q112 216 98 238 L80 263 Q68 273 69 282 Q58 277 72 261 L90 237 Q103 218 97 191Z':'M119 '+(top+3)+' L131 '+(top+4)+' L130 176 Q126 200 130 222 L126 250 L118 269 L107 277 L111 250 L116 230 Q113 207 117 186Z',m[3],'opacity=".7"');
  }else{
   shoe+=p(side===0?'M120 208 L134 211 L138 248 L126 279 L116 287 L113 251Z':'M88 210 L101 208 L100 239 L81 262 L72 293 L58 286 Q51 272 65 253 L83 230Z',m[2]);
  }
  shoe+=p('M69 270 Q89 250 113 262 L117 279 Q93 292 66 281Z',m[3],'opacity=".6"');
  shoe+=p('M54 281 Q78 309 121 285 L136 251 L136 265 L125 291 Q94 316 59 298 L53 291Z',q[2],outline);
  shoe+=p('M87 '+top+' Q111 '+(top-5)+' 135 '+(top+1)+' L135 '+(top+12)+' Q111 '+(top+5)+' 88 '+(top+12)+'Z',q[1],outline);
  shoe+=line('M84 238 Q99 234 116 245',m[2],1.8);
  if(style==='shoes')shoe+=p('M95 206 Q112 204 126 210 L123 224 Q109 217 96 222Z',q[2])+line('M94 228 L120 233 M89 237 L116 243',q[3],2);
  if(style==='sandals'){
   shoe=p('M92 221 Q118 210 134 232 L133 260 Q120 299 90 300 Q57 302 54 279 Q51 265 73 244Z',m[2],outline)+p('M63 278 Q85 296 116 279 L130 251 L129 268 Q110 308 69 296Z',q[2],outline);
   shoe+=p('M74 250 Q100 264 126 248 L122 266 Q96 276 64 265Z',q[1],outline)+p('M89 225 L104 220 L115 245 L128 248 L126 260 L105 252Z',q[1],outline)+line('M73 254 Q96 266 123 253',q[3],3);
  }
  if(style==='wrapped')for(const y of [171,188,205,222]){
   shoe+=p(`M91 ${y} Q111 ${y+1} 132 ${y-7} L131 ${y+7} Q110 ${y+16} 92 ${y+13}Z`,q[1],outline)
    +p(side===0?`M92 ${y+2} Q100 ${y+4} 109 ${y+1} L106 ${y+10} L93 ${y+11}Z`:`M121 ${y-1} L131 ${y-5} L130 ${y+6} L119 ${y+10}Z`,q[3],'opacity=".42"')
    +line(`M93 ${y+13} Q111 ${y+15} 130 ${y+7}`,q[2],2);
  }
  if(style!=='sandals'){
   shoe+=line('M60 288 Q84 306 117 288',q[3],.9);
   for(let i=0;i<7;i++){const x=65+i*7,y=294+Math.sin(i/6*Math.PI)*5;shoe+=line(`M${x} ${y-2} l-.3 2`,q[1],1.2);}
  }
  if(style==='fur')shoe+=p('M86 90 L97 86 L104 92 L115 85 L124 90 L136 88 L138 115 L129 110 L122 118 L110 112 L98 119 L89 113Z',q[3],outline);
  if(style==='plate'){
   shoe+=p('M95 117 Q111 111 129 117 L125 187 L110 203 L96 189Z',m[1],outline)+p(side===0?'M97 120 L109 116 L106 187 L99 192Z':'M120 117 L127 120 L123 185 L115 195Z',m[3],'opacity=".7"');
   shoe+=p('M84 240 Q105 235 125 246 L121 260 Q97 254 76 259Z',m[1],outline)+line('M85 244 Q105 240 121 249',m[3],2);
  }
  if(style==='scale'||style==='mail')for(const y of [129,149,169])shoe+=line('M97 '+y+' q7 9 14 0 q7 9 14 0',m[3],2);
  shoe=weaponPaint(shoe,{id:id+'-foot-'+side,m,q,f:q,seed,mirrored:side===1},{p,rng});
  art+='<g data-footwear-light="upper-left" data-footwear-style="'+style+'" transform="'+(side===0?'translate(0 0)':'translate(340 0) scale(-1 1)')+'">'+shoe+'</g>';
 }
 return '<g data-footwear-perspective="forward" transform="translate(4 15) scale(.8 .95)">'+art+'</g>';
}
