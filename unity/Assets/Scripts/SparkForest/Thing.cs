// Everything the witch can touch: hiding spots, companions waiting to join, puzzle props that react to a
// companion's ability (wind / water / glow), gates, NPCs, signs and the Great Tree.
using UnityEngine;

namespace SparkForest
{
    public class Thing : MonoBehaviour
    {
        public ObjData d; public string kind; public bool done; public Vector2 basePos; public float reach = 0.3f;
        /// <summary>Flower lamps: 0 buried in leaves → 1 wilted bud → 2 in bloom → 3 lit.</summary>
        public int stage;
        SpriteRenderer sr; int sparksLeft; float shakeT; float twinkleT;

        public static Thing Create(ObjData o, Transform root)
        {
            string spr = o.s;
            switch (o.k)
            {
                case "gate": spr = "gate_closed"; break;
                case "npc": spr = o.s + "_0"; break;
                case "glowcap": spr = "glowcap_off"; break;
                case "lamp": spr = "leafpile"; break;
                case "greattree": spr = "greattree_asleep"; break;
            }
            SpriteRenderer r = Art.MakeWorld(o.k + "_" + o.s, spr, new Vector2(o.x, o.y), Art.Order(o.y), root);
            Thing t = r.gameObject.AddComponent<Thing>();
            t.d = o; t.kind = o.k; t.sr = r; t.basePos = new Vector2(o.x, o.y); t.sparksLeft = o.n;
            t.twinkleT = Random.Range(0.5f, 3f);
            if (o.k == "gate") t.reach = 1.7f;
            else if (o.k == "greattree") t.reach = 1.3f;
            else if (o.k == "blocker") t.reach = 0.8f;
            if (o.k == "gate" || o.k == "blocker") Game.I.SetRect(o.x - o.bw * 0.5f, o.y - 0.15f, o.x + o.bw * 0.5f, o.y - 0.15f + o.bh, 0);
            Game.I.things.Add(t);
            return t;
        }

        public Vector2 Center { get { return basePos + new Vector2(0f, kind == "rustle" ? 1.3f : 0.3f); } }
        public float TopY { get { return basePos.y + Mathf.Min(sr.sprite.bounds.size.y, 2.6f); } }

        public bool CanInteract
        {
            get
            {
                switch (kind)
                {
                    case "shake": case "rustle": return sparksLeft > 0;
                    case "wilt": case "blocker": case "sprout": case "gate": case "glowcap": case "comp": return !done;
                    case "lamp": return stage < 3;
                    default: return true;
                }
            }
        }

        public string Prompt
        {
            get
            {
                switch (kind)
                {
                    case "shake": return "흔들기";
                    case "npc": case "comp": return "말 걸기";
                    case "sign": return "읽기";
                    case "gate": return "등불문";
                    case "greattree": return "큰나무";
                }
                return "살펴보기";
            }
        }

        string Tint()
        {
            string n = sr.sprite.name;
            if (n.Contains("pink") || n.Contains("lilac") || n.Contains("red")) return "pink";
            if (n.Contains("gold") || n.Contains("pumpkin") || n.Contains("barrel") || n.Contains("log") || n.Contains("stump")) return "gold";
            return "mint";
        }

        void Shake(bool high)
        {
            shakeT = 0.45f;
            Sfx.Play("shake", 0.9f, 0.15f);
            Fx.I.Leaves(high ? Center : basePos + new Vector2(0f, 0.4f), high ? 10 : 5, Tint());
        }

        void Release(bool high)
        {
            Shake(high);
            if (sparksLeft > 0) Game.I.ReleaseSparks(d.t, sparksLeft, high ? Center : basePos + new Vector2(0f, 0.3f));
            sparksLeft = 0;
        }

        /// <summary>Hint line that names the companion whose ability is needed.</summary>
        static string Need(string ab)
        {
            Game g = Game.I;
            if (!g.Has(ab)) return "\n" + Texts.AbilityName(ab) + "을(를) 다루는 동료가 있다면…";
            if (g.Active != ab) return "\n[Tab]으로 " + Texts.CompName(ab) + "(으)로 교대해서 [Q]!";
            return "\n" + Texts.CompName(ab) + "에게 부탁해요. [Q]";
        }

