extends Control
## 문 퍼즐 화면 (plan.md 5장). 문제는 data/puzzles/<ID>.json, 그림은 assets/puzzles/<ID>.png.
##  - 선택(choice): 하나 고르고 제출 / 순서(order): 누른 차례대로 놓고 제출
##  - 힌트 4단계: 잉크 방울 1·1·1·2개
##  - 오답 1회마다 얻을 에너지 10% 감소, 최소 50%
##  - 정답이면 해설을 보여 주고 양피지(오메가 문서)에 기록

const Config = preload("res://scripts/core/config.gd")
const UiTheme = preload("res://scripts/ui/ui_theme.gd")

signal closed(solved: bool)

var puzzle_id := ""
var _pz: Dictionary = {}
var _kind := "choice"
var _choice := -1
var _sequence: Array = []          # 순서 문제에서 누른 선택지 번호들

var _title: Label
var _energy: Label
var _figure: TextureRect
var _text: RichTextLabel
var _options: GridContainer
var _option_buttons: Array = []
var _seq_label: Label
var _hints: RichTextLabel
var _feedback: Label
var _hint_btn: Button
var _reset_btn: Button
var _submit_btn: Button
var _quit_btn: Button
var _result: Panel
var _result_text: RichTextLabel


func _ready() -> void:
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	theme = UiTheme.make()
	mouse_filter = Control.MOUSE_FILTER_STOP
	visible = false
	var back := ColorRect.new()
	back.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	back.color = Color(0.1, 0.09, 0.12, 1.0)
	back.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(back)
	_title = UiTheme.label("", 12, UiTheme.ACCENT)
	_title.position = Vector2(12, 6)
	add_child(_title)
	_energy = UiTheme.label("", 8)
	_energy.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	_energy.position = Vector2(300, 9)
	_energy.size = Vector2(328, 12)
	add_child(_energy)
	_figure = TextureRect.new()
	_figure.position = Vector2(12, 28)
	_figure.size = Vector2(220, 160)
	_figure.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	_figure.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	_figure.texture_filter = CanvasItem.TEXTURE_FILTER_LINEAR
	add_child(_figure)
	_text = UiTheme.rich(8)
	_text.position = Vector2(240, 26)
	_text.size = Vector2(388, 164)
	add_child(_text)
	_options = GridContainer.new()
	_options.columns = 2
	_options.position = Vector2(12, 194)
	_options.size = Vector2(616, 52)
	_options.add_theme_constant_override("h_separation", 6)
	_options.add_theme_constant_override("v_separation", 4)
	add_child(_options)
	_seq_label = UiTheme.label("", 8, UiTheme.ACCENT)
	_seq_label.position = Vector2(12, 249)
	_seq_label.size = Vector2(616, 12)
	add_child(_seq_label)
	var hint_panel := Panel.new()
	hint_panel.position = Vector2(12, 263)
	hint_panel.size = Vector2(616, 62)
	hint_panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(hint_panel)
	_hints = UiTheme.rich(8)
	_hints.position = Vector2(18, 265)
	_hints.size = Vector2(604, 58)
	add_child(_hints)
	_hint_btn = _button("", 12, 110, take_hint)
	_reset_btn = _button(Loc.t("ui.puzzle_reset_order"), 128, 70, reset_order)
	_feedback = UiTheme.label("", 8, Color(1.0, 0.6, 0.5))
	_feedback.position = Vector2(204, 335)
	_feedback.size = Vector2(250, 14)
	add_child(_feedback)
	_submit_btn = _button(Loc.t("ui.puzzle_submit"), 462, 80, submit)
	_quit_btn = _button(Loc.t("ui.puzzle_quit"), 548, 80, quit)

	_result = Panel.new()
	_result.position = Vector2(12, 192)
	_result.size = Vector2(616, 164)
	_result.add_theme_stylebox_override("panel", UiTheme.box(Color(0.16, 0.2, 0.16, 1.0), 0))
	_result.visible = false
	add_child(_result)
	_result_text = UiTheme.rich(9)
	_result_text.position = Vector2(8, 6)
	_result_text.size = Vector2(600, 124)
	_result.add_child(_result_text)
	var close := UiTheme.button(Loc.t("ui.puzzle_close"))
	close.position = Vector2(526, 134)
	close.size = Vector2(82, 26)
	close.pressed.connect(close_result)
	_result.add_child(close)


func _button(text: String, x: float, w: float, fn: Callable) -> Button:
	var b: Button = UiTheme.button(text)
	b.position = Vector2(x, 329)
	b.size = Vector2(w, 26)
	b.pressed.connect(fn)
	add_child(b)
	return b


