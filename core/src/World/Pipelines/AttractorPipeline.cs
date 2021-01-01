
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace DiseaseCore
{

    public class AttractorPipeline : AbstractPipeline
    {

        private Random random = new Random();
        private List<Point> attractors = new List<Point>();

        private Mutex dictionaryMutex = new Mutex();
        // Key -> EntityOnMap ID; Value -> index of the next attractor
        private Dictionary<ulong, int> nextAttractor = new Dictionary<ulong, int>();
        private int attractorRadius = World.MaxCoords.X / 300;
        private bool redirectSick = false;
        private bool redirectHealthy = false;
        public AttractorPipeline(bool redirectSick, bool redirectHealthy)
        {
            this.redirectSick = redirectSick;
            this.redirectHealthy = redirectHealthy;
            var attractorCount = random.Next(3, 10);
            var deltaX = World.MaxCoords.X / 10;
            var deltaY = World.MaxCoords.Y / 10;
            for (int i = 0; i < attractorCount; ++i)
            {
                var maxX = World.MaxCoords.X;
                var maxY = World.MaxCoords.Y;
                attractors.Add(new Point(random.Next(0 + deltaX, maxX - deltaX), random.Next(0 + deltaY, maxY - deltaY)));
            }
        }
        public override PipelineReturnData pushThrough(List<EntityOnMap<SickEntity>> currentSick, List<EntityOnMap<HealthyEntity>> currentHealthy, ulong timeDeltaMs)
        {

            if (dictionaryMutex.WaitOne(1))
            {
                if (redirectHealthy)
                    HealthyAttractorPipelineUtility<HealthyEntity>.iterateOver(
                        currentHealthy,
                        random,
                        nextAttractor,
                        attractors,
                        attractorRadius
                    );
                if (redirectSick)
                    HealthyAttractorPipelineUtility<SickEntity>.iterateOver(
                        currentSick,
                        random,
                        nextAttractor,
                        attractors,
                        attractorRadius
                    );
                // Perform attractor assignment
                dictionaryMutex.ReleaseMutex();
            }
            return new PipelineReturnData
            {
                newHealthy = currentHealthy,
                newSick = currentSick,
            };
        }
    }

    class HealthyAttractorPipelineUtility<T> where T : AbstractEntity
    {
        internal static void iterateOver(
            List<EntityOnMap<T>> entities,
            Random random,
            Dictionary<ulong, int> nextAttractor,
            List<Point> attractors,
            int attractorRadius
        )
        {
            // Perform attractor assignment
            entities.ForEach(x =>
                {
                    // Check if is registered
                    if (nextAttractor.ContainsKey(x.ID))
                    {
                        var attractorIndex = nextAttractor[x.ID];
                        // Check if located at desired attractor
                        if (EntityOnMap<SickEntity>.IsIntersecting(x.location, 1, attractors[attractorIndex], (ushort)attractorRadius))
                        {
                            // Generte a new attractor to head towards
                            nextAttractor[x.ID] = random.Next(0, attractors.Count() - 1);
                        }
                        // Add some randomness
                        if (random.Next(100) == 0)
                        {
                            var attractorDirectionX = Math.Sign(attractors[attractorIndex].X - x.location.X);
                            var attractorDirectionY = Math.Sign(attractors[attractorIndex].Y - x.location.Y);
                            x.entity.direction = new Vector3(attractorDirectionX, attractorDirectionY, 0);
                        }
                    }
                    else
                    {
                        // Generte a new attractor to head towards
                        nextAttractor[x.ID] = random.Next(0, attractors.Count() - 1);
                    }
                });
        }
    }
}
