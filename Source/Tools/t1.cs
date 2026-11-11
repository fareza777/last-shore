var gm = LastShore.GameManager.I;
var inv = gm.Inv;
inv.Add("stick", 8); inv.Add("stone", 8); inv.Add("fiber", 8); inv.Add("coconut", 2); inv.Add("log", 6); inv.Add("leaf", 10); inv.Add("meat_raw", 2);
gm.Quests.Notify("pickup", "stick");
foreach (var r in LastShore.ItemDB.Recipes) if (r.Result == "axe" || r.Result == "campfire_kit" || r.Result == "torch") { bool ok = inv.Craft(r); gm.Quests.Notify("craft", r.Result); Debug.Log("craft " + r.Result + " " + ok); }
gm.InvUI.OpenCrafting();
Debug.Log("T1 stage=" + gm.Quests.Stage + " axe=" + inv.Count("axe") + " kit=" + inv.Count("campfire_kit") + " icons=" + (LastShore.ItemDB.Get("axe").Icon != null));
