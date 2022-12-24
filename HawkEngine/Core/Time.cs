using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEngine.Core
{
    public static class Time
    {
        public static float unscaledDeltaTime { get; private set; }
        public static float unscaledTotalTime { get; private set; }

        public static float deltaTime { get; private set; }
        public static float totalTime { get; private set; }

        public static float smoothUnscaledDeltaTime { get; private set; }
        public static float smoothDeltaTime { get; private set; }

        public static float timeScale { get; set; } = 1f;
        public static float timeSmoothFactor { get; set; } = .25f;

        public static void Update(float dt)
        {
            unscaledDeltaTime = dt;
            unscaledTotalTime += unscaledDeltaTime;

            deltaTime = dt * timeScale;
            totalTime += deltaTime;

            smoothUnscaledDeltaTime = Utils.Lerp(smoothUnscaledDeltaTime, dt, timeSmoothFactor);
            smoothDeltaTime = smoothUnscaledDeltaTime * dt;
        }
    }
}
