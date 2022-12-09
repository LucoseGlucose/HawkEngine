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
using HawkEngine.Editor;
using HawkEngine.Components;
using System.Drawing;
using Silk.NET.Maths;
using Silk.NET.Assimp;

namespace HawkEngine.Graphics
{
    public static class Rendering
    {
        public static GL gl { get; set; }

        public static event Action<DebugSource, DebugType, int, string> glDebugCallback;
        public static Queue<Action> deletedObjects = new();

        public static CameraComponent outputCam { get; set; }

        public static Mesh quad { get; private set; }
        public static ShaderProgram outputShader { get; private set; }
        public static readonly List<ShaderProgram> postProcessShaders = new();
        public static Framebuffer postProcessFB { get; private set; }

        public static Model skyboxModel { get; set; }
        public static TextureCubemap skyboxTexture { get; set; }

        public static ShaderProgram shadowShader { get; private set; }

        public static float gamma { get; set; } = 2.2f;
        public static float exposure { get; set; } = 1f;
        public static float tonemapStrength { get; set; } = 1f;
        public static Vector2D<float> shadowNormalBias { get; set; } = new(.005f, .05f);
        public static Vector3D<float> ambientColor { get; set; } = new(.2f);

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
            gl.Viewport(size);
            CreatePPFB();
        }
        public static void CreatePPFB()
        {
            postProcessFB = new(new FramebufferTexture((uint)App.window.FramebufferSize.X, (uint)App.window.FramebufferSize.Y,
                FramebufferAttachment.ColorAttachment0, InternalFormat.Rgba16f, PixelFormat.Rgba));
        }
        public static void CreateStandardResources()
        {
            CreatePPFB();

            quad = new("Models/Quad.obj");
            outputShader = new(Shader.Create("Shaders/OutputVert.glsl", ShaderType.VertexShader),
                Shader.Create("Shaders/OutputFrag.glsl", ShaderType.FragmentShader));

            skyboxModel = new(new(Shader.Create("Shaders/SkyboxVert.glsl", ShaderType.VertexShader),
                Shader.Create("Shaders/SkyboxFrag.glsl", ShaderType.FragmentShader)), new("Models/Inverted Cube.obj"));
            skyboxTexture = new(new string[6]
            {
                "Images/Skybox/right.jpg",
                "Images/Skybox/left.jpg",
                "Images/Skybox/bottom.jpg",
                "Images/Skybox/top.jpg",
                "Images/Skybox/front.jpg",
                "Images/Skybox/back.jpg",
            });

            shadowShader = new(Shader.Create("Shaders/ShadowVert.glsl", ShaderType.VertexShader),
                Shader.Create("Shaders/ShadowFrag.glsl", ShaderType.FragmentShader));
        }
        public static unsafe void Render()
        {
            List<MeshComponent> meshes = App.scene.FindComponents<MeshComponent>();
            List<LightComponent> lights = App.scene.FindComponents<LightComponent>();

            for (int l = 0; l < lights.Count; l++)
            {
                if (lights[l] is not DirectionalLightComponent light || !light.shadowsEnabled) continue;

                light.shadowMapBuffer.Bind();
                gl.DrawBuffer(DrawBufferMode.None);
                gl.Viewport(light.shadowResolution);
                gl.Clear(ClearBufferMask.DepthBufferBit);

                shadowShader.SetMat4Cache("uViewMat", light.viewMat);
                shadowShader.SetMat4Cache("uProjMat", light.projectionMat);

                for (int m = 0; m < meshes.Count; m++)
                {
                    if (!meshes[m].castShadows) continue;

                    Model model = new(shadowShader, meshes[m].mesh);
                    shadowShader.SetMat4Cache("uModelMat", meshes[m].transform.matrix);
                    model.Render();
                }
            }

            outputCam.framebuffer.Bind();
            gl.Viewport(outputCam.size);
            gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            skyboxModel.shader.SetMat4Cache("uViewMat", outputCam.viewMat);
            skyboxModel.shader.SetMat4Cache("uProjMat", outputCam.projectionMat);
            skyboxModel.shader.textures.Item1[0] = skyboxTexture;
            skyboxModel.Render();

            for (int m = 0; m < meshes.Count; m++)
            {
                if (meshes[m].lightingEnabled)
                {
                    IOrderedEnumerable<LightComponent> orderedLights =
                        lights.OrderBy(l => Math.Min(l.type, 1) * Vector3D.DistanceSquared(meshes[m].transform.position, l.transform.position));

                    for (int l = 0; l < 5; l++)
                    {
                        if (orderedLights.Count() > l)
                        {
                            LightComponent light = orderedLights.ElementAt(l);

                            meshes[m].shader.SetIntCache($"uLights[{l}].uType", light.type);
                            meshes[m].shader.SetVec3Cache($"uLights[{l}].uPosition", light.positionUniform);
                            meshes[m].shader.SetVec3Cache($"uLights[{l}].uColor", light.output);
                            meshes[m].shader.SetVec2Cache($"uLights[{l}].uFalloff", light.falloff);

                            if (!meshes[m].recieveShadows || light is not DirectionalLightComponent dLight) continue;

                            meshes[m].shader.SetTexture($"uLights[{l}].uShadowTexW", dLight.shadowMapBuffer.attachments[0]);
                            meshes[m].shader.SetMat4Cache($"uLights[{l}].uViewMat", dLight.viewMat);
                            meshes[m].shader.SetMat4Cache($"uLights[{l}].uProjMat", dLight.projectionMat);
                            meshes[m].shader.SetVec2Cache("uShadowNormalBias", shadowNormalBias);
                        }
                        else meshes[m].shader.SetVec3Cache($"uLights[{l}].uColor", Vector3D<float>.Zero);

                        meshes[m].shader.SetVec3Cache("uAmbientColor", ambientColor);
                    }
                }

                meshes[m].SetUniforms();
                meshes[m].Render();
                meshes[m].Cleanup();
            }

            gl.BlitNamedFramebuffer(outputCam.framebuffer.id, postProcessFB.id, 0, 0, outputCam.size.X, outputCam.size.Y, 0, 0, App.window.FramebufferSize.X,
                App.window.FramebufferSize.Y, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            postProcessFB.Bind();

            for (int i = 0; i < postProcessShaders.Count; i++)
            {
                Model ppModel = new(postProcessShaders[i], quad);
                ppModel.shader.SetTexture("uColorTex", postProcessFB.attachments[0]);
                ppModel.Render();
            }

            postProcessFB.Unbind();
            gl.Clear(ClearBufferMask.ColorBufferBit);

            Model outputModel = new(outputShader, quad);
            outputModel.shader.SetTexture("uColorTex", postProcessFB.attachments[0]);
            outputModel.shader.SetFloatCache("uGamma", gamma);
            outputModel.shader.SetFloatCache("uExposure", exposure);
            outputModel.shader.SetFloatCache("uTonemapStrength", tonemapStrength);
            outputModel.Render();

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
}
