extends Control
## 대화 창. 줄은 data/dialogue/*.json에서, 문장은 Loc(dlg.<대사 ID>.<줄 ID>)에서 가져온다.
## E, Space/Enter, 좌클릭으로 다음 줄.

const UiTheme = preload("res://scripts/ui/ui_theme.gd")

signal finished

var dialogue_id := ""
var index := 0
var _lines: Array = []
var _tint: ColorRect
var _portrait: TextureRect
var _name: Label
var _text: Label
var _next: Label
var _opened_frame := -1


func _ready() -> void:
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	theme = UiTheme.make()
	mouse_filter = Control.MOUSE_FILTER_STOP
	visible = false
	_tint = ColorRect.new()
	_tint.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_tint.color = Color(0.25, 0.4, 0.7, 0.35)
	_tint.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_tint)
	var panel := Panel.new()
	panel.position = Vector2(16, 262)
	panel.size = Vector2(608, 92)
	panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(panel)
	_portrait = TextureRect.new()
	_portrait.position = Vector2(8, 8)
	_portrait.size = Vector2(76, 76)
	_portrait.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	_portrait.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	_portrait.texture_filter = CanvasItem.TEXTURE_FILTER_LINEAR
	panel.add_child(_portrait)
	_name = UiTheme.label("", 10, UiTheme.ACCENT)
	_name.position = Vector2(94, 6)
	panel.add_child(_name)
	_text = UiTheme.label("", 10)
	_text.position = Vector2(94, 22)
	_text.size = Vector2(504, 60)
	_text.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_text.clip_text = false
	panel.add_child(_text)
	_next = UiTheme.label(Loc.t("ui.dialogue_next"), 7, Color(0.7, 0.7, 0.7))
	_next.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	_next.position = Vector2(400, 78)
	_next.size = Vector2(200, 10)
	panel.add_child(_next)


## 대사를 끝까지 보여 주고 돌아온다 (await).
func play(id: String, style: String = "") -> void:
	if not Data.dialogues.has(id):
		push_error("대사 없음: " + id)
		return
	dialogue_id = id
	_lines = Data.dialogues[id]
	index = 0
	_tint.visible = style == "vision"
	visible = true
	_opened_frame = Engine.get_process_frames()
	_show_line()
	await finished


func _show_line() -> void:
	var line: Dictionary = _lines[index]
	var speaker: String = line.get("speaker", "narrator")
	var text_x := 94.0
	var tex: Texture2D = null
	var path := "res://assets/portraits/%s_%s.png" % [speaker, line.get("expr", "neutral")]
	if speaker != "narrator" and ResourceLoader.exists(path):
		tex = load(path)
	_portrait.texture = tex
	_portrait.visible = tex != null
	if tex == null:
		text_x = 14.0
	_name.text = "" if speaker == "narrator" else Loc.t("speaker." + speaker) + (Loc.t("ui.vision_tag") if _tint.visible else "")
	_name.position.x = text_x
	_text.position.x = text_x
	_text.size.x = 598.0 - text_x
	_text.text = Loc.t("dlg.%s.%s" % [dialogue_id, line.get("id", "")])
	_text.add_theme_color_override("font_color", Color(0.82, 0.86, 0.95) if speaker == "narrator" else Color(0.95, 0.93, 0.88))


func current_text() -> String:
	return _text.text


func advance() -> void:
	if not visible:
		return
	index += 1
	if index >= _lines.size():
		visible = false
		finished.emit()
	else:
		_show_line()


func _input(event: InputEvent) -> void:
	if not visible:
		return
	var go := event.is_action_pressed("interact") or event.is_action_pressed("ui_accept") or event.is_action_pressed("place")
	if go:
		get_viewport().set_input_as_handled()
		if Engine.get_process_frames() != _opened_frame:
			advance()
