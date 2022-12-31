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

                MeshComponent mesh1 = scene.CreateObject("Cube").AddComponent<MeshComponent>();
                mesh1.shader = new("Shaders/Scene/LitVert.glsl", "Shaders/Scene/LitFrag.glsl");
                mesh1.shader.SetTexture("uAlbedoTexW", new Texture2D("Images/brickwall.jpg"));
                mesh1.shader.SetTexture("uNormalMapN", new Texture2D("Images/brickwall_normal.jpg", false));
                mesh1.mesh = new("Models/Cube.obj");
                mesh1.shader.SetFloatCache("uMetallic", .1f);
                mesh1.shader.SetFloatCache("uRoughness", .8f);

                MeshComponent mesh2 = scene.CreateObject("Monkey").AddComponent<MeshComponent>();
                mesh2.shader = new("Shaders/Scene/LitVert.glsl", "Shaders/Scene/LitFrag.glsl");
                mesh2.mesh = new("Models/Monkey.obj");
                mesh2.transform.position = new(2f, 4f, 1f);
                mesh2.shader.SetVec4Cache("uAlbedo", new(.5f, .5f, 1f, 1f));
                mesh2.shader.SetFloatCache("uMetallic", .1f);
                mesh2.shader.SetFloatCache("uRoughness", .9f);

                MeshComponent meshG = scene.CreateObject("Ground").AddComponent<MeshComponent>();
                meshG.shader = new("Shaders/Scene/LitVert.glsl", "Shaders/Scene/LitFrag.glsl");
                meshG.shader.SetVec4Cache("uAlbedo", new(.5f, .5f, .5f, 1f));
                meshG.mesh = new("Models/Quad.obj");
                meshG.transform.position = new(0f, -2f, 0f);
                meshG.transform.scale = new(50f);
                meshG.transform.rotation = new(90f, 0f, 0f);
                meshG.shader.SetFloatCache("uRoughness", 1f);
                mesh2.shader.SetFloatCache("uMetallic", 0f);

                MeshComponent mesh = scene.CreateObject("Ball").AddComponent<MeshComponent>();
                mesh.shader = new("Shaders/Scene/LitVert.glsl", "Shaders/Scene/LitFrag.glsl");
                mesh.shader.SetVec4Cache("uAlbedo", new(.05f, .2f, 1f, 1f));
                mesh.mesh = new("Models/Smooth Sphere.obj");
                mesh.transform.position = new(-4f, 1f, 0f);
                mesh.shader.SetFloatCache("uRoughness", .1f);
                mesh.shader.SetFloatCache("uMetallic", .1f);

                LightComponent light = scene.CreateObject("Light").AddComponent<DirectionalLightComponent>();
                light.color = new(.94f, .97f, .85f);
                light.strength = 5f;
                light.transform.rotation = new(60f, 230f, 0f);
                light.transform.position = new(0f, 5f, -2f);

                return scene;
            });
        }
    }
}