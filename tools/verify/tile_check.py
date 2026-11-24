#!/usr/bin/env python3
"""타일셋 PNG 규격 점검 (CLAUDE.md 에셋 규격: 32×32 격자).

사용: python3 tools/verify/tile_check.py assets/tiles/archive.png [--max-colors 64] [--palette]
점검: 크기가 32의 배수 / 알파가 0 또는 255만 있는지(반투명 없음) / 색 개수 /
      칸별로 비어 있는지 · 일부만 찼는지 · 가득 찼는지 표시.
의존: Pillow
"""
import argparse, sys
from PIL import Image

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("png")
    ap.add_argument("--tile", type=int, default=32)
    ap.add_argument("--max-colors", type=int, default=64)
    ap.add_argument("--palette", action="store_true", help="사용한 색을 많이 쓴 순으로 출력")
    a = ap.parse_args()
    im = Image.open(a.png).convert("RGBA")
    W, H, T = im.width, im.height, a.tile
    ok = True
    print(f"파일 {a.png}  크기 {W}×{H}")
    if W % T or H % T:
        print(f"[실패] 크기가 {T}의 배수가 아님"); ok = False
    else:
        print(f"[통과] 크기 {W//T}×{H//T}칸")
    px = im.load()
    semi = sum(1 for y in range(H) for x in range(W) if 0 < px[x, y][3] < 255)
    print(("[실패]" if semi else "[통과]"), f"반투명 픽셀 {semi}개")
    ok &= semi == 0
    colors = {}
    for y in range(H):
        for x in range(W):
            r, g, b, al = px[x, y]
            if al: colors[(r, g, b)] = colors.get((r, g, b), 0) + 1
    n = len(colors)
    print(("[통과]" if n <= a.max_colors else "[실패]"), f"색 {n}개 (최대 {a.max_colors})")
    ok &= n <= a.max_colors
    print("칸 지도 (. 빈칸, # 가득, + 일부):")
    for ty in range(H // T):
        row = ""
        for tx in range(W // T):
            filled = sum(1 for y in range(ty*T, (ty+1)*T) for x in range(tx*T, (tx+1)*T) if px[x, y][3])
            row += "." if filled == 0 else "#" if filled == T*T else "+"
        print("  " + row)
    if a.palette:
        for (r, g, b), c in sorted(colors.items(), key=lambda kv: -kv[1]):
            print(f"  #{r:02x}{g:02x}{b:02x}  {c}")
    sys.exit(0 if ok else 1)

if __name__ == "__main__":
    main()
