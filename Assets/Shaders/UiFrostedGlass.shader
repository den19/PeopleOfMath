Shader "PeopleOfMath/UiFrostedGlass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _GlassTint ("Glass Tint", Color) = (1, 1, 1, 0.14)
        _BlurSize ("Blur Size", Range(0, 4)) = 1.5
        _ColorMask ("Color Mask", Float) = 15
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _GlassBackdropTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _GlassBackdropTex_TexelSize;
            fixed4 _GlassTint;
            float _BlurSize;
            float4 _ClipRect;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 SampleBlurredBackdrop(float2 screenUv)
            {
                float2 texel = _GlassBackdropTex_TexelSize.xy * _BlurSize;
                fixed4 c = tex2D(_GlassBackdropTex, screenUv);
                c += tex2D(_GlassBackdropTex, screenUv + float2(texel.x, 0));
                c += tex2D(_GlassBackdropTex, screenUv + float2(-texel.x, 0));
                c += tex2D(_GlassBackdropTex, screenUv + float2(0, texel.y));
                c += tex2D(_GlassBackdropTex, screenUv + float2(0, -texel.y));
                c += tex2D(_GlassBackdropTex, screenUv + float2(texel.x, texel.y));
                c += tex2D(_GlassBackdropTex, screenUv + float2(-texel.x, -texel.y));
                c += tex2D(_GlassBackdropTex, screenUv + float2(texel.x, -texel.y));
                c += tex2D(_GlassBackdropTex, screenUv + float2(-texel.x, texel.y));
                return c * 0.1111;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 screenUv = i.screenPos.xy / i.screenPos.w;

                fixed4 sprite = tex2D(_MainTex, i.uv);
                fixed4 backdrop = SampleBlurredBackdrop(screenUv);
                if (backdrop.a <= 0.001)
                    backdrop = fixed4(0.239, 0.082, 0.471, 1);

                fixed4 frosted = backdrop;
                frosted.rgb = lerp(frosted.rgb, fixed3(1, 1, 1), _GlassTint.a * 0.4);

                fixed4 glass = frosted;
                glass.a = sprite.a * _GlassTint.a;
                glass = UnityApplyAlphaClip(glass, _ClipRect, i.worldPosition.xy);
                return glass;
            }
            ENDCG
        }
    }
}
