using System;
using System.Collections.Generic;
using HawkEngine.Core;
using HawkEngine.Graphics;
using Silk.NET.OpenGL;
using Silk.NET.Maths;

namespace HawkEngine.Components
{
    public class MeshComponent : Component
    {
        public ShaderProgram shader;
        public Mesh mesh;

        public bool lightingEnabled = true;
        public bool castShadows = true;
        public bool recieveShadows = true;
        public bool transparent = false;

        public virtual void SetUniforms()
        {
            shader.SetVec3Cache("uCameraPos", Rendering.outputCam.transform.position);
            shader.SetMat4Cache("uModelMat", transform.matrix);
            shader.SetMat4Cache("uViewMat", Rendering.outputCam.viewMat);
            shader.SetMat4Cache("uProjMat", Rendering.outputCam.projectionMat);
        }
        public unsafe virtual void Render()
        {
            if (shader == null || mesh == null) return;

            shader.Bind();
            shader.BindTextures();
            mesh.vertexArray.Bind();
            Rendering.gl.DrawElements(PrimitiveType.Triangles, (uint)mesh.meshData.indices.Length, DrawElementsType.UnsignedInt, null);
        }
        public virtual void Cleanup()
        {
            shader.UnbindTextures();
        }
    }
}
