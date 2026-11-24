extends Node
## 진행 상태 (자동 로드 이름: State). 세이브는 없다 — 프로그램을 끄면 처음부터.

const Config = preload("res://scripts/core/config.gd")

signal changed

var flags: Dictionary = {}
var sparks: Array = []            # 가진 스파크 id
var selected_spark := ""
var placed: Dictionary = {}       # 스파크 id → 놓인 사물 id (지금 방 안에서만)
var ink := 0
var energy := 0
var items: Array = []
var inventions: Array = []
var door: Dictionary = {}         # 문 퍼즐 id → {wrong, hints, solved, energy}
var field_solved: Array = []      # 푼 필드 퍼즐 id
var obj_states: Dictionary = {}   # 지역 id → {사물 id → 상태}
var zone := ""
var investigate := false
var hints_used := 0
var ended := false


func _ready() -> void:
	reset()


func reset() -> void:
	flags = {}
	sparks = []
	selected_spark = ""
	placed = {}
	ink = Config.START_INK
	energy = 0
	items = []
	inventions = []
	door = {}
	field_solved = []
	obj_states = {}
	zone = ""
	investigate = false
	hints_used = 0
	ended = false
	changed.emit()


func door_record(id: String) -> Dictionary:
	if not door.has(id):
		door[id] = {"wrong": 0, "hints": 0, "solved": false, "energy": 0}
	return door[id]


func is_solved(id: String) -> bool:
	if door.has(id) and door[id]["solved"]:
		return true
	return id in field_solved


## 오답 수에 따라 얻는 에너지 (plan.md: 오답 1회마다 10%씩 줄고 최소 50%)
func energy_for(base: float, wrong: int) -> int:
	var ratio: float = maxf(Config.MIN_ENERGY_RATIO, 1.0 - Config.WRONG_PENALTY * wrong)
	return int(round(base * ratio))


func solve_door(id: String) -> int:
	var rec := door_record(id)
	if rec["solved"]:
		return rec["energy"]
	var pz: Dictionary = Data.puzzles[id]
	rec["solved"] = true
	rec["energy"] = energy_for(float(pz.get("energy", 0)), rec["wrong"])
	energy += rec["energy"]
	changed.emit()
	return rec["energy"]


func add_spark(id: String) -> void:
	if not (id in sparks):
		sparks.append(id)
		changed.emit()


func select_spark(id: String) -> void:
	selected_spark = id if (id in sparks and id != selected_spark) else ""
	changed.emit()


func cycle_spark(step: int) -> void:
	var order: Array = [""]
	for s in Data.sparks:
		if s["id"] in sparks:
			order.append(s["id"])
	var i := order.find(selected_spark)
	selected_spark = order[posmod(i + step, order.size())]
	changed.emit()


func set_flag(name: String) -> void:
	flags[name] = true
	changed.emit()


func touch() -> void:
	changed.emit()


## 지역 데이터의 조건(if)을 따진다. 여러 키를 함께 쓰면 모두 맞아야 한다.
func check(cond: Variant) -> bool:
	if not (cond is Dictionary):
		return true
	for key in cond:
		var v: Variant = cond[key]
		var ok := true
		match key:
			"flag": ok = flags.has(v)
			"not_flag": ok = not flags.has(v)
			"solved": ok = is_solved(v)
			"not_solved": ok = not is_solved(v)
			"has_spark": ok = v in sparks
			"has_item": ok = v in items
			"has_invention": ok = v in inventions
			"all":
				for c in v:
					ok = ok and check(c)
			"any":
				ok = false
				for c in v:
					ok = ok or check(c)
			_:
				push_error("모르는 조건: " + str(key))
				ok = false
		if not ok:
			return false
	return true
