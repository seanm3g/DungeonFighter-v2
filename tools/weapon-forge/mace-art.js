import {mixPaint} from './paint-light.js';
// The reference's broad forged flanges, recessed socket and continuous leather
// grip are construction, not a texture laid over the former pointed head.
export const MACE_BODY='M104 64 L119 59 L128 52 L139 58 L149 53 L158 59 L160 78 L174 88 L166 115 L169 144 L152 153 L145 176 L116 178 L110 159 L94 146 L98 121 L88 87 L101 79Z';

export function maceHead(m,f,{p,line}){
 const edge='stroke="#24221f" stroke-width="1.7" stroke-linejoin="round"';
 return `<g data-construction="forged-flanges">`
  +p(MACE_BODY,m[2],edge)
  // Back flange and its exposed top thickness.
  +p('M90 87 L105 80 L120 62 L127 56 L130 68 L114 92 L111 132 L117 169 L109 157 L96 145 L102 118Z',m[1])
  +p('M91 86 L105 79 L120 62 L127 56 L125 65 L109 85 L101 91Z',m[3])
  +p('M110 93 L123 73 L119 132 L123 170 L114 170 L105 143Z',m[2])
  // Right-hand flange stays a single broad dark mass.
  +p('M147 63 L157 61 L159 79 L173 89 L163 119 L166 142 L149 151 L142 174 L130 174 L142 131Z',m[2])
  +p('M153 72 L158 81 L169 90 L161 108 L154 115 L153 96Z',m[1],'opacity=".55"')
  +p('M144 148 L162 138 L166 143 L149 153 L143 173 L136 175Z',m[1],'opacity=".45"')
  // The broad central striking face leans across the recessed side.
  +p('M121 68 L136 59 L149 55 L155 64 L151 99 L147 137 L138 173 L114 175 L117 135 L117 100Z',m[1],edge)
  +p('M121 68 L136 59 L132 68 L126 74 L123 98 L125 106 L121 135 L122 145 L117 163 L114 175 L117 136 L117 100Z',m[3])
  +p('M149 58 L155 64 L151 99 L147 137 L138 173 L131 174 L140 134 L143 100 L146 76Z',m[2])
  +p('M123 67 L136 59 L149 55 L152 60 L140 62 L133 68 L126 72Z',mixPaint(m[3],m[1],.32))
  +p('M131 77 L140 68 L144 68 L137 95 L133 124 L128 137 L130 103Z',m[3],'opacity=".19"')
  // Paint follows the long forged faces; irregular patches interrupt the
  // smooth ramp without cutting holes or simulating chipped metal everywhere.
  +p('M133 78 L141 72 L139 82 L136 84 L133 94 L129 97 L130 87Z',mixPaint(m[1],m[3],.26),'opacity=".6"')
  +p('M129 102 L135 98 L137 103 L134 109 L135 120 L130 127 L127 123Z',mixPaint(m[1],m[2],.25),'opacity=".5"')
  +p('M125 141 L132 135 L132 142 L129 145 L129 152 L123 160Z',mixPaint(m[1],m[3],.28),'opacity=".48"')
  +p('M105 94 L111 91 L109 100 L110 109 L107 114 L104 108Z',mixPaint(m[1],m[2],.35))
  +p('M148 116 L154 107 L158 112 L154 121 L155 127 L147 136Z',mixPaint(m[1],m[2],.7))
  // Broken edge light, rather than an outline around every plane.
  +line('M128 63 L136 61 M123 105 L123 124 M118 157 L116 168',m[3],1.2)
  +p('M115 171 Q130 177 145 171 L144 185 Q131 190 117 184Z',f[2],edge)
  +p('M117 173 Q130 178 143 173 L142 179 Q130 183 118 179Z',f[1])
  +`</g>`;
}

export function wrappedGrip(g,q,{p,line}){
 const start=g.joint+8,end=g.end-17;
 let art=p(`M118 ${start} Q130 ${start-3} 142 ${start} L141 ${end} Q130 ${end+7} 118 ${end}Z`,q[2],'stroke="#24211f" stroke-width="2"');
 for(let y=start,i=0;y<end;i++){
  const pitch=[11,10,12,10,11][i%5],bottom=Math.min(y+pitch+1,end+2);
  art+=p(`M119 ${y} Q129 ${y-5} 141 ${y-2} Q143 ${y+3} 141 ${bottom-3} Q130 ${bottom-6} 119 ${bottom} Q117 ${y+6} 119 ${y}Z`,q[1]);
  art+=p(`M120 ${y+1} Q126 ${y-2} 133 ${y-1} L131 ${y+2} L127 ${y+2} L124 ${y+5} L120 ${y+6}Z`,q[3],'opacity=".4"');
  art+=p(`M${121+i%3} ${y+4} l4 -1 2 2 -3 1 -2 2 -2 -1Z`,mixPaint(q[1],q[3],.5),'opacity=".32"');
  art+=p(`M134 ${y-2} L141 ${y-2} V${bottom-3} L134 ${bottom-4}Z`,q[2],'opacity=".7"');
  art+=line(`M119 ${bottom} Q130 ${bottom-6} 140 ${bottom-3}`,'#211e1d',1.3);
  art+=line(`M121 ${bottom-2} Q125 ${bottom-4} 129 ${bottom-4}`,q[3],.65);
  y+=pitch;
 }
 art+=p(`M118 ${start} Q130 ${start+5} 142 ${start} V${start+6} Q130 ${start+10} 118 ${start+5}Z`,'#211e1d','opacity=".65"');
 return `<g data-construction="overlapping-leather">${art}</g>`;
}

export function roundedCollar(y,left,right,f,{p,line}){
 return p(`M${left} ${y} Q130 ${y-5} ${right} ${y} L${right+1} ${y+12} Q130 ${y+18} ${left-1} ${y+12}Z`,f[2],'stroke="#24211f" stroke-width="1.7"')
  +p(`M${left} ${y} Q130 ${y+5} ${right} ${y} V${y+9} Q130 ${y+15} ${left} ${y+9}Z`,f[1])
  +p(`M${left+3} ${y+1} L${left+9} ${y+3} V${y+11} L${left+3} ${y+9}Z`,f[3])
  +p(`M${left} ${y} Q130 ${y-5} ${right} ${y} Q132 ${y+5} ${left} ${y}Z`,f[1])
  +p(`M${left+5} ${y} Q130 ${y-2} ${right-4} ${y} Q130 ${y+2} ${left+5} ${y}Z`,f[2])
  +line(`M${left+2} ${y} Q123 ${y+4} 132 ${y+3}`,f[3],1.3)
  +line(`M${left+2} ${y+11} Q124 ${y+14} 132 ${y+13}`,mixPaint(f[3],f[1],.4),.75);
}
