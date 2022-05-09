﻿// Toony Colors Pro+Mobile 2
// (c) 2014-2021 Jean Moreno

/// #define fixed half
/// #define fixed2 half2
/// #define fixed3 half3
/// #define fixed4 half4

// Built-in renderer (CG) to SRP (HLSL) bindings
#if defined(TCP2_HYBRID_URP)
	#define UnityObjectToClipPos TransformObjectToHClip
	#define UnityObjectToWorldNormal TransformObjectToWorldNormal
	#define _WorldSpaceLightPos0 _MainLightPosition
	#define UnpackScaleNormal UnpackNormalScale
#endif

#if defined(TCP2_HYBRID_URP)
	// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
	// This would cause a compilation error if URP isn't installed, so instead we use the dedicated
	// "URP Support" file which contains all needed .hlslinc files embedded within a single file.
	#include "TCP2 Hybrid URP Support.cginc"
	#define UNITY_PASS_FORWARDBASE
#endif

//================================================================================================================================
//================================================================================================================================

// MAIN

//================================================================================================================================
//================================================================================================================================

// Uniforms
CBUFFER_START(UnityPerMaterial)
	half _RampSmoothing;
	half _RampThreshold;
	half _RampBands;
	half _RampBandsSmoothing;
	half _RampScale;
	half _RampOffset;

	float4 _BumpMap_ST;
	half _BumpScale;

	float4 _BaseMap_ST;

	half _Cutoff;

	half4 _BaseColor;

	float4 _EmissionMap_ST;
	half _EmissionChannel;
	half4 _EmissionColor;

	half4 _MatCapColor;
	half _MatCapMaskChannel;
	half _MatCapType;

	half4 _SColor;
	half4 _HColor;

	half _RimMin;
	half _RimMax;
	half4 _RimColor;

	half _SpecularRoughness;
	half4 _SpecularColor;
	half _SpecularMapType;
	half _SpecularToonSize;
	half _SpecularToonSmoothness;

	half _ReflectionSmoothness;
	half4 _ReflectionColor;
	half _FresnelMax;
	half _FresnelMin;
	half _ReflectionMapType;

	half _OcclusionStrength;
	half _OcclusionChannel;

	half _IndirectIntensity;
	half _SingleIndirectColor;

	half _OutlineWidth;
	half _OutlineMinWidth;
	half _OutlineMaxWidth;
	half4 _OutlineColor;
	half _OutlineTextureLOD;
	half _DirectIntensityOutline;
	half _IndirectIntensityOutline;
CBUFFER_END

// Samplers
sampler2D _BaseMap;
sampler2D _Ramp;
sampler2D _BumpMap;
sampler2D _EmissionMap;
sampler2D _OcclusionMap;
sampler2D _ReflectionTex;
sampler2D _SpecGlossMap;
sampler2D _ShadowBaseMap;
sampler2D _MatCapTex;
sampler2D _MatCapMask;

// Meta Pass
#if defined(UNITY_PASS_META) && !defined(TCP2_HYBRID_URP)
	#include "UnityMetaPass.cginc"
#endif

//Specular help functions (from UnityStandardBRDF.cginc)
inline half3 TCP2_SafeNormalize(half3 inVec)
{
	half dp3 = max(0.001f, dot(inVec, inVec));
	return inVec * rsqrt(dp3);
}

//GGX
#define TCP2_PI 3.14159265359
#define TCP2_INV_PI 0.31830988618f
#if defined(SHADER_API_MOBILE)
	#define TCP2_EPSILON 1e-4f
#else
	#define TCP2_EPSILON 1e-7f
#endif
inline half GGX(half NdotH, half roughness)
{
	half a2 = roughness * roughness;
	half d = (NdotH * a2 - NdotH) * NdotH + 1.0f;
	return TCP2_INV_PI * a2 / (d * d + TCP2_EPSILON);
}

float GetOcclusion(sampler2D _OcclusionMap, float2 mainTexcoord, half _OcclusionStrength, half _OcclusionChannel, half4 albedo)
{
#if defined(TCP2_MOBILE)
	half occlusion = tex2D(_OcclusionMap, mainTexcoord).a;
#else
	half occlusion = 1.0;
	if (_OcclusionChannel >= 4)
	{
		occlusion = tex2D(_OcclusionMap, mainTexcoord).a;
	}
	else if (_OcclusionChannel >= 3)
	{
		occlusion = tex2D(_OcclusionMap, mainTexcoord).b;
	}
	else if (_OcclusionChannel >= 2)
	{
		occlusion = tex2D(_OcclusionMap, mainTexcoord).g;
	}
	else if (_OcclusionChannel >= 1)
	{
		occlusion = tex2D(_OcclusionMap, mainTexcoord).r;
	}
	else
	{
		occlusion = albedo.a;
	}
#endif
	occlusion = lerp(1, occlusion, _OcclusionStrength);
	return occlusion;
}

half3 CalculateRamp(half ndlWrapped)
{
	#if defined(TCP2_RAMPTEXT)
		half3 ramp = tex2D(_Ramp, _RampOffset + ((ndlWrapped.xx - 0.5) * _RampScale) + 0.5).rgb;
	#elif defined(TCP2_RAMP_BANDS) || defined(TCP2_RAMP_BANDS_CRISP)
		half bands = _RampBands;

		half rampThreshold = _RampThreshold;
		half rampSmooth = _RampSmoothing * 0.5;
		half x = smoothstep(rampThreshold - rampSmooth, rampThreshold + rampSmooth, ndlWrapped);

		#if defined(TCP2_RAMP_BANDS_CRISP)
			half bandsSmooth = fwidth(ndlWrapped) * (2.0 + bands);
		#else
			half bandsSmooth = _RampBandsSmoothing * 0.5;
		#endif
		half3 ramp = saturate((smoothstep(0.5 - bandsSmooth, 0.5 + bandsSmooth, frac(x * bands)) + floor(x * bands)) / bands).xxx;
	#else
		#if defined(TCP2_RAMP_CRISP)
			half rampSmooth = fwidth(ndlWrapped) * 0.5;
		#else
			half rampSmooth = _RampSmoothing * 0.5;
		#endif
		half rampThreshold = _RampThreshold;
		half3 ramp = smoothstep(rampThreshold - rampSmooth, rampThreshold + rampSmooth, ndlWrapped).xxx;
	#endif
	return ramp;
}

