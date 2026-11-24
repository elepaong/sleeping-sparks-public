extends Node
## 처음부터 끝까지 자동으로 밟아 보는 시험. 정답은 data/에서 읽는다(여기에 적지 않는다).
##
## 실행 (헤드리스):
##   godot --headless --path . res://scenes/test/playthrough.tscn
## 화면을 찍으려면 창을 띄워 실행하고 폴더를 넘긴다:
##   godot --path . res://scenes/test/playthrough.tscn -- --shots=C:/어떤/폴더
## 끝나면 "PLAYTHROUGH OK" 또는 "PLAYTHROUGH FAILED"를 찍고, 종료 코드 0 / 1로 끝난다.
##
## 실제 게임과 같은 길을 쓴다: 루카가 걸어가서(길찾기) 조사·배치하고, 문 퍼즐 화면의 버튼 함수를 누른다.
## 키 입력은 입력 맵의 액션으로 넣는다(스파크 선택, 조사 모드, 초기화, 오메가 문서).

const Config = preload("res://scripts/core/config.gd")

var main: Node
var zv: Node2D
var failures: Array = []
var shots_dir := ""
var step_name := ""
var _shot_dialogues: Dictionary = {}


func _ready() -> void:
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--shots="):
			shots_dir = arg.substr(8)
	Engine.time_scale = 12.0
	main = load("res://scenes/main.tscn").instantiate()
	add_child(main)
	zv = main.zone_view
	await get_tree().process_frame
	await _run()
	if not Data.errors.is_empty():
		_fail("데이터 읽기 오류: " + str(Data.errors))
	if not Loc.missing.is_empty():
		_fail("문자열 키 없음: " + str(Loc.missing.keys()))
	if failures.is_empty():
		print("PLAYTHROUGH OK")
	else:
		print("PLAYTHROUGH FAILED (%d)" % failures.size())
		for f in failures:
			print("  - " + f)
	get_tree().quit(0 if failures.is_empty() else 1)


# ------------------------------------------------------------------ 도우미

func _step(text: String) -> void:
	step_name = text
	print("[단계] " + text)


func _fail(msg: String) -> void:
	failures.append("%s: %s" % [step_name, msg])
	print("  [실패] " + msg)


func _check(cond: bool, msg: String) -> void:
	if not cond:
		_fail(msg)


func _frames(n: int) -> void:
	for _i in n:
		await get_tree().process_frame


## 대사는 넘기고, 밀린 행동이 끝나거나 문 퍼즐·제작 창이 뜰 때까지 기다린다.
func _settle() -> void:
	for _i in 3000:
		await get_tree().process_frame
		if main.dialogue.visible:
			var txt: String = main.dialogue.current_text()
			_check(not txt.begins_with("#"), "대사 문장을 찾지 못함: " + txt)
			var did: String = main.dialogue.dialogue_id
			if shots_dir != "" and not _shot_dialogues.has(did) and did in ["jjirit_awakens", "vision_1", "stairs_open", "crafted_lens"]:
				_shot_dialogues[did] = true
				await _shot("dialogue_" + did)
			main.dialogue.advance()
		elif main.puzzle_screen.visible or main.craft.visible or main.end_screen.visible:
			return
		elif not main._running:
			return
	_fail("행동이 끝나지 않음 (멈춤)")


func _press(action: String) -> void:
	_check(InputMap.has_action(action), "입력 맵에 액션 없음: " + action)
	var ev := InputEventAction.new()
	ev.action = action
	ev.pressed = true
	Input.parse_input_event(ev)
	await _frames(2)
	var up := InputEventAction.new()
	up.action = action
	up.pressed = false
	Input.parse_input_event(up)
	await _frames(1)


## 루카가 사물까지 걸어가서 조사(interact)하거나 스파크를 배치(place)한다.
func _act(kind: String, obj: String, spark: String = "") -> void:
	if not zv.objects.has(obj):
		_fail("사물 없음: " + obj)
		return
	_check(zv.objects[obj].visible, "사물이 보이지 않음: " + obj)
	if not zv.can_reach(obj):
		_fail("걸어서 닿을 수 없음: " + obj)
		return
	zv.go_and({"kind": kind, "obj": obj, "spark": spark})
	for _i in 4000:
		if zv.path.is_empty() and zv.pending.is_empty():
			break
		await get_tree().process_frame
	_check(zv.path.is_empty() and zv.pending.is_empty(), "도착하지 못함: " + obj)
	await _settle()


