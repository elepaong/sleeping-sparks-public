# Unity판

2026-11-16 사람 결정: **개발 환경을 Godot에서 Unity로 바꾼다.** 추구미는 '숲속의 작은 마녀' 같은 동화풍·힐링 도트.
다른 세션이 만든 Unity 프로젝트(`SleepingSparkForest_Unity_2.zip`, 환경 시험용 숲 게임)를 `unity/`로 들여왔고, 그 옆에 기획서를 따르는 본 게임을 옮겼다.

## 조건 (사람이 준 것)
- 2D 탑다운(또는 아이소메트릭) 픽셀 아트, 동화풍·판타지·힐링.
- **외곽선(가장 중요):** 색을 맞춘 자연스러운 외곽선(선택적 외곽선). 거친 검정·한 가지 어두운 갈색 외곽선 금지. → `art-direction.md` "도트 그림체 기준"의 '제 색 외곽선'과 같은 규칙.
- 촘촘한 디테일. 맵 크기와 분위기는 이 조건에 맞추고, 기존 `assets/`는 참고용.
- 색과 분위기는 스테이지마다 다르다 → [`stages.md`](stages.md). ("나뭇잎 사이로 드는 따뜻한 낮 햇살"은 `spring_day` 한 스테이지의 분위기로 남았다.)

## 본 게임: 프롤로그 + 1장 (2026-11-16에 Godot판을 옮김)
설계는 [`ch1-prototype.md`](ch1-prototype.md) 그대로다. 내용(문제·대사·배치·반응)은 전부 `data/`에 있고 코드에는 없다.

### 구조 — 규칙과 화면을 나눴다
| 층 | 위치 | 하는 일 | Unity 필요 |
|---|---|---|---|
| 규칙 (Core) | `unity/Assets/Scripts/Sparks/Core/` | 데이터 읽기(`GameData`, `Json`), 문자열(`Loc`), 진행 상태(`State`), 행동 목록 실행·대화·문 퍼즐·제작·오메가 문서(`Game`), 필드 규칙(`Zone`, `FieldObject`), 빛줄기(`Beam`), 길찾기(`Nav`), 조정값(`Config`) | 아니오 — `UnityEngine`을 쓰지 않는다 |
| 화면 (View) | `unity/Assets/Scripts/Sparks/View/` | 조립·카메라·입력 전달·그리기(`GameView`), 글과 창(`Ui`, IMGUI), 그림 불러오기(`Art`), 입력(`Keys`), 띄울 게임 고르기(`Launch`) | 예 |

규칙 쪽이 Unity와 무관해서 **Unity 없이 컴파일하고 처음부터 끝까지 자동 시험을 돌릴 수 있다.** 화면은 매 프레임 규칙의 상태를 읽어 그리기만 한다.
Godot판과의 대응: `data.gd`·`loc.gd`·`state.gd`·`beam.gd` → 같은 이름의 Core 파일, `zone_view.gd` → `Zone`(규칙) + `GameView`(그리기), `main.gd` → `Game`, `ui/*.gd` → `Game`(상태) + `Ui`(그리기), `test/playthrough.gd` → `tools/unity/Playthrough.cs`.

### 데이터와 그림이 Unity로 가는 길
원본은 저장소 루트의 `data/`와 `assets/`다. `python3 tools/unity/sync.py`가 이를 `unity/Assets/Resources/Sparks/`로 옮긴다(JSON은 그대로, PNG는 임포트 설정의 영향을 받지 않게 `.bytes`로, 목록과 그림 크기는 `index.json`). **`Resources/Sparks/`는 옮긴 결과라 손으로 고치지 않는다.** 데이터나 그림을 고치면 sync를 다시 돌린다.

### 점검 (Unity 없이)
`bash tools/unity/check.sh` (mono의 `mcs` 필요: `apt-get install mono-mcs mono-runtime`)
1. Core만 컴파일해 `tools/unity/Playthrough.cs`를 실행 → `PLAYTHROUGH OK`. 실제 게임과 같은 길(길찾기로 걸어가 조사·배치, 문 퍼즐 화면의 함수)로 프롤로그부터 끝 화면까지 밟고, 큰 맵 카메라와 '처음부터 다시'까지 본다.
2. 화면 코드와 숲 프로토타입까지 Unity API 껍데기(`tools/unity/stubs/UnityEngine.cs`)에 대고 컴파일 → `COMPILE OK`. 옛 입력 방식과 새 Input System 양쪽으로. **문법·형식 오류만 잡는다 — 그려지는 모습과 실제 Unity API와의 차이는 Unity에서 봐야 한다.**

### 화면 확인 (Unity 없이, 2026-11-18)
`bash tools/unity/snapshot.sh` → `design/refs/screens/<장면>.png` (1920×1080). `tools/unity/Snapshot.cs`가 Core로 장면을 만들고(시작, 수반 퍼즐, 열린 계단, 창가), `tools/unity/snapshot_render.py`가 `GameView`와 같은 규칙(카메라, 사물 자리 `SpriteRect`, 발치 y 순서, 납작한 사물, 그림자, 스파크 띄우기, 햇빛·먼지, 이름표)으로 그린다. 필요: mono, Pillow, 한글 글꼴(`apt-get install fonts-nanum`). **실제 Unity 화면이 아니다** — 글꼴, 먼지 자리, HUD·스파크 칸·대화 창은 다르거나 없다. 그림·배치 확인용.

