Shader "Quest/GlassOrbMobile"
{
    // Simplified version optimized for Quest/Mobile performance
    // Uses fewer texture samples and simplified math
    
    Properties
    {
        [Header(Colors)]
        _BaseColor ("Base Color", Color) = (0.05, 0.2, 0.6, 0.4)
        _RimColor ("Rim Glow Color", Color) = (0.3, 0.7, 1.0, 1.0)
        _CoreColor ("Core Color", Color) = (0.0, 0.1, 0.3, 1.0)
        
        [Header(Rim Settings)]
        _RimPower ("Rim Power", Range(1, 6)) = 2.5
        _RimIntensity ("Rim Intensity", Range(0, 2)) = 1.2
        
        [Header(Reflection)]
        _Cubemap ("Environment Cubemap", CUBE) = "" {}
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.3
        
        [Header(Specular)]
        _SpecularPower ("Specular Sharpness", Range(8, 128)) = 48
        _SpecularIntensity ("Specular Intensity", Range(0, 1)) = 0.6
        
        [Header(Transparency)]
        _Opacity ("Opacity", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            Name "GLASS_MOBILE"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            // Mobile/Quest optimizations
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                half3 viewDirWS : TEXCOORD1;
                half3 reflectDir : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RimColor;
                half4 _CoreColor;
                half _RimPower;
                half _RimIntensity;
                half _ReflectionStrength;
                half _SpecularPower;
                half _SpecularIntensity;
                half _Opacity;
            CBUFFER_END
            
            TEXTURECUBE(_Cubemap);
            SAMPLER(sampler_Cubemap);
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                
                // Calculate reflection in vertex shader to save fragment cost
                output.reflectDir = reflect(-output.viewDirWS, output.normalWS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                half3 N = normalize(input.normalWS);
                half3 V = normalize(input.viewDirWS);
                
                // Core calculations
                half NdotV = saturate(dot(N, V));
                half rimFactor = 1.0 - NdotV;
                
                // Rim/Fresnel glow
                half rim = pow(rimFactor, _RimPower) * _RimIntensity;
                
                // Simple specular (single light from above-right)
                half3 L = half3(0.4, 0.8, 0.3);
                half3 H = normalize(V + L);
                half spec = pow(saturate(dot(N, H)), _SpecularPower) * _SpecularIntensity;
                
                // Cubemap reflection (single sample)
                half3 reflection = SAMPLE_TEXTURECUBE_LOD(_Cubemap, sampler_Cubemap, input.reflectDir, 3.0).rgb;
                reflection *= _ReflectionStrength * rimFactor;
                
                // Color blending: rim color at edges, core color at center
                half3 baseBlend = lerp(_CoreColor.rgb, _BaseColor.rgb, NdotV * 0.5 + 0.5);
                
                // Final color
                half3 finalColor = baseBlend;
                finalColor += _RimColor.rgb * rim;
                finalColor += reflection;
                finalColor += spec;
                
                // Alpha: more opaque at edges
                half alpha = lerp(_Opacity * 0.4, _Opacity, rim + 0.3);
                alpha = saturate(alpha);
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    
    // Built-in pipeline fallback
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
        }
        
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                half3 worldNormal : TEXCOORD0;
                half3 viewDir : TEXCOORD1;
                half3 reflectDir : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            fixed4 _BaseColor;
            fixed4 _RimColor;
            fixed4 _CoreColor;
            half _RimPower;
            half _RimIntensity;
            samplerCUBE _Cubemap;
            half _ReflectionStrength;
            half _SpecularPower;
            half _SpecularIntensity;
            half _Opacity;
            
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                o.reflectDir = reflect(-o.viewDir, o.worldNormal);
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                half3 N = normalize(i.worldNormal);
                half3 V = normalize(i.viewDir);
                
                half NdotV = saturate(dot(N, V));
                half rimFactor = 1.0 - NdotV;
                half rim = pow(rimFactor, _RimPower) * _RimIntensity;
                
                half3 L = half3(0.4, 0.8, 0.3);
                half3 H = normalize(V + L);
                half spec = pow(saturate(dot(N, H)), _SpecularPower) * _SpecularIntensity;
                
                half3 reflection = texCUBElod(_Cubemap, float4(i.reflectDir, 3.0)).rgb;
                reflection *= _ReflectionStrength * rimFactor;
                
                half3 baseBlend = lerp(_CoreColor.rgb, _BaseColor.rgb, NdotV * 0.5 + 0.5);
                
                half3 finalColor = baseBlend + _RimColor.rgb * rim + reflection + spec;
                half alpha = saturate(lerp(_Opacity * 0.4, _Opacity, rim + 0.3));
                
                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
    }
    
    FallBack "Mobile/Diffuse"
}
