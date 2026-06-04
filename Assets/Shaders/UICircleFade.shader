Shader "UI/CircleFade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0, 0, 0, 1)
        _Radius ("Radius", Range(0, 1.5)) = 1.2
        _EdgeSoftness ("Edge Softness", Range(0, 0.2)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float _Radius;
            float _EdgeSoftness;
            sampler2D _MainTex;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centered = i.uv - 0.5;
                float dist = length(centered) * 2.0;
                float alpha = smoothstep(_Radius - _EdgeSoftness, _Radius, dist);
                fixed4 col = _Color;
                col.a *= alpha;
                return col;
            }
            ENDCG
        }
    }
}
