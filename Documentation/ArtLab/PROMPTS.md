# Art asset provenance

Tool: built-in `image_gen.imagegen`. The first cathedral image was rejected as too painterly. The user's approved direction is the edited chunky pixel-art environment below. Images referenced by the game are copied into the repository and embedded in the application.

## Cathedral — final edit prompt

Saved asset: `Code/UI/Avalonia/Assets/ArtLab/cathedral.png`.

> Transform this environment into AUTHENTIC CHUNKY LOW RESOLUTION PIXEL ART. User correction: it must feel WAY more like pixel art. Preserve the basic gothic cathedral scene, central acid yellow doorway, red streams, stone pillars and wide framing. Completely redraw as if authored on a 256x170 pixel canvas and enlarged 6x with nearest-neighbor scaling. Every source pixel must be a conspicuous uniform sharp 6x6 square. Restrict to a 24-color indexed palette. Reduce all the tiny detail: skulls are simple 5-pixel motifs, candles simple 1-pixel stems, steps bold horizontal pixel clusters. Broad blocky shapes, hard stair-stepped silhouettes, visible checkerboard dithering only in shadow transitions, bold dramatic contrast, fewer layers of stone texture. Classic 16-bit point-and-click background, cinematic composition, neon medieval punk with chartreuse, blood red, blue charcoal. NO painterly texture, NO antialiasing, NO smooth gradients, NO subpixel details, NO realistic fine shading, NO text. This is a deliberate sprite-art redraw, not a lightly pixelated painting.

## Equipment atlas — final generation prompt

Saved asset: `Code/UI/Avalonia/Assets/ArtLab/hero-loadouts.png`.

> Game asset sprite sheet, exactly THREE equally spaced full-body front-facing warrior sprites, side by side in three equal-width columns on a uniform solid charcoal #101313 background. Wide 1536x1024 image. Same female fighter all three: short white hair, pale face, red tattered cape, leather armor, boots, same stance. Left fighter: bare head, worn leather, small steel sword. Middle fighter: horned iron helmet, steel breastplate, large blood red axe. Right fighter: enclosed black helmet with acid yellow eyes, black armor, glowing acid yellow long sword. Each entire body and weapon fits inside its column from head to boots, no overlap, generous clear border. Genuine low-resolution CHUNKY 16-bit pixel sprites as if total sheet is 256x170 enlarged 6x nearest-neighbor. Large visibly square pixels, flat color clusters, stair-stepped edges, restricted 20-color palette, simple bold silhouettes. Gothic retro medieval punk. No fine detail, no smooth gradients, no painterly rendering, no text, no frame, no UI. Flat solid charcoal background everywhere between the three figures.

The prompts describe the requested treatment; generated pixels are not guaranteed to conform to an exact indexed palette or source resolution. The native UI uses nearest-neighbor image interpolation.
