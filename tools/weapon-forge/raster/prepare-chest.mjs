// Offline preparation; runtime rendering uses only Canvas and standard PNGs.
// Run with `node raster/prepare-chest.mjs [absolute path to sharp] chestplate|mail|tunic|coat`.
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {writeFile} from 'node:fs/promises';
const require=createRequire(import.meta.url),sharp=require(process.argv[2]||'sharp');
const root=new URL('../assets/painted/chest/',import.meta.url);
const read=async name=>sharp(fileURLToPath(new URL(name,root))).removeAlpha().raw().toBuffer({resolveWithObject:true});
const id=process.argv[3];
if(!['chestplate','mail','tunic','coat'].includes(id))throw Error('Specify chestplate, mail, tunic or coat after sharp path');
const version='v1';
const source=await read(id+'-study-'+version+'.png'),matte=source;
const {width:w,height:h}=source.info,n=w*h;
const inputPath=fileURLToPath(new URL(id+'-study-'+version+'.png',root));
const hasAlpha=(await sharp(inputPath).metadata()).hasAlpha;
const originalRGBA=hasAlpha?await sharp(inputPath).ensureAlpha().raw().toBuffer():null;
if(matte.info.width!==w||matte.info.height!==h)throw Error('Matte and painting dimensions must match');
// The extraction source has a neutral checker field and a dark closed outline.
// Flood only the border-connected field, preserving enclosed light brushwork.
const background=new Uint8Array(n),queue=new Int32Array(n);let first=0,last=0;
function add(i){if(i<0||i>=n||background[i])return;const p=i*3,r=matte.data[p],g=matte.data[p+1],b=matte.data[p+2];if(Math.min(r,g,b)<175||Math.max(r,g,b)-Math.min(r,g,b)>20)return;background[i]=1;queue[last++]=i;}
for(let x=0;x<w;x++){add(x);add((h-1)*w+x);}for(let y=0;y<h;y++){add(y*w);add(y*w+w-1);}
while(first<last){const i=queue[first++],x=i%w;add(i-w);add(i+w);if(x)add(i-1);if(x<w-1)add(i+1);}
// Keep the largest foreground component; discard checker compression flecks.
const seen=new Uint8Array(n);let largest=[];
for(let i=0;i<n;i++)if(!background[i]&&!seen[i]){first=0;last=1;queue[0]=i;seen[i]=1;while(first<last){const j=queue[first++],x=j%w;for(const k of [j-w,j+w,...(x?[j-1]:[]),...(x<w-1?[j+1]:[])])if(k>=0&&k<n&&!background[k]&&!seen[k]){seen[k]=1;queue[last++]=k;}}if(last>largest.length)largest=Array.from(queue.subarray(0,last));}
if(hasAlpha)largest=Array.from({length:n},(_,i)=>i).filter(i=>originalRGBA[i*4+3]>0);
const solid=new Uint8Array(n);for(const i of largest)solid[i]=1;
const rgba=Buffer.alloc(n*4),regions=Buffer.alloc(n*4);
function inside(x,y,poly){let yes=false;for(let i=0,j=poly.length-1;i<poly.length;j=i++){const [a,b]=poly[i],[c,d]=poly[j];if((b>y)!==(d>y)&&x<(c-a)*(y-b)/(d-b)+a)yes=!yes;}return yes;}

