#version 460

layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec3 inNormal;
layout (location = 2) in vec2 inUV;
layout (location = 3) in vec3 inTangent;
layout (location = 4) in vec3 inBitangent;

out vec2 outUV;

out vec3 outWorldPosition;
out mat3 outTBNMat;

uniform mat4 uModelMat;
uniform mat4 uMat;

void main()
{
	outUV = inUV;
	
	outWorldPosition = (uModelMat * vec4(inPosition, 1)).xyz;

	vec3 T = normalize(vec3(uModelMat * vec4(inTangent, 0.0)));
	vec3 B = normalize(vec3(uModelMat * vec4(inBitangent, 0.0)));
	vec3 N = normalize(vec3(uModelMat * vec4(inNormal, 0.0)));
	outTBNMat = mat3(T, B, N);

	gl_Position =  uMat * vec4(inPosition, 1.0);
}