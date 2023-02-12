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

            shader = new("Shaders/UI/TextVert.glsl", "Shaders/UI/TextFrag.glsl");
            font = new(256, 256, "Fonts/Ubuntu/Ubuntu-Regular.ttf", 32f, CharacterRange.BasicLatin);
            text = "1000 FPS";
            shader.SetVec4Cache("uColor", new(5f, 5f, 5f, 1f));
            transform.scale = new(50f);
        }
        public override void Update()
        {
            base.Update();

            mesh.Dispose();
            text = Math.Round(1f / Time.smoothUnscaledDeltaTime).ToString() + " FPS";

            transform.position = new(-Rendering.outputCam.size.X * .5f, Rendering.outputCam.size.Y * .5f
                - font.pixelHeight * transform.scale.Y / 50f, 0f);
        }
    }
}
