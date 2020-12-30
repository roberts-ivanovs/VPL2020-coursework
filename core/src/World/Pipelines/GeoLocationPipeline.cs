
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiseaseCore
{

    internal class GeoLocationPipeline : AbstractPipeline
    {
        public override PipelineReturnData pushThrough(List<EntityOnMap> currentSick, List<EntityOnMap> currentHealthy, ulong timeDeltaMs)
        {
            var scaledTime = (ulong)(timeDeltaMs * timeScale);
            // TODO move lambda block to a separate function
            currentSick.ForEach(x =>
            {
                var velocity = x.entity.direction * scaledTime;
                x.location.X += (int)velocity.X;
                x.location.Y += (int)velocity.Y;
                x.location.Y = Math.Min(Math.Max(x.location.Y, 1), World.MaxCoords.Y - 1);
                x.location.X = Math.Min(Math.Max(x.location.X, 1),  World.MaxCoords.X - 1);
            });
            currentHealthy.ForEach(x =>
            {
                var velocity = x.entity.direction * scaledTime;
                x.location.X += (int)velocity.X;
                x.location.Y += (int)velocity.Y;
                x.location.Y = Math.Min(Math.Max(x.location.Y, 1), World.MaxCoords.Y - 1);
                x.location.X = Math.Min(Math.Max(x.location.X, 1),  World.MaxCoords.X - 1);
            });
            return new PipelineReturnData
            {
                newHealthy = currentHealthy,
                newSick = currentSick,
            };
        }
    }
}
