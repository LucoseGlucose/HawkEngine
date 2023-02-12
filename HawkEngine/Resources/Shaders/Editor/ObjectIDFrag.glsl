#version 460

uniform vec4 uIDColor;

layout(location=0) out vec2 outIDColor0;
layout(location=1) out vec2 outIDColor1;

layout(location=2) out float outDepth;

void main()
{
	outIDColor0 = uIDColor.xy;
	outIDColor1 = uIDColor.zw;

	outDepth = 1 - gl_FragCoord.z * gl_FragCoord.w;
}