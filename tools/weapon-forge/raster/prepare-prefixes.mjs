// Offline artwork preparation: node raster/prepare-prefixes.mjs /path/to/sharp
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {writeFile} from 'node:fs/promises';
import {ASSETS} from './manifest.js';
import {PREFIXES,prefixSurface,prefixMapURL} from './prefixes.js';
const sharp=createRequire(import.meta.url)(process.argv[2]||'sharp');
const root=new URL('../',import.meta.url),path=s=>fileURLToPath(new URL(s,root));
const report=[];
for(const prefix of PREFIXES.slice(1)){
 const {data,info}=await sharp(path(`assets/painted/prefixes/${prefix.id}-source-v1.png`)).removeAlpha().raw().toBuffer({resolveWithObject:true});
 const {width:w,height:h}=info,rgba=Buffer.alloc(w*h*4);let left=w,top=h,right=0,bottom=0;
 // Sources have neutral white negative spaces, including enclosed rune holes.
 for(let i=0;i<w*h;i++){const p=i*3,min=Math.min(data[p],data[p+1],data[p+2]);if(min>225)continue;const x=i%w,y=Math.floor(i/w),q=i*4;
  const v=Math.round(.2126*data[p]+.7152*data[p+1]+.0722*data[p+2]);rgba[q]=rgba[q+1]=rgba[q+2]=v;rgba[q+3]=255;left=Math.min(left,x);right=Math.max(right,x);top=Math.min(top,y);bottom=Math.max(bottom,y);
 }
 const crop={left,top,width:right-left+1,height:bottom-top+1};
 const cutout=await sharp(rgba,{raw:{width:w,height:h,channels:4}}).extract(crop).png().toBuffer();
 await sharp(cutout).toFile(path(`assets/painted/prefixes/${prefix.id}-cutout-v1.png`));
 for(const asset of ASSETS.filter(a=>a.supportsPrefixes!==false)){
  const surface=prefixSurface(asset.id,prefix.id),size=surface.size;
  const {data:stamp,info:si}=await sharp(cutout).resize(size[0],size[1],{fit:'inside'}).raw().toBuffer({resolveWithObject:true});
  const {data:mask}=await sharp(path(asset.mask)).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const {data:base}=await sharp(path(asset.base)).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const out=Buffer.alloc(asset.width*asset.height*4),a=surface.angle*Math.PI/180,co=Math.cos(a),sn=Math.sin(a);let count=0;
  for(let y=0;y<asset.height;y++)for(let x=0;x<asset.width;x++){
   const p=(y*asset.width+x)*4;if(base[p+3]!==255||mask[p+surface.region]!==255)continue;
   const dx=x-surface.center[0],dy=y-surface.center[1],v=-sn*dx+co*dy,u=co*dx+sn*dy-surface.shear*v;
   const sx=Math.round(u+si.width/2),sy=Math.round(v+si.height/2);if(sx<0||sy<0||sx>=si.width||sy>=si.height)continue;
   const q=(sy*si.width+sx)*4;if(!stamp[q+3])continue;
   out[p]=out[p+1]=out[p+2]=stamp[q];out[p+3]=stamp[q+3];count++;
  }
  if(count<100)throw Error('Empty prefix placement '+asset.id+' '+prefix.id);
  await sharp(out,{raw:{width:asset.width,height:asset.height,channels:4}}).png().toFile(path(prefixMapURL(asset,prefix.id)));
  report.push({asset:asset.id,prefix:prefix.id,pixels:count,surface});
 }
}
await writeFile(path('assets/painted/prefixes/preparation-v1.json'),JSON.stringify(report,null,2));console.log('Prepared',report.length,'weapon-specific relief maps');
