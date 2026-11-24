// 필드 규칙: 지역 하나(data/zones/<id>.json)의 이동·조사·스파크 배치·빛줄기·필드 퍼즐 판정.
// 여기에는 내용이 없다. 무엇이 어디 있고 어떻게 반응하는지는 지역 데이터와 상호작용표에서 온다.
// 그리기는 하지 않는다 (Unity 쪽 View가 이 상태를 읽어 그린다).
using System;
using System.Collections.Generic;

namespace Sparks.Core
{
    using Dict = Dictionary<string, object>;

    public class FieldAction { public string kind, obj, spark = ""; }

    public class Zone
    {
        // 방향 순서는 CLAUDE.md 필드 캐릭터 표와 같다 (시트의 위에서부터).
        public static readonly string[] DirNames = { "down", "down_right", "right", "up_right", "up", "up_left", "left", "down_left" };
        // atan2(y, x)를 45도 단위로 나눈 번호 0..7 → DirNames 번호 (화면 y는 아래가 +)
        static readonly int[] SectorToDir = { 2, 1, 0, 7, 6, 5, 4, 3 };
        static readonly V2[] FollowOffsets = { new V2(-20, -4), new V2(22, -8), new V2(-30, 8), new V2(32, 6) };

        readonly Game game; readonly GameData data; readonly State st; readonly Loc loc;

        public Dict zone; public string zoneId = "";
        public Dict grid;                                    // 없으면 null
        public readonly Dictionary<string, FieldObject> objects = new Dictionary<string, FieldObject>();
        public readonly List<FieldObject> objectList = new List<FieldObject>();   // 데이터 순서
        public List<V2> walkPoly = new List<V2>();
        public string ambient = "#ffffff";                   // 지역 조명 색 (지역 데이터 "ambient")
        public readonly List<RectF> solids = new List<RectF>();   // 배경에 그려진 것(기계, 기둥 …)이 막는 네모들
        public V2 playerPos; public int playerDir; public bool moving;
        public readonly Dictionary<string, V2> followers = new Dictionary<string, V2>();   // 스파크 id → 자리
        public readonly Dictionary<string, int> followerDir = new Dictionary<string, int>();   // 스파크 id → 보는 방향 (DirNames 순서)
        public readonly List<string> followerOrder = new List<string>();
        public readonly List<string> bursts = new List<string>();    // 발동 그림을 틀어야 할 스파크 (View가 가져가며 비운다)
        public Nav nav = new Nav();
        public List<V2> path = new List<V2>();               // 클릭 이동으로 따라갈 점들
        public FieldAction pending;                          // 도착하면 할 일
        public readonly List<List<BeamStep>> beams = new List<List<BeamStep>>();   // 마지막으로 계산한 빛줄기
        public float mapW = Config.ViewW, mapH = Config.ViewH;   // 맵 크기 = 배경 그림 크기. 지역 데이터의 좌표는 모두 이 맵 기준 픽셀
        /// <summary>지역을 새로 불러올 때마다, 빛줄기를 다시 계산할 때마다 1씩 는다.</summary>
        public int loadSerial, beamSerial;
        bool refreshing;

        public Zone(Game game)
        {
            this.game = game; data = game.data; st = game.state; loc = game.loc;
        }

        // ------------------------------------------------------------------ 지역 불러오기