half CalculateSpecular(half3 lightDir, half3 viewDir, float3 normal, half specularMap)
{
	half3 halfDir = TCP2_SafeNormalize(lightDir + viewDir);
	half nh = saturate(dot(normal, halfDir));

	#if defined(TCP2_SPECULAR_STYLIZED) || defined(TCP2_SPECULAR_CRISP)
		half specSize = 1 - (_SpecularToonSize * specularMap);
		nh = nh * (1.0 / (1.0 - specSize)) - (specSize / (1.0 - specSize));

		#if defined(TCP2_SPECULAR_CRISP)
			float specSmoothness = fwidth(nh);
		#else
			float specSmoothness = _SpecularToonSmoothness;
		#endif

		half spec = smoothstep(0, specSmoothness, nh);
	#else
		float specularRoughness = max(0.00001,  _SpecularRoughness) * specularMap;
		half roughness = specularRoughness * specularRoughness;
		half spec = GGX(nh, saturate(roughness));
		spec *= TCP2_PI * 0.05;
		#ifdef UNITY_COLORSPACE_GAMMA
			spec = max(0, sqrt(max(1e-4h, spec)));
			half surfaceReduction = 1.0 - 0.28 * roughness * specularRoughness;
		#else
			half surfaceReduction = 1.0 / (roughness*roughness + 1.0);
		#endif
		spec *= surfaceReduction;
	#endif

	return max(0, spec);
}

// Custom macros to separate shadows from attenuation
// Based on UNITY_LIGHT_ATTENUATION macros from "AutoLight.cginc"

#ifdef POINT
#	define TCP2_LIGHT_ATTENUATION(input, worldPos) \
		unityShadowCoord3 lightCoord = mul(unity_WorldToLight, unityShadowCoord4(worldPos, 1)).xyz; \
		half shadow = UNITY_SHADOW_ATTENUATION(input, worldPos); \
		half attenuation = tex2D(_LightTexture0, dot(lightCoord, lightCoord).rr).r;
#endif

#ifdef SPOT
#	define TCP2_LIGHT_ATTENUATION(input, worldPos) \
		DECLARE_LIGHT_COORD(input, worldPos); \
		half shadow = UNITY_SHADOW_ATTENUATION(input, worldPos); \
		half attenuation = (lightCoord.z > 0) * UnitySpotCookie(lightCoord) * UnitySpotAttenuate(lightCoord.xyz);
#endif

#ifdef DIRECTIONAL
#	define TCP2_LIGHT_ATTENUATION(input, worldPos) \
		half shadow = UNITY_SHADOW_ATTENUATION(input, worldPos); \
		half attenuation = 1;
#endif

#ifdef POINT_COOKIE
#	define TCP2_LIGHT_ATTENUATION(input, worldPos) \
		DECLARE_LIGHT_COORD(input, worldPos); \
		half shadow = UNITY_SHADOW_ATTENUATION(input, worldPos); \
		half attenuation = tex2D(_LightTextureB0, dot(lightCoord, lightCoord).rr).r * texCUBE(_LightTexture0, lightCoord).w;
#endif

#ifdef DIRECTIONAL_COOKIE
#	define TCP2_LIGHT_ATTENUATION(input, worldPos) \
		DECLARE_LIGHT_COORD(input, worldPos); \
		half shadow = UNITY_SHADOW_ATTENUATION(input, worldPos); \
		half attenuation = tex2D(_LightTexture0, lightCoord).w;
#endif

// Vertex input
struct Attributes
{
	float4 vertex         : POSITION;
	float3 normal         : NORMAL;
	float4 tangent        : TANGENT;
	float4 texcoord0      : TEXCOORD0;
	#if defined(LIGHTMAP_ON) || defined(UNITY_PASS_META)
		float2 texcoord1  : TEXCOORD1;
	#endif
	#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
		float2 texcoord2 : TEXCOORD2;
	#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

// Vertex output / Fragment input
struct Varyings
{
	float4 pos             : SV_POSITION;
	float3 normal          : NORMAL;
	float4 worldPos        : TEXCOORD0; /* w = fog coords */
	float4 texcoords       : TEXCOORD1; /* xy = main texcoords, zw = raw texcoords */
#if defined(_NORMALMAP) || (defined(TCP2_MOBILE) && (defined(TCP2_RIM_LIGHTING) || (defined(TCP2_REFLECTIONS) && defined(TCP2_REFLECTIONS_FRESNEL)))) // if normalmap or (mobile + rim or fresnel)
	float4 tangentWS       : TEXCOORD2; /* w = ndv (mobile) */
#endif
#if defined(_NORMALMAP)
	float4 bitangentWS     : TEXCOORD3;
#endif
#if defined(TCP2_MATCAP) && !defined(_NORMALMAP)
	float4 matcap          : TEXCOORD4;
#endif
#if defined(TCP2_HYBRID_URP)
	#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
		float4 shadowCoord : TEXCOORD5; // compute shadow coord per-vertex for the main light
	#endif
	#ifdef _ADDITIONAL_LIGHTS_VERTEX
		half3 vertexLights : TEXCOORD6;
	#endif
	#if defined(DYNAMICLIGHTMAP_ON) || defined(LIGHTMAP_ON)
		float4 lightmapUV  : TEXCOORD7;
	#endif
#else
	#if defined(DYNAMICLIGHTMAP_ON) || defined(LIGHTMAP_ON)
		float4 lmap        : TEXCOORD5;
	#endif
	UNITY_LIGHTING_COORDS(6,7)
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

struct Varyings_Meta
{
	float4 positionCS   : SV_POSITION;
	float2 uv    : TEXCOORD0;
};

#if defined(UNITY_PASS_META)
	#define VERTEX_OUTPUT Varyings_Meta
#else
	#define VERTEX_OUTPUT Varyings
#endif

VERTEX_OUTPUT Vertex(Attributes input)
{
	#if defined(UNITY_PASS_META)
		Varyings_Meta meta_output;
		#if defined(TCP2_HYBRID_URP)
			meta_output.positionCS = MetaVertexPosition(input.vertex, input.texcoord1, input.texcoord2, unity_LightmapST, unity_DynamicLightmapST);
		#else
			meta_output.positionCS = UnityMetaVertexPosition(input.vertex, input.texcoord1, input.texcoord2, unity_LightmapST, unity_DynamicLightmapST);
		#endif
	meta_output.uv = TRANSFORM_TEX(input.texcoord0, _BaseMap);
	return meta_output;
	#else

	Varyings output = (Varyings)0;

	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	// Texture Coordinates
	output.texcoords.xy = input.texcoord0.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;
	output.texcoords.zw = input.texcoord0.xy;

	#if defined(TCP2_HYBRID_URP)
		OUTPUT_LIGHTMAP_UV(input.texcoord1, unity_LightmapST, output.lightmapUV);

		VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
		#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
			output.shadowCoord = GetShadowCoord(vertexInput);
		#endif
		float3 positionWS = vertexInput.positionWS;
		float4 positionCS = vertexInput.positionCS;
		output.pos = positionCS;

		VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normal, input.tangent);
		float3 normalWS = vertexNormalInput.normalWS;
		#if defined(_NORMALMAP)
			float3 tangentWS = vertexNormalInput.tangentWS;
			float3 bitangentWS = vertexNormalInput.bitangentWS;
		#endif

