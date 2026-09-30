// Broad painted bevels and restrained bronze hardware, shared by body armor.
// Keep this underneath affix attachments so equipment identity stays readable.
export function armorFinish(shape,m,q,{p,line,circ}) {
 const rim='#917343',rimLight='#c2ab7b',rimShadow='#493c2a';
 const bevel=(d)=>line(d,'#242321',7)+line(d,rim,4)+line(d,rimLight,1.2);
 const stud=(x,y)=>circ(x,y,3.5,rimShadow)+circ(x-0.7,y-0.8,2.2,rim)+circ(x-1.1,y-1.4,.8,rimLight);
 if(shape==='chestplate')return p('M94 117 L126 143 L129 204 L112 235 L93 217 L88 176Z',m[3],'opacity=".36"')
  +p('M135 143 L162 122 L176 176 L165 224 L138 247 L145 187Z',m[2])
  +p('M128 143 L135 142 L138 247 L130 260 L125 220Z',m[3],'opacity=".65"')
  +bevel('M93 99 Q132 136 172 99 M52 164 L78 178 M190 177 L217 163')
  +p('M85 254 Q132 276 182 254 L184 268 Q134 289 82 268Z',q[2])
  +line('M86 256 Q133 278 180 256',q[3],3)
  +bevel('M81 299 Q132 326 187 298')
  +[ [83,132],[185,132],[95,289],[171,289] ].map(([x,y])=>stud(x,y)).join('');
 if(['coat','tunic','mail'].includes(shape)){
  const long=shape==='coat',bottom=long?312:299;
  return p(`M104 144 L117 161 L109 219 L115 ${bottom} L103 ${bottom-5} L99 220Z`,m[3],'opacity=".3"')
   +p(`M162 145 L153 204 L163 ${bottom} L173 ${bottom-3} L166 211Z`,m[2])
   +p('M74 135 L89 153 L72 183 L61 180Z',m[3],'opacity=".32"')
   +line(`M103 111 L119 135 M153 134 L165 111`,q[2],7)
   +line(`M103 109 L119 132 M153 131 L165 109`,q[3],2.5)
   +p('M103 221 Q134 229 171 221 L172 236 Q134 245 104 236Z',q[2])
   +line('M105 222 Q134 230 169 222',q[3],2.5)
   +bevel(`M89 ${bottom} L108 ${bottom+3} M153 ${bottom+3} L179 ${bottom}`)
   +stud(112,227)+stud(161,227);
 }
 if(shape==='greaves')return [0,78].map(dx=>`<g transform="translate(${dx} 0)">${p('M98 159 L102 169 L103 290 L97 307 L92 290Z',m[3],'opacity=".5"')}${bevel('M78 106 L97 112 L124 105 M81 306 L97 316 L124 306')}${stud(88,124)}${stud(116,124)}</g>`).join('');
 if(shape==='trousers')return p('M105 121 L112 165 L104 199 L109 278 L103 303 L100 207 L104 164Z',m[3],'opacity=".28"')+p('M156 120 L151 165 L161 207 L154 303 L165 276 L167 205 L159 161Z',m[2])+line('M90 112 Q135 123 178 112',q[3],4);
 return '';
}
