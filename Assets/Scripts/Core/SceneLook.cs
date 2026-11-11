using UnityEngine;

namespace LastShore
{
    /// <summary>Fixed look for non-gameplay scenes (menu backdrop, intro): feeds the shader globals the water and particles read.</summary>
    public class SceneLook : MonoBehaviour
    {
        public Color Horizon = new Color(0.9f, 0.6f, 0.42f);
        [Range(0, 1)] public float Day = 0.6f;
        void OnEnable() { Shader.SetGlobalVector("_LS_ShoreParams", new Vector4(0.001f, 0, 16, 0)); Apply(); }
        void Update() { Apply(); }
        void Apply() { Shader.SetGlobalColor("_LS_Horizon", Horizon); Shader.SetGlobalFloat("_LS_Day", Day); }
    }

}
