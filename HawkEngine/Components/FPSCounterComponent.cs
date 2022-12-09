using HawkEngine.Core;
using HawkEngine.Graphics;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEngine.Components
{
    public class FPSCounterComponent : TextComponent
    {
        public override void Create(SceneObject owner)
        {
            base.Create(owner);

            shader = new(Graphics.Shader.Create("Shaders/TextVert.glsl", ShaderType.VertexShader),
               Graphics.Shader.Create("Shaders/TextFrag.glsl", ShaderType.FragmentShader));
            font = new(256, 256, "Fonts/Ubuntu/Ubuntu-Regular.ttf", 32f, CharacterRange.BasicLatin);
            text = "1000 FPS";
            shader.SetVec4Cache("uColor", new(5f, 5f, 5f, 1f));
            transform.scale = new(50f);
        }
        public override void Update()
        {
            text = Math.Round(1f / App.deltaTime).ToString() + " FPS";

            transform.position = new(-App.window.FramebufferSize.X * .5f, App.window.FramebufferSize.Y * .5f
                - font.pixelHeight * transform.scale.Y / 50f, 0f);
        }
    }
}