        public void Load(string id, string spawn)
        {
            Leave();
            objects.Clear(); objectList.Clear(); followers.Clear(); followerDir.Clear(); followerOrder.Clear(); bursts.Clear();
            path.Clear(); pending = null; beams.Clear();
            zoneId = id;
            zone = (Dict)data.zones[id];
            st.zone = id;
            grid = J.Has(zone, "grid") ? J.Dict(zone, "grid") : null;
            walkPoly = new List<V2>();
            foreach (object p in J.List(zone, "walk_poly")) { var l = J.AsList(p); walkPoly.Add(new V2(J.At(l, 0), J.At(l, 1))); }
            ambient = J.Str(zone, "ambient", "#ffffff");
            solids.Clear();
            foreach (object r in J.List(zone, "solids")) { var l = J.AsList(r); solids.Add(new RectF(J.At(l, 0), J.At(l, 1), J.At(l, 2), J.At(l, 3))); }
            int[] size = data.ImageSize(J.Str(zone, "background"));
            mapW = size[0]; mapH = size[1];
            if (!data.HasAsset(J.Str(zone, "background"))) { mapW = Config.ViewW; mapH = Config.ViewH; }

            Dictionary<string, string> saved;
            st.objStates.TryGetValue(id, out saved);
            foreach (object d in J.List(zone, "objects"))
            {
                string oid = J.Str(d, "id"), s = null;
                if (saved != null) saved.TryGetValue(oid, out s);
                var o = new FieldObject((Dict)d, grid, s, data, st);
                objects[o.id] = o; objectList.Add(o);
            }
            var spawns = J.Dict(zone, "spawns");
            var sp = J.Has(spawns, spawn) ? J.List(spawns, spawn) : (J.Has(spawns, "default") ? J.List(spawns, "default") : new List<object> { 320.0, 240.0 });
            playerPos = new V2(J.At(sp, 0), J.At(sp, 1));
            playerDir = 0; moving = false;
            loadSerial++;
            Refresh();
        }

        public void Leave()
        {
            if (zoneId != "" && st.placed.Count > 0) RecallAll();
        }

        /// <summary>맵 크기를 바꾸고 길찾기 칸을 맵 전체에 다시 깐다 (보통은 Load가 배경 그림 크기로 정한다. 시험용).</summary>
        public void SetMapSize(float w, float h)
        {
            mapW = w; mapH = h;
            BuildNav();
        }

        /// <summary>카메라가 볼 곳: 루카의 몸 가운데, 맵 밖을 비추지 않게. 도트가 번지지 않게 정수 픽셀.</summary>
        public V2 CameraCenter()
        {
            float hw = Config.ViewW / 2f, hh = Config.ViewH / 2f;
            float x = (float)Math.Round(playerPos.x), y = (float)Math.Round(playerPos.y - Config.CameraLift);
            x = mapW <= Config.ViewW ? mapW / 2f : Math.Max(hw, Math.Min(mapW - hw, x));
            y = mapH <= Config.ViewH ? mapH / 2f : Math.Max(hh, Math.Min(mapH - hh, y));
            return new V2(x, y);
        }

        // ------------------------------------------------------------------ 매 프레임: 이동

        /// <param name="move">키보드 이동 방향 (없으면 0, 0). 화면 y는 아래가 +</param>
        public void Update(float dt, V2 move)
        {
            moving = false;
            if (!game.IsBusy)
            {
                if (!move.IsZero)
                {
                    path.Clear(); pending = null;
                    V2 step = move.Normalized * (Config.WalkSpeed * dt);
                    V2 p = playerPos;
                    if (IsWalkable(p + step)) playerPos = p + step;
                    else if (IsWalkable(p + new V2(step.x, 0))) playerPos = p + new V2(step.x, 0);
                    else if (IsWalkable(p + new V2(0, step.y))) playerPos = p + new V2(0, step.y);
                    Face(move);
                    moving = true;
                }
                else if (path.Count > 0)
                {
                    V2 target = path[0], to = target - playerPos;
                    float dist = Config.WalkSpeed * dt;
                    if (to.Length <= dist)
                    {
                        playerPos = target;
                        path.RemoveAt(0);
                        if (path.Count == 0) Arrive();
                    }
                    else
                    {
                        playerPos = playerPos + to.Normalized * dist;
                        Face(to);
                    }
                    moving = true;
                }
            }
            for (int i = 0; i < followerOrder.Count; i++)
            {
                string id = followerOrder[i];
                V2 target;
                string at;
                if (st.placed.TryGetValue(id, out at) && objects.ContainsKey(at)) target = objects[at].PlacePos + new V2(0, 1);
                else target = playerPos + FollowOffsets[i % FollowOffsets.Length];
                V2 before = followers[id];
                followers[id] = V2.Lerp(before, target, Math.Max(0f, Math.Min(1f, 6f * dt)));
                // 보는 방향: 움직이는 중이면 가는 쪽, 놓여 있으면 앞(아래), 멈춰 따라다니면 루카와 같은 쪽
                V2 step = followers[id] - before;
                if (dt > 0f && step.Length / dt > 12f) followerDir[id] = DirOf(step);
                else if (at != null && st.placed.ContainsKey(id)) followerDir[id] = 0;
                else followerDir[id] = playerDir;
            }
        }

