using System.Collections.Generic;

namespace LastShore
{
    /// <summary>All narrative text. Voice clips in Resources/Audio/VO share the same keys.</summary>
    public static class Story
    {
        public static readonly string[] Intro =
        {
            "The wind hit like a wall. The mast snapped clean through.",
            "Water came over the rail faster than I could think.",
            "I clipped my harness and held on.",
            "The Meridian went down in the dark.",
            "I don't remember reaching the shore.",
            "I woke to sun on my face and sand in my mouth.",
        };

        public static readonly string[] Ending =
        {
            "The raft holds. The sail catches the wind.",
            "The island grows small behind me, green against the morning.",
            "I came here with nothing. I am leaving with everything I learned. I am going home.",
        };

        static readonly Dictionary<string, string> Lines = new Dictionary<string, string>
        {
            { "wake", "Alive. The island is small, green, and empty." },
            { "sticks_done", "Enough dry wood to start with." },
            { "axe_done", "A real tool. This changes everything." },
            { "water_done", "Fresh water. I can think clearly now." },
            { "fire_done", "Fire means warmth, safety, and cooked food." },
            { "shelter_done", "A roof over my head. It will hold." },
            { "first_night", "I made it through one day. Tomorrow I start looking for a way off." },
            { "camp_found", "Someone else was here. Tomas Reyes. He survived too." },
            { "wreck_found", "The Meridian broke apart, but the rope and canvas are still good." },
            { "raft_parts", "Logs, rope, canvas. Everything I need is here." },
            { "raft_built", "She's not pretty, but she'll float. Time to leave." },
            { "low_health", "I need to eat and rest or I won't make it." },
            { "storm", "The wind is picking up. I need to find shelter fast." },
            { "boar", "Boars. They're dangerous, but they avoid the fire." },
            { "found_falls", "Fresh water. Clean, cold, and all I can drink." },
            { "found_lookout", "From up here I can see the whole island." },
            { "found_plane", "A wrecked plane. Maybe there is something I can use." },
            { "found_light", "An old lighthouse. Its lamp went dark a long time ago." },
            { "found_jetty", "Somebody built this jetty. Good water for fishing." },
            { "found_arch", "The sea carved this arch. It must have taken a thousand years." },
            { "found_tree", "This tree was old before anyone ever sailed here." },
            { "found_bamboo", "Bamboo. Light, strong, and it grows everywhere here." },
            { "found_tent", "Another camp. Tomas really did map this whole island." },
            { "ship", "A ship! Too far to see me. But they do pass this way." },
            { "monkey", "Hey! That little thief took my food." },
            { "snake", "Snake bite. I need herbs, fast." },
            { "shark", "Shark! Back to the shallows!" },
            { "pyre", "If anyone is out there, they will see this." },
            { "raid", "Something is moving out there in the dark." },
            { "meteor", "Look at that sky." },
            { "flotsam", "Something washed up on the beach." },
            { "quake", "The ground is shaking!" },
        };

        public static string Line(string key) => Lines.TryGetValue(key, out var s) ? s : "";

        public static readonly (string title, string body)[] Notes =
        {
            ("Water Source", "Coconuts saved me the first three days. Green ones have liquid inside. After that I stretched canvas between trees and caught rainwater in containers. Dew collects on broad leaves at dawn. Boil everything if you can. I got sick once and lost two days."),
            ("Fire and Animals", "Friction fire took me six hours the first time. Dry grass, soft wood, harder wood. Keep tinder bundled and protected. The boars come out at dusk. They're aggressive if cornered. They won't come near fire. I keep it burning at night. Cooked meat lasts longer and won't make you sick."),
            ("Camp Location", "I built my camp on the east ridge where the breeze keeps insects down. Shelter frame from branches, roof from palm fronds, overlapped tight. I made an axe by lashing a sharp stone to hardwood. It's still here. The work kept me sane those first weeks."),
            ("Raft Design", "I've gathered twelve logs, each about two meters, lashed side by side. Rope from vines works but frays. Real rope would be better. A canvas sail and basic rudder for steering. The current runs west in the mornings. I've tested the frame. It holds my weight. Soon."),
            ("Final Entry", "A ship passed close today. They saw my signal fire and sent a boat. I'm leaving the tools and this raft nearly finished. If you're reading this, everything you need is here. Stay calm, work steady, and you'll get off too. Tomas Reyes."),
        };
    }
}
