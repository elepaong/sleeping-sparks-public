#!/usr/bin/env python3
"""data/ 아래 JSON이 서로 맞물리는지 점검한다 (CLAUDE.md 데이터 규칙).

사용: python tools/verify/data_check.py [저장소 루트]
점검:
  - 모든 JSON이 읽히는지, ID가 겹치지 않는지
  - 상호작용표의 스파크·발명품이 sparks.json·inventions.json에 있는지
  - 문 퍼즐: 템플릿의 항목이 모두 있는지, 파일명과 id가 같은지, 스파크가 있는지,
    힌트가 4개인지, 정답이 선택지 안에 있는지
  - status가 draft보다 높은 퍼즐은 verification이 채워져 있는지
  - 상호작용표에서 게임이 쓰는 줄(effect가 있는 줄)의 trigger·effect 형식
  - 대사(data/dialogue/*.json): 줄 ID, 말하는 이, 표정, 초상화 파일
  - 지역(data/zones/*.json): 필수 항목, 그림 파일, 문자열 키, 조건·행동이 가리키는 대사·퍼즐·지역·사물이
    실제로 있는지, 게임에 넣은 문 퍼즐이 reviewed 이상인지 (형식은 design/ch1-prototype.md 9절)
  - 문자열 테이블(data/strings/ko.json)
  - 스테이지 테마(data/stages/*.json): index.json의 스테이지가 있는지, base가 있는지, 색 값 형식, 램프 색 4개
의존: 없음 (표준 라이브러리)
"""
import json
import pathlib
import sys

sys.stdout.reconfigure(encoding="utf-8")

STATUSES = ["draft", "reviewed", "verified", "playtested"]
SPEAKERS = ["luca", "jjirit", "lumi", "isolde", "narrator"]
EXPRESSIONS = ["neutral", "happy", "surprised", "sad"]
CONDITIONS = ["flag", "not_flag", "solved", "not_solved", "has_spark", "has_item", "has_invention", "all", "any"]
ACTIONS = ["dialogue", "door_puzzle", "give_spark", "give_item", "add_ink", "set_flag", "set_state",
           "recall_sparks", "goto_zone", "craft", "ending"]
TRIGGERS = ["place", "beam"]
EFFECTS = ["set_state", "emit_beam", "reflect", "focus"]
errors = []
warnings = []


def err(where, msg):
    errors.append(f"{where}: {msg}")


def warn(where, msg):
    warnings.append(f"{where}: {msg}")


def load(path):
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception as e:  # 읽기 실패도 점검 결과로 남긴다
        err(path.name, f"JSON을 읽지 못함 ({e})")
        return None


def unique_ids(items, where):
    ids = [it.get("id") for it in items if isinstance(it, dict)]
    for i in sorted({x for x in ids if ids.count(x) > 1}, key=str):
        err(where, f"id '{i}'가 겹침")
    if any(x is None for x in ids):
        err(where, "id가 없는 항목이 있음")
    return set(ids)


