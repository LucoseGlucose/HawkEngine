#version 460

in vec2 outUV;

out vec4 outColor;

uniform vec4 uDiffuse = vec4(1);
layout(binding=0) uniform sampler2D uDiffuseTexW;
uniform float uAlphaClip;

void main()
{
	vec4 diffuse = uDiffuse * texture(uDiffuseTexW, outUV);

	if (diffuse.a < uAlphaClip) discard;

	outColor = diffuse;
}