		#ifdef _ADDITIONAL_LIGHTS_VERTEX
			// Vertex lighting
			output.vertexLights = VertexLighting(positionWS, normalWS);
		#endif
	#else
		#ifdef LIGHTMAP_ON
			output.lmap.xy = input.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
			output.lmap.zw = 0;
		#endif
		#ifdef DYNAMICLIGHTMAP_ON
			output.lmap.zw = input.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
		#endif

		float3 positionWS = mul(unity_ObjectToWorld, input.vertex).xyz;
		float4 positionCS = UnityWorldToClipPos(positionWS);
		output.pos = positionCS;

		half sign = half(input.tangent.w) * half(unity_WorldTransformParams.w);
		float3 normalWS = UnityObjectToWorldNormal(input.normal);
		#if defined(_NORMALMAP)
			float3 tangentWS = UnityObjectToWorldDir(input.tangent.xyz);
			float3 bitangentWS = cross(normalWS, tangentWS) * sign;
		#endif

		// This Unity macro expects the vertex input to be named 'v'
		#define v input
		UNITY_TRANSFER_LIGHTING(output, input.texcoord1.xy);
	#endif

	// world position
	output.worldPos = float4(positionWS.xyz, 0);

	// Compute fog factor
	#if defined(TCP2_HYBRID_URP)
		output.worldPos.w = ComputeFogFactor(positionCS.z);
	#else
		UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(output, positionCS);
	#endif

	// normal
	output.normal = normalWS;

	// tangent
	#if defined(_NORMALMAP) || (defined(TCP2_MOBILE) && (defined(TCP2_RIM_LIGHTING) || (defined(TCP2_REFLECTIONS) && defined(TCP2_REFLECTIONS_FRESNEL)))) // if mobile + rim or fresnel
		output.tangentWS = float4(0, 0, 0, 0);
	#endif
	#if defined(_NORMALMAP)
		output.tangentWS.xyz = tangentWS;
		output.bitangentWS.xyz = bitangentWS;
	#endif
	#if defined(TCP2_MOBILE) && (defined(TCP2_RIM_LIGHTING) || (defined(TCP2_REFLECTIONS) && defined(TCP2_REFLECTIONS_FRESNEL))) // if mobile + rim or fresnel
		// Calculate ndv in vertex shader
		#if defined(TCP2_HYBRID_URP)
			half3 viewDirWS = TCP2_SafeNormalize(GetCameraPositionWS() - positionWS);
		#else
			half3 viewDirWS = TCP2_SafeNormalize(_WorldSpaceCameraPos.xyz - positionWS);
		#endif
		output.tangentWS.w = 1 - max(0, dot(viewDirWS, normalWS));
	#endif

	#if defined(TCP2_MATCAP) && !defined(_NORMALMAP)
		// MatCap
		float3 worldNorm = normalize(unity_WorldToObject[0].xyz * input.normal.x + unity_WorldToObject[1].xyz * input.normal.y + unity_WorldToObject[2].xyz * input.normal.z);
		worldNorm = mul((float3x3)UNITY_MATRIX_V, worldNorm);
		float4 screenPos = ComputeScreenPos(positionCS);
		float3 perspectiveOffset = (screenPos.xyz / screenPos.w) - 0.5;
		worldNorm.xy -= (perspectiveOffset.xy * perspectiveOffset.z) * 0.5;
		output.matcap.xy = worldNorm.xy * 0.5 + 0.5;
	#endif

	return output;

	#endif
}

// Note: calculations from the main pass are defined with UNITY_PASS_FORWARDBASE
// However it is left out sometimes because some keywords aren't defined for the
// Forward Add pass (e.g. TCP2_MATCAP, TCP2_REFLECTIONS, ...)

half4 Fragment (Varyings input, half vFace : VFACE) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	// Texture coordinates
	float2 mainTexcoord = input.texcoords.xy;
	float2 rawTexcoord = input.texcoords.zw;

	// Vectors
	float3 positionWS = input.worldPos.xyz;
	float3 normalWS = normalize(input.normal);
	normalWS.xyz *= (vFace < 0) ? -1.0 : 1.0;

	#if defined(TCP2_HYBRID_URP)
		half3 viewDirWS = TCP2_SafeNormalize(GetCameraPositionWS() - positionWS);
	#else
		half3 viewDirWS = TCP2_SafeNormalize(_WorldSpaceCameraPos.xyz - positionWS);
	#endif
	#if defined(_NORMALMAP)
		half3 tangentWS = input.tangentWS.xyz;
		half3 bitangentWS = input.bitangentWS.xyz;
		half3x3 tangentToWorldMatrix = half3x3(tangentWS.xyz, bitangentWS.xyz, normalWS.xyz);
	#endif

	// Lighting

	#if defined(TCP2_HYBRID_URP)
		#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
			float4 shadowCoord = input.shadowCoord;
		#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
			float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
		#else
			float4 shadowCoord = float4(0, 0, 0, 0);
		#endif

		#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
			half4 shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
		#elif !defined (LIGHTMAP_ON)
			half4 shadowMask = unity_ProbesOcclusion;
		#else
			half4 shadowMask = half4(1, 1, 1, 1);
		#endif