def main():
    root = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
    data = root / "data"

    sparks = load(data / "sparks.json") or []
    inventions = load(data / "inventions.json") or []
    interactions = load(data / "interactions.json") or []
    spark_ids = unique_ids(sparks, "sparks.json")
    invention_ids = unique_ids(inventions, "inventions.json")
    unique_ids(interactions, "interactions.json")

    for it in inventions:
        if it.get("status") not in STATUSES:
            err("inventions.json", f"{it.get('id')}: status '{it.get('status')}'")

    for it in interactions:
        where = f"interactions.json {it.get('id')}"
        for key in ("object", "sparks", "result", "principle", "status"):
            if key not in it:
                err(where, f"'{key}' 없음")
        for s in it.get("sparks", []):
            if s not in spark_ids:
                err(where, f"스파크 '{s}'가 sparks.json에 없음")
        for r in it.get("requires", []):
            if r not in invention_ids:
                err(where, f"발명품 '{r}'가 inventions.json에 없음")
        if it.get("status") not in STATUSES:
            err(where, f"status '{it.get('status')}'")
        if it.get("status") in STATUSES[1:] and not it.get("fact_check", {}).get("checked"):
            err(where, "fact_check 없이 status가 draft보다 높음")
        if "effect" in it:  # 게임이 쓰는 줄
            if it.get("trigger") not in TRIGGERS:
                err(where, f"trigger '{it.get('trigger')}'")
            if it["effect"].get("type") not in EFFECTS:
                err(where, f"effect.type '{it['effect'].get('type')}'")
            if it["effect"].get("type") == "set_state" and "state" not in it["effect"]:
                err(where, "set_state에 state 없음")
            if it.get("status") not in STATUSES[1:]:
                err(where, "게임이 쓰는 줄(effect 있음)인데 사실 확인 전(draft)")
    if not interactions:
        warn("interactions.json", "비어 있음")

    template = load(data / "puzzles" / "_template.json") or {}
    puzzle_files = sorted(p for p in (data / "puzzles").glob("*.json") if not p.name.startswith("_"))
    if not puzzle_files:
        warn("puzzles/", "문 퍼즐이 없음")
    counts = {}
    puzzles = {}
    for path in puzzle_files:
        pz = load(path)
        if pz is None:
            continue
        puzzles[path.stem] = pz
        where = f"puzzles/{path.name}"
        for key in template:
            if key not in pz:
                err(where, f"'{key}' 없음")
        if pz.get("id") != path.stem:
            err(where, f"id '{pz.get('id')}'가 파일명과 다름")
        if pz.get("spark") not in spark_ids:
            err(where, f"스파크 '{pz.get('spark')}'가 sparks.json에 없음")
        if len(pz.get("hints", [])) != 4:
            err(where, f"힌트가 {len(pz.get('hints', []))}개 (4개여야 함)")
        inp = pz.get("input", {})
        options, answer = inp.get("options", []), pz.get("answer")
        if len(options) != len(set(options)):
            err(where, "선택지가 겹침")
        if inp.get("kind") not in ("choice", "order"):
            err(where, f"input.kind '{inp.get('kind')}'")
        if inp.get("kind") == "choice" and answer not in options:
            err(where, "정답이 선택지에 없음")
        if inp.get("kind") == "order" and sorted(answer or []) != sorted(options):
            err(where, "정답이 선택지를 나열한 순서가 아님")
        status = pz.get("status")
        counts[status] = counts.get(status, 0) + 1
        if status not in STATUSES:
            err(where, f"status '{status}'")
        ver = pz.get("verification", {})
        if status in STATUSES[1:] and not (ver.get("answer_unique") and ver.get("fact_checked")):
            err(where, f"verification이 채워지지 않았는데 status가 '{status}'")
        if status in STATUSES[1:] and not pz.get("knowledge", {}).get("source"):
            err(where, "양피지 지식의 출처가 비어 있음")
        if ver.get("script") and not (root / ver["script"]).is_file():
            err(where, f"검증 스크립트 '{ver['script']}' 없음")

    for inv in inventions:
        for pid in inv.get("cost", {}).get("parchments", []):
            if pid not in puzzles:
                err("inventions.json", f"{inv.get('id')}: 양피지 '{pid}' 문 퍼즐이 없음")

    # ---- 문자열 테이블
    strings_path = data / "strings" / "ko.json"
    strings = (load(strings_path) if strings_path.exists() else None) or {}
    for k, v in strings.items():
        if not isinstance(v, str) or not v:
            err("strings/ko.json", f"'{k}'의 값이 빈 문자열이거나 문자열이 아님")
    for inv in inventions:
        for item in inv.get("cost", {}).get("items", []):
            if f"item.{item}.name" not in strings:
                err("strings/ko.json", f"재료 이름 키 'item.{item}.name' 없음")

    # ---- 대사
    dialogues = {}
    for path in sorted((data / "dialogue").glob("*.json")):
        pack = load(path)
        if pack is None:
            continue
        where = f"dialogue/{path.name}"
        if not isinstance(pack.get("dialogues"), dict):
            err(where, "'dialogues' 없음")
            continue
        for did, lines in pack["dialogues"].items():
            if did in dialogues:
                err(where, f"대사 id '{did}'가 다른 파일과 겹침")
            dialogues[did] = lines
            if not lines:
                err(where, f"{did}: 줄이 없음")
            seen = set()
            for line in lines:
                lid = line.get("id")
                if not lid or lid in seen:
                    err(where, f"{did}: 줄 id '{lid}'가 없거나 겹침")
                seen.add(lid)
                speaker = line.get("speaker")
                if speaker not in SPEAKERS:
                    err(where, f"{did}.{lid}: speaker '{speaker}'")
                elif speaker != "narrator" and f"speaker.{speaker}" not in strings:
                    err(where, f"{did}.{lid}: 문자열 키 'speaker.{speaker}' 없음")
                if not line.get("text"):
                    err(where, f"{did}.{lid}: text 없음")
                if "expr" in line:
                    if line["expr"] not in EXPRESSIONS:
                        err(where, f"{did}.{lid}: expr '{line['expr']}'")
                    elif not (root / "assets" / "portraits" / f"{speaker}_{line['expr']}.png").exists():
                        err(where, f"{did}.{lid}: 초상화 {speaker}_{line['expr']}.png 없음")

    # ---- 지역
    zones = {}
    for path in sorted((data / "zones").glob("*.json")):
        z = load(path)
        if z is not None:
            zones[z.get("id")] = z
            if z.get("id") != path.stem:
                err(f"zones/{path.name}", f"id '{z.get('id')}'가 파일명과 다름")
    field_ids = {fp.get("id") for z in zones.values() for fp in z.get("puzzles", [])}
    used_dialogues, used_doors = set(), set()

    def check_cond(where, cond):
        if cond is None:
            return
        if not isinstance(cond, dict):
            err(where, f"조건이 사전이 아님: {cond}")
            return
        for k, v in cond.items():
            if k not in CONDITIONS:
                err(where, f"모르는 조건 '{k}'")
            elif k in ("all", "any"):
                for c in v:
                    check_cond(where, c)
            elif k in ("solved", "not_solved") and v not in puzzles and v not in field_ids:
                err(where, f"조건의 퍼즐 '{v}'가 없음")
            elif k == "has_spark" and v not in spark_ids:
                err(where, f"조건의 스파크 '{v}'가 없음")
            elif k == "has_invention" and v not in invention_ids:
                err(where, f"조건의 발명품 '{v}'가 없음")

    def check_actions(where, actions, zone):
        obj_ids = {o.get("id") for o in zone.get("objects", [])}
        for a in actions:
            kinds = [k for k in a if k in ACTIONS]
            if len(kinds) != 1:
                err(where, f"행동을 알 수 없음: {a}")
                continue
            check_cond(where, a.get("if"))
            k, v = kinds[0], a[kinds[0]]
            if k == "dialogue":
                used_dialogues.add(v)
                if v not in dialogues:
                    err(where, f"대사 '{v}'가 없음")
            elif k == "door_puzzle":
                used_doors.add(v)
                if v not in puzzles:
                    err(where, f"문 퍼즐 '{v}'가 없음")
                    continue
                if puzzles[v].get("status") not in STATUSES[1:]:
                    err(where, f"문 퍼즐 '{v}'의 status가 '{puzzles[v].get('status')}' — 검토 전인 문제는 게임에 넣지 않는다")
                if puzzles[v].get("zone") != zone.get("id"):
                    err(where, f"문 퍼즐 '{v}'의 zone '{puzzles[v].get('zone')}'이 놓인 지역과 다름")
                if not (root / "assets" / "puzzles" / f"{v}.png").exists():
                    warn(where, f"문 퍼즐 그림 assets/puzzles/{v}.png 없음")
            elif k == "give_spark" and v not in spark_ids:
                err(where, f"스파크 '{v}'가 없음")
            elif k == "give_item" and f"item.{v}.name" not in strings:
                err(where, f"문자열 키 'item.{v}.name' 없음")
            elif k == "craft" and v not in invention_ids:
                err(where, f"발명품 '{v}'가 없음")
            elif k == "goto_zone":
                if v not in zones:
                    err(where, f"지역 '{v}'가 없음")
                elif a.get("spawn", "default") not in zones[v].get("spawns", {}):
                    err(where, f"지역 '{v}'에 들어오는 자리 '{a.get('spawn')}'가 없음")
            elif k == "set_state" and v.get("object") not in obj_ids:
                err(where, f"set_state의 사물 '{v.get('object')}'가 없음")

    for zid, z in zones.items():
        where = f"zones/{zid}.json"
        for key in ("name_key", "background", "walk_poly", "spawns", "objects"):
            if key not in z:
                err(where, f"'{key}' 없음")
        if z.get("name_key") not in strings:
            err(where, f"문자열 키 '{z.get('name_key')}' 없음")
        if not (root / z.get("background", "-")).is_file():
            err(where, f"배경 '{z.get('background')}' 없음")
        if len(z.get("walk_poly", [])) < 3:
            err(where, "walk_poly의 점이 3개보다 적음")
        if "default" not in z.get("spawns", {}):
            err(where, "spawns에 default 없음")
        objs = z.get("objects", [])
        obj_ids = unique_ids(objs, where)
        for entry in z.get("on_enter", []):
            check_cond(where, entry.get("if"))
            check_actions(f"{where} on_enter", entry.get("do", []), z)
        for o in objs:
            ow = f"{where} {o.get('id')}"
            if "type" not in o:
                err(ow, "'type' 없음")
            if ("pos" in o) == ("cell" in o):
                err(ow, "pos와 cell 가운데 하나만 있어야 함")
            if "cell" in o and "grid" not in z:
                err(ow, "cell을 쓰는데 지역에 grid가 없음")
            if o.get("name_key") not in strings:
                err(ow, f"문자열 키 '{o.get('name_key')}' 없음")
            files = list(o.get("sprites", {}).values()) + ([o["sprite"]] if o.get("sprite") else [])
            for f in files:
                if not (root / f).is_file():
                    err(ow, f"그림 '{f}' 없음")
            if "sprites" in o and o.get("state", "default") not in o["sprites"]:
                err(ow, f"처음 상태 '{o.get('state', 'default')}'의 그림이 sprites에 없음")
            if "sprite" not in o and "sprites" not in o:
                err(ow, "sprite(없으면 null) 또는 sprites가 있어야 함")
            if "sprite" in o and o["sprite"] is None and "hotspot" not in o:
                err(ow, "그림이 없는 사물은 hotspot이 있어야 함")
            if o.get("dir", "E") not in ("E", "W", "N", "S"):
                err(ow, f"dir '{o.get('dir')}'")
            if "holds" in o and o["holds"] not in invention_ids:
                err(ow, f"holds의 발명품 '{o['holds']}'가 없음")
            for t in o.get("targets", []):
                if t not in obj_ids:
                    err(ow, f"targets의 사물 '{t}'가 없음")
            if "no_reaction" in o:
                used_dialogues.add(o["no_reaction"])
                if o["no_reaction"] not in dialogues:
                    err(ow, f"no_reaction 대사 '{o['no_reaction']}'가 없음")
            check_cond(ow, o.get("visible_if"))
            check_cond(ow, o.get("place_if"))
            for branch in o.get("interact", []):
                check_cond(ow, branch.get("if"))
                check_actions(ow, branch.get("do", []), z)
            for actions in o.get("on_state", {}).values():
                check_actions(ow, actions, z)
        for fp in z.get("puzzles", []):
            pw = f"{where} {fp.get('id')}"
            if fp.get("name_key") not in strings:
                err(pw, f"문자열 키 '{fp.get('name_key')}' 없음")
            if not fp.get("solved_when"):
                err(pw, "solved_when 없음")
            for c in fp.get("solved_when", []):
                if c.get("object") not in obj_ids:
                    err(pw, f"solved_when의 사물 '{c.get('object')}'가 없음")
            if fp.get("learned_from") and fp["learned_from"] not in puzzles:
                err(pw, f"learned_from의 문 퍼즐 '{fp['learned_from']}'가 없음")
            check_actions(pw, fp.get("on_solved", []), z)
    all_field = [fp.get("id") for z in zones.values() for fp in z.get("puzzles", [])]
    for i in sorted({x for x in all_field if all_field.count(x) > 1}):
        err("zones/", f"필드 퍼즐 id '{i}'가 겹침")
    for did in sorted(set(dialogues) - used_dialogues):
        warn("dialogue/", f"대사 '{did}'를 쓰는 곳이 없음")

    # 스테이지 테마 (design/stages.md)
    import re
    sdir = root / "data" / "stages"
    stage_ids = []
    if sdir.is_dir():
        hexre = re.compile(r"^#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$")

        def check_colors(where, v):
            if isinstance(v, dict):
                for k, x in v.items():
                    check_colors(f"{where}.{k}", x)
            elif isinstance(v, list):
                for i, x in enumerate(v):
                    check_colors(f"{where}[{i}]", x)
            elif isinstance(v, str) and v.startswith("#") and not hexre.match(v):
                err(where, f"색 값 '{v}' 형식이 틀림 (#RRGGBB 또는 #RRGGBBAA)")

        stage_files = {p.stem: p for p in sorted(sdir.glob("*.json")) if p.stem != "index"}
        stages = {}
        for sid, p in stage_files.items():
            try:
                stages[sid] = json.loads(p.read_text(encoding="utf-8"))
            except ValueError as e:
                err(f"stages/{p.name}", f"JSON을 읽을 수 없음: {e}")
        for sid, st in stages.items():
            w = f"stages/{sid}.json"
            if st.get("id") != sid:
                err(w, f"id '{st.get('id')}'가 파일명과 다름")
            if st.get("status") not in STATUSES:
                err(w, f"status '{st.get('status')}'")
            if st.get("base") and st["base"] not in stages:
                err(w, f"base '{st['base']}' 스테이지가 없음")
            if not st.get("base"):
                for key in ("ground", "areas", "mood", "trees_base", "layout"):
                    if key not in st:
                        err(w, f"기준 스테이지에 '{key}'가 없음")
            for k, v in st.get("ramps", {}).items():
                if not (isinstance(v, list) and len(v) == 4):
                    err(w, f"ramps.{k}는 어두운 색부터 4개여야 함")
            for key in ("ramps", "ground", "areas", "mood"):
                check_colors(f"{w} {key}", st.get(key, {}))
        ip = sdir / "index.json"
        if not ip.exists():
            err("stages/", "index.json 없음")
        else:
            index = json.loads(ip.read_text(encoding="utf-8"))
            stage_ids = index.get("stages", [])
            for sid in stage_ids + [index.get("start")]:
                if sid not in stages:
                    err("stages/index.json", f"스테이지 '{sid}'가 없음")
        print(f"스테이지 테마 {len(stages)}개, 게임에 넣은 것 {len(stage_ids)}개")

    print(f"스파크 {len(sparks)}종, 발명품 {len(inventions)}개, 상호작용 {len(interactions)}줄, 문 퍼즐 {len(puzzle_files)}개 {counts}")
    print(f"지역 {len(zones)}곳, 필드 퍼즐 {len(all_field)}개, 대사 {len(dialogues)}개, 문자열 {len(strings)}개, 게임에 넣은 문 퍼즐 {len(used_doors)}개")
    for w in warnings:
        print(f"[주의] {w}")
    for e in errors:
        print(f"[실패] {e}")
    print("통과" if not errors else f"실패 {len(errors)}건")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