        public void Interact()
        {
            Game g = Game.I;
            switch (kind)
            {
                case "shake": Release(false); break;
                case "rustle": g.Say("|나뭇가지 사이에서 무언가 반짝여요. 손이 닿지 않아요…" + Need("wind")); break;
                case "wilt": g.Say("|시들어 고개 숙인 꽃이에요. 촉촉한 비를 기다려요." + Need("water")); break;
                case "sprout": g.Say("|물가에 작은 새싹이 돋아 있어요. 비를 흠뻑 맞으면 쑥쑥 자랄 것 같아요." + Need("water")); break;
                case "glowcap": g.Say("|잠든 등불버섯이에요. 따뜻한 불빛을 나눠 주면 깨어날 것 같아요." + Need("glow")); break;
                case "blocker":
                    if (d.t == "glow") g.Say("|그늘 덩굴이 길을 꼭 감싸고 있어요. 밝은 빛을 보면 수줍어 물러난대요." + Need("glow"));
                    else g.Say("|낙엽이 길을 막을 만큼 수북해요." + Need("wind"));
                    break;
                case "lamp":
                    if (stage == 0) g.Say("|낙엽 더미 아래에 무언가 묻혀 있어요." + Need("wind"));
                    else if (stage == 1) g.Say("|꽃등 봉오리가 시들어 있어요." + Need("water"));
                    else g.Say("|꽃등이 활짝 피었어요. 불씨만 있으면 켜질 거예요." + Need("glow"));
                    break;
                case "sign": g.Say(Texts.Sign(d.id)); break;
                case "npc": g.Say(Texts.Npc(d.id)); break;
                case "comp": Befriend(); break;
                case "gate":
                    if (g.light >= d.need) OpenGate();
                    else { Sfx.Play("deny"); g.Say("|등불문이 깊이 잠들어 있어요.\n빛이 " + (d.need - g.light) + "만큼 더 필요해요. (지금 " + g.light + " / " + d.need + ")"); }
                    break;
                case "greattree":
                    if (g.ended) g.Say("|큰나무가 포근하게 숨 쉬고 있어요. 꽃등 아래로 스파크들이 모여들어요.");
                    else if (g.LampsLit() < 3) { Sfx.Play("deny"); g.Say("|큰나무는 아직 꿈속이에요.\n둘레의 꽃등 세 송이를 모두 밝혀야 해요. (지금 " + g.LampsLit() + " / 3)", "|낙엽을 걷고, 꽃을 피우고, 불을 밝혀요. 세 동료가 번갈아 도와줄 거예요."); }
                    else if (g.light < d.need) { Sfx.Play("deny"); g.Say("|꽃등은 모두 켜졌지만 큰나무는 아직 졸려요.\n빛이 " + (d.need - g.light) + "만큼 더 필요해요. (지금 " + g.light + " / " + d.need + ")"); }
                    else
                    {
                        done = true; sr.sprite = Art.S("greattree_awake"); Mood.Register(sr, 0.3f);
                        Fx.I.Leaves(basePos + new Vector2(0f, 4f), 40, "pink"); Fx.I.Sparkles(basePos + new Vector2(0f, 3.5f), 40, 3f);
                        g.Ending();
                    }
                    break;
            }
        }

        /// <summary>A companion joins once enough sparks of its own glade have been gathered.</summary>
        void Befriend()
        {
            Game g = Game.I;
            int have = g.counts[d.t];
            if (have < d.need) { g.Say(Texts.CompWait(d.id, have, d.need)); return; }
            done = true; sr.enabled = false;
            g.AddComp(d.id);
            Sfx.Play("learn"); Fx.I.Sparkles(basePos + new Vector2(0f, 0.5f), 24, 1.0f);
            g.Say(Texts.CompJoin(d.id));
        }

        void OpenGate()
        {
            Game g = Game.I;
            done = true; sr.sprite = Art.S("gate_open");
            g.SetRect(d.x - d.bw * 0.5f, d.y - 0.15f, d.x + d.bw * 0.5f, d.y - 0.15f + d.bh, 1);
            Sfx.Play("open"); Fx.I.Sparkles(basePos + new Vector2(0f, 0.8f), 18, 1.4f); Fx.I.Leaves(basePos + new Vector2(0f, 0.6f), 10, "mint");
            g.ambient.LightLanterns(basePos, 5.5f);
            g.Say("|등불문이 눈을 뜨고, 덩굴이 스르르 물러나요.");
        }

        void ClearBlocker()
        {
            done = true;
            if (d.t == "wind") { Fx.I.Leaves(basePos + new Vector2(0f, 0.4f), 14, "gold"); Fx.I.Leaves(basePos + new Vector2(0f, 0.4f), 8, "pink"); }
            else { Fx.I.Sparkles(basePos + new Vector2(0f, 0.5f), 12, 0.8f); Fx.I.Burst("puff_0", basePos + new Vector2(0f, 0.5f), 8, 2.5f, 0.6f, 0f, new Color(0.84f, 0.78f, 0.95f, 0.9f)); }
            Game.I.SetRect(d.x - d.bw * 0.5f, d.y - 0.15f, d.x + d.bw * 0.5f, d.y - 0.15f + d.bh, 1);
            Game.I.things.Remove(this);
            Destroy(gameObject);
        }

