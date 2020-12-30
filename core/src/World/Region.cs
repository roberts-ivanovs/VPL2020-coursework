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
        internal float timeScaleNumber;

        private int baseRadius = World.MaxCoords.X / 100;

        private List<Pipeline> pipelines = new List<Pipeline>();
        public Region(
            Func<EntityOnMap, bool> entityMustLeave,
            Func<List<EntityOnMap>, bool> upperPassEntities
        )
        {
            this.SimState = SimulationState.PAUSED;
            this.entityMustLeave = entityMustLeave;
            this.upperPassEntities = upperPassEntities;
            pipelines.Add(new DeathPipeline());
            pipelines.Add(new InfectionPipeline(timeScaleNumber));
            pipelines.Add(new ZombieModePipeline());
            pipelines.Add(new RecoveryPipeline());
        }

        public void timeScale(float timeScale)
        {
            timeScaleNumber = timeScale;
            foreach (var p in pipelines)
            {
                if (p is InfectionPipeline)
                {
                    ((InfectionPipeline)p).updateRadius(timeScale);
                }
            }
        }

        public void StartLooping()
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
                        var timeDeltaMS = (ulong)((current - previous) * timeScaleNumber);
                        populationSick = populationSick
                            .Select(x => SimulateSubset(ref x, timeDeltaMS))
                            .ToList();
                        populationHealthy = populationHealthy
                            .Select(x => SimulateSubset(ref x, timeDeltaMS))
                            .ToList();

                        /* Make sure turn through all the pipelines */
                        var pipelineResult = pipelines.Aggregate(new PipelineReturnData
                        {
                            newHealthy = populationHealthy,
                            newSick = populationSick
                        }, (aggregate, pipeline) => pipeline.pushThrough(aggregate.newSick, aggregate.newHealthy));
                        populationSick = pipelineResult.newSick;
                        populationHealthy = pipelineResult.newHealthy;

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

                        if ((toRemoveSick.Item1.Count() > 0 || toRemoveHealthy.Item1.Count() > 0) && upperPassEntities(toRemoveSick.Item2.Concat(toRemoveHealthy.Item2).ToList()))
                        {
                            populationSick = populationSick.Where(x => !toRemoveSick.Item1.Contains(x.ID)).ToList();
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
            item.location.Y = Math.Min(Math.Max(item.location.Y, 1), World.MaxCoords.Y - 1);
            item.location.X = Math.Min(Math.Max(item.location.X, 1),  World.MaxCoords.X - 1);
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
