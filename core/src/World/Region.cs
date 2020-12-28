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
        private List<EntityOnMap> populationSick = new List<EntityOnMap>();
        private List<EntityOnMap> populationHealthy = new List<EntityOnMap>();

        public Mutex inboundAccess = new Mutex();
        public List<EntityOnMap> inbound = new List<EntityOnMap>();

        private Func<EntityOnMap, bool> entityMustLeave;
        private Func<List<EntityOnMap>, bool> upperPassEntities;

        /* Game defining state */
        public SimulationState SimState { get; set; }

        public Region(
            Func<EntityOnMap, bool> entityMustLeave,
            Func<List<EntityOnMap>, bool> upperPassEntities
        )
        {
            this.SimState = SimulationState.PAUSED;
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
                        populationSick = populationSick
                            .Select(x => SimulateSubset(ref x, timeDeltaMS))
                            .ToList();
                        populationHealthy = populationHealthy
                            .Select(x => SimulateSubset(ref x, timeDeltaMS))
                            .ToList();

                        // Make entities sick if they need to
                        // TODO FINISH THIS AS THIS IS NOT WORKING
                        var toBeSick = populationHealthy
                            .Where(h =>
                                {
                                    // Only keep entries that are intersecting with sick people
                                    return populationSick
                                        .Any(s => EntityOnMap.IsIntersecting(s.location, 5, h.location, 5));
                                })
                            .Select((x, idx) =>
                                {
                                    // Covert to sick entities
                                    x.entity = SickEntity.ConvertToSick((HealthyEntity)x.entity);
                                    return x;
                                })
                            .Aggregate((new List<ulong>(), new List<EntityOnMap>()), (aggregate, item) =>
                                {
                                    aggregate.Item1.Add(item.ID);
                                    aggregate.Item2.Add(item);
                                    return aggregate;
                                })
                            .ToTuple();
                        populationHealthy = populationHealthy.Where(x => !toBeSick.Item1.Contains(x.ID)).ToList();
                        populationSick.AddRange(toBeSick.Item2);


                        // TODO Split this coded into functions to remove duplicate code
                        // Perform entity removal that have left the region
                        var toRemoveHealthy = populationHealthy
                            .Where(x => entityMustLeave(x))
                            .Aggregate((new List<ulong>(), new List<EntityOnMap>()), (aggregate, item) =>
                        {
                            aggregate.Item1.Add(item.ID);
                            aggregate.Item2.Add(item);
                            return aggregate;
                        }).ToTuple();
                        var toRemoveSick = populationSick
                            .Where(x => entityMustLeave(x))
                            .Aggregate((new List<ulong>(), new List<EntityOnMap>()), (aggregate, item) =>
                        {
                            aggregate.Item1.Add(item.ID);
                            aggregate.Item2.Add(item);
                            return aggregate;
                        }).ToTuple();

                        if (toRemoveSick.Item1.Count() > 0 && upperPassEntities(toRemoveSick.Item2))
                        {
                            populationSick = populationSick.Where(x => !toRemoveSick.Item1.Contains(x.ID)).ToList();
                        };

                        if (toRemoveHealthy.Item1.Count() > 0 && upperPassEntities(toRemoveHealthy.Item2))
                        {
                            populationHealthy = populationHealthy.Where(x => !toRemoveHealthy.Item1.Contains(x.ID)).ToList();
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
                var items = sortEntities(inbound);
                populationSick.AddRange(items.Item1);
                populationHealthy.AddRange(items.Item2);

                inbound.Clear();
                inboundAccess.ReleaseMutex();
                populationAccess.ReleaseMutex();
            }
            else
            {
                Console.WriteLine($"Couldnt access inbound lock! items - {inbound.Count()}");
            }
        }

        private static ref EntityOnMap SimulateSubset(ref EntityOnMap item, ulong timeDeltaMs)
        {
            item.entity.Tick(timeDeltaMs);
            var velocity = item.entity.direction * timeDeltaMs;
            item.location.X += (int)velocity.X;
            item.location.Y += (int)velocity.Y;

            // Perform X Y axis boundary check
            item.location.Y = Math.Min(Math.Max(item.location.Y, 0), World.MaxCoords.Y);
            item.location.X = Math.Min(Math.Max(item.location.X, 0),  World.MaxCoords.X);
            return ref item;
        }

        public (List<EntityOnMap>, List<EntityOnMap>) getEntities()
        {
            (List<EntityOnMap>, List<EntityOnMap>) res;
            if (populationAccess.WaitOne())
            {

                ReadFromInbound();
                res = (populationSick, populationHealthy);
                populationAccess.ReleaseMutex();
            }
            else
            {
                throw new Exception("Region public API couldnt get population lock");
            }
            return res;
        }


        internal static (List<EntityOnMap>, List<EntityOnMap>) sortEntities(List<EntityOnMap> inbound)
        {
            var res1 = inbound
                .Where(x => x.entity is SickEntity)
                .ToList();
            var res2 = inbound
                .Where(x => x.entity is HealthyEntity)
                .ToList();

            return (res1, res2);
        }
    }
}
