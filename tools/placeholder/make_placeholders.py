#!/usr/bin/env python3
"""프로토타입용 임시 그림(단색 도형 + 이름표)을 CLAUDE.md 규격 크기로 만든다.

사용: python tools/placeholder/make_placeholders.py [--force] [저장소 루트]
  - 이미 있는 파일은 건드리지 않는다(정식 그림을 덮어쓰지 않도록). --force면 모두 다시 그린다.
  - 문 퍼즐 그림 D-LIGHT-02는 data/puzzles/D-LIGHT-02.json의 'figure'를 읽어 그린다.
    배치를 고쳤으면 --force로 다시 그린다.
  - 연구실 배경은 여기서 만들지 않는다 (사람이 준 그림을 색만 줄여 넣음 — design/art-direction.md).
만드는 것: 배경 1장(필사실), 소품, 초상화(3명 × 표정 4종), 문 퍼즐 그림 6장, 스파크 도감 2장.
출처 기록은 design/licenses.md. 의존: Pillow
"""
import json
import math
import pathlib
import sys

from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding="utf-8")

FONT_FILES = [
    "C:/Windows/Fonts/malgun.ttf",
    "/System/Library/Fonts/AppleSDGothicNeo.ttc",
    "/usr/share/fonts/truetype/nanum/NanumGothic.ttf",
    "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc",
]


def font(size):
    for f in FONT_FILES:
        if pathlib.Path(f).exists():
            return ImageFont.truetype(f, size)
    return None  # 한글 글꼴이 없으면 이름표를 생략한다


def label(draw, xy, text, size, fill, anchor="mm"):
    f = font(size)
    if f is not None:
        draw.text(xy, text, font=f, fill=fill, anchor=anchor)


# ---------------------------------------------------------------- 배경 (640×360, 색 64개 이하, 반투명 없음)

def bg_scriptorium(root):
    im = Image.new("RGBA", (640, 360), (58, 50, 44, 255))
    d = ImageDraw.Draw(im)
    d.fontmode = "1"  # 글자 가장자리를 흐리게 하지 않는다(색 수 제한)
    d.rectangle([0, 0, 639, 139], fill=(92, 80, 68, 255))           # 뒷벽
    d.rectangle([0, 132, 639, 139], fill=(70, 60, 52, 255))         # 걸레받이
    d.rectangle([60, 140, 579, 345], fill=(122, 104, 84, 255))      # 바닥
    for x in range(60, 580, 40):
        d.line([x, 140, x, 345], fill=(110, 94, 76, 255))
    for y in range(140, 346, 34):
        d.line([60, y, 579, y], fill=(110, 94, 76, 255))
    for x in (90, 210, 330):                                         # 벽 책장
        d.rectangle([x, 30, x + 90, 128], fill=(74, 54, 40, 255), outline=(40, 30, 24, 255))
        for y in (52, 76, 100):
            d.line([x + 2, y, x + 88, y], fill=(40, 30, 24, 255))
    d.rectangle([520, 26, 580, 100], fill=(40, 52, 78, 255), outline=(40, 30, 24, 255))  # 밤 창문
    d.line([550, 26, 550, 100], fill=(40, 30, 24, 255))
    label(d, (8, 114), "필사실 (임시 배경)", 12, (230, 220, 200, 255), "la")
    return im


# ---------------------------------------------------------------- 소품 (32의 배수, 반투명 없음)

def prop(size, painter):
    im = Image.new("RGBA", size, (0, 0, 0, 0))
    painter(ImageDraw.Draw(im), size[0], size[1])
    return im


K = (30, 26, 24, 255)  # 윤곽선


def p_desk_book(d, w, h):
    d.rectangle([2, 8, w - 3, 20], fill=(120, 84, 52, 255), outline=K)
    d.rectangle([6, 20, 10, h - 2], fill=(90, 62, 40, 255), outline=K)
    d.rectangle([w - 11, 20, w - 7, h - 2], fill=(90, 62, 40, 255), outline=K)
    d.rectangle([22, 2, 42, 10], fill=(226, 212, 176, 255), outline=K)
    d.line([32, 2, 32, 10], fill=K)


def p_grate(d, w, h):
    d.rectangle([4, 12, w - 5, h - 5], fill=(34, 32, 34, 255), outline=K)
    for x in range(8, w - 5, 5):
        d.line([x, 12, x, h - 5], fill=(120, 120, 126, 255))


def p_iron_key(d, w, h):
    d.ellipse([11, 16, 16, 21], outline=(170, 170, 178, 255))
    d.line([16, 19, 22, 19], fill=(170, 170, 178, 255))
    d.line([21, 19, 21, 22], fill=(170, 170, 178, 255))


