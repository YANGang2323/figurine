Shader "Hidden/Dialogue Backdrop"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Radius ("Blur Radius at 1080p", Range(0, 12)) = 4
        _Dim ("Dimming", Range(0, 0.3)) = 0.05
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Radius;
            float _Dim;

            half3 SampleColor(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 stepUV = float2(_ScreenParams.y / _ScreenParams.x, 1) * (_Radius / 1080);
                half3 color = SampleColor(uv) * 4;
                color += (SampleColor(uv + float2(stepUV.x, 0))
                        + SampleColor(uv - float2(stepUV.x, 0))
                        + SampleColor(uv + float2(0, stepUV.y))
                        + SampleColor(uv - float2(0, stepUV.y))) * 2;
                color += SampleColor(uv + stepUV)
                       + SampleColor(uv - stepUV)
                       + SampleColor(uv + float2(stepUV.x, -stepUV.y))
                       + SampleColor(uv + float2(-stepUV.x, stepUV.y));
                return half4(color * ((1 - _Dim) / 16), 1);
            }
            ENDHLSL
        }
    }
}
