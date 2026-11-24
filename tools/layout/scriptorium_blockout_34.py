#!/usr/bin/env python3
"""필사실 배치 가이드 — 정면 3/4 탑다운 (2026-11-17).

ChatGPT에 구도 참고로 붙이고, 배경이 오면 좌표를 맞출 때 기준으로 쓴다.
설계: design/background-prompts.md "필사실 정식 배경 — 정면 3/4, 층 나누기".
- 회색 상자: 배경 그림에 그릴 것 (벽·창·문·고정 가구)
- 점선 빈 칸(색): 배경에는 그리지 않고 비워 둘 자리 — 엔진이 따로 얹는 사물
- 갈색 인물: 루카 실제 크기(48×72 칸, 몸 높이 66)
그림 속 이름표는 영어(기본 글꼴에 한글이 없고, ChatGPT도 영어를 잘 읽는다). 좌표는 맵 픽셀(1672×941). 사물 자리 표(SLOTS)가 data/zones/scriptorium.json 좌표의 기준이다.
쓰는 법: python3 tools/layout/scriptorium_blockout_34.py → design/refs/scriptorium_blockout_34_2026-11-18.png
배경 채택(2026-11-18, P-1_room_02.png) 뒤로는 SLOTS가 사물 좌표의 기준이고, 회색 상자(FIXED)는 처음 가이드용 기록이다.
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
W, H = 1672, 941
WALL_TOP, WALL_BOTTOM = 0, 208   # 뒷벽: 윗단 + 벽 정면(높이 160). 바닥은 208부터
SIDE = 40                        # 좌우 벽 두께(위에서 내려다본 벽 윗면)
TILE = (64, 48)                  # 바닥 판석 한 칸 (정면 3/4라 세로가 줄어 보임)

FLOOR = (176, 168, 152)
WALL = (128, 120, 112)
CAP = (150, 142, 132)
GREY = (120, 116, 112)

# 배경에 그릴 고정 가구: (이름, x, y, w, h) — x,y는 왼쪽 위, h는 화면에서 보이는 높이
FIXED = [
    ("window", 150, 40, 96, 136), ("window", 330, 40, 96, 136), ("window", 510, 40, 96, 136), ("window", 690, 40, 96, 136),
    ("copy desk", 140, 224, 112, 72), ("copy desk", 320, 224, 112, 72), ("copy desk", 500, 224, 112, 72), ("copy desk", 680, 224, 112, 72),
    ("door (closed)", 940, 96, 144, 112),
    ("book cupboard", 1120, 72, 160, 136),
    ("shelf", 1296, 88, 96, 120),
    ("lectern", 300, 470, 48, 80),
    ("drying rack", 1000, 330, 160, 96),
    ("reading table", 960, 560, 192, 96),
    ("binding table", 220, 600, 160, 88),
    ("books", 1220, 260, 64, 48), ("chest", 1300, 600, 96, 64), ("scroll tub", 120, 420, 64, 64),
]

# 비워 둘 자리 = 엔진이 얹는 사물: (id, 파일, 바닥 닿는 점 x, y, 그림 w, h, 색)
SLOTS = [  # 2026-11-18: 채택한 빈 방(P-1_room_02.png)에 맞춰 옮긴 자리. 처음 가이드(11-17)와 다르다
    ("1 hero desk + book", "desk_book", 566, 735, 128, 96, (230, 120, 60)),
    ("wool", "wool_basket", 650, 742, 32, 32, (240, 200, 90)),
    ("2 floor slab", "hatch_*", 836, 860, 192, 128, (90, 160, 220)),
    ("3 grate", "grate", 220, 820, 64, 64, (90, 200, 120)),
    ("4 grate", "grate", 1476, 820, 64, 64, (90, 200, 120)),
    # 수반 쪽 (2026-11-18 다시 잡음): 벽 꼭지에서 물줄기가 떨어져 받침 수반에 담긴다.
    # 받침돌 둘은 물줄기 양옆 벽에 튀어나온 돌 선반, 깔때기는 왼쪽 받침돌 아래 바닥
    ("spout", "spout", 1550, 112, 32, 32, (200, 90, 200)),
    ("D basin", "fountain", 1550, 262, 96, 96, (200, 90, 200)),
    ("water", "stream_*", 1550, 200, 64, 96, (120, 170, 255)),
    ("E funnel", "funnel", 1478, 262, 32, 64, (220, 180, 60)),
    ("ledge", "ledge", 1498, 150, 32, 32, (200, 200, 200)),
    ("ledge", "ledge", 1602, 150, 32, 32, (200, 200, 200)),
    ("pillar", "pillar", 1050, 740, 64, 192, (160, 110, 200)),
    ("pillar", "pillar", 680, 936, 64, 192, (160, 110, 200)),
    ("pillar", "pillar", 1250, 936, 64, 192, (160, 110, 200)),
]
START = (640, 800)


def label(d, x, y, text):
    w = d.textlength(text)
    d.rectangle((x - w / 2 - 2, y - 7, x + w / 2 + 2, y + 6), fill=(255, 255, 255))
    d.text((x - w / 2, y - 6), text, fill=(20, 20, 20))


def dashed(d, box, color):
    x0, y0, x1, y1 = box
    for x in range(int(x0), int(x1), 8):
        d.line((x, y0, min(x + 4, x1), y0), fill=color, width=2)
        d.line((x, y1, min(x + 4, x1), y1), fill=color, width=2)
    for y in range(int(y0), int(y1), 8):
        d.line((x0, y, x0, min(y + 4, y1)), fill=color, width=2)
        d.line((x1, y, x1, min(y + 4, y1)), fill=color, width=2)


def person(d, fx, fy):
    """발끝(fx, fy) 기준 48×72 칸, 몸 높이 66."""
    top = fy - 66
    d.ellipse((fx - 12, top, fx + 12, top + 24), fill=(97, 70, 65))
    d.rectangle((fx - 16, top + 22, fx + 16, fy), fill=(57, 38, 35))


def main():
    im = Image.new("RGB", (W, H), FLOOR)
    d = ImageDraw.Draw(im)
    # 바닥 격자 (곧은 칸)
    for x in range(SIDE, W - SIDE, TILE[0]):
        d.line((x, WALL_BOTTOM, x, H), fill=(160, 152, 138))
    for y in range(WALL_BOTTOM, H, TILE[1]):
        d.line((SIDE, y, W - SIDE, y), fill=(160, 152, 138))
    # 뒷벽과 좌우 벽
    d.rectangle((0, WALL_TOP, W, 48), fill=CAP)
    d.rectangle((0, 48, W, WALL_BOTTOM), fill=WALL)
    d.rectangle((0, 0, SIDE, H), fill=CAP)
    d.rectangle((W - SIDE, 0, W, H), fill=CAP)
    # 창에서 떨어지는 햇빛 자리 (엔진이 얹음 — 점선만)
    for x in (150, 330, 510, 690):
        dashed(d, (x + 40, 320, x + 150, 420), (250, 240, 200))
    label(d, 470, 440, "sunlight (engine)")
    for name, x, y, w, h in FIXED:
        d.rectangle((x, y, x + w, y + h), fill=GREY, outline=(70, 68, 66), width=2)
        label(d, x + w / 2, y + h / 2, name)
    for name, _f, fx, fy, w, h, c in SLOTS:
        box = (fx - w / 2, fy - h, fx + w / 2, fy)
        d.rectangle(box, fill=FLOOR)
        dashed(d, box, c)
        label(d, fx, fy - h / 2, name)
    person(d, *START)
    label(d, START[0], START[1] + 12, "hero size")
    # 한 화면(640×360) 크기 비교
    d.rectangle((40, 581, 680, 941), outline=(240, 220, 60), width=3)
    label(d, 360, 925, "one screen 640x360")
    out = ROOT / "design/refs/scriptorium_blockout_34_2026-11-18.png"  # 11-17판은 ChatGPT에 준 처음 가이드
    im.save(out)
    print(out.relative_to(ROOT))


if __name__ == "__main__":
    main()
