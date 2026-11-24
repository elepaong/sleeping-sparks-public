extends RefCounted
## 빛줄기 계산 (design/ch1-prototype.md '빛줄기 규칙'). 화면·노드와 무관한 순수 계산이다.
## tools/verify/field_check.py가 같은 규칙을 파이썬으로 다시 계산해 정답을 확인한다 — 한쪽을 고치면 다른 쪽도 고친다.

const DIRS := {"E": Vector2i(1, 0), "W": Vector2i(-1, 0), "N": Vector2i(0, -1), "S": Vector2i(0, 1)}
# '/'는 화면에서 왼쪽 아래↗오른쪽 위로 기운 거울
const TURN := {
	"/": {"E": "N", "N": "E", "W": "S", "S": "W"},
	"\\": {"E": "S", "S": "E", "W": "N", "N": "W"},
}
const MAX_STEPS := 200


## by_cell: Vector2i → {"id", "type", "state", "blocks_beam"} (보이는 사물만)
## find_row: Callable(사물 사전) → 상호작용표의 줄 (없으면 빈 사전)
## 돌려주는 값: {"path": [{"cell", "status"}], "hits": [{"id", "row", "status"}]}
## status: plain(그냥 빛) → converging(렌즈를 지나 모이는 중) → focused(초점) → spread(초점을 지나 퍼짐)
## blocked: Vector2i → true. 배경에 그려진 것(기계, 기둥, 탁자 …)이 차지한 칸. 빛은 그 칸 앞에서 멈춘다.
static func trace(cols: int, rows: int, by_cell: Dictionary, start: Vector2i, dir: String, focal: int, find_row: Callable, blocked: Dictionary = {}) -> Dictionary:
	var path: Array = [{"cell": start, "status": "plain"}]
	var hits: Array = []
	var status := "plain"
	var remain := 0
	var cell := start
	var d := dir
	for _i in MAX_STEPS:
		var step: Vector2i = DIRS[d]
		cell += step
		if blocked.has(cell):
			break
		if status == "converging":
			remain -= 1
			if remain <= 0:
				status = "focused"
		elif status == "focused":
			status = "spread"
		path.append({"cell": cell, "status": status})
		if cell.x < 0 or cell.y < 0 or cell.x >= cols or cell.y >= rows:
			break
		if not by_cell.has(cell):
			continue
		var o: Dictionary = by_cell[cell]
		var row: Dictionary = find_row.call(o)
		if row.is_empty():
			if o.get("blocks_beam", false):
				break
			continue
		var effect: Dictionary = row["effect"]
		var kind: String = effect.get("type", "")
		if kind == "reflect":
			var turn: Dictionary = TURN.get(o.get("state", ""), {})
			if not turn.has(d):
				break
			d = turn[d]
		elif kind == "focus":
			if status == "plain":
				status = "converging"
				remain = focal
		else:
			hits.append({"id": o["id"], "row": row, "status": status})
			break
	return {"path": path, "hits": hits}
