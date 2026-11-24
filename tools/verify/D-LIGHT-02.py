#!/usr/bin/env python3
"""D-LIGHT-02 (거울의 방): 'figure'의 배치대로 빛을 따라가 나가는 창을 확인한다.

사용: python tools/verify/D-LIGHT-02.py [저장소 루트]
확인: 빛이 돌지 않고 밖으로 나가는지, 나간 자리가 창 A~D 가운데 정확히 하나인지,
      그 창이 데이터의 답과 같은지, 창끼리·입구와 자리가 겹치지 않는지,
      해설에 적은 반사 횟수(6)·길이 겹치는 횟수(3)가 맞는지.
"""
import json
import pathlib
import sys

sys.stdout.reconfigure(encoding="utf-8")

DIRS = {"E": (1, 0), "W": (-1, 0), "N": (0, -1), "S": (0, 1)}
# '/'는 왼쪽 아래↗오른쪽 위로 기운 거울, '\\'는 왼쪽 위↘오른쪽 아래 (화면에서 y는 아래가 +)
TURN = {
    "/": {"E": "N", "N": "E", "W": "S", "S": "W"},
    "\\": {"E": "S", "S": "E", "W": "N", "N": "W"},
}
OPPOSITE = {"E": "W", "W": "E", "N": "S", "S": "N"}


def trace(fig):
    cols, rows = fig["cols"], fig["rows"]
    mirrors = {tuple(m["cell"]): m["kind"] for m in fig["mirrors"]}
    side, idx = fig["entry"]["side"], fig["entry"]["index"]
    heading = OPPOSITE[side]  # 서쪽 벽에서 들어오면 동쪽으로 간다
    cell = {"W": (0, idx), "E": (cols - 1, idx), "N": (idx, 0), "S": (idx, rows - 1)}[side]
    path, bounces, seen = [], 0, set()
    while 0 <= cell[0] < cols and 0 <= cell[1] < rows:
        if (cell, heading) in seen:
            return None, path, bounces  # 같은 칸을 같은 방향으로 다시 지남 = 끝없이 돈다
        seen.add((cell, heading))
        path.append(cell)
        if cell in mirrors:
            heading = TURN[mirrors[cell]][heading]
            bounces += 1
        dx, dy = DIRS[heading]
        prev, cell = cell, (cell[0] + dx, cell[1] + dy)
    exit_at = (heading, prev[1] if heading in "EW" else prev[0])
    return exit_at, path, bounces


def main():
    root = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
    pz = json.loads((root / "data/puzzles/D-LIGHT-02.json").read_text(encoding="utf-8"))
    fig = pz["figure"]
    ok = True

    cells = [tuple(m["cell"]) for m in fig["mirrors"]]
    if len(cells) != len(set(cells)):
        print("[실패] 같은 칸에 거울이 둘")
        ok = False
    places = [(e["side"], e["index"]) for e in fig["exits"].values()] + [(fig["entry"]["side"], fig["entry"]["index"])]
    if len(places) != len(set(places)):
        print("[실패] 창이나 입구의 자리가 겹침")
        ok = False

    exit_at, path, bounces = trace(fig)
    crossings = len(path) - len(set(path))
    print(f"지나간 칸: {path}")
    print(f"반사 {bounces}번, 지나온 길과 겹친 칸 {crossings}번, 나간 곳: {exit_at}")
    if exit_at is None:
        print("[실패] 빛이 방 안에서 끝없이 돈다")
        return 1
    hit = [name for name, e in fig["exits"].items() if (e["side"], e["index"]) == exit_at]
    if len(hit) != 1:
        print(f"[실패] 빛이 나간 자리에 창이 {len(hit)}개")
        ok = False
    elif hit[0] != pz["answer"]:
        print(f"[실패] 나간 창 {hit[0]} != 데이터의 답 {pz['answer']}")
        ok = False
    if pz["input"]["options"].count(pz["answer"]) != 1 or sorted(pz["input"]["options"]) != sorted(fig["exits"]):
        print("[실패] 선택지와 창 이름이 맞지 않음")
        ok = False
    if bounces != 6 or crossings != 3:
        print("[실패] 해설·힌트에 적은 '여섯 번 꺾임', '세 번 가로지름'과 다름")
        ok = False
    print("통과" if ok else "실패")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
