// All in-game text (Korean). Edit freely.
namespace SparkForest
{
    public static class Texts
    {
        public const string Title = "잠든 스파크의 숲";
        public const string Witch = "루미";
        public static readonly string[] Types = { "leaf", "mush", "dew", "petal", "moon" };

        public static string SparkName(string t)
        {
            switch (t)
            {
                case "leaf": return "잎새 스파크";
                case "mush": return "버섯 스파크";
                case "dew": return "이슬 스파크";
                case "petal": return "꽃잎 스파크";
                case "moon": return "달빛 스파크";
            }
            return "스파크";
        }

        public static string SparkNote(string t)
        {
            switch (t)
            {
                case "leaf": return "오두막 빈터의 덤불과 나무 틈에서 낮잠을 자요.";
                case "mush": return "버섯 숲의 갓 아래를 좋아하는 수줍음쟁이.";
                case "dew": return "연못 풀밭의 풀잎 끝에 맺혀 있어요.";
                case "petal": return "꽃바람 언덕에서 꽃가루를 타고 놀아요.";
                case "moon": return "드물게 만나는 아이. 빛을 3만큼 품고 있어요.";
            }
            return "";
        }

        // ------------------------------------------------------------ companions: one per glade
        public static string CompName(string id)
        {
            switch (id)
            {
                case "glow": return "포포";
                case "water": return "퐁이";
                case "wind": return "후우";
            }
            return "";
        }

        public static string AbilityName(string id)
        {
            switch (id)
            {
                case "glow": return "불빛";
                case "water": return "꽃비";
                case "wind": return "바람";
            }
            return "";
        }

        public static string[] CompWait(string id, int have, int need)
        {
            string left = " (지금 " + have + " / " + need + ")";
            switch (id)
            {
                case "glow": return new[] { "포포|…포? (등불버섯 정령이 갓을 깜빡이며 뒤로 숨어요.)", "|버섯 숲 친구들과 먼저 친해져야 마음을 열 것 같아요.\n버섯 스파크를 " + need + "마리 모아 봐요." + left };
                case "water": return new[] { "퐁이|퐁… 퐁? (물방울 정령이 연잎 모자를 눌러쓰고 눈치를 봐요.)", "|연못 풀밭 친구들과 먼저 친해져야 할 것 같아요.\n이슬 스파크를 " + need + "마리 모아 봐요." + left };
                case "wind": return new[] { "후우|삐이? (바람새가 고개를 갸웃하며 한 발짝 물러나요.)", "|꽃바람 언덕 친구들과 먼저 친해져야 할 것 같아요.\n꽃잎 스파크를 " + need + "마리 모아 봐요." + left };
            }
            return new[] { "|…" };
        }

        public static string[] CompJoin(string id)
        {
            switch (id)
            {
                case "glow": return new[] { "포포|포! 포포! (버섯 스파크들이 반기자, 포포가 갓을 환하게 밝혀요.)", "|등불버섯 정령 〈포포〉가 동료가 됐어요!\n[Q] 불빛 — 둘레를 환하게 밝혀요. 그늘 덩굴이 물러나고, 잠든 등불버섯이 깨어나요." };
                case "water": return new[] { "퐁이|퐁퐁! (이슬 스파크들 곁에서 퐁이가 신나게 튀어 올라요.)", "|물방울 정령 〈퐁이〉가 동료가 됐어요!\n[Q] 꽃비 — 눈앞에 비를 내려요. 시든 꽃이 피고, 물가의 새싹이 자라나요.", "|동료가 둘 이상이면 [Tab]으로 교대할 수 있어요. 앞장선 동료의 능력만 쓸 수 있답니다." };
                case "wind": return new[] { "후우|삐이이! (꽃잎 스파크들과 한 바퀴 돌고는 루미 어깨에 내려앉아요.)", "|바람새 〈후우〉가 동료가 됐어요!\n[Q] 바람 — 바라보는 쪽으로 바람을 보내요. 낙엽 더미를 날리고, 나무 위 스파크를 떨어뜨려요.", "|지나온 숲에도 바람이 필요한 나무들이 있었죠. 다시 들러 봐도 좋아요." };
            }
            return new[] { "|…" };
        }

