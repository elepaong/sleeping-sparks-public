// 진행 상태. 세이브는 없다 — 프로그램을 끄면 처음부터.
using System;
using System.Collections.Generic;

namespace Sparks.Core
{
    public class DoorRecord { public int wrong; public int hints; public bool solved; public int energy; }

    public class State
    {
        public readonly HashSet<string> flags = new HashSet<string>();
        public readonly List<string> sparks = new List<string>();          // 가진 스파크 id
        public string selectedSpark = "";
        public readonly Dictionary<string, string> placed = new Dictionary<string, string>();   // 스파크 id → 놓인 사물 id (지금 방 안에서만)
        public int ink, energy;
        public readonly List<string> items = new List<string>();
        public readonly List<string> inventions = new List<string>();
        public readonly Dictionary<string, DoorRecord> door = new Dictionary<string, DoorRecord>();
        public readonly List<string> doorOrder = new List<string>();       // 문 퍼즐을 만난 순서
        public readonly List<string> fieldSolved = new List<string>();     // 푼 필드 퍼즐 id
        public readonly Dictionary<string, Dictionary<string, string>> objStates = new Dictionary<string, Dictionary<string, string>>();   // 지역 id → {사물 id → 상태}
        public string zone = "";
        public bool investigate;
        public int hintsUsed;
        public bool ended;
        /// <summary>무언가 바뀔 때마다 1씩 는다 (화면이 다시 그릴지 볼 때 쓴다).</summary>
        public int version;

        readonly GameData data;

        public State(GameData data) { this.data = data; Reset(); }

        public void Reset()
        {
            flags.Clear(); sparks.Clear(); selectedSpark = ""; placed.Clear();
            ink = Config.StartInk; energy = 0;
            items.Clear(); inventions.Clear(); door.Clear(); doorOrder.Clear(); fieldSolved.Clear(); objStates.Clear();
            zone = ""; investigate = false; hintsUsed = 0; ended = false;
            Touch();
        }

        public void Touch() { version++; }

        public DoorRecord DoorRecord(string id)
        {
            DoorRecord r;
            if (!door.TryGetValue(id, out r)) { r = new DoorRecord(); door[id] = r; doorOrder.Add(id); }
            return r;
        }

        public bool IsSolved(string id)
        {
            DoorRecord r;
            if (door.TryGetValue(id, out r) && r.solved) return true;
            return fieldSolved.Contains(id);
        }

        /// <summary>오답 수에 따라 얻는 에너지 (plan.md: 오답 1회마다 10%씩 줄고 최소 50%)</summary>
        public static int EnergyFor(double baseEnergy, int wrong)
        {
            double ratio = Math.Max(Config.MinEnergyRatio, 1.0 - Config.WrongPenalty * wrong);
            return (int)Math.Round(baseEnergy * ratio, MidpointRounding.AwayFromZero);
        }

        public int SolveDoor(string id)
        {
            DoorRecord rec = DoorRecord(id);
            if (rec.solved) return rec.energy;
            rec.solved = true;
            rec.energy = EnergyFor(J.Num(data.puzzles[id], "energy"), rec.wrong);
            energy += rec.energy;
            Touch();
            return rec.energy;
        }

        public void AddSpark(string id)
        {
            if (!sparks.Contains(id)) { sparks.Add(id); Touch(); }
        }

        public void SelectSpark(string id)
        {
            selectedSpark = sparks.Contains(id) && id != selectedSpark ? id : "";
            Touch();
        }

        public void CycleSpark(int step)
        {
            var order = new List<string> { "" };
            foreach (object s in data.sparks) if (sparks.Contains(J.Str(s, "id"))) order.Add(J.Str(s, "id"));
            int i = order.IndexOf(selectedSpark);
            selectedSpark = order[((i + step) % order.Count + order.Count) % order.Count];
            Touch();
        }

        public void SetFlag(string name) { flags.Add(name); Touch(); }

        /// <summary>지역 데이터의 조건(if)을 따진다. 여러 키를 함께 쓰면 모두 맞아야 한다.</summary>
        public bool Check(object cond)
        {
            var d = cond as Dictionary<string, object>;
            if (d == null) return true;
            foreach (var p in d)
            {
                bool ok;
                string v = p.Value as string;
                switch (p.Key)
                {
                    case "flag": ok = flags.Contains(v); break;
                    case "not_flag": ok = !flags.Contains(v); break;
                    case "solved": ok = IsSolved(v); break;
                    case "not_solved": ok = !IsSolved(v); break;
                    case "has_spark": ok = sparks.Contains(v); break;
                    case "has_item": ok = items.Contains(v); break;
                    case "has_invention": ok = inventions.Contains(v); break;
                    case "all":
                        ok = true;
                        foreach (object c in J.AsList(p.Value)) ok = ok && Check(c);
                        break;
                    case "any":
                        ok = false;
                        foreach (object c in J.AsList(p.Value)) ok = ok || Check(c);
                        break;
                    default:
                        data.errors.Add("모르는 조건: " + p.Key);
                        ok = false; break;
                }
                if (!ok) return false;
            }
            return true;
        }
    }
}
