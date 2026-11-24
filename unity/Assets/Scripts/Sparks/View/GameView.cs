// 본 게임의 화면: 규칙(Sparks.Core.Game)을 만들고, 그 상태를 읽어 지역을 그리고, 입력을 규칙에 넘긴다.
// 씬 설정은 필요 없다 — 아무 씬에서 Play 하면 스스로 조립한다. 내용은 전부 Resources/Sparks/data 에서 온다.
using System.Collections.Generic;
using UnityEngine;
using Sparks.Core;

namespace Sparks
{
    public class GameView : MonoBehaviour
    {
        public static GameView I;
        public Game game;
        public Camera cam;
        public bool debugView;
        /// <summary>화면 한 픽셀이 실제 몇 픽셀인가, 그리고 640×360 화면이 놓인 자리 (왼쪽 아래 기준)</summary>
        public float scale = 1f; public Rect viewRect;

        const int FootMargin = 3;          // 시트 칸 맨 아래에서 발끝 줄까지 (chickxel.md 루카 프롬프트)
        const float SparkBase = 24f;      // 그림자·바닥 빛 크기를 정한 옛 스파크 칸 크기. 칸 크기는 시트 높이에서 읽는다
        static readonly Color ShadowColor = new Color(0.05f, 0.03f, 0.1f, 0.5f);

        class ObjView { public FieldObject o; public SpriteRenderer sr; public int flatOrder; }
        class SparkView { public Transform tr; public SpriteRenderer sr; public Sprite[][] idle, burst; public float burstT = -1f; }

