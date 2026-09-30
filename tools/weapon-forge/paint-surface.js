// Two scales of pigment variation: broad brush loading and fine surface tooth.
// Multiplication preserves the modeled light, and the final alpha composite
// keeps PNG/SVG exports transparent without a paper-colored fringe.
export function paintSurface(id,seed,{soft=false,natural=false}={}){
 const key=id+'-pigment';
 const grain=soft?'.44 .68':natural?'.32 .4':'.55 .28';
 return {key,defs:`<filter id="${key}" x="-5%" y="-5%" width="110%" height="110%" color-interpolation-filters="sRGB">
 <feTurbulence type="fractalNoise" baseFrequency=".055 .095" numOctaves="3" seed="${seed%997}" result="brush"/>
 <feColorMatrix in="brush" type="saturate" values="0"/>
 <feComponentTransfer result="glaze"><feFuncR type="linear" slope=".62" intercept=".66"/><feFuncG type="linear" slope=".62" intercept=".66"/><feFuncB type="linear" slope=".62" intercept=".66"/></feComponentTransfer>
 <feTurbulence type="fractalNoise" baseFrequency="${grain}" numOctaves="2" seed="${(seed+17)%997}"/>
 <feColorMatrix type="saturate" values="0"/>
 <feComponentTransfer result="tooth"><feFuncR type="linear" slope=".22" intercept=".87"/><feFuncG type="linear" slope=".22" intercept=".87"/><feFuncB type="linear" slope=".22" intercept=".87"/></feComponentTransfer>
 <feBlend in="glaze" in2="tooth" mode="multiply" result="surface"/>
 <feBlend in="SourceGraphic" in2="surface" mode="multiply"/>
 <feComposite in2="SourceGraphic" operator="in"/>
 </filter>`};
}
