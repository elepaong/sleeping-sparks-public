#!/usr/bin/env python3
"""D-LIGHT-01 (거울에 비스듬히): 반사 벡터를 직접 계산해 답을 확인한다.

사용: python tools/verify/D-LIGHT-01.py [저장소 루트]
거울을 x축에 놓고, 거울면과 30°를 이루며 내려오는 빛을 반사시킨다
(반사: 거울면에 수직인 성분만 뒤집는다). 나오는 빛이 거울면과 이루는 각을 구해
선택지 가운데 정확히 하나와 같은지 본다. 함정 선택지(60°, 120°)가 무엇인지도 계산한다.
"""
import json
import math
import pathlib
import sys

sys.stdout.reconfigure(encoding="utf-8")


def main():
    root = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
    pz = json.loads((root / "data/puzzles/D-LIGHT-01.json").read_text(encoding="utf-8"))
    given = 30.0
    d = (math.cos(math.radians(given)), -math.sin(math.radians(given)))  # 거울(y=0)로 내려오는 빛
    r = (d[0], -d[1])  # y 성분만 뒤집힘
    out_surface = math.degrees(math.atan2(abs(r[1]), abs(r[0])))  # 나오는 빛과 거울면
    from_normal = 90.0 - given  # 수직선(법선)과 빛 사이
    between_rays = math.degrees(math.acos(-(d[0] * r[0] + d[1] * r[1])))  # 들어가는 빛줄기와 나오는 빛줄기 사이
    print(f"나오는 빛과 거울면: {out_surface:.1f}°, 법선과의 각: {from_normal:.1f}°, 두 빛줄기 사이: {between_rays:.1f}°")

    answer = f"{round(out_surface)}°"
    ok = True
    if answer != pz["answer"]:
        print(f"[실패] 계산한 답 {answer} != 데이터의 답 {pz['answer']}")
        ok = False
    if pz["input"]["options"].count(answer) != 1:
        print("[실패] 선택지에 정답이 정확히 한 번 들어 있지 않음")
        ok = False
    if abs(from_normal - 60) > 1e-6 or abs(between_rays - 120) > 1e-6:
        print("[실패] 해설에 적은 60°·120°의 뜻이 계산과 다름")
        ok = False
    print("통과" if ok else "실패")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
