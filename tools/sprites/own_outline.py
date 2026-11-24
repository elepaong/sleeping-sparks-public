#!/usr/bin/env python3
"""도트 스프라이트의 가장자리 외곽선을 '제 색 외곽선'으로 바꾼다.

제 색 외곽선: 외곽선을 검정이나 한 가지 어두운 색으로 두르지 않고, 바로 옆 몸 색을 짙게 한 색으로 그린다
(노란 몸은 짙은 주황, 파란 부분은 짙은 파랑). 기준은 design/art-direction.md '도트 그림체 기준'.
Chickxel에서 처음부터 그렇게 뽑는 것이 먼저이고, 이 스크립트는 한 색 외곽선으로 나온 결과를 고칠 때 쓴다.

사용:
  python3 tools/sprites/own_outline.py assets/sparks/hwaro_idle.png ...            # 미리보기: <이름>_own.png를 옆에 만든다
  python3 tools/sprites/own_outline.py --inplace assets/sparks/hwaro_idle.png ...  # 그 파일을 덮어쓴다
  python3 tools/sprites/own_outline.py --outline 3b2116 --outline 000000 파일 ...   # 외곽선 색을 직접 지정
규칙:
  - 외곽선 색을 지정하지 않으면 그림에서 가장 어두운 색을 외곽선으로 본다.
  - 투명과 맞닿은(위·아래·왼쪽·오른쪽) 외곽선 픽셀만 바꾼다. 눈·입처럼 몸 안쪽의 어두운 선은 그대로 둔다.
  - 새 색 = 둘레 8칸에서 가장 많은 몸 색의 채도를 조금 올리고 밝기를 DARKEN배로 낮춘 색.
  - 한 번 바꾼 그림에 다시 돌려도 달라지지 않는다(외곽선 색이 남아 있지 않으면 할 일이 없다).
덮어쓴 뒤에는 tools/verify/asset_check.py를 돌리고 design/licenses.md에 '세션이 외곽선 색 조정'을 적는다.
의존: Pillow
"""
import colorsys
import pathlib
import sys

from PIL import Image

DARKEN = 0.48
NEAR = [(-1, 0), (1, 0), (0, -1), (0, 1), (-1, -1), (1, -1), (-1, 1), (1, 1)]


def darkest(im):
    colors = [c[:3] for _, c in im.getcolors(1 << 20) if c[3]]
    return min(colors, key=lambda c: 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2])


def own_outline(im, outline):
    """바꾼 그림과 바꾼 픽셀 수를 돌려준다."""
    src = im.copy().load()
    px = im.load()
    w, h = im.size
    changed = 0

    def at(x, y):
        return src[x, y] if 0 <= x < w and 0 <= y < h else (0, 0, 0, 0)

    for y in range(h):
        for x in range(w):
            r, g, b, a = src[x, y]
            if not a or (r, g, b) not in outline:
                continue
            if all(at(x + dx, y + dy)[3] for dx, dy in NEAR[:4]):
                continue  # 몸 안쪽 선
            count = {}
            for dx, dy in NEAR:
                c = at(x + dx, y + dy)
                if c[3] and c[:3] not in outline:
                    count[c[:3]] = count.get(c[:3], 0) + 1
            if not count:
                continue
            body = max(count, key=count.get)
            hh, ss, vv = colorsys.rgb_to_hsv(*(c / 255 for c in body))
            rr, gg, bb = colorsys.hsv_to_rgb(hh, min(1.0, ss * 1.15 + 0.1), vv * DARKEN)
            px[x, y] = (round(rr * 255), round(gg * 255), round(bb * 255), a)
            changed += 1
    return im, changed


def main():
    args = sys.argv[1:]
    inplace = "--inplace" in args
    outline = set()
    files = []
    i = 0
    while i < len(args):
        if args[i] == "--outline":
            hx = args[i + 1].lstrip("#")
            outline.add(tuple(int(hx[k:k + 2], 16) for k in (0, 2, 4)))
            i += 2
        elif args[i] == "--inplace":
            i += 1
        else:
            files.append(pathlib.Path(args[i]))
            i += 1
    if not files:
        print(__doc__)
        return 1
    for f in files:
        im = Image.open(f).convert("RGBA")
        used = outline or {darkest(im)}
        im, changed = own_outline(im, used)
        out = f if inplace else f.with_name(f.stem + "_own.png")
        im.save(out)
        names = ", ".join("#%02x%02x%02x" % c for c in sorted(used))
        print(f"{out}: 외곽선 {names} → 픽셀 {changed}개 바꿈")
    return 0


if __name__ == "__main__":
    sys.exit(main())
