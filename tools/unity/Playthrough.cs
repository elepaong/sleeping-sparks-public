// 처음부터 끝까지 자동으로 밟아 보는 시험 (Godot판 scripts/test/playthrough.gd 를 옮긴 것). 정답은 data/에서 읽는다.
// Unity 없이 돈다: 게임 규칙(unity/Assets/Scripts/Sparks/Core)만 컴파일해 실행한다.
//   bash tools/unity/check.sh
// 끝나면 "PLAYTHROUGH OK" 또는 "PLAYTHROUGH FAILED"를 찍고, 종료 코드 0 / 1로 끝난다.
// 실제 게임과 같은 길을 쓴다: 루카가 걸어가서(길찾기) 조사·배치하고, 문 퍼즐 화면의 버튼 함수를 누른다.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sparks.Core;

static class Playthrough
{
    static Game g; static Zone zv; static State S;
    static readonly List<string> failures = new List<string>();
    static string stepName = "";
    const float Dt = 1f / 30f;

    static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        string root = args.Length > 0 ? args[0] : ".";
        var files = new List<string>(); var assets = new List<string>();
        foreach (string f in Directory.GetFiles(Path.Combine(root, "data"), "*.json", SearchOption.AllDirectories))
            files.Add(f.Substring(root.Length + 1).Replace('\\', '/'));
        foreach (string f in Directory.GetFiles(Path.Combine(root, "assets"), "*.png", SearchOption.AllDirectories))
            assets.Add(f.Substring(root.Length + 1).Replace('\\', '/'));
        var data = new GameData(
            p => File.Exists(Path.Combine(root, p)) ? File.ReadAllText(Path.Combine(root, p), Encoding.UTF8) : null,
            files, assets, p => PngSize(Path.Combine(root, p)));
        g = new Game(data); zv = g.zone; S = g.state;
        g.Restart();
        try { Run(); }
        catch (Exception e) { Fail("예외: " + e); }
        if (data.errors.Count > 0) Fail("데이터 오류: " + string.Join(" / ", data.errors.ToArray()));
        if (g.loc.missing.Count > 0) Fail("문자열 키 없음: " + string.Join(", ", new List<string>(g.loc.missing).ToArray()));
        if (failures.Count == 0) { Console.WriteLine("PLAYTHROUGH OK"); return 0; }
        Console.WriteLine("PLAYTHROUGH FAILED (" + failures.Count + ")");
        foreach (string f in failures) Console.WriteLine("  - " + f);
        return 1;
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

    // ------------------------------------------------------------------ 도우미

    static void Step(string text) { stepName = text; Console.WriteLine("[단계] " + text); }
    static void Fail(string msg) { failures.Add(stepName + ": " + msg); Console.WriteLine("  [실패] " + msg); }
    static void Check(bool cond, string msg) { if (!cond) Fail(msg); }
    static void Frames(int n) { for (int i = 0; i < n; i++) g.Update(Dt, new V2(0, 0)); }

    /// 대사는 넘기고, 밀린 행동이 끝나거나 문 퍼즐·제작 창이 뜰 때까지 기다린다.
    static void Settle()
    {
        for (int i = 0; i < 3000; i++)
        {
            Frames(1);
            if (g.DialogueOpen)
            {
                string txt = g.DialogueText;
                Check(!txt.StartsWith("#"), "대사 문장을 찾지 못함: " + txt);
                if (g.DialogueSpeaker != "narrator") Check(g.loc.Has("speaker." + g.DialogueSpeaker), "말하는 이 이름 없음: " + g.DialogueSpeaker);
                g.DialogueAdvance();
            }
            else if (g.puzzle != null || g.craftId != null || g.endOpen) return;
            else if (!g.running) return;
        }
        Fail("행동이 끝나지 않음 (멈춤)");
    }

