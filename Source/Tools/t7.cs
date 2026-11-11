var gm = LastShore.GameManager.I; var p = gm.Player.transform;
LastShore.Landmark lm = null; foreach (var l in LastShore.Landmark.All) if (l.Id == "tree") lm = l;
Vector3 c = lm.transform.position + new Vector3(18, 0, -14); c.y = LastShore.World.Height(c) + 0.2f;
gm.Player.Teleport(c, 300);
string[] kinds = { "Deer", "Boar", "Monkey", "Fowl", "Turtle", "Snake" };
for (int i = 0; i < kinds.Length; i++)
{
    Vector3 q = c + Quaternion.Euler(0, 300 - 32 + i * 13, 0) * Vector3.forward * (7f + (i % 2) * 2f); q.y = LastShore.World.Height(q);
    var a = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Prefabs/" + kinds[i]), q, Quaternion.Euler(0, 120 + i * 30, 0)).GetComponent<LastShore.Animal>(); a.Home = q;
}
gm.Inv.Add("bow", 1); gm.Inv.Add("arrow", 12); gm.Inv.Add("knife", 1); gm.Inv.Add("stew", 1); gm.Inv.Add("herb", 3); gm.Inv.Add("banana", 4); gm.Inv.Add("bamboo", 6); gm.Inv.Add("torch", 1);
gm.Interaction.Select(0);
Debug.Log("T7 animals=" + LastShore.Animal.All.Count);
