using System;
using UnityEngine;

namespace BlockCrush
{
    /// <summary>
    /// Procedural sound effects and background music, so the game ships with audio without external files.
    /// The editor builder bakes these into WAV files under Assets/Audio (replace them freely).
    /// </summary>
    public static class SfxSynth
    {
        public const int SfxRate = 44100;
        public const int MusicRate = 22050;

        public static readonly string[] Ids = { "pick", "place", "clear", "combo", "invalid", "button", "spawn", "gameover", "newbest", "music" };

        public static AudioClip CreateClip(string id)
        {
            var data = Render(id, out int rate);
            var clip = AudioClip.Create("synth_" + id, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static float[] Render(string id, out int rate)
        {
            rate = SfxRate;
            switch (id)
            {
                case "pick": return Pick();
                case "place": return Place();
                case "clear": return Clear();
                case "combo": return Combo();
                case "invalid": return Invalid();
                case "button": return Button();
                case "spawn": return Spawn();
                case "gameover": return GameOver();
                case "newbest": return NewBest();
                case "music": rate = MusicRate; return Music();
                default: throw new ArgumentException("Unknown sound id " + id);
            }
        }

        // ---------- helpers ----------

        static float Midi(float note) => 440f * Mathf.Pow(2f, (note - 69f) / 12f);

        static float[] Buf(float seconds, int rate = SfxRate) => new float[Mathf.CeilToInt(seconds * rate)];

        static float Env(float t, float attack, float decay) =>
            t < 0 ? 0 : t < attack ? t / attack : Mathf.Exp(-(t - attack) / decay);

        static float Tri(float phase)
        {
            phase -= Mathf.Floor(phase);
            return 4f * Mathf.Abs(phase - 0.5f) - 1f;
        }

        /// <summary>Bell / pluck voice: sine plus a few harmonics with exponential decay.</summary>
        static void Bell(float[] buf, int rate, float start, float freq, float decay, float amp, float bright = 0.35f, bool wrap = false)
        {
            int s0 = Mathf.RoundToInt(start * rate);
            int len = Mathf.CeilToInt(decay * 7f * rate);
            for (int i = 0; i < len; i++)
            {
                int idx = s0 + i;
                if (idx >= buf.Length) { if (!wrap) break; idx %= buf.Length; }
                float t = (float)i / rate;
                float e = Env(t, 0.004f, decay);
                float ph = 2f * Mathf.PI * freq * t;
                float v = Mathf.Sin(ph) + bright * Mathf.Sin(2f * ph) * Mathf.Exp(-t * 6f) + bright * 0.4f * Mathf.Sin(3f * ph) * Mathf.Exp(-t * 10f);
                buf[idx] += v * e * amp;
            }
        }

        static void Pluck(float[] buf, int rate, float start, float freq, float decay, float amp, bool wrap)
        {
            int s0 = Mathf.RoundToInt(start * rate);
            int len = Mathf.CeilToInt(decay * 6f * rate);
            float lp = 0;
            for (int i = 0; i < len; i++)
            {
                int idx = s0 + i;
                if (idx >= buf.Length) { if (!wrap) break; idx %= buf.Length; }
                float t = (float)i / rate;
                float raw = Tri(freq * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f;
                lp += (raw - lp) * 0.35f;
                buf[idx] += lp * Env(t, 0.005f, decay) * amp;
            }
        }

        static void Sweep(float[] buf, float start, float dur, float f0, float f1, float decay, float amp)
        {
            int s0 = Mathf.RoundToInt(start * SfxRate);
            int len = Mathf.CeilToInt(dur * SfxRate);
            float phase = 0;
            for (int i = 0; i < len && s0 + i < buf.Length; i++)
            {
                float t = (float)i / SfxRate;
                float f = Mathf.Lerp(f0, f1, Mathf.Clamp01(t / dur));
                phase += 2f * Mathf.PI * f / SfxRate;
                buf[s0 + i] += Mathf.Sin(phase) * Env(t, 0.002f, decay) * amp;
            }
        }

        static void Noise(float[] buf, float start, float dur, float attack, float decay, float amp, float smooth, int seed)
        {
            var r = new System.Random(seed);
            int s0 = Mathf.RoundToInt(start * SfxRate);
            int len = Mathf.CeilToInt(dur * SfxRate);
            float lp = 0, prev = 0;
            for (int i = 0; i < len && s0 + i < buf.Length; i++)
            {
                float t = (float)i / SfxRate;
                float n = (float)(r.NextDouble() * 2 - 1);
                lp += (n - lp) * smooth;
                float hp = lp - prev; // crude high-pass for "air"
                prev = lp;
                buf[s0 + i] += (smooth > 0.5f ? hp * 2f : lp) * Env(t, attack, decay) * amp;
            }
        }

        static float[] Finish(float[] buf, float peak = 0.85f)
        {
            float max = 0.0001f;
            foreach (var v in buf) max = Mathf.Max(max, Mathf.Abs(v));
            float g = peak / max;
            int fade = Mathf.Min(buf.Length, 220);
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] *= g;
                int fromEnd = buf.Length - 1 - i;
                if (fromEnd < fade) buf[i] *= (float)fromEnd / fade;
            }
            return buf;
        }

        // ---------- sound effects ----------

        static float[] Pick()
        {
            var b = Buf(0.12f);
            Sweep(b, 0, 0.1f, 520, 820, 0.035f, 0.6f);
            Bell(b, SfxRate, 0.0f, 1320, 0.03f, 0.15f);
            return Finish(b, 0.5f);
        }

        static float[] Place()
        {
            var b = Buf(0.22f);
            Sweep(b, 0, 0.18f, 190, 70, 0.05f, 0.9f);
            Noise(b, 0, 0.04f, 0.001f, 0.008f, 0.35f, 0.25f, 11);
            Bell(b, SfxRate, 0.0f, 980, 0.018f, 0.2f, 0.1f);
            return Finish(b, 0.75f);
        }

        static float[] Clear()
        {
            var b = Buf(0.9f);
            float[] notes = { 84, 88, 91, 96 };
            for (int i = 0; i < notes.Length; i++)
                Bell(b, SfxRate, i * 0.045f, Midi(notes[i]), 0.22f, 0.5f, 0.45f);
            Noise(b, 0.02f, 0.5f, 0.02f, 0.14f, 0.12f, 0.9f, 23);
            return Finish(b, 0.7f);
        }

        static float[] Combo()
        {
            var b = Buf(1.0f);
            float[] chord = { 72, 76, 79, 84 };
            for (int i = 0; i < chord.Length; i++)
                Bell(b, SfxRate, i * 0.025f, Midi(chord[i]), 0.35f, 0.4f, 0.5f);
            var r = new System.Random(5);
            for (int i = 0; i < 7; i++)
                Bell(b, SfxRate, 0.08f + i * 0.055f, Midi(96 + r.Next(0, 12)), 0.06f, 0.12f, 0.2f);
            Sweep(b, 0, 0.35f, 400, 1600, 0.2f, 0.12f);
            return Finish(b, 0.75f);
        }

        static float[] Invalid()
        {
            var b = Buf(0.22f);
            Sweep(b, 0, 0.08f, 330, 280, 0.04f, 0.5f);
            Sweep(b, 0.09f, 0.1f, 280, 230, 0.05f, 0.5f);
            return Finish(b, 0.45f);
        }

        static float[] Button()
        {
            var b = Buf(0.09f);
            Sweep(b, 0, 0.07f, 950, 600, 0.02f, 0.7f);
            Noise(b, 0, 0.01f, 0.0005f, 0.003f, 0.15f, 0.3f, 3);
            return Finish(b, 0.5f);
        }

        static float[] Spawn()
        {
            var b = Buf(0.35f);
            Noise(b, 0, 0.32f, 0.12f, 0.06f, 0.5f, 0.12f, 77);
            Bell(b, SfxRate, 0.1f, Midi(79), 0.08f, 0.15f, 0.2f);
            Bell(b, SfxRate, 0.16f, Midi(84), 0.08f, 0.12f, 0.2f);
            return Finish(b, 0.4f);
        }

        static float[] GameOver()
        {
            var b = Buf(1.8f);
            float[] notes = { 67, 64, 60, 55 };
            for (int i = 0; i < notes.Length; i++)
                Pluck(b, SfxRate, i * 0.24f, Midi(notes[i]), i == notes.Length - 1 ? 0.45f : 0.22f, 0.6f, false);
            Bell(b, SfxRate, 0.72f, Midi(43), 0.5f, 0.35f, 0.1f);
            return Finish(b, 0.7f);
        }

        static float[] NewBest()
        {
            var b = Buf(2.0f);
            float[] run = { 72, 76, 79 };
            for (int i = 0; i < run.Length; i++) Bell(b, SfxRate, i * 0.12f, Midi(run[i]), 0.12f, 0.45f, 0.5f);
            float[] chord = { 72, 76, 79, 84, 88 };
            foreach (var n in chord) Bell(b, SfxRate, 0.38f, Midi(n), 0.6f, 0.3f, 0.4f);
            var r = new System.Random(9);
            for (int i = 0; i < 10; i++) Bell(b, SfxRate, 0.45f + i * 0.07f, Midi(96 + r.Next(0, 12)), 0.05f, 0.1f, 0.2f);
            return Finish(b, 0.75f);
        }

        // ---------- background music (seamless loop) ----------

        static float[] Music()
        {
            const int rate = MusicRate;
            const float beat = 60f / 92f;
            int bars = 8;
            var b = new float[Mathf.RoundToInt(bars * 4 * beat * rate)];

            // Cmaj7 - Am7 - Fmaj7 - G6, two bars each.
            int[][] chords =
            {
                new[] { 60, 64, 67, 71 },
                new[] { 57, 60, 64, 67 },
                new[] { 53, 57, 60, 64 },
                new[] { 55, 59, 62, 64 },
            };
            int[] roots = { 36, 33, 29, 31 };
            int[] arp = { 0, 1, 2, 3, 2, 1, 2, 3 };

            for (int c = 0; c < chords.Length; c++)
            {
                float t0 = c * 8 * beat;
                // pad
                foreach (var n in chords[c])
                    Pad(b, rate, t0, 8 * beat, Midi(n), 0.045f);
                // bass on beats 1 and 3
                for (int k = 0; k < 4; k++)
                    Bell(b, rate, t0 + k * 2 * beat, Midi(roots[c] + 12), 0.5f, k % 2 == 0 ? 0.22f : 0.16f, 0.05f, true);
                // arpeggio in eighths, one octave up, with a soft echo
                for (int e = 0; e < 16; e++)
                {
                    int note = chords[c][arp[e % arp.Length]] + 12 + (e % 8 >= 4 ? 12 : 0);
                    float t = t0 + e * beat * 0.5f;
                    float vel = e % 2 == 0 ? 0.11f : 0.08f;
                    Pluck(b, rate, t, Midi(note), 0.22f, vel, true);
                    Pluck(b, rate, t + beat * 0.75f, Midi(note), 0.2f, vel * 0.3f, true);
                }
                // gentle sparkle on the last beat of each chord
                Bell(b, rate, t0 + 7.5f * beat, Midi(chords[c][3] + 24), 0.3f, 0.04f, 0.2f, true);
            }

            // soft one-pole low-pass for warmth (two passes so the loop seam stays continuous)
            float lp = 0;
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < b.Length; i++)
                {
                    lp += (b[i] - lp) * 0.45f;
                    if (pass == 1) b[i] = lp;
                }

            float max = 0.0001f;
            foreach (var v in b) max = Mathf.Max(max, Mathf.Abs(v));
            for (int i = 0; i < b.Length; i++) b[i] *= 0.6f / max;
            return b;
        }

        static void Pad(float[] buf, int rate, float start, float dur, float freq, float amp)
        {
            int s0 = Mathf.RoundToInt(start * rate);
            float release = 1.2f;
            int len = Mathf.CeilToInt((dur + release) * rate);
            for (int i = 0; i < len; i++)
            {
                int idx = (s0 + i) % buf.Length; // wrap so the loop is seamless
                float t = (float)i / rate;
                float env = t < 0.8f ? t / 0.8f : t < dur ? 1f : Mathf.Max(0f, 1f - (t - dur) / release);
                float v = Mathf.Sin(2f * Mathf.PI * freq * t) + Mathf.Sin(2f * Mathf.PI * freq * 1.003f * t) * 0.6f +
                          Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.15f;
                buf[idx] += v * env * amp;
            }
        }
    }
}
