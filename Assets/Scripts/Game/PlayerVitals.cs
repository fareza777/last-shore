using System;
using UnityEngine;

namespace LastShore
{
    public class PlayerVitals : MonoBehaviour
    {
        public float Health = 100, Hunger = 100, Thirst = 100, Stamina = 100, Warmth = 100;
        public bool Exhausted { get; private set; }
        public bool Sheltered { get; private set; }
        public bool NearFire { get; private set; }
        public bool Wet { get; private set; }
        public float PoisonTime { get; private set; }
        public void Poison(float seconds) { PoisonTime = Mathf.Max(PoisonTime, seconds); }
        public void SetPoison(float seconds) { PoisonTime = seconds; }
        public event Action<string> Died;
        public event Action<float> Damaged;
        public event Action<Vector3> HitFrom;
        /// <summary>Seconds left on the buff from a cooked meal (faster stamina and healing).</summary>
        public float WellFed { get; private set; }
        /// <summary>Seconds left on the buff from stew (the cold bites far less).</summary>
        public float Hearty { get; private set; }

        PlayerController _pc; float _check, _wetTimer; bool _lowWarned; string _lastCause = "exhaustion";

        void Awake() { _pc = GetComponent<PlayerController>(); }

        public void Set(float health, float hunger, float thirst, float stamina, float warmth)
        { Health = health; Hunger = hunger; Thirst = thirst; Stamina = stamina; Warmth = warmth; }

        public void UseStamina(float amount)
        {
            Stamina = Mathf.Max(0, Stamina - amount);
            if (Stamina <= 0) Exhausted = true;
        }

        public void Damage(float amount, string cause, bool feedback = true, Vector3? from = null)
        {
            var gm = GameManager.I;
            if (gm.Dead || gm.Cinematic || amount <= 0) return;
            Health -= amount; _lastCause = cause;
            if (feedback)
            {
                Damaged?.Invoke(amount);
                _pc.Shake(Mathf.Clamp(amount / 12f, 0.3f, 1.2f));
                AudioManager.I.Play("hurt", 0.8f);
                Haptics.Pulse(70);
                if (from.HasValue) HitFrom?.Invoke(from.Value);
            }
            if (Health <= 0) { Health = 0; Died?.Invoke(cause); }
        }

        public void Consume(ItemDef def)
        {
            if (def.CuresPoison) PoisonTime = def.Id == "antidote" ? 0 : Mathf.Max(0, PoisonTime - 28f);
            if (def.Id == "stew") { WellFed = 420f; Hearty = 420f; }
            else if (def.Id.EndsWith("_cooked")) WellFed = Mathf.Max(WellFed, 240f);
            Hunger = Mathf.Clamp(Hunger + def.Food, 0, 100);
            Thirst = Mathf.Clamp(Thirst + def.Water, 0, 100);
            if (def.Health > 0) Health = Mathf.Clamp(Health + def.Health, 0, 100);
            else if (def.Health < 0) Damage(-def.Health, "bad food");
        }

        public void Drink(float amount) { Thirst = Mathf.Clamp(Thirst + amount, 0, 100); }

        /// <summary>Back on your feet after a rewarded revive.</summary>
        public void Revive()
        {
            Health = 55;
            Hunger = Mathf.Max(Hunger, 45);
            Thirst = Mathf.Max(Thirst, 45);
            Stamina = 60;
            Warmth = Mathf.Max(Warmth, 60);
            PoisonTime = 0;
            _lowWarned = false;
        }

        public void Rest(float hours)
        {
            Stamina = 100; Exhausted = false;
            Health = Mathf.Min(100, Health + hours * 5f);
            Hunger = Mathf.Max(5, Hunger - hours * 2.5f); Thirst = Mathf.Max(5, Thirst - hours * 3f);
            Warmth = Mathf.Max(Warmth, 70);
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm.Dead || gm.Cinematic) return;
            float dt = Time.deltaTime;

            _check -= dt;
            if (_check <= 0)
            {
                _check = 0.4f;
                Sheltered = Shelter.AnyNear(transform.position, 3.2f);
                NearFire = Campfire.AnyLitNear(transform.position, 5.5f);
            }
            bool raining = gm.Weather.Raining && !Sheltered;
            if (raining || _pc.Swimming) _wetTimer = 45f; else _wetTimer -= dt * (NearFire ? 4f : 1f);
            Wet = _wetTimer > 0;

            // the first two days are forgiving while the player learns where food and water are
            float needs = (gm.Sky.Day <= 1 ? 0.6f : gm.Sky.Day == 2 ? 0.8f : 1f) * Camp.NeedsMul;
            Hunger = Mathf.Max(0, Hunger - dt * (100f / 900f) * (_pc.Sprinting ? 1.5f : 1f) * needs);
            Thirst = Mathf.Max(0, Thirst - dt * (100f / 660f) * (_pc.Sprinting ? 1.6f : 1f) * needs);
            WellFed = Mathf.Max(0, WellFed - dt); Hearty = Mathf.Max(0, Hearty - dt);

            if (_pc.Sprinting) UseStamina(dt * 11f);
            else if (_pc.Swimming) UseStamina(dt * 4.5f);
            else Stamina = Mathf.Min(100, Stamina + dt * (Hunger > 15 && Thirst > 15 ? 16f : 7f) * Camp.StaminaMul * (WellFed > 0 ? 1.35f : 1f));
            if (Exhausted && Stamina > 25) Exhausted = false;

            float warm = gm.Sky.IsNight ? -0.34f : 0.55f;
            if (raining) warm -= 0.4f;
            if (Wet) warm -= 0.15f;
            if (_pc.Swimming) warm -= 0.3f;
            if (warm < 0 && Sheltered) warm *= 0.35f;
            if (warm < 0 && Hearty > 0) warm *= 0.3f;
            if (gm.Interaction.HoldingTorch) warm += 0.2f;
            if (NearFire) warm = 3.2f;
            Warmth = Mathf.Clamp(Warmth + warm * dt, 0, 100);

            if (PoisonTime > 0) { PoisonTime -= dt; Damage(dt * 0.55f, "poison", false); }
            if (Hunger <= 0) Damage(dt * 0.7f, "starvation", false);
            if (Thirst <= 0) Damage(dt * 1.0f, "dehydration", false);
            if (Warmth <= 0) Damage(dt * 0.8f, "the cold", false);
            if (_pc.Swimming && Stamina <= 0) Damage(dt * 7f, "drowning", false);
            if (Hunger > 40 && Thirst > 40 && Warmth > 30 && Health < 100) Health = Mathf.Min(100, Health + dt * (WellFed > 0 ? 1.1f : 0.55f));

            if (Health < 25 && !_lowWarned) { _lowWarned = true; AudioManager.I.Say("low_health", Story.Line("low_health")); }
            if (Health > 50) _lowWarned = false;
            AudioManager.I.SetLayer("heartbeat", Health < 25 ? 0.8f : 0f);
        }
    }
}
