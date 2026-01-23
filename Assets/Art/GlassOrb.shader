Shader "Quest/GlassOrb"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.1, 0.3, 0.8, 0.5)
        _RimColor ("Rim/Glow Color", Color) = (0.4, 0.8, 1.0, 1.0)
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 2.5
        _RimIntensity ("Rim Intensity", Range(0, 3)) = 1.5
        
        [Header(Reflection)]
        _Cubemap ("Environment Cubemap", CUBE) = "" {}
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.4
        _ReflectionBlur ("Reflection Blur (LOD)", Range(0, 8)) = 2.0
        
        [Header(Fresnel)]
        _FresnelPower ("Fresnel Power", Range(0.5, 5.0)) = 2.0
        _FresnelIntensity ("Fresnel Intensity", Range(0, 2)) = 0.8
        
        [Header(Inner Glow)]
        _InnerColor ("Inner Color", Color) = (0.0, 0.5, 1.0, 1.0)
        _InnerGlowPower ("Inner Glow Power", Range(1, 10)) = 4.0
        _InnerGlowIntensity ("Inner Glow Intensity", Range(0, 2)) = 0.6
        
        [Header(Specular Highlights)]
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularPower ("Specular Power", Range(1, 256)) = 64
        _SpecularIntensity ("Specular Intensity", Range(0, 2)) = 0.8
        
        [Header(Transparency)]
        _Opacity ("Base Opacity", Range(0, 1)) = 0.6
        _DepthFade ("Depth Fade (Softness)", Range(0, 1)) = 0.3
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        
        // Disable backface culling for glass effect
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            Name "GLASS_ORB"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0
            
            // Quest/Mobile optimizations
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RimColor;
                half _RimPower;
                half _RimIntensity;
                half _ReflectionStrength;
                half _ReflectionBlur;
                half _FresnelPower;
                half _FresnelIntensity;
                half4 _InnerColor;
                half _InnerGlowPower;
                half _InnerGlowIntensity;
                half4 _SpecularColor;
                half _SpecularPower;
                half _SpecularIntensity;
                half _Opacity;
                half _DepthFade;
            CBUFFER_END
            
            TEXTURECUBE(_Cubemap);
            SAMPLER(sampler_Cubemap);
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input, half facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                // Normalize interpolated vectors
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(input.viewDirWS);
                
                // Flip normal for back faces (inside of sphere)
                normalWS = normalWS * facing;
                
                // Calculate basic dot products
                half NdotV = saturate(dot(normalWS, viewDirWS));
                half fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelIntensity;
                
                // Rim lighting (edge glow)
                half rim = pow(1.0 - NdotV, _RimPower) * _RimIntensity;
                half3 rimColor = _RimColor.rgb * rim;
                
                // Inner glow (center darkening with color)
                half innerGlow = pow(NdotV, _InnerGlowPower) * _InnerGlowIntensity;
                half3 innerColor = _InnerColor.rgb * innerGlow;
                
                // Cubemap reflection
                half3 reflectDir = reflect(-viewDirWS, normalWS);
                half3 reflection = SAMPLE_TEXTURECUBE_LOD(_Cubemap, sampler_Cubemap, reflectDir, _ReflectionBlur).rgb;
                reflection *= _ReflectionStrength * fresnel;
                
                // Specular highlights (fake light sources)
                // Primary light from above
                half3 lightDir1 = normalize(half3(0.3, 1.0, 0.2));
                half3 halfDir1 = normalize(viewDirWS + lightDir1);
                half spec1 = pow(saturate(dot(normalWS, halfDir1)), _SpecularPower);
                
                // Secondary light from below (for bottom highlight)
                half3 lightDir2 = normalize(half3(-0.2, -0.8, 0.3));
                half3 halfDir2 = normalize(viewDirWS + lightDir2);
                half spec2 = pow(saturate(dot(normalWS, halfDir2)), _SpecularPower * 0.5);
                
                half3 specular = _SpecularColor.rgb * (spec1 + spec2 * 0.6) * _SpecularIntensity;
                
                // Combine all lighting
                half3 finalColor = _BaseColor.rgb;
                finalColor += rimColor;
                finalColor += innerColor;
                finalColor += reflection;
                finalColor += specular;
                
                // Calculate alpha with fresnel-based transparency
                half alpha = _Opacity;
                alpha = lerp(alpha * 0.3, alpha, fresnel); // More transparent in center
                alpha = saturate(alpha + rim * 0.5); // Less transparent at edges
                
                // Back face is more transparent
                if (facing < 0)
                {
                    alpha *= 0.3;
                    finalColor *= 0.5;
                }
                
                // Apply fog
                finalColor = MixFog(finalColor, input.fogFactor);
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    
    // Fallback for Built-in Render Pipeline
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            Name "GLASS_ORB_BUILTIN"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
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
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                UNITY_FOG_COORDS(3)
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            fixed4 _BaseColor;
            fixed4 _RimColor;
            half _RimPower;
            half _RimIntensity;
            samplerCUBE _Cubemap;
            half _ReflectionStrength;
            half _ReflectionBlur;
            half _FresnelPower;
            half _FresnelIntensity;
            fixed4 _InnerColor;
            half _InnerGlowPower;
            half _InnerGlowIntensity;
            fixed4 _SpecularColor;
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
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - o.worldPos);
                
                UNITY_TRANSFER_FOG(o, o.pos);
                
                return o;
            }
            
            fixed4 frag(v2f i, fixed facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                half3 normal = normalize(i.worldNormal) * facing;
                half3 viewDir = normalize(i.viewDir);
                
                half NdotV = saturate(dot(normal, viewDir));
                half fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelIntensity;
                
                // Rim
                half rim = pow(1.0 - NdotV, _RimPower) * _RimIntensity;
                half3 rimColor = _RimColor.rgb * rim;
                
                // Inner glow
                half innerGlow = pow(NdotV, _InnerGlowPower) * _InnerGlowIntensity;
                half3 innerColor = _InnerColor.rgb * innerGlow;
                
                // Reflection
                half3 reflectDir = reflect(-viewDir, normal);
                half3 reflection = texCUBElod(_Cubemap, float4(reflectDir, _ReflectionBlur)).rgb;
                reflection *= _ReflectionStrength * fresnel;
                
                // Specular
                half3 lightDir1 = normalize(half3(0.3, 1.0, 0.2));
                half3 halfDir1 = normalize(viewDir + lightDir1);
                half spec1 = pow(saturate(dot(normal, halfDir1)), _SpecularPower);
                
                half3 lightDir2 = normalize(half3(-0.2, -0.8, 0.3));
                half3 halfDir2 = normalize(viewDir + lightDir2);
                half spec2 = pow(saturate(dot(normal, halfDir2)), _SpecularPower * 0.5);
                
                half3 specular = _SpecularColor.rgb * (spec1 + spec2 * 0.6) * _SpecularIntensity;
                
                // Combine
                half3 finalColor = _BaseColor.rgb + rimColor + innerColor + reflection + specular;
                
                half alpha = _Opacity;
                alpha = lerp(alpha * 0.3, alpha, fresnel);
                alpha = saturate(alpha + rim * 0.5);
                
                if (facing < 0)
                {
                    alpha *= 0.3;
                    finalColor *= 0.5;
                }
                
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                
                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