        void Face(V2 v) { playerDir = DirOf(v); }

        /// <summary>화면 방향 v(y는 아래가 +) → DirNames 순서의 방향 번호</summary>
        static int DirOf(V2 v)
        {
            int sector = (int)Math.Round(Math.Atan2(v.y, v.x) / (Math.PI / 4.0));
            return SectorToDir[((sector % 8) + 8) % 8];
        }

        void Arrive()
        {
            if (pending == null) return;
            FieldAction a = pending; pending = null;
            FieldObject o;
            if (!objects.TryGetValue(a.obj, out o)) return;
            if (playerPos.Dist(o.pos) <= Config.Reach) { Face(o.pos - playerPos); Do(a); }
            else game.Toast(loc.T("ui.cant_reach"));
        }

        // ------------------------------------------------------------------ 입력 (View가 부른다)

        public void ToggleInvestigate()
        {
            st.investigate = !st.investigate;
            st.Touch();
            Refresh();
            game.Toast(loc.T(st.investigate ? "ui.investigate_on" : "ui.investigate_off"));
        }

        /// <summary>숫자 키 1~8</summary>
        public void SparkKey(int n)
        {
            if (n <= data.sparks.Count && st.sparks.Contains(J.Str(data.sparks[n - 1], "id"))) st.SelectSpark(J.Str(data.sparks[n - 1], "id"));
            else game.Toast(loc.T("ui.no_such_spark"));
        }

        /// <summary>좌클릭: 스파크를 골랐고 배치할 수 있는 사물이면 배치, 아니면 조사, 빈 바닥이면 이동</summary>
        public void Click(V2 pos)
        {
            FieldObject o = ObjectAt(pos);
            if (o != null && st.selectedSpark != "" && o.Placeable) GoAnd(new FieldAction { kind = "place", obj = o.id, spark = st.selectedSpark });
            else if (o != null && o.CanInteract) GoAnd(new FieldAction { kind = "interact", obj = o.id });
            else WalkTo(pos);
        }

        /// <summary>우클릭: 그 자리의 스파크를 회수. 없으면 고른 스파크를 회수</summary>
        public void RightClick(V2 pos)
        {
            FieldObject o = ObjectAt(pos);
            if (o != null)
                foreach (var p in st.placed)
                    if (p.Value == o.id) { RecallSpark(p.Key); return; }
            if (st.placed.ContainsKey(st.selectedSpark)) RecallSpark(st.selectedSpark);
            else if (st.placed.Count == 1)
                foreach (string k in new List<string>(st.placed.Keys)) RecallSpark(k);
        }

        public FieldObject ObjectAt(V2 pos)
        {
            FieldObject best = null; float bestArea = float.MaxValue;
            foreach (FieldObject o in objectList)
            {
                if (!o.visible || !(o.CanInteract || o.Placeable)) continue;
                RectF r = o.Hotspot();
                if (r.Has(pos) && r.Area < bestArea) { best = o; bestArea = r.Area; }
            }
            return best;
        }

        public void InteractNearest()
        {
            FieldObject best = null; float bestD = Config.Reach;
            foreach (FieldObject o in objectList)
            {
                if (!o.CanInteract) continue;
                float d = playerPos.Dist(o.pos);
                if (d <= bestD) { best = o; bestD = d; }
            }
            if (best != null) { Face(best.pos - playerPos); Interact(best); }
        }

        public void GoAnd(FieldAction action)
        {
            FieldObject o = objects[action.obj];
            if (playerPos.Dist(o.pos) <= Config.Reach)
            {
                path.Clear(); pending = null;
                Face(o.pos - playerPos);
                Do(action);
                return;
            }
            List<V2> p = ApproachPath(o.pos);
            if (p.Count == 0) { game.Toast(loc.T("ui.cant_reach")); return; }
            path = p; pending = action;
        }

