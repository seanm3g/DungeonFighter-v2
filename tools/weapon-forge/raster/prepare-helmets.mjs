// Offline preparation; runtime rendering uses only Canvas and standard PNGs.
// Run with `node raster/prepare-helmets.mjs [absolute path to sharp] closed|open|horned`.
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {writeFile} from 'node:fs/promises';
const require=createRequire(import.meta.url),sharp=require(process.argv[2]||'sharp');
const root=new URL('../assets/painted/helmets/',import.meta.url);
const read=async name=>sharp(fileURLToPath(new URL(name,root))).removeAlpha().raw().toBuffer({resolveWithObject:true});
const id=process.argv[3];
if(!['closed','open','horned'].includes(id))throw Error('Specify closed, open or horned after sharp path');
const version='v2';
const source=await read(id+'-study-'+version+'.png'),matte=source;
const {width:w,height:h}=source.info,n=w*h;
if(matte.info.width!==w||matte.info.height!==h)throw Error('Matte and painting dimensions must match');
// The extraction source has a neutral checker field and a dark closed outline.
// Flood only the border-connected field, preserving enclosed light brushwork.
const background=new Uint8Array(n),queue=new Int32Array(n);let first=0,last=0;
function add(i){if(i<0||i>=n||background[i])return;const p=i*3,r=matte.data[p],g=matte.data[p+1],b=matte.data[p+2];if(Math.min(r,g,b)<220||Math.max(r,g,b)-Math.min(r,g,b)>20)return;background[i]=1;queue[last++]=i;}
for(let x=0;x<w;x++){add(x);add((h-1)*w+x);}for(let y=0;y<h;y++){add(y*w);add(y*w+w-1);}
while(first<last){const i=queue[first++],x=i%w;add(i-w);add(i+w);if(x)add(i-1);if(x<w-1)add(i+1);}
// Keep the largest foreground component; discard checker compression flecks.
const seen=new Uint8Array(n);let largest=[];
for(let i=0;i<n;i++)if(!background[i]&&!seen[i]){first=0;last=1;queue[0]=i;seen[i]=1;while(first<last){const j=queue[first++],x=j%w;for(const k of [j-w,j+w,...(x?[j-1]:[]),...(x<w-1?[j+1]:[])])if(k>=0&&k<n&&!background[k]&&!seen[k]){seen[k]=1;queue[last++]=k;}}if(last>largest.length)largest=Array.from(queue.subarray(0,last));}
const solid=new Uint8Array(n);for(const i of largest)solid[i]=1;
const rgba=Buffer.alloc(n*4),regions=Buffer.alloc(n*4);
function inside(x,y,poly){let yes=false;for(let i=0,j=poly.length-1;i<poly.length;j=i++){const [a,b]=poly[i],[c,d]=poly[j];if((b>y)!==(d>y)&&x<(c-a)*(y-b)/(d-b)+a)yes=!yes;}return yes;}
const shapes={
 closed:{leather:[[[0,1090],[115,1078],[197,1125],[300,1170],[420,1210],[522,1234],[619,1220],[731,1183],[829,1140],[912,1078],[1024,1090],[1024,1536],[0,1536]]],fixed:[]},
 open:{leather:[[[185,680],[390,620],[395,910],[438,958],[499,911],[509,620],[769,675],[769,763],[742,853],[709,914],[705,1113],[663,1065],[631,1007],[365,1007],[319,1071],[266,1118],[268,909],[230,860],[206,756]]],fixed:[]},
 horned:{leather:[[[270,721],[480,782],[479,848],[275,765]],[[577,782],[754,724],[751,769],[577,847]],[[379,901],[472,947],[530,998],[582,942],[658,903],[680,1160],[620,1120],[510,1065],[417,1100],[362,1145]]],fixed:[[[0,0],[300,0],[274,439],[268,477],[248,530],[218,580],[174,620],[0,660]],[[724,0],[1024,0],[1024,660],[849,620],[817,582],[790,530],[770,477],[748,439]]]}
};
for(const i of largest){const x=i%w,y=Math.floor(i/w),p=i*4,s=i*3;
 // Inward antialiasing avoids importing pale background pixels into the edge.
 let neighbors=0;for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++)neighbors+=solid[(y+dy)*w+x+dx]||0;
 const alpha=neighbors===9?255:Math.round(255*(.35+.65*neighbors/9));
 rgba[p]=source.data[s];rgba[p+1]=source.data[s+1];rgba[p+2]=source.data[s+2];rgba[p+3]=alpha;
 if(neighbors<9){let count=0,sum=[0,0,0];for(let dy=-3;dy<=3;dy++)for(let dx=-3;dx<=3;dx++){const k=(y+dy)*w+x+dx;if(solid[k]&&solid[k-1]&&solid[k+1]&&solid[k-w]&&solid[k+w]){for(let c=0;c<3;c++)sum[c]+=source.data[k*3+c];count++;}}if(count)for(let c=0;c<3;c++)rgba[p+c]=Math.round(sum[c]/count);}
 const shape=shapes[id];
 const fixed=shape.fixed.some(poly=>inside(x,y,poly));
 const leather=shape.leather.some(poly=>inside(x,y,poly));
 // Brass pigment separates fittings from neutral steel within the authored material layout.
 const r=source.data[s],g=source.data[s+1],b=source.data[s+2];

 const circle=(cx,cy,radius)=>(x-cx)**2+(y-cy)**2<radius*radius;
 const trimArea=id==='closed'
 ? (y>995||[[510,182,40],[516,223,20],[523,377,22],[523,446,25],[522,632,25],[205,409,20],[175,479,20],[155,617,20],[808,409,20],[837,479,20],[858,617,20]].some(v=>circle(...v)))
 :id==='open'
 ? (inside(x,y,[[122,580],[390,511],[390,624],[180,683],[184,748],[115,745]])||inside(x,y,[[510,512],[893,582],[893,755],[811,756],[804,685],[510,623]]))
 : (inside(x,y,[[168,621],[230,535],[274,437],[340,454],[310,567],[261,649],[210,680]])||inside(x,y,[[683,454],[751,437],[789,535],[856,621],[814,680],[760,649],[711,567]])||inside(x,y,[[222,680],[480,739],[530,629],[577,741],[798,680],[800,779],[686,845],[710,1182],[699,1234],[673,1205],[654,827],[753,765],[757,716],[575,772],[581,945],[530,999],[471,947],[480,775],[272,716],[275,765],[390,827],[367,1206],[338,1237],[329,1183],[365,845],[222,779]])||[[338,861,22],[707,858,22],[318,1138,24],[726,1138,24]].some(v=>circle(...v)));
 const gold=!leather&&trimArea&&r>b*1.2&&g>b*1.08&&r-g>5;
 const region=leather?1:gold?2:0;
 if(!fixed)regions[p+region]=255;
 regions[p+3]=255;
}
await sharp(rgba,{raw:{width:w,height:h,channels:4}}).png().toFile(fileURLToPath(new URL(id+'-base-'+version+'.png',root)));
await sharp(regions,{raw:{width:w,height:h,channels:4}}).png().toFile(fileURLToPath(new URL(id+'-regions-'+version+'.png',root)));
const report={width:w,height:h,foregroundPixels:largest.length,transparentPixels:n-largest.length,regions:{red:'head',green:'grip',blue:'fittings'},source:id+'-study-'+version+'.png',polygons:shapes[id]};
await writeFile(new URL(id+'-preparation-'+version+'.json',root),JSON.stringify(report,null,2));console.log(report);
