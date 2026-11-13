// A thin dark film just above sea level: it only shows where the ground lies between the waterline and the film,
// which reads as sand darkened by the surf. Ground height comes from the baked shore map, like the water.
Shader "LastShore/WetSand"
{
    Properties
    {
        _Strength ("Strength", Range(0, 1)) = 0.6
        _Height ("Film height above sea", Float) = 0.4
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _LS_ShoreMap;
            float4 _LS_ShoreParams;
            float _Strength, _Height;

            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = mul(UNITY_MATRIX_VP, float4(o.wpos, 1.0));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float ground = tex2D(_LS_ShoreMap, i.wpos.xz * _LS_ShoreParams.x).r * _LS_ShoreParams.z + _LS_ShoreParams.y;
                float above = ground - (i.wpos.y - _Height);            // ground height over the sea surface
                float a = smoothstep(-0.06, 0.03, above) * saturate((_Height - above) / (_Height * 0.6));
                a *= _LS_ShoreParams.w * saturate((90.0 - distance(i.wpos, _WorldSpaceCameraPos)) / 30.0);
                clip(a - 0.004);
                return fixed4(0.07, 0.055, 0.03, a * _Strength);
            }
            ENDCG
        }
    }
}
