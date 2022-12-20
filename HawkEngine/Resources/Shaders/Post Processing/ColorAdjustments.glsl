#version 460

in vec2 outUV;

out vec4 outColor;

uniform sampler2D uColorTex;

uniform float uBrightness;
uniform float uContrast;
uniform float uExposure;
uniform float uSaturation;
uniform mat4 uColorMatrix = mat4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

void main()
{
	vec4 texCol = texture(uColorTex, outUV);
	vec3 col = texCol.rgb;

	col += vec3(uBrightness);
	col = 0.5 + (1.0 + uContrast) * (col - 0.5);
	col *= 1.0 + uExposure;

	const vec3 luminosityFactor = vec3(0.2126, 0.7152, 0.0722);
	vec3 grayscale = vec3(dot(col, luminosityFactor));
	col = mix(grayscale, col, 1.0 + uSaturation);

	col = (uColorMatrix * vec4(col, 1)).rgb;

	outColor = vec4(col, texCol.a);
}