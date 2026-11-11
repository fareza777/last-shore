var gm = LastShore.GameManager.I;
LastShore.GameInput.ForceTouch = true; LastShore.TouchControls.Create();
gm.Player.Teleport(LastShore.World.OnGround(new Vector3(250, 0, 62)) + Vector3.up * 0.2f, 180);
gm.Inv.Add("axe", 1); gm.Inv.Add("coconut", 2); gm.Interaction.Select(0);
Debug.Log("TOUCH ok");
