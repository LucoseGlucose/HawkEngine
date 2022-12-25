#version 460

in vec2 outUV;

out vec4 outColor;

uniform sampler2D uTexture0;
uniform sampler2D uTexture1;
uniform float uT;

void main()
{
	outColor = mix(texture(uTexture0, outUV), texture(uTexture1, outUV), uT);
}