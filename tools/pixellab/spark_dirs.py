#!/usr/bin/env python3
"""스파크 32×32 그림(아래 방향)을 PixelLab에서 여러 방향으로 돌린다 (2026-11-22).

create_character(mode="v3", reference_image=32×32 B안, n_directions=8) — 32×32는 1종에 생성 1회.
v3는 참고 그림을 '아래(south)'로 받아 나머지 방향을 그린다. 게임은 5방향(아래·아래오른쪽·오른쪽·위오른쪽·위)만 쓰고
왼쪽 셋은 좌우 뒤집기(tools/sprites/spark_sheets.py) — 루카와 같은 방식.
생성 전에 남은 횟수와 비용을 사람에게 먼저 알린다 (CLAUDE.md). 체험 플랜은 동시에 1개.
결과: design/refs/pixellab_2026-11-22/spark_<id>_dirs/<방향>.png  (5방향, 32×32) + character_id.txt
쓰는 법: python3 tools/pixellab/spark_dirs.py jjirit lumi ...
"""
import base64
import io
import json
import pathlib
import re
import subprocess
import sys
import time
import urllib.request
import zipfile

from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[2]
SRC = ROOT / "design/refs/pixellab_2026-11-21"
OUT = ROOT / "design/refs/pixellab_2026-11-22"
FIVE = ["south", "south-east", "east", "north-east", "north"]
PL = [sys.executable, str(ROOT / "tools/pixellab/plmcp.py"), "call"]
DESC = {   # 스파크 모습 (tools/pixellab/spark32.py와 같은 설명)
    "jjirit": "tiny cute spark spirit of static electricity: a round amber stone body with a fluffy white wool tuft on top, two small soft ears, tiny blue electric sparks, small feet",
    "lumi": "tiny cute spark spirit of light: a small brass lantern with a ring handle and cone cap, glowing yellow glass body with a face, small feet",
    "hwaro": "tiny cute spark spirit of heat: a round copper stove with a chimney and a glowing orange fire door, a face, small feet",
    "mag": "tiny cute spark spirit of magnetism: a red and blue horseshoe magnet with iron nails on the tips, a face, small feet",
    "mulmalle": "tiny cute spark spirit of water pressure: a teal bronze hand pump on a small wooden tub, a face, small feet",
    "ullim": "tiny cute spark spirit of sound: a bronze hand bell with green patina and a ring handle, a face, small feet",
    "hwii": "tiny cute spark spirit of wind: a small windmill with cream cloth sails on a wooden body, a face, small feet",
    "gravi": "tiny cute spark spirit of gravity: a silver balance scale with two pans and a pale blue glass body with a face",
}
NAMES = {"jjirit": "Jjirit", "lumi": "Lumi", "hwaro": "Hwaro", "mag": "Mag", "mulmalle": "Mulmalle",
         "ullim": "Ullim", "hwii": "Hwii", "gravi": "Gravi"}


def call(tool, args):
    return subprocess.run(PL + [tool, json.dumps(args)], capture_output=True, text=True, timeout=600).stdout


def fit32(im):
    """돌려 받은 칸이 32보다 크면 그림을 32 안에 바닥 가운데로 맞춘다 (줄이지 않고 자르기만, 넘치면 알림)."""
    im = im.convert("RGBA")
    im.putalpha(im.getchannel("A").point(lambda v: 255 if v > 100 else 0))
    if im.size == (32, 32):
        return im
    b = im.getbbox()
    body = im.crop(b)
    if body.width > 32 or body.height > 32:
        print("  ! 그림이 32보다 큼:", body.size, "→ 줄임")
        s = 32 / max(body.size)
        body = body.resize((max(1, round(body.width * s)), max(1, round(body.height * s))), Image.NEAREST)
    out = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    out.alpha_composite(body, ((32 - body.width) // 2, 32 - body.height))
    return out


def one(sid):
    d = OUT / f"spark_{sid}_dirs"
    if (d / "north.png").exists():
        print(sid, "있음, 건너뜀")
        return
    d.mkdir(parents=True, exist_ok=True)
    ref = Image.open(SRC / f"spark_{sid}_32_b.png").convert("RGBA")
    b = io.BytesIO(); ref.save(b, "PNG")
    r = call("create_character", {"mode": "v3", "name": f"{NAMES[sid]} spark 32", "n_directions": 8,
                                  "description": DESC[sid],
                                  "view": "low top-down", "reference_image_base64": base64.b64encode(b.getvalue()).decode()})
    m = re.search(r"([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})", r)
    if not m:
        print(sid, "실패:", r.strip()[:400]); sys.exit(1)
    cid = m.group(1)
    (d / "character_id.txt").write_text(cid + "\n")
    print(sid, "character", cid)
    for _ in range(60):
        info = call("get_character", {"character_id": cid})
        if "status: completed" in info:
            break
        time.sleep(15)
    else:
        print(sid, "끝나지 않음 — 나중에 다시 돌리면 이어서 받는다"); sys.exit(1)
    data = urllib.request.urlopen(f"https://api.pixellab.ai/mcp/characters/{cid}/download", timeout=120).read()
    z = zipfile.ZipFile(io.BytesIO(data))
    for name in z.namelist():
        mm = re.search(r"rotations/([a-z-]+)\.png$", name)
        if mm and mm.group(1) in FIVE:
            fit32(Image.open(io.BytesIO(z.read(name)))).save(d / f"{mm.group(1)}.png")
    missing = [x for x in FIVE if not (d / f"{x}.png").exists()]
    print(sid, "→", d.relative_to(ROOT), "빠짐:" if missing else "", missing or "")


if __name__ == "__main__":
    for s in sys.argv[1:]:
        one(s)