    /// 키 입력: 화면(View)이 하는 것과 같이, 창이 떠 있으면 필드 키는 먹지 않는다.
    static void Press(string action)
    {
        if (action == "omega") g.OmegaToggle();
        else if (!g.IsBusy)
        {
            if (action == "investigate") zv.ToggleInvestigate();
            else if (action == "reset_field") zv.ResetField();
            else if (action.StartsWith("spark_")) zv.SparkKey(int.Parse(action.Substring(6)));
        }
        Frames(3);
    }

    /// 루카가 사물까지 걸어가서 조사(interact)하거나 스파크를 배치(place)한다.
    static void Act(string kind, string obj, string spark = "")
    {
        if (!zv.objects.ContainsKey(obj)) { Fail("사물 없음: " + obj); return; }
        Check(zv.objects[obj].visible, "사물이 보이지 않음: " + obj);
        if (!zv.CanReach(obj)) { Fail("걸어서 닿을 수 없음: " + obj); return; }
        zv.GoAnd(new FieldAction { kind = kind, obj = obj, spark = spark });
        for (int i = 0; i < 6000; i++)
        {
            if (zv.path.Count == 0 && zv.pending == null) break;
            Frames(1);
        }
        Check(zv.path.Count == 0 && zv.pending == null, "도착하지 못함: " + obj);
        Settle();
    }

    static string St(string obj) { return zv.objects[obj].state; }

    static int IndexOf(List<object> options, object v)
    {
        for (int i = 0; i < options.Count; i++) if (J.Same(options[i], v)) return i;
        return -1;
    }

    /// 떠 있는 문 퍼즐을 푼다. wrong번 틀리고, hints개의 힌트를 본 뒤 정답을 낸다.
    static void Solve(string id, int wrong = 0, int hints = 0)
    {
        if (g.puzzle == null || g.puzzle.id != id) { Fail("문 퍼즐 " + id + "이(가) 뜨지 않음 (지금: " + (g.puzzle != null ? g.puzzle.id : "없음") + ")"); return; }
        object pz = g.data.puzzles[id];
        List<object> options = g.puzzle.Options;
        string status = J.Str(pz, "status");
        Check(status == "reviewed" || status == "verified" || status == "playtested", id + "의 status가 '" + status + "' — 검토를 거치지 않은 문제가 게임에 들어감");
        Check(g.data.HasAsset("assets/puzzles/" + id + ".png"), id + " 그림 없음");
        foreach (string k in new[] { "title", "text", "explanation", "parchment" }) Check(g.loc.T("puzzle." + id + "." + k) != "", id + " " + k + " 비어 있음");
        for (int h = 0; h < hints; h++)
        {
            int before = S.ink, shown = S.DoorRecord(id).hints;
            g.PuzzleTakeHint();
            if (before >= Config.HintCosts[shown])
            {
                Check(S.ink == before - Config.HintCosts[shown], "힌트 값이 잉크에서 빠지지 않음");
                Check(S.DoorRecord(id).hints == shown + 1, "힌트가 열리지 않음");
            }
            else Check(S.ink == before && S.DoorRecord(id).hints == shown, "잉크가 모자란데 힌트가 열림");
        }
        int wrongBefore = S.DoorRecord(id).wrong;
        g.PuzzleSubmit();   // 아무것도 고르지 않고 제출 → 오답으로 치지 않아야 한다
        Check(S.DoorRecord(id).wrong == wrongBefore && !g.puzzle.resultShown, "빈 답을 제출했는데 채점됨");
        bool choice = g.puzzle.kind == "choice";
        object answer = J.Get(pz, "answer");
        for (int w = 0; w < wrong; w++)
        {
            if (choice)
            {
                for (int i = 0; i < options.Count; i++)
                    if (!J.Same(options[i], answer)) { g.PuzzlePick(i); break; }
            }
            else
            {
                var order = J.AsList(answer);
                for (int i = options.Count - 1; i >= 0; i--) g.PuzzlePick(IndexOf(options, order[i]));   // 정답을 거꾸로 놓는다
            }
            g.PuzzleSubmit();
            Check(!g.puzzle.resultShown, "오답인데 정답 처리됨");
        }
        Check(S.DoorRecord(id).wrong == wrongBefore + wrong, "오답 횟수가 맞지 않음");
        if (choice) g.PuzzlePick(IndexOf(options, answer));
        else foreach (object a in J.AsList(answer)) g.PuzzlePick(IndexOf(options, a));
        int energyBefore = S.energy;
        g.PuzzleSubmit();
        Check(g.puzzle.resultShown, "정답인데 해설이 뜨지 않음");
        int totalWrong = S.DoorRecord(id).wrong;
        int expect = (int)Math.Round(J.Num(pz, "energy") * Math.Max(0.5, 1.0 - 0.1 * totalWrong), MidpointRounding.AwayFromZero);
        Check(S.energy == energyBefore + expect, "에너지 " + expect + "를 얻어야 하는데 " + (S.energy - energyBefore));
        g.PuzzleCloseResult();
        Settle();
        Check(S.IsSolved(id), id + " 풀이가 기록되지 않음");
    }

