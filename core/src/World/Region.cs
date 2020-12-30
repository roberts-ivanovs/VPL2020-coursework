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
        private int baseRadius = World.MaxCoords.X / 100;

        public Region(
            Func<EntityOnMap, bool> entityMustLeave,
            Func<List<EntityOnMap>, bool> upperPassEntities
        )
        {
            this.SimState = SimulationState.PAUSED;
            this.entityMustLeave = entityMustLeave;
            this.upperPassEntities = upperPassEntities;
        }

        public void StartLooping(ref List<Pipeline> pipelines)
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
                        var timeDeltaMS = (ulong)(current - previous);
                        /* Make sure turn through all the pipelines */
                        var pipelineResult = pipelines.Aggregate(new PipelineReturnData
                        {
                            newHealthy = populationHealthy,
                            newSick = populationSick
                        }, (aggregate, pipeline) => pipeline.pushThrough(aggregate.newSick, aggregate.newHealthy, timeDeltaMS));
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
