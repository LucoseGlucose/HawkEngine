#version 460

in vec3 outPosition;

out vec4 outColor;

uniform samplerCube uSkyboxW;

void main()
{
	outColor = vec4(texture(uSkyboxW, outPosition).xyz, 1);
}