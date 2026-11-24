// 화면 위의 글과 창 (IMGUI): 늘 떠 있는 표시, 사물 이름표, 대화, 문 퍼즐, 제작, 오메가 문서, 끝 화면.
// 자리는 640×360 화면 기준으로 적고(Godot판과 같은 배치), 그릴 때 창 크기에 맞춰 늘린다.
// 문장은 모두 Loc.T(키)로 찾는다. 저장소에 글꼴 파일이 없어서 시스템 한글 글꼴을 쓴다.
using System.Collections.Generic;
using UnityEngine;
using Sparks.Core;

namespace Sparks
{
    public class Ui : MonoBehaviour
    {
        static readonly Color Text = new Color(0.95f, 0.93f, 0.88f);
        static readonly Color Dim = new Color(0.6f, 0.59f, 0.55f);
        static readonly Color Panel = new Color(0.13f, 0.12f, 0.15f, 0.94f);
        static readonly Color Accent = new Color(0.95f, 0.78f, 0.35f);
        const string AccentHex = "#f2c75a", DimHex = "#9a968c", OkHex = "#8fd18a", NoHex = "#e07a6a";

        GameView view; Game g; Loc L;
        float s = 1f; float ox, oy;
        Font font; GUIStyle style;
        Vector2 scrollText, scrollHints, scrollResult, scrollOmega;
        string lastPuzzle = "";

        void Start()
        {
            view = GameView.I; g = view.game; L = g.loc;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Noto Sans KR", "NanumGothic", "Arial" }, 16);
        }

        // ------------------------------------------------------------------ 그리기 도우미 (자리는 640×360 기준)

        Rect R(float x, float y, float w, float h) { return new Rect(ox + x * s, oy + y * s, w * s, h * s); }

        void Box(Rect r, Color c)
        {
            Color old = GUI.color; GUI.color = c;
            GUI.DrawTexture(r, Art.White);
            GUI.color = old;
        }

        GUIStyle St(float size, Color c, TextAnchor anchor, bool wrap)
        {
            if (style == null) { style = new GUIStyle(); style.richText = true; }
            style.font = font;
            style.fontSize = Mathf.Max(1, Mathf.RoundToInt(size * s));
            style.alignment = anchor; style.wordWrap = wrap; style.clipping = TextClipping.Overflow;
            style.normal.textColor = c;
            return style;
        }

        void Label(Rect r, string text, float size, Color c, TextAnchor anchor = TextAnchor.UpperLeft, bool wrap = false)
        {
            GUI.Label(r, text, St(size, c, anchor, wrap));
        }

        /// <summary>검은 테두리를 두른 글 (배경 그림 위에 바로 쓰는 글)</summary>
        void Outlined(Rect r, string text, float size, Color c, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            float d = Mathf.Max(1f, Mathf.Round(s * 0.5f));
            var black = new Color(0f, 0f, 0f, 0.9f * c.a);
            Label(new Rect(r.x - d, r.y, r.width, r.height), text, size, black, anchor);
            Label(new Rect(r.x + d, r.y, r.width, r.height), text, size, black, anchor);
            Label(new Rect(r.x, r.y - d, r.width, r.height), text, size, black, anchor);
            Label(new Rect(r.x, r.y + d, r.width, r.height), text, size, black, anchor);
            Label(r, text, size, c, anchor);
        }

