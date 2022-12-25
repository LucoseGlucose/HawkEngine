#version 460

in vec2 outUV;

uniform sampler2D uTexture;
uniform float uFilterRadius;

out vec4 outColor;

void main()
{
	float x = uFilterRadius;
	float y = uFilterRadius;

	vec3 a = texture(uTexture, vec2(outUV.x - x, outUV.y + y)).rgb;
	vec3 b = texture(uTexture, vec2(outUV.x,     outUV.y + y)).rgb;
	vec3 c = texture(uTexture, vec2(outUV.x + x, outUV.y + y)).rgb;

	vec3 d = texture(uTexture, vec2(outUV.x - x, outUV.y)).rgb;
	vec3 e = texture(uTexture, vec2(outUV.x,     outUV.y)).rgb;
	vec3 f = texture(uTexture, vec2(outUV.x + x, outUV.y)).rgb;

	vec3 g = texture(uTexture, vec2(outUV.x - x, outUV.y - y)).rgb;
	vec3 h = texture(uTexture, vec2(outUV.x,     outUV.y - y)).rgb;
	vec3 i = texture(uTexture, vec2(outUV.x + x, outUV.y - y)).rgb;

	vec3 upsample = e*4.0;
	upsample += (b+d+f+h)*2.0;
	upsample += (a+c+g+i);
	upsample *= 1.0 / 16.0;

	outColor = vec4(upsample, 1);
}