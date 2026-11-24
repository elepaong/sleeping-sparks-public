# 맵 배경 시안 프롬프트 — 전 스테이지 (2026-11-16)

스테이지(지역) 8곳 × 맵 3~4개 = **29개 맵**의 배경 시안을 한꺼번에 뽑기 위한 프롬프트 묶음입니다.
이 문서는 **시안용**입니다. 채택한 그림의 기록은 [`background-prompts.md`](background-prompts.md) "시안 기록"과 [`art-direction.md`](art-direction.md)에, 출처는 [`licenses.md`](licenses.md)에 적습니다.

## 이번에 정한 것 (사람, 2026-11-16)
1. **스테이지마다 맵 3~4개.** 한 지역을 큰 한 장으로 그리지 않고, 이어진 맵 여러 장으로 나눈다(방과 방을 오가는 구성). `plan.md` 4장 "지역 하나의 구성 기준"의 '화면 6~10개 분량'은 이제 **맵 3~4개를 합친 분량**으로 읽는다.
2. **도트의 짜임새는 추구미 게임(밝은 숲의 꼬마 마녀 게임)을 따른다. 색과 분위기는 따르지 않는다** — 스테이지마다 다르게 간다.
3. **디자인은 기획서(`plan.md`)가 먼저.** 무대·시대·소품은 기획서에서 가져온다.
4. **캐릭터 크기와 맵 크기는 고정하지 않는다.** 여러 크기로 시안을 뽑아 보고 괜찮은 것을 채택한다. 지금의 48×72·1672×941은 '현재 값'일 뿐이고, 채택 뒤에 `CLAUDE.md` 규격표를 고친다.

### 세션이 정한 가정 (★ — 고칠 곳)
- ★ **맵 목록 29개**는 기획서의 챕터 무대를 세션이 3~4개로 나눈 가안이다. 이름·수·이어지는 순서를 바꿔도 된다.
- **시점은 정면 3/4 탑다운(`VIEW T`)으로 확정 (2026-11-17, 사람).** 크기는 L(가구가 루카 48×72에 맞음), 맵은 화면보다 크게 그리고 카메라가 따라간다. 이유: 빛 퍼즐의 각도가 화면에서 그대로 보이고, 추구미와 같고, PixelLab이 이 시점만 낸다. `VIEW Q`(쿼터뷰)는 기록으로만 남긴다. 지금 서고·연구실 쿼터뷰 그림은 다시 그린다.
- ★ "촘촘한 디테일"과 `art-direction.md`의 "면 위주, 자잘한 질감 금지"는 이렇게 함께 지킨다: **물건을 많이 놓되, 물건 하나하나는 평평한 색 면 2~4단계.**
- 프롬프트에는 남의 게임 이름을 넣지 않는다. 특징만 글로 적는다.
- 도구는 ChatGPT(이미지) 기준. **PixelLab은 상업 이용 조건을 아직 확인하지 않았으므로 결과물은 레퍼런스로만 쓴다**(`CLAUDE.md` 이미지 생성 규칙). 쓰려면 먼저 약관을 확인해 `licenses.md`에 적는다. PixelLab처럼 짧은 영어 프롬프트만 받는 도구에는 "공통 블록"의 [STYLE] 줄과 맵 블록의 첫 문장만 넣는다.

## 쓰는 법
ChatGPT 대화 하나 = 스테이지 하나.
1. 대화를 열고 **공통 블록**을 붙인다. 이어서 **스테이지 분위기 블록**(그 스테이지 것 하나)을 붙인다.
2. **크기 블록** 하나와 **시점 블록** 하나를 골라 붙인다.
3. **맵 블록**을 하나씩 붙여 그림을 받는다. 같은 대화 안에서 맵 3~4개를 이어 뽑으면 그림체가 맞는다.
4. 마음에 드는 맵이 나오면 같은 대화에서 크기 블록·시점 블록만 바꿔 "같은 맵을 이 조건으로 다시"라고 한다.
5. 받은 그림은 `design/refs/maps_2026-11/<맵 ID>_<크기><시점>_<번호>.png`로 모은다(예: `P-1_MT_01.png`). 고르면 세션이 기록·규격 점검을 한다.

**먼저 뽑아 볼 것(추천):** `P-1` 필사실 하나를 크기 3종 × 시점 2종 = 6장. 여기서 크기와 시점을 정한 뒤 나머지 28개를 그 조건으로 뽑으면 다시 뽑는 일이 줄어든다.

---

## 공통 블록 (대화마다 맨 처음 한 번)

