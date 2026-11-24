// 게임의 뼈대 (그림 없는 쪽): 지역 데이터의 '행동 목록'을 차례로 실행하고, 대화·문 퍼즐·제작·오메가 문서·끝 화면의 상태를 가진다.
// 행동의 종류는 design/ch1-prototype.md 9절 '데이터 형식'과 같다.
// 화면(View)은 이 상태를 읽어 그리고, 버튼이 눌리면 여기의 함수를 부른다.
using System;
using System.Collections.Generic;

namespace Sparks.Core
{
    using Dict = Dictionary<string, object>;

    /// <summary>문 퍼즐 화면의 상태 (plan.md 5장).
    ///  - 선택(choice): 하나 고르고 제출 / 순서(order): 누른 차례대로 놓고 제출
    ///  - 힌트 4단계: 잉크 방울 1·1·1·2개
    ///  - 오답 1회마다 얻을 에너지 10% 감소, 최소 50%
    ///  - 정답이면 해설을 보여 주고 양피지(오메가 문서)에 기록</summary>
    public class PuzzleSession
    {
        public string id; public Dict pz; public string kind = "choice";
        public int choice = -1;
        public List<int> sequence = new List<int>();   // 순서 문제에서 누른 선택지 번호들
        public string feedback = "";
        public bool resultShown; public int gained;
        public List<object> Options { get { return J.List(J.Get(pz, "input"), "options"); } }
    }

    public class Game
    {
        public readonly GameData data; public readonly Loc loc; public readonly State state; public readonly Zone zone;

        // 떠 있는 창
        public string dialogueId = ""; public List<object> dialogueLines; public int dialogueIndex; public bool dialogueVision;
        public bool DialogueOpen { get { return dialogueLines != null; } }
        public PuzzleSession puzzle;
        public string craftId;                   // 제작 창이 떠 있으면 발명품 id
        public bool omegaOpen; public string omegaTab = "sparks";
        public static readonly string[] OmegaTabs = { "sparks", "parchment", "puzzles" };
        public bool endOpen;
        public string toastText = ""; public float toastLeft;

        // 행동 실행
        readonly Queue<List<object>> queue = new Queue<List<object>>();
        List<object> cur; int curIndex;
        public bool running;

        public Game(GameData data)
        {
            this.data = data;
            loc = new Loc(data);
            state = new State(data);
            zone = new Zone(this);
        }

        public void Restart()
        {
            endOpen = false; omegaOpen = false; puzzle = null; craftId = null; dialogueLines = null;
            queue.Clear(); cur = null; running = false;
            state.Reset();
            ChangeZone(Config.StartZone, "default");
        }

        public void ChangeZone(string id, string spawn)
        {
            if (!data.zones.ContainsKey(id)) { data.errors.Add("지역 없음: " + id); return; }
            zone.Load(id, spawn);
            state.Touch();
            foreach (object entry in J.List(data.zones[id], "on_enter"))
                if (state.Check(J.Get(entry, "if"))) RunScript(J.List(entry, "do"));
        }

        /// <summary>필드 입력을 막아야 하는가 (행동 실행 중이거나 창이 떠 있음)</summary>
        public bool IsBusy { get { return running || DialogueOpen || puzzle != null || omegaOpen || craftId != null || endOpen; } }

        public void Toast(string text) { toastText = text; toastLeft = Config.ToastTime; }

        public void Update(float dt, V2 move)
        {
            if (toastLeft > 0f) toastLeft -= dt;
            zone.Update(dt, move);
        }

        // ------------------------------------------------------------------ 행동 목록

        /// <summary>행동 목록을 차례로 실행한다. 이미 실행 중이면 줄을 세워 뒤에 실행한다.</summary>
        public void RunScript(List<object> actions)
        {
            queue.Enqueue(actions);
            if (running) return;
            running = true;
            Pump();
        }

