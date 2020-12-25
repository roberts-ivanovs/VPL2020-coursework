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
        public readonly static Point MaxCoords = new Point(1000, 1000);

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
            int deltaX = World.MaxCoords.X / NumberOfCores;
            List<EntityOnMap> population = new List<EntityOnMap>();
            // Populate the initially healthy population
            if (outOfBoundsLock.WaitOne(10))
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
            if (outOfBoundsLock.WaitOne(10))
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

            Console.WriteLine($"GetCurrentState 1");

            SyncTaskCode();
            Console.WriteLine($"GetCurrentState 2");
            this.SimState = SimulationState.PAUSED;
            List<EntityOnMap>[] population;
            {
                for (int i = 0; i < regionManagers.Length; ++i)
                {
                    regionManagers[i].SimState = SimulationState.PAUSED;
                }
                Console.WriteLine($"GetCurrentState 2.1");
                // Task.WaitAll(tasks);
                Console.WriteLine($"GetCurrentState 2.1.1");

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
                Console.WriteLine($"GetCurrentState 2.2");
                Task.WaitAll(waitingData);
            }
            Console.WriteLine($"GetCurrentState 3");
            this.SimState = SimulationState.RUNNING;
            var returnable = population.Aggregate(
                new List<EntityOnMap>(),
                (current, next) => { current.AddRange(next); return current; }
            ).ToList();
            Console.WriteLine($"GetCurrentState 4 {returnable.Count()}");
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
            int deltaX = MaxCoords.X / NumberOfCores;
            if (outOfBoundsLock.WaitOne(10))
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
                    Console.WriteLine($"out of bounds new idx {index}");
                    // Perform X axis wrapping
                    if (index >= regionManagers.Length)
                    {
                        // var newLocX = deltaX - (item.location.X / deltaX) ;
                        item.location.X = 0;
                        index = 0;
                    }
                    else if (index <= 0)
                    {
                        // var newLocX = MaxCoords.X - item.location.X;
                        item.location.X = MaxCoords.X;
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
            else
            {
                Console.WriteLine("Blocked inside SyncTaskCode!");
            }
        }
    }
}