		Light mainLight = GetMainLight(shadowCoord, positionWS, shadowMask);

		#if defined(_SCREEN_SPACE_OCCLUSION)
			float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.pos);
			AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV);
			mainLight.color *= aoFactor.directAmbientOcclusion;
		#endif

		half3 lightDir = mainLight.direction;
		half3 lightColor = mainLight.color.rgb;
		half atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
	#else
		half3 lightDir = normalize(UnityWorldSpaceLightDir(positionWS));
		half3 lightColor = _LightColor0.rgb;

		TCP2_LIGHT_ATTENUATION(input, positionWS)
		#if defined(_RECEIVE_SHADOWS_OFF)
			half atten = attenuation;
		#else
			half atten = shadow * attenuation;
		#endif
	#endif

	// Base

	half4 albedo = tex2D(_BaseMap, mainTexcoord).rgba;
	albedo.rgb *= _BaseColor.rgb;
	half alpha = albedo.a * _BaseColor.a;
	half3 emission = half3(0,0,0);

	// Normal Mapping
	#if defined(_NORMALMAP)
		half4 normalMap = tex2D(_BumpMap, rawTexcoord * _BumpMap_ST.xy + _BumpMap_ST.zw).rgba;
		half3 normalTS = UnpackScaleNormal(normalMap, _BumpScale);
		normalWS = mul(normalTS, tangentToWorldMatrix);
	#endif

	// Alpha Testing
	#if defined(_ALPHATEST_ON)
		clip(alpha - _Cutoff);
	#endif

	// Emission
	#if defined(_EMISSION)
		emission = _EmissionColor.rgb;
		#if defined(TCP2_MOBILE)
			half4 emissionMap = tex2D(_EmissionMap, rawTexcoord * _EmissionMap_ST.xy + _EmissionMap_ST.zw);
			emission *= emissionMap.rgb;
		#else
			if (_EmissionChannel < 5)
			{
				half4 emissionMap = tex2D(_EmissionMap, rawTexcoord * _EmissionMap_ST.xy + _EmissionMap_ST.zw);
				if (_EmissionChannel >= 4)		emission *= emissionMap.rgb;
				else if (_EmissionChannel >= 3)	emission *= emissionMap.a;
				else if (_EmissionChannel >= 2) emission *= emissionMap.b;
				else if (_EmissionChannel >= 1) emission *= emissionMap.g;
				else							emission *= emissionMap.r;
			}
		#endif
	#endif

	#if defined(UNITY_PASS_META)
		half3 meta_albedo = albedo.rgb;
		half3 meta_emission = emission.rgb;
		half3 meta_specular = half3(0, 0, 0);
	#endif

	// MatCap
	#if defined(TCP2_MATCAP)
		#if defined(_NORMALMAP)
			half3 matcapCoordsNormal = mul((float3x3)UNITY_MATRIX_V, normalWS);
			half3 matcap = tex2D(_MatCapTex, matcapCoordsNormal.xy * 0.5 + 0.5).rgb * _MatCapColor.rgb;
		#else
			half3 matcap = tex2D(_MatCapTex, input.matcap.xy).rgb * _MatCapColor.rgb;
		#endif
		half matcapMask = 1.0;
		#if defined(TCP2_MATCAP_MASK)
			half4 matcapMaskTex = tex2D(_MatCapMask, mainTexcoord);
			#if defined(TCP2_MOBILE)
				matcapMask *= matcapMaskTex.a;
			#else
				if (_MatCapMaskChannel >= 3)
				{
					matcapMask *= matcapMaskTex.a;
				}
				else if (_MatCapMaskChannel >= 2)
				{
					matcapMask *= matcapMaskTex.b;
				}
				else if (_MatCapMaskChannel >= 1)
				{
					matcapMask *= matcapMaskTex.g;
				}
				else
				{
					matcapMask *= matcapMaskTex.r;
				}
			#endif
		#endif

		#if defined(TCP2_MOBILE)
			emission += matcap * matcapMask;
		#else
			if (_MatCapType >= 1)
			{
				albedo.rgb = lerp(albedo.rgb, matcap.rgb, matcapMask);
			}
			else
			{
				emission += matcap * matcapMask;
			}
		#endif
	#endif

	half ndl = dot(normalWS, lightDir);
	half ndlWrapped = ndl * 0.5 + 0.5;
	ndl = saturate(ndl);

	// Calculate ramp
	half3 ramp = CalculateRamp(ndlWrapped);

	// Apply attenuation
	ramp *= atten;
	#if defined(TCP2_RIM_LIGHTING)
		#if defined(TCP2_RIM_LIGHTING_LIGHTMASK)
			half3 rimMask = ramp.xxx * lightColor.rgb;
		#else
			half3 rimMask = half3(1, 1, 1);
		#endif
	#endif

	// Shadow Albedo
	#if defined(TCP2_SHADOW_TEXTURE)
		half4 shadowAlbedo = tex2D(_ShadowBaseMap, mainTexcoord).rgba;
		albedo = lerp(shadowAlbedo, albedo, ramp.x);
	#endif

	// Highlight/shadow colors
	#if !defined(TCP2_SHADOW_LIGHT_COLOR)
		half3 highlightColor = _HColor.rgb * lightColor.rgb;
	#else
		half3 highlightColor = _HColor.rgb;
	#endif
	#if defined(UNITY_PASS_FORWARDBASE)
		ramp = lerp(_SColor.rgb, highlightColor, ramp);
	#else
		ramp = lerp(half3(0, 0, 0), highlightColor, ramp);
	#endif

	#if defined(TCP2_SHADOW_LIGHT_COLOR)
		ramp *= lightColor.rgb;
	#endif

	// Output color
	half3 color = albedo.rgb * ramp;

	// Occlusion
	#if defined(TCP2_OCCLUSION)
		half occlusion = GetOcclusion(_OcclusionMap, mainTexcoord, _OcclusionStrength, _OcclusionChannel, albedo);
	#else
		half occlusion = 1.0;
	#endif

	#if defined(TCP2_HYBRID_URP) && defined(_SCREEN_SPACE_OCCLUSION)
		occlusion = min(occlusion, aoFactor.indirectAmbientOcclusion);
	#endif

	// Setup lighting environment (Built-In)
	#if !defined(TCP2_HYBRID_URP) && defined(UNITY_PASS_FORWARDBASE)
		UnityGI gi;
		UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
		gi.indirect.diffuse = 0;
		gi.indirect.specular = 0;
		gi.light.color = lightColor;
		gi.light.dir = lightDir;

		// Call GI (lightmaps/SH/reflections) lighting function
		UnityGIInput giInput;
		UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
		giInput.light = gi.light;
		giInput.worldPos = positionWS;
		giInput.worldViewDir = viewDirWS;
		giInput.atten = atten;
		#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
			giInput.lightmapUV = input.lmap;
		#else
			giInput.lightmapUV = 0.0;
		#endif
		giInput.ambient.rgb = 0.0;
		giInput.probeHDR[0] = unity_SpecCube0_HDR;
		giInput.probeHDR[1] = unity_SpecCube1_HDR;
		#if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
			giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
		#endif
		#ifdef UNITY_SPECCUBE_BOX_PROJECTION
			giInput.boxMax[0] = unity_SpecCube0_BoxMax;
			giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
			giInput.boxMax[1] = unity_SpecCube1_BoxMax;
			giInput.boxMin[1] = unity_SpecCube1_BoxMin;
			giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
		#endif

		half3 shNormal = (_SingleIndirectColor > 0) ? viewDirWS : normalWS;
		#if defined(TCP2_REFLECTIONS)
			// GI: indirect diffuse & specular
			half smoothness = _ReflectionSmoothness;
			Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(smoothness, giInput.worldViewDir, normalWS, half3(0,0,0));
			gi = UnityGlobalIllumination(giInput, occlusion, shNormal, g);
		#else
			// GI: indirect diffuse only
			gi = UnityGlobalIllumination(giInput, occlusion, shNormal);
		#endif

		gi.light.color = _LightColor0.rgb; // remove attenuation, taken into account separately
	#endif

	// Apply ambient/indirect lighting
	#if defined(UNITY_PASS_FORWARDBASE)
		#if !defined(TCP2_MOBILE)
			if (_IndirectIntensity > 0)
		#endif
	{
		#if defined(TCP2_HYBRID_URP)
			#ifdef LIGHTMAP_ON
				// Normal is required in case Directional lightmaps are baked
				half3 bakedGI = SampleLightmap(input.lightmapUV.xy, normalWS);
				MixRealtimeAndBakedGI(mainLight, normalWS, bakedGI, half4(0, 0, 0, 0));
			#else
				// Sample SH fully per-pixel
				half3 bakedGI = SampleSH(_SingleIndirectColor > 0 ? viewDirWS : normalWS);
			#endif
			half3 indirectDiffuse = bakedGI * occlusion * albedo.rgb * _IndirectIntensity;
			color += indirectDiffuse;
		#else
			half3 indirectDiffuse = gi.indirect.diffuse * albedo.rgb * _IndirectIntensity;
			color.rgb += indirectDiffuse;
		#endif
	}
	#endif

	// Calculate N.V
	#if defined(TCP2_RIM_LIGHTING) || (defined(TCP2_REFLECTIONS) && defined(TCP2_REFLECTIONS_FRESNEL))
		#if defined(TCP2_MOBILE)
			half ndv = input.tangentWS.w;
		#else
			half ndv = 1 - max(0, dot(viewDirWS, normalWS));
		#endif
	#endif

	// Rim Lighting
	#if defined(TCP2_RIM_LIGHTING)
		#if defined(UNITY_PASS_FORWARDBASE) || defined(TCP2_RIM_LIGHTING_LIGHTMASK)
			half rim = smoothstep(_RimMin, _RimMax, ndv);
			emission.rgb += rimMask.rgb * rim * _RimColor.rgb;
		#endif
	#endif

	// Specular
	#if defined(TCP2_SPECULAR)

		half specularMap = 1.0;
		#if defined(TCP2_MOBILE)
			specularMap *= tex2D(_SpecGlossMap, mainTexcoord).a;
		#else
			if (_SpecularMapType >= 5)
			{
				specularMap *= tex2D(_SpecGlossMap, mainTexcoord).a;
			}
			else if (_SpecularMapType >= 4)
			{
				specularMap *= tex2D(_SpecGlossMap, mainTexcoord).b;
			}
			else if (_SpecularMapType >= 3)
			{
				specularMap *= tex2D(_SpecGlossMap, mainTexcoord).g;
			}
			else if (_SpecularMapType >= 2)
			{
				specularMap *= tex2D(_SpecGlossMap, mainTexcoord).r;
			}
			else if (_SpecularMapType >= 1)
			{
				specularMap *= albedo.a;
			}
		#endif

		half spec = CalculateSpecular(lightDir, viewDirWS, normalWS, specularMap);
		emission.rgb += spec * atten * ndl * lightColor.rgb * _SpecularColor.rgb;

		#if defined(UNITY_PASS_META)
			meta_specular = specularMap * _SpecularColor.rgb;
			meta_albedo += specularMap * _SpecularColor.rgb * max(0.00001, _SpecularRoughness * _SpecularRoughness) * 0.5;
		#endif

	#endif

	// Meta pass
	#if defined(UNITY_PASS_META)
		#if defined(TCP2_HYBRID_URP)
			MetaInput metaInput;
		#else
			UnityMetaInput metaInput;
			UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaInput);
		#endif
		metaInput.Albedo = meta_albedo.rgb;
		metaInput.SpecularColor = meta_specular.rgb;
		metaInput.Emission = meta_emission.rgb;

		#if defined(TCP2_HYBRID_URP)
			return MetaFragment(metaInput);
		#else
			return UnityMetaFragment(metaInput);
		#endif
	#endif

	// Additional lights loop
	#if defined(TCP2_HYBRID_URP) && defined(_ADDITIONAL_LIGHTS)
		uint pixelLightCount = GetAdditionalLightsCount();
		for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
		{
			Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);

			#if defined(_SCREEN_SPACE_OCCLUSION)
				light.color *= aoFactor.directAmbientOcclusion;
			#endif

			half atten = light.shadowAttenuation * light.distanceAttenuation;
			half3 lightDir = light.direction;
			half3 lightColor = light.color.rgb;

			half ndl = dot(normalWS, lightDir);
			half ndlWrapped = ndl * 0.5 + 0.5;
			ndl = saturate(ndl);

			// Calculate ramp
			half3 ramp = CalculateRamp(ndlWrapped);

			// Apply attenuation (shadowmaps & point/spot lights attenuation)
			ramp *= atten;

			#if defined(TCP2_RIM_LIGHTING)
				#if defined(TCP2_RIM_LIGHTING_LIGHTMASK)
					half3 rimMask = ramp.xxx * lightColor.rgb;
				#else
					half3 rimMask = half3(1, 1, 1);
				#endif
			#endif

			// Apply highlight color only
			ramp = lerp(half3(0,0,0), _HColor.rgb, ramp);

			// Output color
			color += albedo.rgb * lightColor.rgb * ramp;

			// Specular
			#if defined(TCP2_SPECULAR)
				half spec = CalculateSpecular(lightDir, viewDirWS, normalWS, specularMap);
				emission.rgb += spec * atten * ndl * lightColor.rgb * _SpecularColor.rgb;
			#endif
			// Rim Lighting
			#if defined(TCP2_RIM_LIGHTING) && defined(TCP2_RIM_LIGHTING_LIGHTMASK)
				emission.rgb += rimMask * rim * _RimColor.rgb;
			#endif
		}
		#ifdef _ADDITIONAL_LIGHTS_VERTEX
			color += input.vertexLights * albedo.rgb;
		#endif
	#endif

	// Environment Reflection
	#if defined(TCP2_REFLECTIONS)
		half3 reflections = half3(0, 0, 0);

		half reflectionRoughness = _ReflectionSmoothness;
		half reflectionMask = 1.0;
		#if defined(TCP2_MOBILE)
			reflectionRoughness *= tex2D(_ReflectionTex, mainTexcoord).a;
		#else
			if (_ReflectionMapType > 0)
			{
				if (_ReflectionMapType <= 1)
				{
					reflectionRoughness *= albedo.a;
				}
				else if (_ReflectionMapType <= 2)
				{
					reflectionRoughness *= tex2D(_ReflectionTex, mainTexcoord).r;
				}
				else if (_ReflectionMapType <= 3)
				{
					reflectionRoughness *= tex2D(_ReflectionTex, mainTexcoord).g;
				}
				else if (_ReflectionMapType <= 4)
				{
					reflectionRoughness *= tex2D(_ReflectionTex, mainTexcoord).b;
				}
				else if (_ReflectionMapType <= 5)
				{
					reflectionRoughness *= tex2D(_ReflectionTex, mainTexcoord).a;
				}
				else if (_ReflectionMapType <= 6)
				{
					reflectionMask *= albedo.a;
				}
				else if (_ReflectionMapType <= 7)
				{
					reflectionMask *= tex2D(_ReflectionTex, mainTexcoord).r;
				}
				else if (_ReflectionMapType <= 8)
				{
					reflectionMask *= tex2D(_ReflectionTex, mainTexcoord).g;
				}
				else if (_ReflectionMapType <= 9)
				{
					reflectionMask *= tex2D(_ReflectionTex, mainTexcoord).b;
				}
				else if (_ReflectionMapType <= 10)
				{
					reflectionMask *= tex2D(_ReflectionTex, mainTexcoord).a;
				}
			}
		#endif
		reflectionRoughness = 1 - reflectionRoughness;

		#if defined(TCP2_HYBRID_URP)
			half3 reflectVector = reflect(-viewDirWS, normalWS);
			half3 indirectSpecular = GlossyEnvironmentReflection(reflectVector, reflectionRoughness, occlusion);
		#else
			half3 indirectSpecular = gi.indirect.specular;
		#endif
		half reflectionRoughness4 = max(pow(reflectionRoughness, 4), 6.103515625e-5);
		float surfaceReductionRefl = 1.0 / (reflectionRoughness4 + 1.0);
		reflections += indirectSpecular * reflectionMask * surfaceReductionRefl * _ReflectionColor.rgb;

		#if defined(TCP2_REFLECTIONS_FRESNEL)
			half fresnelTerm = smoothstep(_FresnelMin, _FresnelMax, ndv);
			reflections *= fresnelTerm;
		#endif

		emission.rgb += reflections;
	#endif

	// Premultiply blending
	#if defined(_ALPHAPREMULTIPLY_ON)
		color.rgb *= alpha;
	#else
		alpha = 1;
	#endif

	color += emission;

	// Fog
	#if defined(TCP2_HYBRID_URP)
		color = MixFog(color, input.worldPos.w);
	#else
		UNITY_EXTRACT_FOG_FROM_WORLD_POS(input);
		UNITY_APPLY_FOG(_unity_fogCoord, color);
	#endif

	return half4(color, alpha);
}

