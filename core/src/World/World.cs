using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiseaseCore
{
    public enum SimulationState
    {
        PAUSED,
        RUNNING,
        DEAD,
    }

    public struct GameState
    {

        public (List<EntityOnMap>, List<EntityOnMap>) items;
        public ushort sickPeople;
        public ushort healthyPeople;
    }

    public class World
    {
        /* Static initializers */
        private static Random rnd = new Random();

        /* Map bounds */
        public readonly static Point MaxCoords = new Point(5000, 5000);

        /* Non defining game state values */
        private ushort initialHealthy { get; }
        private ushort initialSick { get; }
        public float timeScale { get; set; }
        public int NumberOfCores { get; set; }


        /* Live population accessors  */
        private Mutex outOfBoundsLock = new Mutex();
        private List<EntityOnMap> outOfBoundsPopulation = new List<EntityOnMap>();

        /* Multiple threads */
        private Region[] regionManagers;
        private Task[] tasks;
        private Task syncTask;
        private SimulationState SimState;
        private List<Pipeline> pipelines = new List<Pipeline>();

        public World(ushort initialHealthy, ushort initialSick, float timeScale, bool singleCore, List<Pipeline> pipelines)
        {

            // Using 2/3 of the computer cores. The other 1/3 is used for UI
            // rendering, server handling, your OS, etc.
            // Remove the arithmetic below and watch your computer INCENERATE.
            this.NumberOfCores = singleCore ? 1 : Environment.ProcessorCount * 2 / 3;
            this.initialHealthy = initialHealthy;
            this.initialSick = initialSick;
            this.timeScale = timeScale;
            this.SimState = SimulationState.PAUSED;
            this.pipelines = pipelines;

            /* Create different entity managers */
            regionManagers = new Region[NumberOfCores];
            int deltaX = World.MaxCoords.X / NumberOfCores;
            List<EntityOnMap> population = new List<EntityOnMap>();
            // Populate the initially healthy population
            if (outOfBoundsLock.WaitOne())
            {
                for (ushort _ = 0; _ < initialHealthy; _++)
                {
                    var p = new Point(rnd.Next(0, MaxCoords.X), rnd.Next(0, MaxCoords.Y));
                    var entity_constructed = new HealthyEntity();
                    outOfBoundsPopulation.Add(new EntityOnMap(p, entity_constructed));
                }
                // Populate the initially sick population
                for (ushort _ = 0; _ < initialSick; _++)
                {
                    var p = new Point(rnd.Next(0, MaxCoords.X), rnd.Next(0, MaxCoords.Y));
                    var entity_constructed = new SickEntity();
                    outOfBoundsPopulation.Add(new EntityOnMap(p, entity_constructed));
                }
                outOfBoundsLock.ReleaseMutex();
            }
            else
            {
                throw new InvalidOperationException("The initial mutex was already locked!");
            }
            for (int i = 0; i < NumberOfCores; i++)
            {
                int localMaxX = i * deltaX + deltaX;
                int localMinX = i * deltaX;
                regionManagers[i] = new Region(
                    (EntityOnMap entity) => MustLeave(entity, localMaxX, localMinX),
                    (List<EntityOnMap> entity) => SyncResource(entity, i)
                );
            }
        }

        private static bool MustLeave(EntityOnMap entity, int maxAllowedX, int minAllowedX)
        {
            return entity.location.X < minAllowedX || entity.location.X > maxAllowedX;
        }

        private bool SyncResource(List<EntityOnMap> entities, int currentIndex)
        {
            if (outOfBoundsLock.WaitOne())
            {
                outOfBoundsPopulation.AddRange(entities);
                outOfBoundsLock.ReleaseMutex();
                return true;
            }
            else
            {
                Console.WriteLine("Access NOT acquired by thread.");
                return false;
            }
        }

        public void Start()
        {
            tasks = new Task[regionManagers.Length];
            for (int i = 0; i < regionManagers.Length; ++i)
            {
                int procIndex = i;
                var item = regionManagers[procIndex];
                item.SimState = SimulationState.RUNNING;
                tasks[procIndex] = new Task(() =>
                {
                    item.StartLooping(ref pipelines);
                });
                tasks[i].Start();
            }

            // Spawn a task to sync the out-of-bounds population
            syncTask = new Task(() =>
            {
                while (this.SimState != SimulationState.DEAD)
                {
                    while (this.SimState == SimulationState.RUNNING)
                    {
                        SyncTaskCode();
                    }
                }
            });
            this.SimState = SimulationState.RUNNING;
            syncTask.Start();
        }

        public void Stop()
        {
            Console.WriteLine("KILLING THE SIMULATION");
            this.SimState = SimulationState.DEAD;
            foreach (var item in regionManagers)
            {
                item.SimState = SimulationState.DEAD;
            }
            Task.WaitAll(tasks); // Wait for all refion tasks to finish
            syncTask.Wait();
            syncTask.Dispose(); // Clear SyncTask
            Console.WriteLine("SIMULATION IS DEAD");

        }

        public GameState GetCurrentState()
        {
            this.SimState = SimulationState.PAUSED;
            List<EntityOnMap>[] population;
            {
                pipelines.ForEach(x => x.updateTimeScale(timeScale));
                for (int i = 0; i < regionManagers.Length; ++i)
                {
                    regionManagers[i].SimState = SimulationState.PAUSED;
                }

                // Asynchronously extract the current state from each running task
                Task[] waitingData = new Task[regionManagers.Length];
                population = new List<EntityOnMap>[regionManagers.Length];
                for (int i = 0; i < regionManagers.Length; ++i)
                {
                    int procIndex = i;
                    waitingData[procIndex] = new Task(() =>
                    {
                        var item = regionManagers[procIndex].getEntities();
                        population[procIndex] = item.Item1.Concat(item.Item2).ToList();
                    });
                    waitingData[procIndex].Start();
                }
                Task.WaitAll(waitingData);
            }
            SyncTaskCode();

            outOfBoundsLock.WaitOne();
            var returnable = population.Aggregate(
                new List<EntityOnMap>(),
                (current, next) => { current.AddRange(next); return current; }
            ).Concat(outOfBoundsPopulation).ToList();
            outOfBoundsLock.ReleaseMutex();

            this.SimState = SimulationState.RUNNING;
            for (int i = 0; i < regionManagers.Length; ++i)
            {
                regionManagers[i].SimState = SimulationState.RUNNING;
            }

            var items = returnable
                .Aggregate(
                    new Tuple<List<EntityOnMap>, List<EntityOnMap>>(new List<EntityOnMap>(), new List<EntityOnMap>()),
                    (tuple, item) =>
                    {
                        if (item.entity is SickEntity)
                        {
                            tuple.Item1.Add(item);
                        }
                        else
                        {
                            tuple.Item2.Add(item);
                        }
                        return tuple;
                    }
                ).ToValueTuple();

            var sickPeople = (ushort)items.Item1.Count();
            var healthyPeople = (ushort)items.Item2.Count();

            return new GameState
            {
                items = items,
                sickPeople = sickPeople,
                healthyPeople = healthyPeople,
            };
        }

        private void SyncTaskCode()
        {
            int deltaX = MaxCoords.X / NumberOfCores;
            if (outOfBoundsLock.WaitOne())
            {
                // Initiate placeholder inbound values for each thread
                List<EntityOnMap>[] inbound = new List<EntityOnMap>[regionManagers.Length];
                for (int i = 0; i < regionManagers.Length; ++i)
                {
                    inbound[i] = new List<EntityOnMap>();
                }

                // Iterate over all `outOfBoundsPopulation` and
                // figure out where should each item be placed
                foreach (var item in outOfBoundsPopulation)
                {
                    // Normalise the location
                    item.location.Y = Math.Min(Math.Max(item.location.Y, 1), World.MaxCoords.Y - 1);
                    item.location.X = Math.Min(Math.Max(item.location.X, 1), World.MaxCoords.X - 1);
                    // Place each item in its appropriate placeholder data container
                    int index = item.location.X / deltaX;
                    index = Math.Min(Math.Max(index, 0), regionManagers.Length - 1);
                    inbound[index].Add(item);
                }

                outOfBoundsPopulation.Clear();
                // Place each item in its appropriate thread
                for (int i = 0; i < regionManagers.Length; ++i)
                {
                    if (inbound[i].Count() > 0 && regionManagers[i].inboundAccess.WaitOne())
                    {
                        regionManagers[i].inbound.AddRange(inbound[i]);
                        regionManagers[i].inboundAccess.ReleaseMutex();
                    }
                    else
                    {
                        // Try appending the inbound item later
                        outOfBoundsPopulation.AddRange(inbound[i]);
                    }
                }
                outOfBoundsLock.ReleaseMutex();
            }
            else
            {
                Console.WriteLine("Blocked inside SyncTaskCode!");
            }
        }
    }
}
