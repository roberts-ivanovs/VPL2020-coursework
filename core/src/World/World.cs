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
        public List<EntityOnMap> items;
        public ushort sickPeople;
        public ushort healthyPeople;
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
        private ushort initialHealthy { get; }
        private ushort initialSick { get; }
        private float timeScale { get; set; }

        // Using 2/3 of the computer cores. The other 1/3 is used for UI
        // rendering, server handling, etc.
        // WARNING: Remove the arithmetic below and watch your computer incenerate
        public static int NumberOfCores { get; } = Environment.ProcessorCount * 2 / 3;


        /* Live population accessors  */
        private Mutex outOfBoundsLock = new Mutex();
        private List<EntityOnMap> outOfBoundsPopulation = new List<EntityOnMap>();

        /* Multiple threads */
        private Region[] regionManagers;
        private Task[] tasks;
        private Task syncTask;
        private SimulationState SimState;

        public World(ushort initialHealthy, ushort initialSick, float timeScale)
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
            for (ushort _ = 0; _ < initialHealthy; _++)
            {
                var p = new Point(rnd.Next(minX, maxX), rnd.Next(minY, maxY));
                var entity_constructed = new HealthyEntity();
                outOfBoundsPopulation.Add(new EntityOnMap(p, entity_constructed));
            }
            // Populate the initially sick population
            for (ushort _ = 0; _ < initialSick; _++)
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

        public SimulationState GetState()
        {
            return SimState;
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
                var item = regionManagers[procIndex];
                item.SimState = SimulationState.RUNNING;
                tasks[procIndex] = new Task(() =>
                {
                    item.StartLooping(this.timeScale);
                });
                tasks[i].Start();
            }

            // Spawn a task to sync the out-of-bounds population
            syncTask = new Task(() =>
            {
                while (this.SimState == SimulationState.RUNNING)
                {
                    SyncTaskCode();
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
            SyncTaskCode();
            this.SimState = SimulationState.PAUSED;
            List<EntityOnMap>[] population;
            {
                for (int i = 0; i < regionManagers.Length; ++i)
                {
                    regionManagers[i].SimState = SimulationState.PAUSED;
                }
                Task.WaitAll(tasks);

                // Asynchronously extract the current state from each running task
                Task[] waitingData = new Task[regionManagers.Length];
                population = new List<EntityOnMap>[regionManagers.Length];
                for (int i = 0; i < regionManagers.Length; ++i)
                {
                    int procIndex = i;
                    waitingData[procIndex] = new Task(() =>
                    {
                        var item = regionManagers[procIndex].getEntities();
                        population[procIndex] = item;
                    });
                    waitingData[procIndex].Start();
                }
                Task.WaitAll(waitingData);
            }
            this.SimState = SimulationState.RUNNING;
            var returnable = population.Aggregate(
                new List<EntityOnMap>(),
                (current, next) => { current.AddRange(next); return current; }
            ).ToList();
            for (int i = 0; i < regionManagers.Length; ++i)
            {
                regionManagers[i].SimState = SimulationState.RUNNING;
            }
            var sickPeople = (ushort)returnable.GroupBy(x => x.entity is SickEntity).Count();
            var healthyPeople = (ushort)(returnable.Count() - sickPeople);

            Console.WriteLine($"HEALTHY {healthyPeople} | SICK {sickPeople}");
            return new GameState
            {
                items = returnable,
                sickPeople = sickPeople,
                healthyPeople = healthyPeople,
            };
        }

        private void SyncTaskCode()
        {
            int deltaX = maxX / NumberOfCores;
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
    }
}
