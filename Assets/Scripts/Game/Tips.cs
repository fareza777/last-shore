using UnityEngine;

namespace LastShore
{
    /// <summary>One-time contextual hints: each appears the first time its situation comes up, then never again.</summary>
    public class Tips : MonoBehaviour
    {
        float _t = 3f;

        /// <summary>Shows a tip once per save. Returns true when it was shown.</summary>
        public static bool Show(string key, string text)
        {
            var gm = GameManager.I;
            if (gm == null || gm.Hud == null || !gm.Flags.Add("tip_" + key)) return false;
            gm.Hud.Tip(text);
            return true;
        }

        void Update()
        {
            var gm = GameManager.I; if (gm == null || gm.Player == null || gm.InputLocked) return;
            _t -= Time.deltaTime; if (_t > 0) return; _t = 1f;
            var v = gm.Vitals; bool touch = GameInput.TouchMode;

            if (gm.Hud.HasPrompt) Show("take", touch ? "Look at something and tap TAKE to pick it up or use it." : "Look at something and press E to pick it up or use it.");
            else if (v.Thirst < 55) Show("thirst", "Thirsty? Coconuts lie under the palms. Hold one and " + (touch ? "tap USE" : "click") + " to drink it.");
            else if (v.Hunger < 50) Show("hunger", "Hungry? Berries and bananas are safe raw. Cook meat and fish at a campfire.");
            else if (v.Warmth < 45) Show("cold", "You are getting cold. A fire, a shelter or a torch in hand will warm you.");
            else if (gm.Sky.TimeOfDay > 0.70f && gm.Sky.TimeOfDay < 0.78f) Show("dusk", "Night is coming. Animals fear flame: stay near a fire or carry a torch.");
            else if (gm.Player.Swimming) Show("swim", "Swimming drains stamina. Do not run out of breath far from shore.");
            else if (gm.Weather.Raining) Show("rain", "Rain fills rain collectors and drowns a fire that has no shelter beside it.");
            else
            {
                var s = gm.Interaction.Current;
                if (!s.Empty && s.Def.Kind == ItemKind.Tool) Show("tool", "Your tool is in hand. Aim at a tree, rock or animal and " + (touch ? "tap USE." : "click."));
                else if (!s.Empty && s.Def.Kind == ItemKind.Placeable) Show("build", "Aim at flat ground and " + (touch ? "tap USE" : "click") + " to build. " + (touch ? "TURN rotates it." : "R rotates it."));
                else if (gm.PlaySeconds > 40 && gm.Quests.Stage == 0) Show("bag", touch ? "The grid button opens your backpack, crafting and journal." : "Tab opens your backpack, crafting and journal.");
            }
        }
    }
}
