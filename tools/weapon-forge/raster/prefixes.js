export const PREFIXES=[
 {id:'none',name:'No prefix'},
 {id:'flaming',name:'Flaming',motif:'Flame relief'},
 {id:'poisonous',name:'Poisonous',motif:'Coiled serpent relief'},
 {id:'charged',name:'Charged',motif:'Lightning relief'},
 {id:'enchanted',name:'Enchanted',motif:'Arcane rune relief'},
 {id:'blessed',name:'Blessed',motif:'Sun relief'}
];
// Each surface has an independently authored center, size and local axes.
// Coordinates are in the 1024 x 1536 source painting. Wand uses its crystal face.
export const PREFIX_SURFACES={
 'painted-mace-v1':{center:[745,265],size:[106,164],angle:27,shear:-.12,region:0},
 'painted-sword-v1':{center:[465,855],size:[90,195],angle:33,shear:0,region:0},
 'painted-dagger-v1':{center:[558,696],size:[94,190],angle:30,shear:0,region:0},
 'painted-wand-v1':{center:[845,246],size:[128,185],angle:32,shear:-.10,region:0}
};
// A bolt has an intrinsic diagonal; compensate so its long axis follows the weapon.
// Circular seals use the surface width, rather than being stretched into a tall crest.
export function prefixSurface(assetId,prefix){
 const surface=PREFIX_SURFACES[assetId];
 return {...surface,size:prefix==='blessed'?[surface.size[0],surface.size[0]]:surface.size,
 angle:surface.angle+(prefix==='charged'?-16:0)};
}
export const prefixMapURL=(asset,prefix)=>`./assets/painted/prefixes/${asset.id}-${prefix}-relief-v1.png`;

// Sample one material palette for the stamp, then paint its own opaque relief.
// Underlying brush marks must not show through the stamped face. Only silhouette
// edges use fractional coverage for antialiasing; holes remain transparent.
export function shadePrefix(base,relief,{layerOnly=false}={}){
 if(base.length!==relief.length||base.length%4)throw Error('Prefix map dimensions differ.');
 const out=layerOnly?new Uint8ClampedArray(base.length):new Uint8ClampedArray(base);
 const palette=[0,0,0];let weight=0;
 for(let i=0;i<base.length;i+=4){const coverage=base[i+3]/255*relief[i+3]/255;if(!coverage)continue;weight+=coverage;for(let c=0;c<3;c++)palette[c]+=base[i+c]*coverage;}
 if(!weight)return out;
 for(let c=0;c<3;c++)palette[c]/=weight;
 for(let i=0;i<base.length;i+=4){if(!base[i+3]||!relief[i+3])continue;
  const value=relief[i],amount=relief[i+3]/255;
  for(let c=0;c<3;c++){
   const host=base[i+c],pigment=palette[c];
   const lit=value<128?pigment*value/128:pigment+(255-pigment)*(value-128)/127;
   out[i+c]=Math.round(layerOnly?lit:host+(lit-host)*amount);
  }
  out[i+3]=layerOnly?Math.round(base[i+3]*amount):base[i+3];
 }
 return out;
}