        /// <summary>단추. 눌렸으면 true. on = 켜진(고른) 모양</summary>
        bool Button(Rect r, string text, bool enabled = true, bool on = false, float size = 9f)
        {
            bool hover = enabled && r.Contains(Event.current.mousePosition);
            Color c = !enabled ? new Color(0.18f, 0.17f, 0.2f) : on ? new Color(0.5f, 0.4f, 0.2f) : hover ? new Color(0.34f, 0.31f, 0.4f) : new Color(0.25f, 0.23f, 0.3f);
            Box(r, c);
            Label(new Rect(r.x + 3f * s, r.y, r.width - 6f * s, r.height), text, size, enabled ? Text : new Color(0.5f, 0.5f, 0.5f), TextAnchor.MiddleCenter);
            return enabled && GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        /// <summary>긴 글. 칸을 넘치면 스크롤된다.</summary>
        void LongText(Rect r, ref Vector2 scroll, string text, float size)
        {
            GUIStyle st = St(size, Text, TextAnchor.UpperLeft, true);
            float h = st.CalcHeight(new GUIContent(text), r.width - 14f * s);
            if (h <= r.height) { GUI.Label(r, text, st); return; }
            scroll = GUI.BeginScrollView(r, scroll, new Rect(0f, 0f, r.width - 14f * s, h));
            GUI.Label(new Rect(0f, 0f, r.width - 14f * s, h), text, St(size, Text, TextAnchor.UpperLeft, true));
            GUI.EndScrollView();
        }

        static string Col(string hex, string text) { return "<color=" + hex + ">" + text + "</color>"; }

        // ------------------------------------------------------------------

        void OnGUI()
        {
            if (view == null || g == null) return;
            s = view.scale;
            ox = view.viewRect.x; oy = Screen.height - (view.viewRect.y + view.viewRect.height);
            GUI.depth = 0;

            if (g.puzzle == null && !g.endOpen)
            {
                if (view.debugView) DebugView();
                Labels();
                Hud();
            }
            if (g.DialogueOpen) Dialogue();
            if (g.puzzle != null) Puzzle();
            if (g.craftId != null) Craft();
            if (g.omegaOpen) Omega();
            if (g.endOpen) End();
        }

        // ------------------------------------------------------------------ 사물 이름표 (임시 그림 위에 띄운다)

        void Labels()
        {
            foreach (FieldObject o in g.zone.objectList)
            {
                if (!o.visible || !o.HasLabel) continue;
                RectF h = o.Hotspot();
                Vector2 p = view.MapToView(h.x + h.w / 2f, h.y);
                if (p.x < -40f || p.x > Config.ViewW + 40f || p.y < 0f || p.y > Config.ViewH + 10f) continue;
                Outlined(R(p.x - 60f, p.y - 9f, 120f, 9f), L.T(J.Str(o.def, "name_key")), 6f, new Color(1f, 1f, 1f, 0.9f), TextAnchor.LowerCenter);
            }
        }

        // ------------------------------------------------------------------ 늘 떠 있는 표시: 지역 이름, 잉크·에너지, 스파크 칸(1~8), 알림 한 줄, 조작 안내

        void Hud()
        {
            State st = g.state;
            if (st.zone != "" && g.data.zones.ContainsKey(st.zone))
                Outlined(R(6, 3, 200, 14), L.T(J.Str(g.data.zones[st.zone], "name_key")), 9f, Text);
            Outlined(R(440, 3, 194, 14), L.T("ui.hud_stats", "ink", st.ink, "energy", st.energy), 9f, Text, TextAnchor.UpperRight);
            if (st.investigate) Outlined(R(170, 3, 300, 14), L.T("ui.hud_investigate"), 8f, Accent, TextAnchor.UpperCenter);
            if (g.toastLeft > 0f)
                Outlined(R(20, 22, 600, 14), g.toastText, 9f, new Color(Text.r, Text.g, Text.b, Mathf.Clamp01(g.toastLeft)), TextAnchor.UpperCenter);
            Outlined(R(200, 347, 436, 10), L.T("ui.hud_controls"), 6f, Text, TextAnchor.UpperRight);
            for (int i = 0; i < g.data.sparks.Count; i++)
            {
                string id = J.Str(g.data.sparks[i], "id");
                bool owned = st.sparks.Contains(id);
                Rect r = R(6 + i * 23, 334, 21, 21);
                if (st.selectedSpark == id) Box(new Rect(r.x - 2f * s, r.y - 2f * s, r.width + 4f * s, r.height + 4f * s), Accent);
                Box(r, new Color(0.1f, 0.1f, 0.12f, owned ? 0.8f : 0.4f));
                Texture2D icon = owned ? Art.Tex("assets/sparks/" + id + "_idle.png") : null;
                if (icon != null)
                {
                    Color old = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, st.placed.ContainsKey(id) ? 0.35f : 1f);
                    GUI.DrawTextureWithTexCoords(R(7 + i * 23, 335, 19, 19), icon, new Rect(0f, 0f, (float)icon.height / icon.width, 1f));
                    GUI.color = old;
                }
                Outlined(R(8 + i * 23, 333, 10, 8), (i + 1).ToString(), 6f, Text);
            }
        }

