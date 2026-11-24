#!/usr/bin/env python3
"""D-LIGHT-04 (세 개의 볼록 렌즈): 렌즈 제작자 공식으로 초점 거리 순서를 확인한다.

사용: python tools/verify/D-LIGHT-04.py [저장소 루트]
지름과 가장자리 두께가 같은 양면 볼록 렌즈에서, 가운데 두께가 두꺼울수록 면의 곡률 반지름 R이 작다.
  한쪽 면의 볼록한 높이 h = (가운데 두께 - 가장자리 두께) / 2,  R = (a² + h²) / (2h)  (a = 반지름)
  두꺼운 렌즈 공식: 1/f = (n-1) [ 2/R - (n-1) d / (n R²) ]   (d = 가운데 두께, 양면 대칭)
여러 유리 굴절률과 치수에 대해 '두꺼울수록 초점이 가깝다'가 유지되는지 본다.
"""
import itertools
import json
import pathlib
import sys

sys.stdout.reconfigure(encoding="utf-8")


def focal(n, a, edge, center):
    h = (center - edge) / 2.0
    r = (a * a + h * h) / (2.0 * h)
    inv = (n - 1.0) * (2.0 / r - (n - 1.0) * center / (n * r * r))
    return 1.0 / inv


def main():
    root = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
    pz = json.loads((root / "data/puzzles/D-LIGHT-04.json").read_text(encoding="utf-8"))
    ok = True
    checked = 0
    # 굴절률 1.45~1.9, 반지름 20~60mm, 가장자리 2mm, 가운데 두께 세 가지(얇음 < 중간 < 두꺼움)
    for n, a in itertools.product((1.45, 1.52, 1.7, 1.9), (20.0, 40.0, 60.0)):
        for thin, mid, thick in ((4, 6, 8), (3, 8, 14), (5, 10, 20), (2.5, 3, 3.5)):
            fa, fb, fc = focal(n, a, 2.0, thin), focal(n, a, 2.0, mid), focal(n, a, 2.0, thick)
            checked += 1
            if not (0 < fc < fb < fa):
                print(f"[실패] n={n} a={a} 두께 {thin}/{mid}/{thick}: f = {fa:.1f}, {fb:.1f}, {fc:.1f}")
                ok = False
    fa, fb, fc = focal(1.52, 40.0, 2.0, 4), focal(1.52, 40.0, 2.0, 6), focal(1.52, 40.0, 2.0, 8)
    print(f"예 (n=1.52, 지름 80mm): 가 {fa:.0f}mm, 나 {fb:.0f}mm, 다 {fc:.0f}mm  — {checked}가지 조합 확인")

    names = {"가": fa, "나": fb, "다": fc}
    order = [k for k, _ in sorted(names.items(), key=lambda kv: kv[1])]
    data_order = [s.split(" ")[0] for s in pz["answer"]]
    if order != data_order:
        print(f"[실패] 계산한 순서 {order} != 데이터의 답 {data_order}")
        ok = False
    if sorted(pz["answer"]) != sorted(pz["input"]["options"]):
        print("[실패] 답이 선택지를 나열한 순서가 아님")
        ok = False
    print("통과" if ok else "실패")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
