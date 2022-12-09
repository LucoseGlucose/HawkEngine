#version 460

in vec2 outUV;

in vec3 outWorldPosition;
in mat3 outTBNMat;

out vec4 outColor;

uniform vec3 uCameraPos;
uniform vec3 uAmbientColor = vec3(.2);

uniform vec4 uDiffuse = vec4(1);
uniform sampler2D uDiffuseTexW;
uniform float uAlphaClip = 0;

uniform vec3 uSpecular = vec3(.25);
uniform sampler2D uSpecularTexW;
uniform float uShininess = 32;

uniform sampler2D uNormalTexN;
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

void main()
{
	vec4 diffuse = uDiffuse * texture(uDiffuseTexW, outUV);
	if (diffuse.a < uAlphaClip) discard;

	vec3 finalColor = uAmbientColor * diffuse.xyz;

	vec3 specular = uSpecular * texture(uSpecularTexW, outUV).rgb;

	vec3 normal = texture(uNormalTexN, outUV).xyz;
	normal.xy *= uNormalStrength;
	normal = normalize(outTBNMat * normal);

	vec3 viewDir = normalize(uCameraPos - outWorldPosition);

	for (int i = 0; i < 5; i++)
	{
		if (uLights[i].uColor == vec3(0)) break;

		vec3 lightDir = normalize(uLights[i].uPosition - outWorldPosition * min(uLights[i].uType, 1.0));
		vec4 lightSpacePos = uLights[i].uProjMat * uLights[i].uViewMat * vec4(outWorldPosition, 1);

		vec3 lightCoords = lightSpacePos.xyz / lightSpacePos.w;
		lightCoords = lightCoords * .5 + .5;

		float lightDepth = texture(uLights[i].uShadowTexW, lightCoords.xy).r;
		float bias = max(uShadowNormalBias.y * (1.0 - dot(normal, lightDir)), uShadowNormalBias.x);

		float shadow = lightCoords.z - bias > lightDepth ? 0.0 : 1.0;
		if (lightCoords.z > 1.0) shadow = 1.0;

		float diffuseStrength = max(dot(lightDir, normal), 0.0);
		vec3 diffuseColor = diffuseStrength * diffuse.xyz * uLights[i].uColor;

		vec3 reflectDir = reflect(-lightDir, normal);
		vec3 halfwayDir = normalize(viewDir + lightDir);

		float specularStrength = pow(max(dot(normal, halfwayDir), 0.0), uShininess);
		vec3 specularColor = specularStrength * specular * uLights[i].uColor;

		float lightDistance = distance(uLights[0].uPosition, outWorldPosition);
		float attenuation = 1.0 / (1 + uLights[i].uFalloff.x * lightDistance + uLights[i].uFalloff.y * (lightDistance * lightDistance));
		finalColor += (diffuseColor + specularColor) * shadow * attenuation;
	}

	outColor = vec4(finalColor, diffuse.a);
}