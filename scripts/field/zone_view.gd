extends Node2D
## 필드 화면: 지역 하나(data/zones/<id>.json)를 그리고, 이동·조사·스파크 배치·빛줄기·필드 퍼즐 판정을 맡는다.
## 여기에는 내용이 없다. 무엇이 어디 있고 어떻게 반응하는지는 지역 데이터와 상호작용표에서 온다.

const Config = preload("res://scripts/core/config.gd")
const Beam = preload("res://scripts/field/beam.gd")
const FieldObject = preload("res://scripts/field/field_object.gd")

const LUCA_WALK := "res://assets/chars/luca_walk.png"   # 가로 4프레임, 세로 8방향 (규격 192×576 = 칸 48×72. 옛 32×48 시트도 읽힌다)
const LUCA_IDLE := "res://assets/chars/luca_idle.png"   # 가로 2프레임, 세로 8방향 (규격 96×576)
# 방향 순서는 CLAUDE.md 필드 캐릭터 표와 같다 (시트의 위에서부터).
const DIR_NAMES := ["down", "down_right", "right", "up_right", "up", "up_left", "left", "down_left"]
# atan2(y, x)를 45도 단위로 나눈 번호 0..7 → DIR_NAMES 번호 (화면 y는 아래가 +)
const SECTOR_TO_DIR := [2, 1, 0, 7, 6, 5, 4, 3]
const FOOT_MARGIN := 3   # 시트 칸 맨 아래에서 발끝 줄까지 (chickxel.md 루카 프롬프트)
const SPARK_FRAME := Vector2i(24, 24)
const FOLLOW_OFFSETS := [Vector2(-20, -4), Vector2(22, -8), Vector2(-30, 8), Vector2(32, 6)]
const BEAM_COLORS := {
	"plain": Color(1.0, 0.92, 0.45, 0.95),
	"converging": Color(1.0, 0.75, 0.3, 0.95),
	"focused": Color(1.0, 1.0, 0.9, 1.0),
	"spread": Color(1.0, 0.9, 0.5, 0.3),
}

var main: Node                       # scripts/main.gd (대사·문 퍼즐 등을 부탁할 곳)
var zone: Dictionary = {}
var zone_id := ""
var grid: Dictionary = {}
var objects: Dictionary = {}         # id → FieldObject
var walk_poly := PackedVector2Array()
var ambient := Color.WHITE           # 지역 조명 색 (지역 데이터 "ambient"). 루카·사물·스파크 그림에 곱한다
var solids: Array = []               # 배경에 그려진 것(기계, 기둥 …)이 막는 네모들 (지역 데이터 "solids")
var player: Node2D
var player_sprite: AnimatedSprite2D
var player_dir := 0
var followers: Dictionary = {}       # 스파크 id → {"node", "sprite"}
var astar: AStarGrid2D
var path: Array = []                 # 클릭 이동으로 따라갈 점들
var pending: Dictionary = {}         # 도착하면 할 일 {"kind", "obj", "spark"}
var beam_cells: Array = []           # 마지막으로 계산한 빛줄기 (시험·디버그용)
var map_size := Vector2(640, 360)    # 맵 크기 = 배경 그림 크기. 지역 데이터의 좌표는 모두 이 맵 기준 픽셀
var camera: Camera2D                 # 루카를 따라가며 맵 안에서만 움직인다 (맵이 화면만 하면 고정 화면)

var _flat_layer: Node2D
var _actors: Node2D
var _beam_layer: Node2D
var _debug_layer: Node2D
var _refreshing := false


# ------------------------------------------------------------------ 지역 불러오기