        void Do(FieldAction a)
        {
            FieldObject o = objects[a.obj];
            if (a.kind == "place") PlaceSpark(a.spark, o);
            else Interact(o);
        }

        // ------------------------------------------------------------------ 조사

        public void Interact(FieldObject o)
        {
            if (o.type == "ink")
            {
                SetState(o, "taken");
                st.ink += 1;
                st.Touch();
                Refresh();
                game.Toast(loc.T("ui.ink_found", "n", st.ink));
                return;
            }
            if (o.type == "mirror")
            {
                SetState(o, o.state == "/" ? "\\" : "/");
                Refresh();
                return;
            }
            if (J.Has(o.def, "holds") && st.inventions.Contains(J.Str(o.def, "holds")))
            {
                bool put = o.state != "lens";
                foreach (FieldObject other in objectList)
                    if (J.Str(other.def, "holds") == J.Str(o.def, "holds") && other.state == "lens") SetState(other, other.initialState);
                if (put) SetState(o, "lens");
                Refresh();
                game.Toast(loc.T(put ? "ui.lens_put" : "ui.lens_taken"));
                return;
            }
            foreach (object branch in J.List(o.def, "interact"))
                if (st.Check(J.Get(branch, "if"))) { game.RunScript(J.List(branch, "do")); return; }
        }

        // ------------------------------------------------------------------ 스파크 배치·회수

        List<object> Targets(FieldObject o)
        {
            return J.Has(o.def, "targets") ? J.List(o.def, "targets") : new List<object> { o.id };
        }

        public void PlaceSpark(string sparkId, FieldObject o)
        {
            if (!st.sparks.Contains(sparkId) || !o.Placeable) return;
            if (st.placed.ContainsKey(sparkId)) RecallSpark(sparkId);
            foreach (string other in new List<string>(st.placed.Keys))
                if (st.placed.ContainsKey(other) && st.placed[other] == o.id) RecallSpark(other);
            st.placed[sparkId] = o.id;
            bool reacted = false;
            foreach (object tidObj in Targets(o))
            {
                string tid = J.Text(tidObj);
                FieldObject t;
                if (!objects.TryGetValue(tid, out t) || !t.visible) continue;
                Dict row = data.FindInteraction(t.type, t.state, sparkId, "place", st.inventions);
                if (row == null) continue;
                reacted = true;
                object effect = J.Get(row, "effect");
                if (J.Str(effect, "type") == "set_state")
                    SetState(t, J.Format(J.Str(effect, "state"), J.Dict(o.def, "params")));
                game.Toast(loc.T("interaction." + J.Str(row, "id") + ".result"));
            }
            if (!reacted)
            {
                // 표에 없는 조합은 반응하지 않는다
                st.placed.Remove(sparkId);
                st.Touch();
                if (J.Has(o.def, "no_reaction"))
                    game.RunScript(new List<object> { new Dict { { "dialogue", J.Str(o.def, "no_reaction") } } });
                else game.Toast(loc.T("ui.no_reaction"));
                return;
            }
            bursts.Add(sparkId);
            st.Touch();
            Refresh();
        }

        public void RecallSpark(string sparkId)
        {
            string at;
            if (!st.placed.TryGetValue(sparkId, out at)) return;
            st.placed.Remove(sparkId);
            FieldObject o;
            if (objects.TryGetValue(at, out o))
                foreach (object tidObj in Targets(o))
                {
                    FieldObject t;
                    if (!objects.TryGetValue(J.Text(tidObj), out t)) continue;
                    Dict row = data.FindInteraction(t.type, t.initialState, sparkId, "place", st.inventions);
                    if (row != null && !J.Bool(row, "persistent") && J.Str(J.Get(row, "effect"), "type") == "set_state")
                        SetState(t, t.initialState);
                }
            st.Touch();
            Refresh();
        }

        public void RecallAll()
        {
            foreach (string id in new List<string>(st.placed.Keys)) RecallSpark(id);
        }

