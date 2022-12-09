#version 460

in vec2 outUV;

out vec4 outColor;

layout(binding=0) uniform sampler2D uColorTex;

uniform float uGamma = 2.2;
uniform float uExposure = 1;
uniform float uTonemapStrength = 1;

void main()
{
	vec4 texCol = texture(uColorTex, outUV);
    vec3 tonemapping = texCol.rgb + uTonemapStrength * ((vec3(1) - exp(-texCol.rgb * uExposure)) - texCol.rgb);
    vec3 gamma = pow(tonemapping, vec3(1.0 / uGamma));

	outColor = vec4(gamma, texCol.a);
}