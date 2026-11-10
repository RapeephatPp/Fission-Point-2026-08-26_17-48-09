Shader "UI/MinigameBorder"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)
        _BorderColor ("Border Color", Color) = (1, 0.85, 0.1, 1)

        [Header(Box SDF Border Dimensions)]
        _BorderThickness ("Border Thickness (px)", Range(1, 20)) = 4.0
        _CornerRadius ("Corner Radius (px)", Range(0, 30)) = 6.0
        _Padding ("Glow Padding (px)", Range(0, 30)) = 14.0
        _GlowSpread ("Glow Spread (px)", Range(1, 40)) = 14.0
        _GlowIntensity ("Glow Intensity", Range(0, 2)) = 0.9

        [Header(Animation Settings)]
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 4.0
        _PulseMinAlpha ("Pulse Min Alpha", Range(0.2, 1.0)) = 0.65
        _PulseMaxAlpha ("Pulse Max Alpha", Range(0.5, 1.0)) = 1.0
        _AnimatedDash ("Traveling Dash Intensity", Range(0, 1)) = 0.4
        _DashSpeed ("Dash Speed", Range(0, 10)) = 1.5

        [HideInInspector] _RectSize ("Rect Size (Width, Height)", Vector) = (400, 300, 0, 0)

        // UI Mask / Stencil Support
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _BorderColor;
            float4 _ClipRect;

            float _BorderThickness;
            float _CornerRadius;
            float _Padding;
            float _GlowSpread;
            float _GlowIntensity;

            float _PulseSpeed;
            float _PulseMinAlpha;
            float _PulseMaxAlpha;
            float _AnimatedDash;
            float _DashSpeed;

            float4 _RectSize;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // คำนวณขนาด Rect (pixels)
                float2 rectPixels = _RectSize.xy;
                if (rectPixels.x <= 1.0 || rectPixels.y <= 1.0)
                {
                    float2 dF = max(fwidth(IN.texcoord), float2(0.00005, 0.00005));
                    rectPixels = 1.0 / dF;
                }

                // พิกัดพิกเซลเทียบจากจุดกึ่งกลาง (-halfSize ถึง +halfSize)
                float2 p = (IN.texcoord - 0.5) * rectPixels;

                // ขนาดครึ่งหนึ่งของกรอบที่หักลบ Padding เพื่อให้มีพื้นที่แสงเรืองไม่ถูกตัดขอบ
                float pad = max(_Padding, _BorderThickness * 0.5 + 2.0);
                float2 halfBox = max((rectPixels * 0.5) - pad, float2(2.0, 2.0));

                // รัศมีมุมมน (Corner Radius)
                float r = min(_CornerRadius, min(halfBox.x, halfBox.y));

                // 2D Rounded Box SDF
                // dist = 0 คือเส้นขอบรอบรูปพอดี, dist < 0 คือด้านใน, dist > 0 คือด้านนอก
                float2 q = abs(p) - (halfBox - r);
                float dist = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
                float absDist = abs(dist);

                // 1. เส้นขอบทึบหลัก (Anti-aliased Sharp Core Line)
                float halfThick = _BorderThickness * 0.5;
                float coreLine = 1.0 - smoothstep(halfThick - 0.75, halfThick + 0.75, absDist);

                // 2. แสงเรืองรองนุ่มนวล (Smooth Exponential Glow ทั้งด้านในและด้านนอก)
                float glow = 0.0;
                if (absDist > halfThick)
                {
                    float t = (absDist - halfThick) / max(_GlowSpread, 0.001);
                    glow = exp(-t * 2.5) * _GlowIntensity;
                }

                // ป้องกันแสงเรืองด้านนอกถูกตัดแข็งที่ขอบ Quad (Soft Edge Falloff)
                float2 distToQuadEdge = (rectPixels * 0.5) - abs(p);
                float edgeFade = smoothstep(0.0, pad * 0.6, min(distToQuadEdge.x, distToQuadEdge.y));
                glow *= edgeFade;

                float combinedShape = coreLine + glow;
                if (combinedShape <= 0.001)
                {
                    discard;
                }

                // 3. จังหวะกะพริบไฟเตือน (Breathing Pulse)
                float pulse = (sin(_Time.y * _PulseSpeed) + 1.0) * 0.5;
                float pulseAlpha = lerp(_PulseMinAlpha, _PulseMaxAlpha, pulse);

                // 4. ลำแสงวิ่งรอบกรอบ (Traveling Dash)
                float angle = atan2(p.y, p.x); // -PI ถึง +PI
                float travelPos = frac((angle / 6.2831853) - (_Time.y * _DashSpeed * 0.15));
                float dash = pow(saturate(1.0 - abs(travelPos - 0.5) * 5.0), 3.0) * _AnimatedDash;

                // รวมผลลัพธ์สี
                fixed4 col = _BorderColor * IN.color;
                float finalAlpha = (coreLine * pulseAlpha) + (glow * pulseAlpha) + (dash * (coreLine + glow * 0.5));
                col.a *= saturate(finalAlpha);

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
