extends Control
## 발명품 제작 창 (연구실 작업대). 비용은 data/inventions.json의 cost: 양피지(문 퍼즐 ID), 에너지, 재료.

const UiTheme = preload("res://scripts/ui/ui_theme.gd")

signal closed(made: bool)

var invention_id := ""
var _title: Label
var _body: RichTextLabel
var _make: Button


func _ready() -> void:
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	theme = UiTheme.make()
	mouse_filter = Control.MOUSE_FILTER_STOP
	visible = false
	var back := ColorRect.new()
	back.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	back.color = Color(0, 0, 0, 0.6)
	back.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(back)
	var panel := Panel.new()
	panel.position = Vector2(140, 70)
	panel.size = Vector2(360, 220)
	add_child(panel)
	_title = UiTheme.label("", 12, UiTheme.ACCENT)
	_title.position = Vector2(12, 8)
	panel.add_child(_title)
	_body = UiTheme.rich(9)
	_body.position = Vector2(12, 30)
	_body.size = Vector2(336, 152)
	panel.add_child(_body)
	_make = UiTheme.button(Loc.t("ui.craft_make"))
	_make.position = Vector2(160, 186)
	_make.size = Vector2(90, 26)
	_make.pressed.connect(make)
	panel.add_child(_make)
	var close: Button = UiTheme.button(Loc.t("ui.craft_close"))
	close.position = Vector2(258, 186)
	close.size = Vector2(90, 26)
	close.pressed.connect(cancel)
	panel.add_child(close)


## 제작 창을 연다. 만들었으면 true (await).
func open(id: String) -> bool:
	invention_id = id
	var inv: Dictionary = Data.inventions[id]
	var cost: Dictionary = inv.get("cost", {})
	_title.text = "%s — %s" % [Loc.t("ui.craft_title"), Loc.t("invention.%s.name" % id)]
	var lines := PackedStringArray()
	lines.append(Loc.t("invention.%s.description" % id))
	lines.append("")
	for pid in cost.get("parchments", []):
		lines.append(_mark(State.is_solved(pid)) + Loc.t("ui.craft_need_parchment", {"name": Loc.t("puzzle.%s.title" % pid)}))
	var need: int = int(cost.get("energy", 0))
	lines.append(_mark(State.energy >= need) + Loc.t("ui.craft_need_energy", {"need": need, "have": State.energy}))
	for item in cost.get("items", []):
		lines.append(_mark(item in State.items) + Loc.t("ui.craft_need_item", {"name": Loc.t("item.%s.name" % item)}))
	_body.text = "\n".join(lines)
	_make.disabled = not can_make()
	visible = true
	var made: bool = await closed
	return made


func _mark(ok: bool) -> String:
	return "[color=#8fd18a]✔[/color] " if ok else "[color=#e07a6a]✘[/color] "


func can_make() -> bool:
	var cost: Dictionary = Data.inventions[invention_id].get("cost", {})
	for pid in cost.get("parchments", []):
		if not State.is_solved(pid):
			return false
	for item in cost.get("items", []):
		if not (item in State.items):
			return false
	return State.energy >= int(cost.get("energy", 0))


func make() -> void:
	if not can_make():
		return
	var cost: Dictionary = Data.inventions[invention_id].get("cost", {})
	State.energy -= int(cost.get("energy", 0))
	for item in cost.get("items", []):
		State.items.erase(item)
	State.inventions.append(invention_id)
	State.touch()
	visible = false
	closed.emit(true)


func cancel() -> void:
	visible = false
	closed.emit(false)


func _unhandled_input(event: InputEvent) -> void:
	if visible and event.is_action_pressed("cancel"):
		get_viewport().set_input_as_handled()
		cancel()
