extends Node
## data/ 아래 JSON을 모두 읽어 둔다 (자동 로드 이름: Data). 내용은 전부 여기서 나온다.

var sparks: Array = []            # sparks.json 순서 그대로 (1~8 키 순서)
var interactions: Array = []
var inventions: Dictionary = {}   # id → 항목
var puzzles: Dictionary = {}      # 문 퍼즐 id → 항목
var dialogues: Dictionary = {}    # 대사 id → 줄 목록
var zones: Dictionary = {}        # 지역 id → 항목
var strings: Dictionary = {}      # data/strings/ko.json
var errors: Array = []


func _ready() -> void:
	load_all()


func load_all() -> void:
	sparks = _array(load_json("res://data/sparks.json"))
	interactions = _array(load_json("res://data/interactions.json"))
	inventions.clear()
	for inv in _array(load_json("res://data/inventions.json")):
		inventions[inv["id"]] = inv
	puzzles.clear()
	for f in list_json("res://data/puzzles"):
		if f.begins_with("_"):
			continue
		var pz: Variant = load_json("res://data/puzzles/" + f)
		if pz is Dictionary:
			puzzles[pz["id"]] = pz
	dialogues.clear()
	for f in list_json("res://data/dialogue"):
		var pack: Variant = load_json("res://data/dialogue/" + f)
		if pack is Dictionary:
			var d: Dictionary = pack.get("dialogues", {})
			for id in d:
				dialogues[id] = d[id]
	zones.clear()
	for f in list_json("res://data/zones"):
		var z: Variant = load_json("res://data/zones/" + f)
		if z is Dictionary:
			zones[z["id"]] = z
	var s: Variant = load_json("res://data/strings/ko.json")
	strings = s if s is Dictionary else {}


func load_json(path: String) -> Variant:
	if not FileAccess.file_exists(path):
		_error("파일 없음: " + path)
		return null
	var text := FileAccess.get_file_as_string(path)
	var json := JSON.new()
	if json.parse(text) != OK:
		_error("%s %d번째 줄: %s" % [path, json.get_error_line(), json.get_error_message()])
		return null
	return json.data


func list_json(dir: String) -> Array:
	var out: Array = []
	var d := DirAccess.open(dir)
	if d == null:
		_error("폴더 없음: " + dir)
		return out
	for f in d.get_files():
		if f.ends_with(".json"):
			out.append(f)
	out.sort()
	return out


func spark(id: String) -> Dictionary:
	for s in sparks:
		if s["id"] == id:
			return s
	return {}


## 상호작용표에서 (사물 종류, 스파크, 언제)에 맞는 줄을 찾는다. 없으면 빈 사전 = 반응 없음.
## 'effect'가 없는 줄(아직 초안뿐인 뒤 챕터의 줄)은 쓰지 않는다.
func find_interaction(obj_type: String, obj_state: String, spark_id: String, trigger: String, owned_inventions: Array) -> Dictionary:
	for row in interactions:
		if row.get("object", "") != obj_type or not row.has("effect"):
			continue
		if row.get("trigger", "") != trigger or not (spark_id in row.get("sparks", [])):
			continue
		if row.has("object_state") and row["object_state"] != obj_state:
			continue
		var ok := true
		for need in row.get("requires", []):
			if not (need in owned_inventions):
				ok = false
		if ok:
			return row
	return {}


func _array(v: Variant) -> Array:
	return v if v is Array else []


func _error(msg: String) -> void:
	errors.append(msg)
	push_error(msg)
