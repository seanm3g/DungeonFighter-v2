const p=(d,c,edge=false)=>'<path d="'+d+'" fill="'+c+'" '+(edge?'stroke="#302e2b" stroke-width="2.2" stroke-linejoin="round"':'')+'/>';
function pair(kind){
 // Each boot points forward and slightly outward, as a worn pair would.
 const toe=kind===1?'Q53 266 54 284 Q57 295 81 299 L109 296 Q125 291 126 279':'Q48 271 55 286 Q65 302 90 300 Q120 299 126 279';
 const body='M87 87 Q111 82 135 88 L135 175 Q130 197 138 221 L136 252 L126 279 Q120 299 90 300 Q65 302 55 286 Q48 271 65 253 L83 230 Q94 216 90 197Z';
 let art='';
 for(let side=0;side<2;side++){
  let boot=p(kind===1?'M87 87 Q111 82 135 88 L135 175 Q130 197 138 221 L136 252 L126 279 Q125 291 109 296 L81 299 Q57 295 54 284 Q53 266 65 253 L83 230 Q94 216 90 197Z':body,'#796b55',true);
  // Keep the light coming from upper-left even on the mirrored right boot.
  boot+=p(side===0?'M119 87 L135 88 L135 175 Q130 197 138 221 L136 252 L126 279 L116 287 L115 260 L115 234 Q110 216 113 188Z':'M87 89 L99 87 L102 188 Q108 215 97 236 L80 260 Q66 277 72 293 L58 286 Q51 272 65 253 L83 230 Q94 216 90 197Z','#48473e');
  boot+=p(side===0?'M91 91 L105 89 L107 187 Q112 216 98 238 L80 263 Q68 273 69 282 Q58 277 72 261 L90 237 Q103 218 97 191Z':'M119 90 L131 91 L130 176 Q126 200 130 222 L126 250 L118 269 L107 277 L111 250 L116 230 Q113 207 117 186Z','#aa9674');
  boot+=p(kind===2?'M77 255 Q95 237 115 251 L119 269 Q99 287 67 281 Q65 268 77 255Z':'M69 270 Q89 250 113 262 L117 279 Q93 292 66 281Z','#a89475');
  boot+=p('M54 281 Q78 309 121 285 L136 251 L136 265 L125 291 Q94 316 59 298 L53 291Z','#3b3c34',true);
  boot+=p('M87 87 Q111 82 135 88 L135 99 Q111 92 88 99Z','#574f40',true);
  boot+='<path d="M84 238 Q99 234 116 245" fill="none" stroke="#655a46" stroke-width="1.8"/>';
  art+='<g transform="'+(side===0?'translate(0 0)':'translate(340 0) scale(-1 1)')+'">'+boot+'</g>';
 }
 return '<svg viewBox="30 55 280 275" role="img" aria-label="Forward-facing pair of simple leather boots, study '+(kind+1)+'">'+art+'</svg>';
}
const options=[['A · Rounded toe','A forward three-quarter view, with toes angled slightly outward.'],['B · Squarer toe','The same standing stance with a broader toe box.'],['C · Soft instep','The same pair with a wider highlight following the top of the foot.']];
document.getElementById('studies').innerHTML=options.map(([name,description],i)=>'<article><div class="art">'+pair(i)+'</div><div class="caption"><h2>'+name+'</h2><p>'+description+'</p></div></article>').join('');
