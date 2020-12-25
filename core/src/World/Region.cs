using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace DiseaseCore
{
    internal class Region
    {
        private Mutex populationAccess = new Mutex();
        private List<EntityOnMap> population = new List<EntityOnMap>();

        public Mutex inboundAccess = new Mutex();
        public List<EntityOnMap> inbound = new List<EntityOnMap>();

        private Func<EntityOnMap, bool> entityMustLeave;
        private Func<List<EntityOnMap>, bool> upperPassEntities;

        /* Game defining state */
        public SimulationState SimState { get; set; }

        public Region(
            List<EntityOnMap> population,
            Func<EntityOnMap, bool> entityMustLeave,
            Func<List<EntityOnMap>, bool> upperPassEntities
        )
        {
            this.SimState = SimulationState.PAUSED;
            this.population = population;
            this.entityMustLeave = entityMustLeave;
            this.upperPassEntities = upperPassEntities;
        }

        public void StartLooping(float timeScale)
        {
            var sw = new Stopwatch();
            sw.Start();
            var previous = 0L;
            while (SimState != SimulationState.DEAD)
            {
                while (SimState == SimulationState.RUNNING)
                {
                    if (populationAccess.WaitOne())
                    {
                        var current = sw.ElapsedMilliseconds;
                        var timeDeltaMS = (ulong)((current - previous) * timeScale);
                        population = population
                            .Select(x => SimulateSubset(ref x, timeDeltaMS))
                            .ToList();
                        // TODO: Make entities sick if they need to


                        // Perform entity removal that have left the region
                        var toRemove = population.Where(x => entityMustLeave(x)).ToList();
                        if (upperPassEntities(toRemove))
                        {
                            population = population.Where(x => !toRemove.Contains(x)).ToList();
                        };

                        ReadFromInbound();
                        previous = current; // Stopwatch update
                        populationAccess.ReleaseMutex();
                    }
                    else
                    {
                        Console.WriteLine($"Region couldnt access its own population");
                    }
                }
            }
        }

        private void ReadFromInbound()
        {
            // Perform entity addition that have entered the region
            if (inboundAccess.WaitOne() && populationAccess.WaitOne())
            {
                population.AddRange(inbound);
                inbound.Clear();
                inboundAccess.ReleaseMutex();
                populationAccess.ReleaseMutex();
            }
            else
            {
                // if (inbound.Count() > 0)
                // {
                Console.WriteLine($"Couldnt access inbound lock! items - {inbound.Count()}");
                // }
            }
        }

        private static ref EntityOnMap SimulateSubset(ref EntityOnMap item, ulong timeDeltaMs)
        {
            item.entity.Tick(timeDeltaMs);
            var velocity = item.entity.direction * timeDeltaMs;
            item.location.X += (int)velocity.X;
            item.location.Y += (int)velocity.Y;

            // Perform Y axis wrapping
            if ((uint)item.location.Y >= World.MaxCoords.Y)
            {
                item.location.Y = 0;
            }
            else if ((uint)item.location.Y <= 0)
            {
                item.location.Y = World.MaxCoords.Y;
            }
            return ref item;
        }

        public List<EntityOnMap> getEntities()
        {
            SimState = SimulationState.PAUSED;
            List<EntityOnMap> res;
            if (populationAccess.WaitOne())
            {

                ReadFromInbound();
                res = population;
                populationAccess.ReleaseMutex();
            }
            else
            {
                Console.WriteLine("Regioun public API couldnt get population lock");
                res = new List<EntityOnMap>();
            }
            SimState = SimulationState.RUNNING;
            return res;
        }
    }
}
