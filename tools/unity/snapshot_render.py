#!/usr/bin/env python3
"""Snapshot.cs가 적은 장면 JSON을 게임 화면처럼 그린다 (Unity 없이 화면 확인용).

GameView.cs의 그리기 규칙을 그대로 따른다:
- 640×360 화면을 카메라 중심으로 잘라 3배로 키운다(1920×1080, Point 필터).
- 순서: 배경 → 햇빛 자락 → 납작한 사물(목록 순) → 나머지는 발치 y가 클수록 앞(루카·스파크·사물을 함께 정렬, 그림자는 바로 뒤) → 먼지 → 이름표.
- 사물 그림의 자리는 Core의 SpriteRect, 루카는 발끝이 위치(칸 아래에서 3px 위), 스파크는 칸 크기 비율로 띄운다.
다른 점: 글꼴(Unity는 OS 글꼴), 먼지 자리(난수), 화면 위 HUD·스파크 칸은 그리지 않는다.
쓰는 법: python3 tools/unity/snapshot_render.py 장면.json 결과.png
"""
import json
import math
import pathlib
import random
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = pathlib.Path(__file__).resolve().parents[2]
SCALE = 3
FOOT_MARGIN = 3
SPARK_HOVER = 8               # Config.SparkHover
SHADOW = (13, 8, 26)          # GameView.ShadowColor (0.05, 0.03, 0.1)
FONT = "/usr/share/fonts/truetype/nanum/NanumGothicBold.ttf"


def img(path):
    return Image.open(ROOT / path).convert("RGBA")


def hexc(s, default=(255, 255, 255)):
    s = (s or "").lstrip("#")
    return tuple(int(s[i:i + 2], 16) for i in (0, 2, 4)) if len(s) == 6 else default


def ellipse(layer, cx, cy, rx, ry, rgba):
    d = ImageDraw.Draw(layer)
    d.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=rgba)


def glow(cx, cy, w, h, rgb, alpha):
    """Art.GlowSprite: 가운데가 진하고 가장자리로 옅어지는 원 (알파 섞기)."""
    w, h = max(1, round(w)), max(1, round(h))
    g = Image.new("RGBA", (w, h))
    px = g.load()
    for y in range(h):
        for x in range(w):
            dx, dy = (x + 0.5 - w / 2) / (w / 2), (y + 0.5 - h / 2) / (h / 2)
            a = max(0.0, 1 - math.hypot(dx, dy)) * alpha
            px[x, y] = rgb + (round(a * 255),)
    return g, (round(cx - w / 2), round(cy - h / 2))


