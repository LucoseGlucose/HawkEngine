#version 460

layout(location=0) out float outDepth;

void main()
{
	outDepth = gl_FragCoord.z * gl_FragCoord.w;
}