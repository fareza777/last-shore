// Surface shader for every low-poly model. One shared material: the colour and baked ambient occlusion come from vertex colours
// (so a whole tree is a single draw call), vertex alpha is the wind weight, and lighting is a cheap Lambert with URP shadows.
Shader "LastShore/Lit"
{
    Properties
    {
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _Emission ("Emission", Color) = (0, 0, 0, 0)
        _Detail ("Detail grain", Range(0, 1)) = 0.35
        _DetailScale ("Detail scale", Float) = 0.9
        _WindAmount ("Wind amount", Range(0, 3)) = 1
        _VertexColor ("Use vertex colour", Range(0, 1)) = 1
        _DetailTex ("Detail noise", 2D) = "gray" {}
        [Enum(Off, 0, Front, 1, Back, 2)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_DetailTex); SAMPLER(sampler_DetailTex);
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _Emission;
            float4 _DetailTex_ST;
            half _Detail;
            half _DetailScale;
            half _WindAmount;
            half _VertexColor;
            half _Cull;
        CBUFFER_END
        float _LS_Wind;
        float _LS_Lite;     // 1 on the Low quality tier: no grain texture

        float3 ApplyWind(float3 positionWS, float3 positionOS, float weight)
        {
            float w = weight * _WindAmount * saturate(positionOS.y * 0.16 + 0.04);
            float phase = positionWS.x * 0.31 + positionWS.z * 0.23;
            float s = sin(_Time.y * 1.3 + phase) + 0.5 * sin(_Time.y * 2.9 + phase * 1.7);
            positionWS.xz += float2(0.8, 0.6) * (s * w * (0.10 + _LS_Wind * 0.38));
            return positionWS;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                half3 albedo : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                half3 ambient : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 ws = ApplyWind(TransformObjectToWorld(v.positionOS.xyz), v.positionOS.xyz, v.color.a);
                o.positionWS = ws;
                o.positionCS = TransformWorldToHClip(ws);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionOS = v.positionOS.xyz;
                // vertex colours are stored sRGB for precision: convert to linear here
                half3 c = v.color.rgb;
                half3 lin = c * (c * (c * 0.305306011h + 0.682171111h) + 0.012522878h);
                o.albedo = _BaseColor.rgb * lerp(half3(1, 1, 1), lin, _VertexColor);
                o.ambient = SampleSH(o.normalWS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half face = IS_FRONT_VFACE(facing, 1.0, -1.0);
                half3 n = normalize(i.normalWS) * face;
                half3 albedo = i.albedo;
                if (_LS_Lite < 0.5)
                {
                    half3 bw = abs(n); bw /= (bw.x + bw.y + bw.z);
                    float3 p = i.positionOS * _DetailScale;
                    half grain = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, p.zy).r * bw.x
                               + SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, p.xz).r * bw.y
                               + SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, p.xy).r * bw.z;
                    albedo *= lerp(1.0, 0.62 + grain * 0.76, _Detail);
                }

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half3 ambient = face > 0 ? i.ambient : i.ambient * 0.8;
                half3 lit = ambient + mainLight.color * (saturate(dot(n, mainLight.direction)) * mainLight.shadowAttenuation);
            #ifdef _ADDITIONAL_LIGHTS
                uint count = GetAdditionalLightsCount();
                for (uint li = 0u; li < count; ++li)
                {
                    Light l = GetAdditionalLight(li, i.positionWS);
                    lit += l.color * (saturate(dot(n, l.direction)) * l.distanceAttenuation);
                }
            #endif
                half3 c = albedo * lit + _Emission.rgb;
                c = MixFog(c, i.fogFactor);
                return half4(c, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 ws = ApplyWind(TransformObjectToWorld(v.positionOS.xyz), v.positionOS.xyz, v.color.a);
                float3 nws = TransformObjectToWorldNormal(v.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 l = normalize(_LightPosition - ws);
            #else
                float3 l = _LightDirection;
            #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(ws, nws, l));
            #if UNITY_REVERSED_Z
                cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
            #else
                cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                o.positionCS = cs;
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 ws = ApplyWind(TransformObjectToWorld(v.positionOS.xyz), v.positionOS.xyz, v.color.a);
                o.positionCS = TransformWorldToHClip(ws);
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
