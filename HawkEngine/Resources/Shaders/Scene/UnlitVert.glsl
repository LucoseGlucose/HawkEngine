#version 460

layout (location = 0) in vec3 inPosition;
layout (location = 2) in vec2 inUV;

out vec2 outUV;

uniform mat4 uMat;

void main()
{
	outUV = inUV;

	gl_Position =  uMat * vec4(inPosition, 1);
}