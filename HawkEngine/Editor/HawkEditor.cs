#if DEBUG
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using HawkEngine.Core;
using HawkEngine.Graphics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using static ImGuiNET.ImGui;

namespace HawkEngine.Editor
{
    public static class HawkEditor
    {
        public static readonly List<HawkObject> selectedObjects = new();

        public static Graphics.Framebuffer objectIDFB { get; private set; }
        public static Graphics.Framebuffer outlineFB { get; private set; }

        private static readonly List<ShaderProgram> objectIDShaders = new();
        private static Graphics.Shader objectIDShader;

        private static readonly List<ShaderProgram> selectShaders = new();
        private static Graphics.Shader selectShader;

        private static Model outlineModel;

        public static void Init()
        {
            objectIDShader = Graphics.Shader.Create("Shaders/Editor/ObjectIDFrag.glsl", ShaderType.FragmentShader);
            selectShader = Graphics.Shader.Create("Shaders/Editor/SelectedFrag.glsl", ShaderType.FragmentShader);

            outlineModel = new(new("Shaders/Post Processing/OutputVert.glsl", "Shaders/Editor/OutlineFrag.glsl"), Rendering.quad);

            GenFramebuffers();
            EditorGUI.FindWindow("Viewport").sizeChanged += (_) => GenFramebuffers();

            Rendering.renderPasses.Insert(4, editorInfoPass);
            Rendering.renderPasses.Insert(6, outlinePass);
        }
        private static void GenFramebuffers()
        {
            if (Rendering.outputCam.size == Vector2D<int>.Zero) return;

            objectIDFB = new
            (
                new FramebufferTexture(new Texture2D((uint)Rendering.outputCam.size.X, (uint)Rendering.outputCam.size.Y,
                    InternalFormat.RG32f, PixelFormat.RG), FramebufferAttachment.ColorAttachment0),
                new FramebufferTexture(new Texture2D((uint)Rendering.outputCam.size.X, (uint)Rendering.outputCam.size.Y,
                    InternalFormat.RG32f, PixelFormat.RG), FramebufferAttachment.ColorAttachment1),
                new FramebufferTexture(new Texture2D((uint)Rendering.outputCam.size.X, (uint)Rendering.outputCam.size.Y,
                    InternalFormat.R32f, PixelFormat.Red, wrap: GLEnum.ClampToEdge), FramebufferAttachment.ColorAttachment2),
                new FramebufferTexture(new Texture2D((uint)Rendering.outputCam.size.X, (uint)Rendering.outputCam.size.Y, InternalFormat.DepthComponent24,
                    PixelFormat.DepthComponent, 1, GLEnum.Nearest), FramebufferAttachment.DepthAttachment)
            );

            outlineFB = new
            (
                new FramebufferTexture(new Texture2D((uint)Rendering.outputCam.size.X, (uint)Rendering.outputCam.size.Y,
                    InternalFormat.R32f, PixelFormat.Red, wrap: GLEnum.ClampToEdge), FramebufferAttachment.ColorAttachment0),
                new FramebufferTexture(new Texture2D((uint)Rendering.outputCam.size.X, (uint)Rendering.outputCam.size.Y, InternalFormat.DepthComponent24,
                    PixelFormat.DepthComponent, 1, GLEnum.Nearest), FramebufferAttachment.DepthAttachment)
            );
        }
        public static void Update()
        {
            EditorGUI.Update();
        }
        public static void Render()
        {
            EditorGUI.Render();
        }
        public static void Close()
        {
            EditorGUI.Close();
        }

        private static readonly Rendering.Pass editorInfoPass = (meshes, _) =>
        {
            objectIDFB.Bind();
            Rendering.gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            for (int m = 0; m < meshes.Count; m++)
            {
                ShaderProgram meshShader = meshes[m].shader;
                ShaderProgram newShader = objectIDShaders.FirstOrDefault(ns => ns[ShaderType.VertexShader] == meshShader[ShaderType.VertexShader]);

                if (newShader == null)
                {
                    newShader = new(meshShader[ShaderType.VertexShader], objectIDShader);
                    objectIDShaders.Add(newShader);
                }
                meshes[m].shader = newShader;

                newShader.SetVec4Cache("uIDColor", EditorUtils.IDToColor(meshes[m].owner.engineID));

                meshes[m].SetUniforms();
                meshes[m].Render();
                meshes[m].Cleanup();

                meshes[m].shader = meshShader;
            }

            outlineFB.Bind();
            Rendering.gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            for (int m = 0; m < meshes.Count; m++)
            {
                if (!selectedObjects.Contains(meshes[m].owner)) continue;

                ShaderProgram meshShader = meshes[m].shader;
                ShaderProgram newShader = selectShaders.FirstOrDefault(ns => ns[ShaderType.VertexShader] == meshShader[ShaderType.VertexShader]);

                if (newShader == null)
                {
                    newShader = new(meshShader[ShaderType.VertexShader], selectShader);
                    selectShaders.Add(newShader);
                }
                meshes[m].shader = newShader;

                meshes[m].SetUniforms();
                meshes[m].Render();
                meshes[m].Cleanup();

                meshes[m].shader = meshShader;
            }

            Rendering.outputCam.framebuffer.Bind();
        };

        private static readonly Rendering.Pass outlinePass = (_, _) =>
        {
            outlineModel.shader.SetTexture("uStencilTexB", outlineFB[FramebufferAttachment.ColorAttachment0]);
            outlineModel.shader.SetTexture("uDepthTexB", objectIDFB[FramebufferAttachment.ColorAttachment2]);
            outlineModel.shader.SetTexture("uSceneTexB", Rendering.postProcessFB[FramebufferAttachment.ColorAttachment0]);
            outlineModel.Render();
        };
    }
}
#endif