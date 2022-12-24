#version 460

layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec3 inNormal;
layout (location = 2) in vec2 inUV;
layout (location = 3) in vec3 inTangent;
layout (location = 4) in vec3 inBitangent;

out VSOut
{
	vec2 uv;
	vec3 worldPosition;
	mat3 tbnMat;
	vec3 geometryNormal;
}
vsOut;

uniform mat4 uModelMat;
uniform mat4 uMat;

void main()
{
	vsOut.uv = inUV;
	
	vsOut.worldPosition = (uModelMat * vec4(inPosition, 1)).xyz;

	vec3 T = normalize(vec3(uModelMat * vec4(inTangent, 0.0)));
	vec3 B = normalize(vec3(uModelMat * vec4(inBitangent, 0.0)));
	vec3 N = normalize(vec3(uModelMat * vec4(inNormal, 0.0)));
	vsOut.tbnMat = mat3(T, B, N);

	vsOut.geometryNormal = normalize(vsOut.tbnMat * vec3(0.0, 0.0, 1.0));

	gl_Position =  uMat * vec4(inPosition, 1.0);
}