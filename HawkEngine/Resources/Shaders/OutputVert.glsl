#version 460

layout (location = 0) in vec3 inPosition;
layout (location = 2) in vec2 inUV;

out vec2 outUV;

void main()
{
	outUV = vec2(inUV.x, inUV.y);
	gl_Position =  vec4(-inPosition.x, inPosition.y, 0, 1);
}