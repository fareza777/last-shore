using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    /// <summary>Sleeping through the night.</summary>
    public static class Rest
    {
        public static bool CanSleep { get { float t = GameManager.I.Sky.TimeOfDay; return t > 0.70f || t < 0.22f; } }

        public static void TrySleep(MonoBehaviour host, float healMul)
        {
            var gm = GameManager.I;
            if (!CanSleep) { gm.Hud.Hint("It is too early to sleep. Come back after dusk."); return; }
            if (gm.Vitals.Hunger < 8 || gm.Vitals.Thirst < 8) { gm.Hud.Hint("You are too hungry or thirsty to sleep."); return; }
            foreach (var a in Animal.All) if (a.Engaged && (a.transform.position - gm.Player.transform.position).sqrMagnitude < 30 * 30) { gm.Hud.Hint("You cannot sleep with danger this close."); return; }
            gm.StartCoroutine(Sleep(healMul));
        }

        static IEnumerator Sleep(float healMul)
        {
            var gm = GameManager.I;
            gm.Cinematic = true;
            bool done = false;
            SceneFader.FadeOut(1.2f, () => done = true);
            while (!done) yield return null;
            AudioManager.I.Play("sleep", 0.8f, 0);
            float hours = ((0.27f - gm.Sky.TimeOfDay + 1f) % 1f) * 24f;
            gm.Sky.SkipTo(0.27f);
            gm.Vitals.Rest(hours * healMul);
            yield return new WaitForSecondsRealtime(2.4f);
            gm.Cinematic = false;
            SceneFader.FadeIn(1.6f);
            gm.Quests.Notify("sleep");
            gm.SaveGame();
        }
    }
}
