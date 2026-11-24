#!/usr/bin/env python3
"""필드 퍼즐을 모든 경우로 풀어 본다 (data/zones/*.json + data/interactions.json).

사용: python tools/verify/field_check.py [저장소 루트]

빛 퍼즐 (칸이 있는 지역): 등잔 받침(어느 것에 루미를 놓나) × 거울 방향(모든 조합) × 집광기 위치(없음 또는 렌즈 틀 하나)를
모두 계산해, 퍼즐마다
  - 풀리는 경우가 있는지,
  - 풀리는 경우의 빛길이 하나뿐인지 (빛이 닿지 않는 거울의 방향은 상관없으므로 길로 센다),
  - 처음 상태로는 풀리지 않는지
를 확인한다. 한 번 켜진 문양은 계속 켜져 있으므로 퍼즐은 하나씩 따로 본다.
빛줄기 규칙은 scripts/field/beam.gd와 같아야 한다 — 한쪽을 고치면 다른 쪽도 고친다.

배치 퍼즐 (찌릿): 스파크를 놓을 수 있는 자리마다 상호작용표를 적용해, 풀리는 자리가 정확히 하나인지 본다.

그 밖: 발명품 비용을 문 퍼즐 에너지의 최소치(50%)로도 낼 수 있는지.
"""
import itertools
import json
import pathlib
import sys

sys.stdout.reconfigure(encoding="utf-8")

DIRS = {"E": (1, 0), "W": (-1, 0), "N": (0, -1), "S": (0, 1)}
TURN = {
    "/": {"E": "N", "N": "E", "W": "S", "S": "W"},
    "\\": {"E": "S", "S": "E", "W": "N", "N": "W"},
}
errors = []


def err(msg):
    errors.append(msg)
    print(f"[실패] {msg}")


def find_row(rows, obj_type, obj_state, spark, trigger, inventions):
    for r in rows:
        if r.get("object") != obj_type or "effect" not in r or r.get("trigger") != trigger:
            continue
        if spark not in r.get("sparks", []):
            continue
        if "object_state" in r and r["object_state"] != obj_state:
            continue
        if all(n in inventions for n in r.get("requires", [])):
            return r
    return None


def trace(grid, by_cell, start, heading, focal, rows, inventions):
    """beam.gd의 trace와 같은 계산. (지나간 칸 목록, [(사물 id, 줄, 빛 상태)])"""
    path, hits = [tuple(start)], []
    status, remain = "plain", 0
    cell = tuple(start)
    for _ in range(200):
        dx, dy = DIRS[heading]
        cell = (cell[0] + dx, cell[1] + dy)
        if any(b[0] <= cell[0] <= b[2] and b[1] <= cell[1] <= b[3] for b in grid.get("blocked", [])):
            break
        if status == "converging":
            remain -= 1
            if remain <= 0:
                status = "focused"
        elif status == "focused":
            status = "spread"
        path.append(cell)
        if not (0 <= cell[0] < grid["cols"] and 0 <= cell[1] < grid["rows"]):
            break
        o = by_cell.get(cell)
        if o is None:
            continue
        row = find_row(rows, o["type"], o["state"], "lumi", "beam", inventions)
        if row is None:
            if o.get("blocks_beam"):
                break
            continue
        kind = row["effect"].get("type")
        if kind == "reflect":
            heading = TURN[o["state"]][heading]
        elif kind == "focus":
            if status == "plain":
                status, remain = "converging", focal
        else:
            hits.append((o["id"], row, status))
            break
    return path, hits


