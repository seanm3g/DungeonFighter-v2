// Offline preparation; runtime rendering uses only Canvas and standard PNGs.
// Run with `node raster/prepare-weapons.mjs [absolute path to sharp] sword|dagger|wand`.
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {writeFile} from 'node:fs/promises';
const require=createRequire(import.meta.url),sharp=require(process.argv[2]||'sharp');
const root=new URL('../assets/painted/',import.meta.url);
const read=async name=>sharp(fileURLToPath(new URL(name,root))).removeAlpha().raw().toBuffer({resolveWithObject:true});
const id=process.argv[3];
if(!['sword','dagger','wand','mace'].includes(id))throw Error('Specify sword, dagger or wand after sharp path');
const version=['mace','dagger'].includes(id)?'v2':'v1';
const source=await read('steel-'+id+'-study-'+version+'.png'),matte=source;
const {width:w,height:h}=source.info,n=w*h;
if(matte.info.width!==w||matte.info.height!==h)throw Error('Matte and painting dimensions must match');
// The extraction source has a neutral checker field and a dark closed outline.
// Flood only the border-connected field, preserving enclosed light brushwork.
const background=new Uint8Array(n),queue=new Int32Array(n);let first=0,last=0;
function add(i){if(i<0||i>=n||background[i])return;const p=i*3,r=matte.data[p],g=matte.data[p+1],b=matte.data[p+2];if(Math.min(r,g,b)<(id==='dagger'?175:220)||Math.max(r,g,b)-Math.min(r,g,b)>20)return;background[i]=1;queue[last++]=i;}
for(let x=0;x<w;x++){add(x);add((h-1)*w+x);}for(let y=0;y<h;y++){add(y*w);add(y*w+w-1);}
while(first<last){const i=queue[first++],x=i%w;add(i-w);add(i+w);if(x)add(i-1);if(x<w-1)add(i+1);}
// Keep the largest foreground component; discard checker compression flecks.
const seen=new Uint8Array(n);let largest=[];
for(let i=0;i<n;i++)if(!background[i]&&!seen[i]){first=0;last=1;queue[0]=i;seen[i]=1;while(first<last){const j=queue[first++],x=j%w;for(const k of [j-w,j+w,...(x?[j-1]:[]),...(x<w-1?[j+1]:[])])if(k>=0&&k<n&&!background[k]&&!seen[k]){seen[k]=1;queue[last++]=k;}}if(last>largest.length)largest=Array.from(queue.subarray(0,last));}
const solid=new Uint8Array(n);for(const i of largest)solid[i]=1;
const rgba=Buffer.alloc(n*4),regions=Buffer.alloc(n*4);
function inside(x,y,poly){let yes=false;for(let i=0,j=poly.length-1;i<poly.length;j=i++){const [a,b]=poly[i],[c,d]=poly[j];if((b>y)!==(d>y)&&x<(c-a)*(y-b)/(d-b)+a)yes=!yes;}return yes;}
const shapes={
 mace:{head:[[580,350],[600,130],[742,84],[865,80],[915,315],[750,446],[715,408],[677,386],[642,372],[616,366]],grip:[]},
 sword:{head:[[328,941],[852,130],[991,18],[945,185],[443,1008],[388,966]],grip:[[292,1022],[319,1028],[348,1042],[374,1060],[397,1080],[232,1320],[207,1300],[175,1280],[139,1270]]},
 dagger:{head:[[379,770],[675,295],[954,50],[863,420],[605,900],[526,845],[440,805]],grip:[[355,900],[420,918],[496,983],[323,1286],[252,1241],[180,1206]]},
 wand:{head:[[694,326],[742,190],[971,20],[964,248],[856,397],[780,451],[795,340],[741,371],[691,414]],grip:[[644,518],[682,530],[717,560],[266,1315],[212,1280],[170,1265]]}
};
for(const i of largest){const x=i%w,y=Math.floor(i/w),p=i*4,s=i*3;
 // Inward antialiasing avoids importing pale background pixels into the edge.
 let neighbors=0;for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++)neighbors+=solid[(y+dy)*w+x+dx]||0;
 const alpha=neighbors===9?255:Math.round(255*(.35+.65*neighbors/9));
 rgba[p]=source.data[s];rgba[p+1]=source.data[s+1];rgba[p+2]=source.data[s+2];rgba[p+3]=alpha;
 if(neighbors<9){let count=0,sum=[0,0,0];for(let dy=-3;dy<=3;dy++)for(let dx=-3;dx<=3;dx++){const k=(y+dy)*w+x+dx;if(solid[k]&&solid[k-1]&&solid[k+1]&&solid[k-w]&&solid[k+w]){for(let c=0;c<3;c++)sum[c]+=source.data[k*3+c];count++;}}if(count)for(let c=0;c<3;c++)rgba[p+c]=Math.round(sum[c]/count);}
 const along=y-.6*x;
 let head=inside(x,y,shapes[id].head);
 if(id==='mace'&&y<135&&source.data[s]>source.data[s+2]*1.5)head=false;
 const range=id==='sword'?[845,1183]:id==='dagger'?[688,1098]:[132,1163];
 const grip=id==='sword'?inside(x,y,shapes[id].grip):id==='mace'?(y-.52*x>145&&y-.52*x<1126):along>range[0]&&along<range[1];
 const region=head?0:grip?1:2;
 regions[p+region]=255;regions[p+3]=255;
}
await sharp(rgba,{raw:{width:w,height:h,channels:4}}).png().toFile(fileURLToPath(new URL('steel-'+id+'-base-'+version+'.png',root)));
await sharp(regions,{raw:{width:w,height:h,channels:4}}).png().toFile(fileURLToPath(new URL('steel-'+id+'-regions-'+version+'.png',root)));
const report={width:w,height:h,foregroundPixels:largest.length,transparentPixels:n-largest.length,regions:{red:'head',green:'grip',blue:'fittings'},source:'steel-'+id+'-study-'+version+'.png',polygons:shapes[id]};
await writeFile(new URL('steel-'+id+'-preparation-'+version+'.json',root),JSON.stringify(report,null,2));console.log(report);
