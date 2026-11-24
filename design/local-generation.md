# 로컬 이미지 생성 (ComfyUI · Draw Things)

Scenario 무료 플랜 대신 맥북(Apple Silicon)에서 직접 생성할 수 있는지 확인한 결과입니다 (2026-11-06).

## 결론
- **된다:** 스파크 도감 카드, 대화 초상화, 양피지 삽화, 문 퍼즐 그림처럼 **큰 일러스트**. 구글 드라이브 `시안` 폴더의 ChatGPT 시안(둥근 마스코트, 점선 갈색 테두리, 파스텔 수채, 양피지)과 같은 계열을 WAI-Illustrious SDXL로 낼 수 있습니다.
- **출발점은 있다:** 32×32 타일, 24×24 필드 스파크, 32×48 걷기 프레임. Draw Things에 도트 LoRA 3종이 있어 픽셀 그림은 나오지만, 규격으로 줄이고 프레임을 맞추는 일은 따로 해야 합니다.
- 크레딧 제한이 없고, 상업 이용은 **모델 라이선스**만 확인하면 됩니다 (Scenario 무료 플랜의 "레퍼런스 전용" 제약이 없음).

## 설치 현황

| 항목 | 내용 |
|---|---|
| ComfyUI | 0.43.1 (standalone, MPS), `~/ComfyUI-Installs/ComfyUI`, 모델은 `~/ComfyUI-Shared/models`, 주소 `http://127.0.0.1:8188` |
| 커스텀 노드 | ComfyUI-GGUF 하나 |
| 속도 | SDXL 1024×1024, 28스텝에 약 2분 (3.7초/스텝, 로그 실측) |
| Draw Things | WAI-Illustrious + LoRA, 로컬 스크립트 `SparkSpriteBatch.js`(내용 미확인). 모델 폴더는 macOS 권한으로 직접 열 수 없어 앱 화면에서 확인 |

| 모델 | 쓸 수 있나 |
|---|---|
| `waiIllustriousSDXL_v170` (체크포인트) | **사용.** 애니·치비 계열이라 시안 그림체에 맞음 |
| `lustifyNSFWCheckpoint_zenithV9`, `…v10Krea2_q40.gguf` | 사용 안 함. 실사 NSFW 모델 |
| `SDXL_BetterFaces-LoRA_v1` | 사용 안 함. 실사 얼굴용 |
| `arin_model` (LoRA) | 용도 불명, 사용 안 함 |
| `qwen3vl_4b`, `wan_2.1_vae` | Krea2 GGUF용 부속 |

## Draw Things 로컬 LoRA (2026-11-06 앱 화면 확인)

| LoRA | 용도 |
|---|---|
| `pixel_character_sprite_illustrious` | **도트 캐릭터 스프라이트.** Illustrious용이라 WAI와 맞음. 찌릿·루카 픽셀 시안을 이걸로 뽑은 기록 있음 (가중치 86%) |
| `pixel-art-xl-v1.1` | 도트 일반 (SDXL). 타일·소품 후보 |
| `Pixel_Xl_V1` | 도트 일반 (SDXL). 위와 비교 필요 |
| `LCM SDXL Base` | 적은 스텝으로 빠르게 생성 |
| `HerrscherAGGA20…`, `arin_model`, `SDXL_BetterFaces` | 프로젝트와 무관 |

공식 LoRA 목록에 **Transparent Image**(투명 배경 생성)가 있어 내려받으면 도감 카드의 배경 제거를 대신할 수 있습니다. ComfyUI 쪽에는 도트 LoRA가 없으니, 쓰려면 파일을 `~/ComfyUI-Shared/models/loras`로 복사합니다.

## 시험 생성 결과 (ComfyUI, 2026-11-06)
- `spark_cards.py`와 같은 워크플로로 루미·휘이 1장씩, 장당 약 145초.
- 둥근 몸, 볼터치, 수채 질감은 시안과 맞음. 점선 테두리·양피지 바탕은 안 나오고, 색과 소품이 시안과 달라짐 (휘이 바람개비가 빨강·노랑, 루미에 잎사귀·가짜 서명).
- Draw Things를 켠 상태에서는 남는 메모리가 3.7GB 정도라, 여러 장을 돌릴 때는 하나만 켭니다 (맥 메모리 16GB).

