using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastShore
{
    /// <summary>Boot scene: animated studio splash, then the main menu.</summary>
    public class SplashScreen : MonoBehaviour
    {
        bool _skipped;

        IEnumerator Start()
        {
            GameSettings.Load();
            var canvas = UI.MakeCanvas("Splash", 5);
            var bg = UI.Img(canvas.transform, null, UI.Hex("05090D"), "Bg", true); bg.rectTransform.Stretch();
            var glow = UI.Img(canvas.transform, UI.Glow, UI.Hex("F2B45A", 0.0f), "Glow"); glow.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 900));

            var studio = UI.Label(canvas.transform, UI.Spaced(GameSettings.Studio, 4), 84, UI.TextCol, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            studio.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 26), new Vector2(1600, 120));
            var line = UI.Img(canvas.transform, null, UI.Accent, "Line"); line.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -44), new Vector2(0, 3));
            var presents = UI.Label(canvas.transform, UI.Spaced("PRESENTS", 3), 24, UI.Dim, TextAnchor.MiddleCenter);
            presents.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -86), new Vector2(800, 40));
            var sg = studio.Group(); var pg = presents.Group(); sg.alpha = 0; pg.alpha = 0;
            studio.transform.localScale = Vector3.one * 1.12f;

            AudioManager.I.SetLayer("ocean", 0.5f);
            yield return Wait(0.4f);
            Tween.Fade(sg, 1, 1.1f); Tween.Scale(studio.transform, Vector3.one, 2.6f, Ease.OutCubic);
            Tween.To(this, 0, 0.16f, 1.6f, v => { var c = glow.color; c.a = v; glow.color = c; });
            Tween.To(this, 0, 300, 0.9f, v => line.rectTransform.sizeDelta = new Vector2(v, 3), Ease.OutCubic, 0.5f);
            Tween.Fade(pg, 1, 0.8f, 1.1f);
            yield return Wait(2.9f);
            Tween.Fade(sg, 0, 0.6f); Tween.Fade(pg, 0, 0.6f); Tween.Fade(line, 0, 0.6f);
            yield return Wait(0.7f);

            var note = UI.Label(canvas.transform, "Best experienced with headphones", 30, UI.TextCol, TextAnchor.MiddleCenter);
            note.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 50));
            var ng = note.Group(); ng.alpha = 0; Tween.Fade(ng, 1, 0.6f);
            yield return Wait(1.6f);
            Tween.Fade(ng, 0, 0.5f);
            yield return Wait(0.5f);
            SceneFader.Load("MainMenu", 0.4f);
        }

        IEnumerator Wait(float seconds)
        {
            float t = 0;
            while (t < seconds && !_skipped) { t += Time.unscaledDeltaTime; if (Input.anyKeyDown || Input.GetMouseButtonDown(0)) _skipped = true; yield return null; }
            if (_skipped) { _skipped = false; StopAllCoroutines(); SceneFader.Load("MainMenu", 0.3f); }
        }
    }
}
