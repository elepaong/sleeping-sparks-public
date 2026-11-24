// 게임 화면 확인용: 게임 규칙(Core)으로 장면을 만들고, 화면에 그릴 것(배경·사물·루카·스파크·햇빛·이름표)을 JSON으로 적는다.
// Unity 없이 화면을 보기 위한 도구다. 그림은 tools/unity/snapshot_render.py가 GameView와 같은 규칙으로 그린다.
//   bash tools/unity/snapshot.sh  →  design/refs/screens/<장면>.png
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Sparks.Core;

static class Snapshot
{
    static Game g; static Zone zv; static State S;
    const float Dt = 1f / 30f;

    static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        string root = args.Length > 0 ? args[0] : ".", outDir = args.Length > 1 ? args[1] : "snap";
        Directory.CreateDirectory(outDir);
        var files = new List<string>(); var assets = new List<string>();
        foreach (string f in Directory.GetFiles(Path.Combine(root, "data"), "*.json", SearchOption.AllDirectories))
            files.Add(f.Substring(root.Length + 1).Replace('\\', '/'));
        foreach (string f in Directory.GetFiles(Path.Combine(root, "assets"), "*.png", SearchOption.AllDirectories))
            assets.Add(f.Substring(root.Length + 1).Replace('\\', '/'));
        var data = new GameData(
            p => File.Exists(Path.Combine(root, p)) ? File.ReadAllText(Path.Combine(root, p), Encoding.UTF8) : null,
            files, assets, p => PngSize(Path.Combine(root, p)));
        g = new Game(data); zv = g.zone; S = g.state;

        // 1) 시작: 도입 대사를 넘긴 직후, 첫 자리
        g.Restart(); Settle();
        Dump(Path.Combine(outDir, "scriptorium_01_start.json"), 0.0f);

        // 2) 수반: 찌릿을 왼쪽 벽 선반에 놓아 물줄기가 깔때기 쪽으로 휜 모습 (퍼즐 풀이 처리 없이 상태만)
        S.AddSpark("jjirit"); S.SolveDoor("D-STATIC-01"); S.SolveDoor("D-STATIC-02");
        zv.Refresh();
        zv.playerPos = new V2(1440, 262); Face(0, -1);
        S.placed["jjirit"] = "ledge_left";
        zv.objects["stream"].SetState("bent_left");
        zv.Refresh();
        Frames(60);
        Dump(Path.Combine(outDir, "scriptorium_02_basin.json"), 0.4f);

        // 3) 계단이 열린 바닥 돌판과 기둥, 조사 모드(잉크 방울이 보임)
        S.placed.Clear(); zv.objects["stream"].SetState("straight");
        zv.objects["hatch"].SetState("open");
        S.investigate = true; zv.Refresh();
        zv.playerPos = new V2(760, 900); Face(1, -1);
        Frames(60);
        Dump(Path.Combine(outDir, "scriptorium_03_stairs.json"), 0.8f);
        // 4) 창가: 바닥에 떨어진 햇빛 자락과 그 속의 먼지
        S.investigate = false; zv.Refresh();
        zv.playerPos = new V2(470, 372); Face(0, -1);
        Frames(60);
        Dump(Path.Combine(outDir, "scriptorium_04_windows.json"), 0.2f);
        Console.WriteLine("SNAPSHOT OK");
        return 0;
    }

    static void Face(float x, float y)
    {
        // Zone.Face와 같은 방향 계산 (화면 y는 아래가 +)
        int sector = (int)Math.Round(Math.Atan2(y, x) / (Math.PI / 4.0));
        int[] sectorToDir = { 2, 1, 0, 7, 6, 5, 4, 3 };
        zv.playerDir = sectorToDir[((sector % 8) + 8) % 8];
    }

    static void Frames(int n) { for (int i = 0; i < n; i++) g.Update(Dt, new V2(0, 0)); zv.moving = false; }

    static void Settle()
    {
        for (int i = 0; i < 3000; i++)
        {
            g.Update(Dt, new V2(0, 0));
            if (g.DialogueOpen) g.DialogueAdvance();
            else if (!g.running) return;
        }
    }

    static int[] PngSize(string path)
    {
        using (var fs = File.OpenRead(path))
        {
            var b = new byte[24];
            fs.Read(b, 0, 24);
            return new[] { (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19], (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23] };
        }
    }

    // ------------------------------------------------------------------ JSON 쓰기

    static string N(float v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }
    static string Q(string s) { return "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""; }

    static void Dump(string path, float animT)
    {
        var sb = new StringBuilder();
        V2 cam = zv.CameraCenter();
        sb.Append("{\"zone\":" + Q(S.zone) + ",\"zone_name\":" + Q(g.loc.T(J.Str(zv.zone, "name_key"))));
        sb.Append(",\"map\":[" + N(zv.mapW) + "," + N(zv.mapH) + "],\"camera\":[" + N(cam.x) + "," + N(cam.y) + "]");
        sb.Append(",\"view\":[" + Config.ViewW + "," + Config.ViewH + "],\"anim_t\":" + N(animT));
        sb.Append(",\"background\":" + Q(J.Str(zv.zone, "background")) + ",\"ambient\":" + Q(zv.ambient));
        sb.Append(",\"investigate\":" + (S.investigate ? "true" : "false"));
        sb.Append(",\"objects\":[");
        bool first = true;
        foreach (FieldObject o in zv.objectList)
        {
            if (!o.visible || o.SpritePath == "") continue;
            RectF r = o.SpriteRect();
            RectF h = o.Hotspot();
            if (!first) sb.Append(","); first = false;
            sb.Append("{\"id\":" + Q(o.id) + ",\"sprite\":" + Q(o.SpritePath) + ",\"frames\":" + o.Frames + ",\"fps\":" + N(o.Fps));
            sb.Append(",\"rect\":[" + N(r.x) + "," + N(r.y) + "," + N(r.w) + "," + N(r.h) + "],\"flat\":" + (o.Flat ? "true" : "false") + ",\"y\":" + N(o.pos.y));
            sb.Append(",\"label\":" + (o.HasLabel ? Q(g.loc.T(J.Str(o.def, "name_key"))) : "null"));
            sb.Append(",\"label_at\":[" + N(h.x + h.w / 2f) + "," + N(h.y) + "]}");
        }
        sb.Append("],\"player\":{\"pos\":[" + N(zv.playerPos.x) + "," + N(zv.playerPos.y) + "],\"dir\":" + zv.playerDir + ",\"moving\":" + (zv.moving ? "true" : "false") + "}");
        sb.Append(",\"sparks\":[");
        first = true;
        foreach (string id in zv.followerOrder)
        {
            if (!first) sb.Append(","); first = false;
            V2 p = zv.followers[id];
            int sd; zv.followerDir.TryGetValue(id, out sd);
            sb.Append("{\"id\":" + Q(id) + ",\"dir\":" + sd + ",\"pos\":[" + N(p.x) + "," + N(p.y) + "],\"glow\":" + Q(J.Str(g.data.Spark(id), "glow")) + "}");
        }
        sb.Append("]}");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }
}
