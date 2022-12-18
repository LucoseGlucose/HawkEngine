using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace HawkEngine.Graphics
{
    public class Framebuffer : IDisposable
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
                Rendering.gl.FramebufferTexture(FramebufferTarget.Framebuffer, textures[i].attachment, textures[i].texture.id, 0);
            }

            Unbind();
        }
        ~Framebuffer()
        {
            Rendering.deletedObjects.Enqueue(() => Rendering.gl.DeleteFramebuffer(id));
        }
        public void Bind()
        {
            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, id);
        }
        public void Unbind()
        {
            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
        public FramebufferTexture this[FramebufferAttachment attachment]
        {
            get { return attachments.FirstOrDefault(t => t.attachment == attachment); }
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Rendering.gl.DeleteFramebuffer(id);
        }
    }

    public class FramebufferTexture
    {
        public readonly FramebufferAttachment attachment;
        public readonly Texture texture;

        public FramebufferTexture(Texture texture, FramebufferAttachment attachment)
        {
            this.texture = texture;
            this.attachment = attachment;
        }
    }
}
