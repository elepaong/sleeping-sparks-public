// 입력. 옛 Input Manager와 새 Input System 어느 쪽이든 돈다. 키 배치는 plan.md 6장 '조작' 표 (Godot판 입력 맵과 같다).
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Sparks
{
    public enum Key { Interact, Investigate, Omega, ResetField, Cancel, DebugView, Accept }

    public static class Keys
    {
        /// <summary>이동 방향. 화면 기준이라 위쪽 키가 y = -1 (맵 좌표는 아래가 +).</summary>
        public static Vector2 Move()
        {
            Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1f;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1f;
                if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y -= 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y += 1f;
            }
#else
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) v.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) v.x += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v.y -= 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v.y += 1f;
#endif
            return v;
        }

        public static bool Down(Key key)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            switch (key)
            {
                case Key.Interact: return k.eKey.wasPressedThisFrame;
                case Key.Investigate: return k.qKey.wasPressedThisFrame;
                case Key.Omega: return k.tabKey.wasPressedThisFrame;
                case Key.ResetField: return k.rKey.wasPressedThisFrame;
                case Key.Cancel: return k.escapeKey.wasPressedThisFrame;
                case Key.DebugView: return k.f1Key.wasPressedThisFrame;
                case Key.Accept: return k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame;
            }
            return false;
#else
            switch (key)
            {
                case Key.Interact: return Input.GetKeyDown(KeyCode.E);
                case Key.Investigate: return Input.GetKeyDown(KeyCode.Q);
                case Key.Omega: return Input.GetKeyDown(KeyCode.Tab);
                case Key.ResetField: return Input.GetKeyDown(KeyCode.R);
                case Key.Cancel: return Input.GetKeyDown(KeyCode.Escape);
                case Key.DebugView: return Input.GetKeyDown(KeyCode.F1);
                case Key.Accept: return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
            }
            return false;
#endif
        }

        /// <summary>숫자 키 1~8 가운데 방금 눌린 것. 없으면 0.</summary>
        public static int SparkNumber()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return 0;
            if (k.digit1Key.wasPressedThisFrame) return 1;
            if (k.digit2Key.wasPressedThisFrame) return 2;
            if (k.digit3Key.wasPressedThisFrame) return 3;
            if (k.digit4Key.wasPressedThisFrame) return 4;
            if (k.digit5Key.wasPressedThisFrame) return 5;
            if (k.digit6Key.wasPressedThisFrame) return 6;
            if (k.digit7Key.wasPressedThisFrame) return 7;
            if (k.digit8Key.wasPressedThisFrame) return 8;
            return 0;
#else
            for (int n = 1; n <= 8; n++)
                if (Input.GetKeyDown(KeyCode.Alpha0 + n)) return n;
            return 0;
#endif
        }

        /// <summary>button: 0 왼쪽, 1 오른쪽</summary>
        public static bool MouseDown(int button)
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            if (m == null) return false;
            return button == 0 ? m.leftButton.wasPressedThisFrame : m.rightButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(button);
#endif
        }

        /// <summary>화면 픽셀 (왼쪽 아래가 0, 0)</summary>
        public static Vector2 MousePos()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            return m != null ? m.position.ReadValue() : Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }

        /// <summary>휠: +1 아래로(다음 스파크), -1 위로(이전 스파크), 0 없음</summary>
        public static int Wheel()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            float y = m != null ? m.scroll.ReadValue().y : 0f;
#else
            float y = Input.mouseScrollDelta.y;
#endif
            return y < -0.01f ? 1 : (y > 0.01f ? -1 : 0);
        }
    }
}
