using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastShore
{
    /// <summary>Shared letterbox + subtitle overlay for cinematics.</summary>
    public class CinemaOverlay : MonoBehaviour
    {
        public Canvas Canvas; Text _sub; CanvasGroup _subG; Coroutine _c;

        public static CinemaOverlay Create(System.Action onSkip)
        {
            var canvas = UI.MakeCanvas("Cinema", 40);
            var o = canvas.gameObject.AddComponent<CinemaOverlay>(); o.Canvas = canvas;
            var top = UI.Img(canvas.transform, null, Color.black, "BarTop"); top.rectTransform.Anchor(0, 1, 1, 1, new Vector2(0, 0), new Vector2(0, 0));
            var bot = UI.Img(canvas.transform, null, Color.black, "BarBottom"); bot.rectTransform.Anchor(0, 0, 1, 0, new Vector2(0, 0), new Vector2(0, 0));
            Tween.To(o, 0, 118, 1.2f, v => { top.rectTransform.offsetMin = new Vector2(0, -v); bot.rectTransform.offsetMax = new Vector2(0, v); }, Ease.OutCubic);
            o._sub = UI.Label(canvas.transform, "", 34, UI.TextCol, TextAnchor.MiddleCenter).Shadowed(0.9f, 3);
            o._sub.rectTransform.Anchor(0.1f, 0, 0.9f, 0, new Vector2(0, 130), new Vector2(0, 230)); o._subG = o._sub.Group(); o._subG.alpha = 0;
            if (onSkip != null)
            {
                var safe = UI.SafeArea(canvas);
                var b = UI.Btn(safe, UI.Spaced("SKIP"), onSkip, 20);
                ((RectTransform)b.transform).Pin(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -30), new Vector2(130, 50));
                var g = b.Group(); g.alpha = 0; Tween.Fade(g, 0.8f, 0.8f, 1.5f);
            }
            return o;
        }

        void OnEnable() { AudioManager.Subtitle += OnSub; }
        void OnDisable() { AudioManager.Subtitle -= OnSub; }

        void OnSub(string text, float duration)
        {
            Tween.Stop(_c);
            if (string.IsNullOrEmpty(text)) { Tween.Fade(_subG, 0, 0.2f); return; }
            _sub.text = text; Tween.Fade(_subG, 1, 0.3f);
            _c = Tween.Fade(_subG, 0, 0.5f, duration + 0.4f);
        }
    }

    /// <summary>Finale: sailing away from the island at dawn, then the end card.</summary>
    public class EndingCinematic : MonoBehaviour
    {
        public static bool Active;

        public static void Play(RaftSite site)
        {
            if (Active) return;
            var go = new GameObject("Ending");
            go.AddComponent<EndingCinematic>().StartCoroutine(go.GetComponent<EndingCinematic>().Run(site));
        }

        void OnDestroy() { Active = false; }

        IEnumerator Run(RaftSite site)
        {
            Active = true;
            var gm = GameManager.I; var a = AudioManager.I;
            gm.SaveGame();
            gm.Cinematic = true; gm.InvUI.Close(); gm.Hud.SetVisible(false);
            gm.Quests.SetStage(Quests.Final, false);
            bool black = false; SceneFader.FadeOut(1.4f, () => black = true);
            while (!black) yield return null;

            gm.Sky.Frozen = true; gm.Sky.TimeOfDay = 0.272f; gm.Sky.Cloud = 0; gm.Weather.Force(WeatherState.Clear, 99999f);
            Transform raft = site.RaftVisual != null ? site.RaftVisual.transform : site.transform;
            raft.SetParent(null);
            Vector3 dir = site.transform.position - new Vector3(World.Size * 0.5f, 0, World.Size * 0.5f); dir.y = 0; dir.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            Vector3 start = site.transform.position + dir * 22f; start.y = World.SeaLevel + 0.05f;
            Quaternion facing = Quaternion.LookRotation(-dir);
            gm.Player.enabled = false;
            var cam = gm.Player.Cam.transform; cam.SetParent(null); gm.Player.Hand.gameObject.SetActive(false);
            gm.Player.Cam.fieldOfView = 55f;
            var overlay = CinemaOverlay.Create(null);

            a.SetLayer("birds", 0.2f); a.SetLayer("ocean", 0.9f); a.SetLayer("wind", 0.5f); a.SetLayer("rain", 0); a.SetLayer("night", 0);
            yield return new WaitForSeconds(0.8f);
            a.Play("sail", 0.9f, 0);
            SceneFader.FadeIn(2.6f);

            float t = 0; int said = 0; float[] cue = { 2f, 11.5f, 22f };
            bool fading = false; bool done = false;
            while (!done)
            {
                t += Time.deltaTime;
                Vector3 rp = start + dir * t * 2.3f + Vector3.up * Mathf.Sin(t * 1.2f) * 0.12f;
                raft.SetPositionAndRotation(rp, facing * Quaternion.Euler(Mathf.Sin(t * 0.9f) * 3f, 0, Mathf.Sin(t * 1.3f) * 4f));
                Vector3 pos, look;
                if (t < 10f) { pos = rp - dir * 8f + Vector3.up * (2.4f + t * 0.12f) + right * 3f; look = rp + Vector3.up * 1.6f; }
                else if (t < 21f) { pos = rp + right * (10f - (t - 10f) * 0.25f) + Vector3.up * 1.0f + dir * 4f; look = rp + Vector3.up * 1.4f; }
                else { float u = t - 21f; pos = rp + dir * (9f + u * 1.2f) + Vector3.up * (4f + u * 1.3f) + right * 2f; look = rp + Vector3.up * 1f - dir * 30f; }
                cam.position = Vector3.Lerp(cam.position, pos, Time.deltaTime * (t < 0.2f || Mathf.Abs(t - 10f) < 0.1f || Mathf.Abs(t - 21f) < 0.1f ? 200f : 2.5f));
                cam.rotation = Quaternion.Slerp(cam.rotation, Quaternion.LookRotation(look - cam.position), Time.deltaTime * 3f);
                if (said < cue.Length && t >= cue[said]) { a.Say("ending_" + (said + 1), Story.Ending[said]); said++; }
                if (t > 33f && !fading && !a.VoicePlaying) { fading = true; SceneFader.FadeOut(2.5f, () => done = true); }
                yield return null;
            }
            Destroy(overlay.Canvas.gameObject);
            PlayerPrefs.SetInt("completed", 1); PlayerPrefs.Save();
            EndCard(gm.Sky.Day, gm.PlaySeconds);
            SceneFader.FadeIn(1.5f);
        }

        static void EndCard(int days, float seconds)
        {
            var canvas = UI.MakeCanvas("EndCard", 60);
            var bg = UI.Img(canvas.transform, null, UI.Hex("05090D"), "Bg", true); bg.rectTransform.Stretch();
            var glow = UI.Img(canvas.transform, UI.Glow, UI.Hex("F2B45A", 0.12f), "Glow"); glow.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(1500, 900));
            var title = UI.Label(canvas.transform, UI.Spaced(GameSettings.GameTitle, 4), 96, UI.TextCol, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            title.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(1700, 130));
            var line = UI.Img(canvas.transform, null, UI.Accent, "Line"); line.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(220, 3));
            var home = UI.Label(canvas.transform, "You made it home.", 40, UI.Accent, TextAnchor.MiddleCenter);
            home.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(1200, 60));
            int minutes = Mathf.Max(1, Mathf.RoundToInt(seconds / 60f));
            var stats = UI.Label(canvas.transform, "Days on the island   " + days + "\nTime played   " + (minutes / 60 > 0 ? minutes / 60 + " h " : "") + minutes % 60 + " min", 28, UI.TextCol, TextAnchor.MiddleCenter);
            stats.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(1200, 100)); stats.lineSpacing = 1.3f;
            var credits = UI.Label(canvas.transform, "A game by " + GameSettings.Studio + "\nNarration: Deepgram Aura   ·   Built with Unity and Blender\nThank you for playing.", 22, UI.Dim, TextAnchor.MiddleCenter);
            credits.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -130), new Vector2(1400, 120)); credits.lineSpacing = 1.3f;
            var b = UI.Btn(canvas.transform, UI.Spaced("MAIN MENU"), () => { Active = false; SceneFader.Load("MainMenu"); }, 26, true);
            ((RectTransform)b.transform).Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -280), new Vector2(360, 70));
            var g = canvas.Group(); g.alpha = 0; Tween.Fade(g, 1, 1.5f, 0.3f);
            title.transform.localScale = Vector3.one * 1.08f; Tween.Scale(title.transform, Vector3.one, 3f, Ease.OutCubic, 0.3f);
        }
    }
}
