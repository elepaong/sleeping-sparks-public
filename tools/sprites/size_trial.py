#!/usr/bin/env python3
"""루카 설정화의 SD 정면·뒷모습을 여러 스프라이트 크기로 줄여 배경 위에 올려 본다.
크기 판단용 어림 그림이다(자동 축소라 실제 도트보다 뭉개진다). 기록: design/art-direction.md "루카 새 설정화와 캐릭터 크기".
쓰는 법: python3 tools/sprites/size_trial.py  → design/refs/size_trial_2026-11-11/
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "design/refs/size_trial_2026-11-11"
SHEET = ROOT / "design/refs/luca_sheet_2026-11-11.png"
SIZES = [("32x48", 44), ("40x60", 56), ("48x72", 66), ("64x96", 90)]  # 칸 크기, 그 안의 몸 높이


def cut(src, box):
    """종이색 배경을 지우고 인물만 남긴다."""
    im = src.crop(box).convert("RGBA")
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, _ = px[x, y]
            if abs(r - 247) + abs(g - 240) + abs(b - 235) < 40:
                px[x, y] = (0, 0, 0, 0)
    return im.crop(im.getbbox())


def shrink(im, h):
    w = round(im.width * h / im.height)
    s = im.resize((w, h), Image.BOX)
    a = s.split()[3].point(lambda v: 255 if v > 110 else 0)
    q = s.convert("RGB").quantize(18, method=Image.MEDIANCUT, dither=Image.NONE).convert("RGBA")
    q.putalpha(a)
    return q


def main():
    src = Image.open(SHEET).convert("RGB")
    front, back = cut(src, (608, 662, 752, 982)), cut(src, (880, 662, 1012, 982))
    cur = Image.open(ROOT / "assets/chars/luca_idle.png").convert("RGBA").crop((0, 0, 32, 48))
    OUT.mkdir(parents=True, exist_ok=True)

    def scene(bg, name):
        bg = bg.convert("RGBA").copy()
        d = ImageDraw.Draw(bg)
        x, y = 60, 250
        bg.alpha_composite(cur, (x - 16, y - 48))
        d.text((x - 14, y + 3), "now", fill="white")
        x += 60
        for lab, h in SIZES:
            for im in (shrink(front, h), shrink(back, h)):
                d.ellipse((x - im.width // 2, y - 3, x + im.width // 2, y + 3), fill=(10, 5, 20, 255))
                bg.alpha_composite(im, (x - im.width // 2, y - h))
                x += im.width + 8
            d.text((x - 70, y + 5), lab, fill="white")
            x += 24
        bg.resize((bg.width * 3, bg.height * 3), Image.NEAREST).save(OUT / name)

    scene(Image.open(ROOT / "assets/backgrounds/archive.png"), "on_archive.png")
    lab = Image.open(ROOT / "assets/backgrounds/lab.png").convert("RGBA")
    scene(lab.crop((500, 480, 1140, 840)), "on_lab.png")

    strip = Image.new("RGBA", (360, 104), (150, 142, 130, 255))  # 필사실 바닥으로 잡은 밝은 돌색
    x = 6
    strip.alpha_composite(cur, (x, 104 - 52))
    x += 40
    for _, h in SIZES:
        for im in (shrink(front, h), shrink(back, h)):
            strip.alpha_composite(im, (x, 100 - h))
            x += im.width + 6
        x += 10
    strip.resize((strip.width * 5, strip.height * 5), Image.NEAREST).save(OUT / "strip.png")
    shrink(front, 66).save(OUT / "luca_66_front.png")


if __name__ == "__main__":
    main()
