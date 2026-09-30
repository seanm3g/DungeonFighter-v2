// Authored food and apothecary paintings; no equipment planes or metallic trim.
export function consumableArt(item,{p,line,circ,motif}){
 const outline='stroke="#342d2c" stroke-width="2.2" stroke-linejoin="round"';
 const shape=(d,c)=>p(d,c,outline),n=item.name.toLowerCase();let art='';
 if(n.includes('bread')){
  art=shape('M48 251 C43 224 54 194 79 181 C109 159 167 164 193 183 C217 197 231 223 227 249 C202 271 79 278 48 251Z','#9b754c');
  art+=p('M50 242 Q89 264 150 255 Q201 250 224 228 L227 249 Q172 282 65 263Z','#604536');
  art+=p('M60 224 Q60 197 99 182 Q145 170 176 184 Q130 178 113 191 Q80 195 72 225Z','#c2a071');
  for(const x of [88,122,157])art+=shape(`M${x} 190 Q${x-12} 207 ${x-7} 230 Q${x+1} 220 ${x+4} 197Z`,'#684b35')+line(`M${x-1} 193 Q${x-8} 209 ${x-5} 222`,'#d3b58a',3);
  for(const [x,y] of [[69,237],[104,247],[147,243],[179,234],[189,205],[113,178]])art+=line(`M${x} ${y} l3 -1`,'#d8c5a0',2);
 }else if(n.includes('cheese')){
  art=shape('M66 196 Q91 160 174 159 L222 245 Q157 270 67 255Z','#b19861');
  art+=shape('M66 196 Q120 199 174 159 L222 245 Q146 235 67 255Z','#d0b87c');
  art+=p('M67 239 Q149 220 214 231 L222 245 Q142 235 67 255Z','#ab8a53');
  art+=shape('M66 196 Q94 163 174 159 L177 169 Q106 174 75 201 L76 253 L67 255Z','#776045');
  for(const [x,y,rad] of [[102,216,6],[150,201,5],[175,225,7],[121,235,3]])art+=circ(x,y,rad,'#927343')+line(`M${x-rad+1} ${y+2} q${rad} ${rad} ${rad*2-2} -1`,'#e0c997',1.7);
 }else if(n.includes('sausage')){
  art=shape('M83 162 C51 174 58 206 74 230 C97 272 158 288 196 247 C215 226 222 187 204 170 C191 159 171 173 180 191 C193 224 154 252 127 232 C99 219 103 199 108 186 C118 169 98 155 83 162Z','#895347');
  art+=p('M75 178 C66 210 95 248 125 259 C160 275 196 250 204 224 Q173 262 136 244 Q91 228 91 181Z','#573c37');
  art+=line('M84 175 C77 195 88 217 102 229 M116 239 Q142 255 158 247 M197 180 Q204 199 195 215','#b58260',7);
  art+=shape('M85 166 L72 148 L94 154 L105 144 L103 167Z','#76513f')+shape('M188 170 L181 150 L201 156 L212 148 L205 174Z','#76513f');
  art+=line('M78 168 L104 172 M185 173 L208 178','#c1a982',3);
  art+=line('M79 169 Q60 157 56 172 M79 169 Q60 182 56 172 M202 175 Q225 162 228 177','#b09976',2);
 }else if(n.includes('jerky')){
  const strips=[['M64 174 L96 164 L120 258 L105 279 L84 265Z','#784b3e'],['M112 153 L143 162 L150 264 L128 278 L113 250Z','#8d5943'],['M164 170 L194 182 L176 275 L148 281 L147 259Z','#694137']];
  for(const [d,c] of strips)art+=shape(d,c);
  art+=line('M77 182 L98 251 M88 190 L109 256 M125 171 L135 250 M174 192 L160 259','#b17a58',3)+line('M91 234 L104 249 M120 219 L137 234 M160 228 L175 222','#472f2d',3);
  art+=line('M82 241 Q127 258 181 240 M83 246 Q130 264 180 245','#b3a080',3);
 }else if(n.includes('ration')){
  art=shape('M54 196 Q87 164 131 176 L211 218 L208 281 L60 287 L45 247Z','#77735b');
  art+=shape('M70 204 Q57 171 89 158 Q121 148 141 183 L135 226 L77 232Z','#ad8858')+line('M83 171 L107 206 M105 166 L126 197','#725135',5);
  art+=shape('M145 195 L181 169 L205 184 L181 242 L145 244Z','#805347');
  art+=shape('M46 221 Q105 251 139 218 Q162 245 212 217 L208 281 Q123 303 58 281Z','#89836a');
  art+=p('M60 242 L111 264 L124 288 L59 280Z','#aca185')+p('M139 221 L151 276 L198 282 L207 235 L174 254Z','#55594a');
  art+=line('M62 271 Q131 248 202 263 M137 229 L139 289','#c2b393',3);
 }else if(n.includes('apple')){
  art=shape('M137 170 C94 137 63 161 62 201 C56 239 83 280 112 276 Q135 264 149 276 C184 287 213 238 212 201 C210 157 171 145 137 170Z','#854b43');
  art+=p('M170 159 Q222 169 205 229 Q187 275 157 271 L135 263 Q184 233 170 159Z','#503637');
  art+=p('M83 180 Q101 161 121 174 Q89 188 85 220 Q69 206 83 180Z','#b27f62');
  art+=line('M134 172 Q126 148 143 129','#5c4938',7)+shape('M141 150 Q165 120 191 142 Q172 166 141 150Z','#66704a')+line('M145 148 L177 142','#a7a476',2);
 }else{
  const colors={Strength:['#873f43','#bf8071'],Agility:['#68754d','#aeb88a'],Technique:['#a48e58','#d0bd8c'],Intelligence:['#665e83','#a39bc1'],HIT:['#9b7946','#ccac70'],COMBO:['#487975','#92b0a0'],CRIT:['#82495e','#bc8195'],'CRIT MISS':['#607a91','#a6bdc8']};
  const [dark,light]=colors[item.effect]||colors.Strength;
  const squat=n.includes('balm')||n.includes('oil');
  const d=squat?'M101 166 H170 V184 Q202 194 204 221 L198 281 Q139 306 78 281 L72 221 Q74 195 102 184Z':'M112 128 H157 L160 169 Q195 185 193 229 L185 290 Q138 312 88 290 L80 229 Q77 190 109 170Z';
  art=shape(d,'#5b7470');
  art+=p(squat?'M81 227 Q139 238 197 226 L192 276 Q139 296 85 276Z':'M89 224 Q134 236 184 225 L178 284 Q138 301 96 283Z',dark);
  art+=p(squat?'M80 215 Q81 196 105 191 L99 268 L88 271Z':'M111 140 L119 140 L119 175 Q93 193 94 220 L104 276 L96 275 L87 223 Q84 187 115 172Z','#a8b7a2');
  art+=p(squat?'M168 187 Q195 198 196 223 L190 273 L177 278 L180 218Z':'M153 148 L158 175 Q190 193 185 233 L178 283 L165 287 L173 220 Q173 190 151 182Z','#34484b');
  // Meniscus, thick glass base and broken reflection follow the vessel curves.
  art+=line(squat?'M83 227 Q137 241 195 227':'M91 225 Q136 237 183 225',light,1.8);
  art+=p(squat?'M85 276 Q138 297 193 276 L191 284 Q139 306 85 285Z':'M93 280 Q139 301 180 282 L183 291 Q137 313 90 291Z','#829a8b');
  art+=line(squat?'M84 209 Q87 200 95 197 M83 215 L85 244':'M105 184 Q95 193 92 205 M92 212 L94 225 M99 264 L103 279','#d2d2b5',2.2);
  art+=shape(squat?'M96 156 Q136 149 176 156 V177 Q137 185 96 177Z':'M108 112 Q135 107 161 112 L159 139 Q134 145 110 138Z','#8a7051');
  art+=line(squat?'M100 160 Q137 155 172 160':'M113 116 Q134 111 156 116','#b59b72',3);
  art+=p(squat?'M101 176 Q137 183 173 176 L173 183 Q136 192 101 182Z':'M111 137 Q135 143 159 137 L159 146 Q135 153 111 145Z','#283c3b');
  art+=line(squat?'M104 183 Q135 189 171 183':'M113 147 Q135 153 158 147','#bfd0b6',1.2);
  art+=shape('M106 214 Q135 209 165 214 L163 253 Q135 259 107 253Z','#b6a888');
  art+=`<g transform="translate(135 233) scale(.82)">${motif({Strength:'strength',Agility:'agility',Technique:'technique',Intelligence:'intelligence',HIT:'accuracy',COMBO:'chain',CRIT:'ferocity','CRIT MISS':'protection'}[item.effect],dark,light,'#d7c9a5')}</g>`;
  art+=line(squat?'M99 182 Q137 191 174 182':'M110 150 Q135 156 160 150','#b3a084',3);
 }
 return `<g data-consumable-art="${item.id}">${art}</g>`;
}
