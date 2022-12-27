#version 460

in vec2 outUV;

uniform sampler2D uStencilTexB;
uniform sampler2D uDepthTexB;
uniform sampler2D uSceneTexB;

uniform vec4 uOutlineColor = vec4(1, .12, .02, 1);
uniform vec2 uOutlineWidth = vec2(2, .00000001);
uniform vec2 uThreshold = vec2(.000001, .1);

out vec4 outColor;

void getNeighbors(in float width, in vec2 texelSize, out vec2 right, out vec2 left, out vec2 up, out vec2 down)
{
	right = outUV + vec2(width, 0) * texelSize;
	left = outUV + vec2(-width, 0) * texelSize;
	up = outUV + vec2(0, width) * texelSize;
	down = outUV + vec2(0, -width) * texelSize;
}

float getDepth(in vec2 uv)
{
	return texture(uStencilTexB, uv).r;
}

void main()
{
	float centerDepth = getDepth(outUV);
	float sceneDepth = texture(uDepthTexB, outUV).r;

	if (sceneDepth != centerDepth) 
	{
		outColor = texture(uSceneTexB, outUV);
		return;
	}

	vec2 texelSize = 1.0 / textureSize(uSceneTexB, 0);

	vec2 right;
	vec2 left;
	vec2 up;
	vec2 down;

	getNeighbors(uOutlineWidth.x, texelSize, right, left, up, down);
	float widthDepth = min(min(min(getDepth(right), getDepth(left)), min(getDepth(up), getDepth(down))), centerDepth);
	
	float width = mix(uOutlineWidth.x, uOutlineWidth.y, widthDepth);
	getNeighbors(width, texelSize, right, left, up, down);

	float variation = abs(getDepth(right) + getDepth(left) + getDepth(up) + getDepth(down) - 4.0 * centerDepth);
	outColor = mix(texture(uSceneTexB, outUV), uOutlineColor, step(mix(uThreshold.x, uThreshold.y, centerDepth), variation));
}