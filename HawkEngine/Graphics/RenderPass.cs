using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HawkEngine.Components;
using HawkEngine.Core;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using static System.Net.Mime.MediaTypeNames;
using static HawkEngine.Graphics.Rendering;

namespace HawkEngine.Graphics
{
    public static class RenderPass
    {
        public static readonly Pass shadowPass = (meshes, lights) =>
        {
            for (int l = 0; l < lights.Count; l++)
            {
                if (lights[l].shadowsEnabled) lights[l].RenderShadowMap(meshes);
            }
        };

        public static readonly Pass preMainDrawPass = (_, _) =>
        {
            outputCam.framebuffer.Bind();
            gl.Viewport(outputCam.size);
            gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        };

        public static readonly Pass skyboxPass = (_, _) =>
        {
            skyboxShader.SetTexture("uSkyboxW", skybox.skybox);
            skyboxModel.shader.SetMat4Cache("uViewMat", outputCam.viewMat);
            skyboxModel.shader.SetMat4Cache("uProjMat", outputCam.projectionMat);
            skyboxModel.Render();
        };

        public static readonly Pass scenePass = (meshes, lights) =>
        {
            for (int m = 0; m < meshes.Count; m++)
            {
                if (meshes[m].lightingEnabled)
                {
                    meshes[m].shader.SetTexture("uIrradianceCubeB", skybox.irradiance);
                    meshes[m].shader.SetTexture("uReflectionCubeB", skybox.specularReflections);
                    meshes[m].shader.SetTexture("uBrdfLutB", Texture2D.brdfTex);
                    meshes[m].shader.SetVec3Cache("uAmbientColor", ambientColor);

                    IOrderedEnumerable<LightComponent> orderedLights =
                        lights.OrderBy(l => Math.Min(l.type, 1) * Vector3D.DistanceSquared(meshes[m].transform.position, l.transform.position));

                    for (int l = 0; l < 5; l++)
                    {
                        if (orderedLights.Count() > l) orderedLights.ElementAt(l).SetUniforms($"uLights[{l}]", meshes[m].shader);
                        else meshes[m].shader.SetIntCache($"uLights[{l}].uType", 0);
                    }
                }

                meshes[m].SetUniforms();
                meshes[m].Render();
                meshes[m].Cleanup();
            }
        };

        public static readonly Pass prePostProcessPass = (_, _) =>
        {
            gl.BlitNamedFramebuffer(outputCam.framebuffer.id, postProcessFB.id, 0, 0, outputCam.size.X, outputCam.size.Y, 0, 0, App.window.FramebufferSize.X,
                App.window.FramebufferSize.Y, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            postProcessFB.Bind();
        };

        public static readonly Pass outputPass = (_, _) =>
        {
            postProcessFB.Unbind();
            gl.Clear(ClearBufferMask.ColorBufferBit);

            Model outputModel = new(outputShader, quad);
            outputModel.shader.SetTexture("uColorTex", postProcessFB[FramebufferAttachment.ColorAttachment0].texture);
            outputModel.Render();
        };

        public static readonly Pass bloomPass = (_, _) =>
        {
            bloom.Render();
        };

        public static Pass BlitScreenPass(ShaderProgram shader)
        {
            return (_, _) =>
            {
                Model ppModel = new(shader, quad);
                ppModel.shader.SetTexture("uColorTex", postProcessFB[FramebufferAttachment.ColorAttachment0].texture);
                ppModel.Render();
            };
        }
    }
}
