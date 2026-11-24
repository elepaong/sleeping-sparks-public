// 어느 게임을 띄울지 고른다. Assets/Resources/boot.json 의 "game":
//   "sparks" (기본) = 기획서를 따르는 본 게임 '잠든 스파크의 수도원'
//   "forest"        = Unity 환경 시험용 프로토타입 '잠든 스파크의 숲'
using UnityEngine;
using Sparks.Core;

namespace Sparks
{
    public static class Launch
    {
        public static string Which()
        {
            TextAsset boot = Resources.Load<TextAsset>("boot");
            if (boot == null) return "sparks";
            try { return J.Str(Json.Parse(boot.text), "game", "sparks"); }
            catch (System.Exception) { return "sparks"; }
        }
    }
}