func load_zone(id: String, spawn: String) -> void:
	leave()
	for c in get_children():
		c.queue_free()
	objects.clear()
	followers.clear()
	path.clear()
	pending.clear()
	zone_id = id
	zone = Data.zones[id]
	State.zone = id
	grid = zone.get("grid", {})
	walk_poly = PackedVector2Array()
	for p in zone["walk_poly"]:
		walk_poly.append(Vector2(p[0], p[1]))

	ambient = Color(str(zone.get("ambient", "#ffffff")))
	solids.clear()
	for r in zone.get("solids", []):
		solids.append(Rect2(r[0], r[1], r[2], r[3]))

	var bg := Sprite2D.new()
	bg.name = "Background"
	bg.texture = load("res://" + zone["background"])
	bg.centered = false
	add_child(bg)
	_flat_layer = _layer("Flat")
	_actors = _layer("Actors")
	_actors.y_sort_enabled = true
	_beam_layer = _layer("Beams")
	_debug_layer = _layer("Debug")
	_debug_layer.visible = false
	camera = Camera2D.new()
	camera.name = "Camera"
	add_child(camera)
	camera.make_current()
	set_map_size(bg.texture.get_size(), false)

	var saved: Dictionary = State.obj_states.get(id, {})
	for d in zone.get("objects", []):
		var o: Node2D = FieldObject.new()
		o.setup(d, grid, saved.get(d["id"]))
		o.sprite.modulate = ambient
		objects[o.id] = o
		if o.is_flat():
			_flat_layer.add_child(o)
		else:
			_actors.add_child(o)

	var spawns: Dictionary = zone.get("spawns", {})
	var sp: Array = spawns.get(spawn, spawns.get("default", [320, 240]))
	_make_player(Vector2(sp[0], sp[1]))
	_follow_camera()
	refresh()
	_build_debug()


func leave() -> void:
	if zone_id != "" and not State.placed.is_empty():
		recall_all()


## 맵 크기를 정한다: 카메라가 맵 밖을 비추지 않게 하고, 길찾기 칸을 맵 전체에 깐다.
## 보통은 load_zone이 배경 그림 크기로 부른다. 시험에서는 화면보다 큰 맵을 흉내 낼 때 직접 부른다.
func set_map_size(size: Vector2, rebuild: bool = true) -> void:
	map_size = size
	camera.limit_left = 0
	camera.limit_top = 0
	camera.limit_right = int(size.x)
	camera.limit_bottom = int(size.y)
	if rebuild:
		_follow_camera()
		_build_nav()


## 카메라를 루카(몸 가운데)에 맞춘다. 도트가 번지지 않게 정수 픽셀에 맞춘다.
func _follow_camera() -> void:
	if camera != null and player != null:
		camera.position = (player.position + Vector2(0, -Config.CAMERA_LIFT)).round()


func _layer(n: String) -> Node2D:
	var l := Node2D.new()
	l.name = n
	add_child(l)
	return l


# ------------------------------------------------------------------ 루카와 스파크 (그림은 시험 장면과 같은 방식으로 읽는다)

func _add_sheet(frames: SpriteFrames, tex: Texture2D, prefix: String, cols: int, fps: float) -> Vector2i:
	# 칸 크기는 시트 크기에서 읽는다: 가로 cols칸, 세로 8방향. 그림을 바꿔 넣어도 코드를 고치지 않는다.
	var frame := Vector2i(tex.get_width() / cols, tex.get_height() / DIR_NAMES.size())
	for row in DIR_NAMES.size():
		var anim: String = prefix + "_" + DIR_NAMES[row]
		frames.add_animation(anim)
		frames.set_animation_speed(anim, fps)
		frames.set_animation_loop(anim, true)
		for col in cols:
			var at := AtlasTexture.new()
			at.atlas = tex
			at.region = Rect2(col * frame.x, row * frame.y, frame.x, frame.y)
			frames.add_frame(anim, at)
	return frame


func _make_player(at: Vector2) -> void:
	var frames := SpriteFrames.new()
	if frames.has_animation("default"):
		frames.remove_animation("default")
	var frame := _add_sheet(frames, load(LUCA_WALK), "walk", 4, 8.0)
	_add_sheet(frames, load(LUCA_IDLE), "idle", 2, 2.0)
	player = Node2D.new()
	player.name = "Luca"
	player.position = at
	_actors.add_child(player)
	player.add_child(_shadow(Config.CHAR_SHADOW, 1.0))
	player_sprite = AnimatedSprite2D.new()
	player_sprite.sprite_frames = frames
	player_sprite.centered = false
	player_sprite.scale = Vector2(Config.CHAR_SCALE, Config.CHAR_SCALE)
	player_sprite.offset = Vector2(-frame.x / 2.0, -(frame.y - FOOT_MARGIN))   # 발끝이 노드 위치
	player_sprite.modulate = ambient
	player.add_child(player_sprite)
	player_sprite.play("idle_" + DIR_NAMES[player_dir])


