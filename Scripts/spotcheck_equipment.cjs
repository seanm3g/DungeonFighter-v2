// Reproducible randomized loadouts, rendered at full resolution for visual review.
const { chromium } = require(process.argv[2] || 'playwright');
const { pathToFileURL } = require('url');
const path = require('path');
const fs = require('fs');
(async () => {
  const browser = await chromium.launch({headless:true,channel:'msedge'});
  try {
    const page = await browser.newPage({viewport:{width:1280,height:1000}});
    await page.goto(pathToFileURL(path.resolve('Documentation/ArtLab/equipment-preview.html')).href);
    await page.waitForFunction(()=>window.spritesReady);
    const samples = await page.evaluate(()=>{
      let seed=9162026;
      Math.random=()=>((seed=(Math.imul(seed,1664525)+1013904223)>>>0)/4294967296);
      const review=document.createElement('div');review.id='random-review';
      review.style.cssText='position:absolute;left:0;top:0;z-index:9999;background:#101313;color:#ddd;display:grid;grid-template-columns:repeat(3,400px);gap:12px;padding:12px;font:14px system-ui';
      const results=[];
      for(let i=0;i<12;i++) {
        document.querySelector('#random').click();
        const card=document.createElement('div'),canvas=document.createElement('canvas');canvas.width=400;canvas.height=600;
        canvas.getContext('2d').drawImage(hero,0,0);
        const label=slots.map(s=>equipment[s].Name).join(' · ');
        card.append(canvas,document.createTextNode(`${i+1}. ${label}`));review.append(card);
        results.push({number:i+1,items:slots.map(s=>({slot:s,name:equipment[s].Name,index:equipment[s].CatalogIndex}))});
      }
      document.body.append(review);return results;
    });
    for(let row=0;row<4;row++) {
      await page.locator('#random-review > div').evaluateAll((cards,row)=>cards.forEach((c,i)=>c.style.display=Math.floor(i/3)===row?'block':'none'),row);
      await page.locator('#random-review').screenshot({path:`Documentation/ArtLab/equipment-random-${row+1}.png`});
    }
    fs.writeFileSync('Documentation/ArtLab/equipment-random-samples.json',JSON.stringify(samples,null,2));
    console.log('Captured 12 seeded random loadouts in four full-resolution review sheets.');
  } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
