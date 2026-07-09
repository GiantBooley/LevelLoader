Shader "Custom/PotTest"
{
    Properties {
        _MainTex ("Albedo", 2D) = "white" {}
        _MainTexGold ("Gold Albedo", 2D) = "white" {}
        _MetalSmoothness ("MetallicSmoothness", 2D) = "white" {}
        [Normal] _Normal ("Normal", 2D) = "bump" {}
        [Normal] _NormalGold ("NormalGold", 2D) = "bump" {}
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Glossiness ("Glossiness", Range(0, 1)) = 0
        _Goldness ("Goldness", Range(0, 1)) = 0
    }

    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _MainTexGold;
        sampler2D _MetalSmoothness;
        sampler2D _Normal;
        sampler2D _NormalGold;

        float _Metallic;
        float _Glossiness;
        float _Goldness;

        struct Input {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex;

            float4 colMain = tex2D(_MainTex, uv);
            float4 colGold = tex2D(_MainTexGold, uv);

            float3 albedo = lerp(colMain.rgb, colGold.rgb, _Goldness);

            float3 nMain = UnpackNormal(tex2D(_Normal, uv));
            float3 nGold = UnpackNormal(tex2D(_NormalGold, uv));
            float3 nMix = normalize(lerp(nMain, nGold, _Goldness));

            float4 ms = tex2D(_MetalSmoothness, uv);

            float metallic = lerp(_Metallic, ms.r, ms.r);
            float smoothness = lerp(_Glossiness, ms.a, ms.a);

            o.Albedo = albedo;
            o.Normal = nMix;
            o.Metallic = metallic;
            o.Smoothness = smoothness;
        }
        ENDCG
    }

    Fallback "Diffuse"
}
