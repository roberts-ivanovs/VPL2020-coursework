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
        private Func<EntityOnMap, bool> entityLeavesRegion;

        public Region(List<EntityOnMap> population, SimulationState SimState, Func<EntityOnMap, bool> entityLeavesRegion)
        {
            this.population = population;
            this.SimState = SimState;
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
                population
                    .Select(x => SimulateSubset(ref x, timeDeltaMS))
                    .Where(x => !entityLeavesRegion(x));
                previous = current;
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
