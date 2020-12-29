
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DiseaseCore
{

    internal class GravityPipeline : Pipeline
    {
        PipelineReturnData Pipeline.pushThrough(List<EntityOnMap> currentSick, List<EntityOnMap> currentHealthy)
        {
            // Find the closest person via Y coordinates and change diraction to match it
            var newSick = currentSick.Select(x =>
            {
                if (currentHealthy.Count() > 0)
                {
                    var closestY = currentHealthy.Select(c => c.location.Y - x.location.Y).Min();
                    // var closestX = currentHealthy.Select(c => (c.location.X - x.location.X, c.location.X)).Min();
                    x.entity.direction = new Vector3(x.entity.direction.X, closestY, 0);
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
