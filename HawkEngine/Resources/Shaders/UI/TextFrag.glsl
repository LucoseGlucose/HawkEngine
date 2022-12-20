#version 460

in vec2 outUV;

out vec4 outColor;

uniform vec4 uColor = vec4(1);
layout(binding=0) uniform sampler2D uFontAtlasB;
uniform float uAlphaClip = .5;

void main()
{
	float col = texture(uFontAtlasB, outUV).r;
	if (col < uAlphaClip) discard;
	outColor = uColor;
}