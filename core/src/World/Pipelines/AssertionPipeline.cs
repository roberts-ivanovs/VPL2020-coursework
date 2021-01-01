
using System.Diagnostics;
using System.Collections.Generic;

namespace DiseaseCore
{

    public class AssertionPipeline : AbstractPipeline
    {
        public override PipelineReturnData pushThrough(List<EntityOnMap<SickEntity>> currentSick, List<EntityOnMap<HealthyEntity>> currentHealthy, ulong timeDeltaMs)
        {
            currentSick
                .ForEach(x => Debug.Assert(x.entity is SickEntity, "Entity was not SickEntity"));
            currentHealthy
                .ForEach(x => Debug.Assert(x.entity is HealthyEntity, "Entity was not HealthyEntity"));
            return new PipelineReturnData
            {
                newHealthy = currentHealthy,
                newSick = currentSick,
            };
        }
    }
}
