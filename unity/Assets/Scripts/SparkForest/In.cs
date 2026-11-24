// Input wrapper: works with the legacy Input Manager, the new Input System, or both.
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SparkForest
{
    public enum Btn { Capture, Interact, Ability, Swap, Journal, Help, Any, Stage }

    public static class In
    {
        public static Vector2 Move()
        {
            Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1f;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1f;
                if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1f;
            }
            var g = Gamepad.current;
            if (g != null)
            {
                Vector2 st = g.leftStick.ReadValue();
                if (st.magnitude > 0.3f) v += st;
                Vector2 dp = g.dpad.ReadValue();
                if (dp.magnitude > 0.3f) v += dp;
            }
#else
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) v.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) v.x += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v.y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v.y += 1f;
#endif
            if (v.sqrMagnitude > 1f) v.Normalize();
            return v;
        }

        public static bool Held(Btn b) { return Read(b, false); }
        public static bool Down(Btn b) { return Read(b, true); }

        static bool Read(Btn b, bool down)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            var g = Gamepad.current;
            bool r = false;
            if (k != null)
            {
                switch (b)
                {
                    case Btn.Capture: r = down ? k.spaceKey.wasPressedThisFrame : k.spaceKey.isPressed; break;
                    case Btn.Interact: r = down ? (k.eKey.wasPressedThisFrame || k.zKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame) : (k.eKey.isPressed || k.zKey.isPressed || k.enterKey.isPressed); break;
                    case Btn.Ability: r = down ? k.qKey.wasPressedThisFrame : k.qKey.isPressed; break;
                    case Btn.Swap: r = down ? k.tabKey.wasPressedThisFrame : k.tabKey.isPressed; break;
                    case Btn.Journal: r = down ? k.jKey.wasPressedThisFrame : k.jKey.isPressed; break;
                    case Btn.Help: r = down ? k.hKey.wasPressedThisFrame : k.hKey.isPressed; break;
                    case Btn.Any: r = down ? k.anyKey.wasPressedThisFrame : k.anyKey.isPressed; break;
                    case Btn.Stage: r = down ? k.f2Key.wasPressedThisFrame : k.f2Key.isPressed; break;
                }
            }
            if (!r && g != null)
            {
                switch (b)
                {
                    case Btn.Capture: r = down ? (g.buttonWest.wasPressedThisFrame || g.rightTrigger.wasPressedThisFrame) : (g.buttonWest.isPressed || g.rightTrigger.isPressed); break;
                    case Btn.Interact: r = down ? g.buttonSouth.wasPressedThisFrame : g.buttonSouth.isPressed; break;
                    case Btn.Ability: r = down ? g.buttonNorth.wasPressedThisFrame : g.buttonNorth.isPressed; break;
                    case Btn.Swap: r = down ? g.buttonEast.wasPressedThisFrame : g.buttonEast.isPressed; break;
                    case Btn.Journal: r = down ? g.selectButton.wasPressedThisFrame : g.selectButton.isPressed; break;
                    case Btn.Help: r = down ? g.startButton.wasPressedThisFrame : g.startButton.isPressed; break;
                    case Btn.Any: r = down ? (g.buttonSouth.wasPressedThisFrame || g.startButton.wasPressedThisFrame) : g.buttonSouth.isPressed; break;
                }
            }
            return r;
#else
            switch (b)
            {
                case Btn.Capture: return down ? Input.GetKeyDown(KeyCode.Space) : Input.GetKey(KeyCode.Space);
                case Btn.Interact: return down ? (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return)) : (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.Return));
                case Btn.Ability: return down ? Input.GetKeyDown(KeyCode.Q) : Input.GetKey(KeyCode.Q);
                case Btn.Swap: return down ? Input.GetKeyDown(KeyCode.Tab) : Input.GetKey(KeyCode.Tab);
                case Btn.Journal: return down ? Input.GetKeyDown(KeyCode.J) : Input.GetKey(KeyCode.J);
                case Btn.Help: return down ? Input.GetKeyDown(KeyCode.H) : Input.GetKey(KeyCode.H);
                case Btn.Any: return down ? Input.anyKeyDown : Input.anyKey;
                case Btn.Stage: return down ? Input.GetKeyDown(KeyCode.F2) : Input.GetKey(KeyCode.F2);
            }
            return false;
#endif
        }
    }
}
