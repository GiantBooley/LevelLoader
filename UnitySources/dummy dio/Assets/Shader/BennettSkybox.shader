Shader "Unlit/BennettSkybox"
{
    Properties
    {
		_TopColor ("Top Color", Color) = (1,1,1,1)
		_MidColor ("Middle Color", Color) = (0.5,0.5,0.5,1)
		_BottomColor ("Bottom Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            fixed4 _TopColor;
            fixed4 _MidColor;
            fixed4 _BottomColor;


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            float smoothstep(float x) {
                return x * x * (3.0 - 2.0 * x);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = fixed4(0.0, 0.0, 0.0, 0.0);
                if (i.uv.y < 0.5) {
                    col = lerp(_BottomColor, _MidColor, smoothstep(i.uv.y * 2.0));
                } else {
                    col = lerp(_MidColor, _TopColor, smoothstep(i.uv.y * 2.0 - 1.0));
                }
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
