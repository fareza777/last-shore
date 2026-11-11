using UnityEngine;

namespace LastShore
{
    /// <summary>
    /// A curious thief. It creeps up only while you are not looking, snatches a piece of food and runs.
    /// Stare it down and it backs off; chase it or hit it and it drops what it took.
    /// </summary>
    public class Monkey : Animal
    {
        enum S { Idle, Sneak, Watch, Run }
        S _state; Vector3 _target; float _timer, _legPhase, _cooldown; string _loot;
        Transform[] _legs; Transform _tail;

        void Start()
        {
            Health = 15f;
            _legs = new[] { Deep("LegFL"), Deep("LegBR"), Deep("LegFR"), Deep("LegBL") }; _tail = Deep("Tail");
            _target = WanderTarget(12f); _timer = Random.Range(2f, 6f);
        }

        int FoodSlot()
        {
            var inv = GM.Inv; int best = -1;
            for (int i = 0; i < inv.Unlocked; i++)
                if (!inv.Slots[i].Empty && inv.Slots[i].Def.Kind == ItemKind.Food && (best < 0 || Random.value < 0.4f)) best = i;
            return best;
        }

        void RunAway()
        {
            Vector3 away = transform.position - PlayerT.position; away.y = 0;
            _target = transform.position + (Quaternion.Euler(0, Random.Range(-35f, 35f), 0) * away.normalized) * 34f;
            _state = S.Run; _timer = 9f;
        }

        void DropLoot()
        {
            if (_loot == null) return;
            Pickup.Spawn(_loot, 1, transform.position + Vector3.up * 0.3f); _loot = null;
        }

        void Update()
        {
            if (Dead || GM == null || GM.Player == null) return;
            float dt = Time.deltaTime, dist = PlayerDist;
            if (dist > 90f) { Speed = 0; return; }
            _timer -= dt; _cooldown -= dt;
            bool watched = PlayerLookingAtMe(0.55f) && dist < 16f;
            Engaged = _state != S.Idle;

            switch (_state)
            {
                case S.Idle:
                    {
                        if (Threat && _cooldown <= 0 && dist < 20f && !watched && FoodSlot() >= 0 && !Fires.Near(PlayerT.position, 4f)) { _state = S.Sneak; break; }
                        Vector3 d = _target - transform.position; d.y = 0;
                        if (_timer <= 0 || d.magnitude < 0.8f) { Speed = 0; if (_timer < -Random.Range(1f, 4f)) { _target = WanderTarget(12f); _timer = Random.Range(3f, 7f); } }
                        else if (!MoveSmart(d, 2.2f, 5f)) _target = WanderTarget(12f);
                        break;
                    }
                case S.Sneak:
                    {
                        if (!Threat || dist > 26f || FoodSlot() < 0) { _state = S.Idle; break; }
                        if (watched) { _state = S.Watch; _timer = Random.Range(1.2f, 2.5f); AudioManager.I.PlayAt("monkey_cry", transform.position, 0.5f, 0.2f); break; }
                        Vector3 d = PlayerT.position - transform.position; d.y = 0;
                        if (dist > 1.7f) MoveSmart(d, dist > 7f ? 4.6f : 3.2f, 8f);
                        else
                        {
                            int slot = FoodSlot();
                            if (slot >= 0)
                            {
                                var def = GM.Inv.Slots[slot].Def; _loot = def.Id; GM.Inv.RemoveAt(slot);
                                GM.Hud.Hint("A monkey snatched your " + def.Name + "!", 3f, true);
                                AudioManager.I.PlayAt("monkey_cry", transform.position); GM.SayOnce("monkey");
                                GM.Player.Shake(0.3f);
                            }
                            _cooldown = 60f; RunAway();
                        }
                        break;
                    }
                case S.Watch:
                    {
                        // stared at: hold still or edge away, then try again when the player turns
                        Speed = 0; Face(PlayerT.position, 6f);
                        if (dist < 6f) { Vector3 back = transform.position - PlayerT.position; MoveSmart(back, 3.5f, 2f); }
                        if (_timer <= 0) _state = watched ? S.Watch : S.Sneak;
                        if (_timer <= 0 && watched) _timer = 1.5f;
                        break;
                    }
                case S.Run:
                    {
                        Vector3 d = _target - transform.position; d.y = 0;
                        if (!MoveSmart(d, 7.2f, 9f)) _target = transform.position + Quaternion.Euler(0, 70, 0) * d.normalized * 20f;
                        if (_timer <= 0 || d.magnitude < 2f)
                        {
                            DropLoot(); Home = transform.position; _state = S.Idle; _timer = 4f; _target = WanderTarget(12f);
                        }
                        break;
                    }
            }
            SwingLegs(_legs, ref _legPhase, Speed, 4.4f, 40f);
            if (_tail != null) _tail.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 2.3f) * 14f, Mathf.Sin(Time.time * 1.7f) * 18f, 0);
        }

        protected override void OnHit(ItemDef tool, float damage, Vector3 point)
        {
            Health -= damage; Blood(point); AudioManager.I.PlayAt("monkey_cry", transform.position);
            DropLoot();
            if (Health <= 0) { StartCoroutine(DieRoutine(("meat_raw", 1))); return; }
            _cooldown = 120f; RunAway();
        }
    }
}
