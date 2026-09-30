import {torsoConstruction,garmentDetails} from './torso-construction.js';
import {weaponConstruction} from './weapon-construction.js';
import {weaponPaint} from './weapon-paint.js';
import {consumableArt} from './consumables.js';
import {footwearArt,footwearProfile} from './footwear.js';
import {headwearArt,headwearProfile} from './headwear.js';
import {paintedPlanes,refinedShape} from './paint-planes.js';
import {armorFinish} from './armor-finish.js';
import {clubArt} from './mace-finish.js';
import {sculptedFocus} from './focus-sculpture.js';
import {charmArt} from './charm-art.js';
import {bladeArt} from './blade-art.js';
import {redesignedRelic} from './relic-redesign.js';
import {legwearArt} from './legwear.js';
// Shared flat-painted silhouettes. Affixes are composed after the base and trim.
export const SHAPES={
 club:{d:'M116 338 L119 182 Q76 81 113 60 Q146 40 156 85 L143 190 L140 338Z',trim:'M119 266 H141 V324 H118Z'},
 hammer:{d:'M120 339 V154 H70 V87 L91 73 H174 L190 89 V146 L175 158 H141 V339Z',trim:'M120 267 H141 V327 H120Z'},
 morningstar:{d:'M121 337 V170 L103 157 L83 171 L90 140 L65 121 L91 106 L88 78 L112 85 L130 54 L145 82 L174 75 L170 104 L196 120 L171 137 L175 166 L149 157 L140 174 V337Z',trim:'M120 266 H142 V325 H120Z'},
 flail:{d:'M145 231 L162 234 L131 348 L111 342Z M71 92 L83 64 L100 88 L123 76 L123 101 L146 112 L125 126 L129 154 L103 148 L86 171 L77 147 L50 150 L58 125 L37 111 L61 103Z',trim:'M136 266 L153 271 L135 327 L119 323Z',line:'M105 141 C180 150 198 201 153 232'},
 cleaver:{d:'M121 345 V249 L90 242 L83 67 L169 73 L170 235 L143 252 V345Z',trim:'M119 271 H143 V331 H119Z'},
 saber:{d:'M119 341 L119 269 Q193 182 181 60 Q149 186 102 248 L115 274 L140 280 L140 341Z',trim:'M118 282 H141 V330 H118Z'},
 rapier:{d:'M130 44 L137 270 L142 339 H120 L125 269Z',trim:'M120 283 H142 V331 H120Z',line:'M101 269 Q130 296 162 267 M108 271 Q78 300 120 316'},
 polearm:{d:'M125 368 V153 L101 162 L92 104 L123 110 L131 33 L151 91 L169 121 L140 153 V368Z',trim:'M124 256 H140 V317 H124Z'},
 scythe:{d:'M120 370 L127 99 Q81 92 53 130 Q65 37 196 81 L174 94 L141 97 L137 370Z',trim:'M123 261 H139 V321 H122Z'},
 kris:{d:'M123 319 V247 Q95 221 125 194 Q143 175 123 153 L132 108 Q142 149 140 165 Q157 191 133 214 Q120 228 139 247 V319Z',trim:'M123 261 H140 V308 H123Z'},
 hook:{d:'M121 318 V243 Q153 225 162 184 Q159 151 139 156 L141 127 Q197 137 179 194 Q166 238 141 250 V318Z',trim:'M121 268 H141 V309 H121Z'},
 fang:{d:'M122 319 V250 Q100 200 110 118 Q129 199 145 230 L141 250 V319Z',trim:'M122 267 H142 V308 H122Z'},
 staff:{d:'M124 365 L121 135 Q79 109 102 71 Q120 42 151 60 L160 79 L146 96 L137 81 Q116 77 116 100 L142 119 L139 365Z',trim:'M122 242 H141 V304 H122Z'},
 book:{d:'M67 118 L186 100 L212 121 L210 313 L88 332 L65 309Z',trim:'M66 119 L84 131 L87 329 L65 309Z M85 207 L211 188 V212 L85 232Z',line:'M86 131 L205 115 M94 314 L201 300 M95 321 L202 308'},
 scroll:{d:'M82 110 Q60 88 55 113 V144 L72 148 V290 Q51 289 55 311 Q61 325 86 316 L198 309 Q219 288 202 277 L189 279 V126 Q212 130 215 109 Q210 92 192 101Z',trim:'M73 202 H188 V225 H73Z',line:'M82 110 H193 M84 296 H190 M91 153 H167 M91 167 H159'},
 lantern:{d:'M112 100 V80 Q137 53 159 81 V100 L181 124 L192 284 L174 315 H94 L78 282 L89 125Z',trim:'M89 125 H181 V143 H89Z M81 272 H190 V289 H81Z',line:'M113 144 V271 M158 144 V271 M115 245 Q96 212 135 171 Q158 220 149 245Z'},
 vessel:{d:'M98 116 L88 104 H182 L174 116 L171 167 Q224 221 196 288 L175 308 H97 L75 282 Q53 215 100 168Z',trim:'M96 123 H176 V142 H96Z M76 258 Q135 280 195 258 V278 Q135 298 80 278Z'},
 idol:{d:'M100 104 L91 139 L101 170 L78 196 L93 230 L97 310 H172 L176 230 L194 197 L167 171 L181 132 L165 103 L134 85Z',trim:'M96 245 H175 V263 H97Z',line:'M109 131 L122 141 M149 141 L164 129 M122 159 L144 159 M113 282 H159'},
 obelisk:{d:'M135 54 L165 113 L177 298 L195 314 V333 H73 V312 L93 298 L106 114Z',trim:'M98 227 H173 V246 H97Z M76 313 H191 V325 H76Z',line:'M135 57 L139 298 L177 298'},
 crystal:{d:'M131 65 L163 103 L176 225 L151 312 L110 324 L76 236 L95 112Z',trim:'M82 242 L174 226 L169 248 L90 262Z',line:'M131 65 L122 220 L110 324 M122 220 L176 225 M95 112 L122 220'},
 orb:{d:'M135 98 C244 101 237 272 138 281 C32 279 29 106 135 98Z',trim:'M72 243 Q134 282 198 238 L193 271 L162 291 L160 317 H110 V291 L79 275Z',line:'M91 127 Q119 110 152 122'},
 amulet:{d:'M131 183 L184 225 L167 292 L129 319 L86 288 L77 227Z',trim:'M131 191 L173 229 L159 282 L129 304 L97 282 L88 230Z',line:'M114 196 C29 61 231 51 148 197'},
 crown:{d:'M68 187 L53 113 L103 146 L130 88 L157 146 L210 111 L190 188 L188 244 Q131 277 70 245Z',trim:'M68 220 Q130 245 191 218 L188 244 Q129 269 70 245Z'},
 hood:{d:'M133 77 Q204 93 199 177 L224 291 Q135 338 51 290 L76 176 Q75 99 133 77Z',trim:'M128 111 Q96 139 101 193 L120 229 L91 262 L73 247 L87 157 Q92 123 128 111Z',line:'M130 115 Q163 135 165 180 L142 222 L115 218 L99 186Z'},
 cap:{d:'M65 206 Q61 107 119 103 Q179 81 204 196 L217 222 Q141 265 58 228Z',trim:'M65 201 Q133 229 203 197 L214 220 Q135 251 60 227Z'},
 mask:{d:'M70 115 Q132 75 196 117 L185 242 L133 296 L80 243Z',trim:'M74 121 L130 147 L190 122 L188 139 L132 166 L76 139Z',line:'M91 169 L118 177 M148 177 L176 165 M132 177 L121 220 L143 220 M117 245 H149'},
 helmet:{d:'M70 246 L66 155 Q67 94 132 87 Q195 93 200 155 L197 247 L168 268 L147 255 L132 294 L117 254 L96 269Z',trim:'M69 175 L123 183 L129 111 L138 111 L144 183 L198 172 V194 L144 207 L138 263 L127 263 L119 207 L70 196Z',line:'M81 201 L109 210 M155 210 L185 200'},
 coat:{d:'M104 83 L79 99 L45 188 L75 211 L91 172 L83 318 L126 327 L135 280 L146 327 L188 316 L178 173 L198 211 L228 190 L193 100 L164 84 L135 103Z',trim:'M115 94 L126 161 L121 319 L133 285 L144 319 L140 162 L156 94 L135 109Z M90 217 H180 V233 H89Z'},
 tunic:{d:'M106 101 L76 107 L38 169 L78 193 L94 162 L86 308 Q134 323 184 306 L176 162 L194 192 L230 170 L192 108 L164 99 Q135 132 106 101Z',trim:'M91 224 H180 V239 H90Z',line:'M103 118 Q135 143 166 116 M108 285 Q138 291 169 283'},
 chestplate:{d:'M94 94 L63 123 L47 174 L82 185 L88 266 L75 303 L130 323 L193 302 L181 265 L187 182 L221 171 L203 124 L171 93 Q132 132 94 94Z',trim:'M88 247 Q133 265 182 246 L181 265 Q136 282 89 266Z',line:'M94 142 Q115 172 130 239 M171 141 Q152 171 140 239 M57 161 L84 171 M188 169 L211 160'},
 mail:{d:'M99 99 L72 114 L46 180 L81 197 L91 177 L84 309 L184 309 L176 177 L189 197 L224 180 L196 114 L168 99 Q132 132 99 99Z',trim:'M88 234 H180 V251 H88Z',line:'M104 144 Q115 157 125 144 Q136 157 147 144 Q158 157 169 144 M103 163 Q114 176 125 163 Q136 176 147 163 Q158 176 169 163 M102 182 Q114 195 125 182 Q136 195 147 182 Q158 195 170 182 M101 201 Q113 214 125 201 Q136 214 147 201 Q159 214 171 201'},
 trousers:{d:'M87 97 H181 L190 177 L175 322 L141 322 L133 203 L124 322 H88 L77 177Z',trim:'M86 98 H182 V119 H86Z M88 292 H126 V311 H88Z M140 292 H177 V311 H140Z',line:'M133 121 V182 M95 143 L85 164 M171 143 L183 162'},
 greaves:{d:'M72 144 Q65 116 79 103 Q96 90 114 103 Q128 116 121 144 L117 164 Q125 198 117 238 L109 290 L116 314 Q96 324 76 313 L82 290 L75 238 Q66 197 76 164Z M150 144 Q143 116 157 103 Q174 90 192 103 Q206 116 199 144 L195 164 Q203 198 195 238 L187 290 L194 314 Q174 324 154 313 L160 290 L153 238 Q144 197 154 164Z',trim:'M74 163 Q96 173 118 163 L120 180 Q96 190 72 180Z M79 267 Q96 275 113 267 L110 281 Q96 288 81 281Z M152 163 Q174 173 196 163 L198 180 Q174 190 150 180Z M157 267 Q174 275 191 267 L188 281 Q174 288 159 281Z',line:'M73 139 Q96 158 121 139 M151 139 Q174 158 199 139 M96 189 L94 263 M174 189 L172 263 M83 299 Q96 306 109 299 M161 299 Q174 306 187 299'},
 boots:{d:'M65 121 L112 117 L118 248 L130 278 L130 305 L52 309 L48 283 L73 250Z M155 123 L202 119 L191 246 L220 280 L218 307 L144 306 L138 279 L151 248Z',trim:'M65 123 L112 119 L114 145 L66 150Z M155 123 L202 121 L199 148 L154 150Z M50 289 H129 V305 H52Z M143 289 H219 V306 H144Z',line:'M81 177 H109 M80 192 H110 M158 177 H190 M157 192 H188'},
 shoes:{d:'M80 168 L115 164 L121 218 L133 251 L128 281 L47 286 L44 265 L72 231Z M153 166 L189 168 L193 232 L222 265 L217 287 L140 281 L134 253 L148 219Z',trim:'M48 267 H131 V281 L48 286Z M138 266 H221 L218 285 L140 282Z',line:'M82 203 H112 M80 216 H115 M154 203 H185 M151 216 H188'},
 potion:{d:'M112 105 H155 V161 Q190 183 187 223 L179 298 Q137 320 92 298 L82 221 Q81 181 113 160Z',trim:'M109 99 H159 V123 H109Z M90 222 H183 V257 H90Z'},
 apple:{d:'M134 165 Q105 133 80 164 Q51 199 86 261 Q111 295 134 277 Q166 298 190 253 Q224 175 174 155 Q152 147 134 165Z',trim:'M130 162 Q121 139 139 123 L147 130 L138 164Z',line:'M141 143 Q168 107 185 140 Q165 163 141 143'},
 bread:{d:'M50 249 Q33 184 92 158 Q164 116 207 184 L219 256 Q146 290 60 273Z',trim:'M53 252 Q136 271 216 241 L219 256 Q145 290 60 273Z',line:'M87 183 L101 232 M120 169 L137 223 M154 165 L174 216'},
 cheese:{d:'M62 197 L139 135 L210 213 L201 280 L62 266Z',trim:'M62 249 L202 264 L201 280 L62 266Z',line:'M63 197 L201 219 L210 213 M91 222 L105 220 L109 231 L96 238Z M148 232 L159 233 L159 247 L147 244Z'},
 ration:{d:'M76 152 L185 161 L205 262 L170 292 L70 278 L53 189Z',trim:'M118 156 L141 158 L153 286 L128 282Z M57 209 L195 212 L199 237 L60 233Z',line:'M76 154 L93 181 M180 165 L170 191 M72 257 L91 262'}
};