    static void WalkableSpawns()
    {
        foreach (var p in J.Dict(zv.zone, "spawns"))
        {
            var l = J.AsList(p.Value);
            Check(zv.IsWalkable(new V2(J.At(l, 0), J.At(l, 1))), "들어오는 자리 '" + p.Key + "'가 걸을 수 없는 곳");
        }
    }

    // ------------------------------------------------------------------ 처음부터 끝까지

    static void Run()
    {
        Step("프롤로그 시작");
        Settle();
        Check(S.zone == "scriptorium", "시작 지역이 필사실이 아님");
        WalkableSpawns();
        Press("spark_1");
        Check(S.selectedSpark == "", "아직 없는 스파크가 선택됨");

        Step("키보드·마우스 이동");
        V2 before = zv.playerPos;
        for (int i = 0; i < 10; i++) g.Update(Dt, new V2(1, 0));
        Frames(2);
        Check(zv.playerPos.x > before.x + 4f, "오른쪽 키로 움직이지 않음");
        zv.Click(new V2(900, 640));
        Check(zv.path.Count > 0, "바닥을 클릭해도 걸어가지 않음");
        for (int i = 0; i < 2000 && zv.path.Count > 0; i++) Frames(1);
        Check(zv.playerPos.Dist(new V2(900, 640)) < 12f, "클릭한 곳에 도착하지 않음: " + zv.playerPos);

        Step("오메가 문서와 첫 수수께끼");
        Act("interact", "desk");
        Solve("D-STATIC-01", 1);
        Check(S.sparks.Contains("jjirit"), "찌릿을 얻지 못함");

        Step("스파크가 보는 방향");
        bool facedRight = false;
        for (int i = 0; i < 20; i++) { g.Update(Dt, new V2(1, 0)); if (zv.followerDir["jjirit"] == 2) facedRight = true; }
        Check(facedRight, "오른쪽으로 걸을 때 찌릿이 오른쪽을 보지 않음");
        Frames(60);
        Check(zv.followerDir["jjirit"] == zv.playerDir, "멈춘 뒤 찌릿이 루카와 같은 쪽을 보지 않음");

        Step("필드 F-STATIC-01: 쇠 열쇠는 반응 없음, 종잇조각은 끌려옴");
        Press("spark_1");
        Check(S.selectedSpark == "jjirit", "1 키로 찌릿이 선택되지 않음");
        Act("place", "grate_key", "jjirit");
        Check(!S.placed.ContainsKey("jjirit"), "반응 없는 곳에 찌릿이 놓인 채로 남음");
        Check(!S.IsSolved("F-STATIC-01"), "열쇠 창살에서 퍼즐이 풀림");
        Act("place", "grate_scrap", "jjirit");
        Check(S.IsSolved("F-STATIC-01"), "F-STATIC-01이 풀리지 않음");
        Check(S.items.Contains("isolde_note"), "쪽지를 얻지 못함");
        Check(!zv.objects["scrap"].visible, "끌어올린 종잇조각이 그대로 보임");

        Step("수반의 수수께끼 (힌트 사용)");
        Check(!zv.objects["ledge_left"].visible, "받침돌이 미리 보임");
        Act("interact", "fountain");
        Solve("D-STATIC-02", 0, 3);   // 잉크 2개로 힌트 2개, 셋째는 잉크가 모자라 열리지 않아야 한다
        Check(S.ink == 0, "잉크가 0이 아님: " + S.ink);
        Check(zv.objects["ledge_left"].visible && zv.objects["ledge_right"].visible, "받침돌이 나타나지 않음");

        Step("필드 F-STATIC-02: 물줄기");
        Act("place", "ledge_right", "jjirit");
        Check(St("stream") == "bent_right", "물줄기가 오른쪽으로 휘지 않음: " + St("stream"));
        Check(!S.IsSolved("F-STATIC-02"), "반대쪽 받침돌에서 퍼즐이 풀림");
        zv.RightClick(zv.objects["ledge_right"].pos);
        Settle();
        Check(!S.placed.ContainsKey("jjirit") && St("stream") == "straight", "회수해도 물줄기가 돌아오지 않음");
        Act("place", "ledge_left", "jjirit");
        Check(S.IsSolved("F-STATIC-02"), "F-STATIC-02가 풀리지 않음");
        Check(St("hatch") == "open", "계단이 열리지 않음");
        Check(!S.placed.ContainsKey("jjirit"), "풀린 뒤 찌릿이 돌아오지 않음");

        Step("조사 모드와 잉크 방울");
        Check(!zv.objects["ink_1"].visible, "조사 모드가 아닌데 잉크가 보임");
        Press("investigate");
        Check(S.investigate && zv.objects["ink_1"].visible, "조사 모드에서 잉크가 보이지 않음");
        foreach (string ink in new[] { "ink_1", "ink_2", "ink_3" }) Act("interact", ink);
        Check(S.ink == 3, "잉크 3개를 줍지 못함: " + S.ink);
        Press("investigate");
        Check(!S.investigate, "조사 모드가 꺼지지 않음");

        Step("오메가 문서");
        Press("omega");
        Check(g.omegaOpen, "Tab으로 오메가 문서가 열리지 않음");
        foreach (string t in Game.OmegaTabs)
            foreach (string line in g.OmegaLines(t)) Check(!line.Contains("#"), "오메가 문서 '" + t + "' 쪽에 찾지 못한 문장이 있음: " + line);
        Press("omega");
        Check(!g.omegaOpen, "오메가 문서가 닫히지 않음");

        Step("지하 서고로");
        Act("interact", "hatch");
        Check(S.zone == "archive", "서고로 가지 못함");
        WalkableSpawns();

        Step("봉인의 문: 문 퍼즐 3개 연속");
        Act("interact", "seal_door");
        Check(g.puzzle != null && g.puzzle.id == "D-LIGHT-01", "첫 고리 문제가 뜨지 않음");
        g.PuzzleQuit();   // 그만두기 → 다음에 같은 자리에서 다시
        Settle();
        Check(!S.sparks.Contains("lumi"), "그만뒀는데 루미를 얻음");
        Act("interact", "seal_door");
        Solve("D-LIGHT-01", 0, 1);
        Solve("D-LIGHT-02", 2);
        Solve("D-LIGHT-03", 0);
        Check(S.sparks.Contains("lumi"), "루미를 얻지 못함");
        Act("interact", "seal_door");
        Check(S.zone == "archive", "빛 문양을 켜기 전에 문이 열림");

        Step("필드 F-LIGHT-01: 거울 세 개");
        Press("spark_2");
        Check(S.selectedSpark == "lumi", "2 키로 루미가 선택되지 않음");
        Act("place", "stand_1", "lumi");
        Check(St("lock_1") == "off", "거울을 돌리기 전에 문양이 켜짐");
        Check(zv.beams.Count == 1 && zv.beams[0].Count > 1, "빛줄기가 나가지 않음");
        Act("interact", "mirror_1");
        Act("interact", "mirror_3");
        Check(St("lock_1") == "lit" && S.IsSolved("F-LIGHT-01"), "F-LIGHT-01이 풀리지 않음");
        Press("reset_field");
        Check(St("mirror_1") == zv.objects["mirror_1"].initialState && !S.placed.ContainsKey("lumi"), "R로 초기화되지 않음");
        Check(St("lock_1") == "lit", "초기화가 이미 켜진 문양을 꺼 버림");

        Step("필드 F-LIGHT-02: 빛을 되돌리기");
        Act("place", "stand_2", "lumi");
        Check(St("lock_2") == "off", "거울을 돌리기 전에 둘째 문양이 켜짐");
        Act("interact", "mirror_4");
        Check(St("lock_2") == "off", "거울 하나만으로 둘째 문양이 켜짐");
        Act("interact", "mirror_5");
        Check(St("lock_2") == "lit" && S.IsSolved("F-LIGHT-02"), "F-LIGHT-02가 풀리지 않음");
        Check(S.flags.Contains("lab_open"), "문이 열리지 않음");

        Step("연구실");
        Act("interact", "seal_door");
        Check(S.zone == "lab", "연구실로 가지 못함");
        Check(S.placed.Count == 0, "방을 나왔는데 스파크가 놓인 채로 남음");
        Check(zv.mapW == 1672 && zv.mapH == 941, "연구실 맵 크기가 배경 그림 크기와 다름: " + zv.mapW + "×" + zv.mapH);
        WalkableSpawns();
        Act("interact", "workbench");   // 도면이 없을 때
        Check(g.craftId == null && S.inventions.Count == 0, "도면 없이 제작 창이 뜸");
        Act("interact", "chest");
        Solve("D-LIGHT-04", 1);
        Act("interact", "workbench");   // 재료가 없을 때
        Check(g.craftId != null && !g.CraftCanMake(), "재료 없이 만들 수 있음");
        foreach (string line in g.CraftLines()) Check(!line.Contains("#"), "제작 창에 찾지 못한 문장이 있음: " + line);
        g.CraftCancel();
        Settle();
        Act("interact", "slot_b");
        Check(St("slot_b") == "empty", "집광기 없이 렌즈가 끼워짐");
        Act("interact", "shelf");
        Check(S.items.Contains("glass_disc"), "유리 원반을 얻지 못함");
        Act("interact", "workbench");
        Check(g.craftId != null && g.CraftCanMake(), "재료를 모았는데 만들 수 없음");
        g.CraftMake();
        Settle();
        Check(S.inventions.Contains("convex_lens"), "집광기를 만들지 못함");

        Step("필드 F-LIGHT-03: 초점에 덩굴 맞추기");
        Act("interact", "passage");
        Check(!S.ended, "덩굴을 태우기 전에 끝남");
        Act("place", "stand_4", "lumi");
        Check(St("vine") == "dry", "거울에 막혀야 할 빛이 덩굴에 닿음");
        Act("place", "stand_3", "lumi");
        Check(St("vine") == "dry", "거울을 돌리기 전에 빛이 덩굴에 닿음");
        // 거울을 돌리기 전: 빛이 왼쪽으로 꺾여 돌기둥(막힌 칸) 앞에서 멈춘다
        Cell last = zv.beams[0][zv.beams[0].Count - 1].cell;
        Check(last.Equals(zv.objects["mirror_6"].cell), "빛이 배경의 돌기둥 앞에서 멈추지 않음: " + last);
        Act("interact", "mirror_6");
        Check(St("vine") == "dry", "거울 하나만 돌렸는데 빛이 덩굴에 닿음");
        Act("interact", "mirror_8");
        Check(St("vine") == "lit_plain", "렌즈 없는 빛: " + St("vine"));
        Act("interact", "slot_c");
        Check(St("vine") == "lit_converging", "렌즈가 너무 가까울 때: " + St("vine"));
        Act("interact", "slot_a");
        Check(St("vine") == "lit_spread" && St("slot_c") == "empty", "렌즈가 너무 멀 때: " + St("vine"));
        Act("interact", "slot_d");
        Check(St("vine") == "lit_spread", "거울 앞 렌즈 틀: " + St("vine"));
        Check(!S.IsSolved("F-LIGHT-03"), "틀린 렌즈 틀에서 풀림");
        Act("interact", "slot_b");
        Check(St("vine") == "burnt" && S.IsSolved("F-LIGHT-03"), "F-LIGHT-03이 풀리지 않음: " + St("vine"));
        Check(!zv.objects["vine"].visible, "탄 덩굴이 그대로 보임");
        Check(zv.objects["arch_open"].visible, "열린 통로 그림이 나타나지 않음");

        Step("끝");
        Act("interact", "passage");
        Check(g.endOpen && S.ended, "끝 화면이 뜨지 않음");
        Check(g.loc.T("ui.end_title") == "프로토타입 끝", "끝 화면 문구가 다름");
        Check(!g.EndStats().Contains("#") && !g.EndStats().Contains("{"), "끝 화면 기록이 채워지지 않음");
        Check(S.fieldSolved.Count == 5, "필드 퍼즐 5개를 모두 풀지 않음");
        CameraCheck();

        Step("처음부터 다시");
        g.Restart();
        Settle();
        Check(S.zone == "scriptorium" && S.sparks.Count == 0 && !g.endOpen && S.ink == Config.StartInk, "다시 시작해도 상태가 남아 있음");
    }

