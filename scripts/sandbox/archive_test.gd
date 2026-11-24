extends Node2D
## 조합 시험: 서고 배경 + 루카 8방향 걷기/대기 + 찌릿·루미(대기/발동)
## 모든 노드는 이 스크립트가 만든다. 시험이 끝나면 정식 장면으로 옮긴다.
## 조작: 방향키/WASD 이동, 1 = 찌릿 발동, 2 = 루미 발동, F1 = 이동 영역 보기

const BG_PATH := "res://assets/backgrounds/archive.png"
const LUCA_WALK := "res://assets/chars/luca_walk.png"   # 128x384: 가로 4프레임, 세로 8방향
const LUCA_IDLE := "res://assets/chars/luca_idle.png"   # 64x384: 가로 2프레임, 세로 8방향
const SPARKS := [
	{"id": "jjirit", "key": KEY_1, "follow": Vector2(-20, -6), "color": Color(0.5, 0.8, 1.0)},
	{"id": "lumi",   "key": KEY_2, "follow": Vector2(22, -10), "color": Color(1.0, 0.9, 0.5)},
]

# 방향 순서는 CLAUDE.md 필드 캐릭터 표와 같다 (시트의 위에서부터).
const DIRS := ["down", "down_right", "right", "up_right", "up", "up_left", "left", "down_left"]
# atan2(y, x)를 45도 단위로 나눈 번호 0..7 → DIRS 번호 (화면 y는 아래가 +)
const SECTOR_TO_DIR := [2, 1, 0, 7, 6, 5, 4, 3]
const FRAME := Vector2i(32, 48)
const SPARK_FRAME := Vector2i(24, 24)
const WALK_SPEED := 70.0   # 640x360 기준 픽셀/초
const START := Vector2(330, 250)
# 크기 시험용 배율. 1.0 = 원본. 1.5나 2.0으로 바꿔 보고 맵과의 비율을 확인한다.
const LUCA_SCALE := 1.0
const SPARK_SCALE := 1.0

# 걸을 수 있는 바닥 (640x360 기준 좌표). F1로 확인하고 값을 고쳐서 쓴다.
var WALK_POLY := PackedVector2Array([
	Vector2(120, 222), Vector2(240, 180), Vector2(345, 162), Vector2(500, 168),
	Vector2(560, 198), Vector2(598, 226), Vector2(540, 272), Vector2(470, 300),
	Vector2(410, 334), Vector2(322, 354), Vector2(255, 330), Vector2(195, 302),
	Vector2(140, 270),
])

var luca: Node2D
var luca_sprite: AnimatedSprite2D
var luca_dir := 0
var sparks: Array = []   # {node, sprite, def}
var poly_line: Line2D


func _ready() -> void:
	_make_background()
	var actors := Node2D.new()
	actors.name = "Actors"
	actors.y_sort_enabled = true
	add_child(actors)
	_make_luca(actors)
	for def in SPARKS:
		_make_spark(actors, def)
	_make_hud()
	_make_debug_line()


func _make_background() -> void:
	var bg := Sprite2D.new()
	bg.name = "Background"
	bg.texture = load(BG_PATH)
	bg.centered = false
	add_child(bg)


# 시트(가로 cols프레임 x 세로 방향 8개)에서 방향별 애니메이션을 만든다.
func _add_sheet(frames: SpriteFrames, tex: Texture2D, prefix: String, cols: int, fps: float) -> void:
	for row in DIRS.size():
		var anim: String = prefix + "_" + DIRS[row]
		frames.add_animation(anim)
		frames.set_animation_speed(anim, fps)
		frames.set_animation_loop(anim, true)
		for col in cols:
			var at := AtlasTexture.new()
			at.atlas = tex
			at.region = Rect2(col * FRAME.x, row * FRAME.y, FRAME.x, FRAME.y)
			frames.add_frame(anim, at)


func _make_luca(parent: Node2D) -> void:
	var frames := SpriteFrames.new()
	if frames.has_animation("default"):
		frames.remove_animation("default")
	_add_sheet(frames, load(LUCA_WALK), "walk", 4, 8.0)
	_add_sheet(frames, load(LUCA_IDLE), "idle", 2, 2.0)
	luca = Node2D.new()
	luca.name = "Luca"
	luca.position = START
	parent.add_child(luca)
	luca_sprite = AnimatedSprite2D.new()
	luca_sprite.sprite_frames = frames
	luca_sprite.centered = false
	luca_sprite.scale = Vector2(LUCA_SCALE, LUCA_SCALE)
	luca_sprite.offset = Vector2(-FRAME.x / 2.0, -45)   # 발끝이 노드 위치
	luca.add_child(luca_sprite)
	luca_sprite.play("idle_down")


