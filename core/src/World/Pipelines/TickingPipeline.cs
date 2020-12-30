
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiseaseCore
{

    internal class TickingPipeline : AbstractPipeline
    {
        public override PipelineReturnData pushThrough(List<EntityOnMap> currentSick, List<EntityOnMap> currentHealthy, ulong timeDeltaMs)
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
