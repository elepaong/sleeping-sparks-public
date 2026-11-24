# 그림 도구별 사용 조건

게임에 들어갈 그림을 만드는 도구마다, 결과물을 Steam 출시작에 써도 되는지 확인한 기록입니다 (2026-11-10 확인, PixelLab은 2026-11-17).

- 여기 적힌 내용은 각 약관·라이선스 페이지를 **요약으로 읽은 것**이고 법률 자문이 아닙니다. 출시 전에 사람이 원문을 한 번 직접 읽습니다.
- 약관은 바뀝니다. 다시 확인하면 날짜를 고칩니다.

## 요약

| 도구 | 쓰는 곳 | 결과물을 게임에 써도 되나 | 근거 |
|---|---|---|---|
| ChatGPT (이미지) | 캐릭터·스파크·배경 시안 | 약관상 가능. 결과물 권리를 이용자에게 넘기고 상업 목적 이용을 허용 | OpenAI 이용약관 "Ownership of content" |
| Chickxel (앱) | 도트 정리·스프라이트 시트 | 가능. 공식 배포판은 상업 작업에도 무료, 만든 그림은 만든 사람 것 | GitHub README (LICENSE 전문은 미확인) |
| Chickxel의 AI 채팅 | 도트 생성 | 연결한 제공자 약관을 따름 (Codex → OpenAI, Claude Code → Anthropic) | GitHub README |
| WAI-Illustrious (로컬) | 큰 일러스트 후보 | 라이선스가 결과물을 다루지 않음(권리 주장 없음). 설치한 버전의 Civitai 페이지 확인이 남음 | Fair AI Public License 1.0-SD |
| Scenario 무료 플랜 | 초기 시안 | **불가.** 레퍼런스 전용, 유료로 올려도 소급되지 않음 | `scenario.md` |
| PixelLab (체험·유료 공통) | 맵·캐릭터·스파크 시안, 애니메이션 (세션이 API로 생성) | **약관상 가능.** 결과물 저작권은 이용자 것이고 상업·비상업 이용에 허락이 필요 없음. 플랜별 구분 없음. 결과물로 다른 모델을 학습시키는 것만 금지 | PixelLab Terms of Service 1.3·3.3 (최종 갱신 2026-10-30, 2026-11-17 확인) |
| Retro Diffusion | 후보 (아직 안 씀): 도트 생성·애니메이션, 가짜 도트 정리(Pixel Art Fixer) | **FAQ상 가능.** "You own your outputs and can use them in personal and commercial projects". 약관 전문은 페이지가 스크립트로 그려져 못 읽음 → 쓰기 전에 사람이 `retrodiffusion.ai/terms` 확인 | retrodiffusion.ai 첫 화면·Learn more FAQ (2026-11-17) |

## 도구별 내용

### ChatGPT (OpenAI)
- 이용약관은 입력은 이용자 소유이고, 결과물에 대한 OpenAI의 권리를 이용자에게 넘긴다고 적습니다. 약관을 지키는 한 판매·출판을 포함한 상업 목적에 쓸 수 있습니다.
- 약관에서 무료·유료 요금제에 따라 결과물 권리가 달라진다는 내용은 찾지 못했습니다.
- 제한: 결과물로 OpenAI와 경쟁하는 모델을 만들지 말 것, 사람이 만들지 않은 결과물을 사람이 만든 것처럼 표시하지 말 것.
- 다른 이용자에게도 같거나 비슷한 결과물이 나올 수 있고, 그런 결과물은 내 것으로 보지 않는다고 적혀 있습니다.
- 출처: https://openai.com/policies/terms-of-use

### Chickxel
- 저장소: https://github.com/1000Pro-1997/Chickxel
- README 기준: "소스는 공개하지만 오픈 소스는 아닌" 자체 라이선스. 공식 배포판은 상업 작업을 포함해 누구나 무료로 쓸 수 있고, Chickxel로 만든 그림·에셋은 만든 사람의 것이라고 적혀 있습니다.
- 앱 자체(원본·수정본)를 다시 배포하거나 파는 것은 허락 없이는 안 됩니다. 게임에 그림만 넣는 것과는 관계없습니다.
- v1.14.60까지의 옛 버전은 MIT 라이선스입니다.
- AI 채팅은 사용자가 설치·로그인한 Claude Code나 Codex CLI를 쓰고, 그때는 Anthropic·OpenAI 약관을 따릅니다.
- **남은 확인:** `LICENSE` 파일 전문(영어 원문이 기준)을 직접 읽지 못했습니다. 팔레트는 작성자별 저작권이므로 외부 팔레트를 받아 썼다면 그 조건도 확인합니다.

