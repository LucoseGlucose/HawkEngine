#version 460

in vec2 outUV;

uniform sampler2D uStencilTexB;
uniform sampler2D uDepthTexB;
uniform sampler2D uSceneTexB;

uniform vec4 uOutlineColor = vec4(1, .12, .02, 1);
uniform float uOutlineWidth = 3;

out vec4 outColor;

vec2 texelSize;

struct PixelInfo
{
	float index;
	float depth;
	vec2 coord;
};

PixelInfo sampleTextures(vec2 offset)
{
	vec2 coords = outUV + offset * uOutlineWidth * texelSize;
	vec2 texData = texture(uStencilTexB, coords).rg;
	return PixelInfo(texData.r, texData.g, coords);
}

void main()
{
	texelSize = 1.0 / textureSize(uSceneTexB, 0);

	PixelInfo center = sampleTextures(vec2(0));

	PixelInfo up = sampleTextures(vec2(0, 1));
	PixelInfo down = sampleTextures(vec2(0, -1));
	PixelInfo right = sampleTextures(vec2(1, 0));
	PixelInfo left = sampleTextures(vec2(-1, 0));
	PixelInfo upRight = sampleTextures(vec2(1, 1));
	PixelInfo downRight = sampleTextures(vec2(1, -1));
	PixelInfo upLeft = sampleTextures(vec2(-1, 1));
	PixelInfo downLeft = sampleTextures(vec2(-1, -1));

	PixelInfo pixels[8] = PixelInfo[](up, down, right, left, upRight, downRight, upLeft, downLeft);

	PixelInfo edgePixel = center;
	for (int i = 0; i < 8; i++)
	{
		PixelInfo pixel = pixels[i];
		if (pixel.index != center.index)
		{
			edgePixel = pixels[i];
			break;
		}
	}

	if (edgePixel == center || center.depth > texture(uDepthTexB, edgePixel.coord).r)
	{
		outColor = texture(uSceneTexB, outUV);
	}
	else
	{
		outColor = uOutlineColor;
	}
}