// Body-specific construction, before material paint and affix placement.
export function torsoConstruction(item,m,q,f,{p,line,circ}){
 const edge='stroke="#252326" stroke-width="2" stroke-linejoin="round"';
 const pin=(x,y)=>circ(x,y,3,f[2])+circ(x-.7,y-.9,1.3,f[3]);
 if(item.shape==='coat'){
  const coat=item.shape==='coat',hem=coat?321:304;
  // A hollow garment: back neck, sleeve tubes, then the hanging front panels.
  let art=p(`M101 91 Q133 77 167 93 L181 173 L180 ${hem} Q135 ${hem+15} 87 ${hem} L90 176Z`,m[2],edge);
  art+=p('M101 95 Q131 82 166 95 L158 119 Q133 130 108 117Z',q[2]);
  art+=p('M102 94 Q82 92 74 111 L48 177 L46 190 Q58 205 75 207 L98 157 L111 117Z',m[1],edge)
   +p('M164 93 Q188 94 197 116 L224 184 L222 197 L201 209 L178 162 L155 119Z',m[1],edge);
  art+=p('M81 111 Q91 101 101 101 L92 137 Q82 157 67 184 L53 184 Q67 148 74 126Z',m[3],'opacity=".48"')
   +p('M91 133 Q82 162 77 187 L73 196 L64 193 Q74 173 74 161Z',m[2])
   +p('M186 111 Q206 147 216 184 L209 194 L197 174 L181 148Z',m[2]);
  art+=p('M47 187 Q62 197 77 196 L72 209 Q55 209 45 197Z M200 197 L221 186 L226 197 L205 210Z',q[1],edge)
   +p('M48 199 Q60 208 72 205 L70 211 Q54 211 47 203Z M207 208 L224 198 L223 204 L211 213Z',q[2]);
  art+=p(`M105 104 Q117 121 133 127 L130 219 L125 ${hem+5} Q105 ${hem+10} 84 ${hem-2} Q95 260 94 209 Q88 170 105 104Z`,m[1],edge)
   +p(`M163 104 Q149 122 136 127 L136 220 L146 ${hem+5} Q172 ${hem+5} 189 ${hem-4} Q177 266 177 207 Q181 166 163 104Z`,m[1],edge);
  art+=p(`M107 126 Q96 157 100 181 Q113 197 112 219 L99 ${hem-5} L111 ${hem} Q124 265 119 227 Q108 190 114 159Z`,m[3],'opacity=".38"')
   +p(`M164 129 Q173 163 162 193 Q153 220 164 248 L180 ${hem-7} L165 ${hem} L151 251 Q146 227 157 198Z`,m[2])
   +p(`M129 133 L135 128 L140 ${hem-9} L132 ${hem-24} L126 ${hem+3}Z`,q[2]);
  art+=p('M104 95 L127 116 L130 164 L111 144 L119 133Z M164 95 L143 117 L140 162 L160 143 L152 132Z',q[1],edge)
   +line('M106 98 L123 118 L125 145 M162 99 L147 120',q[3],1.3);
  // Fold compression follows the belt, not an arbitrary polygon grid.
  art+=p('M98 199 L119 218 L98 211Z M102 239 L120 230 L116 250Z M155 211 L174 198 L171 214Z M157 232 L176 238 L172 248Z',m[2]);
  art+=p('M93 219 Q133 228 178 218 L180 233 Q133 245 92 234Z',q[1],edge)
   +line('M96 222 Q130 231 173 222',q[3],1)
   +p('M123 225 H145 V239 H123Z',f[2],edge)+p('M125 226 H143 V236 H125Z',f[1])+p('M129 228 H140 V234 H129Z',q[2])+line('M134 229 L145 230',f[3],1.5);
  art+=line(`M91 ${hem-4} Q106 ${hem+3} 121 ${hem} M150 ${hem} Q169 ${hem+1} 182 ${hem-6}`,q[2],2);
  for(const y of [165,181,197])art+=pin(137,y);
  return art;
 }
 if(item.shape==='chestplate'){
  let art=p('M94 101 Q134 79 174 101 L188 165 L178 257 L195 300 Q137 333 76 303 L88 256 L80 163Z',m[2],edge);
  // Rounded shoulder caps overlap a shaped breast shell.
  art+=p('M94 97 Q65 101 54 127 L43 165 Q59 186 84 184 L98 148Z',m[1],edge)+p('M95 101 Q69 105 60 130 L55 158 L46 163 Q48 126 71 109Z',m[3]);
  art+=p('M173 98 Q202 104 211 128 L225 166 Q211 183 188 184 L172 147Z',m[1],edge)+p('M178 103 Q199 110 205 129 L213 158 L223 163 L210 130 Q199 104 178 103Z',m[2]);
  for(const y of [148,163])art+=line(`M49 ${y} Q64 ${y+17} 85 ${y+12} M187 ${y+12} Q206 ${y+14} 220 ${y}`,f[2],4)+line(`M52 ${y} Q63 ${y+11} 79 ${y+11}`,f[3],1.3);
  art+=p('M94 99 Q134 120 174 99 Q167 126 181 160 Q185 188 169 230 L135 253 L100 232 Q84 203 87 167 Q102 128 94 99Z',m[1],edge);
  art+=p('M97 114 Q113 127 129 129 L130 180 Q113 189 102 218 Q91 196 94 166 Q103 139 97 114Z',m[3])+p('M138 131 Q156 124 171 113 Q167 143 178 165 Q180 199 166 225 L138 247 Q151 210 145 178Z',m[2]);
  art+=p('M130 126 L137 127 L140 211 L135 251 L129 223 L132 178Z',m[3])+line('M96 103 Q134 126 173 103',f[2],6)+line('M98 102 Q133 123 170 102',f[3],2);
  for(let i=0;i<3;i++){const y=248+i*19,l=88-i*5,r=181+i*5;art+=p(`M${l} ${y} Q135 ${y+19} ${r} ${y} L${r+3} ${y+17} Q137 ${y+43} ${l-4} ${y+19}Z`,m[1],edge)+p(`M${l+3} ${y+4} Q110 ${y+14} 130 ${y+15} L128 ${y+29} Q103 ${y+27} ${l-1} ${y+17}Z`,m[3])+line(`M${l-1} ${y+19} Q136 ${y+41} ${r} ${y+17}`,f[2],3);}
  art+=pin(95,247)+pin(176,247)+pin(89,286)+pin(182,286);
  return art;
 }
 return null;
}

