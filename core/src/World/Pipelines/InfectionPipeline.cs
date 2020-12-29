
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiseaseCore
{

    internal class InfectionPipeline : Pipeline
    {
        private ushort radius;
        private int baseRadius = World.MaxCoords.X / 50;

        public InfectionPipeline(float timeScale)
        {
            updateRadius(timeScale);
        }

        public void updateRadius(float timeScale)
        {
            this.radius = (ushort)(baseRadius * (Math.Log10(timeScale) + timeScale / 10));
        }
        PipelineReturnData Pipeline.pushThrough(List<EntityOnMap> currentSick, List<EntityOnMap> currentHealthy)
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
                                        .Any(s => EntityOnMap.IsIntersecting(
                                            s.location, radius,
                                            h.location, radius)
                                        );
                                })
                            .Select((x, idx) =>
                                {
                                    // Covert to sick entities
                                    x.entity = SickEntity.ConvertToSick((HealthyEntity)x.entity);
                                    return x;
                                })
                            .Aggregate((new List<ulong>(), new List<EntityOnMap>()), (aggregate, item) =>
                                {
                                    aggregate.Item1.Add(item.ID);
                                    aggregate.Item2.Add(item);
                                    return aggregate;
                                })
                            .ToTuple();
            Console.WriteLine($"Infection ppline {toBeSick.Item2.Count()}");
            return new PipelineReturnData
            {
                newHealthy = currentHealthy.Where(x => !toBeSick.Item1.Contains(x.ID)).ToList(),
                newSick = currentSick.Concat(toBeSick.Item2).ToList(),
            };
        }
    }
}