        /// <summary>필드 퍼즐 초기화: 스파크를 모두 회수하고, 손으로 돌리거나 끼운 것(거울, 렌즈 틀)을 처음 상태로.
        /// 이미 일어난 일(켜진 문양, 탄 덩굴 …)은 그대로 둔다.</summary>
        public void ResetField()
        {
            RecallAll();
            foreach (FieldObject o in objectList)
                if (o.type == "mirror" || J.Has(o.def, "holds")) SetState(o, o.initialState);
            Refresh();
            game.Toast(loc.T("ui.field_reset"));
        }

        public void SetObjectState(string id, string s)
        {
            FieldObject o;
            if (objects.TryGetValue(id, out o)) { SetState(o, s); Refresh(); }
            else data.errors.Add("set_state: '" + zoneId + "' 지역에 사물 '" + id + "' 없음");
        }

        bool SetState(FieldObject o, string s, bool transient = false)
        {
            o.beamTransient = transient;
            if (!o.SetState(s)) return false;
            if (!st.objStates.ContainsKey(zoneId)) st.objStates[zoneId] = new Dictionary<string, string>();
            st.objStates[zoneId][o.id] = s;
            st.Touch();
            var actions = J.List(J.Get(o.def, "on_state"), s);
            if (actions.Count > 0) game.RunScript(actions);
            return true;
        }

        // ------------------------------------------------------------------ 다시 계산: 보임, 빛줄기, 길, 퍼즐 판정

        public void Refresh()
        {
            if (refreshing) return;
            refreshing = true;
            SyncFollowers();
            foreach (FieldObject o in objectList) o.UpdateVisibility();
            TraceBeams();
            foreach (FieldObject o in objectList) o.UpdateVisibility();
            BuildNav();
            CheckPuzzles();
            foreach (FieldObject o in objectList) o.UpdateVisibility();   // 방금 풀린 필드 퍼즐에 따라 보이는 것(열린 통로 …)
            refreshing = false;
        }

        void SyncFollowers()
        {
            foreach (string id in st.sparks)
                if (!followers.ContainsKey(id)) { followers[id] = playerPos; followerDir[id] = 0; followerOrder.Add(id); }
        }

        void TraceBeams()
        {
            beams.Clear();
            beamSerial++;
            var lit = new HashSet<string>();   // 이번에 빛이 닿은 사물 id
            if (grid != null)
            {
                var byCell = new Dictionary<Cell, BeamInfo>();
                var blocked = new HashSet<Cell>();
                foreach (object r in J.List(grid, "blocked"))
                {
                    var l = J.AsList(r);
                    for (int bx = (int)J.Num(l[0]); bx <= (int)J.Num(l[2]); bx++)
                        for (int by = (int)J.Num(l[1]); by <= (int)J.Num(l[3]); by++) blocked.Add(new Cell(bx, by));
                }
                foreach (FieldObject o in objectList)
                    if (o.hasCell && o.visible) byCell[o.cell] = o.BeamInfo();
                foreach (var placed in new List<KeyValuePair<string, string>>(st.placed))
                {
                    string sparkId = placed.Key;
                    FieldObject stand;
                    if (!objects.TryGetValue(placed.Value, out stand) || !stand.hasCell) continue;
                    Dict row = data.FindInteraction(stand.type, stand.state, sparkId, "place", st.inventions);
                    if (row == null || J.Str(J.Get(row, "effect"), "type") != "emit_beam") continue;
                    int focal = 0;
                    foreach (string inv in st.inventions) focal = Math.Max(focal, J.Int(data.inventions[inv], "focal_cells"));
                    BeamResult result = Beam.Trace(J.Int(grid, "cols"), J.Int(grid, "rows"), byCell, stand.cell, J.Str(stand.def, "dir", "E"), focal,
                        bi => data.FindInteraction(bi.type, bi.state, sparkId, "beam", st.inventions), blocked);
                    beams.Add(result.path);
                    foreach (BeamHit hit in result.hits)
                    {
                        FieldObject t = objects[hit.id];
                        object effect = J.Get(hit.row, "effect");
                        List<object> when = J.Has(effect, "when_beam") ? J.List(effect, "when_beam") : new List<object> { "plain", "converging", "focused" };
                        lit.Add(t.id);
                        if (J.Contains(when, hit.status))
                        {
                            if (SetState(t, J.Str(effect, "state"), !J.Bool(hit.row, "persistent")))
                                game.Toast(loc.T("interaction." + J.Str(hit.row, "id") + ".result"));
                        }
                        else if (J.Has(effect, "else_state_prefix"))
                            SetState(t, J.Str(effect, "else_state_prefix") + hit.status, true);
                    }
                }
            }
            foreach (FieldObject o in objectList)
                if (o.beamTransient && !lit.Contains(o.id)) SetState(o, o.initialState);
        }

