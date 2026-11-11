using UnityEngine;

namespace LastShore
{
    /// <summary>
    /// Wild boar. Grazes in a sounder, squares up when it notices you, then commits to a fast straight charge you can sidestep.
    /// After a miss it skids, circles at a distance and tries again. Fire and a held torch keep it back; a hurt boar runs.
    /// </summary>
    public class Boar : Animal
    {
        enum S { Graze, Alert, Charge, Recover, Circle, Flee, Return }
        S _state; Vector3 _target, _chargeDir; float _timer, _grunt = 4f, _legPhase, _circleSide = 1; bool _hitDone, _provoked;
        Transform[] _legs;

        void Start()
        {
            Health = 70f;
            _legs = new[] { Deep("LegFL"), Deep("LegBR"), Deep("LegFR"), Deep("LegBL") };
            _target = WanderTarget(14f); _timer = Random.Range(3f, 8f);
            _circleSide = Random.value < 0.5f ? 1 : -1;
        }

        void Set(S s, float timer) { _state = s; _timer = timer; Engaged = s != S.Graze && s != S.Return; }

        protected override void OnAlert(Vector3 threat) { if (!Dead && (_state == S.Graze || _state == S.Return)) { _provoked = true; Set(S.Alert, Random.Range(0.6f, 1.4f)); } }
        protected override void OnNoise(Vector3 pos) { if (!Dead && _state == S.Graze) { _target = pos; _timer = 6f; } }

        void Update()
        {
            if (Dead || GM == null || GM.Player == null) return;
            float dt = Time.deltaTime, dist = PlayerDist;
            if (dist > 110f) { Speed = 0; return; }
            _timer -= dt;
            Vector3 toPlayer = PlayerT.position - transform.position; toPlayer.y = 0;
            bool torch = GM.Interaction.HoldingTorch;

            if (_state != S.Flee && Fires.Near(transform.position, 8f))
            {
                _target = transform.position + (transform.position - Fires.Nearest(transform.position)).normalized * 16f; Set(S.Flee, 3.5f);
            }

            switch (_state)
            {
                case S.Graze:
                    {
                        float sight = _provoked ? 30f : GM.Sky.IsNight ? 22f : 13f;
                        if (Threat && !GM.Vitals.NearFire && (SeesPlayer(sight) || HearsPlayer(15f)))
                        {
                            GM.SayOnce("boar");
                            AudioManager.I.PlayAt("boar_grunt", transform.position);
                            AlertKind(20f); Set(S.Alert, 1.1f); break;
                        }
                        Vector3 d = _target - transform.position; d.y = 0;
                        if (_timer <= 0 || d.magnitude < 1f) { Speed = 0; if (_timer < -Random.Range(1.5f, 5f)) { _target = WanderTarget(14f); _timer = Random.Range(4f, 9f); } }
                        else if (!MoveSmart(d, 1.5f, 3f)) _target = WanderTarget(14f);
                        break;
                    }
                case S.Alert:
                    Speed = 0; Face(PlayerT.position, 8f);
                    if (!Threat || dist > 40f) { Set(S.Return, 0); break; }
                    if (_timer <= 0)
                    {
                        if (torch && dist > 4f) { Set(S.Circle, Random.Range(3f, 5f)); break; }
                        Vector3 lead = PlayerT.position + GM.Player.Velocity * Mathf.Clamp(dist / 9f, 0.1f, 0.7f) - transform.position; lead.y = 0;
                        _chargeDir = lead.normalized; _hitDone = false;
                        AudioManager.I.PlayAt("charge", transform.position, 0.9f);
                        Set(S.Charge, 2.3f);
                    }
                    break;
                case S.Charge:
                    {
                        // committed: only a little steering, so a sidestep makes it miss
                        _chargeDir = Vector3.RotateTowards(_chargeDir, toPlayer.normalized, 0.55f * dt, 0f);
                        bool moved = Step(_chargeDir, 8.2f, 10f);
                        if (!_hitDone && dist < 1.7f && Threat)
                        {
                            _hitDone = true;
                            GM.Vitals.Damage(Random.Range(15f, 22f), "a wild boar");
                            Set(S.Recover, 0.9f);
                        }
                        else if (!moved || _timer <= 0 || Vector3.Dot(_chargeDir, toPlayer) < -3f) Set(S.Recover, 1.2f);
                        break;
                    }
                case S.Recover:
                    Speed = Mathf.MoveTowards(Speed, 0, dt * 14f);
                    if (_timer <= 0)
                    {
                        if (Health < 22f) { _target = transform.position - toPlayer.normalized * 30f; Set(S.Flee, 7f); }
                        else if (Threat && dist < 26f && !GM.Vitals.NearFire) Set(S.Circle, Random.Range(1.6f, 3.4f));
                        else Set(S.Return, 0);
                    }
                    break;
                case S.Circle:
                    {
                        if (!Threat || dist > 38f || GM.Vitals.NearFire || !World.IsLand(PlayerT.position, -0.3f)) { _provoked = false; Set(S.Return, 0); break; }
                        float want = torch ? 9f : 6.5f;
                        Vector3 tangent = Vector3.Cross(Vector3.up, toPlayer.normalized) * _circleSide;
                        Vector3 dir = tangent + toPlayer.normalized * Mathf.Clamp((dist - want) * 0.5f, -1.2f, 1.2f);
                        if (!MoveSmart(dir, 3.6f, 6f)) _circleSide = -_circleSide;
                        if (_timer <= 0 && !(torch && dist > 4f)) Set(S.Alert, 0.55f);
                        else if (_timer <= 0) _timer = 2.5f;
                        break;
                    }
                case S.Flee:
                    if (_timer <= 0) { Set(S.Return, 0); break; }
                    if (!MoveSmart(_target - transform.position, 5.6f)) _target = transform.position + Quaternion.Euler(0, 90, 0) * (_target - transform.position);
                    break;
                case S.Return:
                    {
                        Vector3 d = Home - transform.position; d.y = 0;
                        if (d.magnitude < 4f || !MoveSmart(d, 2.6f, 4f)) { _target = WanderTarget(14f); Set(S.Graze, Random.Range(3f, 7f)); }
                        break;
                    }
            }

            _grunt -= dt;
            if (_grunt <= 0 && dist < 28f) { _grunt = Random.Range(6f, 14f); AudioManager.I.PlayAt("boar_grunt", transform.position, 0.45f, 0.2f); }
            SwingLegs(_legs, ref _legPhase, Speed, 3.4f, 34f);
        }

        protected override void OnHit(ItemDef tool, float damage, Vector3 point)
        {
            Health -= damage; _provoked = true;
            Blood(point); AudioManager.I.PlayAt("boar_squeal", transform.position, 0.9f, 0.15f);
            Vector3 push = transform.position - PlayerT.position; push.y = 0;
            Vector3 np = transform.position + push.normalized * 0.8f;
            if (CanStand(np)) { np.y = World.Height(np); transform.position = np; }
            AlertKind(24f);
            if (Health <= 0) { StopAllCoroutines(); StartCoroutine(DieRoutine(("meat_raw", 2), ("hide", 1), ("bone", 1))); return; }
            // a spear set against the charge stops it cold
            if (_state == S.Charge) Set(S.Recover, tool != null && tool.Tool == ToolType.Spear ? 1.6f : 0.7f);
            else if (_state == S.Graze || _state == S.Return) Set(S.Alert, 0.4f);
        }
    }
}
