using UnityEngine;

namespace ShapeDrop
{
    /// <summary>Âm thanh 8-bit tổng hợp bằng code (không cần file âm thanh).</summary>
    public class Sfx : MonoBehaviour
    {
        const int Rate = 44100;
        AudioSource src;
        AudioClip start, pass, morph, up, win, die;
        public bool Muted;

        void Awake()
        {
            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            start = Mix("start", new[] { T(523, .06f, 0), T(784, .1f, .07f) });
            pass = Mix("pass", new[] { T(660, .06f, 0), T(990, .09f, .06f) });
            morph = Mix("morph", new[] { T(330, .2f, 0, 830, 2) });
            up = Mix("up", new[] { T(523, .08f, 0), T(659, .08f, .07f), T(784, .08f, .14f), T(1046, .08f, .21f) });
            win = Mix("win", new[] { T(392, .09f, 0, 0, 2), T(523, .09f, .08f, 0, 2), T(659, .09f, .16f, 0, 2), T(784, .09f, .24f, 0, 2), T(1046, .09f, .32f, 0, 2), T(1318, .12f, .40f, 0, 2) });
            die = Mix("die", new[] { T(220, .5f, 0, 40, 1, .09f), T(110, .6f, .05f, 40, 0, .06f) });
        }

        struct Tone { public float f, d, at, to, v; public int wave; }
        static Tone T(float f, float d, float at, float to = 0, int wave = 0, float v = .06f)
        {
            return new Tone { f = f, d = d, at = at, to = to, wave = wave, v = v };
        }

        static AudioClip Mix(string name, Tone[] tones)
        {
            float len = 0;
            foreach (var t in tones) len = Mathf.Max(len, t.at + t.d + .03f);
            int n = Mathf.CeilToInt(len * Rate);
            var data = new float[n];
            foreach (var t in tones)
            {
                int s0 = Mathf.RoundToInt(t.at * Rate), sn = Mathf.RoundToInt(t.d * Rate);
                double phase = 0;
                for (int i = 0; i < sn && s0 + i < n; i++)
                {
                    float k = i / (float)sn;
                    float f = t.to > 0 ? t.f * Mathf.Pow(t.to / t.f, k) : t.f;      // trượt tần số
                    phase += f / Rate;
                    float p = (float)(phase - System.Math.Floor(phase)), s;
                    if (t.wave == 1) s = p * 2 - 1;                              // sawtooth
                    else if (t.wave == 2) s = 1 - 4 * Mathf.Abs(p - .5f);        // triangle
                    else s = p < .5f ? 1 : -1;                                    // square
                    float env = t.v * Mathf.Pow(.0001f / t.v, k);                 // tắt dần theo hàm mũ
                    data[s0 + i] += s * env * 2.2f;
                }
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        void Play(AudioClip c) { if (!Muted && c != null) src.PlayOneShot(c); }
        public void Start_() { Play(start); }
        public void Pass() { Play(pass); }
        public void Morph() { Play(morph); }
        public void Up() { Play(up); }
        public void Win() { Play(win); }
        public void Die() { Play(die); }
    }
}
