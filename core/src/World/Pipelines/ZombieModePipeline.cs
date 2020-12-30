
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DiseaseCore
{

    internal class ZombieModePipeline : Pipeline
    {
        // This pipeline will NOT execute always
        private Random random = new Random();

        private static double calculateDistance(int deltaX, int deltaY)
        {
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }
        PipelineReturnData Pipeline.pushThrough(List<EntityOnMap> currentSick, List<EntityOnMap> currentHealthy)
        {

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