        // ------------------------------------------------------------------ 대화 (E, Space/Enter, 좌클릭으로 다음 줄 — GameView가 받는다)

        void Dialogue()
        {
            if (g.dialogueVision) Box(R(0, 0, Config.ViewW, Config.ViewH), new Color(0.25f, 0.4f, 0.7f, 0.35f));
            Box(R(16, 262, 608, 92), Panel);
            string speaker = g.DialogueSpeaker;
            Texture2D portrait = Art.Tex(g.DialoguePortrait, true);
            float tx = portrait != null ? 94f : 14f;
            if (portrait != null) GUI.DrawTexture(R(24, 270, 76, 76), portrait, ScaleMode.ScaleToFit);
            if (speaker != "narrator")
                Label(R(16 + tx, 268, 300, 14), L.T("speaker." + speaker) + (g.dialogueVision ? L.T("ui.vision_tag") : ""), 10f, Accent);
            Label(R(16 + tx, 284, 598f - tx, 60), g.DialogueText, 10f, speaker == "narrator" ? new Color(0.82f, 0.86f, 0.95f) : Text, TextAnchor.UpperLeft, true);
            Label(R(416, 340, 200, 10), L.T("ui.dialogue_next"), 7f, new Color(0.7f, 0.7f, 0.7f), TextAnchor.UpperRight);
        }

        // ------------------------------------------------------------------ 문 퍼즐 (문제는 data/puzzles/<ID>.json, 그림은 assets/puzzles/<ID>.png)

        void Puzzle()
        {
            PuzzleSession p = g.puzzle; string id = p.id;
            if (lastPuzzle != id) { lastPuzzle = id; scrollText = scrollHints = scrollResult = Vector2.zero; }
            DoorRecord rec = g.state.DoorRecord(id);
            int energy = J.Int(p.pz, "energy");
            Box(R(0, 0, Config.ViewW, Config.ViewH), new Color(0.1f, 0.09f, 0.12f, 1f));
            Label(R(12, 6, 400, 16), L.T("puzzle." + id + ".title") + "  ·  " + L.T("ui.puzzle_type." + J.Str(p.pz, "door_type", "principle")), 12f, Accent);
            Label(R(300, 9, 328, 12), L.T("ui.puzzle_energy", "now", State.EnergyFor(energy, rec.wrong), "base", energy, "wrong", rec.wrong, "ink", g.state.ink), 8f, Text, TextAnchor.UpperRight);
            Texture2D fig = Art.Tex("assets/puzzles/" + id + ".png", true);
            if (fig != null) GUI.DrawTexture(R(12, 28, 220, 160), fig, ScaleMode.ScaleToFit);
            LongText(R(240, 26, 388, 164), ref scrollText, L.T("puzzle." + id + ".text"), 8f);

            if (p.resultShown)
            {
                Box(R(12, 192, 616, 164), new Color(0.16f, 0.2f, 0.16f, 1f));
                string txt = Col(AccentHex, L.T("ui.puzzle_correct")) + "\n" + L.T("puzzle." + id + ".explanation") + "\n\n"
                           + Col(AccentHex, L.T("ui.puzzle_parchment")) + "\n" + L.T("puzzle." + id + ".parchment") + "\n\n" + L.T("ui.puzzle_gain", "n", p.gained);
                LongText(R(20, 198, 600, 124), ref scrollResult, txt, 9f);
                if (Button(R(538, 326, 82, 26), L.T("ui.puzzle_close"))) g.PuzzleCloseResult();
                return;
            }

            List<object> options = p.Options;
            bool choice = p.kind == "choice";
            for (int i = 0; i < options.Count; i++)
            {
                Rect r = R(12 + (i % 2) * 311, 194 + (i / 2) * 28, 305, 24);
                bool used = !choice && p.sequence.Contains(i);
                if (Button(r, L.T("puzzle." + id + ".option." + i), !used, choice && p.choice == i, 8f)) g.PuzzlePick(i);
            }
            if (!choice)
            {
                var names = new List<string>();
                foreach (int i in p.sequence) names.Add(L.T("puzzle." + id + ".option." + i));
                Label(R(12, 249, 616, 12), L.T("ui.puzzle_order_seq", "seq", names.Count > 0 ? string.Join("  →  ", names.ToArray()) : "…"), 8f, Accent);
            }
            Box(R(12, 263, 616, 62), Panel);
            string hints;
            if (rec.hints == 0) hints = Col(DimHex, L.T("ui.puzzle_hints_empty"));
            else
            {
                var lines = new List<string>();
                for (int i = 0; i < rec.hints; i++)
                    lines.Add(Col(AccentHex, i == 3 ? L.T("ui.puzzle_hint_special") : L.T("ui.puzzle_hint_title", "n", i + 1)) + " " + L.T("puzzle." + id + ".hint." + i));
                hints = string.Join("\n", lines.ToArray());
            }
            LongText(R(18, 265, 604, 58), ref scrollHints, hints, 8f);
            bool more = rec.hints < Config.HintCosts.Length;
            if (Button(R(12, 329, 110, 26), more ? L.T("ui.puzzle_hint_btn", "cost", Config.HintCosts[rec.hints]) : L.T("ui.puzzle_hint_none"), more)) g.PuzzleTakeHint();
            if (!choice && Button(R(128, 329, 70, 26), L.T("ui.puzzle_reset_order"))) g.PuzzleResetOrder();
            Label(R(204, 335, 250, 14), p.feedback, 8f, new Color(1f, 0.6f, 0.5f));
            if (Button(R(462, 329, 80, 26), L.T("ui.puzzle_submit"))) g.PuzzleSubmit();
            if (Button(R(548, 329, 80, 26), L.T("ui.puzzle_quit"))) g.PuzzleQuit();
        }