```
You are making background concept art for a 2D pixel-art puzzle adventure game. I will send one map at a time. Follow these rules for every image in this conversation.

[STYLE]
- Pixel art, top-down 3/4 view game map, storybook / cozy fairytale feel, hand-placed look.
- Chunky readable pixels. Rounded, simple, friendly shapes.
- Outline rule (MOST IMPORTANT): use color-matched natural outlines (selective outlining). Every object is outlined with a darker shade of its own color: trees dark green, stone dark grey-brown, wood dark warm brown, cloth a darker cloth color. NO harsh black outlines. NO single uniform dark-brown outline around everything.
- Flat color planes, 2 to 4 shades per object. No smooth gradients, no noisy texture, no dithering, no airbrush glow.
- High density of detail through MANY small props and set dressing (books, jars, tools, plants, crates, cloth, cracks, moss), not through texture noise.
- Soft limited palette that reads as one picture. Shadows are colored (cool violet-grey or deep green), never pure black.

[GAME READABILITY]
- This is a walkable game map. Keep clear open floor paths at least 3 character-widths wide connecting every doorway and every point of interest.
- The player character wears a dark brown hooded cloak. Walkable floor must be clearly lighter than dark brown, or clearly different in hue, so the character never disappears.
- Parallel projection: no perspective shrinking. The same object has the same size anywhere on the map.
- Exits and doorways must be obvious at a glance. Leave a little empty floor in front of each one.
- Things the player interacts with should stand slightly apart from clutter, with empty floor around them.

[WORLD]
- Late-medieval mountain monastery and the countryside around it. A woman natural philosopher secretly studied heat, light, magnetism, water, sound, wind and gravity here 40 years ago.
- Everything is real craft and early science: brass instruments, lenses, mirrors, glassware, gears, pulleys, pipes, herbs, hand tools. NO magic: no magic circles, no glowing runes, no floating crystals, no fantasy creatures.

[NEVER DRAW]
- No people, no animals, no creatures, no text, no letters, no numbers, no UI, no frame, no border, no watermark.
- No lit flames and no glow halos (candles and lanterns are unlit; the game engine adds light later). No flowing water streams unless the map block asks for water.
- No paper texture overlay, no vignette, no film grain.

Reply with the image only. Wait for my map description.
```

## 스테이지 분위기 블록 (스테이지마다 하나 — 색과 분위기는 스테이지마다 다르다)

```
[MOOD — Prologue: the monastery above ground]
Bright calm daytime. Warm grey sandstone, cream parchment, faded gold brass, small touches of faded teal cloth. Crisp window-shaped sunlight on the floor. The warmest, safest place in the game.
```
```
[MOOD — Chapter 1: underground archive and laboratory]
Cool, quiet, hidden. Violet-grey stone, deep teal carpet, warm brass and amber wood as accents. Dim but readable: the floor is mid-tone violet-grey, never near black. Secret-study atmosphere, dusty but cared for.
```
```
[MOOD — Chapter 2: herb garden and greenhouse]
Late-spring morning. Sage and olive greens, terracotta pots, pale glass with green-blue tint, warm ochre paths. Humid, lush, a little overgrown. Heat is the theme: copper stoves, clay ovens, steam pipes (no visible steam).
```
```
[MOOD — Chapter 3: forge village and abandoned mine]
Smoky autumn afternoon. Slate grey, rust orange, soot brown, cold iron blue, ember red only as tiny accents. Outside is hazy and warm; inside the mine is cold blue-grey with pale lichen. Magnetism is the theme: iron everywhere.
```
```
[MOOD — Chapter 4: lake and watermill]
Clear early summer day. Soft blue-green water, pale reeds, weathered silver wood, wet dark stone, white foam. Fresh and open. Water and pressure are the theme: wheels, sluices, pipes, pumps, floats.
```
```
[MOOD — Chapter 5: bell tower and windy hill]
High-altitude late afternoon with long light. Wind-bleached grass in yellow-green, pale stone, bronze bells with green patina, big pale sky tones. Everything leans with the wind. Sound and wind are the theme.
```
```
[MOOD — Chapter 6: ruined observatory]
Dusk turning to starry night. Deep indigo and slate blue, pale moonlit stone, warm brass instruments as the only warm color. Still, vast, a little lonely. Gravity is the theme: pendulums, weights, scales, pulleys.
```
```
[MOOD — Final chapter: the village square]
Golden late afternoon on a festival-like day. Timber-and-plaster houses in cream and brown, red and ochre awnings, grey cobblestone, bunting. Busy with objects but with a wide open center.
```

## 크기 블록 (하나 고름 — 캐릭터가 화면에서 얼마나 크게 보일지)
캐릭터는 그리지 않고, 문·가구·계단의 크기로 맞춥니다. 크기 비교용으로만 볼 때는 맨 아래 "크기 확인용 한 줄"을 덧붙입니다.

```
[SCALE L — close and cozy]
The player character would be about 1/6 of the image height. A door is about 1/4 of the image height. The image shows a small area: roughly one room of 10 by 6 character-steps. Big chunky props, few but large. Pixel blocks are large.
```
```
[SCALE M — balanced]
The player character would be about 1/9 of the image height. A door is about 1/6 of the image height. The image shows a medium area: roughly 16 by 9 character-steps. This is the default.
```
```
[SCALE S — wide and grand]
The player character would be about 1/14 of the image height. A door is about 1/9 of the image height. The image shows a large hall or wide outdoor area. Many small props, finer pixels. The character should feel small in a grand place.
```

