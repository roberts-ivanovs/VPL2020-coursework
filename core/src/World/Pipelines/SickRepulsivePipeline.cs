
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DiseaseCore
{

    internal class SickRepulsivePipeline : Pipeline
    {
        PipelineReturnData Pipeline.pushThrough(List<EntityOnMap> currentSick, List<EntityOnMap> currentHealthy)
        {
            // Find the closest person via Y coordinates and change diraction to match it
            var newSick = currentSick;
            if (currentSick.Count() > 0 && currentHealthy.Count() > 0)
            {
                newSick = currentSick.Select(x =>
                {
                        var closestY = currentHealthy.Select(c => c.location.Y - x.location.Y).Min();
                        var closestX = currentHealthy.Select(c => c.location.X - x.location.X).Min();
                        if (x.location.X == closestX && x.location.Y == closestY)
                        {
                            // Hope that a new random direction will be generated
                            Console.WriteLine("SAME");
                        }
                    return x;
                }).ToList();
            }
            return new PipelineReturnData
            {
                newHealthy = currentHealthy,
                newSick = newSick,
            };
        }

    }
}
