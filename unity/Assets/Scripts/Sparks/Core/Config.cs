// 한 곳에 모아 둔 조정값. 내용(문제·대사·배치)은 여기가 아니라 data/ 에 있다. (Godot판 config.gd 와 같은 값)
namespace Sparks.Core
{
    public static class Config
    {
        public const string StartZone = "scriptorium";
        public const int StartInk = 2;

        public const float WalkSpeed = 80f;      // 맵 픽셀/초 (화면에는 640×360이 보인다)
        public const float Reach = 46f;          // 이 거리 안의 사물을 조사·배치할 수 있다
        public const int NavCell = 8;            // 클릭 이동 길찾기 칸 크기
        public const float CameraLift = 24f;     // 카메라가 루카 발끝보다 이만큼 위(몸 가운데)를 본다
        public const int ViewW = 640, ViewH = 360;

        // 문 퍼즐 (plan.md 5장 '힌트와 보상')
        public static readonly int[] HintCosts = { 1, 1, 1, 2 };   // 1·2·3단계는 잉크 1개, 특별 힌트는 2개
        public const float WrongPenalty = 0.1f;  // 오답 1회마다 10%
        public const float MinEnergyRatio = 0.5f;

        public const float SparkHover = 8f;      // 스파크를 땅에서 띄우는 높이 (그림 크기와 상관없이)
        public const float SparkAmbient = 0.35f; // 스파크는 스스로 빛나므로 조명 색을 이만큼만 받는다
        public const bool ShowLabels = true;     // 임시 그림 위에 사물 이름표를 띄운다
        public const float BeamHeight = 10f;     // 빛줄기를 바닥에서 띄워 그리는 높이
        public const float ToastTime = 3f;
    }
}
