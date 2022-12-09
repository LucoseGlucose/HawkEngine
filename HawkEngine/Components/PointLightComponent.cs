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
        public override int type => 1;
        public override Vector3D<float> positionUniform => transform.position;
    }
}
