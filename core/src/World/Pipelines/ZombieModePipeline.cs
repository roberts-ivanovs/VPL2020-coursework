
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DiseaseCore
{

    internal class ZombieModePipeline : AbstractPipeline
    {
        private static double calculateDistance(int deltaX, int deltaY)
        {
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }
        public override PipelineReturnData pushThrough(List<EntityOnMap> currentSick, List<EntityOnMap> currentHealthy, ulong timeDeltaMs)
        {

            // Instantiating here (instead as a class variable), otherwise there
            // are cases where `random.Next()` returns EQUAL values for multiple
            // threads
            Random random = new Random();
            // Find the closest person via Y coordinates and change diraction to match it
            var newSick = currentSick.Select(x =>
            {
                if (currentHealthy.Count() > 0 && random.Next(1000) == 0)
                {
                    var firstElement = currentHealthy.First();

                    // Go through all the entities and find the closest healthy entity
                    var closestEntity = currentHealthy
                        .Aggregate(
                            new Tuple<EntityOnMap, double>(firstElement, ZombieModePipeline.calculateDistance(firstElement.location.X, firstElement.location.Y)),
                            (aggregate, item) =>
                            {
                                var newDistance = ZombieModePipeline.calculateDistance(item.location.X - x.location.X, item.location.Y - x.location.Y);
                                return newDistance < aggregate.Item2 ? new Tuple<EntityOnMap, double>(item, newDistance) : aggregate;
                            });
                    var closestX = Math.Sign(closestEntity.Item1.location.X - x.location.X);
                    var closestY = Math.Sign(closestEntity.Item1.location.Y - x.location.Y);
                    x.entity.direction = new Vector3(closestX, closestY, 0);
                }
                return x;
            }).ToList();
            return new PipelineReturnData
            {
                newHealthy = currentHealthy,
                newSick = newSick,
            };
        }

    }
}