    /// 화면보다 큰 맵: 카메라가 루카를 따라가되 맵 밖을 비추지 않고, 길찾기가 맵 전체에 깔리는지 본다.
    static void CameraCheck()
    {
        Step("카메라: 화면보다 큰 맵");
        float sw = Config.ViewW, sh = Config.ViewH, bw = sw * 2, bh = sh * 2;
        zv.walkPoly = new List<V2> { new V2(8, 8), new V2(bw - 8, 8), new V2(bw - 8, bh - 8), new V2(8, bh - 8) };
        zv.SetMapSize(bw, bh);
        Check(zv.nav.cols == (int)(bw / Config.NavCell) && zv.nav.rows == (int)(bh / Config.NavCell), "길찾기 칸이 맵 전체에 깔리지 않음");
        zv.playerPos = new V2(700, 420);   // 맵 한가운데: 카메라가 루카를 그대로 따라간다
        Check(zv.CameraCenter().Dist(new V2(700, 420 - Config.CameraLift)) < 1f, "카메라가 루카를 따라가지 않음: " + zv.CameraCenter());
        zv.playerPos = new V2(bw - 20, bh - 20);   // 맵 구석: 카메라가 맵 끝에서 멈춘다
        Check(zv.CameraCenter().Dist(new V2(bw - sw / 2, bh - sh / 2)) < 1f, "카메라가 맵 밖을 비춤: " + zv.CameraCenter());
        zv.playerPos = new V2(20, 60);
        Check(zv.CameraCenter().Dist(new V2(sw / 2, sh / 2)) < 1f, "카메라가 맵 밖을 비춤(왼쪽 위): " + zv.CameraCenter());
        zv.SetMapSize(sw, sh);
        Check(zv.CameraCenter().Dist(new V2(sw / 2, sh / 2)) < 1f, "맵이 화면만 한데 화면이 움직임");
        zv.SetMapSize(bw, bh);
        zv.WalkTo(new V2(bw - 60, bh - 60));   // 첫 화면 밖의 바닥으로 클릭 이동
        Check(zv.path.Count > 0 && zv.path[zv.path.Count - 1].Dist(new V2(bw - 60, bh - 60)) <= Config.NavCell, "화면 밖 바닥까지 가는 길이 없음");
    }
}
