export const ASSETS=[{
 id:'painted-mace-v1',name:'Flanged mace',version:2,width:1024,height:1536,
 base:'./assets/painted/steel-mace-base-v2.png',mask:'./assets/painted/steel-mace-regions-v2.png',
 pivot:[.5,.82],regions:['head','grip','fittings'],referenceLuma:[92,50,118],
 description:'Painted steel, leather and brass. Three independently tintable surfaces.'
},...['sword','dagger','wand'].map((type,i)=>({
 id:'painted-'+type+'-v1',name:['Arming sword','Broad dagger','Crystal wand'][i],version:type==='dagger'?2:1,width:1024,height:1536,
 base:'./assets/painted/steel-'+type+'-base-'+(type==='dagger'?'v2':'v1')+'.png',mask:'./assets/painted/steel-'+type+'-regions-'+(type==='dagger'?'v2':'v1')+'.png',
 pivot:[[.26,.76],[.34,.72],[.42,.65]][i],regions:['head','grip','fittings'],referenceLuma:[92,50,118],
 regionLabels:[type==='wand'?'Crystal focus':'Steel blade',type==='wand'?'Wood & leather shaft':'Leather grip','Metal fittings'],
 description:'Three independently tintable painted surfaces.'
 })),...['closed','open','horned'].map((type,i)=>({
 id:'painted-helmet-'+type+'-v2',name:['Closed greathelm','Open-faced helmet','Horned helm'][i],version:2,width:1024,height:1536,
 base:'./assets/painted/helmets/'+type+'-base-v2.png',mask:'./assets/painted/helmets/'+type+'-regions-v2.png',
 pivot:[.5,.8],regions:['head','grip','fittings'],referenceLuma:[110,45,118],
 regionLabels:['Steel shell','Leather lining','Brass trim'],supportsPrefixes:false,
 description:'Reimagined painted helmet. Steel, lining and trim can be tinted independently; ivory horns retain their original painting.'
 })),...['chestplate','mail','tunic','coat'].map((type,i)=>({
 id:'painted-chest-'+type+'-v1',name:['Plate cuirass','Chainmail hauberk','Padded arming tunic','Adventurer coat'][i],version:1,width:1024,height:1536,
 base:'./assets/painted/chest/'+type+'-base-v1.png',mask:'./assets/painted/chest/'+type+'-regions-v1.png',
 pivot:[.5,.55],regions:['head','grip','fittings'],referenceLuma:[i<2?110:90,55,118],
 regionLabels:[['Steel plates','Steel mail','Quilted fabric','Coat fabric'][i],'Leather & lining','Brass fittings'],supportsPrefixes:false,
 description:'Original torso silhouette reimagined with dimensional painted materials and functional construction.'
}))];
export const PRESETS=[
 {id:'original',name:'Original painting',colors:['#747571','#4d382e','#ac7c3e'],strength:0},
 {id:'cold-steel',name:'Cold steel',colors:['#7f929e','#392f31','#a9a8a1'],strength:85},
 {id:'oxblood',name:'Oxblood & iron',colors:['#595d64','#742a32','#928274'],strength:90},
 {id:'bronze',name:'Ancient bronze',colors:['#a17b45','#43382c','#b49458'],strength:90},
 {id:'violet',name:'Amethyst',colors:['#79688f','#332e38','#a29178'],strength:85},
 {id:'bone',name:'Ivory & brass',colors:['#c4b698','#51402e','#a17a43'],strength:90},
 {id:'jade',name:'Jade',colors:['#5e8873','#373b32','#b69353'],strength:90},
 {id:'ember',name:'Ember',colors:['#985349','#45262a','#ba8849'],strength:90}
];
export function presetRecipe(p=PRESETS[0],asset=ASSETS[0]){return {version:1,asset:asset.id,prefix:'none',layers:Object.fromEntries(asset.regions.map((name,i)=>[name,{color:p.colors[i],strength:p.strength}])),background:'#918572',transparent:true};}
