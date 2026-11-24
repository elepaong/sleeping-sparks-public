// 빛줄기 계산 (design/ch1-prototype.md '빛줄기 규칙'). 화면과 무관한 순수 계산이다.
// tools/verify/field_check.py가 같은 규칙을 파이썬으로 다시 계산해 정답을 확인한다 — 한쪽을 고치면 다른 쪽도 고친다.
using System;
using System.Collections.Generic;

namespace Sparks.Core
{
    public struct Cell
    {
        public int x, y;
        public Cell(int x, int y) { this.x = x; this.y = y; }
        public override bool Equals(object o) { return o is Cell && ((Cell)o).x == x && ((Cell)o).y == y; }
        public override int GetHashCode() { return x * 73856093 ^ y * 19349663; }
        public override string ToString() { return "(" + x + ", " + y + ")"; }
    }

    public class BeamStep { public Cell cell; public string status; }
    public class BeamHit { public string id; public Dictionary<string, object> row; public string status; }
    public class BeamInfo { public string id, type, state; public bool blocksBeam; }
    public class BeamResult { public List<BeamStep> path = new List<BeamStep>(); public List<BeamHit> hits = new List<BeamHit>(); }

    public static class Beam
    {
        const int MaxSteps = 200;

        static Cell Dir(string d)
        {
            switch (d) { case "E": return new Cell(1, 0); case "W": return new Cell(-1, 0); case "N": return new Cell(0, -1); default: return new Cell(0, 1); }
        }

        /// <summary>'/'는 화면에서 왼쪽 아래↗오른쪽 위로 기운 거울. 꺾을 수 없으면 null.</summary>
        static string Turn(string mirror, string d)
        {
            if (mirror == "/") switch (d) { case "E": return "N"; case "N": return "E"; case "W": return "S"; case "S": return "W"; }
            if (mirror == "\\") switch (d) { case "E": return "S"; case "S": return "E"; case "W": return "N"; case "N": return "W"; }
            return null;
        }

        /// <summary>status: plain(그냥 빛) → converging(렌즈를 지나 모이는 중) → focused(초점) → spread(초점을 지나 퍼짐).
        /// blocked: 배경에 그려진 것(기계, 기둥, 탁자 …)이 차지한 칸. 빛은 그 칸 앞에서 멈춘다.
        /// findRow: 사물 → 상호작용표의 줄 (없으면 null)</summary>
        public static BeamResult Trace(int cols, int rows, Dictionary<Cell, BeamInfo> byCell, Cell start, string dir, int focal,
                                       Func<BeamInfo, Dictionary<string, object>> findRow, HashSet<Cell> blocked)
        {
            var r = new BeamResult();
            r.path.Add(new BeamStep { cell = start, status = "plain" });
            string status = "plain"; int remain = 0; Cell cell = start; string d = dir;
            for (int i = 0; i < MaxSteps; i++)
            {
                Cell step = Dir(d);
                cell = new Cell(cell.x + step.x, cell.y + step.y);
                if (blocked != null && blocked.Contains(cell)) break;
                if (status == "converging") { remain--; if (remain <= 0) status = "focused"; }
                else if (status == "focused") status = "spread";
                r.path.Add(new BeamStep { cell = cell, status = status });
                if (cell.x < 0 || cell.y < 0 || cell.x >= cols || cell.y >= rows) break;
                BeamInfo o;
                if (!byCell.TryGetValue(cell, out o)) continue;
                Dictionary<string, object> row = findRow(o);
                if (row == null)
                {
                    if (o.blocksBeam) break;
                    continue;
                }
                string kind = J.Str(J.Get(row, "effect"), "type");
                if (kind == "reflect")
                {
                    string nd = Turn(o.state, d);
                    if (nd == null) break;
                    d = nd;
                }
                else if (kind == "focus")
                {
                    if (status == "plain") { status = "converging"; remain = focal; }
                }
                else
                {
                    r.hits.Add(new BeamHit { id = o.id, row = row, status = status });
                    break;
                }
            }
            return r;
        }
    }
}