func _sync_followers() -> void:
	for id in State.sparks:
		if followers.has(id):
			continue
		var idle_path := "res://assets/sparks/%s_idle.png" % id
		var node := Node2D.new()
		node.name = "Spark_" + id
		node.position = player.position
		_actors.add_child(node)
		var glow_color := ""
		for sp in Data.sparks:
			if sp["id"] == id:
				glow_color = str(sp.get("glow", ""))
		if glow_color != "":
			node.add_child(_glow(Color(glow_color)))   # 빛나는 스파크는 그림자 대신 바닥에 빛
		else:
			node.add_child(_shadow(Config.SPARK_SHADOW, Config.SPARK_SHADOW_ALPHA))
		var sprite := AnimatedSprite2D.new()
		sprite.modulate = Color.WHITE.lerp(ambient, Config.SPARK_AMBIENT)
		if ResourceLoader.exists(idle_path):
			var frames := SpriteFrames.new()
			if frames.has_animation("default"):
				frames.remove_animation("default")
			_add_spark_anim(frames, "idle", load(idle_path), 4, 4.0, true)
			var burst_path := "res://assets/sparks/%s_burst.png" % id
			if ResourceLoader.exists(burst_path):
				_add_spark_anim(frames, "burst", load(burst_path), 6, 8.0, false)
			sprite.sprite_frames = frames
			sprite.play("idle")
			sprite.animation_finished.connect(func() -> void:
				if sprite.animation == "burst":
					sprite.play("idle"))
		sprite.centered = false
		sprite.scale = Vector2(Config.SPARK_SCALE, Config.SPARK_SCALE)
		sprite.offset = Vector2(-SPARK_FRAME.x / 2.0, -34)   # 땅에서 살짝 떠 있게
		node.add_child(sprite)
		followers[id] = {"node": node, "sprite": sprite}


## 발밑 그림자: 노드 위치(발끝)를 가운데로 한 납작한 타원
func _shadow(radius: Vector2, alpha: float) -> Polygon2D:
	var pts := PackedVector2Array()
	for i in 16:
		var a := TAU * i / 16.0
		pts.append(Vector2(cos(a) * radius.x, sin(a) * radius.y))
	var s := Polygon2D.new()
	s.name = "Shadow"
	s.polygon = pts
	s.color = Color(Config.SHADOW_COLOR, Config.SHADOW_COLOR.a * alpha)
	return s


## 스파크가 바닥에 떨구는 빛: 가운데가 밝고 가장자리로 사라지는 납작한 타원 (더하기 섞기)
func _glow(color: Color) -> Sprite2D:
	var grad := Gradient.new()
	grad.set_color(0, color)
	grad.set_color(1, Color(0, 0, 0, 1))
	var tex := GradientTexture2D.new()
	tex.gradient = grad
	tex.fill = GradientTexture2D.FILL_RADIAL
	tex.fill_from = Vector2(0.5, 0.5)
	tex.fill_to = Vector2(1.0, 0.5)
	tex.width = 64
	tex.height = 64
	var mat := CanvasItemMaterial.new()
	mat.blend_mode = CanvasItemMaterial.BLEND_MODE_ADD
	var s := Sprite2D.new()
	s.name = "Glow"
	s.texture = tex
	s.material = mat
	s.scale = Config.SPARK_GLOW_RADIUS / 32.0
	return s


func _add_spark_anim(frames: SpriteFrames, anim: String, tex: Texture2D, count: int, fps: float, loop: bool) -> void:
	frames.add_animation(anim)
	frames.set_animation_speed(anim, fps)
	frames.set_animation_loop(anim, loop)
	for i in count:
		var at := AtlasTexture.new()
		at.atlas = tex
		at.region = Rect2(i * SPARK_FRAME.x, 0, SPARK_FRAME.x, SPARK_FRAME.y)
		frames.add_frame(anim, at)


