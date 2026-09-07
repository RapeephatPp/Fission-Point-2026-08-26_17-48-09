Shader "UI/CRT_Scanlines"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _LineColor ("Line Color", Color) = (0, 0, 0, 0.35)
        _LinesDensity ("Lines Density", Float) = 450.0
        _ScanSpeed ("Scan Speed", Float) = 0.5
        _LineSharpness ("Line Sharpness", Range(1, 8)) = 2.0
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

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            fixed4 _LineColor;
            float _LinesDensity;
            float _ScanSpeed;
            float _LineSharpness;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // คำนวณเส้น Scanline ตามแกน Y
                float wave = sin((IN.texcoord.y + _Time.y * _ScanSpeed * 0.02) * _LinesDensity);
                float lineFactor = pow(0.5 + 0.5 * wave, _LineSharpness);

                // คืนค่าสีของเส้นสีดำโปร่งแสงสลับกับความโปร่งใส
                fixed4 col = _LineColor;
                col.a *= (1.0 - lineFactor) * IN.color.a;
                return col;
            }
            ENDCG
        }
    }
}