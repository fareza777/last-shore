var gm = LastShore.GameManager.I; int rend = 0;
foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) if (r.isVisible) rend += r.sharedMaterials.Length;
Debug.Log("PERF visibleDrawSubmeshes=" + rend + " fps=" + (1f / Time.smoothDeltaTime).ToString("0") + " quality=" + LastShore.GameSettings.Quality);
