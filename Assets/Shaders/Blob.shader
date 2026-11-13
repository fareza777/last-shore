// Soft round contact shadow drawn as an instanced quad under trees, rocks and animals when real shadows are off.
Shader "LastShore/Blob"
{
    Properties
    {
        _Strength ("Strength", Range(0, 1)) = 0.4
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-20" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -2, -2

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            float _Strength;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float fade : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 w = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = mul(UNITY_MATRIX_VP, float4(w, 1.0));
                o.uv = v.uv * 2.0 - 1.0;
                o.fade = saturate((62.0 - distance(w, _WorldSpaceCameraPos)) / 14.0);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float a = 1.0 - smoothstep(0.1, 1.0, length(i.uv));
                return fixed4(0.02, 0.035, 0.05, a * _Strength * i.fade);
            }
            ENDCG
        }
    }
}
