using System;
using System.Collections.Generic;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace HawkEngine.Graphics
{
    public class Framebuffer
    {
        public readonly uint id;
        public readonly FramebufferTexture[] attachments;

        public Framebuffer(params FramebufferTexture[] textures)
        {
            attachments = textures;
            id = Rendering.gl.GenFramebuffer();
            Bind();

            for (int i = 0; i < textures.Length; i++)
            {
                Rendering.gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, textures[i].attachment, textures[i].textureType, textures[i].id, 0);
            }

            Unbind();
        }
        ~Framebuffer()
        {
            Rendering.gl.DeleteFramebuffer(id);
        }
        public void Bind()
        {
            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, id);
        }
        public void Unbind()
        {
            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
        public void Delete()
        {
            GC.SuppressFinalize(this);
            Rendering.gl.DeleteFramebuffer(id);
        }
    }
}
