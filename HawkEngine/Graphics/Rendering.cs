using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using ImGuiNET;
using Silk.NET.Windowing;
using HawkEngine.Core;
using HawkEngine.Components;
using System.Drawing;
using Silk.NET.Maths;
using static HawkEngine.Graphics.Rendering;

namespace HawkEngine.Graphics
{
    public static class Rendering
    {
        public static GL gl { get; set; }

        public static event Action<DebugSource, DebugType, int, string> glDebugCallback;
        public static readonly Queue<Action> deletedObjects = new();

        public static CameraComponent outputCam { get; set; }

        public static Mesh quad { get; private set; }
        public static ShaderProgram outputShader { get; private set; }
        public static Model outputModel { get; private set; }

        public static Framebuffer postProcessFB { get; private set; }

        public delegate void Pass(List<MeshComponent> meshes, List<LightComponent> lights);
        public static readonly List<Pass> renderPasses = new();

        public static Mesh skyboxMesh { get; private set; }
        public static ShaderProgram skyboxShader { get; set; }
        public static Model skyboxModel { get; set; }

        public static Skybox skybox { get; set; }
        public static Vector3D<float> ambientColor { get; set; } = new(1f);

        public static ShaderProgram colorAdjustmentsShader { get; private set; }
        public static Bloom bloom { get; private set; }

        public static void Init()
        {
            gl = App.window.CreateOpenGL();

            App.window.FramebufferResize += OnWindowResize;

#if DEBUG
            gl.Enable(EnableCap.DebugOutputSynchronous);
            gl.DebugMessageCallback(GLDebugMessage, nint.Zero);
#endif

            gl.Enable(EnableCap.CullFace);
            gl.Enable(EnableCap.TextureCubeMapSeamless);
            gl.Enable(EnableCap.DepthTest);
            gl.Enable(EnableCap.Blend);

            gl.DepthFunc(DepthFunction.Lequal);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            CreateStandardResources();
        }
        private static void OnWindowResize(Vector2D<int> size)
        {
            postProcessFB = new(new FramebufferTexture(new Texture2D((uint)size.X, (uint)size.Y,
                InternalFormat.Rgba16f, PixelFormat.Rgba), FramebufferAttachment.ColorAttachment0));
        }
        public static void CreateStandardResources()
        {
            postProcessFB = new(new FramebufferTexture(new Texture2D((uint)App.window.FramebufferSize.X, (uint)App.window.FramebufferSize.Y,
                InternalFormat.Rgba16f, PixelFormat.Rgba), FramebufferAttachment.ColorAttachment0));

            quad = new("Models/Quad.obj");
            outputShader = new("Shaders/Post Processing/OutputVert.glsl", "Shaders/Post Processing/OutputFrag.glsl");
            outputModel = new(outputShader, quad);

            skyboxMesh = new("Models/Inverted Cube.obj");
            skyboxShader = new("Shaders/Skybox/SkyboxVert.glsl", "Shaders/Skybox/SkyboxFrag.glsl");
            skyboxModel = new(skyboxShader, skyboxMesh);

            skybox = new("Images/limpopo_golf_course_4k.hdr", 2048u, 64u, 512u);
            colorAdjustmentsShader = new("Shaders/Post Processing/OutputVert.glsl", "Shaders/Post Processing/ColorAdjustments.glsl");
            bloom = new(5);

            renderPasses.Add(RenderPass.shadowPass);
            renderPasses.Add(RenderPass.preMainDrawPass);
            renderPasses.Add(RenderPass.skyboxPass);
            renderPasses.Add(RenderPass.scenePass);
            renderPasses.Add(RenderPass.prePostProcessPass);
            renderPasses.Add(RenderPass.bloomPass);
            renderPasses.Add(RenderPass.BlitScreenPass(colorAdjustmentsShader));
#if DEBUG
            renderPasses.Add(RenderPass.editorOutputPass);
#else
            renderPasses.Add(RenderPass.outputPass);
#endif
        }
        public static unsafe void Render()
        {
            List<MeshComponent> meshes = App.scene.FindComponents<MeshComponent>();
            List<LightComponent> lights = App.scene.FindComponents<LightComponent>();

            foreach (Pass pass in renderPasses)
            {
                pass?.Invoke(meshes, lights);
            }

            CleanupDeletedObjects();
        }
        public static void CleanupDeletedObjects()
        {
            int deleteCount = deletedObjects.Count;
            for (int i = 0; i < deleteCount; i++)
            {
                deletedObjects.Dequeue()?.Invoke();
            }
        }
        private static unsafe void GLDebugMessage(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userParam)
        {
            DebugSource dSource = (DebugSource)source;
            DebugType dType = (DebugType)type;

            string dMessage = Encoding.UTF8.GetString((byte*)message.ToPointer(), length);

            Console.WriteLine($"{dType.ToString().TrimStart("DebugType".ToCharArray()).ToUpper()} {
                id} in {dSource.ToString().TrimStart("DebugSource".ToCharArray())}: {dMessage}");
            glDebugCallback?.Invoke(dSource, dType, id, dMessage);
        }
    }

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
            outputModel.shader.SetTexture("uColorTex", postProcessFB[FramebufferAttachment.ColorAttachment0]);
            outputModel.Render();
        };

        public static readonly Pass editorOutputPass = (_, _) =>
        {
            Model outputModel = new(outputShader, quad);
            outputModel.shader.SetTexture("uColorTex", postProcessFB[FramebufferAttachment.ColorAttachment0]);
            outputModel.Render();

            postProcessFB.Unbind();
            gl.Clear(ClearBufferMask.ColorBufferBit);
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
                ppModel.shader.SetTexture("uColorTex", postProcessFB[FramebufferAttachment.ColorAttachment0]);
                ppModel.Render();
            };
        }
    }
}
