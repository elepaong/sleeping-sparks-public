#!/usr/bin/env python3
"""지하 서고 배치 가이드 — 정면 3/4 탑다운 (2026-11-24).

필사실(scriptorium_blockout_34.py)과 같은 방식: 회색 상자 = 배경에 그릴 것, 색 점선 = 비워 둘 사물 자리,
노란 네모 = 한 화면(640×360), 갈색 인물 = 루카 실제 크기(48×72). 파란 격자 = 빛 퍼즐 칸(가로 8 × 세로 6, 한 칸 64×48 —
바닥 판석 한 칸과 같은 크기라서 판석 줄눈이 곧 칸 경계).
설계: design/background-prompts.md "지하 서고 정식 배경 — 정면 3/4". 칸 배치와 정답은 design/ch1-prototype.md 3절(바뀌지 않음).
그림 속 이름표는 영어(ChatGPT가 읽기 쉽고 기본 글꼴에 한글이 없다).
쓰는 법: python3 tools/layout/archive_blockout_34.py → design/refs/archive_blockout_34_2026-11-24.png
"""
from pathlib import Path

from PIL import Image, ImageDraw

import scriptorium_blockout_34 as S

ROOT = Path(__file__).resolve().parents[2]
W, H = 1672, 941
WALL_BOTTOM = 208
SIDE = 40
TILE = (64, 48)
GRID_ORIGIN = (544, 336)          # 빛 퍼즐 칸 [0,0]의 왼쪽 위 (맵 픽셀)
GRID_CELL = (64, 48)
GRID_SIZE = (8, 6)

FLOOR = (150, 146, 158)           # 서고: 차가운 보라 회색 (가이드 색일 뿐, 그림 색은 프롬프트에서)
WALL = (104, 100, 116)
CAP = (124, 120, 136)
GREY = (96, 94, 104)

# 배경에 그릴 것: (이름, x, y, w, h)
FIXED = [
    ("stair opening up", 96, 64, 160, 144),          # 필사실로 올라가는 계단 (벽 안쪽으로 오르는 돌계단)
    ("bookshelf", 300, 72, 176, 136), ("bookshelf", 1076, 72, 176, 136), ("bookshelf", 1296, 72, 176, 136),
    ("round seal door (closed)", 832, 64, 128, 144),  # 칸 열 5 위 — 문 아래 바닥에 빛 문양 L1
    ("tall mirror", 1500, 80, 80, 128),               # 구경거리 (전신 거울)
    ("old desk", 200, 420, 176, 96),                  # 구경거리 (낡은 책상)
    ("reading table", 1200, 700, 224, 96),
    ("scroll rack", 120, 700, 128, 112),
    ("book piles", 1420, 460, 128, 80),
    ("chest", 360, 760, 112, 72),
]

# 비워 둘 사물 자리: (이름, 파일, 칸 [열, 행] 또는 None, 맵 좌표(바닥 닿는 점) 또는 None, 그림 w, h, 색)
SLOTS = [
    ("S1 stand", "light_stand", (0, 3), None, 32, 48, (240, 200, 90)),
    ("S2 stand", "light_stand", (7, 2), None, 32, 48, (240, 200, 90)),
    ("M1", "mirror_*", (3, 3), None, 48, 64, (120, 200, 255)),
    ("M2", "mirror_*", (3, 1), None, 48, 64, (120, 200, 255)),
    ("M3", "mirror_*", (5, 1), None, 48, 64, (120, 200, 255)),
    ("M4", "mirror_*", (1, 2), None, 48, 64, (120, 200, 255)),
    ("M5", "mirror_*", (1, 4), None, 48, 64, (120, 200, 255)),
    ("L1 plate", "light_lock_*", (5, 0), None, 64, 48, (255, 140, 80)),
    ("L2 plate", "light_lock_*", (7, 4), None, 64, 48, (255, 140, 80)),
    ("door open", "seal_door_open", None, (896, 208), 128, 144, (200, 90, 200)),
]
START = (176, 300)                # 계단에서 내려온 자리 (from_scriptorium)


def cell_foot(c, r):
    """칸 [c, r]의 바닥 닿는 점(칸 가운데 아래쪽)."""
    x = GRID_ORIGIN[0] + (c + 0.5) * GRID_CELL[0]
    y = GRID_ORIGIN[1] + (r + 0.5) * GRID_CELL[1] + 8
    return x, y


def main():
    im = Image.new("RGB", (W, H), FLOOR)
    d = ImageDraw.Draw(im)
    for x in range(SIDE, W - SIDE, TILE[0]):
        d.line((x, WALL_BOTTOM, x, H), fill=(140, 136, 148))
    for y in range(WALL_BOTTOM, H, TILE[1]):
        d.line((SIDE, y, W - SIDE, y), fill=(140, 136, 148))
    d.rectangle((0, 0, W, 48), fill=CAP)
    d.rectangle((0, 48, W, WALL_BOTTOM), fill=WALL)
    d.rectangle((0, 0, SIDE, H), fill=CAP)
    d.rectangle((W - SIDE, 0, W, H), fill=CAP)
    # 빛 퍼즐 칸 (가이드에만, 배경에는 그리지 않음 — 바닥 판석 줄눈이 칸 경계)
    gx, gy = GRID_ORIGIN
    for c in range(GRID_SIZE[0] + 1):
        d.line((gx + c * GRID_CELL[0], gy, gx + c * GRID_CELL[0], gy + GRID_SIZE[1] * GRID_CELL[1]), fill=(60, 120, 230), width=2)
    for r in range(GRID_SIZE[1] + 1):
        d.line((gx, gy + r * GRID_CELL[1], gx + GRID_SIZE[0] * GRID_CELL[0], gy + r * GRID_CELL[1]), fill=(60, 120, 230), width=2)
    S.label(d, gx + GRID_SIZE[0] * GRID_CELL[0] / 2, gy + GRID_SIZE[1] * GRID_CELL[1] + 14, "light puzzle floor (keep clear, no furniture)")
    for name, x, y, w, h in FIXED:
        d.rectangle((x, y, x + w, y + h), fill=GREY, outline=(60, 58, 66), width=2)
        S.label(d, x + w / 2, y + h / 2, name)
    for name, _f, cell, at, w, h, c in SLOTS:
        if not cell:
            continue   # 문 열림 덧그림은 닫힌 문(배경) 자리 그대로라 가이드에는 그리지 않는다
        fx, fy = cell_foot(*cell)
        box = (fx - w / 2, fy - h, fx + w / 2, fy)
        S.dashed(d, box, c)
        S.label(d, fx, fy - h / 2, name)
    S.person(d, *START)
    S.label(d, START[0], START[1] + 12, "hero size")
    d.rectangle((GRID_ORIGIN[0] - 64, 300, GRID_ORIGIN[0] - 64 + 640, 660), outline=(240, 220, 60), width=3)
    S.label(d, GRID_ORIGIN[0] + 256, 312, "one screen 640x360")
    out = ROOT / "design/refs/archive_blockout_34_2026-11-24.png"
    im.save(out)
    print(out.relative_to(ROOT))
    for name, _f, cell, at, *_ in SLOTS:
        print(name, cell, cell_foot(*cell) if cell else at)


if __name__ == "__main__":
    main()
