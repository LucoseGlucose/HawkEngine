using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEngine.Graphics
{
    public class Skybox
    {
        public readonly TextureCubemap skybox;
        public readonly TextureCubemap irradiance;

        public Skybox(string path, uint resolution, uint irradianceResolution)
        {
            Texture2D rectMap = new(path, hdr: true);

            skybox = Rendering.RenderToCubemap(rectMap, new ShaderProgram(Shader.Create("Shaders/Skybox/RectToCubemapVert.glsl", ShaderType.VertexShader),
                Shader.Create("Shaders/Skybox/RectToCubemapFrag.glsl", ShaderType.FragmentShader)), resolution);

            irradiance = Rendering.RenderToCubemap(skybox, new ShaderProgram(Shader.Create("Shaders/Skybox/RectToCubemapVert.glsl", ShaderType.VertexShader),
                Shader.Create("Shaders/Skybox/IrradianceConvolutionFrag.glsl", ShaderType.FragmentShader)), irradianceResolution);
        }
        public Skybox(string[] paths, uint resolution, uint irradianceResolution)
        {
            TextureCubemap cm = new(paths);

            skybox = Rendering.RenderToCubemap(cm, new ShaderProgram(Shader.Create("Shaders/Skybox/RectToCubemapVert.glsl", ShaderType.VertexShader),
                Shader.Create("Shaders/Skybox/RectToCubemapFrag.glsl", ShaderType.FragmentShader)), resolution);

            irradiance = Rendering.RenderToCubemap(skybox, new ShaderProgram(Shader.Create("Shaders/Skybox/RectToCubemapVert.glsl", ShaderType.VertexShader),
                Shader.Create("Shaders/Skybox/IrradianceConvolutionFrag.glsl", ShaderType.FragmentShader)), irradianceResolution);
        }
    }
}