        public static string[] Intro = {
            "모찌|루미, 일어났구냥. 오늘도 숲이 조용하지?",
            "모찌|큰나무가 잠든 뒤로 스파크들이 여기저기 숨어 버렸어.",
            "모찌|덤불이나 항아리를 [E]로 흔들어 보라냥. 스파크가 튀어나오면 [Space]를 꾹 눌러 지팡이 빛으로 끌어당기는 거야.",
            "모찌|숲터마다 그곳을 지키는 정령이 산다냥. 그 숲의 스파크와 친해지면 정령도 동료가 되어 줄 거야."
        };

        public static string[] Npc(string id)
        {
            Game g = Game.I;
            if (id == "cat")
            {
                if (g.ended) return new[] { "모찌|숲이 반짝반짝하구냥. 오늘 저녁은 생선 수프로 축하하자." };
                if (g.comps.Count == 0) return new[] { "모찌|덤불, 항아리, 돌멩이… 수상한 건 일단 [E]로 흔들어 보라냥.", "모찌|빛이 10 모이면 북쪽 등불문이 열려. 그 너머 버섯 숲에 등불버섯 정령이 산다고 들었어." };
                if (!g.Has("water")) return new[] { "모찌|포포를 데려왔구냥! 동쪽 길을 막은 그늘 덩굴은 밝은 빛을 싫어해.", "모찌|가까이 가서 [Q]로 불빛을 비춰 보라냥." };
                if (!g.Has("wind")) return new[] { "모찌|시냇가 새싹에 퐁이의 꽃비를 뿌리면 건널 수 있을 거다냥.", "모찌|[Tab]으로 앞장설 동료를 바꾸는 것, 잊지 말라냥." };
                return new[] { "모찌|셋이 다 모였구냥. 큰나무 둘레의 꽃등은 셋이 번갈아 도와야 켜진대.", "모찌|낙엽은 후우, 꽃은 퐁이, 불은 포포. 순서대로다냥." };
            }
            if (id == "elder")
            {
                if (!g.Has("glow")) return new[] { "송이 할아범|허허, 꼬마 마녀로구나. 저기 깜빡이는 녀석이 보이느냐? 등불버섯 정령 포포란다.", "송이 할아범|낯을 가려서, 버섯 스파크들이 따르는 이에게만 마음을 연단다." };
                return new[] { "송이 할아범|포포의 불빛은 그늘진 것들을 달래 주지. 잿빛으로 잠든 등불버섯에도 비춰 보렴.", "송이 할아범|숲터마다 색도 냄새도 다르듯, 정령들 재주도 저마다 다르단다." };
            }
            if (id == "frog")
            {
                if (!g.Has("water")) return new[] { "개굴 씨|개굴. 연못가에 연잎 모자 쓴 물방울 봤어? 퐁이라고 해, 개굴.", "개굴 씨|이슬 스파크랑 친한 사람만 따라간대." };
                return new[] { "개굴 씨|시든 꽃에 꽃비를 뿌려 봐, 개굴. 이슬 스파크가 좋아서 튀어나올걸.", "개굴 씨|강가 새싹도 잊지 말고, 개굴!" };
            }
            return new[] { "|…" };
        }

        public static string[] Sign(string id)
        {
            switch (id)
            {
                case "sign_home": return new[] { "|〈꼬마 마녀 루미의 오두막〉\n약초차 끓이는 중. 고양이 조심." };
                case "sign_gate1": return new[] { "|〈버섯 숲 가는 길〉\n등불문은 빛 10이 모이면 눈을 떠요." };
                case "sign_leaf": return new[] { "|〈연못 풀밭 가는 길〉\n그늘 덩굴 주의. 밝은 불빛을 비추면 물러납니다." };
                case "sign_river": return new[] { "|〈시냇가〉\n물가의 새싹은 꽃비를 맞으면 쑥쑥 자란대요." };
                case "sign_gate2": return new[] { "|〈큰나무 숲터 가는 길〉\n낙엽은 바람에게. 등불문은 빛 45가 모이면 눈을 떠요." };
            }
            return new[] { "|…글씨가 지워져 있어요." };
        }
    }
}