//================================================================================================================================
//================================================================================================================================

// OUTLINE

//================================================================================================================================
//================================================================================================================================

#if defined(TCP2_OUTLINE_LIGHTING_MAIN) || (defined(TCP2_HYBRID_URP) && defined(TCP2_OUTLINE_LIGHTING_ALL)) || defined(TCP2_OUTLINE_LIGHTING_INDIRECT)
	#define TCP2_OUTLINE_LIGHTING
#endif

struct Attributes_Outline
{
	float4 vertex : POSITION;
	float3 normal : NORMAL;
#if defined(TCP2_UV1_AS_NORMALS) || defined(TCP2_OUTLINE_TEXTURED_VERTEX) || defined(TCP2_OUTLINE_TEXTURED_FRAGMENT)
	float4 texcoord0 : TEXCOORD0;
#endif
#if defined(TCP2_UV2_AS_NORMALS) || (!defined(TCP2_HYBRID_URP) && defined(TCP2_OUTLINE_LIGHTING_ALL))
	float4 texcoord1 : TEXCOORD1;
#endif
#if defined(TCP2_UV3_AS_NORMALS)
	float4 texcoord2 : TEXCOORD2;
#endif
#if defined(TCP2_UV4_AS_NORMALS)
	float4 texcoord3 : TEXCOORD3;
#endif
#if defined(TCP2_COLORS_AS_NORMALS)
	float4 vertexColor : COLOR;
#endif
#if defined(TCP2_TANGENT_AS_NORMALS) || (defined(TCP2_OUTLINE_LIGHTING_ALL) && defined(_ADDITIONAL_LIGHTS_VERTEX))
	float4 tangent : TANGENT;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings_Outline
{
	float4 pos       : SV_POSITION;
	float4 vcolor    : TEXCOORD0;
	float4 worldPos  : TEXCOORD1; // xyz = worldPos  w = fogFactor
#if defined(TCP2_OUTLINE_TEXTURED_FRAGMENT)
	float4 texcoord0 : TEXCOORD2;
#endif
#if defined(TCP2_OUTLINE_LIGHTING)
	float3 normal    : TEXCOORD3;
#endif
#if defined(TCP2_HYBRID_URP)
	#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
		float4 shadowCoord    : TEXCOORD4; // compute shadow coord per-vertex for the main light
	#endif
	#if defined(TCP2_HYBRID_URP) && defined(TCP2_OUTLINE_LIGHTING_ALL) && defined(_ADDITIONAL_LIGHTS_VERTEX)
		half3 vertexLights : TEXCOORD5;
	#endif
#else
	UNITY_LIGHTING_COORDS(5,6)
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

Varyings_Outline vertex_outline (Attributes_Outline input)
{
	Varyings_Outline output = (Varyings_Outline)0;

	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	#if defined(TCP2_HYBRID_URP)
		VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
		#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
			output.shadowCoord = GetShadowCoord(vertexInput);
		#endif
		float3 positionWS = vertexInput.positionWS;
	#else
		float3 positionWS = mul(unity_ObjectToWorld, input.vertex).xyz;
		UNITY_TRANSFER_LIGHTING(output, input.texcoord1.xy);
	#endif

	#ifdef TCP2_COLORS_AS_NORMALS
		//Vertex Color for Normals
		float3 normal = (input.vertexColor.xyz*2) - 1;
	#elif TCP2_TANGENT_AS_NORMALS
		//Tangent for Normals
		float3 normal = input.tangent.xyz;
	#elif TCP2_UV1_AS_NORMALS || TCP2_UV2_AS_NORMALS || TCP2_UV3_AS_NORMALS || TCP2_UV4_AS_NORMALS
		#if TCP2_UV1_AS_NORMALS
			#define uvChannel texcoord0
		#elif TCP2_UV2_AS_NORMALS
			#define uvChannel texcoord1
		#elif TCP2_UV3_AS_NORMALS
			#define uvChannel texcoord2
		#elif TCP2_UV4_AS_NORMALS
			#define uvChannel texcoord3
		#endif

		#if TCP2_UV_NORMALS_FULL
		//UV for Normals, full
		float3 normal = input.uvChannel.xyz;
		#else
		//UV for Normals, compressed
		#if TCP2_UV_NORMALS_ZW
			#define ch1 z
			#define ch2 w
		#else
			#define ch1 x
			#define ch2 y
		#endif
		float3 n;
		//unpack uvs
		input.uvChannel.ch1 = input.uvChannel.ch1 * 255.0/16.0;
		n.x = floor(input.uvChannel.ch1) / 15.0;
		n.y = frac(input.uvChannel.ch1) * 16.0 / 15.0;
		//- get z
		n.z = input.uvChannel.ch2;
		//- transform
		n = n*2 - 1;
		float3 normal = n;
		#endif
	#else
		float3 normal = input.normal;
	#endif

	/// #if TCP2_ZSMOOTH_ON
		/// //Correct Z artefacts
		/// normal = UnityObjectToViewPos(normal);
		/// normal.z = -_ZSmooth;
	/// #endif

	#if !defined(SHADOWCASTER_PASS)
		output.pos = UnityObjectToClipPos(input.vertex.xyz);
		normal = mul(unity_ObjectToWorld, float4(normal, 0)).xyz;
		float2 clipNormals = normalize(mul(UNITY_MATRIX_VP, float4(normal,0)).xy);
		#if defined(TCP2_OUTLINE_CONST_SIZE)
			float2 outlineWidth = (_OutlineWidth * output.pos.w) / (_ScreenParams.xy / 2.0);
			output.pos.xy += clipNormals.xy * outlineWidth;
		#elif defined(TCP2_OUTLINE_MIN_SIZE)
			float screenRatio = _ScreenParams.x / _ScreenParams.y;
			float2 outlineWidth = max(
				(_OutlineMinWidth * output.pos.w) / (_ScreenParams.xy / 2.0),
				(_OutlineWidth / 100) * float2(1.0, screenRatio)
			);
			output.pos.xy += clipNormals.xy * outlineWidth;
		#elif defined(TCP2_OUTLINE_MIN_MAX_SIZE)
			float screenRatio = _ScreenParams.x / _ScreenParams.y;
			float2 outlineWidth = max(
				(_OutlineMinWidth * output.pos.w) / (_ScreenParams.xy / 2.0),
		        (_OutlineWidth / 100) * float2(1.0, screenRatio)
		    );
			outlineWidth = min(
				(_OutlineMaxWidth * output.pos.w) / (_ScreenParams.xy / 2.0),
		        outlineWidth
		    );
			output.pos.xy += clipNormals.xy * outlineWidth;
		#else
			float screenRatio = _ScreenParams.x / _ScreenParams.y;
			output.pos.xy += clipNormals.xy * (_OutlineWidth / 100) * float2(1.0, screenRatio);
		#endif
	#else
		input.vertex = input.vertex + float4(normal,0) * _OutlineWidth * 0.01;
	#endif

	output.vcolor.rgba = _OutlineColor.rgba;
	float4 clipPos = output.pos;

	#if defined(TCP2_OUTLINE_LIGHTING_ALL) && defined(_ADDITIONAL_LIGHTS_VERTEX)
		// Vertex lighting
		VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normal, input.tangent);
		output.vertexLights = VertexLighting(positionWS, vertexNormalInput.normalWS);
	#endif

	// World Position
	output.worldPos.xyz = positionWS;

	// Computes fog factor
	#if defined(TCP2_HYBRID_URP)
		output.worldPos.w = ComputeFogFactor(output.pos.z);
	#else
		UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(output, output.pos);
	#endif

	// Lighting & Texture
	#if defined(TCP2_OUTLINE_LIGHTING)
		output.normal = normalize(UnityObjectToWorldNormal(input.normal));
	#endif

	#if defined(TCP2_OUTLINE_TEXTURED_VERTEX)
		half4 outlineTexture = tex2Dlod(_BaseMap, float4(input.texcoord0.xy * _BaseMap_ST.xy + _BaseMap_ST.zw, 0, _OutlineTextureLOD));
		output.vcolor *= outlineTexture;
	#endif

	#if defined(TCP2_OUTLINE_TEXTURED_FRAGMENT)
		output.texcoord0.xy = input.texcoord0.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;
		output.texcoord0.zw = input.texcoord0.zw;
	#endif

	return output;
}

float4 fragment_outline (Varyings_Outline input) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	#if defined(TCP2_OUTLINE_TEXTURED_FRAGMENT)
		// Manual mip map calculation so that we can apply a max value
		float2 dx = ddx(input.texcoord0.xy);
		float2 dy = ddy(input.texcoord0.xy);
		float delta = max(dot(dx, dx), dot(dy, dy));
		float mipMapLevel = max(0.5 * log2(delta), _OutlineTextureLOD);

