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
        public static readonly Dictionary<string, ShaderProgram> postProcessShaders = new();
        public static Framebuffer postProcessFB { get; private set; }

        public static Model skyboxModel { get; set; }
        public static TextureCubemap skyboxTexture { get; set; }

        public static ShaderProgram shadowShader { get; private set; }

        public static Vector3D<float> ambientColor { get; set; } = new(.05f);

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
        private static unsafe void RenderShadowMap(List<MeshComponent> meshes, Framebuffer fb, Vector2D<int> res, Matrix4X4<float> lightMat)
        {
            fb.Bind();
            gl.DrawBuffer(DrawBufferMode.None);
            gl.Viewport(res);
            gl.Clear(ClearBufferMask.DepthBufferBit);

            for (int m = 0; m < meshes.Count; m++)
            {
                if (!meshes[m].castShadows) continue;

                shadowShader.SetMat4Cache("uMat", meshes[m].transform.matrix * lightMat);
                meshes[m].mesh.vertexArray.Bind();
                gl.DrawElements(PrimitiveType.Triangles, (uint)meshes[m].mesh.meshData.indices.Length, DrawElementsType.UnsignedInt, null);
            }
        }
        public static unsafe void Render()
        {
            List<MeshComponent> meshes = App.scene.FindComponents<MeshComponent>();
            List<LightComponent> lights = App.scene.FindComponents<LightComponent>();

            for (int l = 0; l < lights.Count; l++)
            {
                if (!lights[l].supportsShadows) continue;

                if (lights[l] is DirectionalLightComponent dLight && dLight.shadowsEnabled)
                    RenderShadowMap(meshes, dLight.shadowMapBuffer, new(dLight.shadowResolution), dLight.viewMat * dLight.projectionMat);

                if (lights[l] is SpotLightComponent sLight && sLight.shadowsEnabled)
                    RenderShadowMap(meshes, sLight.shadowMapBuffer, new(sLight.shadowResolution), sLight.viewMat * sLight.projectionMat);
            }

            outputCam.framebuffer.Bind();
            gl.Viewport(outputCam.size);
            gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            skyboxModel.shader.SetMat4Cache("uViewMat", outputCam.viewMat);
            skyboxModel.shader.SetMat4Cache("uProjMat", outputCam.projectionMat);
            skyboxModel.shader.SetTexture("uSkybox", skyboxTexture);
            skyboxModel.Render();

            for (int m = 0; m < meshes.Count; m++)
            {
                if (meshes[m].lightingEnabled)
                {
                    IOrderedEnumerable<LightComponent> orderedLights =
                        lights.OrderBy(l => Math.Min(l.type, 1) * Vector3D.DistanceSquared(meshes[m].transform.position, l.transform.position));

                    for (int l = 0; l < 5; l++)
                    {
                        if (orderedLights.Count() > l) orderedLights.ElementAt(l).SetUniforms($"uLights[{l}]", meshes[m].shader);
                        else meshes[m].shader.SetIntCache($"uLights[{l}].uType", 0);

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

            foreach (KeyValuePair<string, ShaderProgram> shader in postProcessShaders)
            {
                Model ppModel = new(postProcessShaders[shader.Key], quad);
                ppModel.shader.SetTexture("uColorTex", postProcessFB[FramebufferAttachment.ColorAttachment0]);
                ppModel.Render();
            }

            postProcessFB.Unbind();
            gl.Clear(ClearBufferMask.ColorBufferBit);

            Model outputModel = new(outputShader, quad);
            outputModel.shader.SetTexture("uColorTex", postProcessFB[FramebufferAttachment.ColorAttachment0]);
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
