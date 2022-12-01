#version 460

in vec3 outPosition;

out vec4 outColor;

uniform samplerCube uSkybox;

void main()
{
	outColor = vec4(textureCube(uSkybox, -outPosition).xyz, 1);
}