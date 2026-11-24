// Unity API의 껍데기. Unity 없이 화면 코드(unity/Assets/Scripts)의 문법·형식 오류를 잡기 위한 것으로, 실행되지 않는다.
// 실제 Unity의 선언과 같게 적는다 — 여기서 통과해도 Unity에서 처음 열 때 한 번은 직접 확인해야 한다.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static void Destroy(Object o) { }
        public static void Destroy(Object o, float t) { }
        public static bool operator ==(Object a, Object b) { return ReferenceEquals(a, b); }
        public static bool operator !=(Object a, Object b) { return !ReferenceEquals(a, b); }
        public static implicit operator bool(Object o) { return !ReferenceEquals(o, null); }
        public override bool Equals(object o) { return ReferenceEquals(this, o); }
        public override int GetHashCode() { return 0; }
    }
    public class Component : Object
    {
        public Transform transform; public GameObject gameObject; public string tag;
        public T GetComponent<T>() { return default(T); }
    }
    public class Behaviour : Component { public bool enabled; }
    public class MonoBehaviour : Behaviour { }
    public class GameObject : Object
    {
        public Transform transform; public string tag;
        public GameObject() { } public GameObject(string name) { }
        public T AddComponent<T>() where T : Component { return default(T); }
        public T GetComponent<T>() { return default(T); }
        public void SetActive(bool v) { }
    }
    public class Transform : Component
    {
        public Vector3 position, localPosition, localScale; public Quaternion rotation; public Transform parent; public int childCount;
        public void SetParent(Transform p, bool worldPositionStays) { }
        public Transform GetChild(int i) { return null; }
    }
    public struct Vector2
    {
        public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero, one, up, down, left, right;
        public float magnitude { get { return 0; } } public float sqrMagnitude { get { return 0; } } public Vector2 normalized { get { return this; } }
        public void Normalize() { }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return a; } public static Vector2 operator -(Vector2 a, Vector2 b) { return a; }
        public static Vector2 operator -(Vector2 a) { return a; }
        public static Vector2 operator *(Vector2 a, float k) { return a; } public static Vector2 operator *(float k, Vector2 a) { return a; }
        public static Vector2 operator /(Vector2 a, float k) { return a; }
        public static float Dot(Vector2 a, Vector2 b) { return 0; }
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) { return a; }
        public static implicit operator Vector3(Vector2 v) { return new Vector3(); } public static implicit operator Vector2(Vector3 v) { return new Vector2(); }
    }
    public struct Vector3
    {
        public float x, y, z; public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; } public Vector3(float x, float y) { this.x = x; this.y = y; z = 0; }
        public static Vector3 zero, one; public float magnitude { get { return 0; } }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return a; } public static Vector3 operator -(Vector3 a, Vector3 b) { return a; }
        public static Vector3 operator *(Vector3 a, float k) { return a; }
    }
    public struct Quaternion { public static Quaternion identity; public static Quaternion Euler(float x, float y, float z) { return identity; } }
    public struct Matrix4x4 { }
    public struct Color
    {
        public float r, g, b, a; public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; } public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1; }
        public static Color white, black, clear; public static Color Lerp(Color a, Color b, float t) { return a; }
    }
    public struct Rect
    {
        public float x, y, width, height; public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
        public float xMax { get { return x + width; } } public float yMax { get { return y + height; } } public float xMin { get { return x; } } public float yMin { get { return y; } }
        public Vector2 center { get { return new Vector2(); } }
        public bool Contains(Vector2 p) { return false; }
    }
    public struct Bounds { public Vector3 size; }
    public static class Mathf
    {
        public const float Rad2Deg = 57.29578f, Deg2Rad = 0.0174533f, PI = 3.14159f;
        public static float Min(float a, float b) { return a; } public static float Max(float a, float b) { return a; } public static int Min(int a, int b) { return a; } public static int Max(int a, int b) { return a; }
        public static float Floor(float a) { return a; } public static float Round(float a) { return a; } public static float Ceil(float a) { return a; }
        public static int FloorToInt(float a) { return 0; } public static int RoundToInt(float a) { return 0; } public static int CeilToInt(float a) { return 0; }
        public static float Sqrt(float a) { return a; } public static float Abs(float a) { return a; } public static float Sin(float a) { return a; } public static float Cos(float a) { return a; }
        public static float Atan2(float y, float x) { return 0; } public static float Exp(float a) { return a; } public static float Pow(float a, float b) { return a; }
        public static float Clamp01(float a) { return a; } public static float Clamp(float a, float lo, float hi) { return a; } public static int Clamp(int a, int lo, int hi) { return a; }
        public static float Lerp(float a, float b, float t) { return a; } public static float Sign(float a) { return a; }
    }
    public static class Time { public static float deltaTime, time, timeSinceLevelLoad; public static int frameCount; }
    public static class Screen { public static int width, height; }
    public static class Application { public static int targetFrameRate; }
    public static class Debug { public static void Log(object o) { } public static void LogError(object o) { } public static void LogWarning(object o) { } }
    public static class Random
    {
        public static float value; public static Vector2 insideUnitCircle;
        public static float Range(float a, float b) { return a; } public static int Range(int a, int b) { return a; }
    }
    public enum CameraClearFlags { Skybox, SolidColor, Depth, Nothing }
    public class Camera : Behaviour
    {
        public static Camera main; public bool orthographic; public float orthographicSize, nearClipPlane, farClipPlane, depth, aspect;
        public CameraClearFlags clearFlags; public Color backgroundColor; public Rect pixelRect, rect; public int cullingMask;
        public Vector3 WorldToScreenPoint(Vector3 p) { return p; } public Vector3 ScreenToWorldPoint(Vector3 p) { return p; }
    }
    public class AudioListener : Behaviour { }
    public class AudioClip : Object
    {
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream) { return null; }
        public bool SetData(float[] data, int offsetSamples) { return true; }
    }
    public class AudioSource : Behaviour
    {
        public bool playOnAwake, loop; public float spatialBlend, volume, pitch; public AudioClip clip;
        public void Play() { } public void PlayOneShot(AudioClip c, float v) { } public void PlayOneShot(AudioClip c) { }
    }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureWrapMode { Repeat, Clamp }
    public enum TextureFormat { RGBA32, ARGB32 }
    public class Texture : Object { public int width, height; public FilterMode filterMode; public TextureWrapMode wrapMode; }
    public class Texture2D : Texture
    {
        public static Texture2D whiteTexture;
        public Texture2D(int w, int h) { } public Texture2D(int w, int h, TextureFormat f, bool mip) { }
        public void SetPixel(int x, int y, Color c) { } public void Apply() { }
    }
    public static class ImageConversion { public static bool LoadImage(this Texture2D t, byte[] data) { return true; } }
    public enum SpriteMeshType { FullRect, Tight }
    public class Sprite : Object
    {
        public Bounds bounds; public Rect rect; public Texture2D texture;
        public static Sprite Create(Texture2D t, Rect r, Vector2 pivot, float ppu, uint extrude, SpriteMeshType mesh) { return null; }
        public static Sprite Create(Texture2D t, Rect r, Vector2 pivot, float ppu) { return null; }
    }
    public class Shader : Object { public static Shader Find(string n) { return null; } }
    public class Material : Object { public Material(Shader s) { } }
    public class Renderer : Component { public bool enabled; public int sortingOrder; public Material sharedMaterial, material; }
    public class SpriteRenderer : Renderer { public Sprite sprite; public Color color; public bool flipX, flipY; }
    public class TextAsset : Object { public string text; public byte[] bytes; }
    public static class Resources { public static T Load<T>(string path) where T : Object { return null; } }
    public static class JsonUtility { public static T FromJson<T>(string s) { return default(T); } }
    public static class ColorUtility { public static bool TryParseHtmlString(string s, out Color c) { c = new Color(); return true; } }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad }
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute() { } public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }

    // ---- IMGUI
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum TextClipping { Overflow, Clip }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum ScaleMode { StretchToFill, ScaleAndCrop, ScaleToFit }
    public enum EventType { MouseDown, MouseUp, Repaint, Layout, KeyDown }
    public class Font : Object { public static Font CreateDynamicFontFromOSFont(string[] names, int size) { return null; } public static Font CreateDynamicFontFromOSFont(string name, int size) { return null; } }
    public class GUIContent { public static GUIContent none; public GUIContent() { } public GUIContent(string t) { } }
    public class GUIStyleState { public Color textColor; public Texture2D background; }
    public class RectOffset { public int left, right, top, bottom; public RectOffset() { } public RectOffset(int l, int r, int t, int b) { } }
    public class GUIStyle
    {
        public static GUIStyle none; public GUIStyle() { } public GUIStyle(GUIStyle o) { }
        public Font font; public int fontSize; public TextAnchor alignment; public bool wordWrap, richText; public TextClipping clipping; public FontStyle fontStyle;
        public GUIStyleState normal = new GUIStyleState(), hover, active; public RectOffset padding, border, margin;
        public float CalcHeight(GUIContent c, float width) { return 0; } public Vector2 CalcSize(GUIContent c) { return new Vector2(); }
    }
    public class GUISkin : Object { public GUIStyle label, box, button; public Font font; }
    public class Event { public static Event current; public Vector2 mousePosition; public EventType type; }
    public static class GUI
    {
        public static Color color; public static int depth; public static Matrix4x4 matrix; public static GUISkin skin; public static bool enabled;
        public static void DrawTexture(Rect r, Texture t) { } public static void DrawTexture(Rect r, Texture t, ScaleMode m) { }
        public static void DrawTextureWithTexCoords(Rect r, Texture t, Rect uv) { }
        public static void Label(Rect r, string text, GUIStyle s) { } public static void Label(Rect r, string text) { }
        public static bool Button(Rect r, GUIContent c, GUIStyle s) { return false; } public static bool Button(Rect r, string c, GUIStyle s) { return false; }
        public static Vector2 BeginScrollView(Rect pos, Vector2 scroll, Rect view) { return scroll; } public static void EndScrollView() { }
        public static void Box(Rect r, string t) { } public static void Box(Rect r, GUIContent c, GUIStyle s) { }
    }
    public static class GUIUtility { public static void RotateAroundPivot(float angle, Vector2 pivot) { } public static void ScaleAroundPivot(Vector2 scale, Vector2 pivot) { } }

    // ---- 옛 Input Manager
    public enum KeyCode { A, D, S, W, E, Q, R, Z, J, H, LeftArrow, RightArrow, UpArrow, DownArrow, Tab, Escape, Return, Space, F1, F2, Alpha0, Alpha1 }
    public static class Input
    {
        public static bool anyKey, anyKeyDown; public static Vector3 mousePosition; public static Vector2 mouseScrollDelta;
        public static bool GetKey(KeyCode k) { return false; } public static bool GetKeyDown(KeyCode k) { return false; }
        public static bool GetMouseButtonDown(int b) { return false; } public static bool GetMouseButton(int b) { return false; }
    }
}

// ---- 새 Input System (ENABLE_INPUT_SYSTEM 을 켜고 컴파일할 때만 쓰인다)
namespace UnityEngine.InputSystem
{
    using UnityEngine;
    public class ButtonControl { public bool isPressed, wasPressedThisFrame; }
    public class KeyControl : ButtonControl { }
    public class Vector2Control { public Vector2 ReadValue() { return new Vector2(); } }
    public class Keyboard
    {
        public static Keyboard current;
        public KeyControl aKey, dKey, sKey, wKey, eKey, qKey, rKey, zKey, jKey, hKey, leftArrowKey, rightArrowKey, upArrowKey, downArrowKey, tabKey, escapeKey, enterKey, spaceKey,
                          f1Key, f2Key, digit1Key, digit2Key, digit3Key, digit4Key, digit5Key, digit6Key, digit7Key, digit8Key, anyKey;
    }
    public class Mouse { public static Mouse current; public ButtonControl leftButton, rightButton; public Vector2Control position, scroll; }
    public class Gamepad
    {
        public static Gamepad current; public Vector2Control leftStick, dpad;
        public ButtonControl buttonWest, buttonSouth, buttonNorth, buttonEast, rightTrigger, selectButton, startButton;
    }
}
