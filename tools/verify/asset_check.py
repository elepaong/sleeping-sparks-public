#!/usr/bin/env python3
"""assets/ 아래 PNG가 CLAUDE.md 에셋 규격표와 맞는지 한 번에 점검한다.

사용: python3 tools/verify/asset_check.py [저장소 루트]
점검: 파일 크기, 반투명 픽셀(도트 에셋은 알파 0 또는 255만), 색 개수(배경),
      시트의 빈 프레임. 규격표에 없는 위치의 PNG는 '규격 없음'으로 알린다.
타일(보류)은 32의 배수인지만 본다. 자세한 타일 점검은 tile_check.py.
의존: Pillow
"""
import fnmatch
import pathlib
import sys

from PIL import Image

sys.stdout.reconfigure(encoding="utf-8")  # 한글이 아닌 콘솔(예: 일본어 Windows)에서도 출력되게

# (경로 패턴, 설명, 규칙). 위에서부터 처음 맞는 줄을 쓴다.
# own_outline: 제 색 외곽선(가장자리를 어두운 한 색으로 두르지 않음 — design/art-direction.md '도트 그림체 기준')
# old_size: 규격이 바뀌기 전 크기의 임시본(통과시키되 요약에 알림 — 필드 캐릭터 32×48 → 48×72 2026-11-11, 스파크 24×24 → 32×32 2026-11-21, 방향 없는 한 줄 시트 → 8방향 2026-11-22, 캐릭터 걷기 4 → 8프레임 2026-11-24)
# size: 정확한 크기 / min_size: 이 크기 이상(큰 한 장 배경 허용) / multiple: 배수 / frame: 시트 한 칸 크기 / pixel: 도트 에셋(반투명 금지)
SPECS = [
    ("assets/backgrounds/*.png", "필드 배경", {"min_size": (640, 360), "pixel": True, "max_colors": 64}),
    ("assets/chars/*_walk.png", "필드 캐릭터 걷기", {"size": (384, 576), "old_size": [(192, 576), (128, 384)], "pixel": True, "frame": (48, 72)}),
    ("assets/chars/*_idle.png", "필드 캐릭터 대기", {"size": (96, 576), "old_size": (64, 384), "pixel": True, "frame": (48, 72)}),
    ("assets/sparks/*_idle.png", "스파크 대기", {"size": (128, 256), "old_size": [(128, 32), (96, 24)], "pixel": True, "frame": (32, 32), "own_outline": True}),
    ("assets/sparks/*_burst.png", "스파크 발동", {"size": (192, 256), "old_size": [(192, 32), (144, 24)], "pixel": True, "frame": (32, 32), "own_outline": True}),
    ("assets/sparks/*_card.png", "스파크 도감", {"size": (512, 512)}),
    ("assets/portraits/*.png", "대화 초상화", {"size": (512, 512)}),
    ("assets/puzzles/*.png", "문 퍼즐 그림", {"size": (1200, 800)}),
    ("assets/parchment/*.png", "양피지 삽화", {"size": (800, 600)}),
    ("assets/tiles/*.png", "타일 (보류)", {"multiple": 32, "pixel": True}),
    ("assets/props/*.png", "소품 (가안)", {"multiple": 32, "pixel": True}),
]


def check(path: pathlib.Path, rel: str):
    """문제 목록(빈 목록이면 통과)과 요약 문자열을 돌려준다."""
    spec = next(((d, r) for pat, d, r in SPECS if fnmatch.fnmatch(rel, pat)), None)
    im = Image.open(path).convert("RGBA")
    w, h = im.size
    if spec is None:
        return ["규격표에 없는 위치"], f"{w}×{h}"
    desc, rule = spec
    problems = []
    olds = rule.get("old_size") or []
    olds = olds if isinstance(olds, list) else [olds]
    old = "size" in rule and (w, h) in olds
    if "size" in rule and (w, h) != rule["size"] and not old:
        problems.append(f"크기 {w}×{h}, 규격 {rule['size'][0]}×{rule['size'][1]}")
    if "min_size" in rule and (w < rule["min_size"][0] or h < rule["min_size"][1]):
        problems.append(f"크기 {w}×{h}, 최소 {rule['min_size'][0]}×{rule['min_size'][1]}")
    if "multiple" in rule and (w % rule["multiple"] or h % rule["multiple"]):
        problems.append(f"크기 {w}×{h}가 {rule['multiple']}의 배수가 아님")
    raw = im.tobytes()
    data = [tuple(raw[i:i + 4]) for i in range(0, len(raw), 4)]
    info = f"{desc} {w}×{h}"
    if old:
        info += f" (옛 규격 임시본, {rule['size'][0]}×{rule['size'][1]}로 교체 예정)"
    if rule.get("pixel"):
        semi = sum(1 for p in data if 0 < p[3] < 255)
        if semi:
            problems.append(f"반투명 픽셀 {semi}개")
        colors = len({p[:3] for p in data if p[3]})
        info += f", 색 {colors}개"
        if "max_colors" in rule and colors > rule["max_colors"]:
            problems.append(f"색 {colors}개, 최대 {rule['max_colors']}개")
    if rule.get("own_outline"):
        px = im.load()
        edge = {}
        for y in range(h):
            for x in range(w):
                if px[x, y][3] and any(
                    not (0 <= x + dx < w and 0 <= y + dy < h) or not px[x + dx, y + dy][3]
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                ):
                    edge[px[x, y][:3]] = edge.get(px[x, y][:3], 0) + 1
        if edge:
            top = max(edge, key=edge.get)
            share = edge[top] / sum(edge.values())
            if share > 0.5 and 0.299 * top[0] + 0.587 * top[1] + 0.114 * top[2] < 50:
                problems.append(
                    f"가장자리의 {share:.0%}가 어두운 한 색 #{top[0]:02x}{top[1]:02x}{top[2]:02x} — 제 색 외곽선으로 (tools/sprites/own_outline.py)"
                )
    if "frame" in rule and "size" in rule and (w, h) == rule["size"]:
        fw, fh = rule["frame"]
        px = im.load()
        empty = [
            (x // fw, y // fh)
            for y in range(0, h, fh)
            for x in range(0, w, fw)
            if not any(px[i, j][3] for j in range(y, y + fh) for i in range(x, x + fw))
        ]
        if empty:
            problems.append(f"빈 프레임 {len(empty)}개 (열,행): {empty}")
    return problems, info


def main():
    root = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
    files = sorted((root / "assets").rglob("*.png"))
    if not files:
        print("assets/ 아래에 PNG가 없음")
        return 1
    failed = 0
    for f in files:
        rel = f.relative_to(root).as_posix()
        problems, info = check(f, rel)
        if problems:
            failed += 1
            print(f"[실패] {rel}  ({info})")
            for p in problems:
                print(f"        - {p}")
        else:
            print(f"[통과] {rel}  ({info})")
    print(f"\n{len(files)}개 중 {len(files) - failed}개 통과, {failed}개 실패")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
