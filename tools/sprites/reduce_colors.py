#!/usr/bin/env python3
"""배경 그림의 색을 64개 이하로 줄인다 (디더 없음, 반투명 없음).

그냥 줄이면 넓은 바닥·벽 색에 팔레트가 몰려 책등의 청록 같은 적은 포인트 색이 사라진다.
그래서 팔레트를 둘로 나눠 만든다: 그림 전체에서 BASE색 + 차가운 포인트 색(녹색~청록~파랑, 옅은 것까지)에서 ACCENT색.
쓰는 법: python3 tools/sprites/reduce_colors.py 원본.png 결과.png [BASE=50] [ACCENT=14]
의존: Pillow
"""
import colorsys
import sys

from PIL import Image


def cool(p):
    h, s, v = colorsys.rgb_to_hsv(*(c / 255 for c in p))
    return 0.25 < h < 0.75 and s > 0.07 and 0.12 < v < 0.8


def main():
    src, dst = sys.argv[1], sys.argv[2]
    base = int(sys.argv[3]) if len(sys.argv) > 3 else 50
    accent = int(sys.argv[4]) if len(sys.argv) > 4 else 14
    im = Image.open(src).convert("RGB")
    raw = im.tobytes()
    acc = [tuple(raw[i:i + 3]) for i in range(0, len(raw), 3)]
    acc = [p for p in acc if cool(p)]
    pal = im.quantize(colors=base, method=Image.Quantize.MEDIANCUT, kmeans=3).getpalette()[:base * 3]
    if acc and accent:
        a = Image.new("RGB", (len(acc), 1))
        a.putdata(acc)
        pal += a.quantize(colors=accent, method=Image.Quantize.MEDIANCUT, kmeans=3).getpalette()[:accent * 3]
    p = Image.new("P", (1, 1))
    p.putpalette(pal + [0] * (768 - len(pal)))
    out = im.quantize(palette=p, dither=Image.Dither.NONE).convert("RGB")
    out.save(dst)
    print(f"{dst}: {out.size[0]}×{out.size[1]}, 색 {len(out.getcolors(1 << 16) or [])}개")


if __name__ == "__main__":
    main()
