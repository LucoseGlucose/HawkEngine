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

                MeshComponent mesh = scene.CreateObject("Mesh").AddComponent<MeshComponent>();

                mesh.shader = new(Graphics.Shader.Create("../../../Shaders/LitVert.glsl", ShaderType.VertexShader),
                    Graphics.Shader.Create("../../../Shaders/LitFrag.glsl", ShaderType.FragmentShader));
                mesh.shader.SetTexture("uDiffuseTexW", new Texture2D("../../../Resources/Images/brickwall.jpg"));
                mesh.shader.SetTexture("uNormalTexN", new Texture2D("../../../Resources/Images/brickwall_normal.jpg"));
                mesh.shader.SetVec3Cache("uSpecular", new(.1f));

                mesh.mesh = new("../../../Resources/Models/Cube.obj");

                LightComponent light = scene.CreateObject("Light").AddComponent<LightComponent>();
                light.type = LightType.Directional;
                light.falloff = new(0f);
                light.color = new(.94f, .97f, .85f);
                light.strength = 3f;
                light.transform.rotation = new(-30, 20, 0);

                TextComponent text = scene.CreateObject("Text").AddComponent<TextComponent>();
                text.shader = new(Graphics.Shader.Create("../../../Shaders/TextVert.glsl", ShaderType.VertexShader),
                    Graphics.Shader.Create("../../../Shaders/TextFrag.glsl", ShaderType.FragmentShader));
                text.font = new(256, 256, "../../../Resources/Fonts/Ubuntu/Ubuntu-Regular.ttf", 32f, CharacterRange.BasicLatin);
                text.text = "Hello World!";
                text.transform.scale = new(200f);
                text.transform.position = new(-400f, 0f, 0f);

                return scene;
            });
        }
    }
}