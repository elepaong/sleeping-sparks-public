// HUD drawn with IMGUI (no UI package dependency): light counter, spark journal, spell slots, prompts, dialogue, title.
using UnityEngine;

namespace SparkForest
{
    public class Hud : MonoBehaviour
    {
        Font font; GUIStyle st; float s = 3f; bool journal; bool help = true; bool helpTouched;
        static readonly Color Ink = new Color(0.40f, 0.33f, 0.47f);
        static readonly Color Soft = new Color(0.58f, 0.48f, 0.58f);
        static readonly Color Cream = new Color(1f, 0.98f, 0.94f);
        static readonly Color Rose = new Color(0.82f, 0.45f, 0.58f);

        void Start()
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Apple SD Gothic Neo", "AppleSDGothicNeo-Regular", "Malgun Gothic", "Noto Sans CJK KR", "Noto Sans KR", "NanumGothic", "Arial Unicode MS", "Arial" }, 16);
        }

        void Update()
        {
            if (Game.I == null || !Game.I.started) return;
            if (In.Down(Btn.Journal)) journal = !journal;
            if (In.Down(Btn.Help)) { help = !help; helpTouched = true; }
            if (!helpTouched && Game.I.playTime > 40f) help = false;
        }

        // ---------------------------------------------------------------- drawing helpers
        void Spr(string n, float x, float y, float sc)
        {
            SprMeta m = Art.Meta(n);
            GUI.DrawTextureWithTexCoords(new Rect(x, y, m.w * sc, m.h * sc), Art.Atlas, Art.UV(n));
        }

        void Panel(Rect r)
        {
            Rect uv = Art.UV("ui_panel"); float c = 8f * s;
            float[] xs = { r.x, r.x + c, r.xMax - c }; float[] ws = { c, Mathf.Max(0f, r.width - 2f * c), c };
            float[] ys = { r.y, r.y + c, r.yMax - c }; float[] hs = { c, Mathf.Max(0f, r.height - 2f * c), c };
            for (int iy = 0; iy < 3; iy++)
                for (int ix = 0; ix < 3; ix++)
                {
                    if (ws[ix] <= 0f || hs[iy] <= 0f) continue;
                    Rect t = new Rect(uv.x + uv.width * ix / 3f, uv.y + uv.height * (2 - iy) / 3f, uv.width / 3f, uv.height / 3f);
                    GUI.DrawTextureWithTexCoords(new Rect(xs[ix], ys[iy], ws[ix], hs[iy]), Art.Atlas, t);
                }
        }

        void Text(Rect r, string txt, float size, Color col, TextAnchor anchor, bool halo)
        {
            st.fontSize = Mathf.Max(8, Mathf.RoundToInt(size * s)); st.alignment = anchor;
            if (halo)
            {
                st.normal.textColor = new Color(Cream.r, Cream.g, Cream.b, 0.9f);
                float o = Mathf.Max(1f, s * 0.5f);
                GUI.Label(new Rect(r.x - o, r.y, r.width, r.height), txt, st); GUI.Label(new Rect(r.x + o, r.y, r.width, r.height), txt, st);
                GUI.Label(new Rect(r.x, r.y - o, r.width, r.height), txt, st); GUI.Label(new Rect(r.x, r.y + o, r.width, r.height), txt, st);
            }
            st.normal.textColor = col;
            GUI.Label(r, txt, st);
        }

        float TextW(string txt, float size)
        {
            st.fontSize = Mathf.Max(8, Mathf.RoundToInt(size * s));
            bool ww = st.wordWrap; st.wordWrap = false;
            float w = st.CalcSize(new GUIContent(txt)).x; st.wordWrap = ww;
            return w;
        }

        void Key(float x, float y, string label)
        {
            Spr("ui_key", x, y, s);
            Text(new Rect(x, y - s, 14f * s, 14f * s), label, 6.5f, Ink, TextAnchor.MiddleCenter, false);
        }

