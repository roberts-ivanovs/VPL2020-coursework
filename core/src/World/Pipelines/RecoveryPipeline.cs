
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiseaseCore
{

    public class RecoveryPipeline : AbstractPipeline
    {
        public override PipelineReturnData pushThrough(List<EntityOnMap> currentSick, List<EntityOnMap> currentHealthy, ulong timeDeltaMs)
        {
            var newHealthy = currentSick
            .Where(x => ((SickEntity)x.entity).recovery >= 1f)
            .Aggregate(new Tuple<List<EntityOnMap>, List<ulong>>(new List<EntityOnMap>(), new List<ulong>()), (aggregate, x) =>
            {
                x.entity = SickEntity.ConvertToHealthy((SickEntity)x.entity);
                aggregate.Item1.Add(x);
                aggregate.Item2.Add(x.ID);
                return aggregate;
            }).ToValueTuple();
            return new PipelineReturnData
            {
                newHealthy = currentHealthy.Concat(newHealthy.Item1).ToList(),
                newSick = currentSick.Where(x => !newHealthy.Item2.Contains(x.ID)).ToList(),
            };
        }
    }
}
