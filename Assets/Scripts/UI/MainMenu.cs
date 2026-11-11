using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastShore
{
    public class MainMenu : MonoBehaviour
    {
        Canvas _canvas; RectTransform _safe, _column; GameObject _modal;

        void Start()
        {
            Time.timeScale = 1; AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            GameSettings.Load();
            var a = AudioManager.I; a.StopAllLayers();
            a.SetLayer("ocean", 0.7f); a.SetLayer("wind", 0.3f); a.SetLayer("fire", 0.45f); a.SetLayer("night", 0.25f);
            Build();
        }

        void Build()
        {
            _canvas = UI.MakeCanvas("MainMenu", 5);
            var shade = UI.Img(_canvas.transform, UI.GradientSide, new Color(0.01f, 0.03f, 0.05f, 0.9f), "Shade"); shade.rectTransform.Anchor(0, 0, 0.62f, 1, Vector2.zero, Vector2.zero);
            var bottom = UI.Img(_canvas.transform, UI.GradientUp, new Color(0.01f, 0.03f, 0.05f, 0.75f), "Bottom"); bottom.rectTransform.Anchor(0, 0, 1, 0.3f, Vector2.zero, Vector2.zero);
            _safe = UI.SafeArea(_canvas);
            _column = UI.Rect("Column", _safe).Pin(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, 0), new Vector2(760, 900));

            var title = UI.Label(_column, UI.Spaced(GameSettings.GameTitle, 1), 112, UI.TextCol, TextAnchor.MiddleLeft, FontStyle.Bold, true).Shadowed(0.5f, 5);
            title.rectTransform.Pin(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -40), new Vector2(1100, 150)); title.horizontalOverflow = HorizontalWrapMode.Overflow;
            var line = UI.Img(_column, null, UI.Accent, "Line"); line.rectTransform.Pin(new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(6, -200), new Vector2(0, 4));
            var tag = UI.Label(_column, UI.Spaced("SURVIVE.  BUILD.  SAIL HOME."), 26, UI.Accent, TextAnchor.MiddleLeft);
            tag.rectTransform.Pin(new Vector2(0, 1), new Vector2(0, 1), new Vector2(6, -214), new Vector2(900, 40));

            Appear(title.rectTransform, 0.2f); Appear(tag.rectTransform, 0.5f);
            Tween.To(this, 0, 150, 0.9f, v => line.rectTransform.sizeDelta = new Vector2(v, 4), Ease.OutCubic, 0.45f);
            Tween.To(this, 0, 1, 1f, v => { }, Ease.Linear, 0, null);
            StartCoroutine(Breathe(title.rectTransform));

            float y = -330; int i = 0;
            if (SaveSystem.HasSave) MenuButton("CONTINUE", ref y, true, i++, () => { GameManager.StartNewGame = false; SceneFader.Load("Game"); });
            MenuButton("NEW GAME", ref y, !SaveSystem.HasSave, i++, NewGame);
            MenuButton("SETTINGS", ref y, false, i++, OpenSettings);
            MenuButton("ABOUT", ref y, false, i++, OpenAbout);

            var row = UI.Rect("Row", _column).Pin(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, y - 24), new Vector2(700, 60));
            SmallButton(row, "RATE", 0, OpenRate); SmallButton(row, "SHARE", 1, Share);
            if (!Application.isMobilePlatform) SmallButton(row, "QUIT", 2, Application.Quit);
            Appear(row, 0.75f + i * 0.09f);

            var ver = UI.Label(_safe, "v" + Application.version + "   ·   " + GameSettings.Studio, 18, UI.Dim, TextAnchor.LowerRight);
            ver.rectTransform.Pin(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-36, 26), new Vector2(600, 30));
        }

        IEnumerator Breathe(RectTransform rt)
        {
            Vector2 p = rt.anchoredPosition;
            yield return new WaitForSecondsRealtime(1.2f);
            while (rt != null) { rt.anchoredPosition = p + new Vector2(0, Mathf.Sin(Time.unscaledTime * 0.7f) * 5f); yield return null; }
        }

        void Appear(RectTransform rt, float delay) { Tween.SlideIn(rt, rt.Group(), new Vector2(-70, 0), 0.7f, delay); }

        void MenuButton(string label, ref float y, bool primary, int index, Action onClick)
        {
            var b = UI.Btn(_column, label, onClick, 30, primary);
            var rt = (RectTransform)b.transform; rt.Pin(new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(6, y), new Vector2(430, 74));
            var t = b.GetComponentInChildren<Text>(); t.alignment = TextAnchor.MiddleLeft; t.rectTransform.offsetMin = new Vector2(34, 0); t.text = UI.Spaced(label);
            Appear(rt, 0.65f + index * 0.09f);
            y -= 90;
        }

        void SmallButton(RectTransform row, string label, int index, Action onClick)
        {
            var b = UI.Btn(row, UI.Spaced(label), onClick, 21);
            ((RectTransform)b.transform).Pin(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(6 + index * 150, 0), new Vector2(138, 52));
        }

        // ---------------------------------------------------------------- modals
        RectTransform Modal(Vector2 size, string title)
        {
            CloseModal();
            _modal = new GameObject("Modal", typeof(RectTransform)); _modal.transform.SetParent(_canvas.transform, false);
            ((RectTransform)_modal.transform).Stretch();
            var dim = UI.Img(_modal.transform, null, new Color(0.01f, 0.03f, 0.05f, 0.78f), "Dim", true); dim.rectTransform.Stretch();
            var g = _modal.transform.Group(); g.alpha = 0; Tween.Fade(g, 1, 0.2f);
            var bg = UI.Img(_modal.transform, UI.Rounded, UI.Bg, "Panel", true);
            var rt = bg.rectTransform; rt.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            rt.localScale = Vector3.one * 0.92f; Tween.Scale(rt, Vector3.one, 0.3f, Ease.OutBack);
            if (title != null)
            {
                var t = UI.Label(rt, UI.Spaced(title, 2), 36, UI.TextCol, TextAnchor.MiddleCenter, FontStyle.Bold, true); t.rectTransform.Anchor(0, 1, 1, 1, new Vector2(0, -90), new Vector2(0, -24));
                var line = UI.Img(rt, null, UI.Accent, "Line"); line.rectTransform.Pin(new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -98), new Vector2(140, 3));
            }
            _column.gameObject.SetActive(false);
            return rt;
        }

        void CloseModal() { if (_modal != null) Destroy(_modal); _modal = null; if (_column != null) _column.gameObject.SetActive(true); }

        Button ModalButton(RectTransform panel, string label, float x, bool primary, Action onClick, float width = 260)
        {
            var b = UI.Btn(panel, label, onClick, 25, primary);
            ((RectTransform)b.transform).Pin(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(x, 34), new Vector2(width, 64));
            return b;
        }

        void NewGame()
        {
            if (!SaveSystem.HasSave) { StartNew(); return; }
            var p = Modal(new Vector2(760, 380), "START OVER?");
            var t = UI.Label(p, "Your saved journey will be lost and you will wake on the beach again.", 26, UI.TextCol, TextAnchor.MiddleCenter);
            t.rectTransform.Stretch(60, 110, 60, 110);
            ModalButton(p, "CANCEL", -150, false, CloseModal);
            ModalButton(p, "START OVER", 150, true, StartNew);
        }

        void StartNew()
        {
            SaveSystem.Delete();
            GameManager.StartNewGame = true;
            SceneFader.Load("Intro", 0.9f);
        }

        void OpenSettings()
        {
            CloseModal();
            _modal = new GameObject("Modal", typeof(RectTransform)); _modal.transform.SetParent(_canvas.transform, false);
            ((RectTransform)_modal.transform).Stretch();
            var dim = UI.Img(_modal.transform, null, new Color(0.01f, 0.03f, 0.05f, 0.78f), "Dim", true); dim.rectTransform.Stretch();
            var rt = SettingsPanel.Build(_modal.transform, CloseModal);
            rt.localScale = Vector3.one * 0.92f; Tween.Scale(rt, Vector3.one, 0.3f, Ease.OutBack);
            _column.gameObject.SetActive(false);
        }

        void OpenAbout()
        {
            var p = Modal(new Vector2(980, 760), "ABOUT");
            string body =
                "<b>Last Shore</b> is a first-person survival story about one sailor, one island and one way home.\n\n" +
                "Gather, craft, cook and build. Keep the fire alive after dark. Follow the pages a castaway left behind, salvage what the sea gave back, and finish the raft before the storm season returns.\n\n" +
                "<color=#F2B45A>DESIGN, CODE, ART & WORLD</color>\n" + GameSettings.Studio + "\n\n" +
                "<color=#F2B45A>NARRATION</color>\nSynthesised voice by Deepgram Aura\n\n" +
                "<color=#F2B45A>SOUND</color>\nNature ambience and effects only. No music.\n\n" +
                "<color=#F2B45A>MADE WITH</color>\nUnity  ·  Blender";
            var t = UI.Label(p, body, 24, UI.TextCol, TextAnchor.UpperCenter); t.rectTransform.Stretch(70, 120, 70, 124); t.lineSpacing = 1.15f;
            ModalButton(p, "CLOSE", 0, true, CloseModal);
        }

        void OpenRate()
        {
            var p = Modal(new Vector2(820, 470), "ENJOYING THE ISLAND?");
            var t = UI.Label(p, "Tap a star to rate Last Shore.", 26, UI.Dim, TextAnchor.MiddleCenter); t.rectTransform.Anchor(0, 1, 1, 1, new Vector2(0, -170), new Vector2(0, -120));
            var stars = new Image[5]; var info = UI.Label(p, "", 24, UI.Accent, TextAnchor.MiddleCenter); info.rectTransform.Anchor(0, 0, 1, 0, new Vector2(0, 116), new Vector2(0, 156));
            for (int i = 0; i < 5; i++)
            {
                int n = i + 1;
                var img = UI.Img(p, UI.Icon("star"), UI.Hex("3A4654"), "Star" + n, true);
                img.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 2) * 110, 10), new Vector2(84, 84));
                var b = img.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                stars[i] = img;
                b.onClick.AddListener(() =>
                {
                    AudioManager.I.UI();
                    for (int k = 0; k < 5; k++) { stars[k].color = k < n ? UI.Accent : UI.Hex("3A4654"); if (k < n) Tween.Punch(stars[k].transform, 0.3f, 0.3f + k * 0.04f); }
                    PlayerPrefs.SetInt("rated", n);
                    if (n >= 4) { info.text = "Thank you! Opening the store page..."; Tween.Delay(this, 1.1f, OpenStore); }
                    else info.text = "Thank you. We will keep making it better.";
                });
            }
            ModalButton(p, "CLOSE", 0, false, CloseModal);
        }

        static void OpenStore()
        {
            string id = Application.identifier;
#if UNITY_ANDROID && !UNITY_EDITOR
            Application.OpenURL("market://details?id=" + id);
#else
            Application.OpenURL("https://play.google.com/store/apps/details?id=" + id);
#endif
        }

        void Share()
        {
            string text = GameSettings.ShareText + "  https://play.google.com/store/apps/details?id=" + Application.identifier;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var intentClass = new AndroidJavaClass("android.content.Intent"))
                using (var intent = new AndroidJavaObject("android.content.Intent"))
                {
                    intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                    intent.Call<AndroidJavaObject>("setType", "text/plain");
                    intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, "Share Last Shore"))
                        activity.Call("startActivity", chooser);
                }
                return;
            }
            catch (Exception e) { Debug.LogWarning("Share failed: " + e.Message); }
#endif
            GUIUtility.systemCopyBuffer = text;
            var p = Modal(new Vector2(760, 340), "SHARE");
            var t = UI.Label(p, "The invitation was copied to your clipboard.\nPaste it anywhere to share Last Shore.", 26, UI.TextCol, TextAnchor.MiddleCenter); t.rectTransform.Stretch(50, 110, 50, 110);
            ModalButton(p, "CLOSE", 0, true, CloseModal);
        }
    }

}
