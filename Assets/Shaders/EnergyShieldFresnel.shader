Shader "OrbitStrike/EnergyShieldFresnel"
{
    Properties
    {
        _ShieldColor ("Shield Color", Color) = (0, 0.65, 1, 0.6)
        _FresnelPower ("Fresnel Power", Float) = 3.5
        _HexScale ("Hexagon Pattern Scale", Float) = 25.0
        _PulseRate ("Pulse Rate", Float) = 3.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+50" "RenderPipeline"="UniversalPipeline" }
        LOD 200
        Blend SrcAlpha One
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShieldColor;
                float _FresnelPower;
                float _HexScale;
                float _PulseRate;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                float3 V = normalize(input.viewDirWS);
                float NdotV = saturate(dot(N, V));

                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                float pulse = sin(_Time.y * _PulseRate) * 0.15 + 0.85;

                // Hexagonal grid approximation
                float2 coord = input.uv * _HexScale;
                float hexLine = abs(sin(coord.x * 3.14159) * sin(coord.y * 3.14159));
                float hexVal = smoothstep(0.4, 0.5, hexLine);

                float combined = saturate(fresnel * 1.5 + hexVal * 0.35) * pulse * _ShieldColor.a;
                return half4(_ShieldColor.rgb * (1.0 + fresnel * 3.0), combined);
            }
            ENDHLSL
        }
    }
}

