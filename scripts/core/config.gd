extends RefCounted
## 한 곳에 모아 둔 조정값. 내용(문제·대사·배치)은 여기가 아니라 data/에 있다.
## 쓰는 법: const Config = preload("res://scripts/core/config.gd")

# 캐릭터·스파크 배율: 원본 1배수 유지로 확정 (2026-11-10, design/progress.md 로그).
# 웅장한 배경 속에서 캐릭터가 작아 보이게 하려는 연출 의도. 정수가 아닌 배율은 쓰지 않는다.
const CHAR_SCALE := 1.0
const SPARK_SCALE := 1.0

const START_ZONE := "scriptorium"
const START_INK := 2

const WALK_SPEED := 80.0          # 맵 픽셀/초 (화면에는 640×360이 보인다)
const REACH := 46.0               # 이 거리 안의 사물을 조사·배치할 수 있다
const NAV_CELL := 8               # 클릭 이동 길찾기 칸 크기
const CAMERA_LIFT := 24.0         # 카메라가 루카 발끝보다 이만큼 위(몸 가운데)를 본다

# 문 퍼즐 (plan.md 5장 '힌트와 보상')
const HINT_COSTS := [1, 1, 1, 2]  # 1·2·3단계는 잉크 1개, 특별 힌트는 2개
const WRONG_PENALTY := 0.1        # 오답 1회마다 10%
const MIN_ENERGY_RATIO := 0.5     # 최소 50%

# 바닥에 붙어 보이게: 발밑 그림자, 지역 조명 색 (design/ch1-prototype.md 8절 '그림자와 조명 색')
const SHADOW_COLOR := Color(0.05, 0.03, 0.1, 0.5)
const CHAR_SHADOW := Vector2(9, 4)     # 루카 발밑 그림자 타원의 반지름
const SPARK_SHADOW := Vector2(5, 2)    # 스파크는 떠 있으므로 작고 옅게
const SPARK_SHADOW_ALPHA := 0.6        # SHADOW_COLOR의 투명도에 곱한다
const SPARK_GLOW_RADIUS := Vector2(26, 11)   # 스파크가 바닥에 떨구는 빛 (sparks.json의 "glow" 색이 있는 스파크만)
const SPARK_AMBIENT := 0.35            # 스파크는 스스로 빛나므로 조명 색을 이만큼만 받는다 (0 = 안 받음, 1 = 다 받음)

const SHOW_LABELS := true         # 임시 그림 위에 사물 이름표를 띄운다
const BEAM_HEIGHT := 10.0         # 빛줄기를 바닥에서 띄워 그리는 높이