## 문 퍼즐을 연다. 풀고 닫으면 true, 그만두면 false (await).
func open(id: String) -> bool:
	puzzle_id = id
	_pz = Data.puzzles[id]
	_kind = _pz["input"]["kind"]
	_choice = -1
	_sequence = []
	_title.text = "%s  ·  %s" % [Loc.t("puzzle.%s.title" % id), Loc.t("ui.puzzle_type." + str(_pz.get("door_type", "principle")))]
	_text.text = Loc.t("puzzle.%s.text" % id)
	_text.scroll_to_line(0)
	var img := "res://assets/puzzles/%s.png" % id
	_figure.texture = load(img) if ResourceLoader.exists(img) else null
	for b in _option_buttons:
		b.queue_free()
	_option_buttons = []
	var options: Array = _pz["input"]["options"]
	for i in options.size():
		var b: Button = UiTheme.button(Loc.t("puzzle.%s.option.%d" % [id, i]))
		b.custom_minimum_size = Vector2(305, 24)
		b.toggle_mode = _kind == "choice"
		b.clip_text = true
		b.pressed.connect(pick.bind(i))
		_options.add_child(b)
		_option_buttons.append(b)
	_reset_btn.visible = _kind == "order"
	_feedback.text = ""
	_result.visible = false
	visible = true
	_update()
	var solved: bool = await closed
	return solved


func _record() -> Dictionary:
	return State.door_record(puzzle_id)


func _update() -> void:
	var rec := _record()
	var base: int = int(_pz.get("energy", 0))
	_energy.text = Loc.t("ui.puzzle_energy", {"now": State.energy_for(base, rec["wrong"]), "base": base, "wrong": rec["wrong"], "ink": State.ink})
	var shown: int = rec["hints"]
	if shown == 0:
		_hints.text = "[color=#9a968c]%s[/color]" % Loc.t("ui.puzzle_hints_empty")
	else:
		var lines := PackedStringArray()
		for i in shown:
			var head := Loc.t("ui.puzzle_hint_special") if i == 3 else Loc.t("ui.puzzle_hint_title", {"n": i + 1})
			lines.append("[color=#f2c75a]%s[/color] %s" % [head, Loc.t("puzzle.%s.hint.%d" % [puzzle_id, i])])
		_hints.text = "\n".join(lines)
	if shown >= Config.HINT_COSTS.size():
		_hint_btn.text = Loc.t("ui.puzzle_hint_none")
		_hint_btn.disabled = true
	else:
		_hint_btn.text = Loc.t("ui.puzzle_hint_btn", {"cost": Config.HINT_COSTS[shown]})
		_hint_btn.disabled = false
	for i in _option_buttons.size():
		var b: Button = _option_buttons[i]
		if _kind == "choice":
			b.set_pressed_no_signal(i == _choice)
		else:
			b.disabled = i in _sequence
	if _kind == "order":
		var names := PackedStringArray()
		for i in _sequence:
			names.append(Loc.t("puzzle.%s.option.%d" % [puzzle_id, i]))
		_seq_label.text = Loc.t("ui.puzzle_order_seq", {"seq": "  →  ".join(names) if not names.is_empty() else "…"})
	else:
		_seq_label.text = ""


func pick(i: int) -> void:
	if _kind == "choice":
		_choice = i
	elif not (i in _sequence):
		_sequence.append(i)
	_feedback.text = ""
	_update()


func reset_order() -> void:
	_sequence = []
	_update()


func take_hint() -> void:
	var rec := _record()
	var shown: int = rec["hints"]
	if shown >= Config.HINT_COSTS.size():
		return
	var cost: int = Config.HINT_COSTS[shown]
	if State.ink < cost:
		_feedback.text = Loc.t("ui.puzzle_no_ink", {"cost": cost, "have": State.ink})
		return
	State.ink -= cost
	State.hints_used += 1
	rec["hints"] = shown + 1
	State.touch()
	_update()


func _answer_given() -> Variant:
	var options: Array = _pz["input"]["options"]
	if _kind == "choice":
		return options[_choice] if _choice >= 0 else null
	if _sequence.size() != options.size():
		return null
	var out: Array = []
	for i in _sequence:
		out.append(options[i])
	return out


func submit() -> void:
	var given: Variant = _answer_given()
	if given == null:
		_feedback.text = Loc.t("ui.puzzle_pick_first" if _kind == "choice" else "ui.puzzle_order_incomplete")
		return
	if given == _pz["answer"]:
		var gained := State.solve_door(puzzle_id)
		_result_text.text = "[color=#f2c75a]%s[/color]\n%s\n\n[color=#f2c75a]%s[/color]\n%s\n\n%s" % [
			Loc.t("ui.puzzle_correct"), Loc.t("puzzle.%s.explanation" % puzzle_id),
			Loc.t("ui.puzzle_parchment"), Loc.t("puzzle.%s.parchment" % puzzle_id),
			Loc.t("ui.puzzle_gain", {"n": gained})]
		_result_text.scroll_to_line(0)
		_result.visible = true
	else:
		var rec := _record()
		rec["wrong"] += 1
		_choice = -1
		_sequence = []
		_feedback.text = Loc.t("ui.puzzle_wrong", {"n": rec["wrong"]})
		State.touch()
		_update()


func is_result_shown() -> bool:
	return _result.visible


func close_result() -> void:
	visible = false
	closed.emit(true)


func quit() -> void:
	visible = false
	closed.emit(false)


func _unhandled_input(event: InputEvent) -> void:
	if visible and event.is_action_pressed("cancel"):
		get_viewport().set_input_as_handled()
		if _result.visible:
			close_result()
		else:
			quit()