        /// <summary>창(대화, 문 퍼즐, 제작)이 뜨면 멈췄다가, 창이 닫힐 때 다시 불려 이어서 실행한다.</summary>
        void Pump()
        {
            while (true)
            {
                if (cur == null)
                {
                    if (queue.Count == 0) { running = false; return; }
                    cur = queue.Dequeue(); curIndex = 0;
                }
                while (cur != null && curIndex < cur.Count)
                {
                    object a = cur[curIndex++];
                    if (!Step(a)) return;   // 창이 떴다
                }
                cur = null;
            }
        }

        /// <summary>행동 하나. 창을 띄우고 기다려야 하면 false.</summary>
        bool Step(object a)
        {
            if (J.Has(a, "if") && !state.Check(J.Get(a, "if"))) return true;
            if (J.Has(a, "dialogue"))
            {
                string id = J.Str(a, "dialogue");
                if (!data.dialogues.ContainsKey(id)) { data.errors.Add("대사 없음: " + id); return true; }
                dialogueId = id;
                dialogueLines = J.AsList(data.dialogues[id]);
                dialogueIndex = 0;
                dialogueVision = J.Str(a, "style") == "vision";
                if (dialogueLines.Count == 0) { dialogueLines = null; return true; }
                return false;
            }
            if (J.Has(a, "door_puzzle"))
            {
                string id = J.Str(a, "door_puzzle");
                if (state.IsSolved(id)) return true;
                puzzle = new PuzzleSession { id = id, pz = (Dict)data.puzzles[id] };
                puzzle.kind = J.Str(J.Get(puzzle.pz, "input"), "kind", "choice");
                return false;
            }
            if (J.Has(a, "craft")) { craftId = J.Str(a, "craft"); return false; }
            if (J.Has(a, "give_spark"))
            {
                state.AddSpark(J.Str(a, "give_spark"));
                zone.Refresh();
                Toast(loc.T("ui.spark_got", "name", loc.T("spark." + J.Str(a, "give_spark") + ".name")));
            }
            else if (J.Has(a, "give_item"))
            {
                string item = J.Str(a, "give_item");
                if (!state.items.Contains(item)) state.items.Add(item);
                state.Touch();
                Toast(loc.T("ui.item_got", "name", loc.T("item." + item + ".name")));
            }
            else if (J.Has(a, "add_ink")) { state.ink += J.Int(a, "add_ink"); state.Touch(); }
            else if (J.Has(a, "set_flag")) { state.SetFlag(J.Str(a, "set_flag")); zone.Refresh(); }
            else if (J.Has(a, "set_state")) zone.SetObjectState(J.Str(J.Get(a, "set_state"), "object"), J.Str(J.Get(a, "set_state"), "state"));
            else if (J.Has(a, "recall_sparks")) zone.RecallAll();
            else if (J.Has(a, "goto_zone")) ChangeZone(J.Str(a, "goto_zone"), J.Str(a, "spawn", "default"));
            else if (J.Has(a, "ending")) { state.ended = true; endOpen = true; state.Touch(); }
            else data.errors.Add("모르는 행동");
            return true;
        }

        // ------------------------------------------------------------------ 대화

        public Dict DialogueLine { get { return (Dict)dialogueLines[dialogueIndex]; } }
        public string DialogueSpeaker { get { return J.Str(DialogueLine, "speaker", "narrator"); } }
        public string DialogueText { get { return loc.T("dlg." + dialogueId + "." + J.Str(DialogueLine, "id")); } }

        /// <summary>초상화 그림 경로. 없으면 "".</summary>
        public string DialoguePortrait
        {
            get
            {
                string sp = DialogueSpeaker;
                string path = "assets/portraits/" + sp + "_" + J.Str(DialogueLine, "expr", "neutral") + ".png";
                return sp != "narrator" && data.HasAsset(path) ? path : "";
            }
        }

        public void DialogueAdvance()
        {
            if (!DialogueOpen) return;
            dialogueIndex++;
            if (dialogueIndex >= dialogueLines.Count) { dialogueLines = null; Pump(); }
        }

        // ------------------------------------------------------------------ 문 퍼즐