func _burst(spark_id: String) -> void:
	if followers.has(spark_id):
		var s: AnimatedSprite2D = followers[spark_id]["sprite"]
		if s.sprite_frames != null and s.sprite_frames.has_animation("burst"):
			s.play("burst")


# ------------------------------------------------------------------ 매 프레임: 이동

func _process(delta: float) -> void:
	if player == null:
		return
	var moving := false
	if not main.is_busy():
		var v := Input.get_vector("move_left", "move_right", "move_up", "move_down")
		if v != Vector2.ZERO:
			path.clear()
			pending.clear()
			var step := v.normalized() * Config.WALK_SPEED * delta
			var p := player.position
			if is_walkable(p + step):
				player.position = p + step
			elif is_walkable(p + Vector2(step.x, 0)):
				player.position = p + Vector2(step.x, 0)
			elif is_walkable(p + Vector2(0, step.y)):
				player.position = p + Vector2(0, step.y)
			_face(v)
			moving = true
		elif not path.is_empty():
			var target: Vector2 = path[0]
			var to := target - player.position
			var dist := Config.WALK_SPEED * delta
			if to.length() <= dist:
				player.position = target
				path.pop_front()
				if path.is_empty():
					_arrive()
			else:
				player.position += to.normalized() * dist
				_face(to)
			moving = true
	_play(player_sprite, ("walk_" if moving else "idle_") + DIR_NAMES[player_dir])
	var i := 0
	for id in followers:
		var node: Node2D = followers[id]["node"]
		var target: Vector2
		if State.placed.has(id) and objects.has(State.placed[id]):
			target = objects[State.placed[id]].position + Vector2(0, 1)
		else:
			target = player.position + FOLLOW_OFFSETS[i % FOLLOW_OFFSETS.size()]
		node.position = node.position.lerp(target, clampf(6.0 * delta, 0.0, 1.0))
		i += 1
	_follow_camera()


func _face(v: Vector2) -> void:
	var sector := int(round(atan2(v.y, v.x) / (PI / 4.0)))
	player_dir = SECTOR_TO_DIR[posmod(sector, 8)]


func _play(sprite: AnimatedSprite2D, anim: String) -> void:
	if sprite.animation != anim:
		sprite.play(anim)


func _arrive() -> void:
	if pending.is_empty():
		return
	var a := pending
	pending = {}
	if not objects.has(a["obj"]):
		return
	var o: Node2D = objects[a["obj"]]
	if player.position.distance_to(o.position) <= Config.REACH:
		_face(o.position - player.position)
		_do(a)
	else:
		main.toast(Loc.t("ui.cant_reach"))


# ------------------------------------------------------------------ 입력

func _unhandled_input(event: InputEvent) -> void:
	if player == null or main.is_busy():
		return
	if event.is_action_pressed("interact"):
		interact_nearest()
	elif event.is_action_pressed("investigate"):
		toggle_investigate()
	elif event.is_action_pressed("reset_field"):
		reset_field()
	elif event.is_action_pressed("debug_view"):
		_debug_layer.visible = not _debug_layer.visible
	elif event.is_action_pressed("spark_next"):
		State.cycle_spark(1)
	elif event.is_action_pressed("spark_prev"):
		State.cycle_spark(-1)
	elif event.is_action_pressed("place"):
		click(_event_map_pos(event))
	elif event.is_action_pressed("recall"):
		right_click(_event_map_pos(event))
	else:
		for n in range(1, 9):
			if event.is_action_pressed("spark_%d" % n):
				if n <= Data.sparks.size() and Data.sparks[n - 1]["id"] in State.sparks:
					State.select_spark(Data.sparks[n - 1]["id"])
				else:
					main.toast(Loc.t("ui.no_such_spark"))


## 클릭한 자리를 맵 좌표로 (카메라가 움직여도 맞게). 마우스가 아닌 입력이면 지금 마우스 위치.
func _event_map_pos(event: InputEvent) -> Vector2:
	if event is InputEventMouse:
		return make_input_local(event).position
	return get_global_mouse_position()


func toggle_investigate() -> void:
	State.investigate = not State.investigate
	State.touch()
	refresh()
	main.toast(Loc.t("ui.investigate_on" if State.investigate else "ui.investigate_off"))


