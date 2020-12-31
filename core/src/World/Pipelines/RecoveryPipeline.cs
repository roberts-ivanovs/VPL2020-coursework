
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiseaseCore
{

    public class RecoveryPipeline : AbstractPipeline
    {
        public override PipelineReturnData pushThrough(List<EntityOnMap<SickEntity>> currentSick, List<EntityOnMap<HealthyEntity>> currentHealthy, ulong timeDeltaMs)
        {
            var newHealthy = currentSick
            .Where(x => ((SickEntity)x.entity).recovery >= 1f)
            .Aggregate(new Tuple<List<EntityOnMap<HealthyEntity>>, List<ulong>>(new List<EntityOnMap<HealthyEntity>>(), new List<ulong>()), (aggregate, x) =>
            {
                var healthyEntity = SickEntity.ConvertToHealthy(x.entity);
                var sickMapItem = EntityConverterUtility<SickEntity, HealthyEntity>.ConvertInnerEntities(x, healthyEntity);
                aggregate.Item1.Add(sickMapItem);
                aggregate.Item2.Add(sickMapItem.ID);
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
