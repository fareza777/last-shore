Shader "LastShore/Ghost"
{
    Properties { _Color ("Colour", Color) = (0.4, 1, 0.5, 0.45) }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct v2f { float4 pos : SV_POSITION; float3 n : TEXCOORD0; float3 v : TEXCOORD1; };
            v2f vert(appdata_base v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.v = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float rim = 1.0 - abs(dot(normalize(i.n), normalize(i.v)));
                fixed4 c = _Color; c.a = saturate(c.a * (0.55 + rim * 1.2) * (0.85 + 0.15 * sin(_Time.y * 5.0)));
                return c;
            }
            ENDCG
        }
    }
}
