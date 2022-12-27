using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HawkEngine.Core;
using HawkEngine.Graphics;
using Silk.NET.Maths;
using StbTrueTypeSharp;
using static StbTrueTypeSharp.StbTrueType;

namespace HawkEngine.Components
{
    public class TextComponent : MeshComponent
    {
        private Font _font;
        public Font font { get => _font; set { _font = value; shader.SetTexture("uFontAtlasB", _font.fontAtlas); } }

        private string _text;
        public string text { get => _text; set => SetText(value); }

        public override void Create(SceneObject owner)
        {
            base.Create(owner);

            castShadows = false;
            recieveShadows = false;
        }
        public override void SetUniforms()
        {
            shader.SetMat4Cache("uModelMat", transform.matrix);
            shader.SetMat4Cache("uProjMat", Matrix4X4.CreateOrthographic(Rendering.outputCam.size.X, Rendering.outputCam.size.Y, .1f, 1f));
        }
        private unsafe void SetText(string value)
        {
            _text = value;
            List<Vector3D<float>> textVerts = new(_text.Length * 4);
            List<Vector2D<float>> textUVs = new(_text.Length * 4);
            List<uint> textIndices = new(_text.Length * 6);

            for (uint i = 0; i < _text.Length; i++)
            {
                GlyphInfo glyph = font.glyphs[_text[(int)i]];
                stbtt_packedchar packedChar = font.packedChars[_text[(int)i]];

                stbtt_aligned_quad quad;
                float xPos;
                float yPos;
                stbtt_GetPackedQuad(&packedChar, glyph.Width, glyph.Height, 0, &xPos, &yPos, &quad, 0);

                Vector3D<float>[] verts = new Vector3D<float>[4]
                {
                    new Vector3D<float>(quad.x0, quad.y0 - packedChar.yoff2 * 2f + glyph.Height, 0f) * font.scaleFactor,
                    new Vector3D<float>(quad.x1, quad.y0 - packedChar.yoff2 * 2f + glyph.Height, 0f) * font.scaleFactor,
                    new Vector3D<float>(quad.x1, quad.y1 - packedChar.yoff2 * 2f + glyph.Height, 0f) * font.scaleFactor,
                    new Vector3D<float>(quad.x0, quad.y1 - packedChar.yoff2 * 2f + glyph.Height, 0f) * font.scaleFactor,
                };

                Vector2D<float>[] uvs = new Vector2D<float>[4]
                {
                    new Vector2D<float>(quad.s0 * glyph.Width / font.bitmapWidth, quad.t1 * glyph.Height / font.bitmapHeight),
                    new Vector2D<float>(quad.s1 * glyph.Width / font.bitmapWidth, quad.t1 * glyph.Height / font.bitmapHeight),
                    new Vector2D<float>(quad.s1 * glyph.Width / font.bitmapWidth, quad.t0 * glyph.Height / font.bitmapHeight),
                    new Vector2D<float>(quad.s0 * glyph.Width / font.bitmapWidth, quad.t0 * glyph.Height / font.bitmapHeight),
                };

                uint[] indices = new uint[6] { i * 4, i * 4 + 1, i * 4 + 2, i * 4 + 2, i * 4 + 3, i * 4 };

                textVerts.AddRange(verts);
                textUVs.AddRange(uvs);
                textIndices.AddRange(indices);
            }

            Vector3D<float>[] textNorms = Utils.UniformArray(new Vector3D<float>(0f, 0f, 1f), _text.Length * 4);
            Vector3D<float>[] textTangents = Utils.UniformArray(new Vector3D<float>(1f, 0f, 0f), _text.Length * 4);
            Vector3D<float>[] textBitangents = Utils.UniformArray(new Vector3D<float>(0f, 1f, 0f), _text.Length * 4);

            MeshData meshData = new(textIndices.ToArray(), textVerts.ToArray(), textNorms, textUVs.ToArray(), textTangents, textBitangents);
            mesh = new(meshData, new(meshData));
        }
    }
}
