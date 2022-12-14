using HawkEngine.Graphics;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEngine.Components
{
    public class PointLightComponent : LightComponent
    {
        public override Vector2D<float> falloff { get; set; } = new(.09f, .032f);
        public override int type => 2;
        public override bool supportsShadows => false;

        public override void SetUniforms(string prefix, ShaderProgram shader)
        {
            shader.SetIntCache($"{prefix}.uType", type);
            shader.SetVec3Cache($"{prefix}.uColor", output);
            shader.SetVec2Cache($"{prefix}.uFalloff", falloff);
            shader.SetVec3Cache($"{prefix}.uPosition", transform.position);
        }
    }
}
