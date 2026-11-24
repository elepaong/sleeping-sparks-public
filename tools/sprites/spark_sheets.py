#!/usr/bin/env python3
"""채택한 스파크 디자인(32×32)으로 필드 시트를 만든다 — 임시 움직임 (2026-11-21, 방향 2026-11-22).

그림: 64×64 채택 디자인을 PixelLab pixflux로 32×32로 다시 그린 B안(보존 강도 320) — design/refs/pixellab_2026-11-21/spark_<id>_32_b.png
방향: design/refs/pixellab_2026-11-22/spark_<id>_dirs/ 에 5방향(south, south-east, east, north-east, north)이 있으면
      8방향 시트(세로 8줄, 순서는 루카와 같음: 아래·아래오른쪽·오른쪽·위오른쪽·위·위왼쪽·왼쪽·아래왼쪽,
      왼쪽 셋은 오른쪽 셋을 좌우 뒤집기)를 만든다. 없으면 한 줄(방향 없음). 방향은 tools/pixellab/spark_dirs.py로 뽑는다.

대기 4프레임(가로 128): 위아래로 0·1·1·0px 떠오름.
발동 6프레임(가로 192): 떠오르며 밝아졌다가 돌아옴.
도감 512×512: 아래 방향 그림을 16배 (Point 확대).
정식 애니메이션(PixelLab·Chickxel)이 오면 같은 파일명으로 덮어쓴다. 디자인 출처: design/art-direction.md "채택 (2026-11-16)".
쓰는 법: python3 tools/sprites/spark_sheets.py
외곽선이 한 색인 스파크(OUTLINE)는 만든 뒤 tools/sprites/own_outline.py로 제 색 외곽선으로 고친다(이 스크립트가 부른다).
"""
import pathlib
import subprocess
import sys

from PIL import Image, ImageEnhance

ROOT = pathlib.Path(__file__).resolve().parents[2]
SRC = ROOT / "design/refs/pixellab_2026-11-21"
DIRS = ROOT / "design/refs/pixellab_2026-11-22"
PICKED = ["jjirit", "lumi"]   # 지금 게임에 나오는 스파크만
OUTLINE = {"lumi": "542225"}  # 한 색 외곽선 → own_outline.py
IDLE = [0, 1, 1, 0]
BURST = [(0, 1.0), (1, 1.12), (2, 1.25), (2, 1.25), (1, 1.12), (0, 1.0)]
FIVE = ["south", "south-east", "east", "north-east", "north"]
# 시트 줄 순서 → (그림 방향, 뒤집기)
ROWS = [("south", False), ("south-east", False), ("east", False), ("north-east", False), ("north", False),
        ("north-east", True), ("east", True), ("south-east", True)]


def solid(im):
    im = im.convert("RGBA")
    im.putalpha(im.getchannel("A").point(lambda v: 255 if v > 100 else 0))
    return im


def frame(src, lift, bright=1.0):
    im = src
    if bright != 1.0:
        a = src.getchannel("A")
        im = ImageEnhance.Brightness(src.convert("RGB")).enhance(bright).convert("RGBA")
        im.putalpha(a)
    out = Image.new("RGBA", src.size, (0, 0, 0, 0))
    out.alpha_composite(im, (0, -lift))
    return out


def views(sid):
    """시트 줄마다 쓸 그림 (방향 그림이 없으면 아래 방향 한 줄)."""
    d = DIRS / f"spark_{sid}_dirs"
    if all((d / f"{x}.png").exists() for x in FIVE):
        pics = {x: solid(Image.open(d / f"{x}.png")) for x in FIVE}
        return [pics[n].transpose(Image.FLIP_LEFT_RIGHT) if flip else pics[n] for n, flip in ROWS]
    return [solid(Image.open(SRC / f"spark_{sid}_32_b.png"))]


def main():
    for sid in PICKED:
        rows = views(sid)
        w, h = rows[0].size
        idle = Image.new("RGBA", (w * len(IDLE), h * len(rows)))
        burst = Image.new("RGBA", (w * len(BURST), h * len(rows)))
        for r, src in enumerate(rows):
            for i, lift in enumerate(IDLE):
                idle.alpha_composite(frame(src, lift), (i * w, r * h))
            for i, (lift, b) in enumerate(BURST):
                burst.alpha_composite(frame(src, lift, b), (i * w, r * h))
        rows[0].resize((512, 512), Image.NEAREST).save(ROOT / f"assets/sparks/{sid}_card.png")   # 도감: 16배
        idle.save(ROOT / f"assets/sparks/{sid}_idle.png")
        burst.save(ROOT / f"assets/sparks/{sid}_burst.png")
        if sid in OUTLINE:
            subprocess.run([sys.executable, str(ROOT / "tools/sprites/own_outline.py"), "--outline", OUTLINE[sid], "--inplace",
                            str(ROOT / f"assets/sparks/{sid}_idle.png"), str(ROOT / f"assets/sparks/{sid}_burst.png")], check=True)
        print(sid, idle.size, burst.size, "8방향" if len(rows) == 8 else "방향 없음")


if __name__ == "__main__":
    main()
