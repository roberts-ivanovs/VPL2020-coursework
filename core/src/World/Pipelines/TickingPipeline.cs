
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiseaseCore
{

    public class TickingPipeline : AbstractPipeline
    {
        public override PipelineReturnData pushThrough(List<EntityOnMap<SickEntity>> currentSick, List<EntityOnMap<HealthyEntity>> currentHealthy, ulong timeDeltaMs)
        {
            var scaledTime = (ulong)(timeDeltaMs * timeScale);
            currentSick.ForEach(x => x.entity.Tick(scaledTime));
            currentHealthy.ForEach(x => x.entity.Tick(scaledTime));
            return new PipelineReturnData
            {
                newHealthy = currentHealthy,
                newSick = currentSick,
            };
        }
    }
}