        // ------------------------------------------------------------------ 발명품 제작 (연구실 작업대). 비용은 data/inventions.json의 cost

        void Craft()
        {
            Box(R(0, 0, Config.ViewW, Config.ViewH), new Color(0f, 0f, 0f, 0.6f));
            Box(R(140, 70, 360, 220), Panel);
            Label(R(152, 78, 336, 16), L.T("ui.craft_title") + " — " + L.T("invention." + g.craftId + ".name"), 12f, Accent);
            var lines = new List<string>();
            foreach (string line in g.CraftLines())
            {
                string body = line.Substring(1);
                lines.Add(line[0] == '+' ? Col(OkHex, "✔") + " " + body : line[0] == '-' ? Col(NoHex, "✘") + " " + body : body);
            }
            Label(R(152, 100, 336, 152), string.Join("\n", lines.ToArray()), 9f, Text, TextAnchor.UpperLeft, true);
            if (Button(R(300, 256, 90, 26), L.T("ui.craft_make"), g.CraftCanMake())) { g.CraftMake(); return; }
            if (Button(R(398, 256, 90, 26), L.T("ui.craft_close"))) g.CraftCancel();
        }

        // ------------------------------------------------------------------ 오메가 문서 (Tab)

        void Omega()
        {
            Box(R(0, 0, Config.ViewW, Config.ViewH), new Color(0f, 0f, 0f, 0.6f));
            Box(R(40, 20, 560, 320), new Color(0.2f, 0.17f, 0.14f, 0.98f));
            Label(R(52, 26, 130, 16), L.T("ui.omega_title"), 12f, Accent);
            for (int i = 0; i < Game.OmegaTabs.Length; i++)
                if (Button(R(190 + i * 92, 24, 88, 26), L.T("ui.omega_tab_" + Game.OmegaTabs[i]), true, g.omegaTab == Game.OmegaTabs[i]))
                {
                    g.omegaTab = Game.OmegaTabs[i]; scrollOmega = Vector2.zero;
                }
            if (Button(R(510, 24, 80, 26), L.T("ui.omega_close"))) { g.omegaOpen = false; return; }
            var lines = new List<string>();
            foreach (string line in g.OmegaLines(g.omegaTab))
            {
                string body = line.Substring(1);
                lines.Add(line[0] == 'H' ? Col(AccentHex, body) : line[0] == 'D' ? Col("#77736b", body) : body);
            }
            Rect body0 = R(54, 58, 532, 274);
            if (g.omegaTab == "sparks")
            {
                // 깨운 스파크는 도감 그림과 함께 보여 준다
                float y = 58f;
                foreach (object sp in g.data.sparks)
                {
                    string id = J.Str(sp, "id");
                    bool owned = g.state.sparks.Contains(id);
                    Texture2D card = owned ? Art.Tex("assets/sparks/" + id + "_card.png", true) : null;
                    if (card != null) GUI.DrawTexture(R(54, y, 28, 28), card, ScaleMode.ScaleToFit);
                    if (owned) Label(R(90, y, 480, 28), L.T("spark." + id + ".name") + " — " + L.T("spark." + id + ".phenomenon"), 9f, Accent, TextAnchor.MiddleLeft);
                    else Label(R(90, y, 480, 28), "● " + L.T("ui.omega_spark_unknown"), 9f, new Color(0.47f, 0.45f, 0.42f), TextAnchor.MiddleLeft);
                    y += 33f;
                }
            }
            else LongText(body0, ref scrollOmega, string.Join("\n", lines.ToArray()), 9f);
        }

