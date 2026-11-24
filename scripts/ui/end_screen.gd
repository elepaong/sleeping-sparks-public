extends Control
## 끝 화면: "프로토타입 끝"과 이번 플레이의 기록.

const UiTheme = preload("res://scripts/ui/ui_theme.gd")

signal restart_requested

var _stats: RichTextLabel


func _ready() -> void:
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	theme = UiTheme.make()
	mouse_filter = Control.MOUSE_FILTER_STOP
	visible = false
	var back := ColorRect.new()
	back.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	back.color = Color(0.06, 0.07, 0.1, 1.0)
	back.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(back)
	var title: Label = UiTheme.label(Loc.t("ui.end_title"), 28, UiTheme.ACCENT)
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.position = Vector2(0, 60)
	title.size = Vector2(640, 40)
	add_child(title)
	var sub: Label = UiTheme.label(Loc.t("ui.end_thanks"), 10)
	sub.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	sub.position = Vector2(0, 108)
	sub.size = Vector2(640, 16)
	add_child(sub)
	_stats = UiTheme.rich(9)
	_stats.position = Vector2(170, 140)
	_stats.size = Vector2(300, 150)
	add_child(_stats)
	var again: Button = UiTheme.button(Loc.t("ui.end_restart"))
	again.position = Vector2(260, 304)
	again.size = Vector2(120, 24)
	again.pressed.connect(func() -> void: restart_requested.emit())
	add_child(again)


func show_end() -> void:
	var wrong := 0
	var solved := 0
	for id in State.door:
		wrong += int(State.door[id]["wrong"])
		if State.door[id]["solved"]:
			solved += 1
	_stats.text = Loc.t("ui.end_stats", {
		"door": solved, "field": State.field_solved.size(), "wrong": wrong,
		"hints": State.hints_used, "energy": State.energy, "ink": State.ink})
	State.ended = true
	visible = true


func title_text() -> String:
	return Loc.t("ui.end_title")
