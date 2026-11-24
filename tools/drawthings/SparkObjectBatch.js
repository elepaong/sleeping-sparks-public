//@api-1.0
// 잠든 스파크의 수도원 — 스파크 "물건 모양 그대로" 모에화 시안
//
// 물건의 실제 형태(발굽자석, 플라스크, 저울...)를 그대로 몸으로 쓰고,
// 얼굴과 떠 있는 작은 손, 짧은 발만 붙인 마스코트. 그림체는 드라이브 '시안' 폴더 기준.
// 바탕 그림 없이 텍스트→이미지. 도트 LoRA는 끈다.
//
// 사용 전 화면 설정: 모델 waiIllustriousSDXL_v170
// 결과: 사진 폴더 "obj_<id>_<안>_<번호>_seed<시드>.png"

// ───── 설정 ─────
const VARIATIONS = 3;   // 안마다 몇 장 (8종 × 2안 × 3장 = 48장, 약 40분)
const ONLY = [];        // 예) ["mag", "gravi"]  비워두면 전부

const STYLE =
  "masterpiece, best quality, no humans, tiny cute object mascot, anthropomorphic object, living object, " +
  "the whole body is the object itself keeping its real shape, cute face drawn on the object, " +
  "small black dot eyes, small open smile, rosy blush, tiny round hands floating beside it, " +
  "tiny round stubby feet, small and simple, dashed brown ink outline, soft watercolor, pastel colors, " +
  "storybook illustration, solo, centered, full body, plain parchment paper background, soft shadow";

const NEGATIVE =
  "human, humanoid, person, girl, boy, animal, torso, neck, arms, long legs, fingers, hair, " +
  "animal ears, wings, halo, multiple views, reference sheet, text, watermark, signature, frame, border, " +
  "realistic, 3d, photo, lowres, worst quality, bad anatomy";

const SPARKS = [
  { id: "jjirit",   plan: "A", tags: "a glass leyden jar with a brass rod and a brass ball on top, pale blue static sparks crackling from the ball" },
  { id: "jjirit",   plan: "B", tags: "a small chunk of golden amber stone with a tuft of wool stuck to it, tiny pale blue static sparks" },
  { id: "lumi",     plan: "A", tags: "a clear triangular glass prism, splitting a beam of light into a tiny rainbow" },
  { id: "lumi",     plan: "B", tags: "a short cream beeswax candle with a warm little flame on top" },
  { id: "hwaro",    plan: "A", tags: "a small black iron brazier bowl on three short legs, filled with glowing orange coals" },
  { id: "hwaro",    plan: "B", tags: "a round copper kettle with a little puff of steam from its spout" },
  { id: "mag",      plan: "A", tags: "a U-shaped horseshoe magnet, red body with silver tips, tiny iron nails stuck to the tips" },
  { id: "mag",      plan: "B", tags: "a round brass compass with a red and blue needle on its face" },
  { id: "mulmalle", plan: "A", tags: "a laboratory glass flask with a cork stopper, half filled with light blue water" },
  { id: "mulmalle", plan: "B", tags: "a small wooden water bucket with a rope handle, water splashing" },
  { id: "ullim",    plan: "A", tags: "a golden brass handbell with a wooden handle, tiny sound wave arcs" },
  { id: "ullim",    plan: "B", tags: "a silver tuning fork with two prongs, vibrating, tiny sound wave arcs" },
  { id: "hwii",     plan: "A", tags: "a paper pinwheel on a wooden stick, green and white blades, little gust of wind" },
  { id: "hwii",     plan: "B", tags: "a leather bellows with wooden handles, puffing a little gust of wind" },
  { id: "gravi",    plan: "A", tags: "a small brass balance scale with two hanging pans" },
  { id: "gravi",    plan: "B", tags: "a pendulum, an iron plumb bob weight hanging from a string" },
];

// ───── 실행 ─────
const base = pipeline.configuration;
base.width = 1024;
base.height = 1024;
base.batchCount = 1;
base.batchSize = 1;
base.loras = [];
base.strength = 1.0;

const pics = filesystem.pictures.path;
const targets = ONLY.length ? SPARKS.filter(s => ONLY.includes(s.id)) : SPARKS;
let done = 0;

for (let i = 1; i <= VARIATIONS; i++) {
  for (const s of targets) {
    base.seed = Math.floor(Math.random() * 4294967295);
    canvas.clear();
    pipeline.run({ configuration: base, prompt: STYLE + ", " + s.tags, negativePrompt: NEGATIVE });
    const file = pics + "/obj_" + s.id + "_" + s.plan + "_" + i + "_seed" + base.seed + ".png";
    canvas.saveImage(file, false);
    done++;
    console.log("saved " + file);
  }
}

console.log("완료: " + done + "장");
