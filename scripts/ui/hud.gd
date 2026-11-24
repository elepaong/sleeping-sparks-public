extends Control
## 필드 위에 늘 떠 있는 표시: 지역 이름, 잉크·에너지, 스파크 칸(1~8), 알림 한 줄, 조작 안내.

const UiTheme = preload("res://scripts/ui/ui_theme.gd")

var _zone: Label
var _stats: Label
var _mode: Label
var _toast: Label
var _toast_left := 0.0
var _slots: Array = []   # {"panel", "icon", "num"}


func _ready() -> void:
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	theme = UiTheme.make()
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	_zone = _outlined("", 9)
	_zone.position = Vector2(6, 3)
	add_child(_zone)
	_stats = _outlined("", 9)
	_stats.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	_stats.position = Vector2(440, 3)
	_stats.size = Vector2(194, 14)
	add_child(_stats)
	_mode = _outlined(Loc.t("ui.hud_investigate"), 8)
	_mode.add_theme_color_override("font_color", UiTheme.ACCENT)
	_mode.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_mode.position = Vector2(170, 3)
	_mode.size = Vector2(300, 14)
	add_child(_mode)
	_toast = _outlined("", 9)
	_toast.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_toast.position = Vector2(20, 22)
	_toast.size = Vector2(600, 14)
	add_child(_toast)
	var controls := _outlined(Loc.t("ui.hud_controls"), 6)
	controls.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	controls.position = Vector2(200, 347)
	controls.size = Vector2(436, 10)
	add_child(controls)
	for i in Data.sparks.size():
		var panel := Panel.new()
		panel.position = Vector2(6 + i * 23, 334)
		panel.size = Vector2(21, 21)
		panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
		add_child(panel)
		var icon := TextureRect.new()
		icon.position = Vector2(1, 1)
		icon.size = Vector2(19, 19)
		icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
		panel.add_child(icon)
		var num := _outlined(str(i + 1), 6)
		num.position = Vector2(2, -1)
		panel.add_child(num)
		_slots.append({"panel": panel, "icon": icon, "num": num})
	State.changed.connect(update_all)
	update_all()


func _outlined(text: String, size: int) -> Label:
	var l: Label = UiTheme.label(text, size)
	l.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.9))
	l.add_theme_constant_override("outline_size", 3)
	l.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return l


func update_all() -> void:
	if State.zone != "" and Data.zones.has(State.zone):
		_zone.text = Loc.t(Data.zones[State.zone]["name_key"])
	_stats.text = Loc.t("ui.hud_stats", {"ink": State.ink, "energy": State.energy})
	_mode.visible = State.investigate
	for i in _slots.size():
		var id: String = Data.sparks[i]["id"]
		var slot: Dictionary = _slots[i]
		var owned: bool = id in State.sparks
		var icon: TextureRect = slot["icon"]
		if owned and icon.texture == null:
			var path := "res://assets/sparks/%s_idle.png" % id
			if ResourceLoader.exists(path):
				var at := AtlasTexture.new()
				at.atlas = load(path)
				at.region = Rect2(0, 0, 24, 24)
				icon.texture = at
		icon.visible = owned
		icon.modulate = Color(1, 1, 1, 0.35) if State.placed.has(id) else Color(1, 1, 1, 1)
		var sb: StyleBoxFlat = UiTheme.box(Color(0.1, 0.1, 0.12, 0.8 if owned else 0.4), 0)
		if State.selected_spark == id:
			sb.border_color = UiTheme.ACCENT
			sb.set_border_width_all(2)
		var panel: Panel = slot["panel"]
		panel.add_theme_stylebox_override("panel", sb)


func toast(text: String) -> void:
	_toast.text = text
	_toast.modulate.a = 1.0
	_toast_left = 3.0


func _process(delta: float) -> void:
	if _toast_left > 0.0:
		_toast_left -= delta
		_toast.modulate.a = clampf(_toast_left, 0.0, 1.0)
