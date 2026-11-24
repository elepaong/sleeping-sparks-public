// Per-stage look. Every stage ships its own baked art (ground + atlas, painted from data/stages/<id>.json)
// plus a "mood": the colours and atmosphere applied while playing. Nothing about colour is fixed in code.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SparkForest
{
    [Serializable]
    public class MoodData
    {
        public string bg = "#74b490";          // camera clear colour (beyond the map edge)
        public string tint = "#ffffff";        // multiplied over the world: time of day
        public string haze = "#ffffff00";      // #RRGGBBAA veil over the world (mist, dusk glow, night)
        public string shaftColor = "#ffffff"; public float shaftAlpha = 1f; public float shaftAngle = 14f;
        public string moteColor = "#ffffff"; public int moteCount = 42;
        public string glowColor = "#fff2bf"; public float glowAlpha = 0.55f;
        public int butterflies = 9;
        public string[] fall;                  // leafbit tints drifting down; empty = nothing falls
        public float fallMin = 0.35f; public float fallMax = 0.8f;
        public bool lanternsLit;
    }
    [Serializable] public class AreaMood { public string id; public MoodData mood; }
    [Serializable] public class StageData { public string id = ""; public string name = ""; public MoodData mood; public AreaMood[] areas; }
    [Serializable] public class StageIndex { public string start; public string[] stages; }

    public static class Mood
    {
        class W { public SpriteRenderer sr; public float k; }

        public static StageData Stage = new StageData();
        public static MoodData Target = new MoodData();
        /// <summary>Current colours, eased toward the area the witch is standing in.</summary>
        public static Color Bg, Tint = Color.white, Haze, Shaft = Color.white, Mote = Color.white, Glow = Color.white;
        static Color gBg, gTint, gHaze, gShaft, gMote, gGlow, applied;
        static readonly List<W> world = new List<W>();
        static SpriteRenderer hazeSr;

        public static Color Hex(string s, Color fallback)
        {
            Color c;
            if (!string.IsNullOrEmpty(s) && ColorUtility.TryParseHtmlString(s, out c)) return c;
            return fallback;
        }

        public static void Load(string id)
        {
            world.Clear(); hazeSr = null;
            TextAsset ta = Resources.Load<TextAsset>("SparkForest/stages/" + id + "/stage");
            Stage = ta != null ? JsonUtility.FromJson<StageData>(ta.text) : null;
            if (Stage == null) { Debug.LogError("[SparkForest] missing stage: " + id); Stage = new StageData(); }
            if (Stage.mood == null) Stage.mood = new MoodData();
            SetTarget(Stage.mood);
            Bg = gBg; Tint = gTint; Haze = gHaze; Shaft = gShaft; Mote = gMote; Glow = gGlow; applied = gTint;
        }

        static void SetTarget(MoodData m)
        {
            Target = m;
            gBg = Hex(m.bg, new Color(0.455f, 0.706f, 0.565f));
            gTint = Hex(m.tint, Color.white);
            gHaze = Hex(m.haze, new Color(1f, 1f, 1f, 0f));
            gShaft = Hex(m.shaftColor, Color.white); gShaft.a = Mathf.Clamp01(m.shaftAlpha);
            gMote = Hex(m.moteColor, Color.white);
            gGlow = Hex(m.glowColor, new Color(1f, 0.95f, 0.75f)); gGlow.a = Mathf.Clamp01(m.glowAlpha);
        }

        /// <summary>Each area of a stage may override the stage mood; the colours ease over as the witch walks in.</summary>
        public static void SetArea(string areaId)
        {
            MoodData m = Stage.mood;
            if (Stage.areas != null)
                foreach (AreaMood a in Stage.areas)
                    if (a != null && a.id == areaId && a.mood != null) m = a.mood;
            SetTarget(m);
        }

        static Color Mix(float k, float alpha) { Color c = Color.Lerp(Color.white, Tint, k); c.a = alpha; return c; }

        /// <summary>k = how much of the world tint this sprite takes (1 scenery, less for things that glow).</summary>
        public static void Register(SpriteRenderer sr, float k)
        {
            if (sr == null) return;
            W w = null;
            for (int i = 0; i < world.Count; i++) if (world[i].sr == sr) { w = world[i]; break; }
            if (w == null) { w = new W(); w.sr = sr; world.Add(w); }
            w.k = k;
            sr.color = Mix(k, sr.color.a);
        }

        public static void MakeHaze(Transform parent)
        {
            hazeSr = Art.Make("haze", "white", Vector2.zero, 7900, parent);   // over the world, under lantern glows and sun shafts
            hazeSr.color = Haze;
        }

        /// <summary>Call after the camera has moved.</summary>
        public static void Tick(float dt, Camera cam)
        {
            float k = 1f - Mathf.Exp(-1.6f * dt);
            Bg = Color.Lerp(Bg, gBg, k); Tint = Color.Lerp(Tint, gTint, k); Haze = Color.Lerp(Haze, gHaze, k);
            Shaft = Color.Lerp(Shaft, gShaft, k); Mote = Color.Lerp(Mote, gMote, k); Glow = Color.Lerp(Glow, gGlow, k);
            cam.backgroundColor = Bg;
            if (Mathf.Abs(Tint.r - applied.r) + Mathf.Abs(Tint.g - applied.g) + Mathf.Abs(Tint.b - applied.b) > 0.002f)
            {
                applied = Tint;
                for (int i = world.Count - 1; i >= 0; i--)
                {
                    W w = world[i];
                    if (w.sr == null) { world.RemoveAt(i); continue; }
                    w.sr.color = Mix(w.k, w.sr.color.a);
                }
            }
            if (hazeSr != null)
            {
                hazeSr.enabled = Haze.a > 0.004f;
                float hh = cam.orthographicSize + 1f, hw = cam.orthographicSize * cam.aspect + 1f;
                Vector3 cp = cam.transform.position;
                hazeSr.transform.position = new Vector3(cp.x, cp.y, 0f);
                hazeSr.transform.localScale = new Vector3(hw * 2f * Art.PPU, hh * 2f * Art.PPU, 1f);
                hazeSr.color = Haze;
            }
        }
    }
}
