// All audio is synthesised at boot: soft chimes for effects and a slow music-box loop. No audio assets needed.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SparkForest
{
    public class Sfx : MonoBehaviour
    {
        public static Sfx I;
        const int SR = 22050;
        AudioSource fx; AudioSource music;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        float birdT = 5f;

        static float Hz(float midi) { return 440f * Mathf.Pow(2f, (midi - 69f) / 12f); }

        static AudioClip Make(string name, float dur, Func<float, float, float> f)
        {
            int n = Mathf.Max(1, (int)(dur * SR));
            float[] d = new float[n];
            for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(f(i / (float)SR, i / (float)n), -1f, 1f);
            AudioClip c = AudioClip.Create(name, n, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }

        static float Bell(float t, float midi, float decay)
        {
            if (t < 0f) return 0f;
            float w = 6.2832f * Hz(midi) * t;
            return (Mathf.Sin(w) + 0.35f * Mathf.Sin(2f * w) + 0.12f * Mathf.Sin(3.01f * w)) * Mathf.Exp(-t * decay) * Mathf.Min(1f, t * 400f);
        }

        void Awake()
        {
            I = this;
            fx = gameObject.AddComponent<AudioSource>(); fx.playOnAwake = false; fx.spatialBlend = 0f;
            music = gameObject.AddComponent<AudioSource>(); music.playOnAwake = false; music.loop = true; music.volume = 0.5f; music.spatialBlend = 0f;
            System.Random rnd = new System.Random(3);
            clips["pop"] = Make("pop", 0.16f, (t, u) => Mathf.Sin(6.2832f * (500f + 700f * u) * t) * (1f - u) * 0.5f);
            clips["catch"] = Make("catch", 0.45f, (t, u) => (Bell(t, 84f, 9f) + Bell(t - 0.07f, 88f, 9f) + Bell(t - 0.14f, 91f, 7f)) * 0.3f);
            clips["catch3"] = Make("catch3", 0.8f, (t, u) => (Bell(t, 84f, 6f) + Bell(t - 0.08f, 88f, 6f) + Bell(t - 0.16f, 91f, 6f) + Bell(t - 0.24f, 96f, 4f)) * 0.3f);
            clips["shake"] = Make("shake", 0.28f, (t, u) => ((float)rnd.NextDouble() * 2f - 1f) * (1f - u) * (0.5f + 0.5f * Mathf.Sin(t * 90f)) * 0.22f);
            clips["gust"] = Make("gust", 0.8f, (t, u) => ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Sin(u * 3.1416f) * 0.28f * (0.6f + 0.4f * Mathf.Sin(t * 30f)));
            clips["rain"] = Make("rain", 1.0f, (t, u) => { float s = 0f; for (int i = 0; i < 9; i++) s += Bell(t - i * 0.1f, 79f + (i * 5) % 12, 14f); return s * 0.2f; });
            clips["open"] = Make("open", 1.4f, (t, u) => (Bell(t, 72f, 3f) + Bell(t - 0.15f, 76f, 3f) + Bell(t - 0.3f, 79f, 3f) + Bell(t - 0.45f, 84f, 2.5f)) * 0.26f);
            clips["learn"] = Make("learn", 2.0f, (t, u) => (Bell(t, 67f, 2.5f) + Bell(t - 0.18f, 72f, 2.5f) + Bell(t - 0.36f, 76f, 2.5f) + Bell(t - 0.54f, 79f, 2.5f) + Bell(t - 0.72f, 84f, 2f) + Bell(t - 0.9f, 88f, 2f)) * 0.22f);
            clips["talk"] = Make("talk", 0.07f, (t, u) => Mathf.Sin(6.2832f * 620f * t) * (1f - u) * 0.25f);
            clips["deny"] = Make("deny", 0.3f, (t, u) => (Bell(t, 60f, 12f) + Bell(t - 0.1f, 58f, 12f)) * 0.3f);
            clips["awake"] = Make("awake", 4.0f, (t, u) => { float s = 0f; int[] ns = { 60, 64, 67, 72, 76, 79, 84, 88, 91, 96 }; for (int i = 0; i < ns.Length; i++) s += Bell(t - i * 0.16f, ns[i], 1.2f); return s * 0.14f; });
            clips["bird"] = Make("bird", 0.5f, (t, u) => { float ph = (t * 9f) % 1f; return Mathf.Sin(6.2832f * (2600f + 900f * Mathf.Sin(ph * 6.2832f)) * t) * Mathf.Sin(Mathf.Clamp01(ph) * 3.1416f) * (t < 0.36f ? 0.1f : 0f); });
            music.clip = MakeMusic();
            music.Play();
        }

        static void Note(float[] d, float start, float midi, float amp, float decay, float dur)
        {
            int s0 = (int)(start * SR); int n = (int)(dur * SR);
            float w = 6.2832f * Hz(midi) / SR;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float e = Mathf.Exp(-t * decay) * Mathf.Min(1f, i / 60f) * amp;
                d[(s0 + i) % d.Length] += (Mathf.Sin(w * i) + 0.3f * Mathf.Sin(2f * w * i)) * e;
            }
        }

        static AudioClip MakeMusic()
        {
            float beat = 60f / 72f; int bars = 16;
            int n = (int)(bars * 4 * beat * SR);
            float[] d = new float[n];
            int[][] chords = { new[] { 0, 7, 16 }, new[] { -3, 4, 12 }, new[] { -7, 0, 9 }, new[] { -5, 2, 11 } };   // C  Am  F  G
            int[] scale = { 0, 2, 4, 7, 9, 12, 14, 16, 19 };
            System.Random r = new System.Random(11);
            int idx = 4;
            for (int bar = 0; bar < bars; bar++)
            {
                int[] ch = chords[bar % 4];
                for (int b = 0; b < 4; b++)
                {
                    float t0 = (bar * 4 + b) * beat;
                    Note(d, t0, 48 + ch[b % 3], 0.10f, 1.6f, 2.4f);
                    if (b == 0) Note(d, t0, 36 + ch[0], 0.08f, 1.0f, 3.0f);
                    for (int h = 0; h < 2; h++)
                    {
                        double pr = h == 0 ? 0.75 : 0.3;
                        if (bar % 8 == 7 && b > 1) pr = 0.0;
                        if (r.NextDouble() < pr)
                        {
                            idx = Mathf.Clamp(idx + r.Next(-2, 3), 0, scale.Length - 1);
                            Note(d, t0 + h * beat * 0.5f, 72 + scale[idx], 0.11f, 2.6f, 2.0f);
                        }
                    }
                }
            }
            float peak = 0.001f;
            for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(d[i]));
            float g = 0.6f / peak;
            for (int i = 0; i < n; i++) d[i] *= g;
            AudioClip c = AudioClip.Create("musicbox", n, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }

        public static void Play(string name, float vol = 1f, float pitchJitter = 0f)
        {
            if (I == null) return;
            AudioClip c;
            if (!I.clips.TryGetValue(name, out c)) return;
            I.fx.pitch = 1f + UnityEngine.Random.Range(-pitchJitter, pitchJitter);
            I.fx.PlayOneShot(c, vol);
        }

        void Update()
        {
            birdT -= Time.deltaTime;
            if (birdT <= 0f) { birdT = UnityEngine.Random.Range(6f, 15f); Play("bird", 0.7f, 0.15f); }
        }
    }
}
