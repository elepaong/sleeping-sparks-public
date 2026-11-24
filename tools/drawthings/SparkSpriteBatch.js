//@api-1.0
// 잠든 스파크의 수도원 — 캐릭터·스파크 도트 스프라이트 밤샘 일괄 생성
//
// 사용 전 화면 설정(이 스크립트가 그대로 이어받음):
//   모델: waiIllustriousSDXL_v170
//   LoRA: pixel_character_sprite_illustrious (가중치 약 86%)
//   단계 16, DPM++ 2M AYS (이 설정에서 장당 약 1분)
//
// 결과: 사진(Pictures) 폴더에 "spark_<캐릭터>_<번호>_seed<시드>.png" 로 저장
// 순서: 번호 1을 모든 캐릭터에 대해 먼저 뽑고, 그다음 번호 2... (중간에 멈춰도 전 캐릭터가 고르게 남음)

// ───── 설정 ─────
const VARIATIONS = 20;        // 캐릭터당 장 수 (13종 × 20 = 260장, 약 4.5~5시간)
const WIDTH = 1024;
const HEIGHT = 1024;
const FILE_PREFIX = "spark_";
const ONLY = [];              // 특정 캐릭터만: 예) ["lumi", "hwii"]  비워두면 전부

// 모든 캐릭터에 공통으로 붙는 태그
const COMMON =
  "masterpiece, best quality, pixel art, sprite, chibi, solo, full body, standing, " +
  "front view, looking at viewer, simple background, white background";

const HUMAN_COMMON = "human ears, european medieval clothes";

// 스파크 공통: 드라이브 '시안' 폴더의 둥근 마스코트 기준
const SPARK_COMMON =
  "no humans, cute mascot creature, round plump body, tiny stubby arms, short round feet, " +
  "small black dot eyes, rosy cheeks, small open smile";

const NEGATIVE =
  "lowres, worst quality, bad anatomy, multiple views, reference sheet, pointy ears, elf, " +
  "japanese clothes, kimono, sandals, staff, weapon, realistic, 3d, blurry, text, " +
  "watermark, scenery, floor";

const SPARK_NEGATIVE =
  "lowres, worst quality, bad anatomy, multiple views, reference sheet, human, " +
  "long ears, long legs, long arms, tail, realistic, 3d, blurry, text, watermark, scenery, floor";

// 캐릭터별 묘사. 바꾸고 싶으면 여기만 고치면 됨
const CHARACTERS = [
  { id: "luca", human: true,
    tags: "1boy, smile, brown hair, short hair, brown hooded habit, hood down, long sleeves, " +
          "rope belt, leather boots, holding quill, small round blue fire spirit with cute face " +
          "floating at his shoulder" },
  { id: "isolde", human: true,
    tags: "1girl, adult woman, gentle smile, silver hair, long braid, green dress, " +
          "herb-stained apron, magnifying glass pendant, holding small glass flask" },
  { id: "martin", human: true,
    tags: "1boy, old man, elderly, tired smile, white hair, white beard, round glasses, " +
          "black monk habit, rope belt, wooden cross necklace, holding old book" },
  { id: "gregor", human: true,
    tags: "1boy, adult man, stern expression, sharp features, black hair, black cloak, " +
          "dark inquisitor robes, high collar, holding sealed scroll" },
  { id: "vera", human: true,
    tags: "1girl, teenage girl, confident smile, auburn hair, ponytail, freckles, " +
          "leather apron, rolled sleeves, holding hammer, leather boots" },

  { id: "jjirit", human: false,
    tags: "pale sky-blue flame-shaped body with round bottom, two short rounded triangle ears, " +
          "curled antenna with a tiny white spark, small electric sparks, floating" },
  { id: "lumi", human: false,
    tags: "warm cream-yellow glowing ball body, short stem on head with a small clear crystal prism, " +
          "tiny rainbow sparkles" },
  { id: "hwaro", human: false,
    tags: "dark brown ember rock body with glowing orange cracks, small flame on head, floating coals" },
  { id: "mag", human: false,
    tags: "round body, red top half and blue bottom half like a magnet, small grey iron pebbles floating around" },
  { id: "mulmalle", human: false,
    tags: "translucent light aqua water drop body, water swirl on head, small splash droplets" },
  { id: "ullim", human: false,
    tags: "small round bronze bell-shaped body, concentric sound wave rings around it" },
  { id: "hwii", human: false,
    tags: "cream round body, green and white paper pinwheel on head, green leaves blowing" },
  { id: "gravi", human: false,
    tags: "heavy round slate purple stone body, tiny pebble orbiting like a moon" },
];

// ───── 실행 ─────
const base = pipeline.configuration;
base.width = WIDTH;
base.height = HEIGHT;
base.batchCount = 1;
base.batchSize = 1;

const outDir = filesystem.pictures.path;
const targets = ONLY.length ? CHARACTERS.filter(c => ONLY.includes(c.id)) : CHARACTERS;
let done = 0;

for (let i = 1; i <= VARIATIONS; i++) {
  for (const c of targets) {
    const prompt = [COMMON, c.human ? HUMAN_COMMON : SPARK_COMMON, c.tags].join(", ");
    const negative = c.human ? NEGATIVE : SPARK_NEGATIVE;
    base.seed = Math.floor(Math.random() * 4294967295);
    canvas.clear();
    pipeline.run({ configuration: base, prompt: prompt, negativePrompt: negative });
    const file = outDir + "/" + FILE_PREFIX + c.id + "_" + i + "_seed" + base.seed + ".png";
    canvas.saveImage(file, false);
    done++;
    console.log("saved " + file);
  }
}

console.log("완료: " + done + "장 저장 → " + outDir);
