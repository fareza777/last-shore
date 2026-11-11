var gm = LastShore.GameManager.I; LastShore.Landmark lm = null;
foreach (var l in LastShore.Landmark.All) if (l.Id == "shore") lm = l;
Vector3 p = lm.transform.position + new Vector3(6, 0, -5); p.y = LastShore.World.Height(p) + 0.2f;
gm.Player.Teleport(p, 150); gm.Sky.TimeOfDay = 0.44f;
LastShore.GameInput.ForceTouch = true; LastShore.TouchControls.Create();
