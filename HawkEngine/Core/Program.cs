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
using Silk.NET.Input;

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

                MeshComponent cube = scene.CreateObject("Cube").AddComponent<MeshComponent>();
                cube.shader = new("Shaders/Scene/LitVert.glsl", "Shaders/Scene/LitFrag.glsl");
                cube.shader.SetTexture("uAlbedoTexW", new Texture2D("Images/brickwall.jpg"));
                cube.shader.SetTexture("uNormalMapN", new Texture2D("Images/brickwall_normal.jpg", false));
                cube.mesh = new("Models/Cube.obj");
                cube.shader.SetFloatCache("uMetallic", .1f);
                cube.shader.SetFloatCache("uRoughness", .8f);

                MeshComponent monkey = scene.CreateObject("Monkey").AddComponent<MeshComponent>();
                monkey.shader = new("Shaders/Scene/LitVert.glsl", "Shaders/Scene/LitFrag.glsl");
                monkey.mesh = new("Models/Monkey.obj");
                monkey.transform.position = new(2f, 4f, 1f);
                monkey.shader.SetVec4Cache("uAlbedo", new(.5f, .5f, 1f, 1f));
                monkey.shader.SetFloatCache("uMetallic", .1f);
                monkey.shader.SetFloatCache("uRoughness", .9f);

                MeshComponent ground = scene.CreateObject("Ground").AddComponent<MeshComponent>();
                ground.shader = new("Shaders/Scene/LitVert.glsl", "Shaders/Scene/LitFrag.glsl");
                ground.shader.SetVec4Cache("uAlbedo", new(.5f, .5f, .5f, 1f));
                ground.mesh = new("Models/Quad.obj");
                ground.transform.position = new(0f, -2f, 0f);
                ground.transform.scale = new(50f);
                ground.transform.eulerAngles = new(90f, 0f, 0f);
                ground.shader.SetFloatCache("uRoughness", .9f);
                ground.shader.SetFloatCache("uMetallic", .1f);

                MeshComponent ball = scene.CreateObject("Ball").AddComponent<MeshComponent>();
                ball.shader = new("Shaders/Scene/LitVert.glsl", "Shaders/Scene/LitFrag.glsl");
                ball.shader.SetVec4Cache("uAlbedo", new(.05f, .2f, 1f, 1f));
                ball.mesh = new("Models/Smooth Sphere.obj");
                ball.transform.position = new(-4f, 1f, 0f);
                ball.shader.SetFloatCache("uRoughness", .1f);
                ball.shader.SetFloatCache("uMetallic", .1f);

                LightComponent light = scene.CreateObject("Light").AddComponent<DirectionalLightComponent>();
                light.color = new(.94f, .97f, .85f);
                light.strength = 5f;
                light.transform.eulerAngles = new(60f, 230f, 0f);
                light.transform.position = new(0f, 5f, -2f);

                return scene;
            });
        }
    }
}