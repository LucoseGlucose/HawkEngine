using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace HawkEngine.Graphics
{
    public sealed class Framebuffer : IDisposable
    {
        public readonly uint glID;
        public readonly FramebufferTexture[] attachments;

        public Framebuffer(params FramebufferTexture[] textures)
        {
            attachments = textures;
            glID = Rendering.gl.GenFramebuffer();
            Bind();

            for (int i = 0; i < textures.Length; i++)
            {
                Rendering.gl.FramebufferTexture(FramebufferTarget.Framebuffer, textures[i].attachment, textures[i].texture.glID, 0);
            }

            List<DrawBufferMode> drawBuffers = new();
            for (int i = 0; i < textures.Length; i++)
            {
                if (textures[i].attachment.ToString().Contains("ColorAttachment"))
                {
                    if (Enum.TryParse(textures[i].attachment.ToString(), out DrawBufferMode drawBuffer)) drawBuffers.Add(drawBuffer);
                }
            }

            if (drawBuffers.Count == 0) Rendering.gl.DrawBuffers(stackalloc DrawBufferMode[1] { DrawBufferMode.None });
            else Rendering.gl.DrawBuffers(drawBuffers.ToArray());

            Unbind();
        }
        ~Framebuffer()
        {
            Rendering.deletedObjects.Enqueue(() => Rendering.gl.DeleteFramebuffer(glID));
        }
        public void Bind()
        {
            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, glID);
        }
        public static void Unbind()
        {
            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
        public Texture this[FramebufferAttachment attachment]
        {
            get { return attachments.FirstOrDefault(t => t.attachment == attachment).texture; }
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Rendering.gl.DeleteFramebuffer(glID);
        }
    }

    public readonly struct FramebufferTexture
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
