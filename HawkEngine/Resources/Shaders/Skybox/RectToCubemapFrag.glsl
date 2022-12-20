#version 460

in vec3 outLocalPos;

uniform sampler2D uTexture;

const vec2 invAtan = vec2(0.1591, 0.3183);

vec2 SampleSphericalMap(vec3 v)
{
    vec2 uv = vec2(atan(v.z, v.x), asin(v.y));
    uv *= invAtan;
    uv += 0.5;
    return uv;
}

out vec4 outColor;

void main()
{		
    vec2 uv = SampleSphericalMap(normalize(outLocalPos));
    vec3 color = texture(uTexture, uv).rgb;
    
    outColor = vec4(color, 1.0);
}