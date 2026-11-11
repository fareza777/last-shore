// Writes the small v2 gameplay components (one MonoBehaviour per file, as Unity requires for serialized scripts).
import fs from 'node:fs';
const dir = 'C:/Survival Fable 62/Assets/Scripts/Game/';
const head = 'using System.Collections;\nusing System.Collections.Generic;\nusing UnityEngine;\n\nnamespace LastShore\n{\n';
const files = {
  TorchStand: `    /// <summary>A planted torch: permanent light, and animals give it room.</summary>
    public class TorchStand : PlacedStructure
    {
        public static readonly List<TorchStand> All = new List<TorchStand>();
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        protected override void Start() { base.Start(); FX.Fire(transform, new Vector3(0, 1.85f, 0), 0.4f, false, 15f); }
    }`,
  Fence: `    /// <summary>Stake fence segment. Its collider is on the Structure layer, which animals path around.</summary>
    public class Fence : PlacedStructure { }`,
  CookingPot: `    public class CookingPot : PlacedStructure, IInteractable
    {
        public static readonly List<CookingPot> All = new List<CookingPot>();
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        protected override void Start() { base.Start(); FX.Fire(transform, new Vector3(0, 0.08f, 0), 0.45f, true, 7f); }
        public string Prompt() => "Cook in the pot";
        public void Interact() { AudioManager.I.PlayAt("bubble", transform.position, 0.7f); GameManager.I.InvUI.OpenCrafting(); }
    }`,
  Bed: `    public class Bed : PlacedStructure, IInteractable
    {
        public string Prompt() => Rest.CanSleep ? "Sleep until morning" : "Leaf bed  (sleep after dusk)";
        public void Interact() { Rest.TrySleep(this, 1.5f); }
    }`,
  FishTrap: `    /// <summary>Set in the shallows; fills slowly with fish while the player is busy elsewhere.</summary>
    public class FishTrap : PlacedStructure, IInteractable
    {
        public float Fish;
        const float Max = 3f;
        void Update() { if (GameManager.I != null) Fish = Mathf.Min(Max, Fish + Time.deltaTime / 210f); }
        public string Prompt() => Fish >= 1f ? "Empty the fish trap  (" + Mathf.FloorToInt(Fish) + ")" : "Fish trap  (nothing yet)";
        public void Interact()
        {
            int n = Mathf.FloorToInt(Fish);
            if (n < 1) { GameManager.I.Hud.Hint("The trap is empty. Give it time."); return; }
            Fish -= n; AudioManager.I.Play("splash", 0.5f);
            int left = GameManager.I.Inv.Add("fish_raw", n);
            if (left > 0) Pickup.Spawn("fish_raw", left, transform.position + Vector3.up * 0.5f);
        }
        public override PlacedData ToData() { var d = base.ToData(); d.water = Fish; return d; }
        public override void FromData(PlacedData d) { Fish = d.water; }
    }`,
  SignalPyre: `    /// <summary>The great pyre at the Lookout. Once lit it burns for good and can be seen from the sea.</summary>
    public class SignalPyre : MonoBehaviour, IInteractable
    {
        public static readonly List<SignalPyre> All = new List<SignalPyre>();
        public bool Lit;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        void Start() { if (GameManager.I != null && GameManager.I.Flags.Contains("pyre_lit")) Light(false); }
        public string Prompt() => Lit ? null : "Light the signal pyre  (needs a torch or 3 sticks)";
        public void Interact()
        {
            var gm = GameManager.I;
            if (Lit) return;
            if (!gm.Interaction.HoldingTorch && !gm.Inv.Remove("stick", 3)) { gm.Hud.Hint("Bring a lit torch, or three dry sticks."); return; }
            Light(true);
        }
        void Light(bool fresh)
        {
            Lit = true;
            var fire = FX.Fire(transform, new Vector3(0, 0.6f, 0), 2.6f, true, 46f);
            var l = fire.GetComponentInChildren<Light>(); if (l != null) l.intensity = 5f;
            if (!fresh) return;
            var gm = GameManager.I; gm.Flags.Add("pyre_lit");
            AudioManager.I.PlayAt("roar_fire", transform.position, 1f); gm.Player.Shake(0.5f);
            gm.Hud.Banner("SIGNAL PYRE LIT", "It can be seen for miles");
            gm.SayOnce("pyre"); gm.SaveGame();
        }
    }`,
  WaterSource: `    /// <summary>Fresh water: drink your fill and top up flasks.</summary>
    public class WaterSource : MonoBehaviour, IInteractable
    {
        public string Prompt() => GameManager.I.Inv.Count("flask_empty") > 0 ? "Fill your flasks with fresh water" : "Drink fresh water";
        public void Interact()
        {
            var gm = GameManager.I; int n = gm.Inv.Count("flask_empty");
            if (n > 0) { gm.Inv.Remove("flask_empty", n); gm.Inv.Add("flask_full", n); AudioManager.I.Play("splash", 0.4f); }
            else { gm.Vitals.Drink(100f); AudioManager.I.Play("drink", 0.8f); }
            gm.Quests.Notify("consume", "water");
        }
    }`,
  Climbable: `    /// <summary>Ladder: moves the player between the bottom and the top of a tall structure.</summary>
    public class Climbable : MonoBehaviour, IInteractable
    {
        public Vector3 TopOffset = new Vector3(0, 6.2f, 0), BottomOffset = new Vector3(0, 0.2f, -2.6f);
        bool AtTop => GameManager.I.Player.transform.position.y > transform.position.y + TopOffset.y - 1.5f;
        public string Prompt() => AtTop ? "Climb down" : "Climb the ladder";
        public void Interact() { GameManager.I.StartCoroutine(Climb()); }
        IEnumerator Climb()
        {
            var gm = GameManager.I; bool up = !AtTop;
            gm.Cinematic = true; bool black = false;
            AudioManager.I.Play("ladder", 0.8f);
            SceneFader.FadeOut(0.45f, () => black = true);
            while (!black) yield return null;
            gm.Player.Teleport(transform.TransformPoint(up ? TopOffset : BottomOffset), gm.Player.Yaw);
            yield return new WaitForSeconds(0.5f);
            gm.Cinematic = false; SceneFader.FadeIn(0.5f);
        }
    }`,
  Landmark: `    /// <summary>A named place. Walking into it for the first time records the discovery and reveals it on the map.</summary>
    public class Landmark : MonoBehaviour
    {
        public static readonly List<Landmark> All = new List<Landmark>();
        public string Id, Title, Subtitle, Voice; public float Radius = 22f;
        float _t;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        public bool Found => GameManager.I != null && GameManager.I.Flags.Contains("found_" + Id);
        void Update()
        {
            _t -= Time.deltaTime; if (_t > 0) return; _t = 0.5f;
            var gm = GameManager.I; if (gm == null || gm.Player == null || gm.Cinematic || Found) return;
            if ((gm.Player.transform.position - transform.position).sqrMagnitude > Radius * Radius) return;
            gm.Flags.Add("found_" + Id);
            AudioManager.I.Play("discover", 0.8f, 0);
            gm.Hud.Banner(Title, "DISCOVERED  ·  " + gm.DiscoveredCount + " of " + All.Count + " places");
            if (!string.IsNullOrEmpty(Voice)) AudioManager.I.Say(Voice, Story.Line(Voice));
            gm.Quests.Notify("zone", Id);
            gm.SaveGame();
        }
    }`,
};
for (const [name, body] of Object.entries(files)) fs.writeFileSync(dir + name + '.cs', head + body + '\n}\n');

