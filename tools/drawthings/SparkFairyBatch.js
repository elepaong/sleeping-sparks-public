//@api-1.0
// 잠든 스파크의 수도원 — 스파크 요정 디자인 시안 (물건 모에화)
//
// 드라이브 '시안' 폴더 기준: 물건 자체가 둥근 몸이 되는 작은 요정 마스코트,
// 점선 갈색 테두리, 파스텔 수채, 양피지 느낌.
// 모델: waiIllustriousSDXL_v170. LoRA는 이 스크립트에서 끈다 (도트 LoRA가 사람형으로 끌어서).
//
// 결과: 사진(Pictures) 폴더에 "fairy_<스파크>_<물건>_<번호>_seed<시드>.png"
// 순서: 번호 1을 모든 물건에 대해 먼저 → 번호 2 ... (중간에 멈춰도 고르게 남음)

// ───── 설정 ─────
const VARIATIONS = 3;         // 물건당 장 수 (8종 × 3물건 × 3장 = 72장, 약 75분)
const WIDTH = 1024;
const HEIGHT = 1024;
const ONLY = [];              // 특정 스파크만: 예) ["mag", "hwii"]

const STYLE =
  "masterpiece, best quality, no humans, solo, full body, tiny chibi mascot, " +
  "anthropomorphic object, kawaii object with a face, round chubby body, " +
  "small black dot eyes, rosy blush, tiny open smile, tiny stubby round arms, tiny round feet, " +
  "cute, simple design, children's book watercolor illustration, soft pastel colors, " +
  "thin dashed brown outline, gentle soft glow, parchment paper texture, plain cream background, " +
  "small soft shadow under feet, centered";

const NEGATIVE =
  "worst quality, low quality, lowres, human, humanoid, girl, boy, long arms, long legs, neck, hair, " +
  "animal ears, realistic, 3d, pixel art, text, watermark, signature, multiple views, reference sheet, " +
  "frame, border, wings, fairy, halo, crown, holding, creature inside container";

// 스파크별 모에화할 물건 3개: [파일 키, 물건, 덧붙일 묘사]
const SPARKS = [
  { id: "jjirit", items: [
    ["amber", "round honey-colored amber gem", "tiny static sparks crackling around it"],
    ["wool", "fluffy round ball of white wool yarn", "tiny blue static sparks around it"],
    ["jar", "small round glass jar with a brass knob on its lid", "a tiny blue spark at the knob"] ] },
  { id: "lumi", items: [
    ["candle", "short round cream beeswax candle", "a tiny warm flame on top of its head"],
    ["lantern", "small round brass oil lantern", "glass panes glowing warmly"],
    ["prism", "small clear glass prism", "a tiny rainbow beam shining out"] ] },
  { id: "hwaro", items: [
    ["stove", "small round black iron stove pot with a lid", "a glowing fire window on its belly"],
    ["lamp", "round glass alcohol lamp", "a small flame on its wick"],
    ["kettle", "round copper kettle", "a little puff of steam from its spout"] ] },
  { id: "mag", items: [
    ["horseshoe", "round red and blue horseshoe magnet", "tiny iron nails stuck to it"],
    ["compass", "round brass compass", "a red needle on its face"],
    ["lodestone", "round dark grey lodestone rock", "tiny iron filings standing up on it"] ] },
  { id: "mulmalle", items: [
    ["flask", "round glass flask with a cork stopper", "half filled with light blue water"],
    ["bobber", "round red and white fishing float bobber", "floating on a small water ripple"],
    ["bucket", "small round wooden water bucket with a rope handle", "water splashing"] ] },
  { id: "ullim", items: [
    ["bell", "small round golden brass handbell", "tiny sound wave arcs around it"],
    ["fork", "round silver tuning fork", "tiny sound wave arcs around it"],
    ["drum", "little round wooden drum with a leather top", "tiny sound wave arcs around it"] ] },
  { id: "hwii", items: [
    ["pinwheel", "round paper pinwheel with green and white blades", "green leaves drifting"],
    ["bellows", "round brown leather bellows with a brass nozzle", "a little puff of air"],
    ["sail", "round wooden boat with a small green and white striped sail", "small wind swirls"] ] },
  { id: "gravi", items: [
    ["apple", "round shiny red apple with a leaf stem", "falling slightly"],
    ["pendulum", "round brass pendulum bob on a short string", "swinging gently"],
    ["weight", "round heavy iron balance weight with a ring handle", "sitting heavily"] ] },
];

// ───── 실행 ─────
const base = pipeline.configuration;
base.width = WIDTH;
base.height = HEIGHT;
base.batchCount = 1;
base.batchSize = 1;
base.loras = [];

const outDir = filesystem.pictures.path;
const targets = ONLY.length ? SPARKS.filter(s => ONLY.includes(s.id)) : SPARKS;
let done = 0;

for (let i = 1; i <= VARIATIONS; i++) {
  for (const s of targets) {
    for (const [key, obj, extra] of s.items) {
      base.seed = Math.floor(Math.random() * 4294967295);
      canvas.clear();
      const prompt = "(a cute " + obj + " with a little smiling face, the whole body is the " + obj + ":1.3), " +
        extra + ", " + STYLE;
      pipeline.run({ configuration: base, prompt: prompt, negativePrompt: NEGATIVE });
      const file = outDir + "/fairy_" + s.id + "_" + key + "_" + i + "_seed" + base.seed + ".png";
      canvas.saveImage(file, false);
      done++;
      console.log("saved " + file);
    }
  }
}

console.log("완료: " + done + "장 저장 → " + outDir);
