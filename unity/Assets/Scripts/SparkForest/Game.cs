// Bootstrap + world state. The whole game builds itself at runtime: open any scene and press Play.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SparkForest
{
    [Serializable] public class ObjData { public string k; public string s; public string id; public string t; public float x; public float y; public float bw; public float bh; public int n; public int need; public int f; }
    [Serializable] public class AreaData { public string id; public string name; public float x; public float y; public float rx; public float ry; }
    [Serializable] public class MapData { public int w; public int h; public int gw; public int gh; public float cell; public float startX; public float startY; public string[] solid; public string[] water; public ObjData[] objects; public AreaData[] areas; }

    public class Game : MonoBehaviour
    {
        public static Game I;
        /// <summary>Which stage is loaded. Stages are listed in Resources/SparkForest/stages.json.</summary>
        public static string StageId = "";
        public static StageIndex Index;
        static bool resumed;

        public MapData map;
        public Camera cam;
        public Player player;
        public Ambient ambient;
        public readonly List<Thing> things = new List<Thing>();
        public readonly List<Spark> sparks = new List<Spark>();
        bool[,] solid; bool[,] baseSolid;
        public bool[,] water;

        public int light;
        public readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        public bool started, ended;
        /// <summary>Companions that have joined, in order: "glow", "water", "wind". One is active at a time.</summary>
        public readonly List<string> comps = new List<string>();
        public int active;
        public string areaId = "";
        public float playTime;

        public bool dialogOn; public string dialogWho = ""; public string dialogText = ""; public float typed;
        readonly Queue<string> queue = new Queue<string>();
        int openFrame = -1; public int closedFrame = -1;
        public string toast = ""; public float toastT;

        Transform root; Vector2 camPos; public int zoom = 4;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            // 숲 프로토타입은 Assets/Resources/boot.json 의 "game"이 "forest"일 때만 뜬다 (기본은 본 게임)
            if (I != null || Sparks.Launch.Which() != "forest") return;
            new GameObject("SparkForest").AddComponent<Game>();
        }

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            if (Index == null)
            {
                TextAsset ix = Resources.Load<TextAsset>("SparkForest/stages");
                Index = ix != null ? JsonUtility.FromJson<StageIndex>(ix.text) : new StageIndex();
                if (Index.stages == null || Index.stages.Length == 0) Index.stages = new[] { "spring_day" };
            }
            if (string.IsNullOrEmpty(StageId)) StageId = string.IsNullOrEmpty(Index.start) ? Index.stages[0] : Index.start;
            Art.Load(StageId);
            Mood.Load(StageId);
            map = JsonUtility.FromJson<MapData>(Resources.Load<TextAsset>("SparkForest/stages/" + StageId + "/map").text);
            solid = new bool[map.gw, map.gh]; baseSolid = new bool[map.gw, map.gh]; water = new bool[map.gw, map.gh];
            for (int j = 0; j < map.gh; j++)
                for (int i = 0; i < map.gw; i++)
                {
                    bool s = map.solid[j][i] == '#';
                    solid[i, j] = s; baseSolid[i, j] = s;
                    water[i, j] = map.water != null && j < map.water.Length && map.water[j][i] == '~';
                }
            foreach (string t in Texts.Types) counts[t] = 0;

            cam = Camera.main;
            if (cam == null)
            {
                GameObject cg = new GameObject("Main Camera"); cg.tag = "MainCamera";
                cam = cg.AddComponent<Camera>();
            }
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Mood.Bg;
            cam.transform.rotation = Quaternion.identity;
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 50f;

            root = new GameObject("World").transform; root.SetParent(transform, false);
            new GameObject("Fx").AddComponent<Fx>().transform.SetParent(transform, false);
            if (Sfx.I == null) new GameObject("Sfx").AddComponent<Sfx>();      // survives stage changes
            ambient = new GameObject("Ambient").AddComponent<Ambient>(); ambient.transform.SetParent(transform, false);
            gameObject.AddComponent<Hud>();

            GameObject gg = new GameObject("Ground"); gg.transform.SetParent(root, false);
            SpriteRenderer gsr = gg.AddComponent<SpriteRenderer>();
            gsr.sprite = Sprite.Create(Art.Ground, new Rect(0, 0, Art.Ground.width, Art.Ground.height), Vector2.zero, Art.PPU, 0, SpriteMeshType.FullRect);
            gsr.sortingOrder = -30000; if (Art.Mat != null) gsr.sharedMaterial = Art.Mat;
            Mood.Register(gsr, 1f);
            Mood.MakeHaze(transform);

            foreach (ObjData o in map.objects)
            {
                Vector2 p = new Vector2(o.x, o.y);
                switch (o.k)
                {
                    case "deco":
                    case "tree":
                        SpriteRenderer sr = Art.MakeWorld(o.s, o.s, p, o.f == 1 ? -20000 : Art.Order(p.y), root);
                        ambient.RegisterDeco(o.s, sr, o.f == 1);
                        break;
                    case "spark": SpawnSpark(o.t, p, p, false); break;
                    case "shaft": ambient.AddShaft(p); break;
                    default: Thing.Create(o, root); break;
                }
            }

            GameObject pg = new GameObject("Witch"); pg.transform.SetParent(transform, false);
            player = pg.AddComponent<Player>();
            player.pos = new Vector2(map.startX, map.startY);
            camPos = player.pos;
            if (resumed) { started = true; Toast("— " + Mood.Stage.name + " —"); }
        }

        // ------------------------------------------------------------ stages
        /// <summary>Tears the world down and builds another stage. Progress starts over (stage-to-stage carry-over is not designed yet).</summary>
        public static void LoadStage(string id)
        {
            StageId = id; resumed = true;
            if (I != null) { GameObject old = I.gameObject; I = null; old.SetActive(false); Destroy(old); }
            Boot();
        }

        public static void NextStage()
        {
            if (Index == null || Index.stages == null || Index.stages.Length == 0) return;
            int i = Array.IndexOf(Index.stages, StageId);
            LoadStage(Index.stages[(i + 1) % Index.stages.Length]);
        }

        // ------------------------------------------------------------ collision grid
        public bool Blocked(Vector2 c, float hx, float hy)
        {
            int x0 = Mathf.FloorToInt((c.x - hx) / map.cell), x1 = Mathf.FloorToInt((c.x + hx) / map.cell);
            int y0 = Mathf.FloorToInt((c.y - hy) / map.cell), y1 = Mathf.FloorToInt((c.y + hy) / map.cell);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (x < 0 || y < 0 || x >= map.gw || y >= map.gh || solid[x, y]) return true;
            return false;
        }

        /// <summary>mode 0 = block, 1 = restore the original terrain, 2 = force walkable.</summary>
        public void SetRect(float xmin, float ymin, float xmax, float ymax, int mode)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(xmin / map.cell)), x1 = Mathf.Min(map.gw - 1, Mathf.CeilToInt(xmax / map.cell) - 1);
            int y0 = Mathf.Max(0, Mathf.FloorToInt(ymin / map.cell)), y1 = Mathf.Min(map.gh - 1, Mathf.CeilToInt(ymax / map.cell) - 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    solid[x, y] = mode == 0 ? true : (mode == 1 ? baseSolid[x, y] : false);
        }

        public Vector2 FreeSpotNear(Vector2 c, float rmin, float rmax)
        {
            for (int i = 0; i < 24; i++)
            {
                float a = UnityEngine.Random.value * 6.2832f, r = UnityEngine.Random.Range(rmin, rmax);
                Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.8f) * r;
                if (!Blocked(p, 0.3f, 0.25f)) return p;
            }
            return player != null ? player.pos : c;
        }

        // ------------------------------------------------------------ sparks & light
        public Spark SpawnSpark(string type, Vector2 from, Vector2 to, bool hop)
        {
            GameObject g = new GameObject("Spark_" + type); g.transform.SetParent(root, false);
            Spark s = g.AddComponent<Spark>();
            s.Init(type, from, to, hop);
            sparks.Add(s);
            return s;
        }

        public void ReleaseSparks(string type, int n, Vector2 from)
        {
            for (int i = 0; i < n; i++) SpawnSpark(type, from, FreeSpotNear(from, 1.0f, 2.3f), true);
            if (n > 0) Sfx.Play("pop", 0.8f, 0.2f);
        }

        public void AddLight(string type, Vector2 pos)
        {
            int v = type == "moon" ? 3 : 1;
            int before = light;
            light += v; counts[type] = counts[type] + 1;
            Fx.I.Sparkles(pos, 6, 0.35f);
            Sfx.Play(v > 1 ? "catch3" : "catch", 0.8f, 0.08f);
            foreach (Thing t in things)
                if (!t.done && (t.kind == "gate" || t.kind == "greattree") && before < t.d.need && light >= t.d.need)
                    Toast(t.kind == "gate" ? "빛이 " + t.d.need + " 모였어요! 등불문이 반응해요." : "빛이 " + t.d.need + " 모였어요! 큰나무에게 가 봐요.");
        }

        public int NextGoal()
        {
            int best = 0;
            foreach (Thing t in things)
                if (!t.done && (t.kind == "gate" || t.kind == "greattree") && (best == 0 || t.d.need < best)) best = t.d.need;
            return best;
        }

        // ------------------------------------------------------------ companions
        public bool Has(string ab) { return comps.Contains(ab); }
        public string Active { get { return comps.Count > 0 ? comps[active] : ""; } }

        public void AddComp(string id)
        {
            if (!comps.Contains(id)) comps.Add(id);
            active = comps.IndexOf(id);
        }

        public void Swap()
        {
            if (comps.Count < 2) return;
            active = (active + 1) % comps.Count;
            Sfx.Play("pop", 0.7f, 0.1f);
            Fx.I.Emit("puff_1", player.FollowerPos + new Vector2(0f, 0.3f), Vector2.zero, 0.25f, 0f, 0f, 0f, Color.white, 9000);
        }

        public void UseAbility()
        {
            switch (Active)
            {
                case "wind": CastWind(player.Chest, player.face); break;
                case "water": CastWater(player.pos + player.face * 1.8f); break;
                case "glow": CastGlow(player.Chest); break;
            }
        }

        public int LampsLit()
        {
            int n = 0;
            foreach (Thing t in things) if (t.kind == "lamp" && t.stage >= 3) n++;
            return n;
        }

        public void CastGlow(Vector2 center)
        {
            Sfx.Play("catch3", 0.8f, 0.05f);
            Fx.I.Emit("glow", center, Vector2.zero, 0.6f, 0f, 0f, 0f, new Color(1f, 0.97f, 0.8f, 0.9f), 9250);
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * 6.2832f;
                Fx.I.Emit("sparkle_1", center, new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.8f) * 4.2f, 0.55f, 0f, 1.5f, 0f, Color.white, 9260);
            }
            foreach (Thing t in things.ToArray())
                if ((t.Center - center).magnitude < 2.9f) t.OnAbility("glow");
        }

        // ------------------------------------------------------------ abilities
        public void CastWind(Vector2 origin, Vector2 dir)
        {
            Sfx.Play("gust");
            Vector2 side = new Vector2(-dir.y, dir.x);
            for (int i = 0; i < 7; i++)
                Fx.I.Emit("gust", origin + dir * (0.4f + i * 0.35f) + side * UnityEngine.Random.Range(-0.9f, 0.9f), dir * UnityEngine.Random.Range(6f, 9f), 0.6f, 0f, 1.2f, 0f, Color.white, 9200);
            Fx.I.Leaves(origin + dir, 6, "mint");
            foreach (Thing t in things.ToArray())
            {
                Vector2 p = t.Center - origin; float along = Vector2.Dot(p, dir), perp = Mathf.Abs(Vector2.Dot(p, side));
                if (along > -0.6f && along < 5.6f && perp < 1.9f) t.OnAbility("wind");
            }
            foreach (Spark s in sparks)
            {
                Vector2 p = s.pos - origin; float along = Vector2.Dot(p, dir), perp = Mathf.Abs(Vector2.Dot(p, side));
                if (along > -0.3f && along < 5.6f && perp < 1.6f) s.Blow(dir);
            }
        }

        public void CastWater(Vector2 center)
        {
            Sfx.Play("rain");
            for (int i = 0; i < 34; i++)
            {
                Vector2 p = center + UnityEngine.Random.insideUnitCircle * 2.3f;
                Fx.I.Emit("drop", p + new Vector2(0.6f, UnityEngine.Random.Range(2.2f, 4.5f)), new Vector2(-1.2f, -6.5f), UnityEngine.Random.Range(0.35f, 0.7f), 0f, 0f, 0f, Color.white, 9200);
            }
            Fx.I.Sparkles(center, 10, 2.2f);
            foreach (Thing t in things.ToArray())
                if ((t.Center - center).magnitude < 2.6f) t.OnAbility("water");
        }

        // ------------------------------------------------------------ dialogue
        /// <summary>Each line is "speaker|text".</summary>
        public void Say(params string[] lines)
        {
            foreach (string l in lines) queue.Enqueue(l);
            if (!dialogOn) { openFrame = Time.frameCount; NextLine(); }
        }

        void NextLine()
        {
            if (queue.Count == 0) { dialogOn = false; closedFrame = Time.frameCount; return; }
            string l = queue.Dequeue();
            int bar = l.IndexOf('|');
            dialogWho = bar >= 0 ? l.Substring(0, bar) : "";
            dialogText = bar >= 0 ? l.Substring(bar + 1) : l;
            typed = 0f; dialogOn = true;
        }

        public bool Busy { get { return !started || dialogOn; } }

        public void Toast(string s) { toast = s; toastT = 3.5f; }

        public void Ending()
        {
            ended = true;
            ambient.LightLanterns(Vector2.zero, 9999f);
            Sfx.Play("awake");
            Say("|큰나무가 천천히 눈을 떴어요. 가지마다 꽃이 피고, 숲 가득 따스한 빛이 번져요.",
                "모찌|해냈구냥, 루미! 포포도 퐁이도 후우도, 다 같이 애썼어.",
                "|— 숲이 깨어났어요 —\n남은 스파크를 찾아 조금 더 산책해도 좋아요.");
        }

        void Update()
        {
            if (In.Down(Btn.Stage)) { NextStage(); return; }
            if (!started)
            {
                if (Time.timeSinceLevelLoad > 0.6f && In.Down(Btn.Any)) { started = true; Say(Texts.Intro); }
                return;
            }
            playTime += Time.deltaTime;
            if (map.areas != null)
                foreach (AreaData a in map.areas)
                {
                    float nx = (player.pos.x - a.x) / a.rx, ny = (player.pos.y - a.y) / a.ry;
                    if (nx * nx + ny * ny < 0.8f && a.id != areaId)
                    {
                        bool first = areaId.Length == 0;
                        areaId = a.id;
                        Mood.SetArea(a.id);
                        if (!first && !dialogOn) Toast("— " + a.name + " —");
                    }
                }
            if (toastT > 0f) toastT -= Time.deltaTime;
            if (dialogOn)
            {
                float before = typed;
                typed += Time.deltaTime * 38f;
                if ((int)(before / 3f) != (int)(typed / 3f) && typed < dialogText.Length) Sfx.Play("talk", 0.35f, 0.1f);
                if (Time.frameCount > openFrame && (In.Down(Btn.Interact) || In.Down(Btn.Capture)))
                {
                    if (typed < dialogText.Length) typed = dialogText.Length;
                    else NextLine();
                }
            }
        }

        void LateUpdate()
        {
            zoom = Mathf.Max(1, Mathf.RoundToInt(Screen.height / 200f));
            cam.orthographicSize = Screen.height / (2f * Art.PPU * zoom);
            float hh = cam.orthographicSize, hw = hh * cam.aspect;
            Vector2 target = player.pos + new Vector2(0f, 0.7f) + player.face * 0.6f;
            camPos = Vector2.Lerp(camPos, target, 1f - Mathf.Exp(-5f * Time.deltaTime));
            float x = map.w > hw * 2f ? Mathf.Clamp(camPos.x, hw, map.w - hw) : map.w * 0.5f;
            float y = map.h > hh * 2f ? Mathf.Clamp(camPos.y, hh, map.h - hh) : map.h * 0.5f;
            float unit = 1f / (Art.PPU * zoom);
            x = Mathf.Round(x / unit) * unit; y = Mathf.Round(y / unit) * unit;
            cam.transform.position = new Vector3(x, y, -10f);
            Mood.Tick(Time.deltaTime, cam);
        }
    }
}
