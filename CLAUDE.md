# CLAUDE.md — 프로젝트 규칙

모든 세션은 작업 전에 이 파일과 `design/progress.md`를 먼저 읽는다. `progress.md` 맨 위 "▶ 여기서부터 이어서"에 다음 할 일과 사람이 정해야 할 것이 있다 — 세션을 끝낼 때 그 절을 최신으로 고친다.

## 핵심 원칙
- **기획은 파일로 남기고, 구현은 그 파일만 보고 한다.** 대화에서 정한 내용은 반드시 `design/` 또는 `data/`에 반영한다.
- 한 세션에 한 기능만 다룬다. 끝낼 때 `design/progress.md`에 한 일과 다음 할 일을 남긴다.
- 기획은 아직 초안이다. 기획서는 `design/plan.md`. 설정·이름이 바뀌면 `design/`부터 고치고, 코드와 데이터는 그 뒤에 맞춘다.
- 에셋이나 데이터를 고쳤으면 `tools/verify/asset_check.py`, `tools/verify/data_check.py`를 돌려 통과를 확인한다.
- **엔진은 Unity** (2026-11-16에 Godot에서 바꿈). 본 게임은 `unity/Assets/Scripts/Sparks/`(규칙 `Core` + 화면 `View`, 설명은 `design/unity.md`). **최종 디자인과 컨셉은 기획서 `design/plan.md`를 따른다.** `unity/`의 숲 게임(`SparkForest`)은 환경 시험용 프로토타입이다. Godot 프로토타입(`project.godot`, `scenes/`, `scripts/`)은 Unity판을 사람이 확인할 때까지 비교용으로 남겨 둔다.
- **데이터나 그림을 고치면 `python3 tools/unity/sync.py`로 Unity 쪽에 옮긴다.** 코드를 고치면 `bash tools/unity/check.sh`(자동 시험 + 컴파일 점검, Unity 없이 돈다)를 돌린다. 게임 규칙은 `Core`에 두고 `UnityEngine`을 쓰지 않는다 — 그래야 자동 시험이 돈다.

## 역할 분담
| 담당 | 맡는 일 | 산출물 |
|---|---|---|
| 기획 세션 | 기획, 지역 설계, 스토리·대사, 문 퍼즐 초안·교차 검토, 상호작용표, 과학 사실 확인 | `design/*.md`, `data/*.json` |
| 구현 세션 | 엔진 코드, 스파크 배치 시스템, 퍼즐 화면, 검증 스크립트, 버그 수정 | `unity/`, `tools/unity/`, `tools/verify/` (Godot판: `scripts/`, `scenes/`) |
| 사람 | 방향 결정, 필드 퍼즐 다듬기, 플레이 테스트, 이미지 제작·선별 | 테스트 메모, 에셋 |

## 과학 정확성
- 현상은 실제와 같게, 수치만 단순하게. 실제로 일어나지 않는 반응은 넣지 않는다.
- 양피지 지식은 만든 세션과 다른 세션에서 사실 확인을 거치고 출처를 함께 적는다.
- 중세 배경이지만 지식은 현대 기준으로 적는다.

## 데이터 규칙
- 스파크 상호작용은 물리 엔진이 아니라 `data/interactions.json` 규칙표로만 처리한다. 표에 없는 조합은 반응하지 않는다.
- 상호작용표와 발명품도 `status`를 쓴다. 상호작용은 `fact_check.checked`가 참이어야 `draft`를 벗어난다.
- 문 퍼즐은 `data/puzzles/<ID>.json` 하나에 하나. `status`: `draft → reviewed → verified → playtested`.
- `verification`이 채워지지 않은 퍼즐은 게임에 넣지 않는다.
- 모든 화면 문장은 키 기반 문자열 테이블로 분리한다 (한국어 → 일본어·영어).
- `unity/Assets/Resources/Sparks/`는 `sync.py`가 옮긴 결과라 손으로 고치지 않는다.
- **색과 분위기는 스테이지마다 다르다. 코드에 색을 적지 않는다.** (숲 프로토타입) 스테이지 테마는 `data/stages/<id>.json` 하나에 하나(항목은 `design/stages.md`). 고친 뒤 `python3 unity/ArtSource/tools/bake_all.py`로 다시 굽는다. `unity/Assets/Resources/SparkForest/stages/`는 구운 결과라 손으로 고치지 않는다.

## 이미지 생성
- 지금 쓰는 경로: 사람이 ChatGPT로 시안을 뽑고 → Chickxel로 도트 정리 → 세션이 규격 점검·조립·기록. 그림체·프롬프트·채택 기록은 `design/art-direction.md`.
- 새 시안이나 채택본이 생기면 프롬프트와 파일 위치를 `design/art-direction.md`에, `assets/`에 넣는 그림의 출처는 `design/licenses.md`에 기록한다.
- 도구별 상업 이용 조건은 `design/licenses.md`. 조건을 확인하지 않은 도구의 결과물은 레퍼런스로만 쓴다.
- Scenario MCP 커넥터(보조): 설정·한계는 `design/scenario.md`. 생성 전에 남은 CU와 예상 비용을 먼저 알린다 (무료 플랜 하루 50 CU). 무료 플랜 결과물은 레퍼런스 전용이고, 게임에 넣을 그림은 유료 플랜에서 다시 생성한다. 에셋 ID도 함께 기록한다.
- PixelLab(세션이 직접 생성, 2026-11-16 연동): `tools/pixellab/plmcp.py`. 생성 전에 남은 횟수와 예상 비용을 먼저 알린다. **상업 이용 조건을 확인해 `licenses.md`에 적기 전의 결과물은 레퍼런스 전용.** 토큰은 환경의 네트워크 비밀에만 두고 저장소·문서·채팅에 적지 않는다.
- 로컬 생성(ComfyUI·Draw Things)은 `design/local-generation.md`.
- **도트 그림체 기준:** 추구하는 도트 디자인과 '제 색 외곽선' 규칙은 `design/art-direction.md` "도트 그림체 기준". 스파크·소품 도트는 외곽선을 검정이나 한 가지 어두운 색으로 두르지 않고 몸 색을 짙게 한 색으로 그린다. Chickxel 프롬프트에는 `design/chickxel.md` "제 색 외곽선 공통 문구"를 넣고, 한 색 외곽선으로 나온 결과는 `tools/sprites/own_outline.py`로 고친다 (`asset_check.py`가 스파크 시트를 점검).
- 도트 스프라이트 편집·정리는 Chickxel(사람이 Mac에서 쓰는 앱). 사용법은 `design/chickxel.md`. 앱의 MCP는 앱 안의 AI 채팅에서만 쓸 수 있어 세션이 직접 부를 수 없고, 세션은 프롬프트 작성·규격 점검·기록을 맡는다.

