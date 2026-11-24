extends Node
## 키 기반 문자열 테이블 (자동 로드 이름: Loc). 화면에 나오는 문장은 모두 Loc.t(키)로 찾는다.
##
## 키가 만들어지는 곳:
##   data/strings/ko.json                → 적힌 키 그대로 (UI, 사물 이름, 지역 이름 …)
##   data/puzzles/<ID>.json              → puzzle.<ID>.title / text / explanation / parchment / source
##                                          puzzle.<ID>.hint.<0~3> / puzzle.<ID>.option.<번호>
##   data/dialogue/*.json                → dlg.<대사 ID>.<줄 ID>
##   data/sparks.json                    → spark.<id>.name / phenomenon
##   data/inventions.json                → invention.<id>.name / description / unlocks
##   data/interactions.json              → interaction.<ID>.result / principle / object_name
## 데이터 파일의 한국어 문장이 원문이고, 다른 언어는 data/strings/<언어>.json에 같은 키로 넣어 덮어쓴다.

var lang := "ko"
var table: Dictionary = {}
var missing: Dictionary = {}   # 찾지 못한 키 (시험에서 확인)


func _ready() -> void:
	rebuild()


func rebuild() -> void:
	table.clear()
	for id in Data.puzzles:
		var pz: Dictionary = Data.puzzles[id]
		var base := "puzzle.%s." % id
		table[base + "title"] = pz.get("title", "")
		table[base + "text"] = pz.get("text", "")
		table[base + "explanation"] = pz.get("explanation", "")
		var kn: Dictionary = pz.get("knowledge", {})
		table[base + "parchment"] = kn.get("parchment", "")
		table[base + "source"] = kn.get("source", "")
		var hints: Array = pz.get("hints", [])
		for i in hints.size():
			table[base + "hint.%d" % i] = hints[i]
		var options: Array = pz.get("input", {}).get("options", [])
		for i in options.size():
			table[base + "option.%d" % i] = options[i]
	for id in Data.dialogues:
		for line in Data.dialogues[id]:
			table["dlg.%s.%s" % [id, line.get("id", "")]] = line.get("text", "")
	for s in Data.sparks:
		table["spark.%s.name" % s["id"]] = s.get("name", "")
		table["spark.%s.phenomenon" % s["id"]] = s.get("phenomenon", "")
	for id in Data.inventions:
		var inv: Dictionary = Data.inventions[id]
		table["invention.%s.name" % id] = inv.get("name", "")
		table["invention.%s.description" % id] = inv.get("description", "")
		table["invention.%s.unlocks" % id] = inv.get("unlocks", "")
	for row in Data.interactions:
		var base := "interaction.%s." % row.get("id", "")
		table[base + "result"] = row.get("result", "")
		table[base + "principle"] = row.get("principle", "")
		table[base + "object_name"] = row.get("object_name", "")
	for k in Data.strings:
		table[k] = Data.strings[k]
	if lang != "ko":
		var extra: Variant = Data.load_json("res://data/strings/%s.json" % lang)
		if extra is Dictionary:
			for k in extra:
				table[k] = extra[k]


func has(key: String) -> bool:
	return table.has(key)


## 키에 맞는 문장. args의 {이름}을 바꿔 넣는다. 키가 없으면 '#키'를 돌려주고 기록한다.
func t(key: String, args: Dictionary = {}) -> String:
	if not table.has(key):
		if not missing.has(key):
			missing[key] = true
			push_warning("문자열 키 없음: " + key)
		return "#" + key
	var s: String = table[key]
	return s.format(args) if not args.is_empty() else s
