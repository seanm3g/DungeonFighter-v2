import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
const sharp=createRequire(import.meta.url)(process.argv[2]||'sharp');
const root=new URL('./',import.meta.url);
for(const id of ['mail','chestplate','tunic','coat']){
 const {data,info}=await sharp(fileURLToPath(new URL(id+'-study-v2.png',root))).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 const {width:w,height:h}=info,n=w*h,q=new Int32Array(n),bg=new Uint8Array(n);let first=0,last=0;
 function add(i){if(i<0||i>=n||bg[i])return;const p=i*4,r=data[p],g=data[p+1],b=data[p+2];if(data[p+3]>0&&(Math.min(r,g,b)<225||Math.max(r,g,b)-Math.min(r,g,b)>18))return;bg[i]=1;q[last++]=i;}
 for(let x=0;x<w;x++){add(x);add((h-1)*w+x);}for(let y=0;y<h;y++){add(y*w);add(y*w+w-1);}
 while(first<last){const i=q[first++],x=i%w;add(i-w);add(i+w);if(x)add(i-1);if(x<w-1)add(i+1);}
 const out=Buffer.from(data);
 for(let i=0;i<n;i++){if(bg[i]){out[i*4+3]=0;continue;}const x=i%w,y=Math.floor(i/w);let count=0;
 for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++){const k=(y+dy)*w+x+dx;if(k>=0&&k<n&&!bg[k])count++;}
 if(count<9)out[i*4+3]=Math.round(out[i*4+3]*count/9);
 }
 await sharp(out,{raw:{width:w,height:h,channels:4}}).png().toFile(fileURLToPath(new URL(id+'-base-v2.png',root)));
 console.log(id,w,h,'transparent pixels',last);
}
