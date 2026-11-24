// Tiny pooled sprite-particle system (leaves, sparkles, puffs, droplets).
using System.Collections.Generic;
using UnityEngine;

namespace SparkForest
{
    public class Fx : MonoBehaviour
    {
        class P
        {
            public Transform tr; public SpriteRenderer sr; public Vector2 vel; public float life; public float max;
            public float grav; public float drag; public float sway; public float phase; public bool on; public Color col;
        }

        public static Fx I;
        readonly List<P> pool = new List<P>();

        void Awake() { I = this; }

        P Get()
        {
            for (int i = 0; i < pool.Count; i++) if (!pool[i].on) return pool[i];
            P p = new P();
            SpriteRenderer sr = Art.Make("fx", "white", Vector2.zero, 9000, transform);
            p.sr = sr; p.tr = sr.transform; pool.Add(p);
            return p;
        }

        public void Emit(string sprite, Vector2 pos, Vector2 vel, float life, float grav, float drag, float sway, Color col, int order)
        {
            P p = Get();
            p.on = true; p.sr.enabled = true; p.sr.sprite = Art.S(sprite); p.sr.sortingOrder = order; p.col = col; p.sr.color = col;
            p.tr.position = new Vector3(pos.x, pos.y, 0f); p.vel = vel; p.life = life; p.max = life; p.grav = grav; p.drag = drag; p.sway = sway;
            p.phase = Random.value * 6.28f;
        }

        public void Burst(string sprite, Vector2 pos, int n, float speed, float life, float grav, Color col)
        {
            for (int i = 0; i < n; i++)
            {
                float a = Random.value * 6.2832f;
                Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.8f + 0.5f) * speed * Random.Range(0.4f, 1f);
                Emit(sprite, pos + Random.insideUnitCircle * 0.2f, v, life * Random.Range(0.6f, 1f), grav, 2f, 0f, col, 9000);
            }
        }

        public void Sparkles(Vector2 pos, int n, float radius)
        {
            for (int i = 0; i < n; i++)
            {
                string s = Random.value < 0.4f ? "sparkle_0" : "sparkle_1";
                Emit(s, pos + Random.insideUnitCircle * radius, new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.4f, 1.3f)), Random.Range(0.4f, 0.9f), 0f, 1f, 0f, Color.white, 9100);
            }
        }

        public void Leaves(Vector2 pos, int n, string tint)
        {
            for (int i = 0; i < n; i++)
            {
                string s = "leafbit_" + tint + "_" + Random.Range(0, 2);
                Vector2 v = new Vector2(Random.Range(-1.6f, 1.6f), Random.Range(1.2f, 3.2f));
                Emit(s, pos + Random.insideUnitCircle * 0.5f, v, Random.Range(0.8f, 1.5f), -4.5f, 1.5f, 1.2f, Color.white, 9000);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < pool.Count; i++)
            {
                P p = pool[i];
                if (!p.on) continue;
                p.life -= dt;
                if (p.life <= 0f) { p.on = false; p.sr.enabled = false; continue; }
                p.vel.y += p.grav * dt;
                p.vel *= Mathf.Clamp01(1f - p.drag * dt);
                if (p.grav < 0f && p.vel.y < -1.4f) p.vel.y = -1.4f;          // leaves flutter, they don't plummet
                Vector3 pos = p.tr.position;
                pos.x += (p.vel.x + Mathf.Sin(Time.time * 5f + p.phase) * p.sway) * dt;
                pos.y += p.vel.y * dt;
                p.tr.position = pos;
                float a = Mathf.Clamp01(p.life / (p.max * 0.35f));
                p.sr.color = new Color(p.col.r, p.col.g, p.col.b, p.col.a * a);
            }
        }
    }
}
