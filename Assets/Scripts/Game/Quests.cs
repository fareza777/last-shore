using UnityEngine;

namespace LastShore
{
    /// <summary>Linear story objectives. Game systems report events through Notify().</summary>
    public class Quests : MonoBehaviour
    {
        public int Stage { get; private set; }
        public bool BoarSeen, StormSeen;
        public const int Final = 11;

        static readonly string[] Titles =
        {
            "FIRST STEPS", "A REAL TOOL", "THIRST", "FIRE", "A ROOF", "THE FIRST NIGHT",
            "SIGNS OF LIFE", "THE MERIDIAN", "WHAT A RAFT NEEDS", "TOMAS'S RAFT", "PROVISIONS", "HOME",
        };

        public string Title => Titles[Mathf.Clamp(Stage, 0, Titles.Length - 1)];

        public string Body
        {
            get
            {
                var inv = GameManager.I.Inv;
                switch (Stage)
                {
                    case 0: return "Gather sticks " + Mathf.Min(inv.Count("stick"), 3) + "/3 and stones " + Mathf.Min(inv.Count("stone"), 3) + "/3 along the beach.";
                    case 1: return "Gather plant fiber, then craft a Stone Axe." + (GameInput.TouchMode ? "" : "  [Tab] opens your backpack.");
                    case 2: return "Find something to drink. Coconuts lie beneath the palms.";
                    case 3: return "Craft a Campfire and place it before dark.";
                    case 4: return "Fell trees for logs and build a Shelter.";
                    case 5: return "Sleep in your shelter after dusk and see the morning.";
                    case 6: return "Explore inland. Someone lived on the east ridge.";
                    case 7: return "Find the wreck of the Meridian on the north shore.";
                    case 8: return "Collect logs " + Mathf.Min(inv.Count("log"), 12) + "/12, rope " + Mathf.Min(inv.Count("rope"), 6) + "/6 and sailcloth " + Mathf.Min(inv.Count("canvas"), 1) + "/1.";
                    case 9: return "Carry the materials to Tomas's raft on the east beach and finish it.";
                    case 10: return "Stock the raft with 3 cooked meals and 2 full water flasks, then set sail.";
                    default: return "Sail home.";
                }
            }
        }

        /// <summary>World marker for the compass, or null when the player should explore on their own.</summary>
        public Transform Marker
        {
            get
            {
                switch (Stage)
                {
                    case 6: return World.Find("Marker_Camp");
                    case 7: return World.Find("Marker_Wreck");
                    case 9: case 10: return World.Find("Marker_Raft");
                    default: return null;
                }
            }
        }

        static readonly string[] DoneVoice = { "sticks_done", "axe_done", "water_done", "fire_done", "shelter_done", "first_night", "camp_found", "wreck_found", "raft_parts", "raft_built" };

        public void SetStage(int stage, bool announce)
        {
            Stage = Mathf.Clamp(stage, 0, Final);
            if (announce) Announce();
        }

        public void Announce()
        {
            if (GameManager.I.Hud != null) GameManager.I.Hud.Objective(Title, Body, true);
        }

        void Advance()
        {
            int done = Stage;
            Stage++;
            var gm = GameManager.I;
            AudioManager.I.Play("quest", 0.8f, 0);
            if (done < DoneVoice.Length) AudioManager.I.Say(DoneVoice[done], Story.Line(DoneVoice[done]));
            gm.Hud.Banner("OBJECTIVE COMPLETE", Titles[done]);
            Tween.Delay(this, 2.6f, Announce, false);
            Tween.Delay(this, 3f, () => { if (GameManager.I != null) GameManager.I.SaveGame(); }, false);
        }

        public void Notify(string evt, string arg = null)
        {
            var gm = GameManager.I; if (gm == null || gm.Inv == null) return;
            var inv = gm.Inv;
            switch (Stage)
            {
                case 0: if (inv.Count("stick") >= 3 && inv.Count("stone") >= 3) Advance(); break;
                case 1: if (inv.Count("axe") > 0) Advance(); break;
                case 2: if (evt == "consume" && (arg == "coconut" || arg == "flask_full" || arg == "water")) Advance(); break;
                case 3: if (evt == "place" && arg == "Campfire") Advance(); break;
                case 4: if (evt == "place" && arg == "Shelter") Advance(); break;
                case 5: if (evt == "sleep" || (evt == "newday" && gm.Sky.Day >= 2)) Advance(); break;
                case 6: if (evt == "zone" && arg == "camp") Advance(); break;
                case 7: if (evt == "zone" && arg == "wreck") Advance(); break;
                case 8: if (inv.Count("log") >= 12 && inv.Count("rope") >= 6 && inv.Count("canvas") >= 1) Advance(); break;
                case 9: if (evt == "raft_built") Advance(); break;
            }
            if (gm.RaftStage >= 1 && Stage < 10) { Stage = 10; Announce(); }       // the player may finish the raft ahead of the story
            if (gm.Hud != null && Stage < Final) gm.Hud.Objective(Title, Body, false);
        }
    }
}
