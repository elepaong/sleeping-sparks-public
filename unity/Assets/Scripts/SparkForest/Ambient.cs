// Life in the forest: swaying plants, sun shafts through the canopy, pollen motes, butterflies, water glints, drifting petals.
using System.Collections.Generic;
using UnityEngine;

namespace SparkForest
{
    public class Ambient : MonoBehaviour
    {
        class Sway { public Transform tr; public Vector2 p; public float phase; public float kick; }
        class Bfly { public SpriteRenderer sr; public Vector2 pos; public Vector2 target; public float phase; public string col; }

        readonly List<Sway> sways = new List<Sway>();
        readonly List<SpriteRenderer> shafts = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> lanternsOff = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> glows = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> motes = new List<SpriteRenderer>();
        readonly List<Bfly> bflies = new List<Bfly>();
        float glintT, petalT;

        public void RegisterDeco(string name, SpriteRenderer sr, bool flat)
        {
            if (flat) return;
            if (name.StartsWith("flowers_") || name.StartsWith("tulip_") || name.StartsWith("tallgrass") || name == "fern" || name == "reeds")
            {
                Sway s = new Sway(); s.tr = sr.transform; s.p = sr.transform.position; s.phase = s.p.x * 0.9f + s.p.y * 0.4f;
                sways.Add(s);
            }
            else if (name == "lantern_on") { AddGlow(sr); Mood.Register(sr, 0.3f); }
            else if (name == "lantern_off") lanternsOff.Add(sr);
        }

        void AddGlow(SpriteRenderer lantern)
        {
            Vector3 p = lantern.transform.position;
            SpriteRenderer g = Art.Make("lanternGlow", "glow", new Vector2(p.x, p.y + 1.2f), 8000, transform);
            g.color = Mood.Glow;
            glows.Add(g);
        }

        public void AddGlowAt(Vector2 p)
        {
            SpriteRenderer g = Art.Make("lampGlow", "glow", p, 8000, transform);
            g.color = Mood.Glow;
            glows.Add(g);
        }

        public void LightLanterns(Vector2 center, float radius, bool quiet = false)
        {
            for (int i = lanternsOff.Count - 1; i >= 0; i--)
            {
                SpriteRenderer l = lanternsOff[i];
                if (((Vector2)l.transform.position - center).magnitude > radius) continue;
                l.sprite = Art.S("lantern_on"); AddGlow(l); Mood.Register(l, 0.3f);
                if (!quiet) Fx.I.Sparkles((Vector2)l.transform.position + new Vector2(0f, 1.2f), 6, 0.4f);
                lanternsOff.RemoveAt(i);
            }
        }

        public void AddShaft(Vector2 p)
        {
            SpriteRenderer s = Art.Make("sunshaft", "shaft", p + new Vector2(0.8f, 3.2f), 8500, transform);
            s.transform.rotation = Quaternion.Euler(0f, 0f, Mood.Stage.mood.shaftAngle);
            s.transform.localScale = new Vector3(Random.Range(0.9f, 1.5f), 1f, 1f);
            shafts.Add(s);
        }

        void Start()
        {
            MoodData sm = Mood.Stage.mood;      // counts are per stage; colours follow the area the witch is in
            if (sm.lanternsLit) LightLanterns(Vector2.zero, 9999f, true);
            for (int i = 0; i < sm.moteCount; i++)
            {
                SpriteRenderer m = Art.Make("mote", "mote", Game.I.player.pos + Random.insideUnitCircle * 12f, 8600, transform);
                motes.Add(m);
            }
            string[] cols = { "pink", "blue", "gold" };
            for (int i = 0; i < sm.butterflies; i++)
            {
                Bfly b = new Bfly(); b.col = cols[i % 3]; b.phase = Random.value * 10f;
                b.pos = Game.I.FreeSpotNear(Game.I.player.pos, 1f, 9f); b.target = b.pos;
                b.sr = Art.Make("butterfly", "bfly_" + b.col + "_0", b.pos, 8700, transform);
                bflies.Add(b);
            }
        }

