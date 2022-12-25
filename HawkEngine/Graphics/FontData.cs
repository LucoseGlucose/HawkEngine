using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using StbTrueTypeSharp;
using StbImageSharp;
using StbRectPackSharp;

namespace HawkEngine.Graphics
{
    public sealed unsafe class Font
    {
        public readonly byte[] bitmap;
        public readonly int bitmapWidth, bitmapHeight;
        public readonly float scaleFactor;
        public readonly float pixelHeight;

        public readonly Texture2D fontAtlas;
        public readonly Dictionary<int, GlyphInfo> glyphs = new();
        public readonly Dictionary<int, StbTrueType.stbtt_packedchar> packedChars = new();

        private readonly StbTrueType.stbtt_pack_context context;

        public Font(int width, int height, string path, float fontPixelHeight, params CharacterRange[] characterRanges)
        {
            bitmapWidth = width;
            bitmapHeight = height;
            bitmap = new byte[width * height];
            context = new();
            pixelHeight = fontPixelHeight;

            fixed (byte* pixelsPtr = bitmap)
            {
                StbTrueType.stbtt_PackBegin(context, pixelsPtr, width, height, width, 1, null);
            }

            byte[] ttf = File.ReadAllBytes(Path.GetFullPath("../../../Resources/" + path));
            StbTrueType.stbtt_fontinfo fontInfo = StbTrueType.CreateFont(ttf, 0);

            scaleFactor = StbTrueType.stbtt_ScaleForPixelHeight(fontInfo, fontPixelHeight);
            int ascent, descent, lineGap;
            StbTrueType.stbtt_GetFontVMetrics(fontInfo, &ascent, &descent, &lineGap);

            foreach (CharacterRange range in characterRanges)
            {
                StbTrueType.stbtt_packedchar[] cd = new StbTrueType.stbtt_packedchar[range.End - range.Start + 1];
                fixed (StbTrueType.stbtt_packedchar* chardataPtr = cd)
                {
                    StbTrueType.stbtt_PackFontRange(context, fontInfo.data, 0, fontPixelHeight, range.Start, range.End - range.Start + 1, chardataPtr);
                }

                for (int i = 0; i < cd.Length; ++i)
                {
                    float yOff = cd[i].yoff;
                    yOff += ascent * scaleFactor;

                    GlyphInfo glyphInfo = new()
                    {
                        c = (char)(range.Start + i),
                        X = cd[i].x0,
                        Y = cd[i].y0,
                        Width = cd[i].x1 - cd[i].x0,
                        Height = cd[i].y1 - cd[i].y0,
                        XOffset = (int)cd[i].xoff,
                        YOffset = (int)Math.Round(yOff),
                        XAdvance = (int)Math.Round(cd[i].xadvance)
                    };

                    glyphs[i + range.Start] = glyphInfo;
                    packedChars[i + range.Start] = cd[i];
                }
            }

            fontAtlas = new((uint)bitmapWidth, (uint)bitmapHeight, bitmap, InternalFormat.R8, PixelFormat.Red);

            StbTrueType.stbtt_PackEnd(context);
        }
    }

    public readonly struct CharacterRange
    {
        public static readonly CharacterRange BasicLatin = new(0x0020, 0x007F);
        public static readonly CharacterRange Latin1Supplement = new(0x00A0, 0x00FF);
        public static readonly CharacterRange LatinExtendedA = new(0x0100, 0x017F);
        public static readonly CharacterRange LatinExtendedB = new(0x0180, 0x024F);
        public static readonly CharacterRange Cyrillic = new(0x0400, 0x04FF);
        public static readonly CharacterRange CyrillicSupplement = new(0x0500, 0x052F);
        public static readonly CharacterRange Hiragana = new(0x3040, 0x309F);
        public static readonly CharacterRange Katakana = new(0x30A0, 0x30FF);
        public static readonly CharacterRange Greek = new(0x0370, 0x03FF);
        public static readonly CharacterRange CjkSymbolsAndPunctuation = new(0x3000, 0x303F);
        public static readonly CharacterRange CjkUnifiedIdeographs = new(0x4e00, 0x9fff);
        public static readonly CharacterRange HangulCompatibilityJamo = new(0x3130, 0x318f);
        public static readonly CharacterRange HangulSyllables = new(0xac00, 0xd7af);

        public int Start { get; }

        public int End { get; }

        public int Size => End - Start + 1;

        public CharacterRange(int start, int end)
        {
            Start = start;
            End = end;
        }

        public CharacterRange(int single) : this(single, single)
        {

        }
    }

    public struct GlyphInfo
    {
        public char c;
        public int X, Y, Width, Height;
        public int XOffset, YOffset;
        public int XAdvance;
    }
}