        public V2 CellCenter(Cell c)
        {
            var o = J.List(grid, "origin"); var s = J.List(grid, "cell");
            return new V2(J.At(o, 0) + (c.x + 0.5f) * J.At(s, 0), J.At(o, 1) + (c.y + 0.5f) * J.At(s, 1));
        }

        void CheckPuzzles()
        {
            foreach (object pz in J.List(zone, "puzzles"))
            {
                string pid = J.Str(pz, "id");
                if (st.IsSolved(pid)) continue;
                bool ok = true;
                foreach (object cond in J.List(pz, "solved_when"))
                {
                    FieldObject o;
                    if (!objects.TryGetValue(J.Str(cond, "object"), out o) || o.state != J.Str(cond, "state")) ok = false;
                }
                if (!ok) continue;
                st.fieldSolved.Add(pid);
                st.Touch();
                game.Toast(loc.T("ui.field_solved", "name", loc.T(J.Str(pz, "name_key"))));
                game.RunScript(J.List(pz, "on_solved"));
            }
        }

        // ------------------------------------------------------------------ 걸을 수 있는 곳, 길찾기

        public bool IsWalkable(V2 p)
        {
            if (!Nav.InPolygon(p, walkPoly)) return false;
            foreach (RectF s in solids) if (s.Has(p)) return false;
            foreach (FieldObject o in objectList)
            {
                RectF r = o.SolidRect();
                if (!r.Empty && r.Grow(2f).Has(p)) return false;
            }
            return true;
        }

        void BuildNav() { nav.Build(mapW, mapH, IsWalkable); }

        List<V2> PathBetween(V2 from, Cell toCell)
        {
            var o = new List<V2>();
            Cell a = nav.NearestFree(nav.CellAt(from));
            if (a.x < 0 || toCell.x < 0) return o;
            foreach (Cell c in nav.Path(a, toCell)) o.Add(nav.Center(c));
            if (o.Count > 1) o.RemoveAt(0);   // 첫 점은 지금 서 있는 칸
            return o;
        }

        public void WalkTo(V2 pos)
        {
            pending = null;
            path = PathBetween(playerPos, nav.NearestFree(nav.CellAt(pos)));
        }

        /// <summary>사물에 손이 닿는 곳까지 가는 길 (닿을 수 있는 빈 칸 가운데 루카에게 가장 가까운 곳)</summary>
        List<V2> ApproachPath(V2 target)
        {
            int n = Config.NavCell;
            float reach = Config.Reach - n;
            Cell c0 = nav.CellAt(target);
            int span = (int)(Config.Reach / n) + 1;
            var cands = new List<Cell>();
            for (int x = c0.x - span; x <= c0.x + span; x++)
                for (int y = c0.y - span; y <= c0.y + span; y++)
                {
                    var q = new Cell(x, y);
                    if (nav.InRegion(q) && !nav.Solid(q) && nav.Center(q).Dist(target) <= reach) cands.Add(q);
                }
            V2 pp = playerPos;
            cands.Sort((a, b) => nav.Center(a).Dist(pp).CompareTo(nav.Center(b).Dist(pp)));
            foreach (Cell q in cands)
            {
                List<V2> p = PathBetween(playerPos, q);
                if (p.Count > 0) return p;
            }
            return new List<V2>();
        }

        /// <summary>시험용: 그 사물에 지금 걸어서 닿을 수 있는가</summary>
        public bool CanReach(string id)
        {
            FieldObject o = objects[id];
            return playerPos.Dist(o.pos) <= Config.Reach || ApproachPath(o.pos).Count > 0;
        }
    }
}