맵 모양 (그림 비율 — 맵 블록 끝에 한 줄 붙임)
```
Aspect: wide 16:9 landscape.
```
```
Aspect: tall 2:3 portrait (a vertical map: a tower, a mine shaft, a stairway, a narrow street).
```
```
Aspect: square 1:1 (a compact single room).
```

## 시점 블록 (확정: VIEW T)

```
[VIEW T — top-down 3/4]
Camera looks down from the front at a steep angle. Back walls face the viewer and run parallel to the top edge of the image; you see wall fronts and the tops of furniture. Floors are a straight grid, not diamonds. The bottom edge of the room is open or shows only a low wall top.
```
```
[VIEW Q — isometric quarter view]
2:1 isometric. The inner corner of the room is at the top center; only the upper-left and upper-right walls are visible. The floor is a diamond grid that runs to the bottom and side edges of the image.
```

## 크기 확인용 한 줄 (비교 시안에만, 정식 배경에는 넣지 않음)
```
For scale checking only: add one plain flat grey standing mannequin silhouette (no face, no clothes) on open floor near the main entrance, at exactly the player character height described above.
```

---

## 맵 블록 (29개)
각 블록의 첫 문장이 장면, [MUST HAVE]가 게임에 꼭 있어야 하는 것(퍼즐·출입구), [DRESSING]이 채우는 소품입니다.
ID는 `<스테이지>-<번호>`입니다. 이미 게임에 있는 맵은 괄호에 `data/zones/` ID를 적었습니다.

### 프롤로그 — 수도원 (정전기 · 찌릿)

**P-1 필사실** (`scriptorium`) — 시작 지점. 자세한 설계는 `background-prompts.md` "필사실 맵 설계"
```
MAP P-1: A monastery scriptorium, a quiet hall where monks copy books by daylight.
[MUST HAVE] One doorway to a cloister (closed wooden door). Tall gothic arched windows on the back wall with sunlight patches on the floor. One special writing desk standing apart with a thick old leather-bound book lying open and a basket of raw wool beside it. A large square floor slab with one iron ring, slightly different in color from the floor around it. Two small square iron floor grates far apart from each other (dark and empty inside). A plain stone wall basin (lavabo) with a small brass funnel on a stand next to it, dry.
[DRESSING] Rows of slanted copying desks with parchment, ink pots, quills, stools. Book cupboards, parchment drying racks, a lectern, stacked books, scroll tubs, sealing wax, unlit candles, a chest, 2 or 3 thick square stone pillars.
```

**P-2 회랑과 안뜰**
```
MAP P-2: A monastery cloister: a covered arcade walkway around a small square courtyard garden.
[MUST HAVE] Four doorways on different sides: to the scriptorium, to a workshop, to a chapel (closed, heavy), and a small gate to the outside herb garden (blocked by dry vines). A stone well in the center of the garden with a bucket and rope. A sundial on a pedestal. A row of round arches with columns between walkway and garden.
[DRESSING] Trimmed grass, low box hedges, a few fruit trees, flower beds, stone benches, a wheelbarrow, clay jars, a broom, laundry line with habits, fallen leaves, worn flagstones with moss in the joints.
```

**P-3 양피지 공방**
```
MAP P-3: A parchment maker's workshop and storeroom in a monastery, cluttered and practical.
[MUST HAVE] One doorway to the cloister. Large wooden stretching frames with pale skins. A long table with a big lump of polished amber, a silk cloth and a glass rod laid out neatly on it. A hanging brass ball on a silk thread from a wall bracket. A tall locked cupboard. A trapdoor in a corner under some sacks.
[DRESSING] Bales of raw wool, sheepskins, lime tubs, scraping knives, pumice stones, chalk, shelves of rolled parchment, feather bundles, hanging herbs, barrels, a ladder, a small window with dusty light.
```

### 1장 — 지하 서고와 연구실 (빛 · 루미)

**1-1 지하 서고** (`archive`)
```
MAP 1-1: A hidden underground archive beneath a monastery, reached by a stone stair from above.
[MUST HAVE] A stone staircase coming down from above. A large sealed stone door on the far wall with three concentric brass rings set into it (plain metal, no glow). Two round stone wall sockets beside the door, each a dull glass disc in a brass rim. Several waist-high brass lantern stands on the floor. Several small square mirrors on swivel stands placed around the room on a clear floor grid. A big teal carpet in the center, left empty.
[DRESSING] Tall bookshelves, a reading desk, rolled maps, a globe, ladders on rails, crates of books, unlit candle stands, cobwebs in corners, a cracked floor tile or two.
```

**1-2 거울 회랑**
```
MAP 1-2: A long underground gallery built to test light: polished mirrors and dark slots in the walls.
[MUST HAVE] An entrance from the archive on one end and a heavy door to the laboratory on the other. A straight, clear center lane. Tall standing mirrors in brass frames on both sides at angles. Three stone pedestals with empty round holders. A wall of small square shutters, some open, some closed. A window shaft in the ceiling line letting one pale beam-shaped patch onto the floor.
[DRESSING] Cloth-covered mirrors, crates of glass discs packed in straw, polishing cloths, a rack of colored glass panes, chalk marks and straight lines scratched on the floor, measuring rods, a stool, dust sheets.
```

