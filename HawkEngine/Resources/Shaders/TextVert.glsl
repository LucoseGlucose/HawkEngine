#version 460

layout (location = 0) in vec3 inPosition;
layout (location = 2) in vec2 inUV;

out vec2 outUV;

uniform mat4 uModelMat;
uniform mat4 uProjMat;

void main()
{
	outUV = inUV;

	gl_Position =  uProjMat * uModelMat * vec4(inPosition.xy, 0, 1);
}