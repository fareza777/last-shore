using UnityEngine;

namespace LastShore
{
    /// <summary>
    /// Deer live in small herds. They have a wide view and sharp ears, freeze for a moment when something is wrong,
    /// then bolt in a zigzag and take the whole herd with them. Approach slowly from behind, or use a bow.
    /// </summary>
    public class Deer : Animal
    {
        enum S { Graze, Alert, Flee, Return }
        S _state; Vector3 _target; float _timer, _zig, _legPhase; int _zigSide = 1;
        Transform[] _legs;

        void Start()
        {
            Health = 40f;
            _legs = new[] { Deep("LegFL"), Deep("LegBR"), Deep("LegFR"), Deep("LegBL") };
            _target = WanderTarget(18f); _timer = Random.Range(3f, 9f);
        }

        protected override void OnAlert(Vector3 threat) { if (!Dead && _state != S.Flee) Bolt(threat); }
        protected override void OnNoise(Vector3 pos) { if (!Dead && _state == S.Graze) { _state = S.Alert; _timer = 0.5f; } }

        void Bolt(Vector3 from)
        {
            Vector3 away = transform.position - from; away.y = 0;
            _target = transform.position + away.normalized * Random.Range(45f, 60f);
            _state = S.Flee; _timer = Random.Range(6f, 9f); Engaged = true;
        }

        void Update()
        {
            if (Dead || GM == null || GM.Player == null) return;
            float dt = Time.deltaTime, dist = PlayerDist;
            if (dist > 120f) { Speed = 0; return; }
            _timer -= dt;

            switch (_state)
            {
                case S.Graze:
                    {
                        bool sneaking = !GM.Player.Sprinting && GM.Player.Velocity.magnitude < 3f;
                        if (Threat && (SeesPlayer(sneaking ? 17f : 26f, 200f, 5f) || HearsPlayer(22f))) { _state = S.Alert; _timer = Random.Range(0.5f, 1.1f); break; }
                        Vector3 d = _target - transform.position; d.y = 0;
                        if (_timer <= 0 || d.magnitude < 1f) { Speed = 0; if (_timer < -Random.Range(2f, 7f)) { _target = WanderTarget(18f); _timer = Random.Range(4f, 10f); } }
                        else if (!MoveSmart(d, 1.3f, 3f)) _target = WanderTarget(18f);
                        break;
                    }
                case S.Alert:
                    Speed = 0; Face(PlayerT.position, 9f);
                    if (_timer <= 0)
                    {
                        if (dist < 30f) { AudioManager.I.PlayAt("deer", transform.position, 0.8f); AlertKind(26f); Bolt(PlayerT.position); }
                        else { _state = S.Graze; _timer = 3f; }
                    }
                    break;
                case S.Flee:
                    {
                        _zig -= dt; if (_zig <= 0) { _zig = Random.Range(0.6f, 1.1f); _zigSide = -_zigSide; }
                        Vector3 d = _target - transform.position; d.y = 0;
                        Vector3 dir = Quaternion.Euler(0, 26f * _zigSide, 0) * d.normalized;
                        if (!MoveSmart(dir, 9.6f, 9f)) _target = transform.position + Quaternion.Euler(0, 80 * _zigSide, 0) * d.normalized * 40f;
                        if (_timer <= 0 || d.magnitude < 3f)
                        {
                            if (dist < 28f) Bolt(PlayerT.position);
                            else { Home = transform.position; _state = S.Graze; _timer = Random.Range(4f, 8f); _target = WanderTarget(18f); Engaged = false; }
                        }
                        break;
                    }
            }
            SwingLegs(_legs, ref _legPhase, Speed, 2.6f, 38f);
        }

        protected override void OnHit(ItemDef tool, float damage, Vector3 point)
        {
            Health -= damage; Blood(point);
            AlertKind(30f);
            if (Health <= 0) { StartCoroutine(DieRoutine(("venison_raw", 2), ("hide", 1), ("bone", 1))); return; }
            AudioManager.I.PlayAt("deer", transform.position, 0.9f);
            Bolt(PlayerT.position);
        }
    }
}
