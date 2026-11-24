# 스테이지 테마 — 색과 분위기를 스테이지마다 다르게

2026-11-16 사람 결정: **색이나 분위기는 스테이지마다 다르게 할 예정이라 고정하지 않는다.**
그래서 Unity판(`unity/`)의 색과 분위기는 코드에 적지 않고, 스테이지마다 테마 파일 하나에서 읽는다.

- 테마 파일: `data/stages/<id>.json` (하나에 한 스테이지), 순서와 시작 스테이지는 `data/stages/index.json`
- 굽기: `python3 unity/ArtSource/tools/bake_all.py` → `unity/Assets/Resources/SparkForest/stages/<id>/`
- 확인용 그림: `unity/ArtSource/preview/<id>/map_full.png`, `shot_A.png` (실행 중 물들임·안개를 흉내 낸 것)

## 변하는 것과 변하지 않는 것
| 구분 | 내용 |
|---|---|
| 스테이지마다 변함 | 지면 색(풀·숲 바닥·길·물가·물), 나무·덤불·꽃·버섯·바위 색, 구역별 색 기울임, 나무 종류 섞임, 시간대 물들임, 안개, 빛기둥 색·세기·기울기, 떠다니는 가루(반딧불) 색·수, 등불 빛, 나비 수, 떨어지는 잎, 등불이 처음부터 켜져 있는지 |
| 늘 같음 | **제 색 외곽선 규칙**(검정·한 가지 어두운 색 외곽선 금지 — 외곽선은 맞닿은 면 색을 짙게 한 색이라 팔레트를 바꿔도 따라 바뀐다), 루미·동료·스파크·오두막·소품·UI 색, 16px 타일 탑다운 3/4 뷰 |
| 지금은 같지만 나중에 스테이지마다 달라질 것 | 맵 배치(`layout`). 지금은 숲 맵 하나(`forest`, `gen_map.py` 안에 적힘)뿐이다 |

테마가 달라도 **배치·충돌·스파크 수는 같다**(생성기가 난수를 같은 횟수만 쓴다). 2026-11-16 확인: 세 스테이지의 `map.json`이 사물·충돌까지 같음.

## 두 단계로 적용된다
1. **굽는 색** (`ramps`, `ground`, `areas.*.tint`, `areas.*.trees`) — 그림 자체를 그 색으로 다시 그린다. 도트 색 수가 늘지 않고 외곽선도 제 색을 따른다.
2. **실행 중 분위기** (`mood`, `areas.*.mood`) — 엔진이 그림 위에 입힌다. 구역에 들어가면 약 1~2초에 걸쳐 부드럽게 바뀐다.

## 테마 파일 항목
`base`를 적으면 그 스테이지를 물려받고 바꿀 값만 적는다. `spring_day.json`이 모든 항목을 적은 기준 파일이다.
색은 `#RRGGBB`, 안개만 `#RRGGBBAA`(끝 두 자리가 짙기). 색 묶음은 어두운 색 → 밝은 색 순서.

