#version 460

in vec2 outUV;

uniform vec2 uResolution;
uniform sampler2D uTexture;

out vec4 outColor;

void main()
{
	vec2 srcTexelSize = 1.0 / uResolution;
	float x = srcTexelSize.x;
	float y = srcTexelSize.y;

	vec3 a = texture(uTexture, vec2(outUV.x - 2*x, outUV.y + 2*y)).rgb;
	vec3 b = texture(uTexture, vec2(outUV.x,       outUV.y + 2*y)).rgb;
	vec3 c = texture(uTexture, vec2(outUV.x + 2*x, outUV.y + 2*y)).rgb;

	vec3 d = texture(uTexture, vec2(outUV.x - 2*x, outUV.y)).rgb;
	vec3 e = texture(uTexture, vec2(outUV.x,       outUV.y)).rgb;
	vec3 f = texture(uTexture, vec2(outUV.x + 2*x, outUV.y)).rgb;

	vec3 g = texture(uTexture, vec2(outUV.x - 2*x, outUV.y - 2*y)).rgb;
	vec3 h = texture(uTexture, vec2(outUV.x,       outUV.y - 2*y)).rgb;
	vec3 i = texture(uTexture, vec2(outUV.x + 2*x, outUV.y - 2*y)).rgb;

	vec3 j = texture(uTexture, vec2(outUV.x - x, outUV.y + y)).rgb;
	vec3 k = texture(uTexture, vec2(outUV.x + x, outUV.y + y)).rgb;
	vec3 l = texture(uTexture, vec2(outUV.x - x, outUV.y - y)).rgb;
	vec3 m = texture(uTexture, vec2(outUV.x + x, outUV.y - y)).rgb;
	
	vec3 downsample = e*0.125;
	downsample += (a+c+g+i)*0.03125;
	downsample += (b+d+f+h)*0.0625;
	downsample += (j+k+l+m)*0.125;

	outColor = vec4(downsample, 1);
}