## 좌클릭: 스파크를 골랐고 배치할 수 있는 사물이면 배치, 아니면 조사, 빈 바닥이면 이동
func click(pos: Vector2) -> void:
	var o: Node2D = object_at(pos)
	if o != null and State.selected_spark != "" and o.is_placeable():
		go_and({"kind": "place", "obj": o.id, "spark": State.selected_spark})
	elif o != null and o.can_interact():
		go_and({"kind": "interact", "obj": o.id})
	else:
		walk_to(pos)


## 우클릭: 그 자리의 스파크를 회수. 없으면 고른 스파크를 회수
func right_click(pos: Vector2) -> void:
	var o: Node2D = object_at(pos)
	if o != null:
		for id in State.placed:
			if State.placed[id] == o.id:
				recall_spark(id)
				return
	if State.placed.has(State.selected_spark):
		recall_spark(State.selected_spark)
	elif State.placed.size() == 1:
		recall_spark(State.placed.keys()[0])


func object_at(pos: Vector2) -> Node2D:
	var best: Node2D = null
	var best_area := INF
	for id in objects:
		var o: Node2D = objects[id]
		if not o.visible or not (o.can_interact() or o.is_placeable()):
			continue
		var r: Rect2 = o.hotspot()
		if r.has_point(pos) and r.get_area() < best_area:
			best = o
			best_area = r.get_area()
	return best


func interact_nearest() -> void:
	var best: Node2D = null
	var best_d: float = Config.REACH
	for id in objects:
		var o: Node2D = objects[id]
		if not o.can_interact():
			continue
		var d := player.position.distance_to(o.position)
		if d <= best_d:
			best = o
			best_d = d
	if best != null:
		_face(best.position - player.position)
		interact(best)


func go_and(action: Dictionary) -> void:
	var o: Node2D = objects[action["obj"]]
	if player.position.distance_to(o.position) <= Config.REACH:
		path.clear()
		pending.clear()
		_face(o.position - player.position)
		_do(action)
		return
	var p := _approach_path(o.position)
	if p.is_empty():
		main.toast(Loc.t("ui.cant_reach"))
		return
	path = p
	pending = action


func _do(action: Dictionary) -> void:
	var o: Node2D = objects[action["obj"]]
	if action["kind"] == "place":
		place_spark(action["spark"], o)
	else:
		interact(o)


# ------------------------------------------------------------------ 조사

func interact(o: Node2D) -> void:
	if o.type == "ink":
		_set_state(o, "taken")
		State.ink += 1
		State.touch()
		refresh()
		main.toast(Loc.t("ui.ink_found", {"n": State.ink}))
		return
	if o.type == "mirror":
		_set_state(o, "\\" if o.state == "/" else "/")
		refresh()
		return
	if o.def.has("holds") and o.def["holds"] in State.inventions:
		var put: bool = o.state != "lens"
		for id in objects:
			var other: Node2D = objects[id]
			if other.def.get("holds", "") == o.def["holds"] and other.state == "lens":
				_set_state(other, other.initial_state)
		if put:
			_set_state(o, "lens")
		refresh()
		main.toast(Loc.t("ui.lens_put" if put else "ui.lens_taken"))
		return
	for branch in o.def.get("interact", []):
		if State.check(branch.get("if")):
			main.run_script(branch.get("do", []))
			return


# ------------------------------------------------------------------ 스파크 배치·회수

func place_spark(spark_id: String, o: Node2D) -> void:
	if not (spark_id in State.sparks) or not o.is_placeable():
		return
	if State.placed.has(spark_id):
		recall_spark(spark_id)
	for other in State.placed.keys():
		if State.placed[other] == o.id:
			recall_spark(other)
	State.placed[spark_id] = o.id
	var reacted := false
	for tid in o.def.get("targets", [o.id]):
		if not objects.has(tid) or not objects[tid].visible:
			continue
		var t: Node2D = objects[tid]
		var row := Data.find_interaction(t.type, t.state, spark_id, "place", State.inventions)
		if row.is_empty():
			continue
		reacted = true
		var effect: Dictionary = row["effect"]
		if effect.get("type", "") == "set_state":
			var s: String = str(effect["state"]).format(o.def.get("params", {}))
			_set_state(t, s)
		main.toast(Loc.t("interaction.%s.result" % row["id"]))
	if not reacted:
		# 표에 없는 조합은 반응하지 않는다
		State.placed.erase(spark_id)
		State.touch()
		if o.def.has("no_reaction"):
			main.run_script([{"dialogue": o.def["no_reaction"]}])
		else:
			main.toast(Loc.t("ui.no_reaction"))
		return
	_burst(spark_id)
	State.touch()
	refresh()