        Vector2 ToGui(Vector2 world)
        {
            Vector3 p = Game.I.cam.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
            return new Vector2(p.x, Screen.height - p.y);
        }

        // ---------------------------------------------------------------- main
        void OnGUI()
        {
            Game g = Game.I;
            if (g == null || g.player == null || Art.Atlas == null) return;
            if (Event.current.type != EventType.Repaint) return;
            if (st == null) { st = new GUIStyle(); st.richText = false; st.wordWrap = true; st.clipping = TextClipping.Overflow; }
            st.font = font;
            s = Mathf.Max(1f, Mathf.Round(Screen.height / 270f));
            float W = Screen.width, H = Screen.height;
            GUI.color = Color.white;

            if (!g.started) { Title(W, H); return; }

            // floating labels on sleeping gates / the Great Tree
            foreach (Thing t in g.things)
            {
                if (t.done || (t.kind != "gate" && t.kind != "greattree")) continue;
                Vector2 gp = ToGui(new Vector2(t.basePos.x, t.kind == "gate" ? t.basePos.y + 2.5f : t.basePos.y + 3.6f));
                if (gp.x < -100f || gp.x > W + 100f || gp.y < -60f || gp.y > H + 60f) continue;
                string lab = g.light + " / " + t.d.need;
                float w = TextW(lab, 7f) + 22f * s;
                Rect r = new Rect(gp.x - w * 0.5f, gp.y - 9f * s, w, 18f * s);
                Panel(r);
                Spr("ui_light", r.x + 5f * s, r.y + 4.5f * s, s);
                Text(new Rect(r.x + 15f * s, r.y, w - 18f * s, r.height), lab, 7f, g.light >= t.d.need ? Rose : Ink, TextAnchor.MiddleLeft, false);
            }

            // interaction prompt
            if (!g.Busy && g.player.target != null)
            {
                Thing t = g.player.target;
                Vector2 gp = ToGui(new Vector2(t.basePos.x, t.TopY + 0.35f));
                string lab = t.Prompt; float w = TextW(lab, 7f);
                float x = gp.x - (16f * s + w) * 0.5f, y = gp.y - 14f * s + Mathf.Sin(Time.time * 4f) * s;
                Key(x, y, "E");
                Text(new Rect(x + 16f * s, y, w + 4f * s, 14f * s), lab, 7f, Ink, TextAnchor.MiddleLeft, true);
            }

            // light + spark tally
            Rect top = new Rect(4f * s, 4f * s, 132f * s, 42f * s);
            Panel(top);
            Spr("ui_light", top.x + 8f * s, top.y + 7f * s, s);
            Text(new Rect(top.x + 19f * s, top.y + 4f * s, 60f * s, 14f * s), "빛  " + g.light, 9f, Ink, TextAnchor.MiddleLeft, false);
            int goal = g.NextGoal();
            Text(new Rect(top.x + 56f * s, top.y + 4f * s, 70f * s, 14f * s), g.ended ? "숲이 깨어났어요" : (goal > 0 ? "다음 목표  " + goal : ""), 6.5f, Soft, TextAnchor.MiddleRight, false);
            for (int i = 0; i < Texts.Types.Length; i++)
            {
                float x = top.x + 7f * s + i * 24.5f * s;
                Spr("spark_" + Texts.Types[i] + "_0", x, top.y + 21f * s, s);
                Text(new Rect(x + 13f * s, top.y + 22f * s, 14f * s, 14f * s), g.counts[Texts.Types[i]].ToString(), 6.5f, Ink, TextAnchor.MiddleLeft, false);
            }

            // spell slots
            float by = H - 22f * s;
            Slot(4f * s, by, "Space", "ui_light", "모으기", true, 0f, 30f);
            if (g.comps.Count == 0) Slot(66f * s, by, "Q", "ui_glow", "동료 없음", false, 0f, 14f);
            else
            {
                string ab = g.Active;
                Slot(66f * s, by, "Q", "ui_" + ab, Texts.AbilityName(ab), true, g.player.abilityCd, 14f);
                float px = 118f * s;
                Rect cr = new Rect(px, by, (22f + g.comps.Count * 17f + (g.comps.Count > 1 ? 34f : 0f)) * s, 18f * s);
                Panel(cr);
                for (int i = 0; i < g.comps.Count; i++)
                {
                    SprMeta cm = Art.Meta("comp_" + g.comps[i]);
                    GUI.color = new Color(1f, 1f, 1f, i == g.active ? 1f : 0.4f);
                    Spr("comp_" + g.comps[i], px + (5f + i * 17f) * s, by + (18f - cm.h) * 0.5f * s + (i == g.active ? -Mathf.Abs(Mathf.Sin(Time.time * 4f)) * s : 0f), s);
                }
                GUI.color = Color.white;
                Text(new Rect(px + (4f + g.comps.Count * 17f) * s, by, 60f * s, 18f * s), Texts.CompName(ab) + (g.comps.Count > 1 ? "   Tab 교대" : ""), 6f, Ink, TextAnchor.MiddleLeft, false);
            }

            if (help)
            {
                Rect hr = new Rect(W - 126f * s, H - 62f * s, 122f * s, 58f * s);
                Panel(hr);
                Text(new Rect(hr.x + 8f * s, hr.y + 5f * s, hr.width - 16f * s, hr.height - 10f * s),
                    "이동   WASD / 방향키\n흔들기·대화   E      모으기   Space 꾹\n동료 능력   Q      동료 교대   Tab\n도감   J      도움말   H", 6f, Ink, TextAnchor.UpperLeft, false);
            }

            if (g.toastT > 0f && !g.dialogOn)
            {
                float w = TextW(g.toast, 7.5f) + 24f * s;
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(g.toastT));
                Rect r = new Rect((W - w) * 0.5f, 10f * s, w, 20f * s);
                Panel(r); Text(r, g.toast, 7.5f, Rose, TextAnchor.MiddleCenter, false);
                GUI.color = Color.white;
            }

