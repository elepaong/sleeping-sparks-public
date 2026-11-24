#!/usr/bin/env python3
"""D-STATIC-02 (매달린 세 구슬): 정답이 하나뿐인지 모든 경우를 세어 확인한다.

사용: python tools/verify/D-STATIC-02.py [저장소 루트]
구슬 A, B, C는 각각 +, -, 0(전기를 띠지 않음) 가운데 하나다(27가지).
문제의 규칙과 관찰(A-B 밀어냄, B-C 끌어당김)에 맞는 경우만 남기고,
남은 모든 경우에서 A-C의 결과가 같은지 본다.
"""
import itertools
import json
import pathlib
import sys

sys.stdout.reconfigure(encoding="utf-8")

ATTRACT, REPEL, NOTHING = "서로 끌어당긴다", "서로 밀어낸다", "아무 일도 일어나지 않는다"
UNKNOWN = "이것만으로는 알 수 없다"


def between(x, y):
    """문제에 적힌 규칙 네 줄."""
    if x == "0" and y == "0":
        return NOTHING
    if x == y:
        return REPEL
    return ATTRACT  # 다른 전기끼리, 또는 띤 것과 띠지 않은 것


def main():
    root = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
    pz = json.loads((root / "data/puzzles/D-STATIC-02.json").read_text(encoding="utf-8"))

    cases = [
        (a, b, c)
        for a, b, c in itertools.product("+-0", repeat=3)
        if between(a, b) == REPEL and between(b, c) == ATTRACT
    ]
    results = {between(a, c) for a, _, c in cases}
    c_states = {c if c == "0" else ("B와 다른 전기") for _, _, c in cases}
    print(f"관찰에 맞는 경우 {len(cases)}가지: {cases}")
    print(f"C가 될 수 있는 상태: {sorted(c_states)}")
    print(f"A-C 결과: {sorted(results)}")

    ok = True
    if not cases:
        print("[실패] 관찰에 맞는 경우가 없음 (문제가 모순)")
        ok = False
    if len(results) != 1:
        print("[실패] A-C 결과가 하나로 정해지지 않음")
        ok = False
    answer = next(iter(results)) if len(results) == 1 else UNKNOWN
    if answer != pz["answer"]:
        print(f"[실패] 계산한 답 '{answer}' != 데이터의 답 '{pz['answer']}'")
        ok = False
    if pz["input"]["options"].count(answer) != 1:
        print("[실패] 선택지에 정답이 정확히 한 번 들어 있지 않음")
        ok = False
    if len(c_states) < 2:
        print("[실패] C의 상태가 하나로 정해짐 — '알 수 없다' 선택지가 함정 구실을 못 함")
        ok = False
    print("통과" if ok else "실패")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
