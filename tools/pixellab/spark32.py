#!/usr/bin/env python3
"""채택한 스파크 디자인(64×64)을 PixelLab pixflux로 32×32로 다시 그린다 (B안 방법, 2026-11-21에 찌릿·루미로 정함).

바탕 그림 = 64 그림을 32로 줄인 것, 보존 강도 320, 색은 원본 64 그림에서, 제 색 외곽선·basic shading.
1종에 생성 1회. 생성 전에 남은 횟수와 비용을 사람에게 먼저 알린다 (CLAUDE.md).
결과: design/refs/pixellab_2026-11-21/spark_<id>_32_b.png  (같은 이름이 있으면 건너뜀)
쓰는 법: python3 tools/pixellab/spark32.py hwaro mag ...
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

from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[2]
SRC = ROOT / "design/refs/pixellab_2026-11-16"
OUT = ROOT / "design/refs/pixellab_2026-11-21"
PICKED = {"jjirit": 1, "lumi": 2, "hwaro": 1, "mag": 1, "mulmalle": 1, "ullim": 1, "hwii": 1, "gravi": 2}   # art-direction.md "채택 (2026-11-16)"
TAIL = ", dot eyes, small smile, rosy cheeks, tiny feet, color-matched outline (no black outline)"
DESC = {
    "jjirit": "tiny cute spark spirit of static electricity: a round warm amber stone body with a fluffy white wool tuft on top, two small soft ears, tiny blue electric sparks around it",
    "lumi": "tiny cute spark spirit of light: a small brass lantern with a ring handle and cone cap, glowing pale yellow glass body with a face, short orange light rays around it",
    "hwaro": "tiny cute spark spirit of heat: a round copper stove with a riveted lid and a small chimney puffing a curl of smoke, a glowing orange grated fire door on its belly, a face on the copper body",
    "mag": "tiny cute spark spirit of magnetism: a horseshoe magnet with one red pole and one blue pole, iron nails stuck to the pole tips, a brass band around the middle, a face on the curved bottom",
    "mulmalle": "tiny cute spark spirit of water pressure: a teal bronze hand pump with a curved handle and a spout dripping one water drop, standing on a small wooden tub, a face on the pump body",
    "ullim": "tiny cute spark spirit of sound: a bronze hand bell with green patina and a ring handle on top, a face on the bell body",
    "hwii": "tiny cute spark spirit of wind: a small windmill with four cream cloth sails and a wooden body, little green leaves and a pale wind swirl around it, a face on the wooden body",
    "gravi": "tiny cute spark spirit of gravity: a silver balance scale with two hanging pans, one pan holding a small brass weight, a round pale blue glass body at the center with a face",
}
PL = [sys.executable, str(ROOT / "tools/pixellab/plmcp.py"), "call"]


def b64(im):
    b = io.BytesIO()
    im.save(b, "PNG")
    return base64.b64encode(b.getvalue()).decode()


def one(sid):
    out = OUT / f"spark_{sid}_32_b.png"
    if out.exists():
        print(sid, "있음, 건너뜀")
        return
    src = Image.open(SRC / f"spark_{sid}_{PICKED[sid]}.png").convert("RGBA")
    src.putalpha(src.getchannel("A").point(lambda v: 255 if v > 100 else 0))
    small = src.resize((32, 32), Image.BOX)
    small.putalpha(small.getchannel("A").point(lambda v: 255 if v > 110 else 0))
    args = {"description": DESC[sid] + TAIL, "init_image_base64": b64(small), "init_image_strength": 320,
            "color_image_base64": b64(src), "no_background": True, "outline": "selective outline",
            "shading": "basic shading", "detail": "medium detail", "view": "low top-down"}
    r = subprocess.run(PL + ["create_image_pixflux", json.dumps(args)], capture_output=True, text=True, timeout=300).stdout
    m = re.search(r"job_id[=:]\s*\"?([0-9a-f-]{36})", r)
    if not m:
        print(sid, "실패:", r.strip()[:300])
        sys.exit(1)
    url = f"https://api.pixellab.ai/mcp/images/{m.group(1)}/download"
    for _ in range(40):
        try:
            data = urllib.request.urlopen(url, timeout=60).read()
            out.write_bytes(data)
            print(sid, "→", out.relative_to(ROOT))
            return
        except Exception:
            time.sleep(10)
    print(sid, "받지 못함:", url)
    sys.exit(1)


if __name__ == "__main__":
    for s in sys.argv[1:]:
        one(s)