func _state(obj: String) -> String:
	return zv.objects[obj].state


## 떠 있는 문 퍼즐을 푼다. wrong번 틀리고, hints개의 힌트를 본 뒤 정답을 낸다.
func _solve(id: String, wrong: int = 0, hints: int = 0) -> void:
	var ps: Control = main.puzzle_screen
	if not ps.visible or ps.puzzle_id != id:
		_fail("문 퍼즐 %s이(가) 뜨지 않음 (지금: %s)" % [id, ps.puzzle_id if ps.visible else "없음"])
		return
	var pz: Dictionary = Data.puzzles[id]
	var options: Array = pz["input"]["options"]
	var status: String = pz.get("status", "")
	_check(status in ["reviewed", "verified", "playtested"], "%s의 status가 '%s' — 검토를 거치지 않은 문제가 게임에 들어감" % [id, status])
	_check(ResourceLoader.exists("res://assets/puzzles/%s.png" % id), id + " 그림 없음")
	await _shot("puzzle_" + id)
	for _h in hints:
		var before: int = State.ink
		var shown: int = State.door_record(id)["hints"]
		ps.take_hint()
		if before >= Config.HINT_COSTS[shown]:
			_check(State.ink == before - Config.HINT_COSTS[shown], "힌트 값이 잉크에서 빠지지 않음")
			_check(State.door_record(id)["hints"] == shown + 1, "힌트가 열리지 않음")
		else:
			_check(State.ink == before and State.door_record(id)["hints"] == shown, "잉크가 모자란데 힌트가 열림")
	var wrong_before: int = State.door_record(id)["wrong"]
	ps.submit()   # 아무것도 고르지 않고 제출 → 오답으로 치지 않아야 한다
	_check(State.door_record(id)["wrong"] == wrong_before and not ps.is_result_shown(), "빈 답을 제출했는데 채점됨")
	for _w in wrong:
		if pz["input"]["kind"] == "choice":
			for i in options.size():
				if options[i] != pz["answer"]:
					ps.pick(i)
					break
		else:
			var answer_order: Array = pz["answer"]
			for i in range(options.size() - 1, -1, -1):   # 정답을 거꾸로 놓는다
				ps.pick(options.find(answer_order[i]))
		ps.submit()
		_check(not ps.is_result_shown(), "오답인데 정답 처리됨")
	_check(State.door_record(id)["wrong"] == wrong_before + wrong, "오답 횟수가 맞지 않음")
	if pz["input"]["kind"] == "choice":
		ps.pick(options.find(pz["answer"]))
	else:
		for a in pz["answer"]:
			ps.pick(options.find(a))
	var energy_before: int = State.energy
	ps.submit()
	_check(ps.is_result_shown(), "정답인데 해설이 뜨지 않음")
	var total_wrong: int = State.door_record(id)["wrong"]
	var expect: int = int(round(float(pz["energy"]) * maxf(0.5, 1.0 - 0.1 * total_wrong)))
	_check(State.energy == energy_before + expect, "에너지 %d를 얻어야 하는데 %d" % [expect, State.energy - energy_before])
	await _shot("solved_" + id)
	ps.close_result()
	await _settle()
	_check(State.is_solved(id), id + " 풀이가 기록되지 않음")


func _shot(shot_name: String) -> void:
	if shots_dir == "" or DisplayServer.get_name() == "headless":
		return
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(shots_dir.path_join(shot_name + ".png"))


func _walkable_spawns() -> void:
	for sp in zv.zone.get("spawns", {}):
		var p: Array = zv.zone["spawns"][sp]
		_check(zv.is_walkable(Vector2(p[0], p[1])), "들어오는 자리 '%s'가 걸을 수 없는 곳" % sp)


# ------------------------------------------------------------------ 처음부터 끝까지

