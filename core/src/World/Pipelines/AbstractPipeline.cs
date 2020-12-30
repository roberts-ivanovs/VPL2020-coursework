
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DiseaseCore
{

    internal class AbstractPipeline : Pipeline
    {
        protected float timeScale { get; set; } = 1f;

        public virtual PipelineReturnData pushThrough(List<EntityOnMap> currentSick, List<EntityOnMap> currentHealthy, ulong timeDeltaMs)
        {
            throw new NotImplementedException();
        }

        public virtual void updateTimeScale(float timeScale)
        {
            this.timeScale = timeScale;
        }
    }
}
