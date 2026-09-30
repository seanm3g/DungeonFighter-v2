// Painted construction planes: distinct head, neck, grip and end cap.
export function clubArt(m,q,f,{p,line,circ}) {
 const outline='stroke="#252326" stroke-width="2.5" stroke-linejoin="round"';
 let art=p('M116 338 L119 185 Q104 162 97 127 Q87 77 113 61 Q139 46 153 72 Q165 102 145 159 L139 188 L140 338Z',m[1],outline)
  +p('M114 64 Q99 77 103 116 L115 157 L124 182 L127 135 L119 98 L127 62Z',m[3])
  +p('M139 61 Q159 71 155 102 L139 157 L135 185 L138 334 L130 337 L128 178 L133 134 L140 96Z',m[2])
  +p('M106 81 L115 70 L112 100 L118 130 L115 142 L105 114Z',m[1],'opacity=".5"')
  +p('M117 179 L141 179 L143 191 L116 191Z',f[1],outline)
  +line('M119 181 H138',f[3],2.5)
  +p('M118 265 H141 L140 328 L119 329Z',q[1],outline)
  +p('M134 267 H140 V327 H133Z',q[2]);
 for(let y=272;y<324;y+=11)art+=p(`M119 ${y} Q130 ${y-5} 140 ${y-3} V${y+2} Q129 ${y+1} 119 ${y+6}Z`,q[2])+line(`M121 ${y+1} L129 ${y-1}`,q[3],1.8);
 return art+p('M116 258 H143 V269 H117Z M117 326 H142 L143 339 L135 345 H122 L116 337Z',f[1],outline)
  +p('M135 328 H141 L142 338 L135 344 H127 L132 337Z',f[2])
  +line('M119 260 H138 M119 328 H136',f[3],2.5)
  +circ(126,184,2,f[2])+circ(125.5,183.5,.8,f[3]);
}