func _run() -> void:
	_step("프롤로그 시작")
	await _settle()
	_check(State.zone == "scriptorium", "시작 지역이 필사실이 아님")
	_walkable_spawns()
	await _shot("01_scriptorium")
	await _press("spark_1")
	_check(State.selected_spark == "", "아직 없는 스파크가 선택됨")

	_step("키보드·마우스 이동")
	var before: Vector2 = zv.player.position
	Input.action_press("move_right")
	await _frames(20)
	Input.action_release("move_right")
	await _frames(2)
	_check(zv.player.position.x > before.x + 4.0, "오른쪽 키로 움직이지 않음")
	var click := InputEventMouseButton.new()
	click.button_index = MOUSE_BUTTON_LEFT
	click.pressed = true
	click.position = get_viewport().get_final_transform() * Vector2(400, 300)
	click.global_position = click.position
	Input.parse_input_event(click)
	await _frames(3)
	_check(not zv.path.is_empty(), "바닥을 클릭해도 걸어가지 않음")
	for _i in 2000:
		if zv.path.is_empty():
			break
		await get_tree().process_frame
	_check(zv.player.position.distance_to(Vector2(400, 300)) < 12.0, "클릭한 곳에 도착하지 않음: " + str(zv.player.position))

	_step("오메가 문서와 첫 수수께끼")
	await _act("interact", "desk")
	await _solve("D-STATIC-01", 1)
	_check("jjirit" in State.sparks, "찌릿을 얻지 못함")

	_step("필드 F-STATIC-01: 쇠 열쇠는 반응 없음, 종잇조각은 끌려옴")
	await _press("spark_1")
	_check(State.selected_spark == "jjirit", "1 키로 찌릿이 선택되지 않음")
	await _act("place", "grate_key", "jjirit")
	_check(not State.placed.has("jjirit"), "반응 없는 곳에 찌릿이 놓인 채로 남음")
	_check(not State.is_solved("F-STATIC-01"), "열쇠 창살에서 퍼즐이 풀림")
	await _act("place", "grate_scrap", "jjirit")
	_check(State.is_solved("F-STATIC-01"), "F-STATIC-01이 풀리지 않음")
	_check("isolde_note" in State.items, "쪽지를 얻지 못함")
	_check(not zv.objects["scrap"].visible, "끌어올린 종잇조각이 그대로 보임")

	_step("수반의 수수께끼 (힌트 사용)")
	_check(not zv.objects["ledge_left"].visible, "받침돌이 미리 보임")
	await _act("interact", "fountain")
	await _solve("D-STATIC-02", 0, 3)   # 잉크 2개로 힌트 2개, 셋째는 잉크가 모자라 열리지 않아야 한다
	_check(State.ink == 0, "잉크가 0이 아님: %d" % State.ink)
	_check(zv.objects["ledge_left"].visible and zv.objects["ledge_right"].visible, "받침돌이 나타나지 않음")

	_step("필드 F-STATIC-02: 물줄기")
	await _act("place", "ledge_right", "jjirit")
	_check(_state("stream") == "bent_right", "물줄기가 오른쪽으로 휘지 않음: " + _state("stream"))
	_check(not State.is_solved("F-STATIC-02"), "반대쪽 받침돌에서 퍼즐이 풀림")
	await _shot("02_stream_wrong")
	zv.right_click(zv.objects["ledge_right"].position)
	await _settle()
	_check(not State.placed.has("jjirit") and _state("stream") == "straight", "회수해도 물줄기가 돌아오지 않음")
	await _act("place", "ledge_left", "jjirit")
	_check(State.is_solved("F-STATIC-02"), "F-STATIC-02가 풀리지 않음")
	_check(_state("hatch") == "open", "계단이 열리지 않음")
	_check(not State.placed.has("jjirit"), "풀린 뒤 찌릿이 돌아오지 않음")

	_step("조사 모드와 잉크 방울")
	_check(not zv.objects["ink_1"].visible, "조사 모드가 아닌데 잉크가 보임")
	await _press("investigate")
	_check(State.investigate and zv.objects["ink_1"].visible, "조사 모드에서 잉크가 보이지 않음")
	await _shot("03_investigate")
	for ink in ["ink_1", "ink_2", "ink_3"]:
		await _act("interact", ink)
	_check(State.ink == 3, "잉크 3개를 줍지 못함: %d" % State.ink)
	await _press("investigate")
	_check(not State.investigate, "조사 모드가 꺼지지 않음")

	_step("오메가 문서")
	await _press("omega")
	_check(main.omega.visible, "Tab으로 오메가 문서가 열리지 않음")
	for t in ["sparks", "parchment", "puzzles"]:
		main.omega.show_tab(t)
		await _frames(2)
		_check(not ("#" in main.omega.body_text()), "오메가 문서 '%s' 쪽에 찾지 못한 문장이 있음" % t)
		await _shot("04_omega_" + t)
	await _press("omega")
	_check(not main.omega.visible, "오메가 문서가 닫히지 않음")

	_step("지하 서고로")
	await _act("interact", "hatch")
	_check(State.zone == "archive", "서고로 가지 못함")
	_walkable_spawns()
	await _shot("05_archive")

	_step("봉인의 문: 문 퍼즐 3개 연속")
	await _act("interact", "seal_door")
	_check(main.puzzle_screen.visible and main.puzzle_screen.puzzle_id == "D-LIGHT-01", "첫 고리 문제가 뜨지 않음")
	main.puzzle_screen.quit()   # 그만두기 → 다음에 같은 자리에서 다시
	await _settle()
	_check(not ("lumi" in State.sparks), "그만뒀는데 루미를 얻음")
	await _act("interact", "seal_door")
	await _solve("D-LIGHT-01", 0, 1)
	await _solve("D-LIGHT-02", 2)
	await _solve("D-LIGHT-03", 0)
	_check("lumi" in State.sparks, "루미를 얻지 못함")
	await _act("interact", "seal_door")
	_check(State.zone == "archive", "빛 문양을 켜기 전에 문이 열림")

	_step("필드 F-LIGHT-01: 거울 세 개")
	await _press("spark_2")
	_check(State.selected_spark == "lumi", "2 키로 루미가 선택되지 않음")
	await _act("place", "stand_1", "lumi")
	_check(_state("lock_1") == "off", "거울을 돌리기 전에 문양이 켜짐")
	await _shot("06_beam_start")
	await _act("interact", "mirror_1")
	await _act("interact", "mirror_3")
	_check(_state("lock_1") == "lit" and State.is_solved("F-LIGHT-01"), "F-LIGHT-01이 풀리지 않음")
	await _shot("07_lock1")
	await _press("reset_field")
	_check(_state("mirror_1") == zv.objects["mirror_1"].initial_state and not State.placed.has("lumi"), "R로 초기화되지 않음")
	_check(_state("lock_1") == "lit", "초기화가 이미 켜진 문양을 꺼 버림")

	_step("필드 F-LIGHT-02: 빛을 되돌리기")
	await _act("place", "stand_2", "lumi")
	_check(_state("lock_2") == "off", "거울을 돌리기 전에 둘째 문양이 켜짐")
	await _act("interact", "mirror_4")
	_check(_state("lock_2") == "off", "거울 하나만으로 둘째 문양이 켜짐")
	await _act("interact", "mirror_5")
	_check(_state("lock_2") == "lit" and State.is_solved("F-LIGHT-02"), "F-LIGHT-02가 풀리지 않음")
	_check(State.flags.has("lab_open"), "문이 열리지 않음")
	await _shot("08_lock2")

	_step("연구실")
	await _act("interact", "seal_door")
	_check(State.zone == "lab", "연구실로 가지 못함")
	_check(State.placed.is_empty(), "방을 나왔는데 스파크가 놓인 채로 남음")
	_walkable_spawns()
	await _shot("09_lab")
	await _act("interact", "workbench")   # 도면이 없을 때
	_check(not main.craft.visible and State.inventions.is_empty(), "도면 없이 제작 창이 뜸")
	await _act("interact", "chest")
	await _solve("D-LIGHT-04", 1)
	await _act("interact", "workbench")   # 재료가 없을 때
	_check(main.craft.visible and not main.craft.can_make(), "재료 없이 만들 수 있음")
	await _shot("10_craft_missing")
	main.craft.cancel()
	await _settle()
	await _act("interact", "slot_b")
	_check(_state("slot_b") == "empty", "집광기 없이 렌즈가 끼워짐")
	await _act("interact", "shelf")
	_check("glass_disc" in State.items, "유리 원반을 얻지 못함")
	await _act("interact", "workbench")
	_check(main.craft.visible and main.craft.can_make(), "재료를 모았는데 만들 수 없음")
	main.craft.make()
	await _settle()
	_check("convex_lens" in State.inventions, "집광기를 만들지 못함")

	_step("필드 F-LIGHT-03: 초점에 덩굴 맞추기")
	await _act("interact", "passage")
	_check(not State.ended, "덩굴을 태우기 전에 끝남")
	await _act("place", "stand_4", "lumi")
	_check(_state("vine") == "dry", "거울에 막혀야 할 빛이 덩굴에 닿음")
	await _act("place", "stand_3", "lumi")
	_check(_state("vine") == "dry", "거울을 돌리기 전에 빛이 덩굴에 닿음")
	# 거울을 돌리기 전: 빛이 왼쪽으로 꺾여 돌기둥(막힌 칸) 앞에서 멈춘다
	var last: Vector2i = zv.beam_cells[0][-1]["cell"]
	_check(last == zv.objects["mirror_6"].cell, "빛이 배경의 돌기둥 앞에서 멈추지 않음: " + str(last))
	await _shot("10b_beam_far_stand")
	await _act("interact", "mirror_6")
	_check(_state("vine") == "dry", "거울 하나만 돌렸는데 빛이 덩굴에 닿음")
	await _act("interact", "mirror_8")
	_check(_state("vine") == "lit_plain", "렌즈 없는 빛: " + _state("vine"))
	await _act("interact", "slot_c")
	_check(_state("vine") == "lit_converging", "렌즈가 너무 가까울 때: " + _state("vine"))
	await _shot("11_lens_near")
	await _act("interact", "slot_a")
	_check(_state("vine") == "lit_spread" and _state("slot_c") == "empty", "렌즈가 너무 멀 때: " + _state("vine"))
	await _act("interact", "slot_d")
	_check(_state("vine") == "lit_spread", "거울 앞 렌즈 틀: " + _state("vine"))
	_check(not State.is_solved("F-LIGHT-03"), "틀린 렌즈 틀에서 풀림")
	await _act("interact", "slot_b")
	await _shot("12_vine_burn")
	_check(_state("vine") == "burnt" and State.is_solved("F-LIGHT-03"), "F-LIGHT-03이 풀리지 않음: " + _state("vine"))
	_check(not zv.objects["vine"].visible, "탄 덩굴이 그대로 보임")

	_step("끝")
	await _act("interact", "passage")
	_check(main.end_screen.visible and State.ended, "끝 화면이 뜨지 않음")
	_check(main.end_screen.title_text() == "프로토타입 끝", "끝 화면 문구가 다름")
	await _shot("13_end")
	_check(State.field_solved.size() == 5, "필드 퍼즐 5개를 모두 풀지 않음")
	await _camera_check()


