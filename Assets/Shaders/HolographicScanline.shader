Shader "OrbitStrike/HolographicScanline"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0, 0.95, 1, 0.8)
        _GridScale ("Grid Scale", Float) = 15.0
        _ScanSpeed ("Scan Speed", Float) = 2.0
        _RimPower ("Rim Power", Float) = 3.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _GridScale;
                float _ScanSpeed;
                float _RimPower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.uv = input.uv;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Procedural Grid lines
                float2 grid = abs(frac(input.uv * _GridScale - 0.5) - 0.5) / fwidth(input.uv * _GridScale);
                float lineVal = min(grid.x, grid.y);
                float gridIntensity = 1.0 - min(lineVal, 1.0);

                // Scanning Pulse
                float scan = sin((input.uv.y * 10.0) + (_Time.y * _ScanSpeed)) * 0.5 + 0.5;

                // Fresnel Edge
                float fresnel = pow(1.0 - saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS))), _RimPower);

                float alpha = (gridIntensity * 0.7 + scan * 0.3 + fresnel * 0.5) * _BaseColor.a;
                return half4(_BaseColor.rgb * (1.0 + fresnel * 2.0), alpha);
            }
            ENDHLSL
        }
    }
}

