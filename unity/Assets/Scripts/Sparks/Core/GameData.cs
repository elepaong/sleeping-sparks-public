// data/ 아래 JSON을 모두 읽어 둔다. 내용은 전부 여기서 나온다.
// 파일을 어디서 읽는지는 밖에서 정한다 (Unity: Resources, 시험: 저장소의 data/ 폴더).
using System;
using System.Collections.Generic;

namespace Sparks.Core
{
    using Dict = Dictionary<string, object>;

    public class GameData
    {
        public List<object> sparks = new List<object>();            // sparks.json 순서 그대로 (1~8 키 순서)
        public List<object> interactions = new List<object>();
        public Dict inventions = new Dict();                        // id → 항목
        public Dict puzzles = new Dict();                           // 문 퍼즐 id → 항목
        public Dict dialogues = new Dict();                         // 대사 id → 줄 목록
        public Dict zones = new Dict();                             // 지역 id → 항목
        public List<string> zoneOrder = new List<string>();
        public Dict strings = new Dict();                           // data/strings/ko.json
        public List<string> errors = new List<string>();

        readonly Func<string, string> read;                         // "data/..." → 글 (없으면 null)
        readonly List<string> files;
        readonly HashSet<string> assets;
        readonly Func<string, int[]> imageSize;

        /// <param name="files">"data/sparks.json" 같은 경로 목록</param>
        /// <param name="assets">"assets/props/x.png" 같은 그림 경로 목록</param>
        /// <param name="imageSize">그림 경로 → {가로, 세로}</param>
        public GameData(Func<string, string> read, IEnumerable<string> files, IEnumerable<string> assets, Func<string, int[]> imageSize)
        {
            this.read = read;
            this.files = new List<string>(files);
            this.files.Sort(string.CompareOrdinal);
            this.assets = new HashSet<string>(assets);
            this.imageSize = imageSize;
            LoadAll();
        }

        void LoadAll()
        {
            sparks = J.AsList(Load("data/sparks.json"));
            interactions = J.AsList(Load("data/interactions.json"));
            foreach (object inv in J.AsList(Load("data/inventions.json"))) inventions[J.Str(inv, "id")] = inv;
            foreach (string f in InDir("data/puzzles/"))
            {
                if (f.Substring("data/puzzles/".Length).StartsWith("_")) continue;
                object pz = Load(f);
                if (pz is Dict) puzzles[J.Str(pz, "id")] = pz;
            }
            foreach (string f in InDir("data/dialogue/"))
                foreach (var p in J.Dict(Load(f), "dialogues")) dialogues[p.Key] = p.Value;
            foreach (string f in InDir("data/zones/"))
            {
                object z = Load(f);
                if (z is Dict) { zones[J.Str(z, "id")] = z; zoneOrder.Add(J.Str(z, "id")); }
            }
            strings = J.AsDict(Load("data/strings/ko.json"));
        }

        List<string> InDir(string dir)
        {
            var o = new List<string>();
            foreach (string f in files) if (f.StartsWith(dir) && f.EndsWith(".json")) o.Add(f);
            return o;
        }

        public object Load(string path)
        {
            string text = read(path);
            if (text == null) { Error("파일 없음: " + path); return null; }
            try { return Json.Parse(text); }
            catch (Exception e) { Error(path + ": " + e.Message); return null; }
        }

        public bool HasAsset(string path) { return assets.Contains(path); }

        /// <summary>그림 크기. 그림이 없으면 32×32.</summary>
        public int[] ImageSize(string path)
        {
            if (string.IsNullOrEmpty(path) || !HasAsset(path)) return new[] { 32, 32 };
            return imageSize(path) ?? new[] { 32, 32 };
        }

        public object Spark(string id)
        {
            foreach (object s in sparks) if (J.Str(s, "id") == id) return s;
            return null;
        }

        /// <summary>상호작용표에서 (사물 종류, 스파크, 언제)에 맞는 줄을 찾는다. 없으면 null = 반응 없음.
        /// 'effect'가 없는 줄(아직 초안뿐인 뒤 챕터의 줄)은 쓰지 않는다.</summary>
        public Dict FindInteraction(string objType, string objState, string sparkId, string trigger, List<string> ownedInventions)
        {
            foreach (object row in interactions)
            {
                if (J.Str(row, "object") != objType || !J.Has(row, "effect")) continue;
                if (J.Str(row, "trigger") != trigger || !J.Contains(J.List(row, "sparks"), sparkId)) continue;
                if (J.Has(row, "object_state") && J.Str(row, "object_state") != objState) continue;
                bool ok = true;
                foreach (object need in J.List(row, "requires")) if (!ownedInventions.Contains(J.Text(need))) ok = false;
                if (ok) return (Dict)row;
            }
            return null;
        }

        void Error(string msg) { errors.Add(msg); }
    }
}
