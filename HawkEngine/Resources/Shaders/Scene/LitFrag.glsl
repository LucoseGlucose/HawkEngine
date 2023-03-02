#version 460

in VSOut
{
	vec2 uv;
	vec3 worldPosition;
	mat3 tbnMat;
	vec3 geometryNormal;
}
fsIn;

out vec4 outColor;

uniform vec3 uCameraPos;
uniform samplerCube uIrradianceCubeB;
uniform samplerCube uReflectionCubeB;
uniform sampler2D uBrdfLutB;
uniform float uMaxReflectionLod = 4.0;
uniform vec3 uAmbientColor = vec3(1);

uniform sampler2D uAlbedoTexW;
uniform vec4 uAlbedo = vec4(1);
uniform float uAlphaClip = 0;

uniform sampler2D uMetallicMapW;
uniform float uMetallic = .5;
uniform sampler2D uRoughnessMapW;
uniform float uRoughness = .5;

uniform sampler2D uNormalMapN;
uniform float uNormalStrength = 1;

uniform sampler2D uAOMapW;
uniform float uAO = 1;

uniform sampler2D uEmissiveMapW;
uniform vec3 uEmissive;

vec3 poissonDisk[16] = vec3[]
( 
   vec3(-0.94201624, -0.39906216, 0.63561923), 
   vec3(0.94558609, -0.76890725, 0.25371937), 
   vec3(-0.094184101, -0.92938870, -0.53628195), 
   vec3(0.34495938, 0.29387760, -0.26172738), 
   vec3(-0.91588581, 0.45771432, -0.12783648), 
   vec3(-0.81544232, -0.87912464, -0.62819263), 
   vec3(-0.38277543, 0.27676845, 0.124517283), 
   vec3(0.97484398, 0.75648379, -0.24162738), 
   vec3(0.44323325, -0.97511554, -0.82935162), 
   vec3(0.53742981, -0.47373420, -0.19283621), 
   vec3(-0.26496911, -0.41893023, -0.56748292), 
   vec3(0.79197514, 0.19090188, -0.74627364), 
   vec3(-0.24188840, 0.99706507, 0.34512667), 
   vec3(-0.81409955, 0.91437590, -0.12523892), 
   vec3(0.19984126, 0.78641367, -0.46728881), 
   vec3(0.14383161, -0.14100790, 0.23555612) 
);

struct Light
{
	int uType;
	vec3 uColor;
	vec3 uPosition;
	vec3 uDirection;
	vec2 uFalloff;
	vec2 uRadius;
	sampler2D uShadowTexW;
	samplerCube uShadowCubeW;
	float uFarPlane;
	mat4 uShadowMat;
	vec2 uShadowNormalBias;
	int uShadowMapSamples;
	float uShadowSoftness;
	float uShadowNoise;
};

uniform Light[5] uLights;

const float PI = 3.14159265359;

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
	float a = roughness*roughness;
	float a2 = a*a;
	float NdotH = max(dot(N, H), 0.0);
	float NdotH2 = NdotH*NdotH;

	float nom   = a2;
	float denom = (NdotH2 * (a2 - 1.0) + 1.0);
	denom = PI * denom * denom;

	return nom / denom;
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
	float r = (roughness + 1.0);
	float k = (r*r) / 8.0;

	float nom   = NdotV;
	float denom = NdotV * (1.0 - k) + k;

	return nom / denom;
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
	float NdotV = max(dot(N, V), 0.0);
	float NdotL = max(dot(N, L), 0.0);
	float ggx2 = GeometrySchlickGGX(NdotV, roughness);
	float ggx1 = GeometrySchlickGGX(NdotL, roughness);

	return ggx1 * ggx2;
}

