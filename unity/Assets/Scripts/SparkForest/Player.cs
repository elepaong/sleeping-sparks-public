// The little witch: walking, the wand's capture light, interacting, and two gentle spells.
using UnityEngine;

namespace SparkForest
{
    public class Player : MonoBehaviour
    {
        public Vector2 pos;
        public Vector2 face = Vector2.down;
        public bool beaming;
        public Thing target;
        public float abilityCd;
        Vector2 fpos; SpriteRenderer follower;
        public Vector2 FollowerPos { get { return fpos; } }
        public const float BeamRange = 4.0f;

        string dir = "down"; bool flip; float walkT; bool moving; float stepT;
        SpriteRenderer sr, shadow; SpriteRenderer[] rings; SpriteRenderer wandGlow;

        public Vector2 Chest { get { return pos + new Vector2(0f, 0.55f); } }

        void Awake()
        {
            sr = Art.MakeWorld("body", "witch_down_0", Vector2.zero, 0, transform, 0.7f);
            shadow = Art.Make("shadow", "shadow", Vector2.zero, -19000, transform);
            rings = new SpriteRenderer[4];
            for (int i = 0; i < rings.Length; i++) { rings[i] = Art.Make("ring", "ring", Vector2.zero, 9300, transform); rings[i].enabled = false; }
            wandGlow = Art.Make("glow", "glow", Vector2.zero, 9290, transform); wandGlow.enabled = false;
            follower = Art.MakeWorld("companion", "comp_glow", Vector2.zero, 0, transform, 0.35f); follower.enabled = false;
        }

        void Update()
        {
            Game g = Game.I; float dt = Time.deltaTime;
            abilityCd -= dt;
            Vector2 mv = Vector2.zero; beaming = false; target = null;
            if (!g.Busy)
            {
                mv = In.Move();
                beaming = In.Held(Btn.Capture);
                float speed = beaming ? 2.7f : 4.6f;
                Vector2 c = pos + new Vector2(0f, 0.14f);
                float dx = mv.x * speed * dt, dy = mv.y * speed * dt;
                if (dx != 0f && !g.Blocked(c + new Vector2(dx, 0f), 0.28f, 0.16f)) { pos.x += dx; c.x += dx; }
                if (dy != 0f && !g.Blocked(c + new Vector2(0f, dy), 0.28f, 0.16f)) pos.y += dy;
                if (mv.sqrMagnitude > 0.01f)
                {
                    face = mv.normalized;
                    if (Mathf.Abs(mv.x) > Mathf.Abs(mv.y) + 0.01f) { dir = "side"; flip = mv.x < 0f; }
                    else dir = mv.y > 0f ? "up" : "down";
                }

                target = FindTarget();
                if (In.Down(Btn.Interact) && Time.frameCount > g.closedFrame && target != null) target.Interact();
                if (In.Down(Btn.Swap)) g.Swap();
                if (In.Down(Btn.Ability) && g.comps.Count > 0 && abilityCd <= 0f) { abilityCd = 1.1f; g.UseAbility(); }
            }

            moving = mv.sqrMagnitude > 0.01f;
            walkT = moving ? walkT + dt * (beaming ? 6f : 9f) : 0f;
            int frame = moving ? ((int)walkT % 4) : 0;
            if (moving)
            {
                stepT -= dt;
                if (stepT <= 0f) { stepT = 0.28f; Fx.I.Emit("puff_0", pos + new Vector2(0f, 0.05f), new Vector2(-face.x * 0.4f, 0.3f), 0.3f, 0f, 2f, 0f, new Color(1f, 1f, 1f, 0.6f), Art.Order(pos.y) - 1); }
            }
            sr.sprite = Art.S("witch_" + dir + "_" + frame);
            sr.flipX = flip;
            sr.sortingOrder = Art.Order(pos.y);
            transform.position = new Vector3(pos.x, pos.y, 0f);
            shadow.transform.localPosition = new Vector3(0f, 0.02f, 0f);

            // the active companion trots along behind the witch
            if (g.comps.Count > 0)
            {
                if (!follower.enabled) { follower.enabled = true; fpos = pos - face * 0.9f; }
                Vector2 want = pos - face * 0.95f + new Vector2(0f, 0.05f);
                fpos = Vector2.Lerp(fpos, want, 1f - Mathf.Exp(-6f * dt));
                float hop = Mathf.Abs(Mathf.Sin(Time.time * (moving ? 11f : 3f))) * (moving ? 0.16f : 0.05f);
                if (g.Active == "wind") hop += 0.45f;                       // the bird flutters at shoulder height
                follower.sprite = Art.S("comp_" + g.Active);
                follower.transform.position = new Vector3(fpos.x, fpos.y + hop, 0f);
                follower.flipX = (pos.x - fpos.x) < 0f;
                follower.sortingOrder = Art.Order(fpos.y);
            }

            // the wand's gathering light: rings drifting back toward the witch
            float tt = Time.time * 1.3f;
            for (int i = 0; i < rings.Length; i++)
            {
                rings[i].enabled = beaming;
                if (!beaming) continue;
                float f = 1f - ((tt + i / (float)rings.Length) % 1f);
                Vector2 p = Chest + face * (0.5f + f * (BeamRange - 0.6f));
                rings[i].transform.position = new Vector3(p.x, p.y, 0f);
                float s = 0.45f + f * 0.9f;
                rings[i].transform.localScale = new Vector3(s, s, 1f);
                rings[i].color = new Color(1f, 1f, 1f, 0.25f + 0.6f * (1f - f));
            }
            wandGlow.enabled = beaming;
            if (beaming)
            {
                Vector2 p = Chest + face * 0.5f;
                wandGlow.transform.position = new Vector3(p.x, p.y, 0f);
                float s = 0.8f + 0.15f * Mathf.Sin(Time.time * 12f);
                wandGlow.transform.localScale = new Vector3(s, s, 1f);
            }
        }

        Thing FindTarget()
        {
            Thing best = null; float bd = 1.25f;
            foreach (Thing t in Game.I.things)
            {
                if (!t.CanInteract) continue;
                float dx = Mathf.Max(0f, Mathf.Abs(t.basePos.x - pos.x) - t.reach);
                float dy = (t.basePos.y + 0.2f) - pos.y;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d < bd) { bd = d; best = t; }
            }
            return best;
        }
    }
}
