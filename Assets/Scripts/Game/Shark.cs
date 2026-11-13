using UnityEngine;

namespace LastShore
{
    /// <summary>Patrols the deep water. Circles a swimmer before closing in; it cannot follow into the shallows.</summary>
    public class Shark : Animal
    {
        public override bool Hostile => true;
        enum S { Patrol, Stalk, Attack, Retreat }
        S _state; Vector3 _target; float _timer, _orbit; int _side = 1;
        Transform _tail;

        void Start() { Health = 130f; _tail = Deep("Tail"); _target = PatrolPoint(); _side = Random.value < 0.5f ? 1 : -1; }

        protected override bool CanStand(Vector3 p) => World.Height(p) < World.SeaLevel - 1.7f;

        Vector3 PatrolPoint()
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 r = Random.insideUnitCircle * 70f; Vector3 p = Home + new Vector3(r.x, 0, r.y);
                if (CanStand(p)) return p;
            }
            return Home;
        }

        bool Swim(Vector3 dir, float speed, float turn)
        {
            dir.y = 0; if (dir.sqrMagnitude < 0.001f) return true; dir.Normalize();
            Vector3 next = transform.position + dir * speed * Time.deltaTime;
            if (!CanStand(next + dir * 2f)) { Speed = 0; return false; }
            next.y = World.SeaLevel - 0.45f + Mathf.Sin(Time.time * 0.8f) * 0.08f;
            transform.position = next;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * turn);
            Speed = speed; return true;
        }

        void Update()
        {
            if (Dead || GM == null || GM.Player == null) return;
            float dt = Time.deltaTime, dist = PlayerDist;
            if (dist > 160f) return;
            _timer -= dt;
            bool prey = Threat && GM.Player.Swimming && World.Height(PlayerT.position) < World.SeaLevel - 1.5f;
            Vector3 toPlayer = PlayerT.position - transform.position; toPlayer.y = 0;
            Engaged = _state != S.Patrol;

            switch (_state)
            {
                case S.Patrol:
                    {
                        if (prey && dist < 48f) { _state = S.Stalk; _timer = Random.Range(3f, 5.5f); GM.SayOnce("shark"); break; }
                        Vector3 d = _target - transform.position; d.y = 0;
                        if (d.magnitude < 4f || !Swim(d, 2.6f, 1.5f)) _target = PatrolPoint();
                        break;
                    }
                case S.Stalk:
                    {
                        // circle in, tightening the ring
                        if (!prey) { _state = S.Patrol; _target = PatrolPoint(); break; }
                        _orbit = Mathf.Lerp(14f, 6f, 1 - Mathf.Clamp01(_timer / 5f));
                        Vector3 tangent = Vector3.Cross(Vector3.up, toPlayer.normalized) * _side;
                        Vector3 dir = tangent + toPlayer.normalized * Mathf.Clamp((dist - _orbit) * 0.4f, -1f, 1.5f);
                        if (!Swim(dir, 5.2f, 3f)) _side = -_side;
                        if (_timer <= 0) { _state = S.Attack; _timer = 3f; }
                        break;
                    }
                case S.Attack:
                    {
                        if (!prey) { _state = S.Patrol; _target = PatrolPoint(); break; }
                        Swim(toPlayer, 7.5f, 5f);
                        if (dist < 2.2f)
                        {
                            GM.Vitals.Damage(Random.Range(22f, 30f), "a shark", true, transform.position); AudioManager.I.Play("bite", 1f);
                            FX.Burst(PlayerT.position + Vector3.up, new Color(0.9f, 0.95f, 1f, 0.8f), 20, 0.12f, 4f);
                            _target = transform.position - toPlayer.normalized * 25f; _state = S.Retreat; _timer = 4.5f;
                        }
                        else if (_timer <= 0) { _state = S.Stalk; _timer = 3f; }
                        break;
                    }
                case S.Retreat:
                    {
                        Vector3 d = _target - transform.position; d.y = 0;
                        if (_timer <= 0 || !Swim(d, 6f, 3f)) { _state = prey ? S.Stalk : S.Patrol; _timer = 3.5f; _target = PatrolPoint(); }
                        break;
                    }
            }
            if (_tail != null) _tail.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * (2f + Speed)) * 24f, 0);
        }

        protected override void OnHit(ItemDef tool, float damage, Vector3 point)
        {
            Health -= damage; Blood(point);
            if (Health <= 0) { StartCoroutine(DieRoutine(("meat_raw", 4), ("bone", 2))); return; }
            _target = transform.position + (transform.position - PlayerT.position).normalized * 40f; _state = S.Retreat; _timer = 9f;
        }
    }
}
