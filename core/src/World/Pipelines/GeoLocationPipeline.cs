
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiseaseCore
{

    public class GeoLocationPipeline: AbstractPipeline
    {
        public override PipelineReturnData pushThrough(List<EntityOnMap<SickEntity>> currentSick, List<EntityOnMap<HealthyEntity>> currentHealthy, ulong timeDeltaMs)
        {
            var scaledTime = (ulong)(timeDeltaMs * timeScale);
            GeoLocationPipelineUtility<SickEntity>.updateLocation(currentSick, scaledTime);
            GeoLocationPipelineUtility<HealthyEntity>.updateLocation(currentHealthy, scaledTime);
            return new PipelineReturnData
            {
                newHealthy = currentHealthy,
                newSick = currentSick,
            };
        }

    }

    class GeoLocationPipelineUtility<T> where T: AbstractEntity
    {
        internal static void updateLocation(List<EntityOnMap<T>> items, ulong scaledTime)
        {
            items.ForEach(x =>
            {
                var velocity = x.entity.direction * scaledTime;
                x.location.X += (int)velocity.X;
                x.location.Y += (int)velocity.Y;
                x.location.Y = Math.Min(Math.Max(x.location.Y, 1), World.MaxCoords.Y - 1);
                x.location.X = Math.Min(Math.Max(x.location.X, 1),  World.MaxCoords.X - 1);
            });
        }

    }
}
