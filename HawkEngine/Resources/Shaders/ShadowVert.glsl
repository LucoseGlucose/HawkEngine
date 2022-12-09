#version 460

layout (location = 0) in vec3 inPosition;

uniform mat4 uModelMat;
uniform mat4 uViewMat;
uniform mat4 uProjMat;

void main()
{
	gl_Position =  uProjMat * uViewMat * uModelMat * vec4(inPosition, 1);
}