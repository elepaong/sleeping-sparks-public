// A spark: a tiny light spirit. Hops out of hiding, wanders, scampers from the witch, and drifts in when the wand calls.
using UnityEngine;

namespace SparkForest
{
    public class Spark : MonoBehaviour
    {
        enum St { Hop, Roam, Flee, Dizzy }

        public string type; public Vector2 pos;
        St st; Vector2 from, to, target; float t, wait, z, phase, dizzy; bool pulled;
        SpriteRenderer sr, glow, shadow, dots;

        public void Init(string type, Vector2 from, Vector2 to, bool hop)
        {
            this.type = type; this.from = from; this.to = to; pos = hop ? from : to; target = to;
            st = hop ? St.Hop : St.Roam; t = 0f; wait = Random.Range(0.2f, 1.5f); phase = Random.value * 10f;
            sr = Art.MakeWorld("body", "spark_" + type + "_0", Vector2.zero, 0, transform, 0.35f);   // light spirits keep most of their own glow
            glow = Art.Make("glow", "glow", Vector2.zero, 0, transform); glow.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            shadow = Art.Make("shadow", "shadow", Vector2.zero, -19000, transform); shadow.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            dots = Art.Make("dots", "dots", Vector2.zero, 9400, transform); dots.transform.localPosition = new Vector3(0f, 0.95f, 0f); dots.enabled = false;
            Color c = Color.white;
            switch (type)
            {
                case "leaf": c = new Color(0.8f, 1f, 0.75f); break;
                case "mush": c = new Color(1f, 0.8f, 0.88f); break;
                case "dew": c = new Color(0.78f, 0.92f, 1f); break;
                case "petal": c = new Color(1f, 0.93f, 0.7f); break;
                case "moon": c = new Color(0.9f, 0.84f, 1f); break;
            }
            glow.color = c;
            Apply();
        }

        public void Blow(Vector2 dir)
        {
            if (st == St.Hop) return;
            st = St.Dizzy; dizzy = 3.2f;
            Step(dir * 0.8f, true);
        }

        void Step(Vector2 d, bool collide)
        {
            Game g = Game.I;
            if (!collide || g.Blocked(pos + new Vector2(0f, 0.1f), 0.18f, 0.12f)) { pos += d; return; }
            Vector2 c = pos + new Vector2(0f, 0.1f);
            if (!g.Blocked(c + new Vector2(d.x, 0f), 0.18f, 0.12f)) { pos.x += d.x; c.x += d.x; }
            if (!g.Blocked(c + new Vector2(0f, d.y), 0.18f, 0.12f)) pos.y += d.y;
        }

        void Update()
        {
            Game g = Game.I; Player p = g.player; float dt = Time.deltaTime;
            Vector2 toP = p.pos - pos; float dist = toP.magnitude;
            pulled = false;

            if (st == St.Hop)
            {
                t += dt / 0.5f;
                float u = Mathf.Clamp01(t);
                pos = Vector2.Lerp(from, to, u); z = Mathf.Sin(u * 3.1416f) * 1.1f;
                if (t >= 1f) { st = St.Roam; z = 0f; Fx.I.Emit("puff_1", pos + new Vector2(0f, 0.1f), Vector2.zero, 0.25f, 0f, 0f, 0f, new Color(1f, 1f, 1f, 0.7f), Art.Order(pos.y) - 1); }
                Apply(); return;
            }

            if (p.beaming && !g.Busy)
            {
                Vector2 rel = (pos + new Vector2(0f, 0.3f)) - p.Chest; float rd = rel.magnitude;
                if (rd < Player.BeamRange && (rd < 1.0f || Vector2.Dot(rel / Mathf.Max(rd, 0.001f), p.face) > 0.6f)) pulled = true;
            }

            if (pulled)
            {
                Step(toP.normalized * Mathf.Min(dist, (st == St.Dizzy ? 7f : 5.2f) * dt), false);
                if (Random.value < 0.25f) Fx.I.Emit("sparkle_2", pos + new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.1f, 0.6f)), Vector2.zero, 0.3f, 0f, 0f, 0f, Color.white, 9100);
                if (dist < 0.5f)
                {
                    g.AddLight(type, pos + new Vector2(0f, 0.4f));
                    g.sparks.Remove(this);
                    Destroy(gameObject);
                    return;
                }
            }
            else if (st == St.Dizzy)
            {
                dizzy -= dt;
                if (dizzy <= 0f) { st = St.Roam; wait = 0.3f; }
            }
            else if (st == St.Flee)
            {
                Vector2 away = dist > 0.01f ? -toP / dist : Vector2.up;
                Vector2 before = pos;
                Step(away * 2.5f * dt, true);
                if ((pos - before).sqrMagnitude < 0.00001f)                      // cornered: sidle along the edge
                    Step(new Vector2(-away.y, away.x) * (Mathf.Sin(phase) > 0f ? 2.5f : -2.5f) * dt, true);
                if (dist > 3.3f) { st = St.Roam; wait = 0.4f; target = pos; }
            }
            else
            {
                if (dist < 1.9f && !g.Busy) st = St.Flee;
                else if (wait > 0f) wait -= dt;
                else
                {
                    Vector2 d = target - pos;
                    if (d.magnitude < 0.08f || g.Blocked(pos + new Vector2(0f, 0.1f), 0.18f, 0.12f) && d.magnitude > 3f)
                    {
                        wait = Random.Range(0.4f, 2.2f); target = g.FreeSpotNear(pos, 0.5f, 2.4f);
                    }
                    else
                    {
                        Vector2 before = pos;
                        Step(d.normalized * Mathf.Min(d.magnitude, 1.2f * dt), !g.Blocked(pos + new Vector2(0f, 0.1f), 0.18f, 0.12f));
                        if ((pos - before).sqrMagnitude < 0.000001f) { target = g.FreeSpotNear(pos, 0.5f, 2.4f); wait = 0.3f; }
                    }
                }
            }
            float hopRate = st == St.Flee || pulled ? 14f : 5f;
            z = st == St.Dizzy && !pulled ? 0f : Mathf.Abs(Mathf.Sin(Time.time * hopRate + phase)) * (st == St.Flee ? 0.22f : 0.09f);
            Apply();
        }

        void Apply()
        {
            int o = Art.Order(pos.y);
            transform.position = new Vector3(pos.x, pos.y + z, 0f);
            sr.sortingOrder = o; glow.sortingOrder = o - 1;
            int frame = (int)(Time.time * 4f + phase) % 2;
            sr.sprite = Art.S("spark_" + type + "_" + frame);
            shadow.transform.localPosition = new Vector3(0f, 0.03f - z, 0f);
            float pulse = 0.5f + 0.2f * Mathf.Sin(Time.time * 3f + phase);
            Color c = glow.color; c.a = pulled ? 0.95f : pulse; glow.color = c;
            float gs = pulled ? 0.8f : 0.55f; glow.transform.localScale = new Vector3(gs, gs, 1f);
            dots.enabled = st == St.Dizzy;
            if (dots.enabled) dots.transform.localPosition = new Vector3(Mathf.Sin(Time.time * 8f) * 0.1f, 0.95f, 0f);
        }
    }
}