        /// <summary>Called for every thing inside the area of a companion ability: "wind", "water" or "glow".</summary>
        public void OnAbility(string ab)
        {
            Game g = Game.I;
            if (kind == "blocker" && !done && d.t == ab) { ClearBlocker(); return; }
            if (ab == "wind")
            {
                if ((kind == "rustle" || kind == "shake") && sparksLeft > 0) Release(kind == "rustle");
                else if (kind == "rustle") Shake(true);
                else if (kind == "lamp" && stage == 0)
                {
                    stage = 1; sr.sprite = Art.S("lamp_bud");
                    Fx.I.Leaves(basePos + new Vector2(0f, 0.4f), 14, "gold");
                }
            }
            else if (ab == "water")
            {
                if (kind == "wilt" && !done)
                {
                    done = true; sr.sprite = Art.S("bloom");
                    Fx.I.Sparkles(basePos + new Vector2(0f, 0.5f), 6, 0.4f);
                    g.ReleaseSparks(d.t, sparksLeft, basePos + new Vector2(0f, 0.4f)); sparksLeft = 0;
                }
                else if (kind == "sprout" && !done) GrowBridge();
                else if (kind == "lamp" && stage == 1)
                {
                    stage = 2; sr.sprite = Art.S("lamp_bloom");
                    Fx.I.Sparkles(basePos + new Vector2(0f, 0.6f), 8, 0.5f); Sfx.Play("pop", 0.8f, 0.1f);
                }
            }
            else if (ab == "glow")
            {
                if (kind == "glowcap" && !done)
                {
                    done = true; sr.sprite = Art.S("glowcap_on"); Mood.Register(sr, 0.3f);
                    g.ambient.AddGlowAt(basePos + new Vector2(0f, 0.6f));
                    g.ReleaseSparks(d.t, sparksLeft, basePos + new Vector2(0f, 0.4f)); sparksLeft = 0;
                }
                else if (kind == "lamp" && stage == 2)
                {
                    stage = 3; sr.sprite = Art.S("lamp_lit"); Mood.Register(sr, 0.3f);
                    g.ambient.AddGlowAt(basePos + new Vector2(0f, 0.8f));
                    Fx.I.Sparkles(basePos + new Vector2(0f, 0.8f), 14, 0.8f); Sfx.Play("open");
                    int n = g.LampsLit();
                    g.Toast(n >= 3 ? "꽃등 세 송이가 모두 켜졌어요! 큰나무에게 가 봐요." : "꽃등이 켜졌어요. (" + n + " / 3)");
                }
            }
        }

        void GrowBridge()
        {
            Game g = Game.I;
            done = true;
            float[] ax = { 0.95f, 1.95f, 2.95f }; float[] bx = { 1.45f, 2.45f, 3.45f };
            for (int i = 0; i < 3; i++)
            {
                Vector2 p = basePos + new Vector2(bx[i], 0.4f);
                Art.MakeWorld("lilypad", i == 1 ? "lily_big_flower" : "lily_big", p, -19990, transform.parent);
                Fx.I.Sparkles(p + new Vector2(0f, 0.4f), 5, 0.6f);
            }
            for (int i = 0; i < 3; i++)
            {
                Vector2 p = basePos + new Vector2(ax[i], -0.2f);
                Art.MakeWorld("lilypad", i == 0 ? "lily_big_flower" : "lily_big", p, -19980, transform.parent);
                Fx.I.Sparkles(p + new Vector2(0f, 0.4f), 5, 0.6f);
            }
            g.SetRect(basePos.x - 0.1f, basePos.y - 0.25f, basePos.x + 4.6f, basePos.y + 1.25f, 2);
            Sfx.Play("open");
            g.Toast("새싹이 자라 연잎 다리가 놓였어요!");
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (shakeT > 0f)
            {
                shakeT -= dt;
                float k = Mathf.Max(0f, shakeT / 0.45f);
                transform.position = new Vector3(basePos.x + Mathf.Sin(Time.time * 55f) * 0.07f * k, basePos.y, 0f);
                transform.localScale = new Vector3(1f + 0.06f * k * Mathf.Sin(Time.time * 40f), 1f - 0.06f * k * Mathf.Sin(Time.time * 40f), 1f);
                if (shakeT <= 0f) { transform.position = new Vector3(basePos.x, basePos.y, 0f); transform.localScale = Vector3.one; }
            }
            if (kind == "npc") sr.sprite = Art.S(d.s + "_" + ((int)(Time.time * 1.6f + basePos.x) % 2));
            if (kind == "comp" && !done)
                transform.position = new Vector3(basePos.x, basePos.y + Mathf.Abs(Mathf.Sin(Time.time * 2.6f + basePos.x)) * 0.12f, 0f);
            bool hint = ((kind == "shake" || kind == "rustle") && sparksLeft > 0) || (kind == "comp" && !done);
            if (hint)
            {
                twinkleT -= dt;
                if (twinkleT <= 0f)
                {
                    twinkleT = Random.Range(1.4f, 3.4f);
                    if ((Game.I.player.pos - basePos).sqrMagnitude < 150f)
                        Fx.I.Emit(Random.value < 0.5f ? "sparkle_0" : "sparkle_1", Center + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(0f, 0.6f)), new Vector2(0f, 0.3f), 0.7f, 0f, 0f, 0f, Color.white, 9100);
                }
            }
        }
    }
}