## 없는 것 (필요하면 추가)

| 필요 | 이유 | 후보 |
|---|---|---|
| 배경 제거 | 도감 카드는 투명 PNG | BiRefNet / RMBG 모델 + 노드 |
| 그림체 LoRA | 스파크 8종의 그림체를 고정 | ChatGPT 시안 + 1차 생성물 20~40장으로 학습 (Draw Things에 학습 기능 있음) |
| IP-Adapter 또는 캐릭터 LoRA | 루카 표정 4종을 같은 얼굴로 | SDXL IP-Adapter |
| 축소·팔레트 정리 도구 | 도트 LoRA 결과를 24×24·32×32로 | 결과물은 손으로 다듬어야 함 |
| 업스케일러 | 1200×800 이상 출력 | 4x-AnimeSharp 등 |

## 대상별 판단

| 에셋 | 규격 | 로컬 생성 | 방법 |
|---|---|---|---|
| 스파크 도감 | 512 투명 | 가능 (배경 제거 필요) | 1024 생성 → 배경 제거 → 512 축소 |
| 대화 초상화 | 512, 표정 4종 | 1장은 가능, 표정 4종 일치는 IP-Adapter·LoRA 필요 | |
| 문 퍼즐 그림 | 1200×800 | 가능 | 1216×832 생성 → 축소 |
| 양피지 삽화 | 800×600 | 가능 | 글씨는 생성 후 지움 |
| 타일 | 32×32 | 시험 필요 | `pixel-art-xl` + 축소 + 수작업 |
| 필드 캐릭터·스파크 | 32×48 / 24×24 프레임 | 1장은 가능, 프레임 일관성은 과제 | `pixel_character_sprite_illustrious` + 축소 |

## Draw Things 밤샘 생성 (추천)
- 장당 약 1분 9초 (WAI + `pixel_character_sprite_illustrious` 86%, 1024², 16스텝, DPM++ 2M AYS). ComfyUI(28스텝 약 145초)보다 빠름.
- 스크립트 원본: `tools/drawthings/SparkSpriteBatch.js`. Draw Things → 스크립트 → 로컬에 같은 이름으로 붙여넣고 ▶로 실행.
- 인물 5명 + 스파크 8종 × 20장 = 260장, 약 5시간. 번호 1을 전원 먼저 돌고 번호 2로 넘어가서 중간에 멈춰도 고르게 남음.
- 결과: `~/Library/Containers/com.liuliu.draw-things/Data/Pictures/spark_<id>_<번호>_seed<시드>.png` (Draw Things 앱 전용 사진 폴더, 일반 사진 폴더 아님). 잘 나온 건 시드로 다시 뽑거나 키움.
- 밤새 돌릴 때: 전원 연결, 터미널에 `caffeinate -i` 켜둔 채(터미널 닫지 않기), 뚜껑 열어 둠, ComfyUI는 끔.

## 쓰는 법 (ComfyUI)
1. ComfyUI를 켭니다.
2. `python3 tools/comfyui/spark_cards.py` — 스파크 8종 × 4장을 큐에 넣습니다 (약 64분). `lumi hwaro -n 8`처럼 골라 넣을 수 있습니다.
3. 워크플로만 보려면 `tools/comfyui/spark_card_api.json`을 ComfyUI 화면에 끌어다 놓습니다.
4. 결과는 `~/ComfyUI-Shared/output/spark_<id>_*.png`.

## 확인할 것
- 2026-11-10에 확인한 내용은 [`licenses.md`](licenses.md)에 적었습니다. 설치한 버전의 Civitai 페이지와 LoRA별 조건 확인이 남아 있습니다.
- WAI-Illustrious와 바탕 모델(Illustrious XL)의 라이선스에서 생성물 상업 이용 조건을 Civitai 모델 페이지에서 확인하고 여기에 적습니다.
- ChatGPT 시안을 LoRA 학습 데이터로 쓰는 경우, OpenAI 이용약관상 생성물 권리는 사용자에게 있지만 함께 확인합니다.
