// Loads the pixel-art atlas + ground painting that ship as raw PNG bytes (no import settings needed).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SparkForest
{
    [Serializable] public class SprMeta { public string n; public int x; public int y; public int w; public int h; public float px; public float py; }
    [Serializable] public class AtlasMeta { public SprMeta[] sprites; }

    public static class Art
    {
        public const float PPU = 16f;
        public static Texture2D Atlas;
        public static Texture2D Ground;
        public static Material Mat;
        static Dictionary<string, Sprite> sprites;
        static Dictionary<string, SprMeta> metas;

        static Texture2D LoadTex(string res)
        {
            TextAsset ta = Resources.Load<TextAsset>(res);
            if (ta == null) { Debug.LogError("[SparkForest] missing resource: " + res); return Texture2D.whiteTexture; }
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(ta.bytes);
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }

        /// <summary>Loads one stage's art. Each stage has its own atlas and ground painting, baked from its theme.</summary>
        public static void Load(string stage)
        {
            if (sprites != null) foreach (Sprite old in sprites.Values) if (old != null) UnityEngine.Object.Destroy(old);
            if (Atlas != null && Atlas != Texture2D.whiteTexture) UnityEngine.Object.Destroy(Atlas);
            if (Ground != null && Ground != Texture2D.whiteTexture) UnityEngine.Object.Destroy(Ground);
            sprites = new Dictionary<string, Sprite>();
            metas = new Dictionary<string, SprMeta>();
            string dir = "SparkForest/stages/" + stage + "/";
            Atlas = LoadTex(dir + "atlas_png");
            Ground = LoadTex(dir + "ground_png");
            if (Mat == null)
            {
                Shader sh = Shader.Find("Sprites/Default");
                Mat = sh != null ? new Material(sh) : null;
            }
            TextAsset mt = Resources.Load<TextAsset>(dir + "atlas_meta");
            AtlasMeta am = JsonUtility.FromJson<AtlasMeta>(mt.text);
            foreach (SprMeta m in am.sprites)
            {
                Sprite s = Sprite.Create(Atlas, new Rect(m.x, m.y, m.w, m.h), new Vector2(m.px / m.w, m.py / m.h), PPU, 0, SpriteMeshType.FullRect);
                s.name = m.n;
                sprites[m.n] = s;
                metas[m.n] = m;
            }
        }

        public static bool Has(string n) { return sprites.ContainsKey(n); }

        public static Sprite S(string n)
        {
            Sprite s;
            if (sprites.TryGetValue(n, out s)) return s;
            Debug.LogWarning("[SparkForest] unknown sprite: " + n);
            return sprites["white"];
        }

        public static SprMeta Meta(string n)
        {
            SprMeta m;
            return metas.TryGetValue(n, out m) ? m : metas["white"];
        }

        public static Rect UV(string n)
        {
            SprMeta m = Meta(n);
            return new Rect(m.x / (float)Atlas.width, m.y / (float)Atlas.height, m.w / (float)Atlas.width, m.h / (float)Atlas.height);
        }

        /// <summary>Y-sort: lower on screen draws in front.</summary>
        public static int Order(float y) { return Mathf.RoundToInt(-y * 16f); }

        public static SpriteRenderer Make(string name, string sprite, Vector2 pos, int order, Transform parent)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S(sprite);
            sr.sortingOrder = order;
            if (Mat != null) sr.sharedMaterial = Mat;
            return sr;
        }

        /// <summary>A sprite that belongs to the world and takes the stage's tint (see Mood). k = how strongly.</summary>
        public static SpriteRenderer MakeWorld(string name, string sprite, Vector2 pos, int order, Transform parent, float k = 1f)
        {
            SpriteRenderer sr = Make(name, sprite, pos, order, parent);
            Mood.Register(sr, k);
            return sr;
        }
    }
}