**1-3 이졸데의 연구실** (`lab`, 허브)
```
MAP 1-3: A secret old laboratory under a monastery, the hub of the whole game: a wide underground hall with a big optical machine in the middle.
[MUST HAVE] In the center, a large brass optical machine with two big round lenses and gears on a round floor inlay shaped like a clock face. A stair and door back to the archive. A large stone arch exit choked with dry dead vines. Four more closed exits around the hall, each different: an iron sluice-like gate, a door rimmed with frost and ice, a lift cage with a crank, and a plain village door. A long workbench with tools. A big chest. A tall shelf of glass discs. Brass lantern stands and swivel mirrors on a clear floor grid. Several small round brass lens holders set in the floor in a row near the vine arch.
[DRESSING] Stone pillars, a blackboard with diagrams (no readable text), an armillary sphere, a telescope, glass flasks, labeled jars, blueprints pinned up, a covered invention under cloth, stools, rugs, stacked crates.
```

**1-4 렌즈 연마실 (암실)**
```
MAP 1-4: A small lens-grinding room and dark chamber next to the laboratory.
[MUST HAVE] One door to the laboratory. A treadle grinding wheel with a half-finished glass disc. A workbench with brass rims, calipers and lenses of different sizes laid out in a row from small to large. A camera obscura corner: a black curtained booth with a tiny round hole in a shutter and a white screen board inside. A prism on a small stand on a table by a narrow window slit.
[DRESSING] Trays of sand and polishing powder, water buckets, shelves of glass blanks, cloth rolls, a magnifier on an arm, a wall rack of tools, wood shavings, a stove (cold), a hanging apron, small drawers.
```

### 2장 — 약초원과 온실 (열 · 화로)

**2-1 약초원**
```
MAP 2-1: A walled monastery herb garden on a sunny morning, a little overgrown.
[MUST HAVE] A vine-covered stone arch leading back down to the laboratory. A gate to the village path. The front of a glass-and-iron greenhouse with its door on one side. Neat raised herb beds in a grid with gravel paths between. A stone sundial. A hand water pump with a trough. A beehive shelter (no bees).
[DRESSING] Lavender, sage, rosemary, tall foxglove, climbing beans, cold frames with glass lids, clay pots, watering cans, a wheelbarrow, bundles of drying herbs under a lean-to, a scarecrow of sticks and sackcloth, stepping stones, a bench.
```

**2-2 유리 온실**
```
MAP 2-2: The inside of an old glass-and-iron greenhouse, warm and humid, full of rare plants.
[MUST HAVE] A door to the herb garden and a small door to a back room. A copper stove with a long horizontal heating pipe running along the planting benches. Three large glass bell jars on a table, one fogged. A frozen-over stone water basin in one cool corner with a cracked ice sheet. A metal rod thermometer-like instrument on a stand (plain, no numbers). Roof vent windows with long opening rods.
[DRESSING] Benches of potted plants, hanging baskets, citrus trees in tubs, seed trays, trellises, coiled hose, soil sacks, a potting table, glass panes with a few cracked ones, condensation drips, fallen petals.
```

**2-3 증류실과 건조 창고**
```
MAP 2-3: A still room and herb drying loft where medicines were made.
[MUST HAVE] A door to the greenhouse. A brick hearth with a copper alembic still, a coiled cooling pipe going into a water barrel, and a collecting flask. A balance scale with two pans on a table. A metal ring and a metal ball hanging on a chain beside each other on a stand. A wall of small labeled drawers (no readable text). A cellar hatch with an iron handle.
[DRESSING] Bunches of herbs hanging from beams, mortar and pestle, rows of bottles, clay crucibles, bellows, firewood stack, sieves, linen bags, recipe sheets pinned up, a stool, a cat-sized basket (empty).
```

**2-4 얼음 저장고**
```
MAP 2-4: A stone ice cellar dug into the hillside behind the garden, cold and blue.
[MUST HAVE] Stairs up to the still room. Big sawn ice blocks stacked in straw. A thick wall of solid ice completely filling a rough tunnel mouth at the far end, with the shape of a wooden door faintly visible inside it. A small iron brazier stand (empty, cold) on wheels. Drain channels cut in the floor leading to a grate.
[DRESSING] Straw heaps, sawdust, ice saws and tongs on hooks, hanging cured meats wrapped in cloth, barrels, frost on walls, icicles, wooden sleds, a lantern on a hook (unlit).
```

### 3장 — 대장간 마을과 폐광 (자력 · 마그)