func _make_spark(parent: Node2D, def: Dictionary) -> void:
	var id: String = def["id"]
	var frames := SpriteFrames.new()
	if frames.has_animation("default"):
		frames.remove_animation("default")
	_add_spark_anim(frames, "idle", load("res://assets/sparks/%s_idle.png" % id), 4, 4.0, true)
	_add_spark_anim(frames, "burst", load("res://assets/sparks/%s_burst.png" % id), 6, 8.0, false)
	var node := Node2D.new()
	node.name = id
	node.position = START + def["follow"]
	parent.add_child(node)
	var sprite := AnimatedSprite2D.new()
	sprite.sprite_frames = frames
	sprite.centered = false
	sprite.scale = Vector2(SPARK_SCALE, SPARK_SCALE)
	sprite.offset = Vector2(-SPARK_FRAME.x / 2.0, -34)   # 땅에서 살짝 떠 있게
	node.add_child(sprite)
	sprite.play("idle")
	sprite.animation_finished.connect(func(): if sprite.animation == "burst": sprite.play("idle"))
	sparks.append({"node": node, "sprite": sprite, "def": def})


func _add_spark_anim(frames: SpriteFrames, anim: String, tex: Texture2D, count: int, fps: float, loop: bool) -> void:
	frames.add_animation(anim)
	frames.set_animation_speed(anim, fps)
	frames.set_animation_loop(anim, loop)
	for i in count:
		var at := AtlasTexture.new()
		at.atlas = tex
		at.region = Rect2(i * SPARK_FRAME.x, 0, SPARK_FRAME.x, SPARK_FRAME.y)
		frames.add_frame(anim, at)


func _make_hud() -> void:
	var layer := CanvasLayer.new()
	add_child(layer)
	var label := Label.new()
	label.text = "방향키/WASD 이동   1 찌릿   2 루미   F1 이동 영역"
	label.position = Vector2(4, 2)
	label.add_theme_font_size_override("font_size", 8)
	label.add_theme_color_override("font_outline_color", Color.BLACK)
	label.add_theme_constant_override("outline_size", 3)
	layer.add_child(label)


func _make_debug_line() -> void:
	poly_line = Line2D.new()
	poly_line.points = WALK_POLY
	poly_line.closed = true
	poly_line.width = 1.0
	poly_line.default_color = Color(1, 0.2, 0.2, 0.9)
	poly_line.z_index = 100
	poly_line.visible = false
	add_child(poly_line)


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_F1:
			poly_line.visible = not poly_line.visible
		for s in sparks:
			if event.keycode == s["def"]["key"]:
				s["sprite"].play("burst")


func _input_vector() -> Vector2:
	var v := Vector2.ZERO
	if Input.is_key_pressed(KEY_LEFT) or Input.is_key_pressed(KEY_A):
		v.x -= 1
	if Input.is_key_pressed(KEY_RIGHT) or Input.is_key_pressed(KEY_D):
		v.x += 1
	if Input.is_key_pressed(KEY_UP) or Input.is_key_pressed(KEY_W):
		v.y -= 1
	if Input.is_key_pressed(KEY_DOWN) or Input.is_key_pressed(KEY_S):
		v.y += 1
	return v.normalized()


func _process(delta: float) -> void:
	var v := _input_vector()
	if v != Vector2.ZERO:
		var sector := int(round(atan2(v.y, v.x) / (PI / 4.0)))
		luca_dir = SECTOR_TO_DIR[(sector + 8) % 8]
		var step := v * WALK_SPEED * delta
		var p := luca.position
		if Geometry2D.is_point_in_polygon(p + step, WALK_POLY):
			luca.position = p + step
		elif Geometry2D.is_point_in_polygon(p + Vector2(step.x, 0), WALK_POLY):
			luca.position = p + Vector2(step.x, 0)
		elif Geometry2D.is_point_in_polygon(p + Vector2(0, step.y), WALK_POLY):
			luca.position = p + Vector2(0, step.y)
		_play(luca_sprite, "walk_" + DIRS[luca_dir])
	else:
		_play(luca_sprite, "idle_" + DIRS[luca_dir])
	# 스파크는 루카 곁을 부드럽게 따라온다
	for s in sparks:
		var target: Vector2 = luca.position + s["def"]["follow"]
		s["node"].position = s["node"].position.lerp(target, clampf(4.0 * delta, 0.0, 1.0))


func _play(sprite: AnimatedSprite2D, anim: String) -> void:
	if sprite.animation != anim:
		sprite.play(anim)
