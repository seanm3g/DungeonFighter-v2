// Construction replacements for the weakest silhouettes in the reference review.
export function redesignedRelic(item,m,q,f,{p,line,circ}){
 const edge='stroke="#252326" stroke-width="2.2" stroke-linejoin="round"';
 const pin=(x,y)=>circ(x,y,3,f[2])+circ(x-.8,y-1,1.5,f[3]);
 const grip=(x,top,bottom)=>{
  let a=p(`M${x-12} ${top} H${x+12} L${x+10} ${bottom} H${x-11}Z`,q[1],edge)+p(`M${x+4} ${top} H${x+11} V${bottom} H${x+3}Z`,q[2]);
  for(let y=top+9;y<bottom;y+=11)a+=p(`M${x-11} ${y} L${x+11} ${y-4} V${y+2} L${x-11} ${y+6}Z`,q[2])+line(`M${x-9} ${y+1} L${x} ${y-2}`,q[3],1.6);
  return a+p(`M${x-14} ${top-6} H${x+14} V${top+4} H${x-14}Z M${x-13} ${bottom-2} H${x+13} V${bottom+9} L${x} ${bottom+15} L${x-13} ${bottom+9}Z`,f[1],edge)+line(`M${x-10} ${top-4} H${x+10} M${x-9} ${bottom} H${x+9}`,f[3],2);
 };
 if(item.name==='Pounder'){
  // A compact octagonal striking weight, carried by a deep socket and stout haft.
  const shape='M88 82 L111 65 L151 69 L175 89 L168 156 L145 179 L142 204 L140 347 H117 L116 201 L109 179 L86 159 L80 106Z';
  let art=p('M116 168 L144 168 L140 346 L119 351 L115 339Z',q[1],edge)
   +p('M133 177 L142 173 L138 341 L132 349 L128 338Z',q[2])+line('M119 201 L121 244',q[3],2.4)
   +p('M88 82 L111 65 L151 69 L175 89 L168 156 L145 179 L109 179 L86 159 L80 106Z',m[1],edge)
   +p('M89 83 L112 68 L150 72 L172 89 L150 101 L105 99Z',m[3])
   +p('M82 106 L90 87 L105 100 L106 155 L116 174 L109 177 L88 158Z',m[3])
   +p('M151 101 L173 91 L166 154 L143 176 L135 162 L146 147Z',m[2])
   +p('M108 102 L147 104 L142 146 L133 163 L110 153Z',m[1])
   +p('M108 103 L119 105 L116 144 L123 157 L111 153Z',m[3],'opacity=".4"')
   +p('M109 155 L135 164 L164 153 L144 178 L110 177 L90 158Z',m[2])
   +p('M86 92 L103 104 L150 107 L174 96 L173 107 L152 120 L101 116 L82 103Z',f[1],edge)
   +p('M102 108 L149 111 L171 101 L170 109 L151 119 L102 115Z',f[2])
   +line('M88 96 L104 108 L147 112 L169 101',f[3],2)
   +p('M85 145 L107 157 L140 161 L168 146 L167 158 L144 176 L108 171 L87 157Z',f[1],edge)
   +line('M89 148 L108 160 L139 164 L164 151',f[3],2)
   +pin(112,110)+pin(143,113)+pin(111,165)+pin(151,161)
   +p('M110 179 H144 L144 199 L137 214 H120 L112 201Z',f[2],edge)
   +p('M114 180 H124 L126 206 H120 L115 197Z',f[1])+line('M116 183 L119 197',f[3],2)
   +grip(129,248,330);
  return {art,shape,anchor:[129,228,.52]};
 }
 if(item.shape==='club'){
  const shape='M117 352 L116 184 L94 165 L81 123 L88 80 L108 63 L147 64 L165 87 L168 128 L153 167 L139 184 L140 352Z';
  let art=p(shape,m[1],edge)+p('M109 66 L100 87 L98 119 L109 154 L124 178 L123 342 L118 348 L120 181 L96 163 L83 122 L90 81Z',m[3])+p('M143 67 L162 89 L165 127 L151 165 L137 180 L138 348 L131 351 L129 178 L141 153 L149 117Z',m[2])
   +p('M86 91 L99 85 L158 92 L164 105 L101 99 L84 104Z',f[1],edge)+line('M89 92 L102 89 L157 95',f[3],2)
   +p('M96 150 L109 155 L155 145 L151 161 L123 171 L103 163Z',f[1],edge)+line('M104 155 L122 161 L151 151',f[3],2)+pin(102,94)+pin(149,100)+pin(114,161)
   +p('M114 180 H142 V193 H116Z',f[2],edge)+line('M117 182 H140',f[3],2)+grip(129,260,329);
  return {art,shape,anchor:[129,237,.65]};
 }
 if(item.shape==='flail'){
  const shape='M61 88 L83 66 L113 70 L134 91 L136 121 L115 144 L82 148 L58 125Z M152 205 H172 L166 351 H146Z';
  let art=p('M152 209 L170 208 L165 350 L146 350Z',m[1],edge)+p('M164 213 H169 L164 348 H156Z',m[2])+line('M155 217 L150 262',m[3],2)+grip(157,267,334);
  // Visible overlapping chain links, rather than a curved wire.
  const links=[[120,138,-30],[135,149,-36],[147,161,-32],[155,175,-18],[158,191,0],[159,207,8]];
  for(const[x,y,angle]of links)art+=`<g transform="rotate(${angle} ${x} ${y})">${line(`M${x-4} ${y-6} C${x-10} ${y-14} ${x+7} ${y-17} ${x+6} ${y-6} L${x+5} ${y+5} C${x+4} ${y+14} ${x-9} ${y+13} ${x-6} ${y+3}Z`,f[2],6)}${line(`M${x-4} ${y-7} Q${x} ${y-14} ${x+4} ${y-7} L${x+3} ${y+4}`,f[3],2)}</g>`;
  // A spherical iron weight with overlapping ribs and socketed, faceted spikes.
  art+=p('M117 125 L131 141 L123 150 L109 139Z',f[2],edge)
   +circ(120,143,7,'none',`stroke="${f[1]}" stroke-width="4"`)
   +p('M64 90 Q68 65 98 66 Q130 68 139 97 Q149 125 126 144 Q102 163 74 144 Q54 132 57 110Z',m[1],edge)
   +p('M65 94 Q74 70 97 70 Q112 71 123 82 L105 80 L83 90 L74 108 L71 129 L61 122 Q59 107 65 94Z',m[3])
   +p('M120 80 Q141 93 140 115 Q139 141 113 149 L88 148 L98 135 L116 128 L125 109Z',m[2])
   +p('M88 68 L99 68 Q81 105 98 147 L87 148 Q70 106 88 68Z',f[2])
   +p('M89 69 L94 69 Q78 105 93 146 L88 144 Q73 105 89 69Z',f[1])
   +p('M61 109 Q94 117 137 100 L140 111 Q97 130 59 120Z',f[2])
   +line('M63 110 Q97 120 136 103',f[3],2.2)
   +pin(88,113)+pin(128,108);
  // A regular six-position ring. The lower-right chain eye sits between spikes.
  for(const degrees of [-90,-30,30,90,150,210]){
   const a=degrees*Math.PI/180,dx=Math.cos(a),dy=Math.sin(a);
   const bx=99+dx*38,by=109+dy*38,tx=99+dx*57,ty=109+dy*57;
   const lx=bx-dy*5.5,ly=by+dx*5.5,rx=bx+dy*5.5,ry=by-dx*5.5;
   art+=p(`M${lx} ${ly} L${tx} ${ty} L${rx} ${ry} L${bx-dx*3} ${by-dy*3}Z`,m[2],edge);
   // Pick the illuminated face in image space, independently of spike rotation.
   const litLeft=(lx+ly)<(rx+ry),hx=litLeft?lx:rx,hy=litLeft?ly:ry;
   art+=p(`M${hx} ${hy} L${tx} ${ty} L${bx} ${by}Z`,m[3]);
  }
  // Short front-facing studs read as cones seen end-on, not sideways horns.
  for(const [x,y] of [[89,91],[116,105],[94,130]]){
   art+=circ(x,y,6,m[2])+p(`M${x-5} ${y} L${x-2} ${y-8} L${x+5} ${y+1} L${x} ${y+5}Z`,m[1],edge)
    +p(`M${x-5} ${y} L${x-2} ${y-8} L${x} ${y+1}Z`,m[3]);
  }
  return {art,shape,anchor:[157,247,.55]};
 }
 if(item.shape==='book'){
  const shape='M70 112 L182 91 L211 111 L204 310 L95 334 L65 311Z';
  let art=p('M78 118 L184 98 L204 115 L198 304 L94 325 L76 308Z',q[2],edge)
   +p('M93 128 L200 108 L196 301 L92 324Z','#b8aa8c',edge)
   +line('M100 308 L193 290 M100 315 L191 297 M195 126 L192 279','#716856',1.5)
   +p('M68 108 L181 87 L211 106 L205 118 L96 139 L68 123Z',m[1],edge)+p('M70 110 L181 90 L203 104 L95 127Z',m[3])
   +p('M68 116 L95 131 L92 329 L65 313Z',q[1],edge)+p('M80 125 L92 132 L90 324 L78 317Z',q[2])
   +p('M96 134 L204 113 L200 310 L92 334Z',m[1],edge)+p('M185 119 L202 116 L198 308 L93 331 L94 322 L183 302Z',m[2])
   +p('M107 149 L187 133 L183 290 L103 307Z',q[2],edge)+p('M115 157 L177 145 L174 280 L111 294Z',m[1])
   +line('M115 162 L172 151 M115 164 L112 276',m[3],2)
   +p('M97 134 L123 129 L119 140 L105 145 L103 163 L96 165Z M179 119 L204 113 L203 140 L194 141 L194 126 L181 129Z M93 310 L102 308 L104 321 L120 317 L120 328 L92 334Z M178 303 L190 300 L192 283 L201 282 L199 311 L178 315Z',f[1],edge)
   +line('M72 159 L92 171 M70 272 L91 282',f[1],5)
   +p('M148 183 L166 208 L146 239 L127 216Z',f[1],edge)+p('M147 190 L153 208 L145 232 L134 215Z',f[2])
   +p('M179 230 L209 223 L210 241 L180 249Z',q[1],edge)+pin(193,235);
  return {art,shape,anchor:[145,265,.48]};
 }
 if(item.shape==='obelisk'){
  const shape='M134 58 L162 117 L170 272 L196 292 L192 317 L78 325 L73 302 L101 277 L108 121Z';
  let art=p('M78 298 L126 279 L193 290 L194 316 L81 325 L72 313Z',f[2],edge)
   +p('M76 299 L129 283 L192 292 L171 305 L90 314Z',f[1])+line('M80 301 L91 310 L170 302',f[3],2)
   +p('M134 58 L162 117 L170 280 L134 301 L101 281 L108 121Z',m[1],edge)
   +p('M133 62 L128 128 L122 275 L134 299 L104 280 L110 122Z',m[3])
   +p('M134 64 L161 118 L168 280 L135 298 L138 262 L144 127Z',m[2])
   +p('M127 151 L135 134 L141 153 L139 219 L129 235 L124 216Z',f[2],edge)
   +line('M133 151 L130 169 L135 179 L130 193 L133 214',f[3],2)
   +p('M102 254 L119 264 L116 282 L133 296 L135 282 L154 271 L167 254 L171 281 L136 306 L99 284Z',f[1],edge)
   +line('M105 260 L119 270 M140 292 L166 277',f[3],2)+pin(109,276);
  return {art,shape,anchor:[136,259,.42]};
 }
 if(item.shape==='amulet'&&item.family==='wand'){
  const shape='M134 179 Q174 188 180 229 Q179 269 137 291 Q94 279 87 240 Q82 200 116 184Z';
  let art=line('M123 185 C63 136 74 73 121 71 C176 63 198 114 144 183',q[2],4)+line('M120 74 C82 77 78 120 101 148',q[3],1.5)
   +p('M121 174 Q134 165 147 174 V195 H121Z',f[1],edge)+p('M127 175 Q134 172 141 175 V185 H127Z',f[2])
   +p(shape,f[1],edge)+p('M121 185 Q92 197 93 231 Q92 258 118 276 L108 270 Q83 247 91 215 Q98 191 121 185Z',f[3])
   +p('M141 189 Q176 200 176 233 Q173 263 138 286 L139 272 Q159 249 160 223Z',f[2])
   +p('M130 197 Q158 195 166 224 Q169 253 137 276 Q104 264 100 240 Q95 210 130 197Z',m[1],edge)
   +p('M126 201 L139 200 L123 218 L111 239 L102 239 Q100 216 126 201Z',m[3])
   +p('M144 201 Q169 216 162 241 L137 273 L138 250 L149 230Z',m[2])
   +line('M118 227 Q140 214 149 232 Q141 244 126 241 Q121 233 134 231',f[1],2.5)
   +p('M104 260 L114 263 L117 277 L112 279Z M158 254 L168 250 L164 264 L156 270Z',f[1],edge);
  art+=p('M104 252 Q111 266 137 271 Q154 257 162 242 L160 254 Q151 273 137 280 Q116 274 106 264Z',m[2],'opacity=".6"')
   +line('M101 221 Q102 208 113 204 M101 225 L102 233',m[3],1.5)
   +p('M124 183 Q134 188 144 182 L143 194 Q135 201 126 195Z',f[2])
   +line('M126 185 L127 191 Q131 195 137 193',f[3],1.3)
   +line('M93 101 Q87 115 95 132',q[3],.8);
  return {art,shape,anchor:[135,279,.36]};
 }
 if(item.shape==='idol'&&!/skull|head/i.test(item.name)){
  const shape='M104 85 L161 84 L176 111 L166 159 L157 179 L174 199 L164 250 L170 290 L155 311 L109 312 L95 293 L102 252 L89 204 L106 180 L95 153 L92 113Z';
  let art=p(shape,m[1],edge)+p('M105 89 L125 88 L113 112 L117 150 L127 172 L115 191 L107 213 L118 246 L111 290 L123 307 L109 308 L98 291 L105 252 L92 205 L110 179 L98 150 L95 113Z',m[3])
   +p('M145 88 L160 87 L172 112 L162 155 L153 179 L170 200 L159 249 L166 290 L153 307 L137 308 L141 278 L134 249 L146 219 L142 188 L146 155Z',m[2])
   +p('M103 117 L125 125 L121 142 L105 136Z M141 125 L163 117 L159 136 L143 141Z',q[2],edge)
   +p('M133 126 L127 153 L138 155Z',m[3])+line('M118 165 Q132 172 147 164',q[2],3)
   +p('M102 183 L120 194 L132 222 L145 194 L164 181 L169 197 L153 219 L137 236 H126 L109 221 L94 201Z',f[1],edge)
   +p('M116 206 L129 226 L135 224 L149 202 L153 216 L137 239 H124 L108 219Z',f[2])
   +p('M108 261 L131 255 L156 262 L158 285 L135 297 L108 287Z',q[2],edge)
   +line('M116 269 L130 264 L149 269 M117 280 L130 275 L148 280',m[3],2)
   +p('M99 293 L126 302 H154 L167 290 L171 309 L155 320 H107 L94 309Z',f[1],edge)+line('M101 301 L110 313 H151',f[3],2);
  return {art,shape,anchor:[132,246,.44]};
 }
 return null;
}