**3-1 대장간 마을 거리**
```
MAP 3-1: The main street of a small hillside blacksmith village on a hazy autumn afternoon.
[MUST HAVE] A tunnel mouth dripping with meltwater leading back toward the monastery. The open front of a forge workshop with a big chimney. A village notice board (blank). A stone bridge over a narrow stream. A road going uphill toward a mine. A cart loaded with iron scrap that blocks an alley. A large horseshoe sign hanging on a bracket.
[DRESSING] Timber houses with slate roofs, woodpiles, anvils outside doors, wagon wheels, barrels, horseshoes nailed on doors, a water trough, washing lines, pumpkins, fallen leaves, a weathervane, crates of nails.
```

**3-2 베라네 대장간**
```
MAP 3-2: The inside of a busy blacksmith's forge, hot colors against soot.
[MUST HAVE] A wide door to the street. A brick forge hearth with large bellows (coals dark, no flames). A big anvil on a stump in the center with clear floor around it. A quenching trough. A workbench with a lodestone lump on a cord and a tray of iron filings. A compass in a wooden box on a shelf at eye level. A heavy iron-bound door at the back.
[DRESSING] Racks of hammers and tongs, horseshoes on pegs, sword blanks, chains, a grindstone, buckets, leather aprons on hooks, coal bin, half-made gates and hinges, a ladder to a loft, sparks-scorched floor.
```

**3-3 폐광 입구**
```
MAP 3-3: The entrance yard of an abandoned iron mine cut into a rocky hillside.
[MUST HAVE] A path down to the village. A timber-framed mine mouth with rail tracks coming out. A rail junction with a hand lever switch and two branches. Three ore carts, one tipped over. A large iron sluice-like gate closing a side tunnel, with a heavy bolt. A crane arm with a big iron hook and chain.
[DRESSING] Ore heaps, rusty tools, broken barrels, a collapsed shed, warning posts with rope, lanterns on poles (unlit), dry grass and thistles, puddles, timber stacks, a rusted bell.
```

**3-4 자철석 광맥**
```
MAP 3-4: Deep inside the mine: a wide cavern with a dark seam of magnetic lodestone running through the rock.
[MUST HAVE] A rail track entering from one side. A long black-grey lodestone vein across the back wall with iron nails, chains and small tools visibly stuck to it. Three iron plates set in the floor in a line. A wooden lift platform with pulleys. A sealed round stone door with an iron ring and two iron bars across it. A hanging iron cage lamp frame.
[DRESSING] Wooden props and beams, old pickaxes, a spilled crate of iron ore, stalagmites, shallow water pools, pale lichen, coiled rope, a broken cart, scattered compasses with cracked glass.
```

### 4장 — 호수와 물레방아 (물·압력 · 물말래)

**4-1 호숫가 선착장**
```
MAP 4-1: The shore of a calm mountain lake with a wooden pier, on a clear early-summer day.
[MUST HAVE] An iron water gate in a stone channel where a tunnel comes out from the monastery side. A long wooden pier with a small rowboat tied up. A path along the shore to a watermill seen at the edge. A boathouse with closed doors. A row of wooden posts in the shallows at different heights. A mountain path going up, blocked by a broken footbridge.
[DRESSING] Reeds, water lilies, fishing nets on drying racks, crab pots, barrels, coiled rope, oars, pebbles, driftwood, a signpost (blank), wildflowers, stepping stones in the shallows.
```

**4-2 물레방아 방앗간**
```
MAP 4-2: The inside of a stone watermill: big wooden gears turning a millstone, water channel running through.
[MUST HAVE] A door to the lakeside. A large vertical gear and a horizontal gear meshing in the center. A millstone with a grain hopper. An indoor water channel with a wooden gate that can be raised and lowered. A float on a rod connected to a lever. Sacks on a platform scale. A ladder to an upper floor.
[DRESSING] Flour sacks, grain bins, sieves, flour dust on the floor, ropes and pulleys, a small window showing the wheel outside, buckets, a broom, wooden shovels, a cat door, spare gear teeth on a shelf.
```

**4-3 수문과 수로**
```
MAP 4-3: A set of stone canals and lock gates below the lake that control the water level.
[MUST HAVE] Three stepped stone basins at clearly different water levels. Wooden lock gates between them with crank wheels on posts. A large brass hand pump with a long lever and a pipe. A U-shaped glass tube instrument on a stand beside the pump. A floating wooden platform in the lowest basin. A stone footbridge across.
[DRESSING] Mossy walls, iron rings, ladders into the water, lily pads, pipes with valves along the wall, a tool shed, spare planks, wet footprints of dark stone, reeds, a rowboat on a rack.
```

**4-4 호수 섬의 펌프실**
```
MAP 4-4: A tiny rocky island in the lake with an old pump house half-sunk at the water's edge.
[MUST HAVE] A small landing where a boat can tie up. A round stone pump house with an open door. Inside or beside it: a big sealed bronze hatch in the floor with a pressure wheel, and thick pipes going down into the water. A row of glass float balls in a rack. A diving bell hanging from a wooden crane over the water.
[DRESSING] Wind-bent pine, gulls' nests (empty), barnacles and waterline stains, nets, a rusted anchor, crates, a broken window, moss, a lantern post (unlit), ropes, wet rocks.
```

