#!/usr/bin/env python3
"""D-LIGHT-03 (두 거울의 모서리): 여러 각도로 빛을 쏘아 두 번 반사된 뒤의 방향을 확인한다.

사용: python tools/verify/D-LIGHT-03.py [저장소 루트]
거울 하나는 x축(y=0, x>0), 다른 하나는 y축(x=0, y>0)에 세운다(직각).
바닥과 나란한 빛을 1°~89° 사이의 여러 각도·여러 출발점에서 모서리 안쪽으로 쏜다.
두 거울에 한 번씩 반사된 뒤의 방향이 언제나 처음의 정반대인지 본다.
또, 거울 사이 각이 90°가 아니면(예: 80°, 60°) 그렇지 않다는 것도 확인한다
(문제의 '직각'이 꼭 필요한 조건인지).
"""
import json
import math
import pathlib
import sys

sys.stdout.reconfigure(encoding="utf-8")


def reflect(d, n):
    dot = d[0] * n[0] + d[1] * n[1]
    return (d[0] - 2 * dot * n[0], d[1] - 2 * dot * n[1])


def twice(d, wedge_deg):
    """거울 1은 x축, 거울 2는 x축에서 wedge_deg만큼 돌아간 선. 두 거울에 한 번씩 반사."""
    n1 = (0.0, 1.0)
    a = math.radians(wedge_deg)
    n2 = (math.sin(a), -math.cos(a))
    return reflect(reflect(d, n1), n2)


def main():
    root = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
    pz = json.loads((root / "data/puzzles/D-LIGHT-03.json").read_text(encoding="utf-8"))
    ok = True

    worst = 0.0
    for deg in range(1, 90):
        t = math.radians(deg)
        d = (-math.cos(t), -math.sin(t))  # 모서리 안쪽(왼쪽 아래)으로 가는 빛
        out = twice(d, 90)
        worst = max(worst, abs(out[0] + d[0]) + abs(out[1] + d[1]))
    print(f"직각 거울: 1°~89° 모든 각도에서 정반대 방향과의 차이 최대 {worst:.2e}")
    if worst > 1e-9:
        print("[실패] 직각 거울인데 정반대 방향이 아닌 경우가 있음")
        ok = False

    # 직접 길을 따라가 두 거울에 실제로 닿는지도 본다 (한 예: 출발점 (3, 1), 30°)
    p, d = (3.0, 1.0), (-math.cos(math.radians(30)), -math.sin(math.radians(30)))
    s = -p[1] / d[1]
    hit1 = (p[0] + s * d[0], 0.0)
    d1 = reflect(d, (0.0, 1.0))
    s = -hit1[0] / d1[0]
    hit2 = (0.0, hit1[1] + s * d1[1])
    print(f"예: 첫 반사 {hit1[0]:.3f},0  둘째 반사 0,{hit2[1]:.3f}")
    if not (hit1[0] > 0 and hit2[1] > 0):
        print("[실패] 예시 빛이 두 거울에 차례로 닿지 않음")
        ok = False

    for wedge in (80, 60):
        d = (-math.cos(math.radians(30)), -math.sin(math.radians(30)))
        out = twice(d, wedge)
        diff = abs(out[0] + d[0]) + abs(out[1] + d[1])
        print(f"거울 사이 {wedge}°: 정반대 방향과의 차이 {diff:.3f}")
        if diff < 1e-6:
            print("[실패] 직각이 아니어도 되돌아감 — 문제 조건이 헐거움")
            ok = False

    if pz["answer"] != pz["input"]["options"][0] or "되돌아간다" not in pz["answer"]:
        print("[실패] 데이터의 답이 '되돌아간다' 선택지가 아님")
        ok = False
    print("통과" if ok else "실패")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
