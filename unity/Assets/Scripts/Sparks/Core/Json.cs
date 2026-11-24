// 작은 JSON 읽개. data/ 의 파일은 모양이 자유로워서(조건·행동이 겹겹이 들어 있음) JsonUtility로 읽을 수 없다.
// 결과: Dictionary<string, object>, List<object>, string, double, bool, null
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sparks.Core
{
    public static class Json
    {
        public static object Parse(string text)
        {
            int i = 0;
            object v = Value(text, ref i);
            Ws(text, ref i);
            if (i != text.Length) throw new FormatException("JSON 끝에 남은 글자 (위치 " + i + ")");
            return v;
        }

        static void Ws(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\n' || s[i] == '\r' || s[i] == '\t' || s[i] == '﻿')) i++;
        }

        static object Value(string s, ref int i)
        {
            Ws(s, ref i);
            if (i >= s.Length) throw new FormatException("JSON이 중간에 끝남");
            char c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>();
                i++; Ws(s, ref i);
                if (s[i] == '}') { i++; return d; }
                while (true)
                {
                    Ws(s, ref i);
                    string k = Str(s, ref i);
                    Ws(s, ref i);
                    if (s[i] != ':') throw new FormatException("':' 가 있어야 함 (위치 " + i + ")");
                    i++;
                    d[k] = Value(s, ref i);
                    Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == '}') { i++; return d; }
                    throw new FormatException("',' 나 '}' 가 있어야 함 (위치 " + i + ")");
                }
            }
            if (c == '[')
            {
                var l = new List<object>();
                i++; Ws(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(Value(s, ref i));
                    Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == ']') { i++; return l; }
                    throw new FormatException("',' 나 ']' 가 있어야 함 (위치 " + i + ")");
                }
            }
            if (c == '"') return Str(s, ref i);
            if (string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
            int st = i;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '-' || s[i] == '+' || s[i] == '.' || s[i] == 'e' || s[i] == 'E')) i++;
            if (st == i) throw new FormatException("모르는 값 (위치 " + i + ")");
            return double.Parse(s.Substring(st, i - st), CultureInfo.InvariantCulture);
        }

        static string Str(string s, ref int i)
        {
            if (s[i] != '"') throw new FormatException("문자열이 있어야 함 (위치 " + i + ")");
            i++;
            var sb = new StringBuilder();
            while (true)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u': sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture)); i += 4; break;
                    default: sb.Append(e); break;
                }
            }
        }
    }

    /// <summary>읽은 JSON 값을 꺼내는 도우미. 없는 키는 기본값을 돌려준다.</summary>
    public static class J
    {
        static readonly List<object> NoList = new List<object>();
        static readonly Dictionary<string, object> NoDict = new Dictionary<string, object>();

        public static Dictionary<string, object> AsDict(object o) { return o as Dictionary<string, object> ?? NoDict; }
        public static List<object> AsList(object o) { return o as List<object> ?? NoList; }

        public static bool Has(object o, string key)
        {
            var d = o as Dictionary<string, object>;
            return d != null && d.ContainsKey(key);
        }

        public static object Get(object o, string key)
        {
            var d = o as Dictionary<string, object>;
            object v;
            return d != null && d.TryGetValue(key, out v) ? v : null;
        }

        public static string Text(object v)
        {
            if (v == null) return "";
            if (v is string) return (string)v;
            if (v is bool) return (bool)v ? "true" : "false";
            if (v is double)
            {
                double n = (double)v;
                return n == Math.Floor(n) ? ((long)n).ToString(CultureInfo.InvariantCulture) : n.ToString(CultureInfo.InvariantCulture);
            }
            return v.ToString();
        }

        public static string Str(object o, string key, string def = "")
        {
            object v = Get(o, key);
            return v == null ? def : Text(v);
        }

        public static double Num(object v, double def = 0)
        {
            if (v is double) return (double)v;
            if (v is int) return (int)v;
            return def;
        }

        public static double Num(object o, string key, double def = 0) { return Num(Get(o, key), def); }
        public static int Int(object o, string key, int def = 0) { return (int)Num(Get(o, key), def); }
        public static float At(List<object> l, int i) { return (float)Num(l[i]); }

        public static bool Bool(object o, string key, bool def = false)
        {
            object v = Get(o, key);
            return v is bool ? (bool)v : def;
        }

        public static List<object> List(object o, string key) { return AsList(Get(o, key)); }
        public static Dictionary<string, object> Dict(object o, string key) { return AsDict(Get(o, key)); }

        public static bool Contains(List<object> l, string s)
        {
            foreach (object x in l) if (x is string && (string)x == s) return true;
            return false;
        }

        /// <summary>값이 같은가 (문 퍼즐의 답 비교: 문자열, 수, 목록).</summary>
        public static bool Same(object a, object b)
        {
            if (a == null || b == null) return a == null && b == null;
            var la = a as List<object>; var lb = b as List<object>;
            if (la != null || lb != null)
            {
                if (la == null || lb == null || la.Count != lb.Count) return false;
                for (int i = 0; i < la.Count; i++) if (!Same(la[i], lb[i])) return false;
                return true;
            }
            return Text(a) == Text(b);
        }

        /// <summary>"{이름}" 자리에 값을 넣는다.</summary>
        public static string Format(string s, params object[] kv)
        {
            for (int i = 0; i + 1 < kv.Length; i += 2) s = s.Replace("{" + kv[i] + "}", Text(Boxed(kv[i + 1])));
            return s;
        }

        public static string Format(string s, Dictionary<string, object> args)
        {
            foreach (var p in args) s = s.Replace("{" + p.Key + "}", Text(p.Value));
            return s;
        }

        static object Boxed(object v)
        {
            if (v is int) return (double)(int)v;
            if (v is float) return (double)(float)v;
            return v;
        }
    }
}
