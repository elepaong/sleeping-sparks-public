#!/usr/bin/env python3
"""필사실 배치 가이드(회색 상자 그림). ChatGPT에 구도 참고로 붙이고, 배경이 오면 좌표를 맞출 때 기준으로 쓴다.
설계: design/background-prompts.md "필사실 맵 설계". 좌표는 맵 픽셀(1672×941).
쓰는 법: python3 tools/layout/scriptorium_blockout.py → design/refs/scriptorium_blockout_2026-11-11.png
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
W, H = 1672, 941             # 연구실과 같은 큰 맵 (2026-11-11 사람 결정)
OX, OY, A = 836, 200, 32      # 방 안쪽 모서리, 바닥 한 칸(마름모 64×32)
WALL = 160                    # 벽 높이
N = 36                        # 방 한 변의 칸 수 (그림 밖까지 이어진다)


def P(u, v, z=0):
    """u = 오른쪽 아래로, v = 왼쪽 아래로, z = 위로(픽셀)."""
    return (OX + (u - v) * A, OY + (u + v) * A / 2 - z)


def box(d, u, v, du, dv, h, fill, label=None):
    top = [P(u, v, h), P(u + du, v, h), P(u + du, v + dv, h), P(u, v + dv, h)]
    def sh(c, k): return tuple(int(x * k) for x in c)
    if h:
        d.polygon([P(u, v + dv, h), P(u + du, v + dv, h), P(u + du, v + dv), P(u, v + dv)], fill=sh(fill, .72), outline=sh(fill, .4))
        d.polygon([P(u + du, v, h), P(u + du, v + dv, h), P(u + du, v + dv), P(u + du, v)], fill=sh(fill, .56), outline=sh(fill, .4))
    d.polygon(top, fill=fill, outline=sh(fill, .4))
    if label:
        cx, cy = P(u + du / 2, v + dv / 2, h)
        w = d.textlength(label)
        d.rectangle((cx - w / 2 - 2, cy - 7, cx + w / 2 + 2, cy + 6), fill=(255, 255, 255))
        d.text((cx - w / 2, cy - 6), label, fill=(20, 20, 20))


def main():
    im = Image.new("RGB", (W, H), (40, 36, 44))
    d = ImageDraw.Draw(im)
    # 바닥
    d.polygon([P(0, 0), P(N, 0), P(N, N), P(0, N)], fill=(158, 150, 138))
    for i in range(0, N + 1, 2):
        d.line([P(i, 0), P(i, N)], fill=(140, 132, 122))
        d.line([P(0, i), P(N, i)], fill=(140, 132, 122))
    # 벽 (왼쪽 위 = 창문 벽, 오른쪽 위 = 수반·문 벽)
    d.polygon([P(0, 0), P(0, N), P(0, N, WALL), P(0, 0, WALL)], fill=(120, 112, 108), outline=(60, 56, 60))
    d.polygon([P(0, 0), P(N, 0), P(N, 0, WALL), P(0, 0, WALL)], fill=(98, 92, 92), outline=(60, 56, 60))
    # 창문 4개와 바닥에 떨어지는 햇빛
    for v in (4, 11, 18, 25):
        d.polygon([P(0, v, 25), P(0, v + 3, 25), P(0, v + 3, 130), P(0, v + 1.5, 150), P(0, v, 130)], fill=(232, 222, 190), outline=(60, 56, 60))
        d.polygon([P(3, v), P(3, v + 3), P(10, v + 3), P(10, v)], fill=(190, 180, 150))
    d.text((P(0, 13, 160)[0] - 30, P(0, 13, 160)[1] - 14), "A  tall arched windows, daylight", fill=(255, 255, 255))
    # 오른쪽 위 벽의 것들
    d.polygon([P(3, 0), P(6, 0), P(6, 0, 96), P(4.5, 0, 110), P(3, 0, 96)], fill=(86, 62, 44), outline=(30, 20, 16))
    d.text((P(4.5, 0, 125)[0] - 30, P(4.5, 0, 125)[1]), "B  door", fill=(255, 255, 255))
    d.line([P(21.7, 0.3, 80), P(21.7, 0.3, 36)], fill=(120, 190, 230), width=2)
    # 바닥에 놓인 것: 뒤에서 앞으로 그린다 (u+v가 작은 것부터)
    wood, desk, stone = (104, 78, 56), (112, 84, 60), (136, 128, 120)
    things = [
        (8, 0, 6, 1, 90, wood, "C  book cupboards"), (19.2, 0, 1, 1, 26, (170, 140, 70), "D"),
        (20.5, 0, 2.5, 1.2, 36, (150, 150, 160), "E  wall basin"), (26, 0, 8, 1, 90, wood, "F  shelves"),
        (1, 4.2, 1.6, 2.6, 30, desk, None), (1, 11.2, 1.6, 2.6, 30, desk, "G  writing desks"),
        (1, 18.2, 1.6, 2.6, 30, desk, None), (1, 25.2, 1.6, 2.6, 30, desk, None),
        (12, 12, 1.4, 1.4, 150, stone, "L  stone pillar"), (24, 12, 1.4, 1.4, 150, stone, None), (12, 24, 1.4, 1.4, 150, stone, None),
        (6, 14, 2, 3, 30, (124, 92, 62), "1  LUCA'S DESK + old book"),
        (16, 16, 4, 4, 0, (128, 120, 112), "2  carved floor slab"),
        (7, 27, 1.5, 1.5, 0, (60, 60, 66), "3  floor grate"), (27, 7, 1.5, 1.5, 0, (60, 60, 66), "4  floor grate"),
        (14, 5, 6, 2, 30, (118, 90, 62), "M  long reading table"), (25, 16, 4, 1, 44, (132, 104, 74), "H  parchment rack"),
        (10, 6, 1.2, 1.2, 40, (118, 88, 60), "I  lectern"), (5, 18.5, 1, 1, 14, (206, 196, 176), "J  wool basket"),
        (29, 11, 1.5, 1.5, 20, (96, 72, 54), "K  book stacks"), (16, 25, 4, 2, 30, (118, 90, 62), "N  binding table"),
        (26, 23, 2, 3, 26, (96, 72, 54), "O  chests, scroll bins"),
    ]
    for u, v, du, dv, h, c, lab in sorted(things, key=lambda t: t[0] + t[2] / 2 + t[1] + t[3] / 2):
        box(d, u, v, du, dv, h, c, lab)
    # 크기 기준: 루카 (몸 높이 66px)
    luca = ROOT / "design/refs/size_trial_2026-11-11/luca_66_front.png"
    x, y = P(8.8, 16.2)
    if luca.exists():
        s = Image.open(luca).convert("RGBA")
        im.paste(s, (int(x - s.width / 2), int(y - s.height)), s)
    d.text((x + 16, y - 40), "character\n66 px tall", fill=(255, 255, 255))
    d.rectangle((0, 0, 639, 359), outline=(255, 210, 60))
    d.text((6, 4), "one game screen = 640x360", fill=(255, 210, 60))
    im.save(ROOT / "design/refs/scriptorium_blockout_2026-11-11.png")
    for k, (u, v) in {"desk": (7, 15.5), "slab": (18, 18), "grate_scrap": (7.75, 27.75), "grate_key": (27.75, 7.75),
                      "funnel": (19.7, 0.5), "fountain": (21.7, 0.6), "spawn": (8.8, 16.2), "door": (4.5, 0.5)}.items():
        print(k, [round(c) for c in P(u, v)])


if __name__ == "__main__":
    main()
