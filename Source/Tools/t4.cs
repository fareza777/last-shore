var gm = LastShore.GameManager.I; var inv = gm.Inv;
var site = UnityEngine.Object.FindFirstObjectByType<LastShore.RaftSite>();
gm.Player.Teleport(site.transform.position + new Vector3(-4, 1, 0), 90);
inv.Add("log", 12); inv.Add("rope", 6); inv.Add("canvas", 1); inv.Add("meat_cooked", 3); inv.Add("flask_full", 2);
site.Interact();
Debug.Log("RAFT stage=" + gm.RaftStage + " quest=" + gm.Quests.Stage + " prompt=" + site.Prompt());
site.Interact();
Debug.Log("ENDING active=" + LastShore.EndingCinematic.Active);