### 5장 — 종탑과 바람의 언덕 (소리·바람 · 울림, 휘이)

**5-1 산길과 승강기**
```
MAP 5-1: A steep mountain path up a cliff with a wooden lift driven by a strange jar-battery device.
[MUST HAVE] The repaired footbridge from the lake at the bottom. A wooden lift cage on ropes with a winch at the base. Beside the winch, a rack of clay jars connected with copper strips (an early battery) on a table. A cliff face with echoing cave mouths of three sizes. A prayer flag line strung across the gap. Stone steps continuing up at the top.
[DRESSING] Wind-bent grass, loose rocks, a cairn, rope railings, a shepherd's shelter, crates and sacks waiting for the lift, bird nests (empty), small mountain flowers, a broken cartwheel.
```

**5-2 바람의 언덕**
```
MAP 5-2: A wide windswept grassy hilltop with an old windmill and wind instruments.
[MUST HAVE] A windmill with four cloth sails, door at its base. A row of tall poles with cloth windsocks all blowing the same way. A big bronze weathervane on a stone post. A line of stone wind-catcher walls with gaps. A hanging rack of metal tubes (wind chimes) of different lengths. A path on to a stone bell tower visible at the top edge.
[DRESSING] Long grass leaning with the wind, scattered leaves, hay bales, a kite stuck on a pole, low dry-stone walls, sheep pen (empty), wooden fences, wildflowers, a bench, a cart.
```

**5-3 종탑 내부**
```
MAP 5-3: The inside of a tall stone bell tower: a square shaft with a wooden stair winding up and bell ropes hanging down.
[MUST HAVE] A ground door to the hill. Wooden stairs and landings around the walls. Four bell ropes of different thickness hanging to the floor in a row. A big clockwork mechanism with gears and a long pendulum. A row of glass goblets and jars of different sizes on a shelf. A wall niche with a set of tuning-fork-like metal bars.
[DRESSING] Dusty beams, pigeon feathers, spare rope coils, a workbench, oil cans, sandbags, a ladder, narrow window slits, a monk's stool, stacked hymn books, cobwebs.
```

**5-4 종루 꼭대기**
```
MAP 5-4: The open belfry at the top of the tower: big bronze bells under a timber roof, open arches on all sides, sky all around.
[MUST HAVE] A stair hatch in the floor. Four bronze bells of clearly different sizes hung on a wooden frame. Louvered wooden shutters on the arches, some open. A sealed stone niche in the central pillar with a round bronze plate. A long brass speaking tube running down through the floor. A rope bridge leading away toward distant ruins.
[DRESSING] Green patina streaks, bird nests (empty), bell clappers, oil pots, coils of rope, a broken shutter, wind-blown leaves, a small telescope on the parapet, weathered beams, loose roof tiles.
```

### 6장 — 별의 폐허 천문대 (중력 · 그레비)

**6-1 폐허의 진입로**
```
MAP 6-1: The approach to a ruined mountaintop observatory at dusk: broken colonnade and stairs under the first stars.
[MUST HAVE] The rope bridge arriving at one side. A grand stair up to a cracked observatory doorway. A row of standing and fallen columns. A big stone balance-scale monument with two hanging pans. A star map carved as a round floor mosaic (dots and lines only, no text). A gap in the stair where blocks have fallen.
[DRESSING] Rubble, weeds in cracks, toppled statues of scholars, a dry fountain, broken telescopes, a tattered banner, lanterns on posts (unlit), moonlit puddles, a few pine trees.
```

**6-2 진자의 방**
```
MAP 6-2: The great hall of the ruined observatory with a huge pendulum hanging from a broken dome.
[MUST HAVE] The entrance doorway. A very long pendulum with a heavy brass bob hanging over a ring of small standing pegs on a circular floor. A big brass armillary sphere. Two side doors. Stone weights of different sizes on a rack. A gallery level reached by a stair, partly collapsed.
[DRESSING] Fallen dome stones, star charts on cracked walls, a long table with instruments, scattered books, roots and ivy coming through, a hole in the roof showing stars, brass rings on the floor, dust.
```

**6-3 관측 돔**
```
MAP 6-3: The observation dome: a giant brass telescope on a geared mount under a split-open roof.
[MUST HAVE] A stair from the hall. The telescope on a rotating platform with crank wheels. A counterweight system: chains, pulleys and iron weights along one wall. A star chart table with a round brass plate and movable arms. A reclining observer's chair. A sealed round door in the floor ringed with plain brass.
[DRESSING] Lens caps, cloth covers, notebooks, an hourglass, a brazier (cold), rolled charts in racks, a ladder on wheels, cracked marble, fallen leaves, a quadrant on a stand.
```

