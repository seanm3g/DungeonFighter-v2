import {readFile,writeFile} from 'node:fs/promises';
const catalog=JSON.parse(await readFile(new URL('../../GameData/StatBonuses.json',import.meta.url),'utf8'));
const aliases={ferosity:'ferocity',rejuvination:'rejuvenation'};
const poetic=['moon','killing','chain','kin','kin','moon','heart','power','precision','sun','moon','eye','chain','hand','agility','eye','wave','eye','sun','hourglass','eye','chain','stitch','kin','sun','kin','eye','wave','stitch','bone','sun'];
function animal(name){
 if(/raven|corvid|jackdaw|jay|magpie|rook|hawk|eagle|crane|heron|ibis|bird|kestrel|kingfisher|osprey|owl|falcon|phoenix|shrike|sicklebill|swallow|swift/.test(name))return 'raven';
 if(/cobra|serpent|python|viper|fer-de-lance|basilisk|hydra/.test(name))return 'cobra';
 if(/ram|auroch|bison|buffalo|deer|elk|moose|^ox$|pronghorn|kirin|elephant|mammoth|rhinoceros/.test(name))return 'ram';
 if(/abalone|armadillo|crab|lobster|nautilus|pangolin|tortoise|trilobite/.test(name))return 'shell';
 if(/^ant$|beetle|fly|mantis|millipede|scorpion|spider/.test(name))return 'insect';
 if(/squid|octopus|kraken|cuttlefish/.test(name))return 'tentacle';
 if(/fish|whale|shrimp|otter|piranha/.test(name))return 'wave';
 if(/dragon|crocodile|gecko|salamander|toad|velociraptor/.test(name))return 'scale';
 return 'wolf';
}
let rare=0;
const rows=catalog.map(row=>{
 const raw=row.Name.replace(/^of (the )?/i,'').toLowerCase().replace(/[^a-z0-9]+/g,'-');
 const id=aliases[raw]||raw;
 return {id,name:row.Name,rarity:row.Rarity,description:row.Description,motif:row.Rarity==='Rare'?poetic[rare++]:row.Rarity==='Uncommon'?animal(id):id};
});
await writeFile(new URL('./suffix-catalog.js',import.meta.url),'// Generated from GameData/StatBonuses.json by node tools/weapon-forge/sync-suffixes.mjs\nexport const SUFFIX_CATALOG = '+JSON.stringify(rows,null,2)+';\n');