### WAI-Illustrious SDXL (로컬, ComfyUI·Draw Things)
- Hugging Face에 올라온 사본의 모델 카드는 라이선스를 Fair AI Public License 1.0-SD로 적고 있습니다. 바탕 모델은 Illustrious XL입니다.
- 이 라이선스는 모델로 만든 결과물을 적용 대상에서 빼고, 기여자가 결과물에 권리를 주장하지 않는다고 적습니다. 의무(공개, 같은 조건 유지)는 모델을 배포하거나 파생 모델을 만들 때 붙습니다.
- **남은 확인:** 맥에 설치한 정확한 버전의 Civitai 페이지에 적힌 조건이 위와 같은지 확인합니다. 함께 쓴 LoRA(도트 LoRA 3종 등)는 각각 조건이 다를 수 있습니다.
- 출처: https://freedevproject.org/faipl-1.0-sd/ , https://huggingface.co/John6666/wai-nsfw-illustrious-v10-sdxl (사본의 모델 카드)

### Scenario
- 무료 플랜 결과물은 개인·평가용이라 게임에 넣지 않습니다. 자세한 내용은 `scenario.md`.

### PixelLab
2026-11-17에 `https://www.pixellab.ai/termsofservice`(Last Updated 2026-10-30, 운영사 Pixel Heart AB, 스웨덴법)를 읽었습니다.
- **결과물 권리 (1.3, 3.3):** 만든 그림의 저작권은 이용자에게 있고, 상업·비상업 목적 모두 허락 없이 쓸 수 있습니다. 고치고 배포해도 됩니다. 약관에 체험·무료·유료 플랜을 나누는 조건은 없습니다.
- **금지 (1.2, 3.3):** PixelLab 결과물로 다른 모델을 학습시키는 것. (우리는 하지 않음)
- **API (2.2):** 프로그램으로 쓰는 것은 공식 API(`api.pixellab.ai`)로만. "바이브 코딩과 게임 에셋 제작" 용도라고 적혀 있어 `tools/pixellab/plmcp.py`의 쓰임과 맞습니다. 재판매·자체 서비스는 별도 문의.
- **Open RAIL-M (4.1):** 이 라이선스의 사용 제한(불법·유해 용도 금지 등)을 지켜야 합니다. 게임 그림에는 걸리는 것이 없습니다.
- **책임 (3.3):** 결과물이 남의 권리를 침해하지 않는지는 이용자 책임. 기존 게임·캐릭터를 닮게 하는 프롬프트는 쓰지 않습니다.
- **보관 (3.4):** PixelLab이 입력·결과 그림을 디버깅·개선용으로 저장할 수 있습니다(화면이나 문서에 표시). 공개되면 안 되는 그림은 올리지 않습니다.
- **약관 변경 (2.1, 2.3):** 기능·가격 제한은 바뀔 수 있고 미리 알린다고 되어 있습니다. 출시 전에 다시 확인합니다.

→ 이 확인으로 PixelLab 결과물도 채택하면 `assets/`에 넣을 수 있습니다. 넣을 때 아래 출처 표에 'PixelLab'과 생성 도구·날짜를 적습니다.

## 지금 `assets/`에 있는 그림의 출처