const shapes={
 chestplate:{leather:[
 [[342,145],[382,173],[445,190],[591,189],[638,168],[676,143],[672,199],[642,242],[591,269],[515,284],[433,277],[384,250],[353,211]],
 [[281,166],[333,150],[360,247],[354,287],[341,301],[311,298]],
 [[690,150],[741,170],[714,299],[681,299],[665,276]],
 [[36,521],[147,536],[194,496],[231,421],[257,415],[236,506],[219,554],[172,592],[161,579],[177,535],[137,579]],
 [[768,415],[796,428],[820,494],[880,536],[991,517],[882,579],[842,546],[864,592],[817,560],[794,518]],
 [[170,614],[195,627],[219,701],[249,708],[256,764],[244,794],[259,828],[261,877],[237,884],[230,925],[202,912],[207,880],[191,873],[189,818],[210,803],[205,775],[180,774]],
 [[854,614],[829,627],[805,701],[776,707],[766,764],[784,795],[765,830],[766,878],[788,887],[792,925],[821,910],[813,878],[832,875],[835,818],[815,803],[820,775],[845,774]],
 [[112,1160],[152,1191],[127,1250],[96,1222]],[[873,1191],[910,1160],[930,1223],[896,1250]]
 ],hardware:[]},
 mail:{leather:[
 [[334,197],[374,164],[453,170],[589,172],[650,166],[690,198],[705,243],[668,294],[605,326],[526,343],[445,335],[374,307],[338,266]],
 [[22,547],[48,508],[112,553],[168,585],[236,611],[217,666],[152,646],[82,609]],
 [[790,611],[855,586],[929,544],[978,509],[1004,548],[955,592],[883,637],[805,664]],
 [[255,792],[341,818],[429,831],[578,831],[685,819],[772,794],[791,860],[722,897],[620,917],[510,929],[395,909],[313,890],[242,857]],
 [[154,1270],[239,1307],[365,1344],[520,1364],[680,1345],[804,1310],[876,1278],[897,1335],[835,1371],[711,1401],[543,1423],[395,1413],[265,1386],[173,1354],[139,1326]]
 ],hardware:[[347,193,15],[361,243,18],[437,300,18],[595,297,18],[663,244,17],[676,194,14],[41,537,15],[181,625,17],[841,626,17],[983,537,15],[158,1307,16],[305,1363,18],[720,1373,17],[875,1315,14]]},
 tunic:{leather:[
 [[369,174],[397,150],[610,150],[654,173],[685,218],[679,250],[629,280],[536,295],[445,285],[377,253],[354,214]],
 [[29,489],[89,500],[148,535],[197,580],[232,623],[226,654],[204,674],[153,665],[78,627],[17,566],[8,524]],
 [[826,614],[870,568],[923,531],[989,493],[1011,516],[1018,545],[989,599],[924,638],[856,672],[827,657]],
 [[281,758],[375,787],[473,800],[590,803],[690,790],[777,762],[798,871],[737,897],[676,904],[564,900],[478,900],[367,878],[270,844]],
 [[151,1141],[187,1196],[239,1234],[331,1293],[431,1326],[544,1341],[651,1327],[748,1285],[835,1220],[899,1147],[916,1189],[883,1239],[802,1307],[696,1354],[584,1379],[466,1373],[351,1343],[256,1298],[174,1238],[138,1186]]
 ],hardware:[]},
 coat:{leather:[
 [[283,255],[322,154],[382,112],[403,66],[462,48],[591,48],[634,86],[677,141],[718,181],[755,259],[640,222],[620,232],[731,310],[651,366],[564,412],[507,456],[417,400],[310,312],[405,258],[390,233]],
 [[77,561],[139,584],[211,622],[263,650],[227,761],[169,760],[91,728],[38,690]],
 [[769,649],[852,608],[946,561],[986,687],[930,729],[842,767],[798,758]],
 [[319,622],[389,645],[482,654],[585,654],[668,641],[714,625],[727,699],[650,728],[566,748],[460,747],[376,726],[310,699]],
 [[507,448],[522,457],[511,577],[505,647],[490,647],[491,521]],
 [[486,746],[507,749],[515,872],[547,974],[573,1107],[605,1286],[621,1451],[650,1457],[782,1395],[827,1350],[839,1377],[784,1421],[619,1478],[593,1370],[573,1234],[539,1038],[517,947],[493,1114],[442,1350],[409,1470],[291,1425],[188,1370],[189,1344],[281,1396],[388,1430],[426,1274],[470,1058],[494,919]],
 [[115,1203],[135,1244],[176,1270],[201,1280],[197,1310],[135,1279],[107,1235]],
 [[862,1270],[895,1240],[909,1198],[921,1235],[883,1280],[833,1310],[828,1283]]
 ],hardware:[[530,497,25],[527,607,25],[533,803,25],[569,975,26],[80,605,22],[942,604,22]]}
};
const circles=(x,y,list)=>list.some(([cx,cy,r])=>(x-cx)**2+(y-cy)**2<r*r);
function buckle(x,y){
 const bounds=id==='mail'?[430,819,570,938]:id==='tunic'?[466,796,630,904]:id==='coat'?[484,646,585,751]:null;
 if(!bounds)return false;
 const [l,t,r,b]=bounds;
 return x>=l&&x<=r&&y>=t&&y<=b&&!(x>l+23&&x<r-22&&y>t+23&&y<b-20);
}
for(const i of largest){
 const x=i%w,y=Math.floor(i/w),p=i*4,s=i*3;
 let neighbors=0;for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++)neighbors+=solid[(y+dy)*w+x+dx]||0;
 const alpha=hasAlpha?originalRGBA[p+3]:neighbors===9?255:Math.round(255*(.35+.65*neighbors/9));
 rgba[p]=source.data[s];rgba[p+1]=source.data[s+1];rgba[p+2]=source.data[s+2];rgba[p+3]=alpha;
 if(!hasAlpha&&neighbors<9){let count=0,sum=[0,0,0];for(let dy=-3;dy<=3;dy++)for(let dx=-3;dx<=3;dx++){const k=(y+dy)*w+x+dx;if(solid[k]&&solid[k-1]&&solid[k+1]&&solid[k-w]&&solid[k+w]){for(let c=0;c<3;c++)sum[c]+=source.data[k*3+c];count++;}}if(count)for(let c=0;c<3;c++)rgba[p+c]=Math.round(sum[c]/count);}
 const shape=shapes[id],leather=shape.leather.some(poly=>inside(x,y,poly));
 const r=source.data[s],g=source.data[s+1],b=source.data[s+2];
 const warm=r>b*1.32&&g>b*1.12&&r-g>7;
 const gold=id==='chestplate'?warm&&(!leather||r>145&&g>95):circles(x,y,shape.hardware)||buckle(x,y)&&warm;
 const region=gold?2:leather?1:0;
 regions[p+region]=255;regions[p+3]=255;
}
await sharp(rgba,{raw:{width:w,height:h,channels:4}}).png().toFile(fileURLToPath(new URL(id+'-base-'+version+'.png',root)));
await sharp(regions,{raw:{width:w,height:h,channels:4}}).png().toFile(fileURLToPath(new URL(id+'-regions-'+version+'.png',root)));
const report={width:w,height:h,foregroundPixels:largest.length,preservedSourceAlpha:hasAlpha,regions:{red:'head (primary material)',green:'grip (leather)',blue:'fittings (brass)'},source:id+'-study-'+version+'.png',polygons:shapes[id]};
await writeFile(new URL(id+'-preparation-'+version+'.json',root),JSON.stringify(report,null,2));console.log(id,report.foregroundPixels,'pixels; source alpha preserved:',hasAlpha);
