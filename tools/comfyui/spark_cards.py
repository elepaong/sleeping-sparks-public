#!/usr/bin/env python3
"""Queue spark card drafts to a local ComfyUI (WAI-Illustrious SDXL).

Usage (ComfyUI must be running on http://127.0.0.1:8188):
    python3 spark_cards.py                 # all sparks, 4 seeds each
    python3 spark_cards.py lumi hwaro -n 8 # chosen sparks, 8 seeds each
    python3 spark_cards.py --dry-run       # write the API workflow JSON only

Standard library only. Results land in ComfyUI's output folder as
spark_<id>_XXXXX_.png (1024x1024). Downscale to 512x512 and remove the
background afterwards (see design/local-generation.md).
"""
import argparse
import json
import random
import sys
import urllib.request
from pathlib import Path

CHECKPOINT = "waiIllustriousSDXL_v170.safetensors"

# Shared style, tuned to the ChatGPT drafts in Google Drive/시안:
# round chibi mascot, dashed brown ink outline, pastel watercolor, parchment.
STYLE = (
    "masterpiece, best quality, no humans, chibi mascot creature, solo, full body, "
    "round plump body, tiny stubby arms and feet, small black dot eyes, rosy blush, "
    "small open happy mouth, thin dashed warm-brown ink outline, soft watercolor, "
    "pastel colors, flat gouache shading, storybook illustration, simple design, "
    "centered, plain cream background, soft shadow under feet"
)
NEGATIVE = (
    "worst quality, low quality, lowres, human, girl, boy, realistic, 3d, photo, "
    "text, watermark, signature, frame, border, multiple views, sharp black lineart, "
    "glossy, nsfw"
)

# Design notes per spark (from art-direction.md and the Drive drafts).
SPARKS = {
    "jjirit": "pale sky-blue body like a candle flame with a round bottom, two short rounded triangular ears, curled wisp antenna with a tiny white spark at the tip, faint static glow",
    "lumi": "cream-yellow ball of warm light, short stem on top ending in a small clear crystal prism bud, faint tiny rainbow, shy and kind",
    "hwaro": "dark brown ember rock body with glowing orange cracks, small flame on top, tiny floating coals",
    "mag": "round body half red on top and half blue on the bottom like a magnet, tiny grey iron pebbles floating around",
    "mulmalle": "translucent pale aqua water-drop body, small water swirl on top, tiny splash droplets",
    "ullim": "small round bronze bell-shaped body, faint concentric sound-wave rings around it",
    "hwii": "cream round body wearing a green and white paper pinwheel on its head, small green leaves drifting",
    "gravi": "heavy round slate-purple stone body, a tiny orbiting pebble like a moon, slightly squashed",
}


def workflow(spark_id: str, seed: int, steps: int = 28, cfg: float = 5.5) -> dict:
    positive = f"{STYLE}, {SPARKS[spark_id]}"
    return {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": CHECKPOINT}},
        "2": {"class_type": "CLIPTextEncode", "inputs": {"text": positive, "clip": ["1", 1]}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"text": NEGATIVE, "clip": ["1", 1]}},
        "4": {"class_type": "EmptyLatentImage", "inputs": {"width": 1024, "height": 1024, "batch_size": 1}},
        "5": {
            "class_type": "KSampler",
            "inputs": {
                "model": ["1", 0], "positive": ["2", 0], "negative": ["3", 0],
                "latent_image": ["4", 0], "seed": seed, "steps": steps, "cfg": cfg,
                "sampler_name": "euler_ancestral", "scheduler": "normal", "denoise": 1.0,
            },
        },
        "6": {"class_type": "VAEDecode", "inputs": {"samples": ["5", 0], "vae": ["1", 2]}},
        "7": {"class_type": "SaveImage", "inputs": {"images": ["6", 0], "filename_prefix": f"spark_{spark_id}"}},
    }


def queue(wf: dict, host: str) -> str:
    req = urllib.request.Request(
        f"{host}/prompt",
        data=json.dumps({"prompt": wf}).encode(),
        headers={"Content-Type": "application/json"},
    )
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.load(r)["prompt_id"]


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("sparks", nargs="*", default=list(SPARKS), help="spark ids (default: all)")
    ap.add_argument("-n", "--count", type=int, default=4, help="seeds per spark")
    ap.add_argument("--host", default="http://127.0.0.1:8188")
    ap.add_argument("--dry-run", action="store_true", help="write workflow JSON instead of queuing")
    a = ap.parse_args()

    unknown = [s for s in a.sparks if s not in SPARKS]
    if unknown:
        print("unknown spark id:", ", ".join(unknown), "| known:", ", ".join(SPARKS))
        return 1

    if a.dry_run:
        out = Path(__file__).with_name("spark_card_api.json")
        out.write_text(json.dumps(workflow(a.sparks[0], 42), indent=2, ensure_ascii=False))
        print("wrote", out)
        return 0

    total = 0
    for sid in a.sparks:
        for _ in range(a.count):
            seed = random.randint(0, 2**32 - 1)
            try:
                pid = queue(workflow(sid, seed), a.host)
            except OSError as e:
                print(f"ComfyUI not reachable at {a.host}: {e}")
                return 1
            print(f"queued {sid} seed={seed} id={pid}")
            total += 1
    print(f"{total} jobs queued (~2 min each on this Mac at 1024x1024, 28 steps)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
