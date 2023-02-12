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
using System.Runtime.Intrinsics;
using System.IO;

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

                MeshComponent ground = scene.CreateObject("Ground").AddComponent<MeshComponent>();
                ground.shader = new("Shaders/Scene/LitVert.glsl", "Shaders/Scene/LitFrag.glsl");
                ground.shader.SetVec4Cache("uAlbedo", new(.5f, .5f, .5f, 1f));
                ground.mesh = new("Models/Quad.obj");
                ground.transform.position = new(0f, -2f, 0f);
                ground.transform.scale = new(50f);
                ground.transform.eulerAngles = new(90f, 0f, 0f);
                ground.shader.SetFloatCache("uRoughness", .9f);
                ground.shader.SetFloatCache("uMetallic", .1f);

                return scene;
            });
        }
    }
}