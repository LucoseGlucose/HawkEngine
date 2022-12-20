#version 460

layout (location = 0) in vec3 inPosition;

uniform mat4 uMat;

void main()
{
	gl_Position = uMat * vec4(inPosition, 1);
}