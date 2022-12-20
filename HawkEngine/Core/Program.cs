using HawkEngine.Components;
using HawkEngine.Graphics;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Maths;
using Silk.NET.GLFW;
using Silk.NET.Windowing;
using System.Drawing;
using Silk.NET.Assimp;

namespace HawkEngine.Core
{
    internal static class Program
    {
        private static unsafe void Main()
        {
            App.Run(() =>
            {
                Scene scene = new("Scene");

                CameraComponent cam = scene.CreateObject("Camera").AddComponent<CameraComponent>();
                Rendering.outputCam = cam;
                cam.transform.position = new(0f, 0f, -4f);
                cam.owner.AddComponent<CameraControllerComponent>();

                MeshComponent mesh1 = scene.CreateObject("Mesh").AddComponent<MeshComponent>();
                mesh1.shader = new(Graphics.Shader.Create("Shaders/Scene/LitVert.glsl", ShaderType.VertexShader),
                    Graphics.Shader.Create("Shaders/Scene/LitFrag.glsl", ShaderType.FragmentShader));
                mesh1.shader.SetTexture("uAlbedoTexW", new Texture2D("Images/brickwall.jpg"));
                mesh1.shader.SetTexture("uNormalMapN", new Texture2D("Images/brickwall_normal.jpg", false));
                mesh1.mesh = new("Models/Cube.obj");
                mesh1.shader.SetFloatCache("uMetallic", .1f);
                mesh1.shader.SetFloatCache("uRoughness", .65f);

                MeshComponent mesh2 = scene.CreateObject("Mesh").AddComponent<MeshComponent>();
                mesh2.shader = new(Graphics.Shader.Create("Shaders/Scene/LitVert.glsl", ShaderType.VertexShader),
                    Graphics.Shader.Create("Shaders/Scene/LitFrag.glsl", ShaderType.FragmentShader));
                mesh2.mesh = new("Models/Monkey.obj");
                mesh2.transform.position = new(2f, 4f, 1f);
                mesh2.shader.SetVec4Cache("uAlbedo", new(.5f, .5f, 1f, 1f));
                mesh2.shader.SetFloatCache("uMetallic", .2f);
                mesh2.shader.SetFloatCache("uRoughness", .4f);

                MeshComponent mesh = scene.CreateObject("Mesh").AddComponent<MeshComponent>();
                mesh.shader = new(Graphics.Shader.Create("Shaders/Scene/LitVert.glsl", ShaderType.VertexShader),
                    Graphics.Shader.Create("Shaders/Scene/LitFrag.glsl", ShaderType.FragmentShader));
                mesh.shader.SetVec4Cache("uAlbedo", new(.5f, .5f, .5f, 1f));
                mesh.mesh = new("Models/Quad.obj");
                mesh.transform.position = new(0f, -2f, 0f);
                mesh.transform.scale = new(50f);
                mesh.transform.rotation = new(90f, 0f, 0f);
                mesh.shader.SetFloatCache("uRoughness", .9f);

                LightComponent light = scene.CreateObject("Light").AddComponent<DirectionalLightComponent>();
                light.color = new(.94f, .97f, .85f);
                light.strength = 8f;
                light.transform.rotation = new(60f, 200f, 0f);
                light.transform.position = new(0f, 5f, -2f);

                FPSCounterComponent text = scene.CreateObject("FPS Counter").AddComponent<FPSCounterComponent>();

                return scene;
            });
        }
    }
}