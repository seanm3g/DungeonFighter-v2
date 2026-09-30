// Offline preparation; runtime rendering uses only Canvas and standard PNGs.
// Run with `node raster/prepare-mace.mjs [absolute path to sharp]`.
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {writeFile} from 'node:fs/promises';
const require=createRequire(import.meta.url),sharp=require(process.argv[2]||'sharp');
const root=new URL('../assets/painted/',import.meta.url);
const read=async name=>sharp(fileURLToPath(new URL(name,root))).removeAlpha().raw().toBuffer({resolveWithObject:true});
const source=await read('steel-mace-study-v1.png'),matte=await read('steel-mace-matte-source-v1.png');
const {width:w,height:h}=source.info,n=w*h;
if(matte.info.width!==w||matte.info.height!==h)throw Error('Matte and painting dimensions must match');
// The extraction source has a neutral checker field and a dark closed outline.
// Flood only the border-connected field, preserving enclosed light brushwork.
const background=new Uint8Array(n),queue=new Int32Array(n);let first=0,last=0;
function add(i){if(i<0||i>=n||background[i])return;const p=i*3,r=matte.data[p],g=matte.data[p+1],b=matte.data[p+2];if(Math.min(r,g,b)<182||Math.max(r,g,b)-Math.min(r,g,b)>26)return;background[i]=1;queue[last++]=i;}
for(let x=0;x<w;x++){add(x);add((h-1)*w+x);}for(let y=0;y<h;y++){add(y*w);add(y*w+w-1);}
while(first<last){const i=queue[first++],x=i%w;add(i-w);add(i+w);if(x)add(i-1);if(x<w-1)add(i+1);}
// Keep the largest foreground component; discard checker compression flecks.
const seen=new Uint8Array(n);let largest=[];
for(let i=0;i<n;i++)if(!background[i]&&!seen[i]){first=0;last=1;queue[0]=i;seen[i]=1;while(first<last){const j=queue[first++],x=j%w;for(const k of [j-w,j+w,...(x?[j-1]:[]),...(x<w-1?[j+1]:[])])if(k>=0&&k<n&&!background[k]&&!seen[k]){seen[k]=1;queue[last++]=k;}}if(last>largest.length)largest=Array.from(queue.subarray(0,last));}
const solid=new Uint8Array(n);for(const i of largest)solid[i]=1;
const rgba=Buffer.alloc(n*4),regions=Buffer.alloc(n*4);
function inside(x,y,poly){let yes=false;for(let i=0,j=poly.length-1;i<poly.length;j=i++){const [a,b]=poly[i],[c,d]=poly[j];if((b>y)!==(d>y)&&x<(c-a)*(y-b)/(d-b)+a)yes=!yes;}return yes;}
const ears=[[[688,100],[722,76],[756,111],[746,130],[710,122]],[[759,118],[822,79],[851,99],[857,174],[817,145]]];
const socket=[[425,527],[447,534],[475,534],[505,541],[538,549],[566,561],[596,580],[622,600],[650,623],[671,654],[695,661]];
function boundary(x){for(let j=1;j<socket.length;j++){const [a,b]=socket[j-1],[c,d]=socket[j];if(x<=c)return b+(d-b)*(x-a)/(c-a);}return 661;}
for(const i of largest){const x=i%w,y=Math.floor(i/w),p=i*4,s=i*3;
 // Inward antialiasing avoids importing pale background pixels into the edge.
 let neighbors=0;for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++)neighbors+=solid[(y+dy)*w+x+dx]||0;
 const alpha=neighbors===9?255:Math.round(255*(.35+.65*neighbors/9));
 rgba[p]=source.data[s];rgba[p+1]=source.data[s+1];rgba[p+2]=source.data[s+2];rgba[p+3]=alpha;
 if(neighbors<9){let count=0,sum=[0,0,0];for(let dy=-3;dy<=3;dy++)for(let dx=-3;dx<=3;dx++){const k=(y+dy)*w+x+dx;if(solid[k]&&solid[k-1]&&solid[k+1]&&solid[k-w]&&solid[k+w]){for(let c=0;c<3;c++)sum[c]+=source.data[k*3+c];count++;}}if(count)for(let c=0;c<3;c++)rgba[p+c]=Math.round(sum[c]/count);}
 const along=y-.52*x;const region=ears.some(poly=>inside(x,y,poly))?2:y<boundary(x)?0:along<446?2:along<1080?1:2;
 regions[p+region]=255;regions[p+3]=255;
}
await sharp(rgba,{raw:{width:w,height:h,channels:4}}).png().toFile(fileURLToPath(new URL('steel-mace-base-v1.png',root)));
await sharp(regions,{raw:{width:w,height:h,channels:4}}).png().toFile(fileURLToPath(new URL('steel-mace-regions-v1.png',root)));
const report={width:w,height:h,foregroundPixels:largest.length,transparentPixels:n-largest.length,regions:{red:'head',green:'grip',blue:'fittings'},source:'steel-mace-study-v1.png',matteSource:'steel-mace-matte-source-v1.png'};
await writeFile(new URL('steel-mace-preparation-v1.json',root),JSON.stringify(report,null,2));console.log(report);
