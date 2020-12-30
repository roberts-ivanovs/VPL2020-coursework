
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace DiseaseCore
{

    internal class HealthyAttractorPipeline : AbstractPipeline
    {

        private Random random = new Random();
        private List<Point> attractors = new List<Point>();

        // Key -> EntityOnMap ID; Value -> index of the next attractor
        private Dictionary<ulong, int> nextAttractor = new Dictionary<ulong, int>();
        private int attractorRadius = World.MaxCoords.X / 300;
        public HealthyAttractorPipeline()
        {
            var attractorCount = random.Next(3, 10);
            for (int i = 0; i < attractorCount; ++i)
            {
                var maxX = World.MaxCoords.X;
                var maxY = World.MaxCoords.Y;
                attractors.Add(new Point(random.Next(0, maxX), random.Next(0, maxY)));
            }
        }
        public override PipelineReturnData pushThrough(List<EntityOnMap> currentSick, List<EntityOnMap> currentHealthy, ulong timeDeltaMs)
        {
            // Random random = new Random();

            // Perform attractor assignment
            currentHealthy.ForEach(x =>
            {
                // Check if is registered
                if (nextAttractor.ContainsKey(x.ID))
                {
                    var attractorIndex = nextAttractor[x.ID];
                    // Check if located at desired attractor
                    if (EntityOnMap.IsIntersecting(x.location, 1, attractors[attractorIndex], (ushort)attractorRadius))
                    {
                        // Generte a new attractor to head towards
                        nextAttractor[x.ID] = random.Next(0, attractors.Count() - 1);
                    }
                }
                else
                {
                    // Generte a new attractor to head towards
                    nextAttractor[x.ID] = random.Next(0, attractors.Count() - 1);
                }
            });

            // perform direction alterations
            currentHealthy.ForEach(x =>
            {
                // Add some randomness
                if (random.Next(500) == 0)
                {
                    var attractorIndex = nextAttractor[x.ID];
                    var closestX = Math.Sign(attractors[attractorIndex].X - x.location.X);
                    var closestY = Math.Sign(attractors[attractorIndex].Y - x.location.Y);
                    x.entity.direction = new Vector3(closestX, closestY, 0);
                }
            });
            return new PipelineReturnData
            {
                newHealthy = currentHealthy,
                newSick = currentSick,
            };
        }
    }
}