**6-4 저울의 방**
```
MAP 6-4: A hidden chamber of weights and pulleys below the dome where falling and balance were studied.
[MUST HAVE] One doorway. A tall drop shaft with marks on the wall (plain lines) and a landing cushion at the bottom. Three hanging platforms on ropes over pulleys at different heights. A long ramp with grooves and small bronze balls in a tray. A large beam balance in the center. A final sealed stone door with seven small round sockets around it.
[DRESSING] Sandbags, coiled rope, wooden wedges, chalk dust, feather and lead-ball pairs on a shelf, a water clock, benches, measuring rods, ivy, a cracked skylight with stars.
```

### 최종장 — 마을 광장 (실험 재판)

**7-1 마을 광장**
```
MAP 7-1: A medieval village square on a busy golden afternoon, wide open center.
[MUST HAVE] A stone well in the middle. A wooden stage on one side with a long demonstration table on it. A town hall with a clock tower face (no numbers). Four streets leaving the square. A row of market stalls with awnings. A bell on a post.
[DRESSING] Cobblestones, bunting, barrels, crates of apples and bread, flower pots on windowsills, benches, a cart, hay, pigeons' perches (empty), shop signs with pictures only, lanterns (unlit), a notice board (blank).
```

**7-2 시장 골목**
```
MAP 7-2: A narrow market lane off the square, crowded with stalls and workshop fronts.
[MUST HAVE] An opening to the square at one end and a back gate at the other. A glassblower's stall with lenses and bottles. A clockmaker's window. A smith's stall with magnets and nails. A water carrier's cart with a hand pump. A balcony with a rope pulley and a basket.
[DRESSING] Awnings, hanging signs with pictures only, baskets of vegetables, cloth bolts, pottery, cages (empty), ladders, steps up to doors, drains, potted herbs, a sleeping-mat sized rug, stacked chairs.
```

**7-3 재판 무대**
```
MAP 7-3: The stage in the village square prepared for a public experiment trial, seen close.
[MUST HAVE] A raised wooden platform with steps on both sides. A long table with seven empty stands in a row for instruments. A tall judge's desk on one side. Rows of empty benches for the crowd below. A large covered object under a cloth in the center back. Poles with banners at the corners.
[DRESSING] A water barrel, a brazier (cold), a rack of mirrors and lenses, a bell, ropes and pulleys overhead, crates with straw, scrolls on the judge's desk, lantern posts (unlit), flower garlands, scattered petals.
```

---

## PixelLab으로 세션이 직접 뽑기 (2026-11-16 연동)
- **연결:** PixelLab MCP(`https://api.pixellab.ai/mcp`)를 세션이 HTTP로 직접 부른다. API 토큰은 클라우드 환경 `sleeping-sparks`의 네트워크 비밀로 저장돼 있다(저장소·문서에는 없다).
- **계정:** 체험(trial) 플랜. 한 번에 1개씩만 생성된다. 시작 시 생성 40회 + 하루 보너스 20회.
- **쓸 도구:** `create_image_pixen` — 640×360까지, 1장에 생성 1회, 약 2분. 시점 지정은 `low top-down`/`high top-down`뿐이라 **쿼터뷰(마름모 바닥)는 말로 적어도 나오지 않는다.** 품질을 올릴 때는 `create_image_pro`(688×384까지, 10~25회).
- **한계:** 한 장이 한 화면(640×360) 크기다. 더 큰 맵은 여러 장을 이어 붙이거나 다른 도구를 써야 한다.
- **상업 이용 조건 미확인** — 약관 페이지가 네트워크 정책에 막혀 읽지 못했다. 확인해서 `licenses.md`에 적기 전까지 **결과물은 레퍼런스 전용**이다.
- PixelLab에는 위 블록을 통째로 넣지 않고 한 문단으로 줄여 넣는다(장면 → 꼭 있어야 할 것 → 그림체 → 금지 → 크기 → 시점).

