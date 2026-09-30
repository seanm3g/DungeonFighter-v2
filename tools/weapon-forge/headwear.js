import {paintLight,mixPaint} from './paint-light.js';
// Silhouette first: named headwear families share construction, not one generic shell.
export function headwearProfile(item){
 const n=item.name.toLowerCase();
 const style=/kabuto/.test(n)?'kabuto':/plumed|corinthian/.test(n)?'crested':/dragon/.test(n)?'dragon':/doom|godhelm/.test(n)?'horned':/death|skull plate/.test(n)?'death-mask':/worldcrown|headdress/.test(n)?'sun-crown':/warcrown/.test(n)?'war-crown':/band|circlet/.test(n)?'circlet':/hounskull|forg-mouth/.test(n)?'beaked':/jaw|neckguard|throat/.test(n)?'gorget':/mask|face guard|faceplate|visor$/.test(n)?'mask':/pointed/.test(n)?'spire':/chainhood|linked hood/.test(n)?'mail-hood':/plated hood/.test(n)?'armored-hood':/hood|cowl|veil/.test(n)?'hood':/^hat$|toque/.test(n)?'hat':/wrap|scrap|kerf/.test(n)?'wrap':/cap/.test(n)?'cap':/great|siege|sealed|full plate/.test(n)?'greathelm':'helmet';
 return {style,anchor:style.includes('crown')||style==='circlet'?[134,240,.65]:style==='gorget'?[134,260,.6]:['hood','mail-hood','armored-hood'].includes(style)?[134,272,.6]:[134,153,.6]};
}
const forms={
 helmet:'M76 246 Q70 204 73 153 C75 112 98 92 133 91 C172 89 194 116 195 153 L191 237 Q188 256 167 273 L153 238 L147 218 L118 219 Q112 250 101 273 Q83 265 76 246Z',
 greathelm:'M74 128 Q87 103 108 97 Q135 88 160 98 Q183 104 192 128 L194 241 Q183 277 160 287 Q133 300 105 287 Q82 278 72 246Z',
 spire:'M70 242 L77 146 Q102 120 133 48 Q158 118 190 143 L201 242 L166 272 L134 242 L102 272Z',
 beaked:'M74 154 Q74 103 135 88 Q196 103 194 154 L189 235 L163 283 L134 264 L104 283 L79 237Z M81 173 L186 173 L135 252Z',
 horned:'M87 123 Q40 105 36 53 Q14 129 72 163 L77 244 L104 285 L132 251 L163 285 L193 244 L198 163 Q255 129 233 53 Q229 105 182 123 Q134 86 87 123Z',
 dragon:'M81 128 L47 88 L56 144 L33 134 L61 177 L53 214 L82 205 L88 263 L113 288 L134 245 L155 288 L181 263 L187 205 L215 214 L207 177 L235 134 L211 144 L220 88 L186 128 L157 101 L134 60 L112 101Z',
 kabuto:'M80 159 Q73 102 133 89 Q198 103 188 159 L221 204 L205 218 L216 249 L196 260 L204 279 L170 290 L152 239 H115 L97 290 L63 278 L72 260 L53 249 L65 218 L48 204Z M105 120 Q72 78 81 48 Q115 82 133 95 Q151 82 185 48 Q194 78 162 120Z',
 crested:'M91 115 Q73 62 114 47 Q173 22 201 65 L183 91 Q149 60 134 107 L170 116 Q190 140 185 180 L197 253 L167 277 L153 211 L114 211 L98 277 L70 253 L79 180 Q74 140 91 115Z',
 'death-mask':'M73 120 Q133 78 195 119 L193 209 L174 229 L176 273 L154 278 L145 296 H123 L114 278 L92 273 L95 229 L75 209Z',
 mask:'M64 108 Q133 146 202 108 L190 217 L171 270 L134 296 L97 270 L78 217Z',
 gorget:'M94 151 Q131 174 173 149 L183 211 Q193 242 215 262 Q191 300 135 314 Q80 305 53 268 Q78 241 84 214Z',
 'war-crown':'M65 248 L58 161 Q76 185 89 186 L103 127 Q116 161 134 179 Q151 164 167 126 L180 184 Q199 177 210 159 L202 252 L199 267 Q134 287 70 265Z',
 'sun-crown':'M71 250 L61 172 L85 198 L86 136 Q105 164 116 182 L134 102 L151 181 L177 134 L179 198 L207 169 L197 251 L193 269 Q134 291 76 269Z',
 circlet:'M65 203 Q98 217 117 198 L134 170 L151 198 Q177 217 202 203 L198 244 Q134 270 69 244Z',
 hood:'M55 287 Q70 245 79 186 C72 139 86 108 111 89 Q138 70 159 76 L155 99 Q194 120 190 183 Q193 239 213 290 Q191 300 171 285 Q152 308 132 312 Q107 307 94 286 Q76 299 55 287Z',
 'mail-hood':'M66 295 L76 178 Q71 109 133 91 Q197 109 192 178 L204 295 L165 285 L134 316 L102 285Z',
 'armored-hood':'M50 288 L80 178 L76 140 L133 71 L193 140 L188 178 L218 288 L173 283 L134 320 L93 283Z',
 hat:'M45 231 Q79 219 85 181 L95 117 L152 60 L182 81 L145 95 L151 172 Q158 207 227 230 Q176 267 125 254 Q76 264 45 231Z',
 wrap:'M70 247 L65 161 Q77 115 133 107 Q195 117 202 161 L195 232 L220 289 L184 302 L171 253 L132 265Z',
 cap:'M63 212 Q64 104 145 110 Q197 113 200 197 L227 223 Q155 257 61 242Z'
};
export function headwearArt(item,m,q,f,id,{p,line,circ}){
 const {style}=headwearProfile(item),d=forms[style],edge='stroke="#29292a" stroke-width="1.7" stroke-linejoin="round"';
 const lighting=paintLight(m);
 // Gradients live within the construction planes; silhouette and bevels stay crisp.
 const gradient=(name,stops)=>`<linearGradient id="${id}-${name}" x1="0" y1=".1" x2="1" y2=".7">${stops.map(([offset,color])=>`<stop offset="${offset}%" stop-color="${color}"/>`).join('')}</linearGradient>`;
 const defs=gradient('shell',[[0,lighting.mid],[37,m[1]],[74,mixPaint(m[1],m[2],.35)],[100,m[2]]])
  +gradient('light-plane',[[0,mixPaint(lighting.key,m[3],.5)],[28,lighting.mid],[67,mixPaint(lighting.mid,m[1],.35)],[100,m[1]]])
  +gradient('shade-plane',[[0,m[2]],[52,lighting.deep],[83,mixPaint(lighting.deep,lighting.bounce,.22)],[100,lighting.bounce]])
  +gradient('binding',[[0,q[3]],[28,q[1]],[75,q[2]],[100,q[1]]])
  +gradient('hardware',[[0,f[3]],[25,f[1]],[62,f[2]],[100,f[1]]]);
 const rawPath=p;
 // Base-color pieces receive restrained tonal variation, including cheek plates and trim.
 p=(path,fill,extra='')=>rawPath(path,fill===m[1]?`url(#${id}-shell)`:fill===q[1]?`url(#${id}-binding)`:fill===f[1]?`url(#${id}-hardware)`:fill,extra);
 const backBand=style==='circlet'?p('M65 204 C76 168 188 162 202 204 L198 230 Q183 193 79 233Z',m[2],edge)+line('M69 203 Q127 172 196 201',m[1],4)+line('M77 196 Q126 177 179 191',m[3],1.4):'';
 let art=`<defs><clipPath id="${id}-head">${rawPath(d,'white')}</clipPath>${defs}</defs>`+backBand+p(d,m[1],edge);
 // Shape-specific planes give cloth folds, curved shells and rigid plates their own volume.
 const cloth=['hood','mail-hood','armored-hood','hat','cap','wrap'].includes(style);
 const crown=style.includes('crown')||style==='circlet';
 const planes={
  cap:['M69 202 Q71 127 124 117 L139 116 Q109 140 102 180 L98 207Z','M154 115 Q195 123 199 197 L216 218 L169 235 L156 217 L169 187Z'],
  hat:['M99 120 L150 66 L137 98 L123 137 L122 190 L98 204 L94 177Z M57 233 L87 219 L119 232 L147 240 L122 248Z','M138 109 L148 102 L151 172 Q166 210 218 230 L172 246 L147 227 L136 179Z'],
  wrap:['M74 163 Q91 128 124 119 L122 152 L105 178 L102 233 L81 244Z','M155 115 Q189 125 200 160 L190 229 L215 287 L188 294 L180 246 L151 252 L166 197Z'],
  gorget:['M97 156 L113 195 L130 209 L123 257 L82 283 L57 266 L91 218Z','M155 194 L171 159 L183 219 L212 265 L186 293 L139 310 L145 270 L163 234Z']
 };
 const pair=planes[style]||(cloth?
  ['M82 174 Q81 117 116 88 L151 59 L132 106 L108 140 L97 198 L107 245 L87 274 L61 291Z','M151 106 Q194 126 186 186 L211 289 L175 274 L138 309 L145 254 L163 212 L170 164Z']:
  crown?['M58 105 L113 142 L130 178 L122 231 L79 247 L71 203Z','M152 94 L214 117 L200 255 L139 280 L148 233 L163 192Z']:
  ['M83 141 L103 112 L129 98 L120 139 L108 168 L103 223 L113 258 L100 275 L80 244Z','M147 96 L174 109 L190 142 L186 231 L166 270 L148 244 L155 204 L159 149Z']);
 art+=`<g clip-path="url(#${id}-head)">${p(pair[0],`url(#${id}-light-plane)`)}${p(pair[1],`url(#${id}-shade-plane)`)}`;
 const key={
  cap:'M75 166 Q85 121 124 119 L114 132 Q92 138 83 170Z',
  hat:'M99 120 L149 67 L137 91 L112 126 L104 155 L99 158Z M63 231 L86 221 L105 229 L86 232Z',
  wrap:'M78 149 Q95 126 126 121 L115 132 Q93 135 84 151Z',
  gorget:'M99 163 L111 193 L125 206 L119 216 L104 200Z M62 264 L86 239 L101 252 L79 274Z',
  circlet:'M71 207 Q98 221 115 204 L132 177 L127 202 L114 221 Q90 232 73 221Z'
 }[style]||(cloth?'M85 151 Q91 113 119 88 L149 62 L134 88 L110 112 L97 147 L93 171Z':crown?'M53 130 L85 164 L98 124 L106 157 L95 191 L75 178Z':'M83 141 Q93 113 126 99 L118 112 L102 120 L91 144 L87 160Z');
 art+=p(key,lighting.key,'opacity=".58"')+p(cloth?'M180 212 L200 281 L194 274 L173 231Z':crown?'M191 211 L196 207 L193 253 L175 260 L176 256 L189 249Z':'M182 201 L185 179 L183 230 L170 256 L163 260 L174 236Z',lighting.bounce);
 if(!cloth&&!crown&&style!=='gorget')art+=p('M129 99 Q134 115 134 140 L131 165 L136 170 Q142 137 136 100Z',m[3],'opacity=".55"')+p('M137 99 L145 105 L148 146 L141 168 L136 175 L142 145Z',m[2]);
 if(['horned','dragon','kabuto','spire'].includes(style))art+=p('M37 61 L54 106 L88 132 L76 146 L49 123Z M133 54 L130 113 L115 127 L117 104Z',m[3],'opacity=".75"')+p('M224 64 L219 117 L190 145 L182 136 L207 119Z',m[2]);
 art+='</g>';
 // Repeatable pigment variation, separate from the destructive print-wear mask.
 // Broad translucent dabs stay readable at inventory scale without gritty noise.
 let seed=2166136261;
 for(const c of item.name)seed=Math.imul(seed^c.charCodeAt(0),16777619)>>>0;
 const random=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296;};
 let texture='';
 for(let i=0;i<150;i++){
  const x=48+random()*170,y=78+random()*218,w=2+random()*9,h=1+random()*5;
  texture+=rawPath(`M${x.toFixed(1)} ${y.toFixed(1)} l${w.toFixed(1)} -1 2 ${h.toFixed(1)} -${(w*.6).toFixed(1)} 2 -3 -1Z`,i%3?lighting.key:lighting.deep,`opacity="${(.025+random()*.055).toFixed(3)}"`);
 }
 art+=`<g data-headwear-finish="painted-mottle" clip-path="url(#${id}-head)">${texture}</g>`;
 const hood=style.includes('hood');
 if(hood){
  art+=p('M132 119 Q95 144 100 201 L116 239 L151 239 L169 201 Q172 144 132 119Z',m[2],edge);
  art+=line('M126 125 Q95 150 100 187 Q101 216 116 233',q[3],2.5)+line('M86 238 L69 282 M180 238 L200 282',m[2],3);
  art+=p('M135 124 Q159 146 160 176 L153 217 L145 233 L153 239 L169 201 Q172 151 135 124Z','#1c2021');
  art+=`<g clip-path="url(#${id}-head)">${p('M89 214 L94 235 L79 280 L72 287Z',m[3],'opacity=".5"')}${p('M163 246 L173 273 L145 302 L151 280Z',m[2])}</g>`;
  if(style==='mail-hood')for(const y of [249,263,277])art+=line(`M89 ${y} q7 8 14 0 q7 8 14 0 M151 ${y} q7 8 14 0 q7 8 14 0`,f[3],2);
  if(style==='armored-hood')art+=p('M82 140 L131 88 L185 140 L176 151 L132 112 L93 151Z',f[1],edge);
 }else if(style.includes('crown')||style==='circlet'){
  art+=p('M69 234 Q134 259 200 234 L197 254 Q134 278 71 254Z',q[1],edge)+line('M75 237 Q133 262 195 237',f[3],3);
  art+=line('M73 253 Q134 278 197 253',f[2],4)+[85,181].map(x=>circ(x,248,4,f[2])+circ(x-1,247,2.5,f[3])).join('');
 }else if(['hat','cap','wrap'].includes(style)){
  art+=p(style==='wrap'?'M68 160 Q132 188 200 156 L200 176 Q134 205 68 181Z':'M80 196 Q133 218 189 195 L201 212 Q132 239 70 216Z',q[1],edge);
  if(style==='wrap')art+=line('M77 143 Q129 126 178 144 M77 207 Q110 219 151 210',m[2],3);
  else art+=line('M81 199 Q133 219 187 199',q[3],3)+line('M74 218 Q134 240 198 215',q[2],4);
 }else if(style==='gorget'){
  art+=line('M86 233 L134 266 L183 232 M78 255 L134 287 L195 254',m[3],3);
 }else{
  art+=p('M84 176 Q106 175 122 180 L121 188 Q102 185 85 186Z M146 180 Q163 177 182 175 L181 185 L147 188Z','#212529');
  art+=line('M84 172 Q105 172 122 177',lighting.key,1.7)+line('M146 177 L182 173',lighting.bounce,1.4);
  art+=line('M87 190 L115 202 M154 202 L180 190',m[2],3);
  art+=`<g clip-path="url(#${id}-head)">${line('M83 151 L87 228 L99 247',f[2],5)}${line('M81 150 L85 226 L97 245',f[1],2.5)}${[ [87,155],[91,224],[179,155],[175,224] ].map(([x,y])=>circ(x,y,3.2,f[2])+circ(x-.7,y-.8,1.7,f[3])).join('')}</g>`;
  if(style==='death-mask'){
   art+=p('M134 198 L124 219 H144Z',m[2]);
   art+=line('M113 246 L155 246 M118 239 V262 M130 240 V270 M142 240 V270 M153 239 V262',m[2],3);
  }else if(style==='beaked')art+=p('M90 192 L134 243 L178 192 L136 202Z',m[2])+line('M135 204 V235',m[3],2);
  else art+=p('M126 190 L137 189 L141 222 L132 230 L124 222Z',m[2])+line('M127 193 L128 218',lighting.mid,2)+[0,1,2].map(i=>circ(154+i*8,218+i*2,1.5,lighting.deep)).join('');
  art+=`<g clip-path="url(#${id}-head)">${p('M82 203 Q95 208 112 203 L109 238 L101 264 Q87 254 84 238Z',m[1])}${p('M153 204 Q170 208 187 201 L182 239 Q175 257 166 264 L157 238Z',lighting.deep)}${line('M86 211 L89 237 Q92 248 100 253',lighting.mid,2)}${line('M159 209 L164 241',lighting.bounce,1.5)}</g>`;
  if(style==='kabuto')art+=line('M62 213 L92 232 M64 247 L90 260 M203 213 L176 232 M204 247 L179 260',m[3],3);
  if(style==='crested')art+=p('M94 103 Q76 62 117 49 Q173 29 196 65 L183 80 Q145 53 128 99Z',q[1],edge)+line('M97 80 Q120 38 174 56',q[3],4);
 }
 return `<g data-headwear-style="${style}">${art}</g>`;
}