		half4 albedo = tex2Dlod(_BaseMap, float4(input.texcoord0.xy, 0, mipMapLevel));
	#else
		half4 albedo = half4(1, 1, 1, 1);
	#endif

	// Output Color
	half4 outlineColor = half4(1, 1, 1, 1);

	#if defined(TCP2_OUTLINE_LIGHTING)
			float3 positionWS = input.worldPos.xyz;
			float3 normalWS = input.normal;
			#if defined(TCP2_HYBRID_URP)
				half3 viewDirWS = TCP2_SafeNormalize(GetCameraPositionWS() - positionWS);
			#else
				half3 viewDirWS = TCP2_SafeNormalize(_WorldSpaceCameraPos.xyz - positionWS);
			#endif

		#if defined(TCP2_OUTLINE_LIGHTING_INDIRECT)
			// Indirect only
			outlineColor = half4(0, 0, 0, 1);
		#else
			// Main directional light
			#if defined(TCP2_HYBRID_URP)
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					float4 shadowCoord = input.shadowCoord;
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif
				Light mainLight = GetMainLight(shadowCoord);
				half3 lightDir = mainLight.direction;
				half3 lightColor = mainLight.color.rgb;
				half atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
			#else
				half3 lightDir = normalize(UnityWorldSpaceLightDir(positionWS));
				half3 lightColor = _LightColor0.rgb;