def main():
    sc = json.load(open(sys.argv[1], encoding="utf-8"))
    out = sys.argv[2]
    zone = json.load(open(ROOT / f"data/zones/{sc['zone']}.json", encoding="utf-8"))
    mw, mh = sc["map"]
    canvas = Image.new("RGBA", (int(mw), int(mh)), (0, 0, 0, 255))
    canvas.alpha_composite(img(sc["background"]), (0, 0))
    t = sc["anim_t"]

    sun = zone.get("sunlight")
    if sun:
        c = hexc(sun.get("color")) + (round(sun.get("alpha", 0.2) * 255),)
        lay = Image.new("RGBA", canvas.size)
        d = ImageDraw.Draw(lay)
        shape = img(sun["sprite"]) if sun.get("sprite") else None
        for x, y, w, h in sun["rects"]:
            if shape:
                tint = Image.new("RGBA", shape.size, c)
                tint.putalpha(shape.getchannel("A").point(lambda v: c[3] if v else 0))
                lay.alpha_composite(tint, (x, y))
            else:
                d.rectangle((x, y, x + w - 1, y + h - 1), fill=c)
        canvas.alpha_composite(lay)

    draws = []   # (정렬값, 함수)
    for i, o in enumerate(sc["objects"]):
        im = img(o["sprite"])
        n = o["frames"]
        if n > 1:
            fw = im.width // n
            k = int(t * o["fps"]) % n
            im = im.crop((k * fw, 0, (k + 1) * fw, im.height))
        x, y = round(o["rect"][0]), round(o["rect"][1])
        order = -10000 + i if o["flat"] else round(o["y"]) * 2 + 1
        draws.append((order, lambda im=im, x=x, y=y: canvas.alpha_composite(im, (x, y))))

    # 루카
    p = sc["player"]
    sheet = img("assets/chars/luca_walk.png" if p["moving"] else "assets/chars/luca_idle.png")
    fh = sheet.height // 8
    fw = round(fh * 48 / 72)                 # GameView.Sheet와 같다: 칸 48×72 비율, 프레임 수 = 폭 ÷ 칸 폭
    frame = sheet.crop((0, p["dir"] * fh, fw, (p["dir"] + 1) * fh))
    px_, py_ = p["pos"]
    po = round(py_) * 2 + 1

    def draw_player():
        lay = Image.new("RGBA", canvas.size)
        ellipse(lay, px_, py_ - 2, 10, 4, SHADOW + (round(0.5 * 0.7 * 255),))
        canvas.alpha_composite(lay)
        canvas.alpha_composite(frame, (round(px_ - fw / 2), round(py_ - fh + FOOT_MARGIN)))
    draws.append((po, draw_player))

    # 스파크
    for s in sc["sparks"]:
        idle = img(f"assets/sparks/{s['id']}_idle.png")
        f = idle.width // 4                      # 대기 4프레임, 칸은 정사각형
        row = s.get("dir", 0) if idle.height // f >= 8 else 0   # 8줄이면 방향별 시트
        k = f / 24
        fi = int(t * 4) % 4
        body = idle.crop((fi * f, row * f, (fi + 1) * f, (row + 1) * f))
        sx, sy = s["pos"]

        def draw_spark(body=body, sx=sx, sy=sy, k=k, gl=s["glow"], f=f):
            lay = Image.new("RGBA", canvas.size)
            ellipse(lay, sx, sy, 4 * k, 1.5 * k, SHADOW + (round(0.5 * 0.5 * 255),))
            canvas.alpha_composite(lay)
            if gl:
                c = hexc(gl)
                m = max(0.05, max(c) / 255)
                rgb = tuple(min(255, round(v / m)) for v in c)
                g, at = glow(sx, sy, 30 * k, 12 * k, rgb, min(1.0, m * 1.0))
                canvas.alpha_composite(g, at)
            canvas.alpha_composite(body, (round(sx - f / 2), round(sy - SPARK_HOVER - f)))
        draws.append((round(sy) * 2 + 1, draw_spark))

    for _, fn in sorted(draws, key=lambda d: d[0]):
        fn()

    if sun and sun.get("dust"):
        rnd = random.Random(1)
        dc = hexc(sun.get("dust_color"))
        hgt = sun.get("dust_height", 80)
        lay = Image.new("RGBA", canvas.size)
        for i in range(sun["dust"]):
            x, y, w, h = sun["rects"][i % len(sun["rects"])]
            mx, my = x + rnd.random() * w, y - hgt + rnd.random() * (h + hgt)
            lay.putpixel((round(mx), round(my)), dc + (round((0.35 + 0.4 * rnd.random()) * 255),))
        canvas.alpha_composite(lay)

    vw, vh = sc["view"]
    cx, cy = sc["camera"]
    left, top = round(cx - vw / 2), round(cy - vh / 2)
    view = canvas.crop((left, top, left + vw, top + vh)).resize((vw * SCALE, vh * SCALE), Image.NEAREST)

    # 이름표 (Ui.Labels: 사물 위쪽 가운데, 6pt 흰 글씨 + 검은 테두리) 와 지역 이름
    d = ImageDraw.Draw(view)
    font = ImageFont.truetype(FONT, 6 * SCALE + 2)
    big = ImageFont.truetype(FONT, 9 * SCALE + 2)
    for o in sc["objects"]:
        if not o["label"]:
            continue
        lx, ly = (o["label_at"][0] - left) * SCALE, (o["label_at"][1] - top) * SCALE
        if -120 < lx < vw * SCALE + 120 and 0 < ly < vh * SCALE + 30:
            d.text((lx, ly - 4), o["label"], font=font, fill=(255, 255, 255, 230), anchor="mb", stroke_width=2, stroke_fill=(0, 0, 0))
    d.text((6 * SCALE, 3 * SCALE), sc["zone_name"], font=big, fill=(240, 236, 228), stroke_width=2, stroke_fill=(0, 0, 0))
    view.convert("RGB").save(out)
    print(out)


if __name__ == "__main__":
    main()
