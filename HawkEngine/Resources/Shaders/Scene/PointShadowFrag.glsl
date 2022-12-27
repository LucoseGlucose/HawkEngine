#version 460

in vec4 outFragPos;

uniform vec3 uLightPos;
uniform float uFarPlane;

void main()
{
    float lightDistance = length(outFragPos.xyz - uLightPos);
    lightDistance /= uFarPlane;
    gl_FragDepth = lightDistance;
}