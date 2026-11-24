#!/usr/bin/env python3
"""필사실 정식 배경·사물을 시안에서 assets/로 만든다 (2026-11-18, 임시본 — Chickxel로 다듬으면 덮어쓴다).

입력 (design/refs/maps_2026-11/):
  P-1_room_02_64.png   빈 방 배경 (tools/sprites/reduce_colors.py로 64색 이하)
  P-1_props_01.png     사물 시트 (ChatGPT, 1536×1024)
  P-1_spout_01.png     벽 꼭지 (ChatGPT)
출력:
  assets/backgrounds/scriptorium.png (1672×941)
  assets/props/<이름>.png — 시트에서 잘라 규격 크기 캔버스(32의 배수)에 바닥 가운데 맞춤, 반투명 없음
  assets/props/stream_{straight,left,right}.png — 물줄기 임시 애니메이션(160×96 × 4프레임, 세션이 그림)
설계: design/background-prompts.md "필사실 정식 배경 — 정면 3/4, 층 나누기"
쓰는 법: python3 tools/sprites/scriptorium_assets.py
"""
import pathlib
import shutil

from PIL import Image, ImageDraw

ROOT = pathlib.Path(__file__).resolve().parents[2]
REF = ROOT / "design/refs/maps_2026-11"
PROPS = ROOT / "assets/props"

# 이름: (시트 안 자를 네모, 들어갈 크기(맞춰 줄임), 캔버스 크기)
CUTS = {
    "desk_book": ((52, 84, 360, 344), (128, 96), (128, 96)),
    "wool_basket": ((428, 152, 580, 312), (32, 32), (32, 32)),
    "hatch_closed": ((628, 120, 932, 360), (192, 128), (192, 128)),
    "hatch_open": ((964, 120, 1248, 360), (192, 128), (192, 128)),
    "grate": ((1288, 164, 1508, 352), (64, 64), (64, 64)),
    "fountain": ((88, 548, 340, 776), (96, 96), (96, 96)),
    "funnel": ((436, 532, 548, 772), (32, 64), (32, 64)),
    "pillar": ((1008, 408, 1176, 956), (64, 192), (64, 192)),
    "ledge": ((696, 672, 884, 772), (32, 32), (32, 32)),
    "ink_drop": ((1356, 804, 1444, 852), (24, 24), (32, 32)),
}
WATER = [(173, 214, 236), (122, 176, 214), (84, 136, 186)]   # 밝은 줄, 몸, 짙은 가장자리 (제 색 외곽선)


def solid_alpha(im, colors=32):
    """반투명을 없애고(0/255) 색을 colors개 이하로 (디더 없음)."""
    a = im.getchannel("A").point(lambda v: 255 if v > 100 else 0)
    rgb = im.convert("RGB").quantize(colors=colors, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGB")
    out = rgb.convert("RGBA")
    out.putalpha(a)
    return out


def fit(src, box, canvas):
    s = min(box[0] / src.width, box[1] / src.height)
    im = src.resize((max(1, round(src.width * s)), max(1, round(src.height * s))), Image.BOX)
    im = solid_alpha(im)
    out = Image.new("RGBA", canvas, (0, 0, 0, 0))
    out.alpha_composite(im, ((canvas[0] - im.width) // 2, canvas[1] - im.height))
    return out


def stream(bend, frames=4, w=160, h=96):
    """가는 물줄기. bend: 0 곧게, -1 왼쪽(깔때기 쪽, 72px), +1 오른쪽. 위 가운데가 꼭지 입."""
    sheet = Image.new("RGBA", (w * frames, h), (0, 0, 0, 0))
    for f in range(frames):
        d = ImageDraw.Draw(sheet)
        pts = []
        for y in range(h):
            t = y / (h - 1)
            x = w / 2 + bend * 72 * t * t   # 아래로 갈수록 더 휜다
            pts.append((x, y))
        for i, (x, y) in enumerate(pts):
            x0 = round(sheet_x := f * w + x)
            d.point((x0 - 2, y), fill=WATER[2] + (255,))
            d.point((x0 + 2, y), fill=WATER[2] + (255,))
            d.line((x0 - 1, y, x0 + 1, y), fill=WATER[1] + (255,))
            if (y + f * 6) % 24 < 5:   # 아래로 흘러내리는 밝은 마디
                d.point((x0, y), fill=WATER[0] + (255,))
    return sheet


def window_light(w=96, h=64, skew=18):
    """창에서 바닥에 떨어진 햇빛 자락: 아치 창 모양(가운데 세로살 하나, 가로살 하나)을 오른쪽 아래로 비스듬히 민 것.
    색은 엔진이 지역 데이터 sunlight.color·alpha로 칠한다 — 그림은 흰 불투명 모양만."""
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = im.load()
    body_w = w - skew
    r = body_w / 2
    for y in range(h):
        dx = round(skew * y / (h - 1))
        for x in range(body_w):
            # 위쪽 r/2 줄은 아치 (바닥에 눕혀 세로가 줄어든 반원)
            ay = y / (r / 2) if y < r / 2 else 1.0
            if y < r / 2 and ((x + 0.5 - r) / r) ** 2 + (1 - ay) ** 2 > 1:
                continue
            if abs(x + 0.5 - r) < 2 or abs(y - h * 0.55) < 1.5:   # 창살 그림자
                continue
            px[x + dx, y] = (255, 255, 255, 255)
    return im


def main():
    shutil.copyfile(REF / "P-1_room_02_64.png", ROOT / "assets/backgrounds/scriptorium.png")
    sheet = Image.open(REF / "P-1_props_01.png").convert("RGBA")
    for name, (cut, box, canvas) in CUTS.items():
        fit(sheet.crop(cut), box, canvas).save(PROPS / f"{name}.png")
    spout = Image.open(REF / "P-1_spout_01.png").convert("RGBA")
    fit(spout.crop(spout.getchannel("A").point(lambda v: 255 if v > 100 else 0).getbbox()), (32, 32), (32, 32)).save(PROPS / "spout.png")
    for name, bend in (("straight", 0), ("left", -1), ("right", 1)):
        stream(bend).save(PROPS / f"stream_{name}.png")
    window_light().save(PROPS / "sunlight_window.png")
    print("필사실 배경 1장, 사물", len(CUTS) + 1, "개, 물줄기 3개")


if __name__ == "__main__":
    main()
