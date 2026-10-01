import fs from 'node:fs/promises';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
const sharp=createRequire(import.meta.url)(process.argv[2]||'sharp'),root=new URL('../',import.meta.url),assets=JSON.parse(await fs.readFile(new URL('raster/assets.json',root))),files=JSON.parse(await fs.readFile(new URL('source-inventory.json',root)));
const surfaces={mace:[745,265,106,164,27],sword:[465,855,90,195,33],dagger:[558,696,94,190,30],wand:[845,246,128,185,32]};
for(const [prefix,index] of [['flaming',25],['poisonous',11],['charged',9],['enchanted',15],['blessed',1]]){
 const {data,info}=await sharp('C:/Users/Seanye/.codex/generated_images/01a0cc1a-be9e-7ac1-b982-ec7780ce733b/'+files[index]).removeAlpha().raw().toBuffer({resolveWithObject:true});
 const rgba=Buffer.alloc(info.width*info.height*4);let l=info.width,t=info.height,r=0,b=0;
 for(let i=0;i<info.width*info.height;i++){if(Math.min(...data.subarray(i*3,i*3+3))>225)continue;const x=i%info.width,y=Math.floor(i/info.width),p=i*4;rgba[p]=rgba[p+1]=rgba[p+2]=Math.round((data[i*3]+data[i*3+1]+data[i*3+2])/3);rgba[p+3]=255;l=Math.min(l,x);r=Math.max(r,x);t=Math.min(t,y);b=Math.max(b,y);}
 const crop=await sharp(rgba,{raw:{width:info.width,height:info.height,channels:4}}).extract({left:l,top:t,width:r-l+1,height:b-t+1}).png().toBuffer();
 for(const a of assets.filter(x=>x.supportsPrefixes)){
 const type=a.id.split('-')[1],[cx,cy,w,h,angle]=surfaces[type],rad=(angle+(prefix==='charged'?-16:0))*Math.PI/180;
 const stamp=await sharp(crop).resize(w,prefix==='blessed'?w:h,{fit:'inside'}).raw().toBuffer({resolveWithObject:true}),mask=await sharp(fileURLToPath(new URL(a.mask,root))).raw().toBuffer(),base=await sharp(fileURLToPath(new URL(a.base,root))).raw().toBuffer(),out=Buffer.alloc(base.length);
 for(let y=0;y<a.height;y++)for(let x=0;x<a.width;x++){const p=(y*a.width+x)*4;if(!base[p+3]||mask[p]!==255)continue;const dx=x-cx,dy=y-cy,u=Math.round(Math.cos(rad)*dx+Math.sin(rad)*dy+stamp.info.width/2),v=Math.round(-Math.sin(rad)*dx+Math.cos(rad)*dy+stamp.info.height/2);if(u<0||v<0||u>=stamp.info.width||v>=stamp.info.height)continue;stamp.data.copy(out,p,(v*stamp.info.width+u)*4,(v*stamp.info.width+u)*4+4);}
 await sharp(out,{raw:{width:a.width,height:a.height,channels:4}}).png().toFile(fileURLToPath(new URL('assets/restored/'+type+'-'+prefix+'.png',root)));
 }
}
console.log('20 affix maps restored');
