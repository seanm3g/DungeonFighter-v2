// Pass a path to the installed Playwright module as argv[2]. No server required.
const { chromium } = require(process.argv[2] || 'playwright');
const path = require('path');
const { pathToFileURL } = require('url');
(async () => {
  const browser = await chromium.launch({ headless: true, channel: process.env.SPRITE_BROWSER_CHANNEL || 'msedge' });
  try {
    const page = await browser.newPage({ viewport: { width: 1280, height: 1000 } });
    const errors=[]; page.on('pageerror',e=>errors.push(e.message));
    await page.goto(pathToFileURL(path.resolve(__dirname,'../Documentation/ArtLab/equipment-preview.html')).href);
    await page.waitForFunction(()=>window.spritesReady);
    if(await page.locator('.card').count()!==400) throw Error('Missing catalog entries');
    await page.locator('#hero').screenshot({path:path.resolve(__dirname,'../Documentation/ArtLab/equipment-alignment.png')});
    if (!(await page.locator('#hero').evaluate(c=>c.width===400&&c.height===600))) throw Error('Portrait must use the shared native pixel grid');
    const before=await page.locator('#hero').screenshot();
    await page.locator('#clear').click();
    const bare=await page.locator('#hero').screenshot();
    if(before.equals(bare)) throw Error('Unequip did not update appearance');
    await page.locator('#search').fill('Void Tome');
    if(await page.locator('.card').count()!==1) throw Error('Catalog search mismatch');
    await page.locator('.card').click();
    if(!((await page.locator('#loadout').textContent()).includes('Void Tome'))) throw Error('Equip failed');
    await page.locator('#search').fill('');
    await page.locator('#slot-head').selectOption({label:'Nasal Helm · Tier 2'});
    await page.locator('#slot-chest').selectOption({label:'Breastplate · Tier 2'});
    await page.locator('#slot-legs').selectOption({label:"Footman's Greaves · Tier 3"});
    await page.locator('#slot-feet').selectOption({label:'Plate Boots · Tier 3'});
    await page.locator('#slot-weapon').selectOption({label:'Longsword · Tier 2'});
    await page.screenshot({path:path.resolve(__dirname,'../Documentation/ArtLab/equipment-preview.png')});
    await page.evaluate(()=>{
      const review=document.createElement('div'); review.id='fit-review';
      review.style.cssText='position:absolute;left:0;top:0;z-index:1000;display:grid;grid-template-columns:repeat(3,260px);gap:16px;padding:24px;background:#101313;color:#e8eadc;font:14px system-ui';
      const sets=[
        ['Base',{}],
        ['Veil / clothing',{head:'Veil',chest:'Jerkin',legs:'trousers',feet:'Shoes',weapon:'Dagger'}],
        ['Reported loadout',{head:'Plated Hood',chest:'Duelist Coat',legs:"Knight's Greaves",feet:'Pelt Boots',weapon:'Mace'}],
        ['Open helm / plate',{head:'Nasal Helm',chest:'Breastplate',legs:"Footman's Greaves",feet:'Plate Boots',weapon:'Longsword'}],
        ['Headdress / plate',{head:'Headdress',chest:'Full Plate Armor',legs:'Full Greaves',feet:'Full Plate Boots',weapon:'Greatsword'}],
        ['Hood / magical implement',{head:'Hood',chest:'Coat',legs:'leggings',feet:'Boots',weapon:'Grimoire'}]
      ];
      for(const [label,items] of sets){
        slots.forEach(s=>equipment[s]=null);
        for(const [s,name]of Object.entries(items)){equipment[s]=entries.find(e=>slotOf(e)===s&&e.Name===name);if(!equipment[s])throw Error('Missing review fixture '+name)}
        render(); const card=document.createElement('div'),c=document.createElement('canvas');c.width=400;c.height=600;c.style.cssText='width:240px;height:360px;image-rendering:pixelated';
        c.getContext('2d').drawImage(hero,0,0);card.append(c,document.createElement('br'),document.createTextNode(label));review.append(card);
      }
      document.body.append(review);
    });
    await page.locator('#fit-review').screenshot({path:path.resolve(__dirname,'../Documentation/ArtLab/equipment-fit-review.png')});
    await page.locator('#fit-review').evaluate(e=>e.remove());
    // Every item can be equipped and painted with a valid source rectangle.
    const count=await page.evaluate(()=>{let n=0;for(const e of window.EQUIPMENT_SPRITES){equipment[slotOf(e)]=e;render();n++}return n});
    if(errors.length) throw Error(errors.join('\n'));
    console.log(`PASS: ${count} sprites rendered; equip, unequip, all five slots and search verified.`);
  } finally { await browser.close(); }
})().catch(e=>{console.error(e);process.exitCode=1});
