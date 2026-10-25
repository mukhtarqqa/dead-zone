# Rework art provenance — 2026-10-26

Generated with built-in ImageGen. Originals retained in the local generated_images conversation folder; copies below are the editable project sources. Enemy component counts and feet pivots were verified in native Unity. The ranger output contains five rows (20 actual poses); its raised-rifle stride is deliberately reused as the firing pose. The other three enemy sheets have six rows each (24 actual poses). No paid Cainos or Lo-Bit files are included.

## City materials

Asset: `Assets/DeadZone/CityV2/CityMaterialsV2.png`. Original: `exec-cd905707-6687-45f5-ad08-eeb2954e438a.png`.

Prompt:

Production 2D pixel-art TILE MATERIAL atlas for a top-down abandoned city game, flat orthographic surfaces only. Exactly FOUR columns and FOUR rows, sixteen equally sized square swatches filling the full canvas without gaps, no borders, no perspective. Every swatch should behave as a seamless repeating terrain texture, use clear restrained low-frequency 32x32-style pixel clusters, NOT photorealism, NOT granular noise. Cohesive muted colors, readable at a small game scale. Row 1 left to right: dark blue-gray asphalt with extremely sparse tiny cracks; dark asphalt with few wider cracks; pale gray sidewalk paving rectangular slabs; muted warm residential paving blocks. Row 2: warm cream plaza stone; ochre dirt park trail; deep muted green park grass with sparse blades; olive garden soil and moss. Row 3: desaturated burgundy roof tiles; rusty industrial corrugated metal; muted teal painted metal roof; dark gray flat roof with subtle panel seams. Row 4: cool gray warehouse concrete floor; faded terracotta market cobblestone; blue-green pond water with very sparse highlights; neutral weathered stone curb texture. NO people, buildings, props, foliage silhouettes, text, symbols, shadows or road markings. This is a clean game texture atlas, each cell only its material. Opaque background.

## Walker — 24 poses

Asset: `Assets/Resources/EnemyWalkerDirections.png`. Original: `exec-e6d9675d-6099-478e-96ef-8213e26ef21e.png`.

Prompt:

Create a NEW production animated enemy sprite sheet for a 2D top-down pixel art game. References are STYLE references only: use the olive zombie identity/shading from the enemy reference, and the compact readable large-head proportions of the hero reference. Subject is ONE ordinary zombie walker: olive-green skin, red eyes, bald damaged head, ragged charcoal shirt and brown trousers, bare green hands, no weapon. Exactly FOUR equally spaced columns and SIX equally spaced rows: 24 full-body frames, every cell a single character, no overlap. Columns consistently face DOWN toward viewer; RIGHT (nose right); UP (strict back view, NO eyes or face); LEFT (nose left). Row 1 neutral idle. Row 2 walking step A with left leg forward. Row 3 walking step B with right leg forward, visibly different legs/arms from row 2. Row 4 clear melee striking/lunging pose with arms out toward facing direction. Row 5 collapsing/down on one knee as a death transition. Row 6 final fallen body lying on the ground oriented in that column's direction, visibly horizontal/collapsed rather than standing. Same body, clothes, colors and scale across every frame. Same relative feet baseline for standing frames; generous clear padding inside each regular grid cell; each sprite fully contained. Crisp pixel clusters, no painted blur, dark thin outlines, limited palette, readable silhouette at about 48-64px character height in the game. Genuine transparent alpha background, no black backdrop, NO fog/glow, NO floor shadow, NO labels/text/grid lines. Tall canvas aspect 2:3, precise 4 by 6 sprite grid.

## Ranger — 20 poses

Asset: `Assets/Resources/EnemyRangerDirections.png`. Original: `exec-5f24818f-2b58-4265-8186-f67bef7cfe4b.png`.

Prompt:

Correct this sprite sheet layout and add the missing walk step. Preserve the SAME undead teal-uniform masked rifle guard design, palette and pixel-art style. Current sheet has only FIVE rows. The final sheet MUST have exactly SIX rows of FOUR sprites, 24 sprites total. Layout: row1 IDLE; row2 WALK A (left foot forward); row3 WALK B (right foot forward, opposite arms/legs, NOT a firing pose); row4 AIM/FIRE; row5 DYING/KNEELING; row6 DEAD/LYING FLAT. Keep the four consistent columns: front facing DOWN, profile facing RIGHT, strict BACK facing UP (no face), profile facing LEFT. All standing poses fit equal square cells with consistent feet baseline. Use tall 1024x1536-like canvas, cell centers at 1/8,3/8,5/8,7/8 of width and 1/12,3/12,5/12,7/12,9/12,11/12 of height. Scale the sprites down slightly if needed so all six rows fit with generous transparent gaps. Do NOT merge walking and firing rows. No labels, no grid lines, no background haze, no floor shadows. Genuine transparent alpha everywhere outside the silhouettes. Keep the rifle attached to the hands and pointing in that column's facing direction.

