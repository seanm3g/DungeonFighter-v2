// A restrained warm key and cool bounce, expressed as discrete painted values.
export function mixPaint(a,b,t){
 const rgb=s=>s.slice(1).match(/../g).map(v=>parseInt(v,16));
 const x=rgb(a),y=rgb(b);
 return '#'+x.map((v,i)=>Math.round(v+(y[i]-v)*t).toString(16).padStart(2,'0')).join('');
}
export function paintLight(m){
 return {key:mixPaint(m[3],'#f0dfb5',.28),mid:mixPaint(m[1],m[3],.3),deep:mixPaint(m[2],'#181b25',.32),bounce:mixPaint(m[2],'#737f8b',.22)};
}