// ---- Rest helper (static, shared by Shelter and Bed)
fs.writeFileSync(dir + 'Rest.cs', head + `    /// <summary>Sleeping through the night.</summary>
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
`);

// ---- patch Shelter to use Rest, Stations to know the pot
let s = fs.readFileSync(dir + 'Shelter.cs', 'utf8');
const a = s.indexOf('        public string Prompt()');
s = s.slice(0, a) + `        public string Prompt() => Rest.CanSleep ? "Sleep until morning" : "Rest  (you can sleep after dusk)";
        public void Interact() { Rest.TrySleep(this, 1f); }
    }
}
`;
fs.writeFileSync(dir + 'Shelter.cs', s);
let st = fs.readFileSync(dir + 'Interactables.cs', 'utf8');
st = st.replace('            if (s == Station.Campfire) return Campfire.AnyLitNear(pos, 4.5f);', `            if (s == Station.Campfire) return Campfire.AnyLitNear(pos, 4.5f);
            if (s == Station.Pot) { foreach (var p in CookingPot.All) if ((p.transform.position - pos).sqrMagnitude < 4.5f * 4.5f) return true; return false; }`)
  .replace('public static string Name(Station s) => s == Station.Campfire ? "a lit campfire" : "a workbench";', 'public static string Name(Station s) => s == Station.Campfire ? "a lit campfire" : s == Station.Pot ? "a cooking pot" : "a workbench";');
if (!st.includes('Bamboo')) st = st.replace('public enum NodeType { Palm, Tree, Rock, FlintRock, Bush, Fiber, Fern }', 'public enum NodeType { Palm, Tree, Rock, FlintRock, Bush, Fiber, Fern, Bamboo, Banana, Herb, DeadTree, Reed }');
fs.writeFileSync(dir + 'Interactables.cs', st);
console.log('written', Object.keys(files).length + 1, st.includes('Station.Pot'), s.includes('Rest.TrySleep'));