| 항목 | 뜻 |
|---|---|
| `id`, `name`, `status` | 파일명과 같은 id, 화면에 뜨는 이름, 진행 상태(`draft` → …) |
| `layout` | 맵 배치 이름. 지금은 `forest` 하나 |
| `ramps` | 풍경 스프라이트의 색 묶음(4색) 바꾸기. 이름은 자리표다 — `mint`·`sage`·`teal`(나무·덤불), `moss`(풀·이끼), `pink`·`gold`·`lilac`·`red`·`blue`(꽃·꽃나무·버섯), `stone`, `bark` 등. 인물·스파크·오두막·UI에는 적용되지 않는다 |
| `ground.grass` (5), `edge` (3), `forest` (3), `clover` (2) | 풀밭, 나무 그늘 가장자리, 깊은 숲 바닥, 클로버 |
| `ground.path` (3), `path_rim`, `path_specks` (2) | 흙길, 길 테두리, 길 위 점 |
| `ground.shore` (2), `water` (4), `water_foam`, `water_edge`, `water_ripple`, `water_shade` | 물가와 물. `water_shade`는 나무 밑 물에 곱하는 값 |
| `ground.dapple` | 나뭇잎 사이로 떨어진 빛 얼룩의 색과 세기(큰 얼룩, 가운데 더 밝은 얼룩) |
| `ground.tuft` (2), `flower_center`, `flower_ramps`, `pebble` (3), `floor_fern` (2), `shadow` | 풀포기, 들꽃, 자갈, 숲 바닥 고사리, 그림자에 곱하는 값 |
| `trees_base` | 숲 전체에 깔리는 나무 색 자리표 |
| `areas.<A~E>.name` | 구역 이름 |
| `areas.<A~E>.tint` | 그 구역 지면에 섞는 색과 세기. `null`이면 없음 |
| `areas.<A~E>.trees`, `special` | 그 구역 둘레에 섞이는 나무 색 자리표, 특별한 나무(버섯 숲의 거대 버섯) |
| `areas.<A~E>.mood` | 그 구역에서만 바꿀 분위기 값 |
| `mood.bg` | 맵 바깥 색 |
| `mood.tint` | 세상 전체에 곱하는 색(시간대). 흰색이면 그대로. 스파크·동료는 35%, 루미는 70%, 켜진 등불·꽃등은 30%만 받는다 |
| `mood.haze` | 세상 위에 덮는 얇은 막(안개·노을·밤). 등불 빛과 빛기둥은 이 막 위에 그려져 밤에 더 또렷하다 |
| `mood.shaftColor`, `shaftAlpha`, `shaftAngle` | 빛기둥 색·세기·기울기(도). 기울기는 스테이지 값만 쓴다 |
| `mood.moteColor`, `moteCount` | 떠다니는 가루 색·수. 밤에는 반딧불로 쓴다. 수는 스테이지 값만 쓴다 |
| `mood.glowColor`, `glowAlpha` | 등불 둘레 빛 |
| `mood.butterflies` | 나비 수(스테이지 값) |
| `mood.fall`, `fallMin`, `fallMax` | 위에서 떨어지는 잎의 색 자리표(`pink`·`gold`·`mint`)와 간격(초). 빈 목록이면 떨어지지 않는다 |
| `mood.lanternsLit` | 꺼진 등불을 처음부터 켜 둘지(스테이지 값) |

## 예시 스테이지 (모두 `draft`, 같은 맵)
| id | 굽는 색 | 실행 중 분위기 |
|---|---|---|
| `spring_day` 봄 한낮의 숲 | 첨부본과 픽셀 단위로 같다 | 물들임 없음, 따뜻한 빛기둥, 꽃잎 |
| `autumn_dusk` 가을 해질녘의 숲 | 단풍(주황·올리브·적갈색) 나무, 누런 풀밭 | 노을빛 물들임과 막, 많이 기운 호박색 빛기둥, 낙엽이 자주 떨어짐, 등불 켜짐 |
| `moon_night` 달밤의 숲 | 청록·남색 나무, 푸른 풀밭, 회색 길 | 어두운 남보라 물들임, 옅은 달빛 기둥, 연두색 반딧불 60, 나비 없음, 등불 켜짐. 오두막 빈터와 큰나무 숲터는 조금 더 밝다 |

## 새 스테이지 만드는 순서
1. `data/stages/<id>.json`을 만든다 (`"base": "spring_day"` + 바꿀 값).
2. `data/stages/index.json`의 `stages`에 넣는다.
3. `python3 unity/ArtSource/tools/bake_all.py <id>` → 미리보기 그림을 본다. (index 반영은 인자 없이 한 번 더 돌린다)
4. `python3 tools/verify/data_check.py`.
5. 게임에서 F2로 스테이지를 넘기며 본다.

## 아직 없는 것
- 스테이지마다 다른 **맵 배치**. `layout`이 `forest` 하나다. 다른 배치를 넣으려면 `gen_map.py`의 배치 부분을 배치 파일로 떼어 내야 한다.
- 스테이지 사이 **진행 이어가기**. 지금 F2는 미리보기용이라 스테이지를 바꾸면 처음부터 시작한다.
- 풍경이 아닌 것(오두막 지붕, 소품)의 스테이지별 색. 필요하면 `gen_map.py`의 `SCENERY` 목록에 이름을 더한다.