				TCP2_LIGHT_ATTENUATION(input, positionWS)
				#if defined(_RECEIVE_SHADOWS_OFF)
					half atten = attenuation;
				#else
					half atten = shadow * attenuation;
				#endif
			#endif

			half ndl = dot(normalWS, lightDir);
			half ndlWrapped = ndl * 0.5 + 0.5;

			// Calculate ramp
			half3 ramp = CalculateRamp(ndlWrapped);

			// Apply attenuation
			ramp *= atten;

			#if defined(TCP2_OUTLINE_TEXTURED_FRAGMENT) && defined(TCP2_SHADOW_TEXTURE)
				half4 shadowAlbedo = tex2Dlod(_BaseMap, float4(input.texcoord0.xy, 0, mipMapLevel));
				albedo = lerp(shadowAlbedo, albedo, ramp.x);
			#endif

			// Highlight/shadow colors
			ramp = lerp(_SColor.rgb, _HColor.rgb, ramp);

			// Apply ramp
			outlineColor.rgb *= lerp(half3(1,1,1), ramp, _DirectIntensityOutline) * lightColor;
		#endif

	#endif

	// Apply albedo
	outlineColor.rgb *= albedo.rgb;

	#if defined(TCP2_OUTLINE_LIGHTING)

		half occlusion = 1.0;