        Transform zoneRoot, beamRoot;
        /// <summary>햇빛 속 먼지 한 알: 햇빛 네모 안을 천천히 떠오르고, 위로 나가면 아래에서 다시</summary>
        class Mote { public SpriteRenderer sr; public RectF area; public float x, y, speed, phase; }
        readonly List<Mote> motes = new List<Mote>();
        readonly List<ObjView> objViews = new List<ObjView>();
        readonly Dictionary<string, SparkView> sparkViews = new Dictionary<string, SparkView>();
        Transform playerTr; SpriteRenderer playerSr; Sprite[][] walk, idle;
        Color ambient = Color.white;
        int loadSerial = -1, beamSerial = -1;
        float animT;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null || Launch.Which() != "sparks") return;
            new GameObject("Sparks").AddComponent<GameView>();
        }

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            Art.Init();

            // 무엇이 들어 있는지는 tools/unity/sync.py 가 적어 둔 목록에서 읽는다
            TextAsset ix = Resources.Load<TextAsset>("Sparks/index");
            object index = ix != null ? Json.Parse(ix.text) : null;
            if (index == null) Debug.LogError("[Sparks] Resources/Sparks/index.json 없음 — python3 tools/unity/sync.py 를 돌려 주세요");
            var files = new List<string>();
            foreach (object f in J.List(index, "data")) files.Add(J.Text(f));
            var sizes = J.Dict(index, "assets");
            var data = new GameData(ReadData, files, sizes.Keys, p =>
            {
                var l = J.AsList(J.Get(sizes, p));
                return l.Count == 2 ? new[] { (int)J.Num(l[0]), (int)J.Num(l[1]) } : null;
            });
            foreach (string e in data.errors) Debug.LogError("[Sparks] " + e);

            cam = Camera.main;
            if (cam == null)
            {
                var cg = new GameObject("Main Camera"); cg.tag = "MainCamera";
                cam = cg.AddComponent<Camera>();
            }
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            cam.orthographic = true; cam.orthographicSize = Config.ViewH / 2f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            cam.transform.rotation = Quaternion.identity;
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 50f;
            // 화면 비율이 16:9가 아니면 남는 자리를 검게 칠할 카메라
            var bg = new GameObject("Letterbox").AddComponent<Camera>();
            bg.transform.SetParent(transform, false);
            bg.depth = cam.depth - 10f; bg.clearFlags = CameraClearFlags.SolidColor; bg.backgroundColor = Color.black; bg.cullingMask = 0;

            zoneRoot = new GameObject("Zone").transform; zoneRoot.SetParent(transform, false);
            gameObject.AddComponent<Ui>();
            game = new Game(data);
            game.Restart();
            Layout();
            Sync(0f);
        }

        static string ReadData(string path)
        {
            string res = "Sparks/" + (path.EndsWith(".json") ? path.Substring(0, path.Length - 5) : path);
            TextAsset ta = Resources.Load<TextAsset>(res);
            return ta != null ? ta.text : null;
        }

        /// <summary>640×360 화면을 창 가운데에 정수 배로 놓는다 (창이 그보다 작으면 줄인다).</summary>
        void Layout()
        {
            float s = Mathf.Min(Screen.width / (float)Config.ViewW, Screen.height / (float)Config.ViewH);
            if (s >= 1f) s = Mathf.Floor(s);
            scale = Mathf.Max(0.1f, s);
            float w = Config.ViewW * scale, h = Config.ViewH * scale;
            viewRect = new Rect(Mathf.Floor((Screen.width - w) / 2f), Mathf.Floor((Screen.height - h) / 2f), w, h);
            cam.pixelRect = viewRect;
            cam.orthographicSize = Config.ViewH / 2f;
        }

        /// <summary>마우스가 가리키는 맵 좌표 (카메라가 움직여도 맞게)</summary>
        public Vector2 MouseMap()
        {
            Vector2 m = Keys.MousePos();
            float vx = (m.x - viewRect.x) / scale, vy = Config.ViewH - (m.y - viewRect.y) / scale;
            V2 c = game.zone.CameraCenter();
            return new Vector2(c.x - Config.ViewW / 2f + vx, c.y - Config.ViewH / 2f + vy);
        }

        /// <summary>맵 좌표 → 640×360 화면 좌표 (왼쪽 위가 0, 0)</summary>
        public Vector2 MapToView(float x, float y)
        {
            V2 c = game.zone.CameraCenter();
            return new Vector2(x - (c.x - Config.ViewW / 2f), y - (c.y - Config.ViewH / 2f));
        }

        // ------------------------------------------------------------------ 매 프레임: 입력 → 규칙 → 그리기

        void Update()
        {
            Layout();
            Zone zone = game.zone;
            Vector2 move = Vector2.zero;
            // 한 프레임의 입력은 한 곳에서만 쓴다 (대사를 넘긴 클릭으로 바닥까지 클릭하지 않게)
            if (game.endOpen) { }
            else if (game.DialogueOpen)
            {
                if (Keys.Down(Key.Interact) || Keys.Down(Key.Accept) || Keys.MouseDown(0)) game.DialogueAdvance();
            }
            else if (Keys.Down(Key.Omega)) game.OmegaToggle();
            else if (Keys.Down(Key.Cancel))
            {
                if (game.omegaOpen) game.omegaOpen = false;
                else if (game.puzzle != null) { if (game.puzzle.resultShown) game.PuzzleCloseResult(); else game.PuzzleQuit(); }
                else if (game.craftId != null) game.CraftCancel();
            }
            else if (!game.IsBusy)
            {
                int n = Keys.SparkNumber(), wheel = Keys.Wheel();
                if (Keys.Down(Key.Interact)) zone.InteractNearest();
                else if (Keys.Down(Key.Investigate)) zone.ToggleInvestigate();
                else if (Keys.Down(Key.ResetField)) zone.ResetField();
                else if (Keys.Down(Key.DebugView)) debugView = !debugView;
                else if (wheel != 0) game.state.CycleSpark(wheel);
                else if (Keys.MouseDown(0)) { Vector2 p = MouseMap(); zone.Click(new V2(p.x, p.y)); }
                else if (Keys.MouseDown(1)) { Vector2 p = MouseMap(); zone.RightClick(new V2(p.x, p.y)); }
                else if (n > 0) zone.SparkKey(n);
                move = Keys.Move();
            }
            game.Update(Time.deltaTime, new V2(move.x, move.y));
            Sync(Time.deltaTime);
        }

        static Vector3 World(float x, float y) { return new Vector3(x, -y, 0f); }
        /// <summary>발치가 아래에 있을수록 앞에 그린다. 그림자는 같은 자리의 그림 바로 뒤.</summary>
        static int Order(float y) { return Mathf.RoundToInt(y) * 2 + 1; }

        void Sync(float dt)
        {
            Zone zone = game.zone;
            if (zone.loadSerial != loadSerial) Build();
            animT += dt;

            for (int i = 0; i < objViews.Count; i++)
            {
                ObjView v = objViews[i];
                string path = v.o.SpritePath;
                Sprite sp = null;
                if (v.o.visible && path != "")
                {
                    int frames = v.o.Frames;
                    sp = frames > 1 ? Art.Strip(path, frames, (int)(animT * v.o.Fps) % frames) : Art.Whole(path);
                }
                v.sr.enabled = sp != null;
                if (sp == null) continue;
                v.sr.sprite = sp;
                RectF r = v.o.SpriteRect();
                v.sr.transform.position = World(r.x, r.y);
                v.sr.sortingOrder = v.o.Flat ? v.flatOrder : Order(v.o.pos.y);
            }

            // 루카
            Sprite[][] sheet = zone.moving ? walk : idle;
            // 걷기 한 바퀴(두 걸음) 시간은 프레임 수와 상관없이 같게: 4프레임 = 초당 8, 8프레임 = 초당 16
            float fps = zone.moving ? (sheet != null ? sheet[0].Length : 4) * WalkCyclesPerSec : 2f;
            if (sheet != null)
            {
                Sprite[] row = sheet[zone.playerDir];
                playerSr.sprite = row[(int)(animT * fps) % row.Length];
            }
            playerTr.position = World(zone.playerPos.x, zone.playerPos.y);
            playerSr.sortingOrder = Order(zone.playerPos.y);
            playerTr.GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = Order(zone.playerPos.y) - 1;

            // 스파크
            foreach (string id in zone.followerOrder)
            {
                SparkView sv;
                if (!sparkViews.TryGetValue(id, out sv)) { sv = MakeSpark(id); sparkViews[id] = sv; }
                V2 p = zone.followers[id];
                sv.tr.position = World(p.x, p.y);
                sv.sr.sortingOrder = Order(p.y);
                for (int ci = 0; ci < sv.tr.childCount; ci++)   // 그림자·바닥 빛은 몸 바로 뒤
                {
                    SpriteRenderer csr = sv.tr.GetChild(ci).GetComponent<SpriteRenderer>();
                    if (csr != sv.sr) csr.sortingOrder = Order(p.y) - 1;
                }
                if (sv.burstT >= 0f)
                {
                    sv.burstT += dt;
                    int f = (int)(sv.burstT * 8f);
                    Sprite[] brow = Row(sv.burst, id);
                    if (brow == null || f >= brow.Length) sv.burstT = -1f;
                    else sv.sr.sprite = brow[f];
                }
                Sprite[] irow = Row(sv.idle, id);
                if (sv.burstT < 0f && irow != null) sv.sr.sprite = irow[(int)(animT * 4f) % irow.Length];
            }
            foreach (string id in zone.bursts)
                if (sparkViews.ContainsKey(id) && sparkViews[id].burst != null) sparkViews[id].burstT = 0f;
            zone.bursts.Clear();

            foreach (Mote m in motes)
            {
                m.y -= m.speed * dt;
                if (m.y < m.area.y) m.y += m.area.h;
                float x = m.x + (float)System.Math.Sin(animT * 0.7f + m.phase) * 3f;
                m.sr.transform.position = World(Mathf.Round(x), Mathf.Round(m.y));
            }

            if (zone.beamSerial != beamSerial) BuildBeams();

            V2 c = zone.CameraCenter();
            cam.transform.position = new Vector3(c.x, -c.y, -10f);
        }

        // ------------------------------------------------------------------ 지역을 새로 그린다

        void Build()
        {
            Zone zone = game.zone;
            loadSerial = zone.loadSerial; beamSerial = -1;
            for (int i = zoneRoot.childCount - 1; i >= 0; i--) Destroy(zoneRoot.GetChild(i).gameObject);
            objViews.Clear(); sparkViews.Clear(); motes.Clear();
            ambient = Art.Hex(zone.ambient, Color.white);

            Sprite bg = Art.Whole(J.Str(zone.zone, "background"));
            Art.Make("Background", bg, zoneRoot, -20000).transform.position = World(0f, 0f);
            beamRoot = new GameObject("Beams").transform; beamRoot.SetParent(zoneRoot, false);
            BuildSunlight(zone);

            int flat = -10000;
            foreach (FieldObject o in zone.objectList)
            {
                var v = new ObjView { o = o, flatOrder = flat++ };
                v.sr = Art.Make(o.id, null, zoneRoot, 0);
                v.sr.color = ambient;
                objViews.Add(v);
            }

            // 루카: 칸 크기는 시트 크기에서 읽는다 (가로 cols칸, 세로 8방향). 그림을 바꿔 넣어도 코드를 고치지 않는다.
            walk = Sheet("assets/chars/luca_walk.png");
            idle = Sheet("assets/chars/luca_idle.png");
            playerTr = new GameObject("Luca").transform; playerTr.SetParent(zoneRoot, false);
            Shadow(playerTr, new Vector2(10f, 4f), 0.7f, 2f);   // 두 발을 받치게: 3/4 시점에서 뒤쪽 발은 앞발보다 2~5px 위에 그려진다
            playerSr = Art.Make("Body", idle != null ? idle[0][0] : null, playerTr, 0);
            playerSr.color = ambient;
        }

        /// <summary>창에서 바닥에 떨어지는 햇빛 자락과 그 속의 먼지 (지역 데이터 "sunlight": color, alpha, rects, dust, dust_color)</summary>
        void BuildSunlight(Zone zone)
        {
            object sun = J.Get(zone.zone, "sunlight");
            if (sun == null) return;
            Color c = Art.Hex(J.Str(sun, "color"), Color.white);
            c.a = (float)J.Num(sun, "alpha", 0.2);
            var areas = new List<RectF>();
            Sprite shape = Art.Whole(J.Str(sun, "sprite"));   // 창 모양 그림(흰 모양). 없으면 네모
            foreach (object r in J.List(sun, "rects"))
            {
                var l = J.AsList(r);
                var rf = new RectF(J.At(l, 0), J.At(l, 1), J.At(l, 2), J.At(l, 3));
                areas.Add(rf);
                SpriteRenderer sr = Art.Make("Sun", shape != null ? shape : Art.WhiteSprite, zoneRoot, -19000);   // 배경 바로 위, 사물·인물 아래
                if (shape != null) sr.transform.position = World(rf.x, rf.y);   // 그림 기준점은 왼쪽 위
                else
                {
                    sr.transform.position = World(rf.x + rf.w / 2f, rf.y + rf.h / 2f);
                    sr.transform.localScale = new Vector3(rf.w, rf.h, 1f);
                }
                sr.color = c;
            }
            int n = (int)J.Num(sun, "dust", 0);
            if (n <= 0 || areas.Count == 0) return;
            Color dc = Art.Hex(J.Str(sun, "dust_color"), Color.white);
            var rnd = new System.Random(zone.loadSerial);
            for (int i = 0; i < n; i++)
            {
                RectF a = areas[i % areas.Count];
                // 먼지는 햇빛 기둥 속(바닥 자락에서 위로 dust_height만큼)을 떠다닌다
                float hgt = (float)J.Num(sun, "dust_height", 80);
                var area = new RectF(a.x, a.y - hgt, a.w, a.h + hgt);
                var m = new Mote
                {
                    area = area,
                    x = area.x + (float)rnd.NextDouble() * area.w,
                    y = area.y + (float)rnd.NextDouble() * area.h,
                    speed = 2f + (float)rnd.NextDouble() * 4f,
                    phase = (float)rnd.NextDouble() * 6.28f,
                };
                m.sr = Art.Make("Dust", Art.WhiteSprite, zoneRoot, 30000);   // 공중: 모든 것 위
                Color mc = dc; mc.a = 0.35f + 0.4f * (float)rnd.NextDouble();
                m.sr.color = mc;
                motes.Add(m);
            }
        }

        const float WalkCyclesPerSec = 2f;      // 걷기 한 바퀴(두 걸음)를 초당 몇 번
        const float CharCellAspect = 48f / 72f; // 필드 캐릭터 칸의 가로 ÷ 세로 (CLAUDE.md 에셋 규격)

        /// <summary>세로 8방향 시트 → [방향][프레임]. 칸 높이 = 시트 높이 ÷ 8, 칸 폭 = 높이 × 48/72, 프레임 수 = 시트 폭 ÷ 칸 폭.
        /// 그래서 걷기를 4프레임에서 8프레임으로 바꿔도 코드를 고치지 않는다.</summary>
        Sprite[][] Sheet(string path)
        {
            Texture2D t = Art.Tex(path);
            if (t == null) return null;
            int rows = Zone.DirNames.Length, fh = t.height / rows, fw = Mathf.RoundToInt(fh * CharCellAspect), cols = Mathf.Max(1, t.width / fw);
            var o = new Sprite[rows][];
            for (int r = 0; r < rows; r++)
            {
                o[r] = new Sprite[cols];
                for (int c = 0; c < cols; c++) o[r][c] = Art.Frame(t, c, r, fw, fh, new Vector2(0.5f, FootMargin / (float)fh));   // 발끝이 루카의 위치
            }
            return o;
        }

        /// <summary>발밑 그림자: 위치(발끝)를 가운데로 한 납작한 타원</summary>
        static void Shadow(Transform parent, Vector2 radius, float alpha, float lift = 0f)
        {
            SpriteRenderer s = Art.Make("Shadow", Art.DiscSprite, parent, 0);
            s.transform.localPosition = new Vector3(0f, lift, 0f);   // lift: 위치보다 이만큼 위(맵 픽셀)
            s.transform.localScale = new Vector3(radius.x / 16f, radius.y / 16f, 1f);
            s.color = new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, ShadowColor.a * alpha);
        }

        SparkView MakeSpark(string id)
        {
            var sv = new SparkView();
            sv.tr = new GameObject("Spark_" + id).transform; sv.tr.SetParent(zoneRoot, false);
            Texture2D it = Art.Tex("assets/sparks/" + id + "_idle.png"), bt = Art.Tex("assets/sparks/" + id + "_burst.png");
            float k = (it != null ? it.width / (float)IdleFrames : SparkBase) / SparkBase;   // 칸이 24보다 크면 그림자·바닥 빛도 같은 비율로
            string glow = J.Str(game.data.Spark(id), "glow");
            // 떠 있으니 몸보다 작고 옅은 그림자. 빛나는 스파크는 그 위에 몸 폭만 한 옅은 바닥 빛을 겹친다
            // (어두운 색을 더하는 대신 같은 빛깔을 밝게 해 옅게 덮는다).
            Shadow(sv.tr, new Vector2(4f, 1.5f) * k, 0.5f);
            if (glow != "")
            {
                Color c = Art.Hex(glow, Color.white);
                float m = Mathf.Max(0.05f, Mathf.Max(c.r, Mathf.Max(c.g, c.b)));
                SpriteRenderer g = Art.Make("Glow", Art.GlowSprite, sv.tr, 0);
                g.transform.localScale = new Vector3(15f / 32f * k, 6f / 32f * k, 1f);
                g.color = new Color(c.r / m, c.g / m, c.b / m, Mathf.Clamp01(m * 1.0f));
            }
            if (it != null) sv.idle = Strip(it, IdleFrames);
            if (bt != null) sv.burst = Strip(bt, BurstFrames);
            sv.sr = Art.Make("Body", sv.idle != null ? sv.idle[0][0] : null, sv.tr, 0);
            sv.sr.transform.localPosition = new Vector3(0f, Config.SparkHover, 0f);   // 땅에서 살짝 떠 있게
            sv.sr.color = Color.Lerp(Color.white, ambient, Config.SparkAmbient);
            return sv;
        }

        const int IdleFrames = 4, BurstFrames = 6;   // 스파크 시트의 가로 프레임 수 (CLAUDE.md 에셋 규격)

        /// <summary>스파크 시트 → [줄][프레임]. 칸은 정사각형(가로 ÷ 프레임 수). 줄이 8개면 방향별(DirNames 순서), 1개면 방향 없음.</summary>
        static Sprite[][] Strip(Texture2D t, int frames)
        {
            int f = t.width / frames, rows = Mathf.Max(1, t.height / f);
            var o = new Sprite[rows][];
            for (int r = 0; r < rows; r++)
            {
                o[r] = new Sprite[frames];
                for (int i = 0; i < frames; i++) o[r][i] = Art.Frame(t, i, r, f, f, new Vector2(0.5f, 0f));
            }
            return o;
        }

        /// <summary>스파크가 보는 방향의 줄 (시트가 한 줄이면 그 줄)</summary>
        Sprite[] Row(Sprite[][] sheet, string id)
        {
            if (sheet == null) return null;
            int d;
            if (sheet.Length < Zone.DirNames.Length || !game.zone.followerDir.TryGetValue(id, out d)) d = 0;
            return sheet[d];
        }

        // ------------------------------------------------------------------ 빛줄기

        static Color BeamColor(string status)
        {
            switch (status)
            {
                case "converging": return new Color(1f, 0.75f, 0.3f, 0.95f);
                case "focused": return new Color(1f, 1f, 0.9f, 1f);
                case "spread": return new Color(1f, 0.9f, 0.5f, 0.3f);
                default: return new Color(1f, 0.92f, 0.45f, 0.95f);
            }
        }

        void BuildBeams()
        {
            Zone zone = game.zone;
            beamSerial = zone.beamSerial;
            for (int i = beamRoot.childCount - 1; i >= 0; i--) Destroy(beamRoot.GetChild(i).gameObject);
            foreach (List<BeamStep> cells in zone.beams)
                for (int i = 1; i < cells.Count; i++)
                {
                    string status = cells[i].status;
                    V2 a = zone.CellCenter(cells[i - 1].cell), b = zone.CellCenter(cells[i].cell);
                    Vector3 wa = World(a.x, a.y - Config.BeamHeight), wb = World(b.x, b.y - Config.BeamHeight);
                    SpriteRenderer line = Art.Make("beam", Art.WhiteSprite, beamRoot, 10000);
                    Vector3 d = wb - wa;
                    line.transform.position = (wa + wb) * 0.5f;
                    line.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                    line.transform.localScale = new Vector3(d.magnitude, status == "spread" ? 5f : (status == "focused" ? 3f : 2f), 1f);
                    line.color = BeamColor(status);
                    if (status == "focused")
                    {
                        SpriteRenderer dot = Art.Make("focus", Art.WhiteSprite, beamRoot, 10001);
                        dot.transform.position = wb;
                        dot.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
                        dot.transform.localScale = new Vector3(5.66f, 5.66f, 1f);
                        dot.color = Color.white;
                    }
                }
        }
    }
}
