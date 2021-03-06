
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiseaseCore
{

    public class InfectionPipeline : AbstractPipeline
    {
        private ushort radius;
        private int baseRadius = World.MaxCoords.X / 50;
        public override void updateTimeScale(float timeScale)
        {
            base.updateTimeScale(timeScale);
            this.radius = (ushort)(baseRadius * (Math.Log10(timeScale) + timeScale / 10));
        }

        public override PipelineReturnData pushThrough(List<EntityOnMap<SickEntity>> currentSick, List<EntityOnMap<HealthyEntity>> currentHealthy, ulong timeDeltaMs)
        {
            // Make entities sick if they need to
            /*
            NOTE: Because the timeScale parameter removes "frames"
            and possible intersections (no interpolation is being
            done as it's quite intensive to do at real time),
            the code tries to compensate by increasing the radius.
            Because we don't want everyone to become sick immediately,
            then linear scaling is not possible. Using this
            (https://www.desmos.com/calculator/7agyg8rqqz) for
            trying out some things.
            */
            var toBeSick = currentHealthy
                            .Where(h =>
                                {
                                    // Only keep entries that are intersecting with sick people
                                    return currentSick
                                        .Any(s => EntityOnMap<SickEntity>.IsIntersecting(
                                            s.location, radius,
                                            h.location, radius)
                                        );
                                })
                            .Select((x, idx) =>
                                {
                                    // Covert to sick entities
                                    var sickEntity = SickEntity.ConvertToSick(x.entity);
                                    var sickMapItem = EntityConverterUtility<HealthyEntity, SickEntity>.ConvertInnerEntities(x, sickEntity);
                                    return sickMapItem;
                                })
                            .Aggregate((new List<ulong>(), new List<EntityOnMap<SickEntity>>()), (aggregate, item) =>
                                {
                                    aggregate.Item1.Add(item.ID);
                                    aggregate.Item2.Add(item);
                                    return aggregate;
                                })
                            .ToTuple();
            return new PipelineReturnData
            {
                newHealthy = currentHealthy.Where(x => !toBeSick.Item1.Contains(x.ID)).ToList(),
                newSick = currentSick.Concat(toBeSick.Item2).ToList(),
            };
        }
    }
}