func recall_spark(spark_id: String) -> void:
	if not State.placed.has(spark_id):
		return
	var o: Node2D = objects.get(State.placed[spark_id])
	State.placed.erase(spark_id)
	if o != null:
		for tid in o.def.get("targets", [o.id]):
			if not objects.has(tid):
				continue
			var t: Node2D = objects[tid]
			var row := Data.find_interaction(t.type, t.initial_state, spark_id, "place", State.inventions)
			if not row.is_empty() and not row.get("persistent", false) and row["effect"].get("type", "") == "set_state":
				_set_state(t, t.initial_state)
	State.touch()
	refresh()


func recall_all() -> void:
	for id in State.placed.keys():
		recall_spark(id)


## 필드 퍼즐 초기화: 스파크를 모두 회수하고, 손으로 돌리거나 끼운 것(거울, 렌즈 틀)을 처음 상태로.
## 이미 일어난 일(켜진 문양, 탄 덩굴 …)은 그대로 둔다.
func reset_field() -> void:
	recall_all()
	for id in objects:
		var o: Node2D = objects[id]
		if o.type == "mirror" or o.def.has("holds"):
			_set_state(o, o.initial_state)
	refresh()
	main.toast(Loc.t("ui.field_reset"))


func set_object_state(id: String, s: String) -> void:
	if objects.has(id):
		_set_state(objects[id], s)
		refresh()
	else:
		push_error("set_state: '%s' 지역에 사물 '%s' 없음" % [zone_id, id])


func _set_state(o: Node2D, s: String, transient: bool = false) -> bool:
	o.beam_transient = transient
	if not o.set_state(s):
		return false
	if not State.obj_states.has(zone_id):
		State.obj_states[zone_id] = {}
	State.obj_states[zone_id][o.id] = s
	var actions: Array = o.def.get("on_state", {}).get(s, [])
	if not actions.is_empty():
		main.run_script(actions)
	return true


# ------------------------------------------------------------------ 다시 계산: 보임, 빛줄기, 길, 퍼즐 판정

func refresh() -> void:
	if _refreshing:
		return
	_refreshing = true
	_sync_followers()
	for id in objects:
		objects[id].update_visibility()
	_trace_beams()
	for id in objects:
		objects[id].update_visibility()
	_build_nav()
	_check_puzzles()
	for id in objects:
		objects[id].update_visibility()   # 방금 풀린 필드 퍼즐에 따라 보이는 것(열린 통로 …)
	_refreshing = false


func _trace_beams() -> void:
	for c in _beam_layer.get_children():
		c.queue_free()
	beam_cells.clear()
	var lit: Dictionary = {}   # 이번에 빛이 닿은 사물 id
	if not grid.is_empty():
		var by_cell: Dictionary = {}
		var blocked: Dictionary = {}
		for r in grid.get("blocked", []):
			for bx in range(int(r[0]), int(r[2]) + 1):
				for by in range(int(r[1]), int(r[3]) + 1):
					blocked[Vector2i(bx, by)] = true
		for id in objects:
			var o: Node2D = objects[id]
			if o.has_cell and o.visible:
				by_cell[o.cell] = o.beam_info()
		for spark_id in State.placed:
			var stand: Node2D = objects.get(State.placed[spark_id])
			if stand == null or not stand.has_cell:
				continue
			var row := Data.find_interaction(stand.type, stand.state, spark_id, "place", State.inventions)
			if row.is_empty() or row["effect"].get("type", "") != "emit_beam":
				continue
			var focal := 0
			for inv_id in State.inventions:
				focal = maxi(focal, int(Data.inventions[inv_id].get("focal_cells", 0)))
			var sid: String = spark_id
			var result := Beam.trace(int(grid["cols"]), int(grid["rows"]), by_cell, stand.cell, stand.def.get("dir", "E"), focal,
				func(o: Dictionary) -> Dictionary:
					return Data.find_interaction(o["type"], o["state"], sid, "beam", State.inventions),
				blocked)
			_draw_beam(result["path"])
			beam_cells.append(result["path"])
			for hit in result["hits"]:
				var t: Node2D = objects[hit["id"]]
				var effect: Dictionary = hit["row"]["effect"]
				var when: Array = effect.get("when_beam", ["plain", "converging", "focused"])
				lit[t.id] = true
				if hit["status"] in when:
					if _set_state(t, str(effect["state"]), not hit["row"].get("persistent", false)):
						main.toast(Loc.t("interaction.%s.result" % hit["row"]["id"]))
				elif effect.has("else_state_prefix"):
					_set_state(t, str(effect["else_state_prefix"]) + str(hit["status"]), true)
	for id in objects:
		var o: Node2D = objects[id]
		if o.beam_transient and not lit.has(id):
			_set_state(o, o.initial_state)


