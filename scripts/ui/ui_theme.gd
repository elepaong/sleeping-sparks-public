extends RefCounted
## UI 공통 모양. 저장소에 글꼴 파일이 없어서 시스템 한글 글꼴을 쓴다(가정 Y11).
## 정식 글꼴을 넣으면 make()에서 FontFile을 불러오게 바꾼다.

const FONT_NAMES := ["Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Noto Sans KR", "NanumGothic"]
const FONT_SIZE := 9

const PAPER := Color(0.93, 0.88, 0.76)
const INK := Color(0.2, 0.16, 0.14)
const PANEL := Color(0.13, 0.12, 0.15, 0.94)
const ACCENT := Color(0.95, 0.78, 0.35)

static var _theme: Theme


static func make() -> Theme:
	if _theme != null:
		return _theme
	var font := SystemFont.new()
	font.font_names = PackedStringArray(FONT_NAMES)
	font.subpixel_positioning = TextServer.SUBPIXEL_POSITIONING_DISABLED
	var t := Theme.new()
	t.default_font = font
	t.default_font_size = FONT_SIZE
	t.set_stylebox("panel", "PanelContainer", box(PANEL, 6))
	t.set_stylebox("panel", "Panel", box(PANEL, 0))
	for st in ["normal", "hover", "pressed", "disabled", "focus"]:
		var c := Color(0.25, 0.23, 0.3)
		if st == "hover":
			c = Color(0.34, 0.31, 0.4)
		elif st == "pressed":
			c = Color(0.5, 0.4, 0.2)
		elif st == "disabled":
			c = Color(0.18, 0.17, 0.2)
		var sb := box(c, 3)
		if st == "focus":
			sb.draw_center = false
			sb.border_color = ACCENT
			sb.set_border_width_all(1)
		t.set_stylebox(st, "Button", sb)
	t.set_color("font_color", "Button", Color(0.95, 0.93, 0.88))
	for kind in ["Button", "Label"]:
		t.set_font_size("font_size", kind, FONT_SIZE)
		t.set_font("font", kind, font)
	t.set_font("normal_font", "RichTextLabel", font)
	t.set_font("bold_font", "RichTextLabel", font)
	t.set_color("font_disabled_color", "Button", Color(0.5, 0.5, 0.5))
	t.set_color("default_color", "RichTextLabel", Color(0.95, 0.93, 0.88))
	t.set_constant("line_separation", "RichTextLabel", 2)
	t.set_constant("line_spacing", "Label", 2)
	_theme = t
	return t


static func box(color: Color, margin: int) -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	sb.bg_color = color
	sb.set_corner_radius_all(2)
	sb.content_margin_left = margin
	sb.content_margin_right = margin
	sb.content_margin_top = maxi(margin - 1, 0)
	sb.content_margin_bottom = maxi(margin - 1, 0)
	return sb


static func label(text: String, size: int = FONT_SIZE, color: Color = Color(0.95, 0.93, 0.88)) -> Label:
	var l := Label.new()
	l.theme = make()   # 트리에 들어가기 전에도 크기를 이 글꼴로 계산하도록
	l.text = text
	l.add_theme_font_size_override("font_size", size)
	l.add_theme_color_override("font_color", color)
	return l


static func rich(size: int = FONT_SIZE) -> RichTextLabel:
	var r := RichTextLabel.new()
	r.theme = make()
	r.bbcode_enabled = true
	r.scroll_active = true
	r.add_theme_font_size_override("normal_font_size", size)
	r.add_theme_font_size_override("bold_font_size", size)
	r.mouse_filter = Control.MOUSE_FILTER_PASS
	return r


static func button(text: String) -> Button:
	var b := Button.new()
	b.theme = make()
	b.text = text
	b.focus_mode = Control.FOCUS_NONE
	return b
