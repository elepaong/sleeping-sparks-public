#!/usr/bin/env python3
"""data/ 와 assets/ 를 Unity가 읽는 자리(unity/Assets/Resources/Sparks/)로 옮긴다.

사용: python3 tools/unity/sync.py
  data/**/*.json      → Resources/Sparks/data/**/*.json      (스테이지 테마 data/stages 와 _로 시작하는 파일은 빼고)
  assets/**/*.png     → Resources/Sparks/assets/**/*.bytes   (임포트 설정의 영향을 받지 않게 PNG 바이트 그대로)
  목록과 그림 크기     → Resources/Sparks/index.json
원본은 data/ 와 assets/ 다. Resources/Sparks/ 는 옮긴 결과라 손으로 고치지 않는다 — 고친 뒤에는 이 스크립트를 다시 돌린다.
의존: 없음 (표준 라이브러리)
"""
import json
import pathlib
import shutil
import struct
import sys

sys.stdout.reconfigure(encoding="utf-8")
ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "unity" / "Assets" / "Resources" / "Sparks"


def png_size(path):
    with open(path, "rb") as f:
        head = f.read(24)
    if head[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError(f"PNG가 아님: {path}")
    return list(struct.unpack(">II", head[16:24]))


def main():
    for sub in ("data", "assets"):
        if (OUT / sub).exists():
            shutil.rmtree(OUT / sub)
    data_files = []
    for p in sorted((ROOT / "data").rglob("*.json")):
        rel = p.relative_to(ROOT).as_posix()
        if rel.startswith("data/stages/") or p.name.startswith("_"):
            continue
        json.loads(p.read_text(encoding="utf-8"))   # 읽히지 않는 파일은 여기서 멈춘다
        dst = OUT / rel
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(p, dst)
        data_files.append(rel)
    assets = {}
    for p in sorted((ROOT / "assets").rglob("*.png")):
        rel = p.relative_to(ROOT).as_posix()
        dst = (OUT / rel).with_suffix(".bytes")
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(p, dst)
        assets[rel] = png_size(p)
    (OUT / "index.json").write_text(json.dumps({"data": data_files, "assets": assets}, ensure_ascii=False, indent=1), encoding="utf-8")
    print(f"데이터 {len(data_files)}개, 그림 {len(assets)}개 → {OUT.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