| 파일 | 만든 경로 | 조건 |
|---|---|---|
| `assets/backgrounds/archive.png` | Chickxel 한 장 도트 (ChatGPT 서고 시안 기준) | ChatGPT + Chickxel |
| `assets/tiles/archive*.png` (보류) | GPT 시트 기준 Chickxel 재제작 | ChatGPT + Chickxel |
| `assets/backgrounds/lab.png` | **임시본.** 사람이 대화에 올린 연구실 2차 시안(`design/refs/bg_lab_wide_v2_2026-11-10.png`)을 세션이 색만 64개로 줄임 (2026-11-10) | ChatGPT (사람 확인 2026-11-10) + 세션의 색 줄이기. 정식본은 Chickxel 정리 뒤 교체 |
| `assets/backgrounds/scriptorium.png` | **임시본.** ChatGPT 빈 방 시안(`design/refs/maps_2026-11/P-1_room_02.png`)을 세션이 64색 이하로 줄임(`tools/sprites/reduce_colors.py`, 2026-11-18) | ChatGPT + 세션의 색 줄이기 |
| `assets/props/{desk_book,wool_basket,hatch_closed,hatch_open,grate,fountain,funnel,pillar,ledge,ink_drop,spout}.png` | **임시본.** ChatGPT 사물 시트·꼭지(`refs/maps_2026-11/P-1_props_01.png`, `P-1_spout_01.png`)를 세션이 잘라 줄이고 32색 이하로(`tools/sprites/scriptorium_assets.py`, 2026-11-18) | ChatGPT + 세션 가공 |
| `assets/props/stream_{straight,left,right}.png` | **임시 그림.** 세션이 Pillow로 그린 물줄기 4프레임 (2026-11-18) | 직접 만든 것 → 정식 그림으로 바꿀 것 |
| `assets/sparks/{jjirit,lumi}_{idle,burst,card}.png` | PixelLab 채택 디자인(64×64)을 PixelLab `create_image_pixflux`로 32×32로 다시 그림(바탕 그림 = 64를 줄인 것, 보존 강도 320, 색은 원본에서; 2026-11-21, B안 `design/refs/pixellab_2026-11-21/spark_<id>_32_b.png`) → 세션이 시트로 조립: 대기·발동은 위아래 흔들림·밝기만 바꾼 **임시 움직임**, 도감은 16배 확대, 루미는 제 색 외곽선으로 조정 (`tools/sprites/spark_sheets.py`) 찌릿·루미는 2026-11-23에 PixelLab `create_character` v3(참고 그림 = 32×32 B안, 1종 생성 1회, character id는 `design/refs/pixellab_2026-11-22/spark_<id>_dirs/character_id.txt`)로 5방향을 받아 8방향 시트로 (`tools/pixellab/spark_dirs.py`, 원본 `design/refs/pixellab_2026-11-22/spark_<id>_dirs/`) | PixelLab + 세션 가공 |
| `assets/chars/luca_walk.png`, `luca_idle.png` | PixelLab 캐릭터 "Luca pro concept 72"(채택 디자인)의 템플릿 애니메이션 걷기 `walking-4-frames`·대기 `breathing-idle` 8방향(2026-11-21, 생성 16회) → 세션이 48×72 칸으로 잘라 조립, 잘못 나온 걷기 두 곳(아래 4번째 프레임, 아래오른쪽 줄)은 좌우 뒤집기로 메움 (`tools/sprites/luca_sheets.py`, 원본 `design/refs/pixellab_2026-11-21/luca_anim/`) | PixelLab + 세션 가공 |
| `assets/props/lab_arch_open.png` | **임시 그림.** 세션이 Pillow 도형으로 그린 '열린 통로' 덧그림 (2026-11-10) | 직접 만든 것 → 정식 그림으로 바꿀 것 |
| `assets/backgrounds/scriptorium.png` | **임시 그림.** `tools/placeholder/make_placeholders.py`가 그린 단색 도형 (2026-11-10) | 직접 만든 스크립트. 글자는 맑은 고딕으로 찍힘 → 정식 그림으로 바꿀 것 |
| `assets/props/*.png` (`lab_arch_open.png`을 뺀 22개) | **임시 그림.** 같은 스크립트 | 위와 같음 (글자 없음) |
| `assets/portraits/*.png` (루카·찌릿·이졸데 × 표정 4종) | **임시 그림.** 같은 스크립트 | 위와 같음 (맑은 고딕 글자 포함) |
| `assets/puzzles/D-*.png` (6장) | **임시 그림.** 같은 스크립트. `D-LIGHT-02`는 퍼즐 데이터의 `figure`로 그린 도면이라 배치가 문제의 일부 | 위와 같음 (맑은 고딕 글자 포함) |

Chickxel의 AI 채팅에 어느 제공자(Codex인지 Claude Code인지)를 연결해 만들었는지는 기록이 없습니다. 사람이 확인해 이 표에 적습니다.

## 출시 전에 따로 볼 것
- **Steam의 AI 사용 고지:** Steam은 게임을 등록할 때 AI로 만든 콘텐츠를 썼는지 적게 합니다. 등록 시점의 양식을 확인하고, 어떤 그림을 어떤 도구로 만들었는지 답할 수 있게 위 출처 표를 계속 채웁니다. (이번에 Steam 문서를 직접 확인하지는 않았습니다.)
- **저작권 보호 범위:** AI가 만든 그림은 나라에 따라 저작권 보호를 받지 못하거나 범위가 좁을 수 있습니다. 약관상 "써도 된다"와 "남이 베껴도 막을 수 있다"는 다른 문제입니다. 사람이 손본 과정(Chickxel 수정, 조립)을 기록해 두면 도움이 됩니다.
- 새 에셋을 추가할 때마다 출처 표에 한 줄을 더합니다.
- **글꼴:** 프로토타입은 글꼴 파일을 넣지 않고 시스템 한글 글꼴을 씁니다. 임시 그림 가운데 글자가 찍힌 것(배경·초상화·문 퍼즐 그림)은 Windows에 딸린 맑은 고딕으로 그려졌고, 그 글자가 들어간 그림을 출시작에 넣어도 되는지는 확인하지 않았습니다. 임시 그림은 출시 전에 모두 바꿉니다. 게임에 넣을 글꼴은 배포가 허용된 것으로 따로 정해야 합니다.
