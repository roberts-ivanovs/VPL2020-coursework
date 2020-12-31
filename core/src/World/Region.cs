using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace DiseaseCore
{
    internal class Region
    {
        private Mutex populationAccess = new Mutex();
        private List<EntityOnMap<SickEntity>> populationSick = new List<EntityOnMap<SickEntity>>();
        private List<EntityOnMap<HealthyEntity>> populationHealthy = new List<EntityOnMap<HealthyEntity>>();

        public Mutex inboundAccess = new Mutex();

        // TODO Separate this into Healthy and sick items!
        public List<EntityOnMap<SickEntity>> inboundSick = new List<EntityOnMap<SickEntity>>();
        public List<EntityOnMap<HealthyEntity>> inboundHealthy = new List<EntityOnMap<HealthyEntity>>();

        private Func<Point, bool> entityMustLeave;
        private Func<List<EntityOnMap<SickEntity>>, List<EntityOnMap<HealthyEntity>>, bool> upperPassEntities;

        /* Game defining state */
        public SimulationState SimState { get; set; }
        private int baseRadius = World.MaxCoords.X / 100;

        public Region(
            Func<Point, bool> entityMustLeave,
            Func<List<EntityOnMap<SickEntity>>, List<EntityOnMap<HealthyEntity>>, bool> upperPassEntities
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
                            .Where(x => entityMustLeave(x.location))
                            .Aggregate((new List<ulong>(), new List<EntityOnMap<HealthyEntity>>()), (aggregate, item) =>
                        {
                            aggregate.Item1.Add(item.ID);
                            aggregate.Item2.Add(item);
                            return aggregate;
                        }).ToTuple();

                        var toRemoveSick = populationSick
                            .Where(x => entityMustLeave(x.location))
                            .Aggregate((new List<ulong>(), new List<EntityOnMap<SickEntity>>()), (aggregate, item) =>
                        {
                            aggregate.Item1.Add(item.ID);
                            aggregate.Item2.Add(item);
                            return aggregate;
                        }).ToTuple();

                        if ((toRemoveSick.Item1.Count() > 0 || toRemoveHealthy.Item1.Count() > 0) && upperPassEntities(toRemoveSick.Item2, toRemoveHealthy.Item2))
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
                populationSick.AddRange(inboundSick);
                populationHealthy.AddRange(inboundHealthy);

                inboundSick.Clear();
                inboundHealthy.Clear();
                inboundAccess.ReleaseMutex();
                populationAccess.ReleaseMutex();
            }
            else
            {
                Console.WriteLine($"Couldnt access inbound lock! items - {inboundSick.Count()  + inboundHealthy.Count()}");
            }
        }

        public (List<EntityOnMap<SickEntity>>, List<EntityOnMap<HealthyEntity>>) getEntities()
        {
            (List<EntityOnMap<SickEntity>>, List<EntityOnMap<HealthyEntity>>) res;
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
    }
}
