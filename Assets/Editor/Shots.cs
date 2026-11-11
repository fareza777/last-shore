using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LastShore.EditorTools
{
    /// <summary>Development helper: renders a still from any viewpoint to a PNG (used to review the scenes).</summary>
    public static class Shots
    {
        public static void Scene(float px, float py, float pz, float pitch, float yaw, string path, float fov = 60f)
        {
            if (!Application.isPlaying) { Shader.SetGlobalColor("_LS_Horizon", RenderSettings.fogColor); Shader.SetGlobalFloat("_LS_Day", 1f); }
            var go = new GameObject("ShotCam"); go.transform.SetPositionAndRotation(new Vector3(px, py, pz), Quaternion.Euler(pitch, yaw, 0));
            var cam = go.AddComponent<Camera>(); cam.fieldOfView = fov; cam.nearClipPlane = 0.1f; cam.farClipPlane = 1500f; cam.enabled = false;
            var d = cam.GetUniversalAdditionalCameraData(); d.renderPostProcessing = true;
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;
            cam.Render(); cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(path, tex.EncodeToJPG(85));
            cam.targetTexture = null; rt.Release();
            Object.DestroyImmediate(tex); Object.DestroyImmediate(go);
        }

        /// <summary>Ground height at a world position in the open scene.</summary>
        public static float GroundY(float x, float z)
        {
            var t = Terrain.activeTerrain;
            return t != null ? t.SampleHeight(new Vector3(x, 0, z)) + t.transform.position.y : 0;
        }
    }
}