        // ------------------------------------------------------------------ 끝 화면: "프로토타입 끝"과 이번 플레이의 기록

        void End()
        {
            Box(R(0, 0, Config.ViewW, Config.ViewH), new Color(0.06f, 0.07f, 0.1f, 1f));
            Label(R(0, 60, 640, 40), L.T("ui.end_title"), 28f, Accent, TextAnchor.MiddleCenter);
            Label(R(0, 108, 640, 16), L.T("ui.end_thanks"), 10f, Text, TextAnchor.MiddleCenter);
            Label(R(170, 140, 300, 150), g.EndStats(), 9f, Text, TextAnchor.UpperLeft, true);
            if (Button(R(260, 304, 120, 24), L.T("ui.end_restart"))) g.Restart();
        }

        // ------------------------------------------------------------------ F1: 이동 영역·칸·막힌 곳 보기

        void Line(Vector2 a, Vector2 b, Color c)
        {
            Vector2 pa = new Vector2(ox + a.x * s, oy + a.y * s), pb = new Vector2(ox + b.x * s, oy + b.y * s);
            float len = (pb - pa).magnitude;
            if (len < 0.5f) return;
            Matrix4x4 m = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(pb.y - pa.y, pb.x - pa.x) * Mathf.Rad2Deg, pa);
            Box(new Rect(pa.x, pa.y - 0.5f * s, len, Mathf.Max(1f, s)), c);
            GUI.matrix = m;
        }

        void RectLines(RectF r, Color c)
        {
            Vector2 a = view.MapToView(r.x, r.y), b = view.MapToView(r.x + r.w, r.y + r.h);
            Line(a, new Vector2(b.x, a.y), c); Line(new Vector2(b.x, a.y), b, c); Line(b, new Vector2(a.x, b.y), c); Line(new Vector2(a.x, b.y), a, c);
        }

        void DebugView()
        {
            Zone z = g.zone;
            var red = new Color(1f, 0.2f, 0.2f, 0.9f); var green = new Color(0.3f, 1f, 0.5f, 0.5f); var blue = new Color(0.2f, 0.6f, 1f, 0.9f);
            for (int i = 0; i < z.walkPoly.Count; i++)
            {
                V2 a = z.walkPoly[i], b = z.walkPoly[(i + 1) % z.walkPoly.Count];
                Line(view.MapToView(a.x, a.y), view.MapToView(b.x, b.y), red);
            }
            if (z.grid != null)
            {
                var o = J.List(z.grid, "origin"); var cs = J.List(z.grid, "cell");
                int cols = J.Int(z.grid, "cols"), rows = J.Int(z.grid, "rows");
                float gx = J.At(o, 0), gy = J.At(o, 1), cw = J.At(cs, 0), ch = J.At(cs, 1);
                for (int c = 0; c <= cols; c++) Line(view.MapToView(gx + c * cw, gy), view.MapToView(gx + c * cw, gy + rows * ch), green);
                for (int r = 0; r <= rows; r++) Line(view.MapToView(gx, gy + r * ch), view.MapToView(gx + cols * cw, gy + r * ch), green);
            }
            foreach (RectF r in z.solids) RectLines(r, blue);
            foreach (FieldObject o in z.objectList)
            {
                RectF r = o.SolidRect();
                if (!r.Empty) RectLines(r, blue);
            }
        }
    }
}
