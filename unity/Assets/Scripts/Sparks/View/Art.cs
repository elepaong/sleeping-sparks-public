// 그림 불러오기. 그림은 임포트 설정의 영향을 받지 않도록 PNG 바이트(Resources/Sparks/assets/…​.bytes)로 들어 있고,
// 실행할 때 Point 필터로 읽는다. 1픽셀 = 1유닛.
using System.Collections.Generic;
using UnityEngine;

namespace Sparks
{
    public static class Art
    {
        static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static Texture2D white, disc, glow;
        public static Material Mat;

        public static void Init()
        {
            if (Mat == null)
            {
                Shader sh = Shader.Find("Sprites/Default");
                Mat = sh != null ? new Material(sh) : null;
            }
        }

        /// <summary>"assets/props/x.png" → 그림. 없으면 null.</summary>
        public static Texture2D Tex(string path, bool smooth = false)
        {
            if (string.IsNullOrEmpty(path)) return null;
            Texture2D t;
            if (textures.TryGetValue(path, out t) && t != null) return t;
            string res = "Sparks/" + (path.EndsWith(".png") ? path.Substring(0, path.Length - 4) : path);
            TextAsset ta = Resources.Load<TextAsset>(res);
            if (ta == null) { textures[path] = null; return null; }
            t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(ta.bytes);
            t.filterMode = smooth ? FilterMode.Bilinear : FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            textures[path] = t;
            return t;
        }

        /// <summary>그림 한 장 전체. 기준점은 왼쪽 위.</summary>
        public static Sprite Whole(string path)
        {
            Sprite s;
            if (sprites.TryGetValue(path, out s) && s != null) return s;
            Texture2D t = Tex(path);
            if (t == null) return null;
            s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0f, 1f), 1f, 0, SpriteMeshType.FullRect);
            sprites[path] = s;
            return s;
        }

        static readonly Dictionary<string, Sprite[]> strips = new Dictionary<string, Sprite[]>();
        /// <summary>가로로 frames칸을 이은 시트의 i번째 칸. 기준점은 왼쪽 위 (Whole과 같다).</summary>
        public static Sprite Strip(string path, int frames, int i)
        {
            Sprite[] a;
            if (!strips.TryGetValue(path, out a) || a == null)
            {
                Texture2D t = Tex(path);
                if (t == null) return null;
                int fw = t.width / frames;
                a = new Sprite[frames];
                for (int k = 0; k < frames; k++) a[k] = Frame(t, k, 0, fw, t.height, new Vector2(0f, 1f));
                strips[path] = a;
            }
            return a[i % a.Length];
        }

        /// <summary>시트의 한 칸. col·row는 왼쪽 위부터 센다.</summary>
        public static Sprite Frame(Texture2D t, int col, int row, int fw, int fh, Vector2 pivot)
        {
            return Sprite.Create(t, new Rect(col * fw, t.height - (row + 1) * fh, fw, fh), pivot, 1f, 0, SpriteMeshType.FullRect);
        }

        public static Texture2D White
        {
            get
            {
                if (white == null)
                {
                    white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    white.SetPixel(0, 0, Color.white); white.Apply();
                    white.filterMode = FilterMode.Point;
                }
                return white;
            }
        }

        static Texture2D Radial(int size, bool soft)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / (size / 2f);
                    float a = soft ? Mathf.Clamp01(1f - d) : (d <= 1f ? 1f : 0f);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            t.Apply();
            t.filterMode = soft ? FilterMode.Bilinear : FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }

        static Sprite whiteSprite, discSprite, glowSprite;
        /// <summary>1×1 흰 점 (빛줄기), 지름 32 원반 (그림자), 지름 64 번지는 빛 (스파크 바닥 빛). 기준점은 가운데.</summary>
        public static Sprite WhiteSprite { get { if (whiteSprite == null) whiteSprite = Sprite.Create(White, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect); return whiteSprite; } }
        public static Sprite DiscSprite { get { if (discSprite == null) { disc = Radial(32, false); discSprite = Sprite.Create(disc, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect); } return discSprite; } }
        public static Sprite GlowSprite { get { if (glowSprite == null) { glow = Radial(64, true); glowSprite = Sprite.Create(glow, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect); } return glowSprite; } }

        public static SpriteRenderer Make(string name, Sprite sprite, Transform parent, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (Mat != null) sr.sharedMaterial = Mat;
            return sr;
        }

        public static Color Hex(string s, Color fallback)
        {
            Color c;
            if (!string.IsNullOrEmpty(s) && ColorUtility.TryParseHtmlString(s, out c)) return c;
            return fallback;
        }
    }
}
