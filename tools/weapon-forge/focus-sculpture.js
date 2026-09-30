// Recognizable constructions for named focuses, before shared paint and affixes.
export function sculptedFocus(item,m,q,f,{p,line,circ}){
 const edge='stroke="#262326" stroke-width="2.2" stroke-linejoin="round"';
 if(item.shape==='staff'){
  const shape='M120 362 L121 146 Q92 124 93 94 Q92 60 119 49 Q146 39 164 58 Q181 76 165 100 L149 113 L141 136 L140 362Z';
  let art=p(shape,m[1],edge)
   +p('M121 51 Q97 67 102 96 Q104 118 128 137 L129 348 L123 360 L124 137 Q91 112 98 85 Q99 63 121 51Z',m[3])
   +p('M143 48 Q177 56 172 83 Q171 100 150 115 L141 139 L140 361 L133 357 L134 133 L147 105 Q169 87 157 67Z',m[2])
   +p('M122 69 Q139 58 151 74 Q157 87 142 98 L129 112 Q111 104 111 87 Q111 75 122 69Z','#252326',edge)
   +p('M125 76 L139 70 L150 85 L138 103 L124 96 L117 85Z',f[1],edge)
   +p('M125 76 L138 72 L135 86 L119 86Z',f[3])+p('M136 86 L148 84 L138 101 L127 96Z',f[2])
   +line('M112 83 Q105 109 128 122 M153 101 L141 122',f[1],4)
   +p('M118 136 Q131 143 146 135 L145 152 Q131 159 118 152Z',f[1],edge)
   +line('M120 139 Q132 145 143 140',f[3],2.5)
   +p('M119 252 H142 L141 323 L120 327Z',q[1],edge);
  for(let y=259;y<319;y+=11)art+=p(`M120 ${y} Q128 ${y-5} 141 ${y-4} V${y+2} L120 ${y+7}Z`,q[2])+line(`M122 ${y+1} L132 ${y-2}`,q[3],2);
  art+=p('M117 248 H144 V258 H118Z M118 322 H143 V336 L134 342 L120 337Z',f[1],edge)+line('M120 250 H140 M121 325 H139',f[3],2.5)
   +p('M123 347 L140 347 L140 362 L133 370 L122 362Z',f[2],edge)+line('M125 349 V359',f[3],2);
  return {art,shape,anchor:[132,220,.7]};
 }
 if(item.shape!=='idol'||! /skull|head/i.test(item.name))return null;
 const skull=/skull/i.test(item.name);
 const shape='M84 145 C79 99 97 76 134 73 C173 69 193 98 186 145 L179 178 L171 213 Q165 240 136 248 Q106 245 96 217 L87 180Z';
 let art='';
 if(!skull)art+=p('M89 120 Q67 172 77 213 L65 258 L90 244 L85 274 L105 252 L113 227 L160 227 L169 269 L180 244 L199 257 L187 211 Q203 155 177 101Z',q[2],edge);
 art+=p(shape,m[1],edge)
  +p('M92 125 Q90 91 123 81 L137 80 L119 101 L109 137 L115 160 L99 177 L94 163Z',m[3])
  +p('M151 77 Q184 88 184 124 L176 154 L169 177 L174 207 L161 228 L141 241 L143 214 L154 191 L148 167 L162 142 L164 107Z',m[2])
  +p('M94 143 Q110 130 125 147 L121 169 Q101 176 94 159Z',m[2],edge)
  +p('M143 146 Q161 132 176 141 L173 160 Q160 173 146 164Z','#232226',edge)
  +line('M96 139 Q111 132 123 142',m[3],3)
  +p('M134 147 L125 182 L137 188 L145 179Z',m[3])+p('M134 159 L134 185 L145 179Z',m[2])
  +p('M96 179 L111 172 L121 184 L109 199 L99 195Z',m[3],'opacity=".55"')
  +p('M111 205 Q135 213 158 203 L155 217 Q133 228 114 217Z',m[2],edge);
 if(skull){for(let x=116;x<154;x+=8)art+=p(`M${x} 208 L${x+6} 209 V218 L${x+1} 220Z`,m[3]);}
 else {
  art+=line('M99 153 L118 157 M150 153 L171 150',q[1],3);
  for(const x of [115,126,138,150])art+=line(`M${x} 203 L${x+2} 218`,q[3],2);
  art+=line('M126 81 Q119 49 131 42 Q155 38 145 76',q[2],5)+line('M127 79 Q123 48 134 45',q[3],1.8);
  art+=line('M122 234 Q119 264 105 283 M140 239 Q145 269 136 296 M153 231 Q167 255 172 282',q[2],4);
  art+=circ(106,280,5,f[1],edge)+circ(137,290,5,f[1],edge)+circ(171,277,5,f[1],edge);
 }
 art+=p('M106 231 Q135 253 166 230 L162 241 Q135 262 110 242Z',q[1],edge)+line('M111 235 Q135 254 160 235',q[3],2);
 return {art,shape,anchor:[136,246,.43]};
}
