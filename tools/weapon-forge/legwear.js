import {paintLight} from './paint-light.js';

export function legwearProfile(item){
 const n=item.name.toLowerCase();
 const style=/tasset/.test(n)?'tassets':/knee/.test(n)?'knee-guards':/shinguard/.test(n)?'shin-guards':/wrap/.test(n)?'wraps':/legging/.test(n)?'leggings':/trouser/.test(n)?'trousers':/breech/.test(n)?(/reinforced|riveted|soldier|hardened/.test(n)?'reinforced-breeches':'breeches'):/linked/.test(n)?'mail-greaves':/full|absolute|godplate|sovereign|overlord|eternal/.test(n)?'full-plate':/heavy|bulwark|fortress|siege|stomp|red october/.test(n)?'heavy-greaves':/plate|knight|crusader|champion|conqueror|war/.test(n)?'plate-greaves':'light-greaves';
 return {style};
}

export function legwearArt(item,m,q,f,id,{p,line,circ}){
 const {style}=legwearProfile(item),light=paintLight(m);
 const edge='stroke="#29272a" stroke-width="2" stroke-linejoin="round"';
 const grad=`${id}-leg-surface`;
 let defs=`<defs><linearGradient id="${grad}" x1="0" y1="0" x2="1" y2=".4"><stop stop-color="${light.mid}"/><stop offset=".4" stop-color="${m[1]}"/><stop offset="1" stop-color="${light.deep}"/></linearGradient></defs>`;
 const bodyFill=`url(#${grad})`;
 const stud=(x,y)=>circ(x,y,2.7,f[2])+circ(x-.6,y-.8,1.3,f[3]);
 const band=(y,w=26)=>p(`M${96-w} ${y} Q96 ${y+8} ${96+w} ${y} L${95+w} ${y+10} Q96 ${y+18} ${97-w} ${y+10}Z`,q[1],edge)+line(`M${100-w} ${y+2} Q96 ${y+10} ${92+w} ${y+2}`,q[3],2);
 let art='',anchor=[134,143,.55],paired=false;
 if(['breeches','reinforced-breeches','trousers','leggings'].includes(style)){
  const short=style.includes('breeches'),slim=style==='leggings',bottom=short?265:326;
  const d=slim?'M94 96 H177 L181 166 L165 327 H141 L134 204 L123 327 H97 L86 166Z':short?'M87 96 H181 Q198 157 180 202 L169 267 H140 L134 209 L126 267 H95 L87 202 Q67 151 87 96Z':'M88 96 H181 L191 176 L178 326 H140 L133 208 L124 326 H86 L76 175Z';
  art+=p(d,bodyFill,edge)+p(short?'M88 113 L103 113 L96 155 L108 192 L104 251 L97 255 L94 198 L83 169Z':'M92 112 L108 112 L97 174 L109 209 L101 313 H94 L91 211 L86 175Z',light.mid)
   +p(`M161 115 L179 114 L181 170 L167 210 L164 ${bottom-4} H153 L158 203 L163 165Z`,m[2])
   +p('M88 96 H180 L183 116 H85Z',q[1],edge)+line('M90 99 H177',q[3],2)
   +p('M124 97 H145 V116 H124Z',f[1],edge)+p('M128 101 H141 V112 H128Z',f[2])
   +line('M134 123 V187 M105 124 L91 150 M162 123 L178 148',m[2],2);
  for(const x of [96,145])art+=p(`M${x} ${bottom-15} H${x+25} V${bottom} H${x}Z`,q[1],edge)+line(`M${x+2} ${bottom-12} H${x+23}`,q[3],2);
  if(!slim){
   const contour=short
    ?'M90 97 Q133 102 177 96 Q181 124 187 153 Q191 181 178 208 L170 253 Q157 259 144 254 L140 218 Q139 201 135 195 Q131 197 128 211 L123 255 Q109 261 96 254 L90 214 Q79 187 80 165 Q81 134 90 97Z'
    :'M91 97 Q133 105 176 96 C176 120 190 143 189 164 Q189 185 181 208 C173 230 187 250 178 276 Q171 298 173 321 Q158 329 143 322 C147 297 136 276 142 252 Q146 233 138 207 Q133 191 129 209 C124 230 133 247 124 269 Q115 295 120 324 Q105 330 89 322 C94 301 81 281 87 256 Q92 233 83 211 C70 179 74 151 81 135 Q87 117 91 97Z';
   art=p(contour,bodyFill,edge);
   art+=p(short?'M94 116 Q82 151 84 174 Q83 192 99 211 Q111 228 106 249 L115 251 Q123 232 109 206 Q96 185 102 161 L109 120Z':'M96 116 Q82 154 87 180 Q87 197 100 218 Q110 238 99 264 Q94 286 104 317 L112 318 Q104 294 116 266 Q126 243 111 216 Q98 188 105 162 L111 120Z',light.mid);
   art+=p(short?'M161 115 Q183 148 179 177 Q177 194 159 215 Q150 230 155 249 L166 250 Q165 230 177 210 Q193 181 186 158Z':'M164 115 Q187 155 177 187 Q164 212 169 238 Q179 255 165 279 Q155 301 163 319 L170 318 Q166 296 177 274 Q185 253 176 235 Q169 215 180 192 Q192 154 172 118Z',m[2]);
   art+=p('M90 96 Q133 104 177 95 L178 112 Q132 123 86 112Z',q[1],edge)+line('M92 99 Q133 108 174 99',q[3],1.5)+p('M125 103 Q136 105 145 102 L146 118 Q134 121 124 118Z',f[1],edge)+p('M129 107 H141 V115 H129Z',q[2]);
   art+=line('M132 125 Q139 150 132 177 M103 124 Q101 140 89 149 M163 123 Q165 139 179 148',m[2],1.6);
   art+=p(short?'M128 179 Q107 187 102 202 Q115 191 129 191Z M140 191 Q161 198 170 212 Q154 204 143 207Z':'M129 190 Q108 203 102 214 Q115 205 128 204Z M141 229 Q159 240 170 238 Q155 247 144 241Z M92 280 Q105 287 116 280 L111 288 Q100 293 92 286Z',m[2]);
   for(const [x,y] of short?[[95,249],[143,248]]:[[89,315],[142,314]])art+=p(`M${x} ${y} Q${x+14} ${y+6} ${x+28} ${y} L${x+27} ${y+12} Q${x+14} ${y+18} ${x+1} ${y+11}Z`,q[1],edge)+line(`M${x+3} ${y+3} Q${x+14} ${y+8} ${x+25} ${y+3}`,q[3],1.4);
  }
  if(style==='breeches'){
   // Tailored knee breeches: a raised waistband, close thigh panels and split cuffs.
   art=p('M91 91 Q131 98 174 90 L179 127 Q184 150 179 181 L172 224 L171 276 Q160 283 145 278 L144 227 Q143 204 136 180 Q134 174 131 180 L121 223 L117 278 Q102 282 88 274 L91 223 L84 175 Q80 149 86 126Z',bodyFill,edge);
   art+=p('M94 114 Q105 119 115 117 Q100 149 101 177 L107 221 L101 268 L91 269 L96 222 L90 177 Q87 148 94 114Z',light.mid)
    +p('M153 114 L173 111 Q181 151 175 179 L166 222 L165 271 L153 274 L153 225 Q158 201 155 176 Q163 142 153 114Z',m[2])
    +p('M126 120 Q130 143 127 166 L118 207 L113 238 Q117 209 119 180 Q123 148 120 125Z',m[2])
    +p('M141 185 Q150 197 151 216 L147 238 Q148 209 139 199Z',light.mid);
   // Broad fall-front tailoring and inset pockets, rather than a cinched belt.
   art+=p('M91 91 Q132 98 174 90 L176 111 Q132 119 87 111Z',q[1],edge)
    +line('M94 95 Q132 103 171 95',q[3],1.4)
    +p('M114 115 Q132 120 152 115 L149 159 Q133 168 116 157Z',m[1])
    +line('M115 118 L117 157 Q132 165 148 158 L151 118',m[2],1.5)
    +line('M96 119 Q94 139 87 146 M164 119 Q168 137 177 142',light.deep,2)
    +line('M98 120 Q98 136 91 143 M162 122 Q165 138 173 143',light.mid,1);
   for(const y of [125,140,154])art+=circ(119,y,1.8,f[2])+circ(118.5,y-.5,.8,f[3])+circ(147,y,1.8,f[2])+circ(146.5,y-.5,.8,f[3]);
   art+=line('M89 157 Q88 178 96 217 L93 256 M174 157 Q174 184 168 216 L168 260',light.deep,1.1)
    +p('M89 258 Q101 264 118 259 L117 278 Q103 283 88 275Z',q[1],edge)
    +p('M144 261 Q158 266 171 259 L171 277 Q160 284 145 279Z',q[1],edge)
    +line('M92 263 Q102 268 114 264 M148 266 Q158 270 168 264',q[3],1.5)
    +line('M91 259 L94 276 M168 261 L166 279',light.deep,2);
   for(const [x,y] of [[94,265],[94,273],[166,268],[166,276]])art+=circ(x,y,1.9,f[1])+circ(x-.5,y-.6,.8,f[3]);
   art+=line('M104 226 Q112 229 118 223 M146 230 Q153 235 164 230',m[2],1.4);
  }
  if(style==='reinforced-breeches')for(const x of [0,49]){
   art+=`<g transform="translate(${x} 0)">`+p('M87 145 Q101 137 115 147 L123 186 L118 230 Q105 238 92 226 L85 184Z',q[2],edge);
   for(const y of [149,175,201])art+=p(`M89 ${y} Q103 ${y-5} 116 ${y+3} L118 ${y+23} Q106 ${y+32} 92 ${y+23}Z`,bodyFill,edge)+p(`M92 ${y+2} Q101 ${y-1} 106 ${y+1} L102 ${y+23} L94 ${y+21}Z`,light.mid)+line(`M94 ${y+25} Q106 ${y+31} 116 ${y+23}`,light.deep,2)+stud(94,y+6)+stud(113,y+8);
   art+='</g>';
  }
  if(style==='trousers'&&/patched/i.test(item.name))art+=p('M96 205 L117 209 L114 238 L93 234Z',q[1],edge)+line('M98 209 L113 213 M96 230 L110 234',q[3],1.5);
 }else{
  paired=true;let piece='';let ay=232;
  if(style==='tassets'){
   ay=221;
   art+=p('M66 108 Q135 129 206 108 L202 126 Q134 146 70 126Z',q[1],edge)+line('M71 112 Q135 133 201 112',q[3],2);
   art+=p('M124 116 H146 V135 H124Z',f[1],edge)+p('M128 120 H142 V131 H128Z',q[2]);
   piece+=p('M76 123 Q78 139 73 153 L83 156 Q91 138 87 124Z M107 129 L117 130 Q120 148 115 160 L105 157Z',q[1],edge)+p('M72 146 Q97 139 119 150 C119 180 111 209 115 240 Q119 263 99 278 Q77 285 56 262 Q70 233 67 207 Q60 175 72 146Z',light.deep,edge);
   for(let i=0;i<4;i++){
    const y=143+i*27,l=[72,68,68,64][i],r=[118,116,113,115][i];
    piece+=p(`M${l} ${y} Q94 ${y-7} ${r} ${y+5} Q${r-5} ${y+18} ${r} ${y+30} Q93 ${y+45} ${l-7} ${y+29} Q${l+4} ${y+15} ${l} ${y}Z`,bodyFill,edge)+p(`M${l+3} ${y+2} Q83 ${y-2} 94 ${y+1} Q86 ${y+15} 87 ${y+34} Q76 ${y+33} ${l-3} ${y+27} Q${l+8} ${y+13} ${l+3} ${y+2}Z`,m[3])+p(`M100 ${y+2} Q108 ${y+3} ${r-2} ${y+7} Q${r-6} ${y+20} ${r-2} ${y+29} Q106 ${y+36} 97 ${y+36} Q103 ${y+17} 100 ${y+2}Z`,m[2])+line(`M${l-4} ${y+29} Q91 ${y+43} ${r-1} ${y+30}`,f[1],2.5)+line(`M${l-2} ${y+28} Q79 ${y+35} 91 ${y+36}`,f[3],1)+stud(l+7,y+7)+stud(r-7,y+10);
   }
  }else if(style==='knee-guards'){
   ay=216;
   piece+=band(173,29)+band(224,27)+p('M95 157 L120 171 L130 194 L120 219 L96 234 L70 219 L62 195 L72 173Z',bodyFill,edge)
    +p('M94 159 L93 187 L68 196 L74 175Z',m[3])+p('M95 189 L127 195 L118 218 L96 231Z',m[2])
    +p('M95 180 L111 195 L96 212 L81 196Z',m[1],edge)+line('M83 194 L94 183',m[3],2);
  }else if(style==='wraps'){
   ay=222;
   piece+=p('M77 132 Q96 124 115 133 L112 191 L118 284 L111 319 H79 L73 283 L80 191Z',bodyFill,edge);
   for(let i=0;i<11;i++){
    const y=130+i*16,w=[17,18,19,20,21,21,20,18,17,18,19][i],l=96-w,r=96+w;
    piece+=p(`M${l} ${y+7} Q93 ${y+7} ${r} ${y-2} L${r-1} ${y+14} Q95 ${y+26} ${l+1} ${y+22}Z`,bodyFill,edge)+p(`M${l+2} ${y+9} Q87 ${y+11} 95 ${y+8} L93 ${y+21} Q84 ${y+23} ${l+3} ${y+20}Z`,light.mid)+p(`M105 ${y+5} L${r-1} ${y} L${r-2} ${y+13} L102 ${y+19}Z`,m[2])+line(`M${l+3} ${y+8} Q92 ${y+11} ${r-3} ${y+1}`,light.key,1.1)+line(`M${l+3} ${y+22} Q96 ${y+25} ${r-2} ${y+14}`,light.deep,2);
   }
   piece+=p('M78 144 L85 142 L107 298 L100 307Z',q[1],edge)+line('M80 146 L103 297',q[3],1.2)+p('M82 146 Q68 133 74 129 Q83 126 88 142 Q96 126 102 133 Q105 143 88 148 L96 172 L86 166 L84 150 L74 167 L71 158Z',q[1],edge)+line('M77 133 L84 143 M98 135 L90 143',q[3],1.5);
  }else if(style==='shin-guards'){
   ay=218;
   piece+=band(163,25)+band(274,22)+p('M73 153 Q96 137 120 153 L112 281 L99 313 L80 291Z',bodyFill,edge)+p('M76 157 L94 149 L91 275 L83 293Z',m[3])+p('M98 150 L117 156 L109 280 L99 309 L96 275Z',m[2])+stud(83,164)+stud(107,164);
  }else{
   const heavy=style==='heavy-greaves',full=style==='full-plate',mail=style==='mail-greaves';
   const top=full?83:116;
   piece+=p(`M72 ${top+12} Q96 ${top-7} 121 ${top+12} L118 173 Q126 209 116 257 L111 291 L118 314 Q96 331 73 314 L81 291 L73 255 Q65 210 76 172Z`,bodyFill,edge)
    +p(`M76 ${top+14} L93 ${top+4} L88 168 L85 212 L93 286 L85 313 L76 312 L84 287 L76 215 L81 168Z`,m[3]);
   if(mail){
    for(let y=139;y<302;y+=13)piece+=line(`M79 ${y} q5 6 10 0 q5 6 10 0 q5 6 10 0`,m[2],2)+line(`M81 ${y-1} q4 4 8 0 M101 ${y-1} q4 4 8 0`,m[3],1.4);
    piece+=band(123)+band(290,22);
   }else{
    piece+=p('M96 182 L114 202 L107 279 L97 310 L84 281 L79 205Z',m[1],edge)+p('M96 185 L95 282 L97 305 L86 279 L82 207Z',m[3])+p('M99 187 L112 205 L105 276 L98 299Z',m[2]);
    piece+=p('M96 139 L120 158 L115 178 L96 194 L75 177 L71 159Z',m[1],edge)+p('M94 143 L94 165 L76 174 L75 159Z',m[3])+p('M98 165 L117 158 L113 177 L96 191Z',m[2]);
    if(style==='plate-greaves'||full)for(const y of [207,231,255])piece+=line(`M80 ${y} Q96 ${y+13} 112 ${y}`,f[1],3)+line(`M83 ${y+1} Q94 ${y+10} 106 ${y+3}`,f[3],1.4);
    if(heavy)piece+=p('M71 133 L96 119 L124 135 L131 163 L117 190 L94 201 L68 182 L61 156Z',m[1],edge)+p('M73 137 L94 124 L93 157 L70 177 L65 156Z',m[3])+p('M97 158 L126 157 L115 185 L95 197Z',m[2])+band(265,24);
    if(full)piece+=p('M70 87 Q96 71 122 87 L117 132 L96 150 L72 132Z',m[1],edge)+p('M73 89 L91 81 L87 127 L76 134Z',m[3])+line('M75 109 Q96 118 118 107 M75 126 Q96 137 117 124',f[1],2.5);
    piece+=band(302,22)+stud(81,202)+stud(107,202);
   }
  }
  art+=`<g>${piece}</g><g transform="${style==='tassets'?'translate(270 0) scale(-1 1)':'translate(78 0)'}">${piece}</g>`;anchor=[96,ay,style==='knee-guards'?.4:.48];
 }
 // Paint transitions stay on individual facets; pigment is clipped to the item.
 const shade=(name,stops)=>`<linearGradient id="${id}-${name}" x1="0" y1="0" x2=".85" y2="1">${stops.map(([o,c])=>`<stop offset="${o}" stop-color="${c}"/>`).join('')}</linearGradient>`;
 defs=defs.replace('</defs>',shade('leg-lit',[[0,light.key],[.35,m[3]],[.72,light.mid],[1,m[1]]])+shade('leg-dark',[[0,light.bounce],[.48,m[2]],[1,light.deep]])+shade('leg-mid',[[0,light.mid],[.38,m[1]],[1,light.deep]])+'</defs>');
 for(const [color,name] of [[m[1],'leg-mid'],[m[3],'leg-lit'],[m[2],'leg-dark']])art=art.split(`fill="${color}"`).join(`fill="url(#${id}-${name})"`);
 // Use a mask so nested paired transforms and curved silhouettes remain intact.
 const silhouette=art.replace(/fill="[^"]*"/g,'fill="#fff"').replace(/stroke="[^"]*"/g,'stroke="none"');
 defs+=`<defs><mask id="${id}-leg-grain" maskUnits="userSpaceOnUse" x="50" y="60" width="170" height="290">${silhouette}</mask></defs>`;
 let grain='',seed=173;
 const random=()=>{seed=(seed*1664525+1013904223)>>>0;return seed/4294967296;};
 for(let i=0;i<360;i++){
  const x=60+random()*153,y=78+random()*250,w=1+random()*5,h=1+random()*3;
  grain+=p(`M${x} ${y} l${w} ${-h*.3} l${w*.2} ${h} l${-w*.8} ${h*.4}Z`,i%3?light.key:light.deep,`opacity="${i%3?.055:.09}"`);
 }
 art+=`<g mask="url(#${id}-leg-grain)" pointer-events="none">${grain}</g>`;
 return {art:`${defs}<g data-legwear-style="${style}">${art}</g>`,anchor,paired};
}