        public void PuzzlePick(int i)
        {
            if (puzzle == null || puzzle.resultShown || i < 0) return;
            if (puzzle.kind == "choice") puzzle.choice = i;
            else if (!puzzle.sequence.Contains(i)) puzzle.sequence.Add(i);
            puzzle.feedback = "";
        }

        public void PuzzleResetOrder() { if (puzzle != null) puzzle.sequence.Clear(); }

        public void PuzzleTakeHint()
        {
            if (puzzle == null) return;
            DoorRecord rec = state.DoorRecord(puzzle.id);
            if (rec.hints >= Config.HintCosts.Length) return;
            int cost = Config.HintCosts[rec.hints];
            if (state.ink < cost) { puzzle.feedback = loc.T("ui.puzzle_no_ink", "cost", cost, "have", state.ink); return; }
            state.ink -= cost;
            state.hintsUsed += 1;
            rec.hints += 1;
            state.Touch();
        }

        object PuzzleAnswerGiven()
        {
            var options = puzzle.Options;
            if (puzzle.kind == "choice") return puzzle.choice >= 0 ? options[puzzle.choice] : null;
            if (puzzle.sequence.Count != options.Count) return null;
            var o = new List<object>();
            foreach (int i in puzzle.sequence) o.Add(options[i]);
            return o;
        }

        public void PuzzleSubmit()
        {
            if (puzzle == null || puzzle.resultShown) return;
            object given = PuzzleAnswerGiven();
            if (given == null)
            {
                puzzle.feedback = loc.T(puzzle.kind == "choice" ? "ui.puzzle_pick_first" : "ui.puzzle_order_incomplete");
                return;
            }
            if (J.Same(given, J.Get(puzzle.pz, "answer")))
            {
                puzzle.gained = state.SolveDoor(puzzle.id);
                puzzle.resultShown = true;
            }
            else
            {
                DoorRecord rec = state.DoorRecord(puzzle.id);
                rec.wrong += 1;
                puzzle.choice = -1; puzzle.sequence.Clear();
                puzzle.feedback = loc.T("ui.puzzle_wrong", "n", rec.wrong);
                state.Touch();
            }
        }

        /// <summary>해설을 닫는다 (풀었다).</summary>
        public void PuzzleCloseResult()
        {
            if (puzzle == null) return;
            puzzle = null;
            zone.Refresh();   // 풀이에 따라 보이는 사물이 달라질 수 있다
            Pump();
        }

        /// <summary>그만두기. 뒤 행동은 하지 않는다 (다음에 같은 자리에서 다시 도전).</summary>
        public void PuzzleQuit()
        {
            if (puzzle == null) return;
            puzzle = null;
            cur = null;
            Pump();
        }

        // ------------------------------------------------------------------ 제작

        Dict CraftCost { get { return J.Dict(data.inventions[craftId], "cost"); } }

        public bool CraftCanMake()
        {
            if (craftId == null) return false;
            foreach (object pid in J.List(CraftCost, "parchments")) if (!state.IsSolved(J.Text(pid))) return false;
            foreach (object item in J.List(CraftCost, "items")) if (!state.items.Contains(J.Text(item))) return false;
            return state.energy >= J.Int(CraftCost, "energy");
        }

        /// <summary>제작 창에 보일 줄들. 맨 앞 글자: '+' 갖춤, '-' 모자람, ' ' 설명.</summary>
        public List<string> CraftLines()
        {
            var lines = new List<string> { " " + loc.T("invention." + craftId + ".description"), " " };
            foreach (object pid in J.List(CraftCost, "parchments"))
                lines.Add((state.IsSolved(J.Text(pid)) ? "+" : "-") + loc.T("ui.craft_need_parchment", "name", loc.T("puzzle." + J.Text(pid) + ".title")));
            int need = J.Int(CraftCost, "energy");
            lines.Add((state.energy >= need ? "+" : "-") + loc.T("ui.craft_need_energy", "need", need, "have", state.energy));
            foreach (object item in J.List(CraftCost, "items"))
                lines.Add((state.items.Contains(J.Text(item)) ? "+" : "-") + loc.T("ui.craft_need_item", "name", loc.T("item." + J.Text(item) + ".name")));
            return lines;
        }

