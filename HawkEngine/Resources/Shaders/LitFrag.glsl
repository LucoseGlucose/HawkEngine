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

uniform vec2 uShadowNormalBias = vec2(.002f, .008f);
uniform int uShadowMapSamples = 1;
uniform float uShadowSoftness = .65;
uniform float uShadowNoise = 4500;
uniform float uShadowBailThreshold = -.1;

vec2 poissonDisk[16] = vec2[]
( 
   vec2( -0.94201624, -0.39906216 ), 
   vec2( 0.94558609, -0.76890725 ), 
   vec2( -0.094184101, -0.92938870 ), 
   vec2( 0.34495938, 0.29387760 ), 
   vec2( -0.91588581, 0.45771432 ), 
   vec2( -0.81544232, -0.87912464 ), 
   vec2( -0.38277543, 0.27676845 ), 
   vec2( 0.97484398, 0.75648379 ), 
   vec2( 0.44323325, -0.97511554 ), 
   vec2( 0.53742981, -0.47373420 ), 
   vec2( -0.26496911, -0.41893023 ), 
   vec2( 0.79197514, 0.19090188 ), 
   vec2( -0.24188840, 0.99706507 ), 
   vec2( -0.81409955, 0.91437590 ), 
   vec2( 0.19984126, 0.78641367 ), 
   vec2( 0.14383161, -0.14100790 ) 
);

struct Light
{
	int uType;
	vec3 uColor;
	vec3 uPosition;
	vec2 uFalloff;
	sampler2D uShadowTexW;
	mat4 uMat;
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

float random(vec3 seed, int i)
{
	vec4 seed4 = vec4(seed,i);
	float dot_product = dot(seed4, vec4(12.9898,78.233,45.164,94.673));
	return fract(sin(dot_product) * 43758.5453);
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
        if (uLights[i].uColor == vec3(0)) break;

        vec3 L = normalize(uLights[i].uPosition - outWorldPosition * min(uLights[i].uType, 1.0));
        float shadow = 1.0;

        if (uLights[i].uType == 0)
        {
            vec4 lightSpacePos = uLights[i].uMat * vec4(outWorldPosition, 1);
		    vec3 lightCoords = lightSpacePos.xyz / lightSpacePos.w;
		    lightCoords = lightCoords * .5 + .5;

		    if (lightCoords.z > 1.0) shadow = 0.0;
            else 
            {
                shadow = 0.0;
                float lightDepth = texture(uLights[i].uShadowTexW, lightCoords.xy).r;
		        float bias = max(uShadowNormalBias.y * (1.0 - dot(N, L)), uShadowNormalBias.x);

		        vec2 pixelSize = 1.0 / textureSize(uLights[i].uShadowTexW, 0) * uShadowSoftness;
                bool bail = false;

		        for(int y = -uShadowMapSamples; y <= uShadowMapSamples && !bail; y++)
		        {
		            for(int x = -uShadowMapSamples; x <= uShadowMapSamples && !bail; x++)
		            {
                        int index = int(16.0 * random(floor(outWorldPosition * 1000.0), i)) % 16;
		                float closestDepth = texture(uLights[i].uShadowTexW, lightCoords.xy + vec2(x, y) * pixelSize + poissonDisk[index] / uShadowNoise).r;

                        float diff = lightCoords.z - (closestDepth + bias);
                        if (diff < uShadowBailThreshold) bail = true;
		        		if (diff > 0.0) shadow += 1.0f;
		            }    
		        }
		        shadow /= pow((uShadowMapSamples * 2 + 1), 2);
            }

            if (shadow >= 1.0) continue;
        }
        
        vec3 H = normalize(V + L);
        float dist = length(uLights[i].uPosition - outWorldPosition);

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

        Lo += (kD * albedo.rgb / PI + specular) * radiance * NdotL * (1.0 - shadow);
    }   
    
    vec3 ambient = uAmbientColor * albedo.rgb;
    outColor = vec4(ambient + Lo, albedo.a);
}