		// Additional lights loop
		#if defined(TCP2_HYBRID_URP) && defined(TCP2_OUTLINE_LIGHTING_ALL) && defined(_ADDITIONAL_LIGHTS)
			uint additionalLightsCount = GetAdditionalLightsCount();
			for (uint lightIndex = 0u; lightIndex < additionalLightsCount; ++lightIndex)
			{
				Light light = GetAdditionalLight(lightIndex, positionWS);
				half atten = light.shadowAttenuation * light.distanceAttenuation;
				half3 lightDir = light.direction;
				half3 lightColor = light.color.rgb;

				half ndl = dot(normalWS, lightDir);
				half ndlWrapped = ndl * 0.5 + 0.5;

				// Calculate ramp
				half3 ramp = CalculateRamp(ndlWrapped);

				// Apply attenuation (shadowmaps & point/spot lights attenuation)
				ramp *= atten;

				// Apply highlight color only
				ramp = lerp(half3(0,0,0), _HColor.rgb, ramp);

				// Apply ramp
				outlineColor.rgb += ramp * _DirectIntensityOutline * lightColor * albedo.rgb;
			}
		#endif
		#if defined(TCP2_OUTLINE_LIGHTING_ALL) && defined(_ADDITIONAL_LIGHTS_VERTEX)
			outlineColor.rgb += input.vertexLights * albedo.rgb;
		#endif

		// Apply ambient/indirect lighting
		#if defined(TCP2_HYBRID_URP)
			// Sample SH fully per-pixel
			half3 bakedGI = SampleSH(_SingleIndirectColor > 0 ? viewDirWS : normalWS);
			half3 indirectDiffuse = bakedGI * occlusion * albedo.rgb * _IndirectIntensityOutline;
			outlineColor.rgb += indirectDiffuse;
		#else
			half3 shNormal = (_SingleIndirectColor > 0) ? viewDirWS : normalWS;
			half3 bakedGI = ShadeSHPerPixel(shNormal, half3(0,0,0), positionWS);
			half3 indirectDiffuse = bakedGI * occlusion * albedo.rgb * _IndirectIntensityOutline;
			outlineColor.rgb += indirectDiffuse;
		#endif

	#endif

	outlineColor.rgba *= input.vcolor.rgba;

	// Fog
	#if defined(TCP2_HYBRID_URP)
		outlineColor.rgb = MixFog(outlineColor.rgb, input.worldPos.w);
	#else
		UNITY_EXTRACT_FOG_FROM_WORLD_POS(input);
		UNITY_APPLY_FOG(_unity_fogCoord, outlineColor.rgb);
	#endif

	return outlineColor;
}