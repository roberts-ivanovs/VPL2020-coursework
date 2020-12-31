
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DiseaseCore
{

    public class AbstractPipeline : Pipeline
    {
        protected float timeScale { get; set; } = 1f;

        public virtual PipelineReturnData pushThrough(List<EntityOnMap<SickEntity>> currentSick, List<EntityOnMap<HealthyEntity>> currentHealthy, ulong timeDeltaMs)
        {
            throw new NotImplementedException();
        }

        public virtual void updateTimeScale(float timeScale)
        {
            this.timeScale = timeScale;
        }
    }
}
