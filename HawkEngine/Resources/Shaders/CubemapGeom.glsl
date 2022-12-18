#version 460

layout (triangles) in;
layout (triangle_strip, max_vertices=18) out;

struct ViewMat
{
    mat4 uMat;
};

uniform ViewMat[6] uMats;

out vec4 outFragPos;

void main()
{
    for(int face = 0; face < 6; ++face)
    {
        gl_Layer = face;
        for(int i = 0; i < 3; ++i)
        {
            outFragPos = gl_in[i].gl_Position;
            gl_Position = uMats[face].uMat * outFragPos;
            EmitVertex();
        }    
        EndPrimitive();
    }
}  