using HawkEngine.Core;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace HawkEngine.Graphics
{
    public class Model
    {
        public ShaderProgram shader;
        public Mesh mesh;

        public Model(ShaderProgram shader, Mesh mesh)
        {
            this.shader = shader;
            this.mesh = mesh;
        }
        public unsafe virtual void Render()
        {
            shader.Bind();
            shader.BindTextures();
            mesh.vertexArray.Bind();
            Rendering.gl.DrawElements(PrimitiveType.Triangles, (uint)mesh.meshData.indices.Length, DrawElementsType.UnsignedInt, null);
            shader.UnbindTextures();
        }
    }
}