export function garmentDetails(shape,m,q,f,{p,line,circ}){
 if(!['coat','tunic','mail'].includes(shape))return '';
 let art=p('M106 102 Q134 88 165 102 Q158 124 135 133 Q114 127 106 102Z',q[2])+line('M108 104 Q134 98 162 104',q[3],2)+line('M106 107 Q134 139 165 106',m[2],4);
 if(shape==='coat')art+=p('M108 111 L123 124 L125 161 L109 148 L118 139Z M161 111 L146 126 L141 165 L161 146 L152 138Z',q[1])+line('M110 113 L120 126 L121 151 M158 114 L148 129',q[3],2);
 if(shape!=='mail'){
  art+=p('M98 161 Q108 181 104 208 L97 223 Q100 192 94 175Z M163 169 Q156 190 162 216 L173 224 Q163 193 170 180Z',m[2])+p('M98 245 Q118 252 119 271 L111 299 L105 295 Q113 264 98 245Z',m[3],'opacity=".45"');
  art+=line('M104 185 Q114 190 122 186 M149 185 Q160 190 170 184',q[2],3)+line('M105 183 Q114 187 121 184',q[3],1.3);
 }else{
  for(let row=0;row<8;row++)for(let col=0;col<7;col++){const x=103+col*10+(row%2?3:0),y=145+row*10;art+=line(`M${x} ${y} q-3 6 2 7 q5 0 4 -6`,m[2],2)+line(`M${x} ${y} q3 -2 5 1`,m[3],1);}
 }
 return art;
}
