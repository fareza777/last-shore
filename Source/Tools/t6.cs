var gm = LastShore.GameManager.I; int active = 0, total = 0;
foreach (Transform g in GameObject.Find("World").transform) foreach (Transform t in g) { total++; if (t.gameObject.activeSelf) active++; }
Debug.Log("V2 world total=" + total + " active=" + active + " animals=" + LastShore.Animal.All.Count + " landmarks=" + LastShore.Landmark.All.Count + " fps=" + (1f / Time.smoothDeltaTime).ToString("0") + " font=" + LastShore.UI.TitleFont.name + "/" + LastShore.UI.BodyFont.name);
