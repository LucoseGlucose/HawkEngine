#version 460

in vec2 outUV;

out vec4 outColor;

uniform vec4 uColor = vec4(1);
layout(binding=0) uniform sampler2D uColorTexW;
uniform float uAlphaClip;

void main()
{
	vec4 col = uColor * texture(uColorTexW, outUV);
	if (col.a < uAlphaClip) discard;
	outColor = col;
}