using System;
using System.Collections;
using UnityEngine;

namespace BlockCrush
{
    public static class Ease
    {
        public static float Linear(float t) => t;
        public static float InQuad(float t) => t * t;
        public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);
        public static float InOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        public static float InCubic(float t) => t * t * t;

        public static float OutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }

    /// <summary>Tiny coroutine tweener. Tweens run on <c>host</c> and stop when it is disabled or destroyed.</summary>
    public static class Tween
    {
        public static Coroutine Run(MonoBehaviour host, float duration, Action<float> step, Func<float, float> ease = null,
            float delay = 0f, Action done = null)
        {
            ease ??= Ease.Linear;
            if (host == null || !host.isActiveAndEnabled)
            {
                step?.Invoke(ease(1f));
                done?.Invoke();
                return null;
            }
            return host.StartCoroutine(Routine(duration, step, ease, delay, done));
        }

        public static Coroutine Delay(MonoBehaviour host, float delay, Action done) => Run(host, 0f, null, null, delay, done);

        static IEnumerator Routine(float duration, Action<float> step, Func<float, float> ease, float delay, Action done)
        {
            if (delay > 0f)
            {
                float d = 0f;
                while (d < delay) { d += Time.unscaledDeltaTime; yield return null; }
            }
            float t = 0f;
            while (t < duration)
            {
                step?.Invoke(ease(Mathf.Clamp01(t / duration)));
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            step?.Invoke(ease(1f));
            done?.Invoke();
        }

        // ---------- common shortcuts ----------

        public static Coroutine Scale(MonoBehaviour host, Transform target, Vector3 from, Vector3 to, float duration,
            Func<float, float> ease = null, float delay = 0f, Action done = null)
        {
            target.localScale = from;
            return Run(host, duration, t => { if (target) target.localScale = Vector3.LerpUnclamped(from, to, t); }, ease, delay, done);
        }

        public static Coroutine Move(MonoBehaviour host, RectTransform target, Vector2 from, Vector2 to, float duration,
            Func<float, float> ease = null, float delay = 0f, Action done = null)
        {
            target.anchoredPosition = from;
            return Run(host, duration, t => { if (target) target.anchoredPosition = Vector2.LerpUnclamped(from, to, t); }, ease, delay, done);
        }

        public static Coroutine Fade(MonoBehaviour host, CanvasGroup target, float from, float to, float duration,
            Func<float, float> ease = null, float delay = 0f, Action done = null)
        {
            target.alpha = from;
            return Run(host, duration, t => { if (target) target.alpha = Mathf.LerpUnclamped(from, to, t); }, ease, delay, done);
        }
    }
}
