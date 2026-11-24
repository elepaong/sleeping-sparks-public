extends Node2D
## 필드의 사물 하나. 모양·위치·반응은 모두 data/zones/<id>.json의 항목(def)에서 온다.

const Config = preload("res://scripts/core/config.gd")

var def: Dictionary = {}
var id := ""
var type := ""
var state := "default"
var initial_state := "default"
var cell := Vector2i(-1, -1)      # 칸 위의 사물이면 칸 좌표
var has_cell := false
var beam_transient := false       # 지금 상태가 '빛이 닿아 있는 동안만'인 상태인가
var sprite: Sprite2D
var name_label: Label
var _textures: Dictionary = {}


func setup(d: Dictionary, grid: Dictionary, saved_state: Variant) -> void:
	def = d
	id = d["id"]
	name = id
	type = d.get("type", "prop")
	initial_state = str(d.get("state", "default"))
	state = str(saved_state) if saved_state != null else initial_state
	if d.has("cell") and not grid.is_empty():
		has_cell = true
		cell = Vector2i(int(d["cell"][0]), int(d["cell"][1]))
		var o: Array = grid["origin"]
		var c: Array = grid["cell"]
		position = Vector2(o[0] + (cell.x + 0.5) * c[0], o[1] + (cell.y + 0.5) * c[1])
	else:
		position = Vector2(d["pos"][0], d["pos"][1])
	sprite = Sprite2D.new()
	sprite.centered = false
	add_child(sprite)
	if Config.SHOW_LABELS and d.get("label", true) and d.has("name_key"):
		name_label = Label.new()
		name_label.text = Loc.t(d["name_key"])
		name_label.add_theme_font_size_override("font_size", 6)
		name_label.add_theme_color_override("font_color", Color(1, 1, 1, 0.9))
		name_label.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.9))
		name_label.add_theme_constant_override("outline_size", 3)
		name_label.z_index = 50
		name_label.z_as_relative = false
		add_child(name_label)
	_apply_look()


func is_flat() -> bool:
	return def.get("flat", false)


func set_state(s: String) -> bool:
	if s == state:
		return false
	state = s
	_apply_look()
	return true


func _texture_for(s: String) -> Texture2D:
	var path := ""
	if def.has("sprites"):
		path = def["sprites"].get(s, "")
	elif def.get("sprite") != null:
		path = def["sprite"]
	if path == "":
		return null
	if not _textures.has(path):
		_textures[path] = load("res://" + path)
	return _textures[path]


func _apply_look() -> void:
	var tex := _texture_for(state)
	sprite.texture = tex
	var size := Vector2(32, 32)
	if tex != null:
		size = tex.get_size()
	var off := Vector2.ZERO
	if def.has("sprite_offset"):
		off = Vector2(def["sprite_offset"][0], def["sprite_offset"][1])
	if is_flat():
		sprite.position = -size / 2.0 + off
	else:
		sprite.position = Vector2(-size.x / 2.0, -size.y + 8) + off   # 발치가 노드 위치보다 조금 아래
	if name_label != null:
		var r := hotspot()
		name_label.reset_size()
		name_label.position = Vector2(r.position.x + r.size.x / 2.0, r.position.y) - position - Vector2(name_label.size.x / 2.0, name_label.size.y)


## 지금 화면에 있는가 (visible_if 조건, 숨는 상태)
func update_visibility() -> void:
	var v := true
	if def.has("visible_if") and not State.check(def["visible_if"]):
		v = false
	if state in def.get("hidden_states", []):
		v = false
	if type == "ink" and (state == "taken" or not State.investigate):
		v = false
	visible = v


## 클릭·이름표 영역 (화면 좌표)
func hotspot() -> Rect2:
	if def.has("hotspot"):
		var h: Array = def["hotspot"]
		return Rect2(h[0], h[1], h[2], h[3])
	var size := Vector2(32, 32)
	if sprite != null and sprite.texture != null:
		size = sprite.texture.get_size()
	return Rect2(position + sprite.position, size)


## 걸음을 막는 영역 (없으면 크기 0)
func solid_rect() -> Rect2:
	if not visible or not def.has("solid"):
		return Rect2()
	var s: Array = def["solid"]
	return Rect2(position.x - s[0] / 2.0, position.y - s[1] / 2.0, s[0], s[1])


func is_placeable() -> bool:
	return visible and def.get("placeable", false) and State.check(def.get("place_if"))


func can_interact() -> bool:
	if not visible:
		return false
	if type == "ink":
		return true
	return type == "mirror" or type == "lens_slot" or def.has("interact")


func beam_info() -> Dictionary:
	return {"id": id, "type": type, "state": state, "blocks_beam": def.get("blocks_beam", false)}