## 화면보다 큰 맵: 카메라가 루카를 따라가되 맵 밖을 비추지 않고, 길찾기가 맵 전체에 깔리는지 본다.
## 정식 큰 배경이 아직 없어서, 마지막 지역의 맵 크기만 화면 2×2개 분량으로 키워 흉내 낸다.
func _camera_check() -> void:
	_step("카메라: 화면보다 큰 맵")
	var screen := Vector2(640, 360)
	var cam: Camera2D = zv.camera
	_check(cam != null and cam.is_current(), "카메라가 없거나 꺼져 있음")
	_check(zv.map_size == Vector2(zv.get_node("Background").texture.get_size()), "맵 크기가 배경 그림 크기와 다름")
	await _frames(2)
	if zv.map_size == screen:
		_check(cam.get_screen_center_position().distance_to(screen / 2.0) < 1.0, "맵이 화면만 한데 화면이 움직임: " + str(cam.get_screen_center_position()))
	var big := screen * 2.0
	zv.walk_poly = PackedVector2Array([Vector2(8, 8), Vector2(big.x - 8, 8), big - Vector2(8, 8), Vector2(8, big.y - 8)])
	zv.set_map_size(big)
	_check(zv.astar.region.size == Vector2i(big / float(Config.NAV_CELL)), "길찾기 칸이 맵 전체에 깔리지 않음: " + str(zv.astar.region))
	# 맵 한가운데: 카메라가 루카를 그대로 따라간다
	zv.player.position = Vector2(700, 420)
	await _frames(3)
	var want := Vector2(700, 420 - Config.CAMERA_LIFT)
	_check(cam.get_screen_center_position().distance_to(want) < 1.0, "카메라가 루카를 따라가지 않음: " + str(cam.get_screen_center_position()))
	# 맵 구석: 카메라가 맵 끝에서 멈춘다
	zv.player.position = big - Vector2(20, 20)
	await _frames(3)
	_check(cam.get_screen_center_position().distance_to(big - screen / 2.0) < 1.0, "카메라가 맵 밖을 비춤: " + str(cam.get_screen_center_position()))
	zv.player.position = Vector2(20, 60)
	await _frames(3)
	_check(cam.get_screen_center_position().distance_to(screen / 2.0) < 1.0, "카메라가 맵 밖을 비춤(왼쪽 위): " + str(cam.get_screen_center_position()))
	# 첫 화면 밖의 바닥으로 클릭 이동
	zv.walk_to(big - Vector2(60, 60))
	_check(not zv.path.is_empty() and zv.path[-1].distance_to(big - Vector2(60, 60)) <= Config.NAV_CELL, "화면 밖 바닥까지 가는 길이 없음")
	await _shot("14_camera_big_map")
