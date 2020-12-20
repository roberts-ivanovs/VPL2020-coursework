using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiseaseCore
{
    internal enum SimulationState
    {
        PAUSED,
        RUNNING,
        SYNCYNG
    }

    public class World
    {
        /* Static initializers */
        private static Random rnd = new Random();

        /* Map bounds */
        internal readonly static int maxX = 1000;
        internal readonly static int maxY = 1000;
        internal readonly static int minX = -1000;
        internal readonly static int minY = -1000;

        /* Non defining game state values */
        private uint initialHealthy { get; }
        private uint initialSick { get; }
        private float timeScale { get; set; }
        static int NumberOfCores = Environment.ProcessorCount;


        /* Live population accessors  */
        private Mutex outOfBoundsLock = new Mutex();
        private List<EntityOnMap> outOfBoundsPopulation = new List<EntityOnMap>();

        /* Multiple threads */
        Region[] regionManagers;
        Task[] tasks;
        Task syncTask;

        SimulationState SimState;

        public World(uint initialHealthy, uint initialSick, float timeScale)
        {
            this.initialHealthy = initialHealthy;
            this.initialSick = initialSick;
            this.timeScale = timeScale;
            this.SimState = SimulationState.PAUSED;

            /* Create different entity managers */
            regionManagers = new Region[NumberOfCores];
            int deltaX = maxX / NumberOfCores;
            List<EntityOnMap> population = new List<EntityOnMap>();
            // Populate the initially healthy population
            outOfBoundsLock.WaitOne();
            for (uint _ = 0; _ < initialHealthy; _++)
            {
                var p = new Point(rnd.Next(minX, maxX), rnd.Next(minY, maxY));
                var entity_constructed = new HealthyEntity();
                outOfBoundsPopulation.Add(new EntityOnMap(p, entity_constructed));
            }
            // Populate the initially sick population
            for (uint _ = 0; _ < initialSick; _++)
            {
                var p = new Point(rnd.Next(minX, maxX), rnd.Next(minY, maxY));
                var entity_constructed = new SickEntity();
                outOfBoundsPopulation.Add(new EntityOnMap(p, entity_constructed));
            }
            outOfBoundsLock.ReleaseMutex();
            for (int i = 0; i < NumberOfCores; i++)
            {
                int localMaxX = i * deltaX + deltaX;
                int localMinX = i * deltaX;
                regionManagers[i] = new Region(
                    new List<EntityOnMap>(),
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
            if (outOfBoundsLock.WaitOne(100))
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
                tasks[procIndex] = new Task(() =>
                {
                    var item = regionManagers[procIndex];
                    item.SimState = SimulationState.RUNNING;
                    item.StartLooping(this.timeScale);
                });
                tasks[i].Start();
            }

            // Spawn a task to sync the out-of-bounds population
            syncTask = new Task(() =>
            {
                int deltaX = maxX / NumberOfCores;
                while (this.SimState == SimulationState.RUNNING)
                {
                    Console.WriteLine($"outOfBoundsPopulation {outOfBoundsPopulation.Count()}");
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
                            // Place each item in its appropriate placeholder data container
                            int index = item.location.X / deltaX;
                            // Perform X axis wrapping
                            if (index >= regionManagers.Length)
                            {
                                index = 0;
                            }
                            else if (index < 0)
                            {
                                index = regionManagers.Length - 1;
                            }
                            inbound[index].Add(item);
                        }

                        outOfBoundsPopulation.Clear();
                        // Place each item in its appropriate thread
                        for (int i = 0; i < regionManagers.Length; ++i)
                        {
                            if (inbound[i].Count() > 0 && regionManagers[i].inboundAccess.WaitOne(10))
                            {
                                Console.WriteLine($"Region manager recieves {inbound[i].Count()} entities");
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
                }
            });
            syncTask.Start();
            this.SimState = SimulationState.RUNNING;
        }

        public void Stop()
        {
            this.SimState = SimulationState.PAUSED;
            foreach (var item in regionManagers)
            {
                item.SimState = SimulationState.PAUSED;
            }
            Task.WaitAll(tasks); // Wait for all refion tasks to finish
            syncTask.Wait();
            syncTask.Dispose(); // Clear SyncTask
        }

        public List<EntityOnMap> GetCurrentState()
        {
            // Asynchronously extract the current state from each running task
            Task[] waitingData = new Task[regionManagers.Length];
            List<EntityOnMap>[] population = new List<EntityOnMap>[regionManagers.Length];
            for (int i = 0; i < regionManagers.Length; ++i)
            {
                int procIndex = i;
                waitingData[procIndex] = new Task(() =>
                {
                    var item = regionManagers[procIndex].getEntities();
                    Console.WriteLine($"Getting entities {item.Count()}");
                    population[procIndex] = item;
                });
                waitingData[procIndex].Start();
            }
            Task.WaitAll(waitingData);
            return population.Aggregate(
                new List<EntityOnMap>(),
                (current, next) => { current.AddRange(next); return current; }
            ).ToList();
        }
    }
}
