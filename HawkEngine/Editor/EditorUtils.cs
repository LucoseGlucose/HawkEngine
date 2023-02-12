using HawkEngine.Core;
using ImGuiNET;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HawkEngine.Editor
{
    public static class EditorUtils
    {
#if DEBUG
        public static Vector4D<float> IDToColor(ulong id)
        {
            ulong first16 = (id & 0x000000000000FFFF) >> 0;
            ulong second16 = (id & 0x00000000FFFF0000) >> 16;
            ulong third16 = (id & 0x0000FFFF00000000) >> 32;
            ulong fourth16 = (id & 0xFFFF000000000000) >> 48;

            return new(first16, second16, third16, fourth16);
        }
        public static ulong ColorToID(Vector4D<float> color)
        {
            ulong first16 = (ulong)color.X;
            ulong second16 = (ulong)color.Y;
            ulong third16 = (ulong)color.Z;
            ulong fourth16 = (ulong)color.W;

            return first16 | (second16 << 16) | (third16 << 32) | (fourth16 << 48);
        }
#endif

        public enum MessageSeverity
        {
            Info,
            Warning,
            Error,
        }
        public struct ConsoleMessage
        {
            public readonly nint id;
            public MessageSeverity severity;
            public string message;
            public HawkObject obj;
            public string extraInfo;

            public ConsoleMessage(MessageSeverity severity, string message, HawkObject obj, string extraInfo)
            {
                id = (nint)Random.Shared.NextInt64();
                this.severity = severity;
                this.message = message;
                this.obj = obj;
                this.extraInfo = extraInfo;
            }
        }

        public static void PrintMessage(ConsoleMessage message)
        {
#if DEBUG
            EditorGUI.FindWindow<EditorConsole>().PrintMessage(message);
#endif
        }
        public static void PrintMessage(MessageSeverity severity, string message, HawkObject obj)
        {
            PrintMessage(new ConsoleMessage(severity, message, obj, null));
        }
        public static void PrintMessage(string message)
        {
            PrintMessage(new ConsoleMessage(MessageSeverity.Info, message, null, null));
        }
        public static void PrintMessage(string message, HawkObject obj)
        {
            PrintMessage(new ConsoleMessage(MessageSeverity.Info, message, obj, null));
        }
        public static void PrintMessage(MessageSeverity severity, string message)
        {
            PrintMessage(new ConsoleMessage(severity, message, null, null));
        }
        public static void PrintMessage(MessageSeverity severity, string message, string extraInfo)
        {
            PrintMessage(new ConsoleMessage(severity, message, null, extraInfo));
        }
        public static void PrintMessage(MessageSeverity severity, string message, HawkObject obj, string extraInfo)
        {
            PrintMessage(new ConsoleMessage(severity, message, obj, extraInfo));
        }
        public static void PrintMessage(object obj)
        {
            PrintMessage(obj?.ToString() ?? "null");
        }

        public static void PrintException(Exception exception)
        {
            PrintMessage(new ConsoleMessage(MessageSeverity.Error, exception.Message, null, exception.StackTrace));
        }

#if DEBUG
        public enum FontStyle
        {
            Regular,
            Bold,
            Italic,
            BoldItalic,
        }
        public enum FontSize
        {
            Medium,
            Small,
            Large,
        }
        public struct EditorFontData
        {
            public FontStyle fontStyle;
            public FontSize fontSize;
            public ImFontPtr fontPtr;
            private ushort[] range;

            public EditorFontData(FontStyle fontStyle, FontSize fontSize, ImFontPtr fontPtr)
            {
                this.fontStyle = fontStyle;
                this.fontSize = fontSize;
                this.fontPtr = fontPtr;
            }
            public EditorFontData(string path, float pixelSize, FontStyle style, FontSize size)
                : this(style, size, EditorGUI.io.Fonts.AddFontFromFileTTF(path, pixelSize))
            {

            }
            public unsafe EditorFontData MergeFonts(string path, float pixelSize, int min, int max)
            {
                ImFontConfig config = new()
                {
                    MergeMode = 1,
                };

                range = new ushort[2] { (ushort)min, (ushort)max };
                fixed (ushort* rangePtr = range)
                {
                    config.GlyphRanges = rangePtr;
                }

                EditorGUI.io.Fonts.AddFontFromFileTTF(path, pixelSize, &config);
                return this;
            }
        }
#endif
    }
}