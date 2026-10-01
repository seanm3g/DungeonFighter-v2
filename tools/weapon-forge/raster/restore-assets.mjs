import fs from 'node:fs/promises';
import {createRequire} from 'node:module';
const sharp=createRequire(import.meta.url)(process.argv[2]||'sharp');
const root=new URL('../',import.meta.url),source='C:/Users/Seanye/.codex/generated_images/01a0cc1a-be9e-7ac1-b982-ec7780ce733b/';
const files=JSON.parse(await fs.readFile(new URL('source-inventory.json',root)));
const specs=[
 ['mace','Flanged mace',19],['sword','Arming sword',22],['dagger','Broad dagger',8],['wand','Crystal wand',14],
 ['helmet-closed','Closed greathelm',0],['helmet-open','Open-faced helmet',26],['helmet-horned','Horned helm',24],
 ['chest-chestplate','Plate cuirass',17],['chest-mail','Chainmail hauberk',13],['chest-tunic','Padded arming tunic',7],['chest-coat','Adventurer coat',21]
];
const assets=[];
const inside=(x,y,poly)=>{let yes=false;for(let i=0,j=poly.length-1;i<poly.length;j=i++){const [a,b]=poly[i],[c,d]=poly[j];if((b>y)!==(d>y)&&x<(c-a)*(y-b)/(d-b)+a)yes=!yes;}return yes;};
const leather={
 'helmet-closed':[[[0,1090],[115,1078],[197,1125],[300,1170],[420,1210],[522,1234],[619,1220],[731,1183],[829,1140],[912,1078],[1024,1090],[1024,1536],[0,1536]]],
 'helmet-open':[[[185,680],[390,620],[395,910],[438,958],[499,911],[509,620],[769,675],[769,763],[742,853],[709,914],[705,1113],[663,1065],[631,1007],[365,1007],[319,1071],[266,1118],[268,909],[230,860],[206,756]]],
 'helmet-horned':[[[270,721],[480,782],[479,848],[275,765]],[[577,782],[754,724],[751,769],[577,847]],[[379,901],[472,947],[530,998],[582,942],[658,903],[680,1160],[620,1120],[510,1065],[417,1100],[362,1145]]],
 'chest-mail':[[[365,120],[415,71],[570,88],[708,140],[758,231],[719,296],[580,288],[470,230]],[[61,429],[161,467],[286,563],[241,647],[137,610],[26,501]],[[806,629],[891,603],[973,555],[995,588],[950,642],[826,696]],[[299,762],[467,810],[660,830],[812,813],[833,897],[716,945],[520,946],[277,848]],[[149,1220],[301,1302],[565,1370],[771,1370],[912,1291],[936,1379],[860,1430],[647,1463],[451,1443],[244,1385],[120,1300]]],
 'chest-tunic':[[[413,132],[440,112],[592,139],[694,176],[713,235],[684,271],[587,272],[486,231],[420,183]],[[57,438],[147,450],[249,507],[315,601],[276,670],[181,673],[89,625],[26,500]],[[799,627],[890,574],[955,484],[981,520],[928,604],[834,667]],[[328,735],[442,767],[613,800],[805,774],[811,824],[845,879],[811,916],[727,901],[576,900],[316,814]],[[154,1122],[270,1220],[455,1312],[649,1355],[788,1321],[918,1194],[951,1223],[933,1280],[857,1357],[756,1393],[573,1413],[414,1374],[237,1286],[134,1174]]],
 'chest-coat':[[[358,209],[365,110],[450,37],[533,33],[635,63],[699,151],[773,272],[662,221],[768,324],[590,432],[394,274],[488,195]],[[104,479],[211,490],[323,574],[278,713],[189,709],[71,630]],[[767,686],[904,589],[940,614],[967,729],[891,776],[813,784]],[[374,562],[550,589],[724,600],[738,667],[675,686],[488,672],[358,637]],[[578,424],[601,427],[594,581],[576,585]],[[573,683],[593,684],[615,853],[662,1017],[713,1242],[754,1439],[885,1311],[909,1315],[889,1365],[728,1475],[706,1303],[658,1126],[610,907],[570,1154],[502,1463],[474,1474],[231,1341],[222,1320],[244,1287],[302,1330],[478,1417],[514,1218],[561,938]]]
};
await fs.mkdir(new URL('assets/restored/',root),{recursive:true});
for(const [type,name,index] of specs){
 const {data,info}=await sharp(source+files[index]).ensureAlpha().raw().toBuffer({resolveWithObject:true}),{width:w,height:h}=info,n=w*h;
 const bg=new Uint8Array(n),q=new Int32Array(n);let first=0,last=0;
 const hasAlpha=data.some((v,i)=>i%4===3&&v===0);
 function add(i){if(i<0||i>=n||bg[i])return;const p=i*4,r=data[p],g=data[p+1],b=data[p+2];if(data[p+3]>0&&(Math.min(r,g,b)<175||Math.max(r,g,b)-Math.min(r,g,b)>20))return;bg[i]=1;q[last++]=i;}
 if(!hasAlpha){for(let x=0;x<w;x++){add(x);add((h-1)*w+x);}for(let y=0;y<h;y++){add(y*w);add(y*w+w-1);}while(first<last){const i=q[first++],x=i%w;add(i-w);add(i+w);if(x)add(i-1);if(x<w-1)add(i+1);}}
 const out=Buffer.from(data),mask=Buffer.alloc(data.length),weapon=index===19||index===22||index===8||index===14;
 for(let i=0;i<n;i++){
 const p=i*4,x=i%w,y=Math.floor(i/w),r=data[p],g=data[p+1],b=data[p+2];
 if(bg[i]||!data[p+3]){out[p+3]=0;continue;}
 if(!hasAlpha){let count=0;for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++)if(!bg[(y+dy)*w+x+dx])count++;out[p+3]=count===9?255:Math.round(255*count/9);}
 let region=0;
 if(weapon){
 const along=y-.6*x;
 if(type==='mace')region=y-.52*x>145&&y-.52*x<1126?1:y<450&&x>560?0:2;
 if(type==='sword')region=inside(x,y,[[328,941],[852,130],[991,18],[945,185],[443,1008],[388,966]])?0:along>845&&along<1183?1:2;
 if(type==='dagger')region=inside(x,y,[[379,770],[675,295],[954,50],[863,420],[605,900],[526,845],[440,805]])?0:along>688&&along<1098?1:2;
 if(type==='wand')region=inside(x,y,[[694,326],[742,190],[971,20],[964,248],[856,397],[780,451],[795,340],[741,371],[691,414]])?0:along>132&&along<1163?1:2;
 }else{
 const isLeather=(leather[type]||[]).some(poly=>inside(x,y,poly));
 const brown=r>g*1.15&&r>b*1.4;
 const gold=r>b*1.4&&g>b*1.17&&r-g>8;
 region=isLeather?1:0;
 if(type==='chest-chestplate')region=brown?(g>100&&r>145?2:1):0;
 else if(type.startsWith('helmet'))region=isLeather?1:gold?2:0;
 else if(isLeather&&gold&&g>105&&r>155)region=2;
 if(type==='helmet-horned'&&((x<274&&y<440)||(x<274-(y-440)*.56&&y<620)||(x>748&&y<440)||(x>748+(y-440)*.56&&y<620))){mask[p+3]=255;continue;}
 }
 mask[p+region]=255;mask[p+3]=255;
 }
 const stem='assets/restored/'+type;
 await sharp(out,{raw:{width:w,height:h,channels:4}}).png().toFile(decodeURIComponent(new URL(stem+'-base.png',root).pathname).replace(/^\/([A-Za-z]:)/,'$1'));
 await sharp(mask,{raw:{width:w,height:h,channels:4}}).png().toFile(decodeURIComponent(new URL(stem+'-regions.png',root).pathname).replace(/^\/([A-Za-z]:)/,'$1'));
 assets.push({id:'painted-'+type+(weapon?'-v1':type.startsWith('helmet')?'-v2':'-v1'),name,base:'./'+stem+'-base.png',mask:'./'+stem+'-regions.png',width:w,height:h,regions:['head','grip','fittings'],referenceLuma:[105,55,118],regionLabels:[type==='wand'?'Crystal focus':type==='chest-tunic'?'Quilted fabric':type==='chest-coat'?'Coat fabric':'Steel',weapon?'Grip & shaft':'Leather & lining','Metal fittings'],supportsPrefixes:weapon});
 console.log(name);
}
await fs.writeFile(new URL('raster/assets.json',root),JSON.stringify(assets,null,2));

