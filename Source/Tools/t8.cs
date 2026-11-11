var gm = LastShore.GameManager.I;
foreach (var l in LastShore.Landmark.All) if (l.Id != "arch" && l.Id != "tent") gm.Flags.Add("found_" + l.Id);
var ui = gm.InvUI; var m = typeof(LastShore.InventoryUI).GetMethod("ShowTabIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
m.Invoke(ui, new object[] { 2 });
