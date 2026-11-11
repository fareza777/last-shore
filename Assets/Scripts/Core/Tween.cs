using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastShore
{
    public enum Ease { Linear, InQuad, OutQuad, InOutQuad, OutCubic, InCubic, InOutCubic, OutBack, OutElastic, InOutSine }

    /// <summary>Small coroutine-driven tween runner. All tweens stop automatically when their owner is destroyed.</summary>
    public class Tween : MonoBehaviour
    {
        static Tween _runner;
        static Tween Runner
        {
            get
            {
                if (_runner == null)
                {
                    var go = new GameObject("[Tween]");
                    DontDestroyOnLoad(go);
                    _runner = go.AddComponent<Tween>();
                }
                return _runner;
            }
        }

        public static float Evaluate(Ease e, float t)
        {
            t = Mathf.Clamp01(t);
            switch (e)
            {
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return 1 - (1 - t) * (1 - t);
                case Ease.InOutQuad: return t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
                case Ease.OutCubic: return 1 - Mathf.Pow(1 - t, 3);
                case Ease.InCubic: return t * t * t;
                case Ease.InOutCubic: return t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
                case Ease.OutBack: { const float c1 = 1.70158f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2); }
                case Ease.OutElastic:
                    if (t <= 0) return 0; if (t >= 1) return 1;
                    return Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * (2 * Mathf.PI / 3)) + 1;
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1) / 2;
                default: return t;
            }
        }

        public static Coroutine To(UnityEngine.Object owner, float from, float to, float duration, Action<float> set,
            Ease ease = Ease.OutCubic, float delay = 0f, Action done = null, bool unscaled = true)
        {
            return Runner.StartCoroutine(Run(owner, from, to, duration, set, ease, delay, done, unscaled));
        }

        static IEnumerator Run(UnityEngine.Object owner, float from, float to, float duration, Action<float> set, Ease ease, float delay, Action done, bool unscaled)
        {
            float t = -delay;
            while (t < duration)
            {
                if (owner == null) yield break;
                if (t >= 0) set(Mathf.LerpUnclamped(from, to, Evaluate(ease, duration <= 0 ? 1 : t / duration)));
                yield return null;
                t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            }
            if (owner == null) yield break;
            set(to);
            done?.Invoke();
        }

        public static void Stop(Coroutine c) { if (c != null && _runner != null) _runner.StopCoroutine(c); }

        public static Coroutine Delay(UnityEngine.Object owner, float seconds, Action action, bool unscaled = true)
            => To(owner, 0, 1, seconds, _ => { }, Ease.Linear, 0, action, unscaled);

        public static Coroutine Fade(CanvasGroup g, float to, float duration, float delay = 0, Action done = null, Ease ease = Ease.OutCubic)
            => g == null ? null : To(g, g.alpha, to, duration, v => g.alpha = v, ease, delay, done);

        public static Coroutine Fade(Graphic g, float to, float duration, float delay = 0, Action done = null)
        {
            if (g == null) return null;
            return To(g, g.color.a, to, duration, v => { var c = g.color; c.a = v; g.color = c; }, Ease.OutCubic, delay, done);
        }

        public static Coroutine Scale(Transform t, Vector3 to, float duration, Ease ease = Ease.OutBack, float delay = 0, Action done = null)
        {
            if (t == null) return null;
            Vector3 from = t.localScale;
            return To(t, 0, 1, duration, v => t.localScale = Vector3.LerpUnclamped(from, to, v), ease, delay, done);
        }

        public static Coroutine Move(RectTransform t, Vector2 to, float duration, Ease ease = Ease.OutCubic, float delay = 0, Action done = null)
        {
            if (t == null) return null;
            Vector2 from = t.anchoredPosition;
            return To(t, 0, 1, duration, v => t.anchoredPosition = Vector2.LerpUnclamped(from, to, v), ease, delay, done);
        }

        public static Coroutine Color(Graphic g, Color to, float duration, float delay = 0)
        {
            if (g == null) return null;
            Color from = g.color;
            return To(g, 0, 1, duration, v => g.color = UnityEngine.Color.LerpUnclamped(from, to, v), Ease.OutCubic, delay);
        }

        /// <summary>Quick scale punch used for feedback (pickup, hit, button press).</summary>
        public static void Punch(Transform t, float amount = 0.18f, float duration = 0.28f)
        {
            if (t == null) return;
            Vector3 b = Vector3.one;
            To(t, 0, 1, duration, v => t.localScale = b * (1 + Mathf.Sin(v * Mathf.PI) * amount), Ease.OutQuad, 0, () => t.localScale = b);
        }

        /// <summary>Slide + fade a panel in from an offset. Requires a CanvasGroup.</summary>
        public static void SlideIn(RectTransform t, CanvasGroup g, Vector2 offset, float duration = 0.45f, float delay = 0f)
        {
            if (t == null) return;
            Vector2 target = t.anchoredPosition;
            t.anchoredPosition = target + offset;
            if (g != null) { g.alpha = 0; Fade(g, 1, duration * 0.9f, delay); }
            To(t, 0, 1, duration, v => t.anchoredPosition = Vector2.LerpUnclamped(target + offset, target, v), Ease.OutCubic, delay);
        }
    }
}