def check_beam_zone(zone, rows, inventions_data):
    grid = zone["grid"]
    objs = [o for o in zone["objects"] if "cell" in o]
    stands = [o for o in objs if find_row(rows, o["type"], o.get("state", "default"), "lumi", "place", [])]
    mirrors = [o for o in objs if o["type"] == "mirror"]
    slots = [o for o in objs if "holds" in o]
    owned = sorted({o["holds"] for o in slots})
    focal = max([inventions_data[i].get("focal_cells", 0) for i in owned], default=0)
    cells = [tuple(o["cell"]) for o in objs]
    if len(cells) != len(set(cells)):
        err(f"{zone['id']}: 같은 칸에 사물이 둘 이상")
    for o in objs:
        if any(b[0] <= o["cell"][0] <= b[2] and b[1] <= o["cell"][1] <= b[3] for b in grid.get("blocked", [])):
            err(f"{zone['id']}: {o['id']}가 막힌 칸(grid.blocked)에 있음")
        if not (0 <= o["cell"][0] < grid["cols"] and 0 <= o["cell"][1] < grid["rows"]):
            err(f"{zone['id']}: {o['id']}가 칸 밖에 있음")

    def run(stand, mirror_states, lens_slot):
        by_cell = {}
        for o in objs:
            state = o.get("state", "default")
            if o["type"] == "mirror":
                state = mirror_states[o["id"]]
            if "holds" in o:
                state = "lens" if o["id"] == lens_slot else "empty"
            by_cell[tuple(o["cell"])] = {"id": o["id"], "type": o["type"], "state": state, "blocks_beam": o.get("blocks_beam", False)}
        path, hits = trace(grid, by_cell, stand["cell"], stand["dir"], focal, rows, owned)
        result = {}
        for oid, row, status in hits:
            eff = row["effect"]
            when = eff.get("when_beam", ["plain", "converging", "focused"])
            if status in when:
                result[oid] = eff["state"]
            elif "else_state_prefix" in eff:
                result[oid] = eff["else_state_prefix"] + status
        return path, result

    initial = {m["id"]: m.get("state", "/") for m in mirrors}
    for pz in zone.get("puzzles", []):
        conds = pz["solved_when"]
        if not all(any(o["id"] == c["object"] for o in objs) for c in conds):
            continue  # 칸 위의 사물로 판정하지 않는 퍼즐
        solutions = {}
        total = 0
        for stand in stands:
            for combo in itertools.product("/\\", repeat=len(mirrors)):
                states = {m["id"]: s for m, s in zip(mirrors, combo)}
                for lens in [None] + [s["id"] for s in slots]:
                    total += 1
                    path, result = run(stand, states, lens)
                    if all(result.get(c["object"]) == c["state"] for c in conds):
                        solutions.setdefault((stand["id"], lens, tuple(path)), []).append(states)
        print(f"{pz['id']}: {total}가지 가운데 풀리는 빛길 {len(solutions)}개")
        for (stand_id, lens, path), states_list in solutions.items():
            on_path = [m for m in mirrors if tuple(m["cell"]) in path]
            need = ", ".join(f"{m['id']}={states_list[0][m['id']]}" for m in on_path)
            print(f"    받침 {stand_id}, 거울 {need or '없음'}, 집광기 {lens or '없음'}  (빛이 닿지 않는 거울은 아무 방향)")
        if not solutions:
            err(f"{pz['id']}: 풀 수 없음")
        elif len(solutions) > 1:
            err(f"{pz['id']}: 풀리는 빛길이 {len(solutions)}개 (하나여야 함)")
        for stand in stands:
            _, result = run(stand, initial, None)
            if all(result.get(c["object"]) == c["state"] for c in conds):
                err(f"{pz['id']}: 처음 상태에서 {stand['id']}에 루미를 놓기만 해도 풀림")
        if slots and solutions:
            # 틀린 렌즈 틀마다 어떤 반응이 나오는지 보여 준다 (정답 받침·거울 기준)
            (stand_id, lens, _), states_list = next(iter(solutions.items()))
            stand = next(s for s in stands if s["id"] == stand_id)
            for other in [None] + [s["id"] for s in slots]:
                _, result = run(stand, states_list[0], other)
                print(f"    집광기 {other or '없음':8s} → {result}")


def check_place_puzzles(zone, rows):
    objs = {o["id"]: o for o in zone["objects"]}
    for pz in zone.get("puzzles", []):
        conds = pz["solved_when"]
        if any("cell" in objs[c["object"]] for c in conds if c["object"] in objs):
            continue
        spots = [o for o in zone["objects"] if o.get("placeable")]
        good = []
        for spot in spots:
            for spark in ("jjirit", "lumi"):
                result = {}
                for tid in spot.get("targets", [spot["id"]]):
                    t = objs[tid]
                    row = find_row(rows, t["type"], t.get("state", "default"), spark, "place", [])
                    if row and row["effect"].get("type") == "set_state":
                        result[tid] = row["effect"]["state"].format(**spot.get("params", {}))
                if all(result.get(c["object"]) == c["state"] for c in conds):
                    good.append((spot["id"], spark))
        print(f"{pz['id']}: 놓을 수 있는 자리 {len(spots)}곳 가운데 풀리는 곳 {good}")
        if len(good) != 1:
            err(f"{pz['id']}: 풀리는 자리가 {len(good)}곳 (하나여야 함)")


def main():
    root = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
    data = root / "data"
    rows = json.loads((data / "interactions.json").read_text(encoding="utf-8"))
    inventions = {i["id"]: i for i in json.loads((data / "inventions.json").read_text(encoding="utf-8"))}
    zones = [json.loads(p.read_text(encoding="utf-8")) for p in sorted((data / "zones").glob("*.json"))]
    for zone in zones:
        if "grid" in zone:
            check_beam_zone(zone, rows, inventions)
        check_place_puzzles(zone, rows)

    zone_ids = {z["id"] for z in zones}
    energies = []
    for p in sorted((data / "puzzles").glob("*.json")):
        if p.name.startswith("_"):
            continue
        pz = json.loads(p.read_text(encoding="utf-8"))
        if pz.get("zone") in zone_ids:
            energies.append(pz.get("energy", 0))
    worst = sum(round(e * 0.5) for e in energies)
    used = {o["holds"] for z in zones for o in z["objects"] if "holds" in o}
    cost = sum(inventions[i].get("cost", {}).get("energy", 0) for i in used)
    print(f"에너지: 문 퍼즐 {len(energies)}개를 모두 최소치(50%)로 풀면 {worst}, 필요한 발명품 비용 {cost}")
    if worst < cost:
        err("오답이 많으면 발명품을 만들 에너지가 모자라 진행이 막힘")

    print("통과" if not errors else f"실패 {len(errors)}건")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