func cell_center(c: Vector2i) -> Vector2:
	var o: Array = grid["origin"]
	var s: Array = grid["cell"]
	return Vector2(o[0] + (c.x + 0.5) * s[0], o[1] + (c.y + 0.5) * s[1])


func _draw_beam(cells: Array) -> void:
	var lift := Vector2(0, -Config.BEAM_HEIGHT)
	for i in range(1, cells.size()):
		var status: String = cells[i]["status"]
		var line := Line2D.new()
		line.points = PackedVector2Array([cell_center(cells[i - 1]["cell"]) + lift, cell_center(cells[i]["cell"]) + lift])
		line.width = 5.0 if status == "spread" else (3.0 if status == "focused" else 2.0)
		line.default_color = BEAM_COLORS.get(status, BEAM_COLORS["plain"])
		_beam_layer.add_child(line)
		if status == "focused":
			var dot := Polygon2D.new()
			var c := cell_center(cells[i]["cell"]) + lift
			dot.polygon = PackedVector2Array([c + Vector2(-4, 0), c + Vector2(0, -4), c + Vector2(4, 0), c + Vector2(0, 4)])
			dot.color = Color(1, 1, 1, 1)
			_beam_layer.add_child(dot)


func _check_puzzles() -> void:
	for pz in zone.get("puzzles", []):
		if State.is_solved(pz["id"]):
			continue
		var ok := true
		for cond in pz.get("solved_when", []):
			if not objects.has(cond["object"]) or objects[cond["object"]].state != cond["state"]:
				ok = false
		if ok:
			State.field_solved.append(pz["id"])
			State.touch()
			main.toast(Loc.t("ui.field_solved", {"name": Loc.t(pz["name_key"])}))
			main.run_script(pz.get("on_solved", []))


# ------------------------------------------------------------------ 걸을 수 있는 곳, 길찾기

func is_walkable(p: Vector2) -> bool:
	if not Geometry2D.is_point_in_polygon(p, walk_poly):
		return false
	for s in solids:
		if s.has_point(p):
			return false
	for id in objects:
		var r: Rect2 = objects[id].solid_rect()
		if r.size != Vector2.ZERO and r.grow(2.0).has_point(p):
			return false
	return true


func _build_nav() -> void:
	var n: int = Config.NAV_CELL
	astar = AStarGrid2D.new()
	astar.region = Rect2i(0, 0, ceili(map_size.x / n), ceili(map_size.y / n))
	astar.cell_size = Vector2(n, n)
	astar.diagonal_mode = AStarGrid2D.DIAGONAL_MODE_ONLY_IF_NO_OBSTACLES
	astar.update()
	for x in astar.region.size.x:
		for y in astar.region.size.y:
			if not is_walkable(_nav_center(Vector2i(x, y))):
				astar.set_point_solid(Vector2i(x, y), true)


func _nav_center(c: Vector2i) -> Vector2:
	var n: int = Config.NAV_CELL
	return Vector2(c.x * n + n / 2.0, c.y * n + n / 2.0)


func _nav_cell(p: Vector2) -> Vector2i:
	var n: int = Config.NAV_CELL
	return Vector2i(clampi(int(p.x / n), 0, astar.region.size.x - 1), clampi(int(p.y / n), 0, astar.region.size.y - 1))