## 에셋 규격
임시 그림도 같은 크기·같은 파일명으로 만들고, 나중에 덮어쓴다.
**필드 크기 확정 (2026-11-17, 사람):** 필사실 시안의 L안(가구가 루카 48×72에 맞는 크기). 맵은 화면보다 크게 만들고 카메라가 캐릭터를 따라 움직인다 — 맵 크기에 위 한도는 두지 않는다. 캐릭터는 작게 줄이지 않는다. 스파크는 32×32 (2026-11-21, 64×64를 게임에서 보니 루카만큼 커서 사람이 다시 정함). **시점은 정면 3/4 탑다운으로 확정 (2026-11-17)** — 벽이 화면과 나란하고 바닥 격자가 곧은 모습. 빛 퍼즐의 각도(입사각 = 반사각, 직각 거울)가 화면에서 그대로 보이기 때문. 마름모 바닥(쿼터뷰)은 쓰지 않는다. 프롬프트는 `design/map-concept-prompts.md`.

| 에셋 | 규격 | 예시 |
|---|---|---|
| 기준 해상도 | 1920×1080 (픽셀 아트면 640×360 확대) | — |
| 필드 배경 | 정면 3/4 탑다운. 화면(640×360, 3배 확대)보다 큰 한 장 도트 배경, 카메라가 캐릭터를 따라 스크롤. 크기는 맵마다 다르고 위 한도 없음(최소 640×360). 가구·소품은 루카 48×72에 맞는 크기(L안). 색 64개 이하·반투명 0. 캐릭터·스파크는 1배수 유지. 정면 3/4 탑다운(지금 서고·연구실 그림은 옛 쿼터뷰라 다시 그린다) | `assets/backgrounds/archive.png` |
| 타일 (보류) | 32×32 — 필드 맵에는 쓰지 않음, 소품 재료만 | `assets/tiles/archive.png` |
| 소품 (가안, 프로토타입에서 추가) | 가로·세로 32의 배수, 반투명 0. 크기는 미확정 | `assets/props/mirror_slash.png` |
| 필드 캐릭터 | 48×72(몸 높이 약 66, 3등신), 8방향 × 걷기 8프레임 (시트 384×576: 가로 8프레임, 세로 8방향, 위에서부터 아래·아래오른쪽·오른쪽·위오른쪽·위·위왼쪽·왼쪽·아래왼쪽) | `assets/chars/luca_walk.png` |
| 필드 캐릭터 대기 | 48×72, 8방향 × 대기 2프레임 (시트 96×576, 방향 순서는 걷기와 같음) | `assets/chars/luca_idle.png` |
| 스파크 (필드) | 32×32, 8방향(5방향을 그리고 왼쪽 셋은 뒤집기) × 대기 4 + 발동 6프레임 (시트 128×256, 192×256, 세로 8방향 — 순서는 필드 캐릭터와 같음). 방향 없는 한 줄 시트(128×32)도 엔진이 읽는다. 24×24 → 64×64(2026-11-17) → 32×32(2026-11-21) → 8방향(2026-11-22) | `assets/sparks/lumi_idle.png` |
| 스파크 (도감) | 512×512 투명 PNG | `assets/sparks/lumi_card.png` |
| 대화 초상화 | 512×512, 표정 4종 | `assets/portraits/luca_happy.png` |
| 문 퍼즐 그림 | 1200×800 PNG | `assets/puzzles/D-LIGHT-03.png` |
| 양피지 삽화 | 800×600, 글씨 없음 | `assets/parchment/refraction.png` |

필드 캐릭터는 2026-11-11에 32×48에서 48×72로 바뀌었다. 지금 `assets/chars/luca_*.png`는 새 규격이다(2026-11-21, PixelLab 걷기·대기를 `tools/sprites/luca_sheets.py`로 조립, 몸 높이 약 55. 걷기는 2026-11-24에 몸 전체가 움직이는 8프레임으로 — 엔진은 프레임 수를 시트 폭에서 읽는다). 스파크는 32×32이고, 찌릿·루미는 새 규격(채택 디자인을 PixelLab으로 32×32로 다시 그린 B안 + 임시 움직임, `tools/sprites/spark_sheets.py`). `asset_check.py`는 옛 규격 시트를 통과시키되 알린다. 엔진은 캐릭터·스파크 모두 칸 크기를 시트에서 읽는다(스파크는 시트 높이 = 칸 크기). 이 규격표는 Unity판에서도 그대로 쓴다 (그림은 1픽셀 = 1유닛, `.bytes`로 옮겨 Point 필터로 읽는다).
