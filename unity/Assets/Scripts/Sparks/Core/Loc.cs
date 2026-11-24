// 키 기반 문자열 테이블. 화면에 나오는 문장은 모두 Loc.T(키)로 찾는다.
//   data/strings/ko.json     → 적힌 키 그대로 (UI, 사물 이름, 지역 이름 …)
//   data/puzzles/<ID>.json   → puzzle.<ID>.title / text / explanation / parchment / source / hint.<0~3> / option.<번호>
//   data/dialogue/*.json     → dlg.<대사 ID>.<줄 ID>
//   data/sparks.json         → spark.<id>.name / phenomenon
//   data/inventions.json     → invention.<id>.name / description / unlocks
//   data/interactions.json   → interaction.<ID>.result / principle / object_name
// 데이터 파일의 한국어 문장이 원문이고, 다른 언어는 data/strings/<언어>.json에 같은 키로 넣어 덮어쓴다.
using System.Collections.Generic;

namespace Sparks.Core
{
    public class Loc
    {
        public string lang = "ko";
        public readonly Dictionary<string, string> table = new Dictionary<string, string>();
        public readonly HashSet<string> missing = new HashSet<string>();   // 찾지 못한 키 (시험에서 확인)
        readonly GameData data;

        public Loc(GameData data) { this.data = data; Rebuild(); }

        public void Rebuild()
        {
            table.Clear();
            foreach (var p in data.puzzles)
            {
                object pz = p.Value; string b = "puzzle." + p.Key + ".";
                table[b + "title"] = J.Str(pz, "title");
                table[b + "text"] = J.Str(pz, "text");
                table[b + "explanation"] = J.Str(pz, "explanation");
                table[b + "parchment"] = J.Str(J.Get(pz, "knowledge"), "parchment");
                table[b + "source"] = J.Str(J.Get(pz, "knowledge"), "source");
                var hints = J.List(pz, "hints");
                for (int i = 0; i < hints.Count; i++) table[b + "hint." + i] = J.Text(hints[i]);
                var options = J.List(J.Get(pz, "input"), "options");
                for (int i = 0; i < options.Count; i++) table[b + "option." + i] = J.Text(options[i]);
            }
            foreach (var p in data.dialogues)
                foreach (object line in J.AsList(p.Value))
                    table["dlg." + p.Key + "." + J.Str(line, "id")] = J.Str(line, "text");
            foreach (object s in data.sparks)
            {
                table["spark." + J.Str(s, "id") + ".name"] = J.Str(s, "name");
                table["spark." + J.Str(s, "id") + ".phenomenon"] = J.Str(s, "phenomenon");
            }
            foreach (var p in data.inventions)
            {
                table["invention." + p.Key + ".name"] = J.Str(p.Value, "name");
                table["invention." + p.Key + ".description"] = J.Str(p.Value, "description");
                table["invention." + p.Key + ".unlocks"] = J.Str(p.Value, "unlocks");
            }
            foreach (object row in data.interactions)
            {
                string b = "interaction." + J.Str(row, "id") + ".";
                table[b + "result"] = J.Str(row, "result");
                table[b + "principle"] = J.Str(row, "principle");
                table[b + "object_name"] = J.Str(row, "object_name");
            }
            foreach (var p in data.strings) table[p.Key] = J.Text(p.Value);
            if (lang != "ko")
                foreach (var p in J.AsDict(data.Load("data/strings/" + lang + ".json"))) table[p.Key] = J.Text(p.Value);
        }

        public bool Has(string key) { return table.ContainsKey(key); }

        /// <summary>키에 맞는 문장. kv는 이름, 값, 이름, 값 … 키가 없으면 '#키'를 돌려주고 기록한다.</summary>
        public string T(string key, params object[] kv)
        {
            string s;
            if (!table.TryGetValue(key, out s)) { missing.Add(key); return "#" + key; }
            return kv.Length > 0 ? J.Format(s, kv) : s;
        }
    }
}