### 시안 기록
| 날짜 | 맵 | 도구 | 파일 | 비고 |
|---|---|---|---|---|
| 2026-11-16 | `P-1` 필사실 | PixelLab Pixen | `refs/maps_2026-11/P-1_{LT,LQ,MT,MQ,ST,SQ}_01.png` (640×360), 비교 그림 `P-1_compare_with_luca.png`(지금 루카 32×48과 1.5배 키운 48×72를 얹음) | 크기 3종 × 시점 2종. 생성 6회 사용. **레퍼런스 전용.** ① 6장 모두 정면 3/4 탑다운으로 나옴(Q 지정이 먹지 않음). ② 크기 차이는 가구 크기로 드러남: L은 책상이 루카 48×72와 맞고, S는 32×48과 맞는다. ③ 촛불 불꽃이 "금지"라고 적어도 그려짐. ④ 외곽선은 대체로 제 색 계열의 짙은 색. ⑤ 꼭 있어야 할 것 가운데 돌판·쇠창살·수반은 나오지만 수가 맞지 않음(돌판 2개 등) — 배치는 따로 손봐야 함 |
| 2026-11-18 | `P-1` 필사실 빈 방 | ChatGPT (`background-prompts.md` 프롬프트 1) | `refs/maps_2026-11/P-1_room_01.png` (1672×941, 색 18,701개), 가이드와 겹친 그림 `P-1_room_01_vs_guide.png`, 사물 자리 다시 잡은 그림 `P-1_room_01_slots.png` | **시점·크기·밝기·그림체 좋음**(정면 3/4, 곧은 격자, 루카 48×72와 가구 축척 맞음, 불꽃·사람 없음, 바닥 빈 자리 남음). 문제: ① 가이드의 회색 상자 하나를 반투명 회색 상자로 그대로 그림(960~1150, 620~735) ② 가구 배치가 가이드와 달라 사물 자리(루카 책상·기둥 3·수반·깔때기)를 가구가 덮음 → 그림을 다시 뽑지 않고 **사물 자리를 그림에 맞춰 옮김**(`_slots.png`), ChatGPT에는 ①과 작은 책상 하나만 지우게 함 ③ 가짜 도트(굵은 알, 색 많음) → 1px 정리 필요 |
| 2026-11-18 | `P-1` 필사실 빈 방 고침 | ChatGPT (같은 대화, 두 곳 지우기) | `refs/maps_2026-11/P-1_room_02.png` (1672×941) | **채택.** 회색 상자와 작은 책상만 사라지고 나머지는 1차와 같음(달라진 32px 칸 33개가 모두 그 두 곳). 남은 일: 1px 도트 정리·색 64개 |
| 2026-11-18 | `P-1` 필사실 사물 시트 | ChatGPT (프롬프트 2, 같은 대화) | `refs/maps_2026-11/P-1_props_01.png` (1536×1024), 세션이 잘라 규격 크기로 줄인 것 `P-1_props_01_cut/`, 방에 얹은 미리보기 `P-1_room_02_with_props.png` | **대체로 채택.** 10개 모두 나옴, 그림체가 방과 맞음. ① 물건끼리 크기 비율이 제각각(바구니·쇠창살이 큼) → 하나씩 규격 크기로 줄여 해결 ② 알파가 전부 반투명(255 없음) → 정리 때 0/255로 ③ 수반이 벽걸이가 아니라 받침 수반이고 물 나오는 곳이 없음 → **벽 꼭지(spout) 하나 추가 요청**, 물줄기는 꼭지→수반, 받침돌은 물줄기 양옆 벽 선반, 깔때기는 왼쪽 아래 바닥(SLOTS 고침) ④ 돌판이 아래가 넓은 사다리꼴(원근이 조금 들어감) — 쓸 만함 |
| 2026-11-18 | `P-1` 빈 방 정리 시도 | Retro Diffusion Pixel Art Fixer(사람) → 세션 색 줄이기 | `refs/maps_2026-11/P-1_room_02_64.png` (1672×941, 62색, `tools/sprites/reduce_colors.py`) | Fixer 결과 두 장: 1장은 바닥·벽에 주황 얼룩, 청록 책등이 남색으로. 2장은 원본과 거의 같음(평균 차 1/255) — 격자 정리가 일어나지 않음. 원본엔 일정한 도트 격자가 없다(3·4·5px 주기 모두 고르게 나옴). **세션이 원본을 64색 이하로 줄임**: 포인트 색(청록) 팔레트를 따로 잡아 사라지지 않게, 디더 없음, 바닥·벽은 평평. 도트 한 알이 루카보다 굵은 차이는 남음 → 임시본으로 쓰고 Chickxel 정리는 나중 |
| 2026-11-18 | `P-1` 벽 꼭지 | ChatGPT (같은 대화) | `refs/maps_2026-11/P-1_spout_01.png` (1536×1024), 자른 것 `P-1_props_01_cut/spout.png` (32×32) | **채택.** 돌 주둥이가 왼쪽 아래로 꺾임. 외곽선이 어두운 한 색(#433840)이지만 32px로 줄이면 섞여 거의 안 보임(`own_outline.py`가 바꿀 픽셀 0개). 미리보기 `P-1_room_02_with_props.png`에서 꼭지 → 물줄기 → 수반이 맞음 |

## 고른 뒤에 받을 것 (같은 대화에서)
1. `Same map, same layout, but remove all movable furniture and props. Keep only walls, floor, doors, windows and fixed structures.` — 가림 레이어용 빈 방.
2. 상태가 바뀌는 곳의 덧그림: `Same style and scale. Only this object, on a plain flat magenta background: <열린 돌판 계단 / 덩굴이 없는 아치 / 녹은 얼음벽 …>`
3. 크기와 시점이 정해지면 세션이 `CLAUDE.md` 규격표, `background-prompts.md`, 맵별 좌표 작업 순서를 고친다.

## 점검 (세션이 한다)
- 받은 시안을 `design/refs/maps_2026-11/`에 모으면: 크기·비율 기록, 캐릭터(루카 설정화)와 나란히 놓은 비교 그림, `art-direction.md` "위화감을 만드는 것" 다섯 가지 점검.
- 게임에 넣기 전: Chickxel 도트 정리(제 색 외곽선 공통 문구) → `asset_check.py` → `licenses.md`.