        void Update()
        {
            Game g = Game.I; float t = Time.time, dt = Time.deltaTime;
            Vector2 pp = g.player.pos; Vector3 cp = g.cam.transform.position;
            float hh = g.cam.orthographicSize + 1f, hw = g.cam.orthographicSize * g.cam.aspect + 1f;

            for (int i = 0; i < sways.Count; i++)
            {
                Sway s = sways[i];
                if (Mathf.Abs(s.p.x - cp.x) > hw + 1f || Mathf.Abs(s.p.y - cp.y) > hh + 2f) continue;
                if (Mathf.Abs(s.p.x - pp.x) < 0.5f && Mathf.Abs(s.p.y - pp.y) < 0.35f) s.kick = 1f;     // brushing past
                s.kick = Mathf.Max(0f, s.kick - dt * 2.2f);
                float a = Mathf.Sin(t * 1.5f + s.phase) * 2.4f + Mathf.Sin(t * 17f) * 9f * s.kick;
                s.tr.rotation = Quaternion.Euler(0f, 0f, a);
            }

            for (int i = 0; i < shafts.Count; i++)
            {
                float a = 0.42f + 0.3f * Mathf.Sin(t * 0.45f + i * 1.7f) + 0.06f * Mathf.Sin(t * 2.1f + i);
                Color sc = Mood.Shaft; sc.a = Mathf.Clamp01(a) * Mood.Shaft.a;
                shafts[i].color = sc;
            }
            for (int i = 0; i < glows.Count; i++)
            {
                float s = 1.25f + 0.08f * Mathf.Sin(t * 6f + i * 2.3f);
                glows[i].transform.localScale = new Vector3(s, s, 1f);
                glows[i].color = Mood.Glow;
            }

            for (int i = 0; i < motes.Count; i++)
            {
                Transform m = motes[i].transform; Vector3 p = m.position;
                p.x += (0.12f + 0.25f * Mathf.Sin(t * 0.7f + i)) * dt; p.y += (0.18f + 0.1f * Mathf.Sin(t * 1.1f + i * 2f)) * dt;
                if (p.x < cp.x - hw || p.x > cp.x + hw || p.y < cp.y - hh || p.y > cp.y + hh)
                    p = new Vector3(cp.x + Random.Range(-hw, hw), cp.y + Random.Range(-hh, hh), 0f);
                m.position = p;
                Color mc = Mood.Mote; mc.a = 0.25f + 0.55f * Mathf.Abs(Mathf.Sin(t * 1.3f + i * 0.77f));
                motes[i].color = mc;
            }

            for (int i = 0; i < bflies.Count; i++)
            {
                Bfly b = bflies[i];
                Vector2 d = b.target - b.pos;
                if (d.magnitude < 0.15f || (b.pos - pp).magnitude > 15f)
                {
                    if ((b.pos - pp).magnitude > 15f) b.pos = g.FreeSpotNear(pp, 7f, 10f);
                    b.target = g.FreeSpotNear(pp, 1f, 8f);
                }
                else b.pos += d.normalized * 1.4f * dt;
                float flutter = Mathf.Sin(t * 6f + b.phase) * 0.18f;
                b.sr.transform.position = new Vector3(b.pos.x, b.pos.y + 0.9f + flutter, 0f);
                b.sr.sprite = Art.S("bfly_" + b.col + "_" + ((int)(t * 9f + b.phase) % 2));
                b.sr.flipX = d.x < 0f;
            }

            glintT -= dt;
            if (glintT <= 0f)
            {
                glintT = 0.12f;
                int cx = Mathf.FloorToInt((cp.x + Random.Range(-hw, hw)) / g.map.cell), cy = Mathf.FloorToInt((cp.y + Random.Range(-hh, hh)) / g.map.cell);
                if (cx >= 0 && cy >= 0 && cx < g.map.gw && cy < g.map.gh && g.water[cx, cy])
                {
                    Vector2 p = new Vector2((cx + Random.value) * g.map.cell, (cy + Random.value) * g.map.cell);
                    Fx.I.Emit(Random.value < 0.6f ? "ripple" : "sparkle_1", p, new Vector2(0f, -0.25f), Random.Range(0.6f, 1.1f), 0f, 0f, 0f, new Color(1f, 1f, 1f, 0.9f), -19995);
                }
            }
            petalT -= dt;
            if (petalT <= 0f)
            {
                MoodData tm = Mood.Target;
                petalT = Random.Range(tm.fallMin, Mathf.Max(tm.fallMin, tm.fallMax));
                string[] tints = tm.fall;
                string sp = tints != null && tints.Length > 0 ? "leafbit_" + tints[Random.Range(0, tints.Length)] + "_" + Random.Range(0, 2) : "";
                if (Art.Has(sp)) Fx.I.Emit(sp, new Vector2(cp.x + Random.Range(-hw, hw + 2f), cp.y + hh), new Vector2(Random.Range(-0.9f, -0.3f), Random.Range(-1.3f, -0.8f)), 7f, 0f, 0f, 0.9f, Color.white, 8800);
            }
        }
    }
}
