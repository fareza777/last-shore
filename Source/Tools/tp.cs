var gm = LastShore.GameManager.I; LastShore.Landmark lm = null;
foreach (var l in LastShore.Landmark.All) if (l.Id == "lookout") lm = l;
Vector3 c = lm.transform.position; Vector3 dir = (c - LastShore.World.Center); dir.y = 0; dir.Normalize();
Vector3 p = c + Quaternion.Euler(0, 120f, 0) * dir * 14f; p.y = LastShore.World.Height(p) + 0.2f;
Vector3 look = c - p; gm.Player.Teleport(p, Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg);
gm.Sky.TimeOfDay = 0.42f; if (gm.Inv.Count("compass") == 0) gm.Inv.Add("compass", 1);
