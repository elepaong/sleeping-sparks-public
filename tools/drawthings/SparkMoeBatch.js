//@api-1.0
// 잠든 스파크의 수도원 — 스파크 "사물 모에화" 디자인 시안
//
// 드라이브 '시안' 폴더의 ChatGPT 시안처럼, 사물 자체가 둥근 몸이 되고
// 작은 손(몸에서 떨어져 떠 있음)과 작은 발만 달린 마스코트로 뽑는다.
// 시안 캐릭터를 잘라 만든 바탕 그림(사진/spark_init/*.png)을 이미지→이미지로 넣어 체형을 고정한다.
//
// 사용 전 화면 설정: 모델 waiIllustriousSDXL_v170. LoRA는 스크립트가 끈다(도트 LoRA가 사람형 몸을 만듦).
// 결과: 사진 폴더 "moe_<id>_<안>_<번호>_seed<시드>.png"

// ───── 설정 ─────
const VARIATIONS = 3;   // 안(案)마다 몇 장
const ONLY = [];        // 예) ["mag", "hwii"]  비워두면 전부

const STYLE =
  "masterpiece, best quality, no humans, tiny cute object mascot, anthropomorphic object, " +
  "the object itself is the round chubby body, face on the body, small black dot eyes, " +
  "small open smile, rosy blush, tiny round floating hands on both sides, two tiny round feet, " +
  "dashed brown ink outline, soft watercolor, pastel colors, storybook illustration, chibi, solo, " +
  "centered, plain parchment paper background, soft shadow under feet";

const NEGATIVE =
  "human, humanoid, person, animal, sheep, horns, torso, neck, long arms, long legs, fingers, animal ears, bunny ears, " +
  "tail, hair, wings, multiple views, reference sheet, text, watermark, signature, frame, border, " +
  "realistic, 3d, photo, lowres, worst quality, bad anatomy";

// init: 바탕 그림, strength: 높을수록 바탕에서 멀어짐 (시안과 같은 사물이면 낮게)
const SPARKS = [
  { id: "jjirit", plan: "A", init: "fire1",    strength: 0.75, tags: "round fluffy white cotton puff ball body, crackling pale blue static electricity sparks around it" },
  { id: "jjirit", plan: "B", init: "magnet0",  strength: 0.75, tags: "round golden amber gemstone body, tiny pale blue lightning sparks" },
  { id: "lumi",   plan: "A", init: "magnet0",  strength: 0.75, tags: "clear crystal glass prism body, warm cream-yellow light inside, tiny rainbow beam" },
  { id: "lumi",   plan: "B", init: "firetool1",strength: 0.65, tags: "small round glass lantern body with a warm glowing light inside, brass cap on top" },
  { id: "hwaro",  plan: "A", init: "firetool0",strength: 0.55, tags: "small round iron stove body with a glowing fire window, little lid on top" },
  { id: "hwaro",  plan: "B", init: "fire0",    strength: 0.6,  tags: "glowing charcoal ember lump body with orange cracks, tiny sparks" },
  { id: "mag",    plan: "A", init: "magnet0",  strength: 0.5,  tags: "round magnet ball body, red top half and blue bottom half" },
  { id: "mag",    plan: "B", init: "magnet2",  strength: 0.6,  tags: "round brass compass body, red and blue needle on top" },
  { id: "mulmalle",plan: "A", init: "water0",  strength: 0.5,  tags: "round glass flask body with cork stopper, light blue water inside" },
  { id: "mulmalle",plan: "B", init: "water2",  strength: 0.6,  tags: "round glass dropper body with red rubber bulb on top, water inside" },
  { id: "ullim",  plan: "A", init: "water1",   strength: 0.75, tags: "small shiny brass hand bell body, wooden handle on top, faint sound wave rings" },
  { id: "ullim",  plan: "B", init: "firetool0",strength: 0.75, tags: "small round drum body with leather drumhead and red rope pattern" },
  { id: "hwii",   plan: "A", init: "wind0",    strength: 0.5,  tags: "cream round body, green and white paper pinwheel on its head, drifting green leaves" },
  { id: "hwii",   plan: "B", init: "magnet0",  strength: 0.75, tags: "small leather bellows body with wooden handles, little puff of wind" },
  { id: "gravi",  plan: "A", init: "fire0",    strength: 0.7,  tags: "round smooth grey stone body, a tiny pebble orbiting around it like a moon" },
  { id: "gravi",  plan: "B", init: "water1",   strength: 0.7,  tags: "round iron plumb bob weight body, string loop on top, heavy" },
];

// ───── 실행 ─────
const base = pipeline.configuration;
base.width = 1024;
base.height = 1024;
base.batchCount = 1;
base.batchSize = 1;
base.loras = [];

const pics = filesystem.pictures.path;
const targets = ONLY.length ? SPARKS.filter(s => ONLY.includes(s.id)) : SPARKS;
let done = 0;

for (let i = 1; i <= VARIATIONS; i++) {
  for (const s of targets) {
    base.seed = Math.floor(Math.random() * 4294967295);
    base.strength = s.strength;
    canvas.clear();
    canvas.loadImage(pics + "/spark_init/" + s.init + ".png");
    pipeline.run({ configuration: base, prompt: STYLE + ", " + s.tags, negativePrompt: NEGATIVE });
    const file = pics + "/moe_" + s.id + "_" + s.plan + "_" + i + "_seed" + base.seed + ".png";
    canvas.saveImage(file, false);
    done++;
    console.log("saved " + file);
  }
}

console.log("완료: " + done + "장");