        public void CraftMake()
        {
            if (!CraftCanMake()) return;
            state.energy -= J.Int(CraftCost, "energy");
            foreach (object item in J.List(CraftCost, "items")) state.items.Remove(J.Text(item));
            state.inventions.Add(craftId);
            state.Touch();
            string made = craftId;
            craftId = null;
            Toast(loc.T("ui.craft_done", "name", loc.T("invention." + made + ".name")));
            Pump();
        }

        public void CraftCancel()
        {
            if (craftId == null) return;
            craftId = null;
            cur = null;
            Pump();
        }

        // ------------------------------------------------------------------ 오메가 문서 (프로토타입에서는 세 쪽: 스파크 도감, 양피지, 퍼즐 목록)

        public void OmegaToggle()
        {
            if (omegaOpen) omegaOpen = false;
            else if (!IsBusy) omegaOpen = true;
        }

        /// <summary>지금 쪽의 줄들. 맨 앞 글자: 'H' 제목, 'D' 흐린 글, ' ' 보통 글.</summary>
        public List<string> OmegaLines(string tab)
        {
            var o = new List<string>();
            if (tab == "sparks")
            {
                foreach (object s in data.sparks)
                {
                    string id = J.Str(s, "id");
                    if (state.sparks.Contains(id)) o.Add("H" + loc.T("spark." + id + ".name") + " — " + loc.T("spark." + id + ".phenomenon"));
                    else o.Add("D● " + loc.T("ui.omega_spark_unknown"));
                    o.Add(" ");
                }
            }
            else if (tab == "parchment")
            {
                bool any = false;
                foreach (string id in state.doorOrder)
                {
                    if (!state.door[id].solved) continue;
                    any = true;
                    o.Add("H" + loc.T("puzzle." + id + ".title"));
                    o.Add(" " + loc.T("puzzle." + id + ".parchment"));
                    o.Add("D" + loc.T("ui.omega_source", "src", loc.T("puzzle." + id + ".source")));
                    o.Add(" ");
                }
                if (!any) o.Add(" " + loc.T("ui.omega_no_parchment"));
            }
            else
            {
                o.Add("H" + loc.T("ui.omega_door_header"));
                var ids = new List<string>(data.puzzles.Keys);
                ids.Sort(string.CompareOrdinal);
                foreach (string id in ids)
                {
                    string z = J.Str(data.puzzles[id], "zone");
                    if (!data.zones.ContainsKey(z)) continue;   // 이 프로토타입에 없는 지역의 문제
                    if (state.IsSolved(id))
                        o.Add(" ✔ " + loc.T("puzzle." + id + ".title") + "  " + loc.T("ui.omega_solved", "wrong", state.door[id].wrong, "energy", state.door[id].energy));
                    else o.Add("D？ " + loc.T("ui.omega_unsolved", "zone", loc.T(J.Str(data.zones[z], "name_key"))));
                }
                o.Add(" ");
                o.Add("H" + loc.T("ui.omega_field_header"));
                foreach (string zid in data.zoneOrder)
                    foreach (object fp in J.List(data.zones[zid], "puzzles"))
                    {
                        if (state.IsSolved(J.Str(fp, "id"))) o.Add(" ✔ " + loc.T(J.Str(fp, "name_key")));
                        else o.Add("D？ " + loc.T("ui.omega_unsolved", "zone", loc.T(J.Str(data.zones[zid], "name_key"))));
                    }
            }
            return o;
        }

        // ------------------------------------------------------------------ 끝 화면

        public string EndStats()
        {
            int wrong = 0, solved = 0;
            foreach (var p in state.door) { wrong += p.Value.wrong; if (p.Value.solved) solved++; }
            return loc.T("ui.end_stats", "door", solved, "field", state.fieldSolved.Count, "wrong", wrong,
                         "hints", state.hintsUsed, "energy", state.energy, "ink", state.ink);
        }
    }
}