export const ATTACHMENTS={chestplate:[134,248,.75],coat:[134,227,.65],tunic:[134,232,.7],mail:[134,242,.7],lantern:[134,289,.6],vessel:[134,270,.7],idol:[134,257,.7],obelisk:[134,286,.7],amulet:[130,251,.85],crown:[130,231,.8],mask:[132,233,.8],helmet:[132,240,.8],hood:[134,246,.8],cap:[134,222,.8],greaves:[96,219,.65],boots:[94,211,.7],shoes:[94,237,.65],trousers:[133,145,.7],book:[144,220,.7],scroll:[131,214,.8],orb:[135,292,.6],crystal:[128,239,.65],flail:[143,276,.65],saber:[130,286,.7],hook:[131,272,.6],fang:[131,272,.6],kris:[131,272,.6]};
export function renderEquipment(r,options,shared){
 const {p,line,circ,motif,effect,suffixAdornment,hash,rng,MATERIALS,FITTINGS,GRIPS,ITEM_DETAILS,SUFFIX_DETAILS,itemName}=shared;
 const item=ITEM_DETAILS[r.item],s=refinedShape(item,SHAPES[item.shape]);
 if(!s)throw Error(`Missing silhouette: ${item.shape}`);
 const m=MATERIALS[r.material],f=FITTINGS[r.fittings],q=GRIPS[r.grip],id='w'+hash((options.uid||'item')+JSON.stringify(r));
 const outline=`stroke="#252326" stroke-width="2.5" stroke-linejoin="round"`;
 const consumable=r.weapon==='consumable';
 const food=consumable&&item.shape!=='potion';
 const palette=food?(item.shape==='apple'?['','#77534b','#382d32','#b09570']:['','#a08459','#524233','#c9b58b']):m;
 const solid=p(s.d,palette[1],outline);
 const planes=item.shape==='greaves'?[0,78].map(dx=>`<g transform="translate(${dx} 0)">${p('M75 100 L97 96 L94 146 L97 165 L94 295 L84 314 L76 307 L82 244 L72 167Z',palette[3],'opacity=".6"')}${p('M105 95 L127 100 V323 H101 L108 281 L111 199 L104 147Z',r.prefix==='hardened'?'#252326':palette[2])}</g>`).join(''):p('M39 75 L128 53 L115 174 L146 248 L107 357 H36Z',palette[3],'opacity=".6"')+p('M152 56 L241 63 V359 L139 345 L155 244 L135 151Z',r.prefix==='hardened'?'#252326':palette[2]);
 let body=solid+`<g clip-path="url(#${id}-shape)">${paintedPlanes(item.shape,r.prefix==='hardened'?[palette[0],palette[1],'#252326',palette[3]]:palette,p)??planes}</g>`;
 if(s.line)body+=line(s.line,palette[2],3)+`<g transform="translate(-1 -2)">${line(s.line,palette[3],1.5)}</g>`;
 if(s.trim)body+=p(s.trim,consumable?'#776044':q[1],outline)+`<g clip-path="url(#${id}-trim)">${line('M40 120 L222 112 M50 230 L221 222 M60 280 L221 272 M65 313 H217',q[3],4)}</g>`;
 if(['chest','legs'].includes(r.weapon))body+=`<g clip-path="url(#${id}-shape)">${armorFinish(item.shape,palette,q,{p,line,circ})}</g>`;
 // Tier changes are shared construction details, never random surface wear.
 if(item.tier>=3&&['chestplate','coat','tunic','mail','book'].includes(item.shape))body+=`<g clip-path="url(#${id}-shape)">${line(item.shape==='book'?'M93 146 L98 292 L190 281':'M96 276 Q134 293 173 276',f[1],2.5)}</g>`;
 if(item.tier>=4&&['chestplate','coat','tunic','mail','book'].includes(item.shape))body+=`<g clip-path="url(#${id}-shape)">${[145,280].map(y=>circ(99,y,2.5,f[3])+circ(170,y,2.5,f[3])).join('')}</g>`;
 const weaponLike=['club','hammer','morningstar','flail','cleaver','saber','rapier','polearm','scythe','kris','hook','fang','staff'].includes(item.shape);
 const footwear=['boots','shoes'].includes(item.shape);
 if(footwear)body=footwearArt(item,r.prefix==='hardened'?[m[0],m[1],'#252326',m[3]]:m,q,{p,line,id,seed:r.seed});
 if(r.weapon==='head')body=headwearArt(item,r.prefix==='hardened'?[m[0],m[1],'#252326',m[3]]:m,q,f,id,{p,line,circ});
 if(item.shape==='club')body=clubArt(r.prefix==='hardened'?[m[0],m[1],'#252326',m[3]]:m,q,f,{p,line,circ});
 if(['morningstar','flail'].includes(item.shape))body+=`<g clip-path="url(#${id}-shape)">${p('M89 92 L108 87 L120 105 L116 133 L96 145 L78 130 L77 111Z',m[3],'opacity=".5"')}${p('M119 98 L132 111 L119 145 L104 153 L108 133Z',m[2])}${line('M82 110 L96 99 L107 101',m[3],3)}</g>`;
 if(item.shape==='hammer'){
  const shadow=r.prefix==='hardened'?'#252326':m[2];
  // Separate striking block, socket and haft; broad painted facets, not surface scratches.
  body=p('M121 137 H141 L140 332 L135 341 H123 L119 332Z',shadow,outline)
   +p('M122 147 H131 V330 H123Z',m[1])+line('M123 157 V256',m[3],2)
   +p('M63 91 L76 78 H176 L191 90 V142 L178 155 H76 L63 143Z',m[1],outline)
   +p('M63 91 L76 78 H176 L191 90 L176 97 H78Z',m[3])
   +p('M176 97 L191 90 V142 L178 155 L174 141Z',shadow)
   +p('M63 91 L78 97 V140 L76 155 L63 143Z',shadow)
   +p('M68 98 L75 101 V137 L68 140Z',m[3])
   +p('M79 140 H174 L178 155 H76Z',shadow)
   +p('M81 101 H113 V107 H87 V133 L81 139Z',m[3],'opacity=".65"')
   +p('M146 101 H172 L169 136 H146Z',shadow,'opacity=".35"')
   +line('M81 98 H112 M147 98 H174',m[3],2)
   +p('M116 83 H143 V155 L139 173 H120 L116 155Z',f[1],outline)
   +p('M118 86 H124 V152 L126 168 H121 L118 152Z',f[3])
   +p('M136 86 H142 V153 L138 171 H132 L136 151Z',f[2])
   +circ(129,108,4,f[2])+circ(128,107,2,f[3])
   +circ(129,145,3.5,f[2])+circ(128,144,1.6,f[3])
   +p('M117 261 H143 L141 328 H119Z',q[1],outline)
   +p('M134 263 H142 L140 326 H133Z',q[2])
   +[269,281,293,305,317].map(y=>line(`M119 ${y} L140 ${y-3}`,q[3],2.5)).join('')
   +p('M117 255 H144 V266 H117Z M118 325 H142 L141 337 H121Z',f[1],outline)
   +line('M119 257 H141 M120 327 H139',f[3],2);
 }
 const sculptPalette=r.prefix==='hardened'?[m[0],m[1],'#252326',m[3]]:m;
 if(r.weapon==='chest')body=torsoConstruction(item,sculptPalette,q,f,{p,line,circ})||(body+garmentDetails(item.shape,sculptPalette,q,f,{p,line,circ}));
 const sculpture=r.weapon==='legs'?legwearArt(item,sculptPalette,q,f,id,{p,line,circ}):weaponConstruction(item,sculptPalette,q,f,id,{p,line,circ})||redesignedRelic(item,sculptPalette,q,f,{p,line,circ})||(['sword','dagger'].includes(r.weapon)?bladeArt(item,sculptPalette,q,f,{p,line,circ}):r.weapon==='charm'?charmArt(item,sculptPalette,q,f,{p,line,circ}):r.weapon==='wand'?sculptedFocus(item,sculptPalette,q,f,{p,line,circ}):null);
 if(sculpture)body=sculpture.art;
 
 const anchor=sculpture?.anchor||(r.weapon==='head'?headwearProfile(item).anchor:footwear?[92,footwearProfile(item).anchorY,.46]:item.shape==='hammer'?[130,249,.7]:ATTACHMENTS[item.shape]||(weaponLike?[130,269,1]:[134,224,1]));
 if(!consumable){
  const [ax,ay,attachmentScale]=anchor,x=0,y=0;
  const baseBody=body;body='';
  const prefixes={reinforced:'M-27-8 H27 V8 H-27Z',sturdy:'M-27-10 H27 V10 H-27Z',balanced:'M-27-5 L-20-12 L-13-5 L-20 2Z M13-5 L20-12 L27-5 L20 2Z',swift:'M-23-13 L-3-3 M3-3 L23-13',acrobatic:'M-25-9 Q0 15 25-9',nimble:'M-18-8 L0 1 L18-8',light:'M-20-7 H20',featherweight:'M-20-8 L0 1 L20-8 M-14-12 L0-4 L14-12',ancient:'M-25 0 Q-32-22-12-15 L0-3 L12-15 Q32-22 25 0',serrated:'M-27-9 l7-7 7 7 7-7 7 7 7-7 7 7',refined:'M-23-12 H23 M-23-8 H23',precise:'M-20-11 H20 M0-19 V-3',keen:'M-23-15 L23-10',brutal:'M-27-17 V2 H-18 M27-17 V2 H18'};
  if(prefixes[r.prefix])body+=`<g transform="translate(${x} ${y-20})">${line(prefixes[r.prefix],f[2],6)}${line(prefixes[r.prefix],f[3],2.5)}</g>`;
  const crest={joint:95,tip:0,crest:{x:0,y:0,scale:r.weapon==='charm'?.5:.6}};
  if(['flaming','poisonous','charged','enchanted','blessed'].includes(r.prefix))body+=`<g transform="translate(${x} ${y-47})">${effect(r,crest,m)}</g>`;
  if(r.suffix!=='none'){
   body+=`<g data-suffix-emblem="${SUFFIX_DETAILS[r.suffix].motif}" transform="translate(${x} ${y})">${circ(0,0,19,f[1],outline)}${circ(0,0,15,f[2])}${motif(r.suffix,f[3],f[3],f[2])}</g>`;
   body+=`<g transform="translate(${x-130} ${y-15})">${suffixAdornment(r,{joint:0},f)}</g>`;
  }
  if(['perfect','masterwork','heirloom','cosmic'].includes(r.quality))body+=`<g transform="translate(${x} ${y+25})">${line('M-18 0 H18 M-12 5 H12',f[3],2)}</g>`;
  if(['broken','worn','battle-scarred','second-hand'].includes(r.quality))body+=`<g transform="translate(${x} ${y+24})">${line('M-15 0 L15 5 M-15 8 L15 13',q[3],3)}</g>`;
  const decoration=body;
  body=baseBody+`<g data-attachment="${item.shape}" transform="translate(${ax} ${ay}) scale(${attachmentScale})">${decoration}</g>`;
  if(sculpture?.paired||item.shape==='greaves'&&r.weapon!=='legs')body+=`<g data-attachment="greaves-pair" transform="translate(${ax+78} ${ay}) scale(${attachmentScale})">${decoration}</g>`;
  if(footwear)body+=`<g data-attachment="footwear-pair" transform="translate(188 ${ay}) scale(${attachmentScale})">${decoration}</g>`;
 }else if(item.shape==='potion'){
  const colors={Strength:'#995650',Agility:'#7e8654',Technique:'#b5a375',Intelligence:'#8781a1',HIT:'#bba16b',COMBO:'#67928b',CRIT:'#976779','CRIT MISS':'#909dba'};
  body+=p('M93 262 H179 L175 292 Q136 307 98 292Z',colors[item.effect]||'#8d7a65');
  body+=`<g transform="translate(136 238) scale(.7)">${motif({Strength:'strength',Agility:'agility',Technique:'technique',Intelligence:'intelligence',HIT:'accuracy',COMBO:'chain',CRIT:'ferocity','CRIT MISS':'protection'}[item.effect],f[3],f[3],f[2])}</g>`;
 }
 if(consumable)body=consumableArt(item,{p,line,circ,motif});
 if(!footwear)body=weaponPaint(body,{id:id+'-surface',m,q,f,seed:r.seed,natural:consumable},{p,rng});
 const sx=r.prefix==='heavy'?1.09:r.prefix==='brutal'?1.05:1,sy=r.prefix==='short'?.9:r.prefix==='long'?1.06:1;
 const amount=Math.max(0,Math.min(35,Number(options.distress??12)||0)),noise=rng('print'+r.seed);let marks='';
 for(let i=0;i<(amount?340:0);i++){const x=35+noise()*207,y=52+noise()*310,w=.7+noise()*2.8;marks+=p(`M${x.toFixed(2)} ${y.toFixed(2)} l${w.toFixed(2)} -.5 -.5 1.5 -${w.toFixed(2)} 1Z`,'black',`opacity="${(amount/35*(.25+noise()*.35)).toFixed(3)}"`);}
 const title=itemName(r).replaceAll('&','&amp;').replaceAll('"','&quot;');
 return `<svg xmlns="http://www.w3.org/2000/svg" width="280" height="420" viewBox="0 0 280 420" role="img" aria-label="${title}"><title>${title}</title><defs><clipPath id="${id}-shape">${p(s.d,'white')}</clipPath><clipPath id="${id}-trim">${p(s.trim||s.d,'white')}</clipPath><mask id="${id}-wear" maskUnits="userSpaceOnUse" x="0" y="0" width="280" height="420"><rect width="280" height="420" fill="white"/>${marks}</mask></defs>${options.background?'<rect width="280" height="420" fill="#8d8271"/>':''}<g transform="rotate(${weaponLike?14:0} 140 210)"><g transform="translate(140 210) scale(${sx} ${sy}) translate(-140 -210)" mask="url(#${id}-wear)">${body}</g></g></svg>`;
}

