extends Control
## 오메가 문서 (Tab). 프로토타입에서는 세 쪽만: 스파크 도감, 양피지, 퍼즐 목록.

const UiTheme = preload("res://scripts/ui/ui_theme.gd")

const TABS := ["sparks", "parchment", "puzzles"]

var tab := "sparks"
var _body: RichTextLabel
var _tab_buttons: Dictionary = {}


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
	panel.position = Vector2(40, 20)
	panel.size = Vector2(560, 320)
	panel.add_theme_stylebox_override("panel", UiTheme.box(Color(0.2, 0.17, 0.14, 0.98), 0))
	add_child(panel)
	var title: Label = UiTheme.label(Loc.t("ui.omega_title"), 12, UiTheme.ACCENT)
	title.position = Vector2(12, 6)
	panel.add_child(title)
	for i in TABS.size():
		var b: Button = UiTheme.button(Loc.t("ui.omega_tab_" + TABS[i]))
		b.position = Vector2(150 + i * 92, 4)
		b.size = Vector2(88, 26)
		b.toggle_mode = true
		b.pressed.connect(show_tab.bind(TABS[i]))
		panel.add_child(b)
		_tab_buttons[TABS[i]] = b
	var close: Button = UiTheme.button(Loc.t("ui.omega_close"))
	close.position = Vector2(470, 4)
	close.size = Vector2(80, 26)
	close.pressed.connect(close_book)
	panel.add_child(close)
	_body = UiTheme.rich(9)
	_body.position = Vector2(14, 38)
	_body.size = Vector2(532, 274)
	panel.add_child(_body)


func open_book() -> void:
	visible = true
	show_tab(tab)


func close_book() -> void:
	visible = false


func show_tab(t: String) -> void:
	tab = t
	for k in _tab_buttons:
		var b: Button = _tab_buttons[k]
		b.set_pressed_no_signal(k == t)
	_body.clear()
	match t:
		"sparks": _page_sparks()
		"parchment": _page_parchment()
		"puzzles": _page_puzzles()
	_body.scroll_to_line(0)


func body_text() -> String:
	return _body.get_parsed_text()


func _head(text: String) -> void:
	_body.append_text("[color=#f2c75a]%s[/color]\n" % text)


func _page_sparks() -> void:
	for s in Data.sparks:
		var id: String = s["id"]
		if id in State.sparks:
			var card := "res://assets/sparks/%s_card.png" % id
			if ResourceLoader.exists(card):
				_body.add_image(load(card), 40, 40)
				_body.append_text("  ")
			_head("%s — %s" % [Loc.t("spark.%s.name" % id), Loc.t("spark.%s.phenomenon" % id)])
		else:
			_body.append_text("[color=#77736b]● %s[/color]\n" % Loc.t("ui.omega_spark_unknown"))
		_body.append_text("\n")


func _page_parchment() -> void:
	var any := false
	for id in State.door:
		if not State.door[id]["solved"]:
			continue
		any = true
		_head(Loc.t("puzzle.%s.title" % id))
		_body.append_text("%s\n" % Loc.t("puzzle.%s.parchment" % id))
		_body.append_text("[color=#9a968c]%s[/color]\n\n" % Loc.t("ui.omega_source", {"src": Loc.t("puzzle.%s.source" % id)}))
	if not any:
		_body.append_text(Loc.t("ui.omega_no_parchment"))


func _page_puzzles() -> void:
	_head(Loc.t("ui.omega_door_header"))
	var ids: Array = Data.puzzles.keys()
	ids.sort()
	for id in ids:
		var pz: Dictionary = Data.puzzles[id]
		var z: String = pz.get("zone", "")
		if not Data.zones.has(z):
			continue   # 이 프로토타입에 없는 지역의 문제
		if State.is_solved(id):
			var rec: Dictionary = State.door[id]
			_body.append_text("✔ %s  [color=#9a968c]%s[/color]\n" % [Loc.t("puzzle.%s.title" % id), Loc.t("ui.omega_solved", {"wrong": rec["wrong"], "energy": rec["energy"]})])
		else:
			_body.append_text("[color=#77736b]？ %s[/color]\n" % Loc.t("ui.omega_unsolved", {"zone": Loc.t(Data.zones[z]["name_key"])}))
	_body.append_text("\n")
	_head(Loc.t("ui.omega_field_header"))
	for zid in Data.zones:
		for fp in Data.zones[zid].get("puzzles", []):
			if State.is_solved(fp["id"]):
				_body.append_text("✔ %s\n" % Loc.t(fp["name_key"]))
			else:
				_body.append_text("[color=#77736b]？ %s[/color]\n" % Loc.t("ui.omega_unsolved", {"zone": Loc.t(Data.zones[zid]["name_key"])}))
