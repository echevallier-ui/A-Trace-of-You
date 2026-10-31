#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

void GetLightingInfo_float(float3 WorldPos, float3 WorldNormal, float2 ScreenPos, out float LightIntensity)
{
#if defined(SHADERGRAPH_PREVIEW)
    LightIntensity = 1.0;
#else
    #if defined(UNIVERSAL_LIGHTING_INCLUDED)
        // 1. Lumière principale (Directional Light)
        float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
        Light mainLight = GetMainLight(shadowCoord);
        
        float NdotL = saturate(dot(WorldNormal, mainLight.direction));
        //float totalIntensity = NdotL * mainLight.distanceAttenuation * mainLight.shadowAttenuation;
        float totalIntensity = 0.0;

        // 2. Contexte pour la boucle Forward+ (Unity 6)
        InputData inputData = (InputData)0;
        inputData.positionWS = WorldPos;
        inputData.normalizedScreenSpaceUV = ScreenPos;

        // 3. Lumières additionnelles (Spotlight, Point Light)
        uint pixelLightCount = GetAdditionalLightsCount();
        LIGHT_LOOP_BEGIN(pixelLightCount)
            Light addLight = GetAdditionalLight(lightIndex, WorldPos, shadowCoord);
            float addNdotL = saturate(dot(WorldNormal, addLight.direction));
            totalIntensity += addNdotL * addLight.distanceAttenuation * addLight.shadowAttenuation;
        LIGHT_LOOP_END

        LightIntensity = saturate(totalIntensity);
    #else
        LightIntensity = 0.0;
    #endif
#endif
unity_FogParams = float4(0.0, 0.0, 0.0, 1.0);
}

#endif