            if (journal) Journal(W, H);
            if (g.dialogOn) Dialog(W, H);
        }

        void Slot(float x, float y, string key, string icon, string label, bool on, float cd, float keyW)
        {
            GUI.color = new Color(1f, 1f, 1f, on ? (cd > 0f ? 0.55f : 1f) : 0.35f);
            float w = keyW * s + 34f * s;
            Panel(new Rect(x, y, w, 18f * s));
            if (keyW > 14f) Text(new Rect(x + 4f * s, y, keyW * s, 18f * s), key, 6f, Soft, TextAnchor.MiddleCenter, false);
            else Key(x + 3f * s, y + 2f * s, key);
            SprMeta m = Art.Meta(icon);
            Spr(icon, x + keyW * s + 6f * s, y + (18f - m.h) * 0.5f * s, s);
            Text(new Rect(x + keyW * s + 17f * s, y, 20f * s, 18f * s), label, 6f, Ink, TextAnchor.MiddleLeft, false);
            GUI.color = Color.white;
        }

        void Dialog(float W, float H)
        {
            Game g = Game.I;
            float w = Mathf.Min(W - 16f * s, 330f * s), h = 62f * s;
            Rect r = new Rect((W - w) * 0.5f, H - h - 6f * s, w, h);
            Panel(r);
            float ty = r.y + 7f * s;
            if (g.dialogWho.Length > 0)
            {
                float nw = TextW(g.dialogWho, 7f) + 16f * s;
                Rect nr = new Rect(r.x + 8f * s, r.y - 9f * s, nw, 17f * s);
                Panel(nr); Text(nr, g.dialogWho, 7f, Rose, TextAnchor.MiddleCenter, false);
                ty += 4f * s;
            }
            int n = Mathf.Clamp((int)g.typed, 0, g.dialogText.Length);
            Text(new Rect(r.x + 12f * s, ty, w - 24f * s, h - 14f * s), g.dialogText.Substring(0, n), 7.5f, Ink, TextAnchor.UpperLeft, false);
            if (n >= g.dialogText.Length && (int)(Time.time * 2.5f) % 2 == 0)
                Text(new Rect(r.xMax - 26f * s, r.yMax - 16f * s, 20f * s, 12f * s), "▼", 6f, Soft, TextAnchor.MiddleRight, false);
        }

        void Journal(float W, float H)
        {
            Game g = Game.I;
            float w = 230f * s, h = 150f * s;
            Rect r = new Rect((W - w) * 0.5f, (H - h) * 0.5f - 10f * s, w, h);
            Panel(r);
            Text(new Rect(r.x, r.y + 6f * s, w, 16f * s), "스파크 도감", 9f, Rose, TextAnchor.MiddleCenter, false);
            for (int i = 0; i < Texts.Types.Length; i++)
            {
                string t = Texts.Types[i]; float y = r.y + 28f * s + i * 23f * s;
                bool seen = g.counts[t] > 0;
                GUI.color = new Color(1f, 1f, 1f, seen ? 1f : 0.3f);
                Spr("spark_" + t + "_0", r.x + 14f * s, y, s * 1.5f);
                GUI.color = Color.white;
                Text(new Rect(r.x + 38f * s, y - 1f * s, 120f * s, 11f * s), seen ? Texts.SparkName(t) : "? ? ?", 7.5f, Ink, TextAnchor.MiddleLeft, false);
                Text(new Rect(r.x + 38f * s, y + 9f * s, w - 80f * s, 11f * s), seen ? Texts.SparkNote(t) : "아직 만나지 못했어요.", 6f, Soft, TextAnchor.MiddleLeft, false);
                Text(new Rect(r.xMax - 44f * s, y, 32f * s, 18f * s), "× " + g.counts[t], 8f, Ink, TextAnchor.MiddleRight, false);
            }
        }

        void Title(float W, float H)
        {
            GUI.color = new Color(1f, 0.985f, 0.94f, 0.78f);
            GUI.DrawTexture(new Rect(0f, 0f, W, H), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float cy = H * 0.3f;
            string[] ic = { "spark_leaf_0", "spark_mush_0", "spark_moon_0", "spark_dew_0", "spark_petal_0" };
            for (int i = 0; i < ic.Length; i++)
                Spr(ic[i], W * 0.5f + (i - 2) * 26f * s - 9f * s, cy - 34f * s - Mathf.Abs(Mathf.Sin(Time.time * 2.4f + i * 0.8f)) * 5f * s, s * 1.5f);
            Text(new Rect(0f, cy - 10f * s, W, 30f * s), Texts.Title, 20f, Rose, TextAnchor.MiddleCenter, false);
            Text(new Rect(0f, cy + 20f * s, W, 14f * s), "꼬마 마녀 루미와 세 동료, 숨어 버린 빛의 정령들", 7.5f, Ink, TextAnchor.MiddleCenter, false);
            Rect r = new Rect((W - 210f * s) * 0.5f, cy + 46f * s, 210f * s, 74f * s);
            Panel(r);
            Text(new Rect(r.x + 14f * s, r.y + 8f * s, r.width - 28f * s, r.height - 16f * s),
                "이동   WASD / 방향키\n흔들기 · 말 걸기   E\n스파크 모으기   Space 를 꾹 누르기\n동료 능력   Q      동료 교대   Tab\n도감   J      스테이지 바꿔 보기   F2", 6.5f, Ink, TextAnchor.UpperLeft, false);
            if ((int)(Time.time * 2f) % 2 == 0)
                Text(new Rect(0f, r.yMax + 10f * s, W, 14f * s), "아무 키나 눌러 시작", 7.5f, Rose, TextAnchor.MiddleCenter, false);
        }
    }
}
