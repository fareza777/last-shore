// Stylised ocean. Two drifting samples of a baked wave-normal texture give organic, non-repeating waves cheaply
// (important on phones); colour is graded by depth, the sky reflects with fresnel, the sun glitters, and foam rolls onto the shore.
Shader "LastShore/Water"
{
    Properties
    {
        _Shallow ("Shallow colour", Color) = (0.20, 0.82, 0.78, 0.45)
        _Deep ("Deep colour", Color) = (0.02, 0.19, 0.38, 0.96)
        _Foam ("Foam colour", Color) = (1, 1, 1, 1)
        _DepthRange ("Depth range", Float) = 6
        _FoamRange ("Foam range", Float) = 0.8
        _WaveHeight ("Wave height", Float) = 0.07
        _WaveLen ("Wave length", Float) = 46
        _WaveSpeed ("Wave speed", Float) = 1
        _Gloss ("Gloss", Float) = 260
        _Ripple ("Ripple strength", Float) = 1
        _NoiseTex ("Wave normal (RG) + height (B)", 2D) = "bump" {}
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _LS_ShoreMap;       // terrain height near sea level, baked by the world builder
            float4 _LS_ShoreParams;       // x = 1 / world size, y = lowest height, z = height range, w = 1 when a map is set
            sampler2D _NoiseTex;
            float4 _MainLightPosition;
            float4 _MainLightColor;
            float4 _LS_Horizon;
            float _LS_Day;
            fixed4 _Shallow, _Deep, _Foam;
            float _DepthRange, _FoamRange, _WaveHeight, _WaveLen, _WaveSpeed, _Gloss, _Ripple;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 wpos : TEXCOORD0;
                float4 screen : TEXCOORD1;
                float4 uv : TEXCOORD2;      // two scrolling wave layers
                UNITY_FOG_COORDS(3)
            };

            float wave(float2 p, float2 dir, float len, float speed)
            {
                return sin(dot(p, dir) * 6.2831853 / len + _Time.y * speed * _WaveSpeed);
            }

            float swell(float2 p)
            {
                return (wave(p, normalize(float2(1.0, 0.35)), _WaveLen, 1.1)
                      + 0.6 * wave(p, normalize(float2(-0.45, 1.0)), _WaveLen * 0.57, 1.7)) * _WaveHeight;
            }

            v2f vert(appdata_base v)
            {
                v2f o;
                float3 w = mul(unity_ObjectToWorld, v.vertex).xyz;
                w.y += swell(w.xz);
                o.wpos = w;
                o.pos = mul(UNITY_MATRIX_VP, float4(w, 1.0));
                o.screen = ComputeScreenPos(o.pos);
                o.screen.z = -mul(UNITY_MATRIX_V, float4(w, 1.0)).z;
                float t = _Time.y * _WaveSpeed;
                o.uv.xy = w.xz * 0.021 + float2(0.011, 0.007) * t;
                o.uv.zw = w.xz * 0.073 + float2(-0.023, 0.017) * t;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 a = tex2D(_NoiseTex, i.uv.xy);
                fixed4 b = tex2D(_NoiseTex, i.uv.zw);
                float dist = i.screen.z;
                float fine = saturate(70.0 / (dist + 1.0));
                float2 slope = (a.rg * 2.0 - 1.0) * 0.75 + (b.rg * 2.0 - 1.0) * 0.6 * fine;
                float h0 = a.b * 0.6 + b.b * 0.4;
                float amp = _Ripple * (0.55 * saturate(120.0 / (dist + 1.0)) + 0.12);
                float3 n = normalize(float3(-slope.x * amp, 1.0, -slope.y * amp));
                float3 v = normalize(_WorldSpaceCameraPos - i.wpos);
                if (v.y < 0) n = -n;

                float ground = tex2D(_LS_ShoreMap, i.wpos.xz * _LS_ShoreParams.x).r * _LS_ShoreParams.z + _LS_ShoreParams.y;
                // the waterline breathes in and out so the surf washes up the sand
                float depth = _LS_ShoreParams.w > 0.5 ? max(0, i.wpos.y - ground + sin(_Time.y * 0.9 + b.b * 4.0) * 0.07) : 30.0;
                float d = saturate(depth / _DepthRange);
                float day = saturate(_LS_Day);

                // ---- body colour: turquoise shallows to deep blue, light patterns near the shore, bright crests
                float3 mid = lerp(_Shallow.rgb, _Deep.rgb, 0.45) * float3(0.7, 1.15, 1.2);
                float3 col = d < 0.5 ? lerp(_Shallow.rgb, mid, d * 2.0) : lerp(mid, _Deep.rgb, d * 2.0 - 1.0);
                col += _Shallow.rgb * smoothstep(0.30, 0.52, a.b * b.b * 2.0) * (1.0 - d) * 0.5;
                col += _Shallow.rgb * saturate(h0 - 0.58) * 0.8 * (0.35 + 0.65 * d);
                col *= 0.10 + 0.90 * day;

                // ---- sky reflection, stronger at grazing angles
                float3 r = reflect(-v, n);
                float3 zenith = float3(0.16, 0.40, 0.80) * day + _LS_Horizon.rgb * 0.12;
                float3 sky = lerp(lerp(_LS_Horizon.rgb, zenith, 0.45), zenith, saturate(r.y * 1.7));
                float fres = min(0.62, 0.025 + 0.975 * pow(1.0 - saturate(dot(n, v)), 5.0));
                col = lerp(col, sky, fres);
                float alpha = lerp(lerp(_Shallow.a, _Deep.a, d), 1.0, fres);

                // ---- sun: soft sheen plus sharp glitter
                float3 l = normalize(_MainLightPosition.xyz);
                float nh = saturate(dot(n, normalize(l + v)));
                col += (pow(nh, _Gloss * 2.5) * 3.5 + pow(nh, _Gloss * 0.3) * 0.3) * _MainLightColor.rgb;

                // ---- shoreline foam: ragged edge, bands rolling in, whitecaps in rough water
                float edge = 1.0 - saturate(depth / (_FoamRange * 1.5));
                float foam = smoothstep(0.34, 0.62, edge + (b.b - 0.5) * 0.6);
                float band = sin(depth * 5.5 - _Time.y * 1.8 + b.b * 5.0) * 0.5 + 0.5;
                foam = saturate(foam + smoothstep(0.78, 0.95, band) * saturate(1.0 - depth / (_FoamRange * 4.5)) * 0.85);
                foam = saturate(foam + smoothstep(0.78, 0.92, h0) * 0.4 * saturate(_WaveHeight * 2.0));
                col = lerp(col, _Foam.rgb * (0.18 + 0.82 * day), foam * 0.9);
                alpha = max(alpha, foam * 0.95) * saturate(depth * 14.0);

                fixed4 outc = fixed4(col, alpha);
                UNITY_APPLY_FOG(i.fogCoord, outc);
                return outc;
            }
            ENDCG
        }
    }
}
