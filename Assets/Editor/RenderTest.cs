using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastShore.EditorTools
{
    /// <summary>Development helper: checks in batch mode that the blob shadow and wet sand materials actually render.</summary>
    public static class RenderTest
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
            var shore = Resources.Load<Texture2D>("World/ShoreMap");
            Shader.SetGlobalTexture("_LS_ShoreMap", shore);
            Shader.SetGlobalVector("_LS_ShoreParams", new Vector4(1f / World.Size, World.SeaLevel - World.ShoreLow, World.ShoreRange, 1f));
            var spawn = GameObject.Find("PlayerSpawn").transform.position;

            // a blob quad on the ground just inland of the spawn point
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Vector3 p = spawn + new Vector3(0, 0, 6f); p.y = Shots.GroundY(p.x, p.z) + 0.12f;
            quad.transform.SetPositionAndRotation(p, Quaternion.Euler(90, 0, 0)); quad.transform.localScale = Vector3.one * 5f;
            quad.GetComponent<Renderer>().sharedMaterial = Resources.Load<Material>("Materials/Blob");

            // the wet sand film
            var sea = GameObject.Find("Sea");
            var film = Object.Instantiate(sea); film.transform.position = sea.transform.position + Vector3.up * 0.4f;
            film.GetComponent<Renderer>().sharedMaterial = Resources.Load<Material>("Materials/WetSand");

            quad.GetComponent<Renderer>().sharedMaterial.SetFloat("_Strength", 0.62f);
            Shots.Scene(spawn.x, spawn.y + 2.2f, spawn.z - 2f, 22f, 0f, "Source/Shots/test_blob.jpg");
            ProjectSetup.BuildExtras();
            Shots.Scene(spawn.x + 3f, spawn.y + 1.8f, spawn.z - 6f, 32f, 180f, "Source/Shots/test_wet.jpg");
            Debug.Log("[LastShore] Render test done. spawn=" + spawn);
        }
    }
}
