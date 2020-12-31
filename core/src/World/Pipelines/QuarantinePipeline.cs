
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace DiseaseCore
{

    public class QuarantinePipeline : AbstractPipeline
    {

        private Random random = new Random();
        private Point quarantineAreaLeftUpperCorner = new Point(0, 0);
        private Point quarantineAreaRightLowerCorner = new Point(World.MaxCoords.X / 10, World.MaxCoords.Y / 10);
        private Point memoAreaCenter;
        public QuarantinePipeline()
        {
            var xMiddlePointInArea = (quarantineAreaRightLowerCorner.X - quarantineAreaLeftUpperCorner.X) / 2;
            var yMiddlePointInArea = (quarantineAreaRightLowerCorner.Y - quarantineAreaLeftUpperCorner.Y) / 2;
            memoAreaCenter = new Point(xMiddlePointInArea, yMiddlePointInArea);
        }
        public override PipelineReturnData pushThrough(List<EntityOnMap> currentSick, List<EntityOnMap> currentHealthy, ulong timeDeltaMs)
        {
            currentSick.ForEach(x =>
            {
                // Determine if inside quarantine area or not
                if (isInsideQuarantineArea(x.location))
                {
                    // MinMax location
                    x.location.X = Math.Min(Math.Max(x.location.X, quarantineAreaLeftUpperCorner.X),  quarantineAreaRightLowerCorner.X);
                    x.location.Y = Math.Min(Math.Max(x.location.Y, quarantineAreaLeftUpperCorner.Y), quarantineAreaRightLowerCorner.Y);
                }
                else
                {
                    // Change direction to head towards quarantine area
                    var directionX = Math.Sign(memoAreaCenter.X - x.location.X);
                    var directionY = Math.Sign(memoAreaCenter.Y - x.location.Y);
                    x.entity.direction = new Vector3(directionX, directionY, 0);
                }
            });
            return new PipelineReturnData
            {
                newHealthy = currentHealthy,
                newSick = currentSick,
            };
        }

        private bool isInsideQuarantineArea(Point entityLocation)
        {
            return (entityLocation.X >= quarantineAreaLeftUpperCorner.X)
            && (entityLocation.Y >= quarantineAreaLeftUpperCorner.Y)
            && (entityLocation.X <= quarantineAreaRightLowerCorner.X)
            && (entityLocation.Y <= quarantineAreaRightLowerCorner.Y);
        }
    }
}
