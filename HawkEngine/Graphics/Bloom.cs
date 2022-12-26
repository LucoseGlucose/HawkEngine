using HawkEngine.Core;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace HawkEngine.Graphics
{
    public class Bloom
    {
        private readonly uint fbID;
        private Texture2D[] textures;

        private readonly ShaderProgram downsampleShader;
        private readonly ShaderProgram upsampleShader;
        private readonly ShaderProgram mixShader;

        private readonly int mipCount;
        public float strength;
        public float filterRadius;

        public Bloom(int mipCount = 5, float strength = .04f, float filterRadius = .005f)
        {
            this.mipCount = mipCount;
            this.strength = strength;
            this.filterRadius = filterRadius;

            fbID = Rendering.gl.GenFramebuffer();
            textures = new Texture2D[mipCount];

            downsampleShader = new("Shaders/Post Processing/OutputVert.glsl", "Shaders/Post Processing/BloomDownsample.glsl");
            upsampleShader = new("Shaders/Post Processing/OutputVert.glsl", "Shaders/Post Processing/BloomUpsample.glsl");
            mixShader = new("Shaders/Post Processing/OutputVert.glsl", "Shaders/Post Processing/MixFrag.glsl");

            App.window.FramebufferResize += InitTextures;
            InitTextures(App.window.FramebufferSize);
        }
        private void InitTextures(Vector2D<int> size)
        {
            Vector2D<uint> mipSize = size.As<uint>();

            for (int i = 0; i < mipCount; i++)
            {
                mipSize /= 2u;
                textures[i] = new(mipSize.X, mipSize.Y, InternalFormat.Rgba16f, wrap: GLEnum.ClampToEdge);
            }
        }
        public void Render()
        {
            Model downModel = new(downsampleShader, Rendering.quad);
            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbID);

            downsampleShader.SetVec2Cache("uResolution", App.window.FramebufferSize.As<float>());
            downsampleShader.SetTexture("uTexture", Rendering.postProcessFB[FramebufferAttachment.ColorAttachment0]);

            for (int i = 0; i < mipCount; i++)
            {
                Texture2D tex = textures[i];
                Rendering.gl.Viewport(tex.size.As<int>());

                Rendering.gl.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, tex.id, 0);
                downModel.Render();

                downsampleShader.SetVec2Cache("uResolution", tex.size.As<float>());
                downsampleShader.SetTexture("uTexture", tex);
            }

            Model upModel = new(upsampleShader, Rendering.quad);
            upsampleShader.SetFloatCache("uFilterRadius", filterRadius);

            for (int i = (mipCount - 1); i > 0; i--)
            {
                Texture2D currentTex = textures[i];
                Texture2D nextTex = textures[i - 1];

                upsampleShader.SetTexture("uTexture", currentTex);

                Rendering.gl.Viewport(nextTex.size.As<int>());
                Rendering.gl.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, nextTex.id, 0);

                upModel.Render();
            }

            Rendering.postProcessFB.Bind();
            Model outModel = new(mixShader, Rendering.quad);
            Rendering.gl.Viewport(Rendering.outputCam.size);

            mixShader.SetFloatCache("uT", strength);
            mixShader.SetTexture("uTexture0", Rendering.postProcessFB[FramebufferAttachment.ColorAttachment0]);
            mixShader.SetTexture("uTexture1", textures[0]);

            outModel.Render();
        }
    }
}