### 지역 데이터에 추가된 항목 (2026-11-18)
- 사물 `"frames"`, `"fps"`: 그림이 가로로 프레임을 이은 시트면 그 수와 빠르기. 상태마다 그림이 달라도 프레임 수는 같아야 한다 (예: 필사실 물줄기 160×96 × 4).
- 사물 `"place_offset"`: 스파크를 놓았을 때 설 자리를 위치에서 옮긴다 (벽 선반처럼 그림이 높이 그려진 사물).
- 지역 `"sunlight"`: `color`, `alpha`, `rects`(바닥 햇빛 자락 자리), `sprite`(창 모양 흰 그림 — 있으면 각 자리 왼쪽 위에 그 모양으로, 없으면 네모), `dust`(먼지 수), `dust_color`, `dust_height`. 햇빛은 배경 바로 위·사물 아래, 먼지는 맨 위.

### 스파크 방향 (2026-11-22)
- Core `Zone.followerDir`: 스파크가 보는 방향(`DirNames` 순서). 움직이는 중이면 가는 쪽, 사물에 놓여 있으면 아래, 멈춰 따라다니면 루카와 같은 쪽. 자동 시험이 확인한다.
- View: 스파크 시트가 세로 8줄이면 그 방향의 줄, 1줄이면 그 줄(방향 없음). 칸 크기 = 가로 ÷ 프레임 수(대기 4, 발동 6).
- 그림: `tools/pixellab/spark_dirs.py`(PixelLab v3로 5방향) → `tools/sprites/spark_sheets.py`(왼쪽 셋은 뒤집어 8줄 시트).

### Godot판과 다른 점 (화면 쪽)
- 화면 늘리기: 640×360을 창 가운데에 정수 배로 놓고 남는 자리는 검게 둔다. 좌표는 1픽셀 = 1유닛, 맵의 y(아래가 +)는 Unity에서 -y.
- 스파크 바닥 빛: Godot은 어두운 색을 더하는 섞기였다. Unity 기본 스프라이트 셰이더에는 더하기 섞기가 없어, 같은 빛깔을 밝게 해 옅게 덮는다. 느낌이 다르면 값을 고친다(`GameView.MakeSpark`).
- UI는 IMGUI로 다시 그렸다(자리와 크기는 Godot판과 같은 640×360 기준). 글꼴은 시스템 한글 글꼴. 오메가 문서의 스파크 도감은 줄마다 도감 그림 + 이름.
- 지역 조명 색(`ambient`)은 그대로 적용. 숲 프로토타입의 스테이지 분위기(안개 막, 떠다니는 가루, 구역별 전환 — [`stages.md`](stages.md))는 **아직 본 게임에 넣지 않았다.**

## 숲 프로토타입 (환경 시험용)
`unity/Assets/Resources/boot.json`의 `"game"`을 `"forest"`로 바꾸면 본 게임 대신 숲 프로토타입이 뜬다(기본은 `"sparks"` = 본 게임).

## 폴더
| 위치 | 내용 |
|---|---|
| `unity/Assets/Scripts/Sparks/` | **본 게임** (Core, View) |
| `unity/Assets/Resources/Sparks/` | 본 게임의 데이터·그림 (sync 결과, 손으로 고치지 않음) |
| `unity/Assets/Resources/boot.json` | 띄울 게임 (`sparks` / `forest`) |
| `tools/unity/` | `sync.py`, `check.sh`, `Playthrough.cs`, `stubs/` |
| `unity/Assets/Scripts/SparkForest/` | 숲 프로토타입 C# 12개 |
| `unity/Assets/Resources/SparkForest/stages/<id>/` | 숲 프로토타입의 스테이지별 구운 결과 (손으로 고치지 않음) |
| `unity/ArtSource/` | 숲 프로토타입의 그림·맵 생성기와 확인용 그림 |
| `data/stages/` | 숲 프로토타입의 스테이지 테마(원본) |

## 실행
Unity Hub에서 2D 템플릿으로 새 프로젝트(2021.3 LTS 이상)를 만들고 `unity/Assets/` 안의 `Resources`, `Scripts`를 그 프로젝트의 `Assets/`로 복사한 뒤 아무 씬에서 Play. 씬 설정은 필요 없다(실행할 때 스스로 조립). 본 게임의 조작·정답·확인 순서는 [`ch1-prototype.md`](ch1-prototype.md) 8·10절(F1 = 이동 영역·칸 보기). 숲 프로토타입의 조작은 `unity/README.md`.

## 이 프로젝트의 위치 (확정, 2026-11-16)
사람 결정: **`unity/`의 숲 게임은 Unity 환경을 시험하기 위한 프로토타입이다. 최종 디자인과 컨셉은 기획서(`plan.md`)를 따른다.**

- 기준은 `plan.md`: 수도원, 견습 필경사 루카, 봉인의 문 퍼즐(과학 원리), 스파크 배치 필드 퍼즐, 허브 1 + 지역 7.
- 숲 게임의 인물(루미·모찌·동료 3)·이야기·맵·수집 루프는 **최종 게임에 넣지 않는다.** 여기에 기획을 더 쌓지 않는다.
- 숲 프로토타입에서 가져갈 것은 기술과 그림 규칙이다: 실행 중 조립 방식, 스테이지 테마(`stages.md` — 지역마다 다른 색·분위기), 제 색 외곽선 생성 방식, 입력·카메라·Y 정렬.
- `plan.md`의 프롤로그+1장은 Unity로 옮겼다(위 '본 게임'). Godot 프로토타입은 Unity판을 사람이 직접 확인할 때까지 비교용으로 남겨 둔다.
