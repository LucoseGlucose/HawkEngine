#version 460

in vec2 outUV;

in vec3 outWorldPosition;
in mat3 outTBNMat;

out vec4 outColor;

uniform vec3 uCameraPos;
uniform vec3 uAmbientColor = vec3(.03);

uniform vec4 uAlbedo = vec4(1);
uniform sampler2D uAlbedoTexW;
uniform float uAlphaClip = 0;

uniform vec3 uSmoothness = vec3(.25);
uniform sampler2D uSmoothnessMapW;
uniform float uMetallic = 32;
uniform sampler2D uMetallicMapW;

uniform sampler2D uNormalMapN;
uniform float uNormalStrength = 1;

uniform vec2 uShadowNormalBias = vec2(.005, .05);
uniform int uShadowMapSamples = 2;
uniform float uShadowSoftness = .75;

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

void main()
{
	vec4 diffuse = uAlbedo * texture(uAlbedoTexW, outUV);
	if (diffuse.a < uAlphaClip) discard;

	vec3 finalColor = uAmbientColor * diffuse.xyz;

	vec3 specular = uSmoothness * texture(uSmoothnessMapW, outUV).rgb;
	float shininess = uMetallic * texture(uMetallicMapW, outUV).r;

	vec3 normal = texture(uNormalMapN, outUV).xyz;
	normal.xy *= uNormalStrength;
	normal = normalize(outTBNMat * normal);

	vec3 viewDir = normalize(uCameraPos - outWorldPosition);

	for (int i = 0; i < 5; i++)
	{
		if (uLights[i].uColor == vec3(0)) break;

		vec3 lightDir = normalize(uLights[i].uPosition - outWorldPosition * min(uLights[i].uType, 1.0));

		float shadow = 1.0;

        if (uLights[i].uType == 0)
        {
            vec4 lightSpacePos = uLights[i].uProjMat * uLights[i].uViewMat * vec4(outWorldPosition, 1);
		    vec3 lightCoords = lightSpacePos.xyz / lightSpacePos.w;
		    lightCoords = lightCoords * .5 + .5;

		    if (lightCoords.z > 1.0) shadow = 0.0;
            else 
            {
                shadow = 0.0;
                float lightDepth = texture(uLights[i].uShadowTexW, lightCoords.xy).r;
		        float bias = max(uShadowNormalBias.y * (1.0 - dot(normal, lightDir)), uShadowNormalBias.x);

		        vec2 pixelSize = 1.0 / textureSize(uLights[i].uShadowTexW, 0) * uShadowSoftness;
		        for(int y = -uShadowMapSamples; y <= uShadowMapSamples; y++)
		        {
		            for(int x = -uShadowMapSamples; x <= uShadowMapSamples; x++)
		            {
		                float closestDepth = texture(uLights[i].uShadowTexW, lightCoords.xy + vec2(x, y) * pixelSize).r;
		        		if (lightCoords.z > closestDepth + bias) shadow += 1.0f;     
		            }    
		        }
		        shadow /= pow((uShadowMapSamples * 2 + 1), 2);
            }

            if (shadow >= 1.0) continue;
        }

		float diffuseStrength = max(dot(lightDir, normal), 0.0);
		vec3 diffuseColor = diffuseStrength * diffuse.xyz * uLights[i].uColor;

		vec3 reflectDir = reflect(-lightDir, normal);
		vec3 halfwayDir = normalize(viewDir + lightDir);

		float specularStrength = pow(max(dot(normal, halfwayDir), 0.0), shininess);
		vec3 specularColor = specularStrength * specular * uLights[i].uColor;

		float lightDistance = distance(uLights[0].uPosition, outWorldPosition);
		float attenuation = 1.0 / (1 + uLights[i].uFalloff.x * lightDistance + uLights[i].uFalloff.y * (lightDistance * lightDistance));
		finalColor += (diffuseColor + specularColor) * attenuation * (1.0 - shadow) / 2.5;
	}

	outColor = vec4(finalColor, diffuse.a);
}