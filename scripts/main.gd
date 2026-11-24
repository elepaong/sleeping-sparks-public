extends Node
## 게임의 뼈대: 필드 화면과 UI를 만들고, 지역 데이터의 '행동 목록'을 차례로 실행한다.
## 행동의 종류는 design/ch1-prototype.md 9절 '데이터 형식'과 같다.

const Config = preload("res://scripts/core/config.gd")
const UiTheme = preload("res://scripts/ui/ui_theme.gd")
const ZoneView = preload("res://scripts/field/zone_view.gd")
const Hud = preload("res://scripts/ui/hud.gd")
const DialogueBox = preload("res://scripts/ui/dialogue_box.gd")
const PuzzleScreen = preload("res://scripts/ui/puzzle_screen.gd")
const OmegaBook = preload("res://scripts/ui/omega_book.gd")
const CraftPanel = preload("res://scripts/ui/craft_panel.gd")
const EndScreen = preload("res://scripts/ui/end_screen.gd")

signal idle   # 밀린 행동을 모두 마쳤을 때

var zone_view: Node2D
var hud: Control
var dialogue: Control
var puzzle_screen: Control
var omega: Control
var craft: Control
var end_screen: Control

var _queue: Array = []
var _running := false


func _ready() -> void:
	get_window().theme = UiTheme.make()
	zone_view = ZoneView.new()
	zone_view.name = "ZoneView"
	zone_view.main = self
	add_child(zone_view)
	var ui := CanvasLayer.new()
	ui.name = "UI"
	add_child(ui)
	hud = _add_ui(ui, Hud, "Hud")
	dialogue = _add_ui(ui, DialogueBox, "DialogueBox")
	puzzle_screen = _add_ui(ui, PuzzleScreen, "PuzzleScreen")
	craft = _add_ui(ui, CraftPanel, "CraftPanel")
	omega = _add_ui(ui, OmegaBook, "OmegaBook")
	end_screen = _add_ui(ui, EndScreen, "EndScreen")
	end_screen.restart_requested.connect(restart)
	restart()


func _add_ui(layer: CanvasLayer, script: GDScript, node_name: String) -> Control:
	var c: Control = script.new()
	c.name = node_name
	layer.add_child(c)
	return c


func restart() -> void:
	end_screen.visible = false
	_queue.clear()
	State.reset()
	change_zone(Config.START_ZONE, "default")


func change_zone(id: String, spawn: String) -> void:
	if not Data.zones.has(id):
		push_error("지역 없음: " + id)
		return
	zone_view.load_zone(id, spawn)
	State.touch()
	for entry in Data.zones[id].get("on_enter", []):
		if State.check(entry.get("if")):
			run_script(entry.get("do", []))


## 필드 입력을 막아야 하는가 (행동 실행 중이거나 창이 떠 있음)
func is_busy() -> bool:
	return _running or dialogue.visible or puzzle_screen.visible or omega.visible or craft.visible or end_screen.visible


func toast(text: String) -> void:
	hud.toast(text)


## 행동 목록을 차례로 실행한다. 이미 실행 중이면 줄을 세워 뒤에 실행한다.
func run_script(actions: Array) -> void:
	_queue.append(actions)
	if _running:
		return
	_running = true
	while not _queue.is_empty():
		var next: Array = _queue.pop_front()
		await _run_actions(next)
	_running = false
	idle.emit()


func _run_actions(actions: Array) -> void:
	for a in actions:
		if a.has("if") and not State.check(a["if"]):
			continue
		if a.has("dialogue"):
			await dialogue.play(a["dialogue"], a.get("style", ""))
		elif a.has("door_puzzle"):
			if not State.is_solved(a["door_puzzle"]):
				var solved: bool = await puzzle_screen.open(a["door_puzzle"])
				if not solved:
					return   # 그만두면 뒤 행동은 하지 않는다 (다음에 같은 자리에서 다시 도전)
				zone_view.refresh()   # 풀이에 따라 보이는 사물이 달라질 수 있다
		elif a.has("craft"):
			var made: bool = await craft.open(a["craft"])
			if not made:
				return
			toast(Loc.t("ui.craft_done", {"name": Loc.t("invention.%s.name" % a["craft"])}))
		elif a.has("give_spark"):
			State.add_spark(a["give_spark"])
			zone_view.refresh()
			toast(Loc.t("ui.spark_got", {"name": Loc.t("spark.%s.name" % a["give_spark"])}))
		elif a.has("give_item"):
			if not (a["give_item"] in State.items):
				State.items.append(a["give_item"])
			State.touch()
			toast(Loc.t("ui.item_got", {"name": Loc.t("item.%s.name" % a["give_item"])}))
		elif a.has("add_ink"):
			State.ink += int(a["add_ink"])
			State.touch()
		elif a.has("set_flag"):
			State.set_flag(a["set_flag"])
			zone_view.refresh()
		elif a.has("set_state"):
			zone_view.set_object_state(a["set_state"]["object"], a["set_state"]["state"])
		elif a.has("recall_sparks"):
			zone_view.recall_all()
		elif a.has("goto_zone"):
			change_zone(a["goto_zone"], a.get("spawn", "default"))
		elif a.has("ending"):
			end_screen.show_end()
		else:
			push_error("모르는 행동: " + str(a))


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("omega"):
		get_viewport().set_input_as_handled()
		if omega.visible:
			omega.close_book()
		elif not is_busy():
			omega.open_book()
	elif event.is_action_pressed("cancel") and omega.visible:
		get_viewport().set_input_as_handled()
		omega.close_book()
