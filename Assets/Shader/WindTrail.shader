Shader "Custom/WindTrailSimple"
{
    Properties
    {
        _MainTex ("Texture (RGBA)", 2D) = "white" {}
        _WindDirection ("Wind Direction", Vector) = (1,0,0,0)
        _WindStrength ("Wind Strength", Float) = 0.15
        _WindSpeed ("Wind Speed", Float) = 1.0
        _Frequency ("Frequency", Float) = 1.0
        _Width ("Max Width", Float) = 0.25
        _Color ("Tint Color", Color) = (1,1,1,0.8)
        _TailFade ("Tail Fade (0..2)", Float) = 1.2
        _EdgeSoft ("Edge Softness", Float) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _WindDirection;
            float _WindStrength;
            float _WindSpeed;
            float _Frequency;
            float _Width;
            float4 _Color;
            float _TailFade;
            float _EdgeSoft;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0; // uv.x along trail (0..1), uv.y across width (-1..1)
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float  trail      : TEXCOORD1;
                float  lateral    : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // world-space position
                float3 worldPos = TransformObjectToWorld(IN.positionOS).xyz;

                // trail progression and lateral coords
                float trail = saturate(IN.uv.x);
                float lateral = IN.uv.y;

                OUT.trail = trail;
                OUT.lateral = lateral;
                OUT.uv = IN.uv;

                // simple wind direction in world space
                float3 windDir = normalize(_WindDirection.xyz);

                // compute a small displacement along wind direction
                float t = _Time.y;
                float phase = dot(worldPos, windDir) * _Frequency + t * _WindSpeed;
                float disp = sin(phase) * _WindStrength * (1.0 - trail); // stronger near origin

                // lateral width (mesh should have uv.y in -1..1)
                float lateralOffset = lateral * _Width * (1.0 - 0.4 * trail); // slightly taper toward tail

                float3 offset = windDir * disp + normalize(cross(float3(0,1,0), windDir)) * lateralOffset;

                float3 displaced = worldPos + offset;
                OUT.positionCS = TransformWorldToHClip(displaced);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // Texture's alpha acts as base shape. If you have a white texture with alpha gradient,
                // it will help create soft streaks. Otherwise a white texture with alpha = 1 works too.
                float baseAlpha = tex.a;

                // tail fade: stronger fade toward tail (trail=1)
                float tail = pow(saturate(1.0 - IN.trail), _TailFade);

                // lateral softness: assume uv.y in -1..1; produce highest alpha at center
                float lat = 1.0 - abs(IN.lateral); // 1 at center, 0 at edges
                lat = pow(saturate(lat), _EdgeSoft);

                // combine alpha
                float alpha = _Color.a * baseAlpha * tail * lat;

                float3 col = tex.rgb * _Color.rgb;

                // premultiplied style output
                return half4(col * alpha, alpha);
            }

            ENDHLSL
        }
    }

    FallBack "Unlit/Transparent"
}
