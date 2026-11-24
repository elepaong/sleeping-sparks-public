// 걸을 수 있는 곳과 클릭 이동 길찾기 (8픽셀 칸, A*). 대각선은 양옆 칸이 모두 비었을 때만 간다.
using System;
using System.Collections.Generic;

namespace Sparks.Core
{
    public struct V2
    {
        public float x, y;
        public V2(float x, float y) { this.x = x; this.y = y; }
        public static V2 operator +(V2 a, V2 b) { return new V2(a.x + b.x, a.y + b.y); }
        public static V2 operator -(V2 a, V2 b) { return new V2(a.x - b.x, a.y - b.y); }
        public static V2 operator *(V2 a, float k) { return new V2(a.x * k, a.y * k); }
        public float Length { get { return (float)Math.Sqrt(x * x + y * y); } }
        public float Dist(V2 o) { return (this - o).Length; }
        public V2 Normalized { get { float l = Length; return l > 1e-6f ? new V2(x / l, y / l) : new V2(0, 0); } }
        public bool IsZero { get { return x == 0f && y == 0f; } }
        public static V2 Lerp(V2 a, V2 b, float t) { return new V2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t); }
        public override string ToString() { return "(" + x + ", " + y + ")"; }
    }

    public struct RectF
    {
        public float x, y, w, h;
        public RectF(float x, float y, float w, float h) { this.x = x; this.y = y; this.w = w; this.h = h; }
        public bool Has(V2 p) { return p.x >= x && p.y >= y && p.x < x + w && p.y < y + h; }
        public float Area { get { return w * h; } }
        public bool Empty { get { return w == 0f && h == 0f; } }
        public RectF Grow(float m) { return new RectF(x - m, y - m, w + m * 2, h + m * 2); }
    }

    public class Nav
    {
        public int cols, rows;
        bool[] solid;

        public static bool InPolygon(V2 p, List<V2> poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                V2 a = poly[i], b = poly[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        public void Build(float mapW, float mapH, Func<V2, bool> walkable)
        {
            int n = Config.NavCell;
            cols = (int)Math.Ceiling(mapW / n); rows = (int)Math.Ceiling(mapH / n);
            solid = new bool[cols * rows];
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                    solid[y * cols + x] = !walkable(Center(new Cell(x, y)));
        }

        public V2 Center(Cell c) { int n = Config.NavCell; return new V2(c.x * n + n / 2f, c.y * n + n / 2f); }

        public Cell CellAt(V2 p)
        {
            int n = Config.NavCell;
            return new Cell(Math.Max(0, Math.Min(cols - 1, (int)(p.x / n))), Math.Max(0, Math.Min(rows - 1, (int)(p.y / n))));
        }

        public bool InRegion(Cell c) { return c.x >= 0 && c.y >= 0 && c.x < cols && c.y < rows; }
        public bool Solid(Cell c) { return solid[c.y * cols + c.x]; }

        /// <summary>가장 가까운 빈 칸. 없으면 (-1, -1).</summary>
        public Cell NearestFree(Cell c)
        {
            if (!Solid(c)) return c;
            var best = new Cell(-1, -1); float bestD = float.MaxValue;
            for (int r = 1; r < 16; r++)
            {
                for (int x = c.x - r; x <= c.x + r; x++)
                    for (int y = c.y - r; y <= c.y + r; y++)
                    {
                        var q = new Cell(x, y);
                        if (!InRegion(q) || Solid(q)) continue;
                        float d = (float)Math.Sqrt((x - c.x) * (x - c.x) + (y - c.y) * (y - c.y));
                        if (d < bestD) { best = q; bestD = d; }
                    }
                if (best.x >= 0) return best;
            }
            return best;
        }

        /// <summary>칸에서 칸까지의 길 (두 끝 포함). 없으면 빈 목록.</summary>
        public List<Cell> Path(Cell from, Cell to)
        {
            var result = new List<Cell>();
            if (!InRegion(from) || !InRegion(to) || Solid(from) || Solid(to)) return result;
            int total = cols * rows, start = from.y * cols + from.x, goal = to.y * cols + to.x;
            var g = new float[total]; var came = new int[total]; var closed = new bool[total];
            for (int i = 0; i < total; i++) { g[i] = float.MaxValue; came[i] = -1; }
            var heap = new List<KeyValuePair<float, int>>();
            g[start] = 0f; Push(heap, 0f, start);
            while (heap.Count > 0)
            {
                int cur = Pop(heap);
                if (closed[cur]) continue;
                closed[cur] = true;
                if (cur == goal) break;
                int cx = cur % cols, cy = cur / cols;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = cx + dx, ny = cy + dy;
                        if (nx < 0 || ny < 0 || nx >= cols || ny >= rows) continue;
                        int ni = ny * cols + nx;
                        if (solid[ni] || closed[ni]) continue;
                        if (dx != 0 && dy != 0 && (solid[cy * cols + nx] || solid[ny * cols + cx])) continue;
                        float ng = g[cur] + (dx != 0 && dy != 0 ? 1.41421356f : 1f);
                        if (ng < g[ni])
                        {
                            g[ni] = ng; came[ni] = cur;
                            int hx = Math.Abs(nx - to.x), hy = Math.Abs(ny - to.y);
                            Push(heap, ng + Math.Max(hx, hy) + 0.41421356f * Math.Min(hx, hy), ni);
                        }
                    }
            }
            if (!closed[goal]) return result;
            for (int c = goal; c != -1; c = came[c]) result.Add(new Cell(c % cols, c / cols));
            result.Reverse();
            return result;
        }

        static void Push(List<KeyValuePair<float, int>> h, float f, int v)
        {
            h.Add(new KeyValuePair<float, int>(f, v));
            int i = h.Count - 1;
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (h[p].Key <= h[i].Key) break;
                var t = h[p]; h[p] = h[i]; h[i] = t; i = p;
            }
        }

        static int Pop(List<KeyValuePair<float, int>> h)
        {
            int v = h[0].Value;
            h[0] = h[h.Count - 1]; h.RemoveAt(h.Count - 1);
            int i = 0;
            while (true)
            {
                int l = i * 2 + 1, r = l + 1, m = i;
                if (l < h.Count && h[l].Key < h[m].Key) m = l;
                if (r < h.Count && h[r].Key < h[m].Key) m = r;
                if (m == i) break;
                var t = h[m]; h[m] = h[i]; h[i] = t; i = m;
            }
            return v;
        }
    }
}
