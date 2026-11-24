//@api-1.0
// 잠든 스파크의 수도원 — 스파크 최종 그림을 도트로 변환
//
// ChatGPT로 만든 최종 그림(정사각, 단색 배경)을 이미지→이미지로 넣고 도트 LoRA를 걸어 변환한다.
// 강도(strength)가 낮을수록 원래 모양을 지킨다. 안 A/B는 LoRA만 다름.
// 결과: 사진 폴더 "pixel_<id>_<안>_<강도>_seed<시드>.png" (1024², 나중에 축소해 24×24·512 규격으로)

// ───── 설정 ─────
const SRC = "/Users/jsh/Downloads";          // spark_final_<id>.png 가 있는 폴더
const ONLY = [];                              // 예) ["jjirit"]  비워두면 전부
const STRENGTHS = [0.3, 0.4, 0.5];   // 0.5는 찌릿 몸통 색이 사라져서 낮춤. 낮을수록 원본 유지
const LORAS = {
  A: "pixel_character_sprite_illustrious",    // 화면에서 이 LoRA를 선택해 둔 상태로 실행 (B는 파일명 확인 후 추가)
};
const LORA_WEIGHT = 0.7;

const STYLE =
  "masterpiece, best quality, pixel art, sprite, 16-bit game sprite, clean dark outline, limited palette, " +
  "no humans, cute object mascot, small dot eyes, rosy blush, tiny round hands, tiny feet, " +
  "front view, centered, simple plain background";
const NEGATIVE =
  "human, humanoid, girl, boy, animal ears, long arms, long legs, blurry, smooth shading, watercolor, " +
  "realistic, 3d, text, watermark, signature, frame, border, multiple views, lowres, worst quality";

const SPARKS = [
  { id: "jjirit",   tags: "translucent honey-colored amber stone body, fluffy white wool on top, tiny blue static sparks" },
  { id: "lumi",     tags: "small hand mirror with a square gold frame and blue gem, face on the mirror glass" },
  { id: "hwaro",    tags: "candle on a brass candlestick, orange flame on top, face on the candlestick" },
  { id: "mag",      tags: "U-shaped horseshoe magnet, red and blue poles, no symbols" },
  { id: "mulmalle", tags: "teal bronze hand water pump with a handle and spout, water drop" },
  { id: "ullim",    tags: "silver steel tuning fork with two prongs on a small base" },
  { id: "hwii",     tags: "rooster weather vane with arrow, green base" },
  { id: "gravi",    tags: "silver-blue balance scale with two hanging pans" },
];

// ───── 실행 ─────
function findLora(part) {
  const all = pipeline.configuration.loras || [];
  // 화면에서 쓰던 LoRA 목록에서 못 찾으면 이름을 그대로 파일명으로 시도
  for (const l of all) if (l.file && l.file.indexOf(part) >= 0) return l.file;
  return null;
}

console.log("current loras: " + JSON.stringify(pipeline.configuration.loras));

const base = pipeline.configuration;
base.width = 1024;
base.height = 1024;
base.batchCount = 1;
base.batchSize = 1;

const pics = filesystem.pictures.path;
const targets = ONLY.length ? SPARKS.filter(s => ONLY.includes(s.id)) : SPARKS;
let done = 0;

for (const s of targets) {
  for (const plan of Object.keys(LORAS)) {
    const file = findLora(LORAS[plan]) || LORAS[plan];
    for (const st of STRENGTHS) {
      base.loras = [{ file: file, weight: LORA_WEIGHT }];
      base.strength = st;
      base.seed = Math.floor(Math.random() * 4294967295);
      canvas.clear();
      canvas.loadImage(SRC + "/spark_final_" + s.id + ".png");
      pipeline.run({ configuration: base, prompt: STYLE + ", " + s.tags, negativePrompt: NEGATIVE });
      const out = pics + "/pixel_" + s.id + "_" + plan + "_" + Math.round(st * 100) + "_seed" + base.seed + ".png";
      canvas.saveImage(out, false);
      done++;
      console.log("saved " + out + " (lora " + file + ")");
    }
  }
}
console.log("완료: " + done + "장");
