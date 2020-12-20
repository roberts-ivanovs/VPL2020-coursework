using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace DiseaseCore
{
    internal class Region
    {
        private List<EntityOnMap> population = new List<EntityOnMap>();
        public Mutex inboundAccess = new Mutex();
        public List<EntityOnMap> inbound = new List<EntityOnMap>();
        public SimulationState SimState { get; set; }
        private Func<EntityOnMap, bool> entityMustLeave;
        private Func<List<EntityOnMap>, bool> upperPassEntities;

        public Region(
            List<EntityOnMap> population,
            SimulationState SimState,
            Func<EntityOnMap, bool> entityMustLeave,
            Func<List<EntityOnMap>, bool> upperPassEntities
        )
        {
            this.population = population;
            this.SimState = SimState;
            this.entityMustLeave = entityMustLeave;
            this.upperPassEntities = upperPassEntities;
        }

        public void StartLooping(float timeScale)
        {
            var sw = new Stopwatch();
            sw.Start();
            var previous = 0L;
            while (SimState == SimulationState.RUNNING)
            {
                var current = sw.ElapsedMilliseconds;
                var timeDeltaMS = (ulong)((current - previous) * timeScale);
                population = population
                    .Select(x => SimulateSubset(ref x, timeDeltaMS))
                    .ToList();

                // Perform entity removal that have left the region
                var toRemove = population.Where(x => !entityMustLeave(x)).ToList();
                if (upperPassEntities(toRemove))
                {
                    population = population.Where(x => !toRemove.Contains(x)).ToList();
                };

                // Perform entity addition that have entered the region
                if (inboundAccess.WaitOne(100))
                {
                    population.AddRange(inbound);
                    inbound.Clear();
                    inboundAccess.ReleaseMutex();
                }
                previous = current; // Stopwatch update

            }
        }

        private ref EntityOnMap SimulateSubset(ref EntityOnMap item, ulong timeDeltaMs)
        {
            item.entity.Tick(timeDeltaMs);
            var scaled = item.entity.direction *= timeDeltaMs / 1000;
            item.location.X += (int)scaled.X;
            item.location.X += (int)scaled.Y;
            return ref item;
        }
    }
}