def p_paper_scrap(d, w, h):
    d.polygon([(12, 17), (19, 15), (21, 21), (14, 23)], fill=(236, 228, 200, 255), outline=K)


def p_fountain(d, w, h):
    d.rectangle([4, 2, w - 5, 22], fill=(128, 132, 136, 255), outline=K)
    d.rectangle([13, 20, 18, 26], fill=(170, 150, 80, 255), outline=K)       # 꼭지
    d.rectangle([2, h - 16, w - 3, h - 3], fill=(108, 112, 118, 255), outline=K)  # 수반
    d.rectangle([5, h - 13, w - 6, h - 9], fill=(70, 120, 170, 255))


def p_stream(dx):
    def paint(d, w, h):
        d.line([16, 0, 16 + dx // 3, 12, 16 + dx, h - 1], fill=(120, 190, 240, 255), width=2)
    return paint


def p_funnel(d, w, h):
    d.polygon([(6, 12), (26, 12), (19, 24), (13, 24)], fill=(196, 160, 70, 255), outline=K)
    d.rectangle([14, 24, 18, 30], fill=(196, 160, 70, 255), outline=K)


def p_ledge(d, w, h):
    d.rectangle([6, 18, w - 7, h - 3], fill=(150, 146, 136, 255), outline=K)
    d.rectangle([4, 14, w - 5, 18], fill=(176, 172, 160, 255), outline=K)


def p_hatch(open_):
    def paint(d, w, h):
        if open_:
            d.rectangle([2, 2, w - 3, h - 3], fill=(24, 22, 22, 255), outline=K)
            for i in range(5):
                d.rectangle([6 + i * 4, 5 + i * 5, w - 7, 8 + i * 5], fill=(92 - i * 10, 86 - i * 10, 80 - i * 10, 255))
        else:
            d.rectangle([2, 2, w - 3, h - 3], fill=(146, 128, 104, 255), outline=K)
            d.rectangle([6, 6, w - 7, h - 7], outline=(110, 94, 76, 255))
    return paint


def p_ink(d, w, h):
    d.polygon([(16, 8), (22, 19), (16, 25), (10, 19)], fill=(40, 60, 150, 255), outline=K)
    d.point([(14, 17), (14, 18)], fill=(170, 190, 255, 255))


def p_stairs(d, w, h):
    d.rectangle([2, 4, w - 3, h - 3], fill=(30, 28, 30, 255), outline=K)
    for i in range(4):
        d.rectangle([4, 6 + i * 6, w - 5 - i * 4, 9 + i * 6], fill=(120 - i * 16, 112 - i * 16, 104 - i * 16, 255))


def p_light_stand(d, w, h):
    d.rectangle([12, 14, 19, h - 6], fill=(150, 120, 60, 255), outline=K)
    d.rectangle([7, h - 7, 24, h - 2], fill=(120, 96, 50, 255), outline=K)
    d.rectangle([8, 9, 23, 14], fill=(196, 160, 70, 255), outline=K)


def p_mirror(kind):
    def paint(d, w, h):
        d.rectangle([9, h - 6, 22, h - 2], fill=(110, 80, 50, 255), outline=K)
        pts = [(6, 22), (25, 5)] if kind == "/" else [(6, 5), (25, 22)]
        d.line(pts, fill=K, width=6)
        d.line(pts, fill=(190, 226, 240, 255), width=3)
    return paint


def p_lock(lit):
    def paint(d, w, h):
        d.ellipse([5, 5, 26, 26], fill=(255, 226, 110, 255) if lit else (70, 76, 96, 255), outline=K)
        c = (255, 255, 230, 255) if lit else (120, 130, 150, 255)
        d.line([16, 8, 16, 23], fill=c)
        d.line([8, 16, 23, 16], fill=c)
        d.line([10, 10, 21, 21], fill=c)
        d.line([21, 10, 10, 21], fill=c)
    return paint


def p_lens_slot(full):
    def paint(d, w, h):
        d.ellipse([5, 11, 26, 27], outline=(196, 160, 70, 255), width=2)
        if full:
            d.ellipse([10, 2, 21, 24], fill=(170, 220, 240, 255), outline=K)
            d.line([15, 5, 15, 12], fill=(240, 250, 255, 255))
    return paint


def p_vine(d, w, h):
    d.rectangle([0, 0, w - 1, h - 1], fill=(96, 84, 50, 255), outline=K)
    for i in range(0, h, 7):
        d.line([2, i, w - 3, i + 9], fill=(60, 52, 30, 255))
        d.line([w - 3, i, 2, i + 9], fill=(130, 114, 64, 255))


PROPS = {
    "desk_book": ((64, 32), p_desk_book),
    "grate": ((32, 32), p_grate),
    "iron_key": ((32, 32), p_iron_key),
    "paper_scrap": ((32, 32), p_paper_scrap),
    "fountain": ((32, 64), p_fountain),
    "stream_straight": ((32, 32), p_stream(0)),
    "stream_left": ((32, 32), p_stream(-12)),
    "stream_right": ((32, 32), p_stream(12)),
    "funnel": ((32, 32), p_funnel),
    "ledge": ((32, 32), p_ledge),
    "hatch_closed": ((64, 32), p_hatch(False)),
    "hatch_open": ((64, 32), p_hatch(True)),
    "ink_drop": ((32, 32), p_ink),
    "stairs": ((32, 32), p_stairs),
    "light_stand": ((32, 32), p_light_stand),
    "mirror_slash": ((32, 32), p_mirror("/")),
    "mirror_backslash": ((32, 32), p_mirror("\\")),
    "light_lock_off": ((32, 32), p_lock(False)),
    "light_lock_lit": ((32, 32), p_lock(True)),
    "lens_slot_empty": ((32, 32), p_lens_slot(False)),
    "lens_slot_lens": ((32, 32), p_lens_slot(True)),
    "dry_vine": ((32, 64), p_vine),
}


# ---------------------------------------------------------------- 초상화 (512×512, 표정 4종)

PEOPLE = {
    "luca": ("루카", (150, 110, 80), (236, 200, 170)),
    "jjirit": ("찌릿", (60, 90, 130), (240, 180, 70)),
    "isolde": ("이졸데", (90, 110, 100), (236, 214, 196)),
}
EXPRESSIONS = {"neutral": "보통", "happy": "기쁨", "surprised": "놀람", "sad": "슬픔"}


def portrait(who, expr):
    name, bg, skin = PEOPLE[who]
    im = Image.new("RGBA", (512, 512), bg + (255,))
    d = ImageDraw.Draw(im)
    d.ellipse([116, 90, 396, 390], fill=skin + (255,), outline=(40, 30, 30, 255), width=6)
    eye = (40, 30, 30, 255)
    if expr == "surprised":
        d.ellipse([186, 196, 226, 236], outline=eye, width=6)
        d.ellipse([286, 196, 326, 236], outline=eye, width=6)
        d.ellipse([232, 290, 280, 340], outline=eye, width=6)
    else:
        d.ellipse([192, 202, 220, 230], fill=eye)
        d.ellipse([292, 202, 320, 230], fill=eye)
        if expr == "happy":
            d.arc([196, 250, 316, 340], 20, 160, fill=eye, width=8)
        elif expr == "sad":
            d.arc([196, 300, 316, 380], 200, 340, fill=eye, width=8)
            d.line([180, 180, 226, 192], fill=eye, width=6)
            d.line([332, 180, 286, 192], fill=eye, width=6)
        else:
            d.line([216, 314, 296, 314], fill=eye, width=8)
    label(d, (256, 440), f"{name} · {EXPRESSIONS[expr]}", 44, (255, 255, 255, 255))
    label(d, (256, 486), "임시 초상화", 22, (255, 255, 255, 255))
    return im


# ---------------------------------------------------------------- 문 퍼즐 그림 (1200×800)

PAPER = (232, 220, 190, 255)
INK = (50, 40, 34, 255)
RAY = (210, 120, 20, 255)
GLASS = (120, 180, 210, 255)


def sheet(title):
    im = Image.new("RGBA", (1200, 800), PAPER)
    d = ImageDraw.Draw(im)
    d.rectangle([12, 12, 1187, 787], outline=INK, width=4)
    label(d, (600, 760), f"{title} (임시 그림)", 24, (130, 110, 90, 255))
    return im, d


def arrow(d, a, b, fill, width=8, head=26):
    d.line([a, b], fill=fill, width=width)
    ang = math.atan2(b[1] - a[1], b[0] - a[0])
    for s in (2.6, -2.6):
        d.line([b, (b[0] + head * math.cos(ang + s), b[1] + head * math.sin(ang + s))], fill=fill, width=width)


def fig_static_01(pz):
    im, d = sheet(pz["title"])
    d.ellipse([500, 110, 700, 230], fill=(226, 150, 40, 255), outline=INK, width=5)
    label(d, (600, 80), "양털로 문지른 호박", 34, INK)
    d.line([120, 560, 1080, 560], fill=INK, width=6)
    xs = [240, 480, 720, 960]
    for i in range(14):
        d.rectangle([180 + (i * 37) % 110, 520 + (i * 13) % 30, 192 + (i * 37) % 110, 530 + (i * 13) % 30], fill=(250, 246, 230, 255), outline=INK)
    d.line([430, 548, 530, 548], fill=(110, 110, 120, 255), width=10)
    d.ellipse([420, 536, 444, 560], fill=(110, 110, 120, 255), outline=INK)
    d.ellipse([670, 510, 770, 560], fill=(150, 146, 140, 255), outline=INK, width=4)
    d.ellipse([925, 520, 995, 560], fill=(200, 164, 70, 255), outline=INK, width=4)
    for x, t in zip(xs, pz["input"]["options"]):
        label(d, (x, 610), t, 30, INK)
    return im


def fig_static_02(pz):
    im, d = sheet(pz["title"])
    d.line([200, 140, 1000, 140], fill=INK, width=10)
    for x, name in ((360, "A"), (600, "B"), (840, "C")):
        d.line([x, 140, x, 440], fill=(120, 110, 100, 255), width=3)
        d.ellipse([x - 60, 440, x + 60, 560], fill=(246, 240, 224, 255), outline=INK, width=6)
        label(d, (x, 500), name, 64, INK)
    label(d, (480, 640), "A·B: 밀어냄", 34, INK)
    label(d, (720, 640), "B·C: 끌어당김", 34, INK)
    return im


def fig_light_01(pz):
    im, d = sheet(pz["title"])
    d.line([150, 560, 1050, 560], fill=GLASS, width=16)
    d.line([150, 570, 1050, 570], fill=INK, width=4)
    label(d, (980, 610), "거울", 34, INK)
    hit = (600, 552)
    t = math.radians(30)
    a = (hit[0] - 460 * math.cos(t), hit[1] - 460 * math.sin(t))
    b = (hit[0] + 460 * math.cos(t), hit[1] - 460 * math.sin(t))
    mid = ((a[0] + hit[0]) / 2, (a[1] + hit[1]) / 2)
    arrow(d, a, mid, RAY)
    d.line([mid, hit], fill=RAY, width=8)
    mid2 = ((b[0] + hit[0]) / 2, (b[1] + hit[1]) / 2)
    arrow(d, hit, mid2, RAY)
    d.line([mid2, b], fill=RAY, width=8)
    d.arc([hit[0] - 150, hit[1] - 150, hit[0] + 150, hit[1] + 150], 180, 210, fill=INK, width=5)
    d.arc([hit[0] - 150, hit[1] - 150, hit[0] + 150, hit[1] + 150], 330, 360, fill=INK, width=5)
    label(d, (400, 505), "30°", 44, INK)
    label(d, (800, 505), "?", 56, INK)
    return im


def fig_light_02(pz):
    fig = pz["figure"]
    im, d = sheet(pz["title"])
    cols, rows = fig["cols"], fig["rows"]
    cell = 110
    ox, oy = (1200 - cols * cell) // 2, (740 - rows * cell) // 2
    d.rectangle([ox, oy, ox + cols * cell, oy + rows * cell], fill=(244, 236, 214, 255), outline=INK, width=8)
    for c in range(1, cols):
        d.line([ox + c * cell, oy, ox + c * cell, oy + rows * cell], fill=(190, 176, 150, 255), width=2)
    for r in range(1, rows):
        d.line([ox, oy + r * cell, ox + cols * cell, oy + r * cell], fill=(190, 176, 150, 255), width=2)
    m = 16
    for mir in fig["mirrors"]:
        c, r = mir["cell"]
        x0, y0 = ox + c * cell, oy + r * cell
        pts = [(x0 + m, y0 + cell - m), (x0 + cell - m, y0 + m)] if mir["kind"] == "/" else [(x0 + m, y0 + m), (x0 + cell - m, y0 + cell - m)]
        d.line(pts, fill=INK, width=16)
        d.line(pts, fill=GLASS, width=8)

    def edge_point(side, idx, out):
        if side == "W":
            return (ox - out, oy + idx * cell + cell // 2)
        if side == "E":
            return (ox + cols * cell + out, oy + idx * cell + cell // 2)
        if side == "N":
            return (ox + idx * cell + cell // 2, oy - out)
        return (ox + idx * cell + cell // 2, oy + rows * cell + out)

    def opening(side, idx):
        x, y = edge_point(side, idx, 0)
        if side in "WE":
            d.line([x, y - 34, x, y + 34], fill=PAPER, width=12)
        else:
            d.line([x - 34, y, x + 34, y], fill=PAPER, width=12)

    for name, e in fig["exits"].items():
        opening(e["side"], e["index"])
        label(d, edge_point(e["side"], e["index"], 52), name, 56, INK)
    e = fig["entry"]
    opening(e["side"], e["index"])
    arrow(d, edge_point(e["side"], e["index"], 150), edge_point(e["side"], e["index"], 16), RAY, 10, 30)
    lx, ly = edge_point(e["side"], e["index"], 150)
    label(d, (lx - 50 if e["side"] in "WE" else lx, ly + (0 if e["side"] in "WE" else 40)), "빛", 44, RAY)
    return im


def fig_light_03(pz):
    im, d = sheet(pz["title"])
    corner = (300, 620)
    d.line([corner, (1000, 620)], fill=GLASS, width=16)
    d.line([corner, (300, 100)], fill=GLASS, width=16)
    d.line([(292, 630), (1000, 630)], fill=INK, width=4)
    d.line([(290, 630), (290, 100)], fill=INK, width=4)
    label(d, (960, 670), "거울", 34, INK)
    label(d, (230, 130), "거울", 34, INK)
    hit = (640, 612)
    start = (980, 300)
    mid = ((start[0] + hit[0]) / 2, (start[1] + hit[1]) / 2)
    arrow(d, start, mid, RAY)
    d.line([mid, hit], fill=RAY, width=8)
    label(d, (460, 420), "?", 90, INK)
    label(d, (640, 80), "위에서 내려다본 모습", 30, INK)
    return im


def fig_light_04(pz):
    im, d = sheet(pz["title"])
    for x, name, bulge in ((300, "가", 26), (600, "나", 60), (900, "다", 104)):
        top, bot = 190, 550
        left = [(x - 8 - bulge * math.sin(math.pi * i / 24), top + (bot - top) * i / 24) for i in range(25)]
        right = [(x + 8 + bulge * math.sin(math.pi * i / 24), top + (bot - top) * i / 24) for i in range(24, -1, -1)]
        d.polygon(left + right, fill=(180, 220, 236, 255), outline=INK)
        d.line(left + right + [left[0]], fill=INK, width=5)
        label(d, (x, 630), name, 64, INK)
    label(d, (600, 100), "옆에서 본 세 렌즈 (지름과 가장자리 두께는 같다)", 32, INK)
    return im


FIGURES = {
    "D-STATIC-01": fig_static_01,
    "D-STATIC-02": fig_static_02,
    "D-LIGHT-01": fig_light_01,
    "D-LIGHT-02": fig_light_02,
    "D-LIGHT-03": fig_light_03,
    "D-LIGHT-04": fig_light_04,
}


# ---------------------------------------------------------------- 스파크 도감 (512×512 투명)

def spark_card(root, spark):
    src = Image.open(root / f"assets/sparks/{spark}_idle.png").convert("RGBA").crop((0, 0, 24, 24))
    im = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    im.paste(src.resize((384, 384), Image.NEAREST), (64, 64))
    return im


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    force = "--force" in sys.argv
    root = pathlib.Path(args[0] if args else ".").resolve()
    jobs = [
        ("assets/backgrounds/scriptorium.png", lambda: bg_scriptorium(root)),
    ]
    for name, (size, painter) in PROPS.items():
        jobs.append((f"assets/props/{name}.png", lambda s=size, p=painter: prop(s, p)))
    for who in PEOPLE:
        for expr in EXPRESSIONS:
            jobs.append((f"assets/portraits/{who}_{expr}.png", lambda w=who, e=expr: portrait(w, e)))
    for pid, fn in FIGURES.items():
        pz = json.loads((root / f"data/puzzles/{pid}.json").read_text(encoding="utf-8"))
        jobs.append((f"assets/puzzles/{pid}.png", lambda f=fn, p=pz: f(p)))
    for spark in ("jjirit", "lumi"):
        jobs.append((f"assets/sparks/{spark}_card.png", lambda s=spark: spark_card(root, s)))

    made = skipped = 0
    for rel, build in jobs:
        path = root / rel
        if path.exists() and not force:
            skipped += 1
            continue
        path.parent.mkdir(parents=True, exist_ok=True)
        build().save(path)
        made += 1
        print(f"만듦: {rel}")
    print(f"\n{made}개 만듦, {skipped}개는 이미 있어 건너뜀" + ("" if font(10) else "  (한글 글꼴을 찾지 못해 이름표 생략)"))
    return 0


if __name__ == "__main__":
    sys.exit(main())
