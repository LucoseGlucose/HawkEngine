#version 460

in vec2 outUV;

in vec3 outWorldPosition;
in mat3 outTBNMat;

out vec4 outColor;

uniform vec3 uCameraPos;
uniform vec3 uAmbientColor = vec3(.03);

uniform sampler2D uAlbedoTexW;
uniform vec4 uAlbedo = vec4(1);
uniform float uAlphaClip = 0;

uniform sampler2D uMetallicMapW;
uniform float uMetallic = .1;
uniform sampler2D uSmoothnessMapW;
uniform float uSmoothness = .8;

uniform sampler2D uNormalMapN;
uniform float uNormalStrength = 1;

uniform vec2 uShadowNormalBias = vec2(.005, .05);

struct Light
{
	int uType;
	vec3 uColor;
	vec3 uPosition;
	vec2 uFalloff;
	sampler2D uShadowTexW;
	mat4 uProjMat;
	mat4 uViewMat;
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

void main()
{		
    vec4 albedo = uAlbedo * texture(uAlbedoTexW, outUV);
    if (albedo.a <= uAlphaClip) discard;

    float metallic = texture(uMetallicMapW, outUV).r * uMetallic;
    float roughness = 1 - (texture(uSmoothnessMapW, outUV).r * uSmoothness);

    vec3 N = texture(uNormalMapN, outUV).xyz;
	N.xy *= uNormalStrength;
	N = normalize(outTBNMat * N);

    vec3 V = normalize(uCameraPos - outWorldPosition);

    vec3 F0 = vec3(0.04); 
    F0 = mix(F0, albedo.rgb, metallic);

    vec3 Lo = vec3(0.0);
    for(int i = 0; i < 5; ++i) 
    {
        vec3 L = normalize(uLights[i].uPosition - outWorldPosition * min(uLights[i].uType, 1.0));
        vec3 H = normalize(V + L);
        float dist = length(uLights[i].uPosition - outWorldPosition);

        vec4 lightSpacePos = uLights[i].uProjMat * uLights[i].uViewMat * vec4(outWorldPosition, 1);

		vec3 lightCoords = lightSpacePos.xyz / lightSpacePos.w;
		lightCoords = lightCoords * .5 + .5;

		float lightDepth = texture(uLights[i].uShadowTexW, lightCoords.xy).r;
		float bias = max(uShadowNormalBias.y * (1.0 - dot(N, L)), uShadowNormalBias.x);

        float shadow = lightCoords.z - bias > lightDepth ? 0.0 : 1.0;
		if (lightCoords.z > 1.0) shadow = 1.0;
        if (shadow <= 0.0) continue;

        float attenuation = 1.0 / (1 + uLights[i].uFalloff.x * dist + uLights[i].uFalloff.y * (dist * dist));
        vec3 radiance = uLights[i].uColor * attenuation;

        float NDF = DistributionGGX(N, H, roughness);
        float G = GeometrySmith(N, V, L, roughness);
        vec3 F = fresnelSchlick(max(dot(H, V), 0.0), F0);
           
        vec3 numerator = NDF * G * F;
        float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001;
        vec3 specular = numerator / denominator;
        
        vec3 kS = F;
        vec3 kD = vec3(1.0) - kS;
        kD *= 1.0 - metallic;

        float NdotL = max(dot(N, L), 0.0);

        Lo += (kD * albedo.rgb / PI + specular) * radiance * NdotL;
    }   
    
    vec3 ambient = uAmbientColor * albedo.rgb;
    outColor = vec4(ambient + Lo, albedo.a);
}