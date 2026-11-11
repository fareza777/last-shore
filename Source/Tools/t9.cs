var gm = LastShore.GameManager.I; gm.InvUI.Close();
LastShore.GameInput.ForceTouch = true; LastShore.TouchControls.Create();
LastShore.Landmark lm = null; foreach (var l in LastShore.Landmark.All) if (l.Id == "lookout") lm = l;
gm.Player.Teleport(lm.transform.position + new Vector3(0, 6.4f, 0), 250); gm.Vitals.Set(100, 80, 70, 100, 90);
