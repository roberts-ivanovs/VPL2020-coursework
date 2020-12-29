
using System.Collections.Generic;
using System.Linq;

namespace DiseaseCore
{

    internal class DeathPipeline : Pipeline
    {
        PipelineReturnData Pipeline.pushThrough(List<EntityOnMap> currentSick, List<EntityOnMap> currentHealthy) => new PipelineReturnData
        {
            newHealthy = currentHealthy,
            newSick = currentSick.Where(x => ((SickEntity)x.entity).health > 0).ToList(),
        };

    }
}
