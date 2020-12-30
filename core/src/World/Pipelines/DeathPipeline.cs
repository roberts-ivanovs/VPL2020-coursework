
using System.Collections.Generic;
using System.Linq;

namespace DiseaseCore
{

    internal class DeathPipeline : AbstractPipeline
    {
        public override PipelineReturnData pushThrough(List<EntityOnMap> currentSick, List<EntityOnMap> currentHealthy, ulong timeDeltaMs) => new PipelineReturnData
        {
            newHealthy = currentHealthy,
            newSick = currentSick.Where(x => ((SickEntity)x.entity).health > 0).ToList(),
        };

    }
}