vec3 fresnelSchlick(float cosTheta, vec3 F0)
{
	return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

vec3 fresnelSchlickRoughness(float cosTheta, vec3 F0, float roughness)
{
    return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

float random(vec3 seed, int i)
{
	vec4 seed4 = vec4(seed,i);
	float dot_product = dot(seed4, vec4(12.9898,78.233,45.164,94.673));
	return fract(sin(dot_product) * 43758.5453);
}

bool calcShadow(in Light l, in vec3 lightDir, in vec3 normal, in vec3 lightCoords, out float shadow)
{
	if (lightCoords.z > 1.0) return true;
	else 
	{
		float bias = max(l.uShadowNormalBias.y * (1.0 - dot(normal, lightDir)), l.uShadowNormalBias.x);
		vec2 pixelSize = 1.0 / textureSize(l.uShadowTexW, 0) * l.uShadowSoftness;

		for(int y = -l.uShadowMapSamples; y <= l.uShadowMapSamples; y++)
		{
			for(int x = -l.uShadowMapSamples; x <= l.uShadowMapSamples; x++)
			{
				int index = int(16.0 * random(floor(fsIn.worldPosition * 1000.0), 21)) % 16;
				float closestDepth = texture(l.uShadowTexW, lightCoords.xy + vec2(x, y) * pixelSize + poissonDisk[index].xy / l.uShadowNoise).r;
				float currentDepth = lightCoords.z;

				float diff = currentDepth - (closestDepth + bias);
				if (diff > 0.0) shadow += 1.0;
			}
		}
		shadow /= pow((l.uShadowMapSamples * 2 + 1), 2);
	}

	return shadow < 1.0;
}

bool calcDirectionalLight(in Light l, in vec3 viewDir, in vec3 normal, out vec3 radiance, out vec3 lightDir, out float shadow)
{
	lightDir = -l.uDirection;
	if (dot(fsIn.geometryNormal, lightDir) <= 0.0) return false;

	shadow = 0.0;
	vec4 lightSpacePos = l.uShadowMat * vec4(fsIn.worldPosition, 1);
	vec3 lightCoords = lightSpacePos.xyz / lightSpacePos.w;
	lightCoords = lightCoords * .5 + .5;

	if (l.uShadowMapSamples >= 0 && !calcShadow(l, lightDir, normal, lightCoords, shadow)) return false;

	radiance = l.uColor;
	return true;
}

bool calcPointLight(in Light l, in vec3 viewDir, in vec3 normal, out vec3 radiance, out vec3 lightDir, out float shadow)
{

	lightDir = normalize(l.uPosition - fsIn.worldPosition);
	if (dot(fsIn.geometryNormal, lightDir) <= 0.0) return false;

	vec3 lightCoords = fsIn.worldPosition - l.uPosition;
	float bias = max(l.uShadowNormalBias.y * (1.0 - dot(normal, lightDir)), l.uShadowNormalBias.x);

	float currentDepth = length(lightCoords);
	float closestDepth = texture(l.uShadowCubeW, lightCoords).r * l.uFarPlane;

	float diff = currentDepth - (closestDepth + bias);
	if (diff > 0.0) shadow = 1.0;

	float dist = length(l.uPosition - fsIn.worldPosition);
	float attenuation = 1.0 / (1 + l.uFalloff.x * dist + l.uFalloff.y * (dist * dist));
	radiance = l.uColor * attenuation;

	return true;
}

bool calcSpotLight(in Light l, in vec3 viewDir, in vec3 normal, out vec3 radiance, out vec3 lightDir, out float shadow)
{
	lightDir = normalize(l.uPosition - fsIn.worldPosition);
	if (dot(fsIn.geometryNormal, lightDir) <= 0.0) return false;

	shadow = 0.0;
	vec4 lightSpacePos = l.uShadowMat * vec4(fsIn.worldPosition, 1);
	vec3 lightCoords = lightSpacePos.xyz / lightSpacePos.w;
	lightCoords = lightCoords * .5 + .5;

	if (l.uShadowMapSamples >= 0 && !calcShadow(l, lightDir, normal, lightCoords, shadow)) return false;

	float dist = length(l.uPosition - fsIn.worldPosition);
	float attenuation = 1.0 / (1 + l.uFalloff.x * dist + l.uFalloff.y * (dist * dist));

	float theta = dot(lightDir, -l.uDirection);
    float epsilon = (l.uRadius.x - l.uRadius.y);
    float intensity = clamp((theta - l.uRadius.y) / epsilon, 0.0, 1.0);

	if (intensity <= 0.0) return false;
	radiance = l.uColor * attenuation * intensity;

	return true;
}

bool calcLight(in Light l, in vec3 viewDir, in vec3 normal, out vec3 radiance, out vec3 lightDir, out float shadow)
{
	if (l.uType == 0) return false;

	if (l.uType == 1) 
	{
		return calcDirectionalLight(l, viewDir, normal, radiance, lightDir, shadow);
	}

	if (l.uType == 2)
	{
		return calcPointLight(l, viewDir, normal, radiance, lightDir, shadow);
	}

	if (l.uType == 3)
	{
		return calcSpotLight(l, viewDir, normal, radiance, lightDir, shadow);
	}

	return false;
}

void main()
{
	vec4 albedo = uAlbedo * texture(uAlbedoTexW, fsIn.uv);
	if (albedo.a <= uAlphaClip) discard;

	float metallic = texture(uMetallicMapW, fsIn.uv).r * uMetallic;
	float roughness = texture(uRoughnessMapW, fsIn.uv).r * uRoughness;

	vec3 normal = texture(uNormalMapN, fsIn.uv).xyz * 2.0 - 1.0;
	normal.xy *= uNormalStrength;
	normal = normalize(fsIn.tbnMat * normal);

	vec3 viewDir = normalize(uCameraPos - fsIn.worldPosition);
	vec3 reflectDir = reflect(-viewDir, normal);

	vec3 F0 = vec3(0.04); 
	F0 = mix(F0, albedo.rgb, metallic);

	vec3 Lo = vec3(0.0);
	for(int i = 0; i < 5; ++i) 
	{
		vec3 radiance;
		vec3 lightDir;
		float shadow;

		if (!calcLight(uLights[i], viewDir, normal, radiance, lightDir, shadow)) continue;

		vec3 halfwayVec = normalize(viewDir + lightDir);

		float NDF = DistributionGGX(normal, halfwayVec, roughness);
		float G = GeometrySmith(normal, viewDir, lightDir, roughness);
		vec3 F = fresnelSchlick(max(dot(halfwayVec, viewDir), 0.0), F0);
		   
		vec3 numerator = NDF * G * F;
		float denominator = 4.0 * max(dot(normal, viewDir), 0.0) * max(dot(normal, lightDir), 0.0) + 0.0001;
		vec3 specular = numerator / denominator;
		
		vec3 kS = F;
		vec3 kD = vec3(1.0) - kS;
		kD *= 1.0 - metallic;

		float NdotL = max(dot(normal, lightDir), 0.0);
		Lo += (kD * albedo.rgb / PI + specular) * radiance * NdotL * (1.0 - shadow);
	}   
	
	vec3 F = fresnelSchlickRoughness(max(dot(fsIn.geometryNormal, viewDir), 0.0), F0, roughness);
    
    vec3 kS = F;
    vec3 kD = 1.0 - kS;
    kD *= 1.0 - metallic;
    
    vec3 irradiance = texture(uIrradianceCubeB, normal).rgb;
    vec3 diffuse = irradiance * albedo.rgb;
    
    vec3 prefilteredColor = textureLod(uReflectionCubeB, reflectDir,  roughness * uMaxReflectionLod).rgb;
    vec2 brdf = texture(uBrdfLutB, vec2(max(dot(fsIn.geometryNormal, viewDir), 0.0), roughness)).rg;
    vec3 specular = prefilteredColor * (F * brdf.x + brdf.y);

    vec3 ambient = kD * diffuse + specular;
	ambient *= uAmbientColor;
	ambient *= texture(uAOMapW, fsIn.uv).r * uAO;

	vec3 emissiveColor = texture(uEmissiveMapW, fsIn.uv).rgb * uEmissive;

	outColor = vec4(ambient + Lo + emissiveColor, albedo.a);
}