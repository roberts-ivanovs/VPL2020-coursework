
using System.Collections.Generic;
using System.Linq;

namespace DiseaseCore
{

    public class DeathPipeline : AbstractPipeline
    {
        public override PipelineReturnData pushThrough(List<EntityOnMap<SickEntity>> currentSick, List<EntityOnMap<HealthyEntity>> currentHealthy, ulong timeDeltaMs) => new PipelineReturnData
        {
            newHealthy = currentHealthy,
            newSick = currentSick.Where(x => ((SickEntity)x.entity).health > 0).ToList(),
        };

    }
}
