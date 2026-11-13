// Game sky: gradient driven by the day/night cycle, a sun and a moon disc with glow, and drifting clouds from one noise texture.
// Everything comes from globals written by DayNight, so the material needs no per-frame updates.
Shader "LastShore/Sky"
{
    Properties
    {
        _NoiseTex ("Cloud noise (B)", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _NoiseTex;
            float4 _LS_Horizon, _LS_SkyTop, _LS_SunDir, _LS_SunCol, _LS_MoonDir, _LS_MoonCol, _LS_CloudCol;
            float _LS_CloudCover;

            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float up = saturate(d.y);
                float3 col = lerp(_LS_Horizon.rgb, _LS_SkyTop.rgb, pow(up, 0.5));
                col = lerp(col, _LS_Horizon.rgb * 0.82, saturate(-d.y * 6.0));

                // sun: warm halo, then the disc
                float sd = saturate(dot(d, _LS_SunDir.xyz));
                col += _LS_SunCol.rgb * (pow(sd, 9.0) * 0.22 + pow(sd, 90.0) * 0.55);
                col += _LS_SunCol.rgb * smoothstep(0.99925, 0.99965, sd) * 2.6;

                // moon: pale disc with a soft glow
                float md = saturate(dot(d, _LS_MoonDir.xyz));
                col += _LS_MoonCol.rgb * (pow(md, 220.0) * 0.35 + smoothstep(0.99945, 0.9997, md) * 1.25);

                // clouds on a flat layer overhead, thinning out toward the horizon
                float2 uv = d.xz / (d.y + 0.14);
                float t = _Time.y;
                float n = tex2D(_NoiseTex, uv * 0.11 + float2(0.004, 0.0015) * t).b * 0.62
                        + tex2D(_NoiseTex, uv * 0.31 + float2(-0.006, 0.0045) * t).b * 0.38;
                float edge = 0.66 - _LS_CloudCover * 0.34;
                float cloud = smoothstep(edge, edge + 0.2, n) * smoothstep(0.015, 0.2, d.y);
                float body = smoothstep(edge + 0.04, edge + 0.42, n);
                float3 lit = _LS_CloudCol.rgb + (_LS_SunCol.rgb * pow(sd, 5.0) + _LS_MoonCol.rgb * pow(md, 14.0)) * 0.45;
                float3 cc = lerp(lit, lit * 0.62 + _LS_Horizon.rgb * 0.1, body);
                col = lerp(col, cc, cloud * (0.55 + 0.4 * _LS_CloudCover));

                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