## Runner — 24 poses

Asset: `Assets/Resources/EnemyRunnerDirections.png`. Original: `exec-1b597b9c-5cac-4a00-ad59-92cf4747d5ff.png`.

Prompt:

Edit the reference 24-pose enemy sprite sheet. Preserve its EXACT layout of FOUR columns and SIX rows (24 separate full-body sprites) with transparent gaps, the pixel-art technique and consistent perspective. Replace every green walker with the SAME fast lean undead runner: ragged dark burgundy/red hoodie with hood back, pale gray skin, narrow eyes, black cropped trousers, athletic shoes, long clawed hands, agile bent-forward stance. No gun. All 24 poses must use this one recognizable runner design. Columns in EVERY row: DOWN/front, RIGHT/profile, UP/back, LEFT/profile. Six rows in order: idle, running step A with left foot forward, running step B with right foot forward, forward CLAW attack, kneeling/dying, lying dead. Running poses should be distinct opposite leg positions with a forward lean, claws extended only in attack row. Keep sprites small enough to fit the 6 rows with no clipping or overlap; same size for first four rows. Crisp controlled pixel edges. No text, no grid, no background, no fog, no shadows outside silhouettes. Transparent background.

## Brute — 24 poses

Asset: `Assets/Resources/EnemyBruteDirections.png`. Original: `exec-5ffd2ab8-becf-4843-8edd-cf80176d6d72.png`.

Prompt:

Edit the reference 24-pose enemy sprite sheet. Preserve its EXACT layout of FOUR columns and SIX rows (24 separate full-body sprites) with transparent gaps, the pixel-art technique and consistent perspective. Replace every green walker with the SAME hulking undead construction brute: broad powerful silhouette, gray-green skin, bald with scars, torn orange construction vest over charcoal shirt, dark slate trousers, heavy work boots, reinforced forearms and huge bare fists. No weapon. All 24 poses must use this one recognizable brute. Columns in EVERY row: DOWN/front, RIGHT/profile, UP/back, LEFT/profile. Six rows in order: idle, heavy walking step A with left foot forward, heavy walking step B with right foot forward, overhead fist SLAM attack, kneeling/dying, lying dead. Exaggerate the attack windup silhouette but keep all poses contained. Scale each brute small enough to fit all SIX rows with no clipping or overlap; consistent size in first four rows. Crisp controlled pixel edges. No text, no grid, no background, no fog, no shadows outside silhouettes. Genuine transparent background.

## District props — 12 objects

Asset: `Assets/DeadZone/CityV2/CityProps.png`. Original: `exec-a0282182-b490-43f5-b0c7-65c5a2e87f83.png`.

Prompt:

Create a professional pixel-art ENVIRONMENT PROP ASSET SHEET for a dark abandoned-city 2D top-down action game, matching the supplied enemy art's crisp pixel clusters and muted teal/rust/olive palette. EXACTLY 4 columns and 3 rows, 12 isolated props, generous empty transparent space between every prop. Orthographic top-down game perspective with short visible south-facing front sides, NOT isometric diamond tiles. NO characters. Row 1 left to right: red striped awning market stall stocked with produce and wooden counter; teal canvas awning market stall with crates and goods; abandoned glass bus shelter with metal roof and bench; small dry round stone plaza fountain. Row 2: leafy park tree with visible trunk and detailed irregular green canopy; weathered wooden park bench; rectangular low stone planter with shrubs; slender single-head streetlamp with pole all connected to its base. Row 3: abandoned blue-gray CAR oriented horizontally facing right; same CAR oriented vertically facing up; rusty corrugated freight CONTAINER horizontal; teal corrugated freight CONTAINER vertical. Each prop is a coherent connected silhouette. Trees taller than benches, cars and containers fully visible without cropping. No background haze, no floor, no cast floor shadows, no labels, no letters, no grid lines. Genuine transparent alpha outside the props. Detailed readable game sprites, carefully separated so each can be extracted whole.

