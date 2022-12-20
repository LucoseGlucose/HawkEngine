#version 460

layout (location = 0) in vec3 inPosition;

uniform mat4 uMat;

out vec3 outLocalPos;

void main()
{
    outLocalPos = inPosition;
    gl_Position =  uMat * vec4(inPosition, 1.0);
}