func _nearest_free(c: Vector2i) -> Vector2i:
	if not astar.is_point_solid(c):
		return c
	var best := Vector2i(-1, -1)
	var best_d := INF
	for r in range(1, 16):
		for x in range(c.x - r, c.x + r + 1):
			for y in range(c.y - r, c.y + r + 1):
				var q := Vector2i(x, y)
				if not astar.region.has_point(q) or astar.is_point_solid(q):
					continue
				var d := Vector2(q - c).length()
				if d < best_d:
					best = q
					best_d = d
		if best.x >= 0:
			return best
	return best


func _path_between(from: Vector2, to_cell: Vector2i) -> Array:
	var a := _nearest_free(_nav_cell(from))
	if a.x < 0 or to_cell.x < 0:
		return []
	var out: Array = []
	for c in astar.get_id_path(a, to_cell):
		out.append(_nav_center(c))
	if out.size() > 1:
		out.pop_front()   # 첫 점은 지금 서 있는 칸
	return out


func walk_to(pos: Vector2) -> void:
	pending.clear()
	path = _path_between(player.position, _nearest_free(_nav_cell(pos)))


## 사물에 손이 닿는 곳까지 가는 길 (닿을 수 있는 빈 칸 가운데 루카에게 가장 가까운 곳)
func _approach_path(target: Vector2) -> Array:
	var n: int = Config.NAV_CELL
	var reach: float = Config.REACH - n
	var c0 := _nav_cell(target)
	var span := int(Config.REACH / n) + 1
	var cands: Array = []
	for x in range(c0.x - span, c0.x + span + 1):
		for y in range(c0.y - span, c0.y + span + 1):
			var q := Vector2i(x, y)
			if astar.region.has_point(q) and not astar.is_point_solid(q) and _nav_center(q).distance_to(target) <= reach:
				cands.append(q)
	cands.sort_custom(func(a: Vector2i, b: Vector2i) -> bool:
		return _nav_center(a).distance_squared_to(player.position) < _nav_center(b).distance_squared_to(player.position))
	for q in cands:
		var p := _path_between(player.position, q)
		if not p.is_empty():
			return p
	return []


## 시험용: 그 사물에 지금 걸어서 닿을 수 있는가
func can_reach(id: String) -> bool:
	var o: Node2D = objects[id]
	return player.position.distance_to(o.position) <= Config.REACH or not _approach_path(o.position).is_empty()


# ------------------------------------------------------------------ F1: 이동 영역·칸·막힌 곳 보기

func _build_debug() -> void:
	var poly := Line2D.new()
	poly.points = walk_poly
	poly.closed = true
	poly.width = 1.0
	poly.default_color = Color(1, 0.2, 0.2, 0.9)
	_debug_layer.add_child(poly)
	if not grid.is_empty():
		var o: Array = grid["origin"]
		var s: Array = grid["cell"]
		for c in int(grid["cols"]) + 1:
			_debug_line(Vector2(o[0] + c * s[0], o[1]), Vector2(o[0] + c * s[0], o[1] + grid["rows"] * s[1]))
		for r in int(grid["rows"]) + 1:
			_debug_line(Vector2(o[0], o[1] + r * s[1]), Vector2(o[0] + grid["cols"] * s[0], o[1] + r * s[1]))
	var rects: Array = solids.duplicate()
	for id in objects:
		rects.append(objects[id].solid_rect())
	for r in rects:
		if r.size != Vector2.ZERO:
			var box := Line2D.new()
			box.points = PackedVector2Array([r.position, Vector2(r.end.x, r.position.y), r.end, Vector2(r.position.x, r.end.y)])
			box.closed = true
			box.width = 1.0
			box.default_color = Color(0.2, 0.6, 1.0, 0.9)
			_debug_layer.add_child(box)


func _debug_line(a: Vector2, b: Vector2) -> void:
	var l := Line2D.new()
	l.points = PackedVector2Array([a, b])
	l.width = 1.0
	l.default_color = Color(0.3, 1.0, 0.5, 0.5)
	_debug_layer.add_child(l)
