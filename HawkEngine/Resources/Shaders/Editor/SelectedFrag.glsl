#version 460

uniform float uIndex;

layout(location=0) out vec2 outResult;

void main()
{
	outResult = vec2(uIndex, 1 - gl_FragCoord.z * gl_FragCoord.w);
}