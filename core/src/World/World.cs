using System;
using System.Collections.Generic;
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
        public ulong[] loopsDone;
        public (List<EntityOnMap<SickEntity>>, List<EntityOnMap<HealthyEntity>>) items;
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
        private List<EntityOnMap<SickEntity>> outOfBoundsPopulationSick = new List<EntityOnMap<SickEntity>>();
        private List<EntityOnMap<HealthyEntity>> outOfBoundsPopulationHealthy = new List<EntityOnMap<HealthyEntity>>();

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
            var calculatedOptimalCores = Environment.ProcessorCount * 2 / 3;
            if (singleCore || calculatedOptimalCores < 1)
            {
                this.NumberOfCores = 1;
            }
            else
            {
                this.NumberOfCores = calculatedOptimalCores;
            }
            this.initialHealthy = initialHealthy;
            this.initialSick = initialSick;
            this.timeScale = timeScale;
            this.SimState = SimulationState.PAUSED;
            this.pipelines = pipelines;

            /* Create different entity managers */
            regionManagers = new Region[NumberOfCores];
            int deltaX = World.MaxCoords.X / NumberOfCores;
            List<EntityOnMap<AbstractEntity>> population = new List<EntityOnMap<AbstractEntity>>();
            // Populate the initially healthy population
            if (outOfBoundsLock.WaitOne())
            {
                for (ushort _ = 0; _ < initialHealthy; _++)
                {
                    var p = new Point(rnd.Next(0, MaxCoords.X), rnd.Next(0, MaxCoords.Y));
                    var entity_constructed = new HealthyEntity();
                    outOfBoundsPopulationHealthy.Add(new EntityOnMap<HealthyEntity>(p, entity_constructed));
                }
                // Populate the initially sick population
                for (ushort _ = 0; _ < initialSick; _++)
                {
                    var p = new Point(rnd.Next(0, MaxCoords.X), rnd.Next(0, MaxCoords.Y));
                    var entity_constructed = new SickEntity();
                    outOfBoundsPopulationSick.Add(new EntityOnMap<SickEntity>(p, entity_constructed));
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
                    (Point point) => MustLeave(point, localMaxX, localMinX),
                    (List<EntityOnMap<SickEntity>> entitiesSick, List<EntityOnMap<HealthyEntity>> entitiesHealthy) => SyncResource(entitiesSick, entitiesHealthy, i)
                );
            }
        }

        private static bool MustLeave(Point point, int maxAllowedX, int minAllowedX)
        {
            return point.X < minAllowedX || point.X > maxAllowedX;
        }

        private bool SyncResource(List<EntityOnMap<SickEntity>> entitiesSick, List<EntityOnMap<HealthyEntity>> entitiesHealthy, int currentIndex)
        {
            if (outOfBoundsLock.WaitOne())
            {
                outOfBoundsPopulationSick.AddRange(entitiesSick);
                outOfBoundsPopulationHealthy.AddRange(entitiesHealthy);
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
            ulong[] loopsDone = new ulong[regionManagers.Length];
            List<EntityOnMap<HealthyEntity>>[] populationHealthy = new List<EntityOnMap<HealthyEntity>>[regionManagers.Length];
            List<EntityOnMap<SickEntity>>[] populationSick = new List<EntityOnMap<SickEntity>>[regionManagers.Length];
            pipelines.ForEach(x => x.updateTimeScale(timeScale));
            for (int i = 0; i < regionManagers.Length; ++i)
            {
                regionManagers[i].SimState = SimulationState.PAUSED;
            }

            // Asynchronously extract the current state from each running task
            Task[] waitingData = new Task[regionManagers.Length];
            for (int i = 0; i < regionManagers.Length; ++i)
            {
                int procIndex = i;
                waitingData[procIndex] = new Task(() =>
                {
                    var item = regionManagers[procIndex].getEntities();
                    populationSick[procIndex] = item.Item1;
                    populationHealthy[procIndex] = item.Item2;
                    loopsDone[procIndex] = item.Item3;
                });
                waitingData[procIndex].Start();
            }
            Task.WaitAll(waitingData);
            SyncTaskCode();

            outOfBoundsLock.WaitOne();
            var populationHealthyList = populationHealthy
                .Aggregate(
                    new List<EntityOnMap<HealthyEntity>>(),
                    (current, next) =>
                    {
                        current.AddRange(next);
                        return current;
                    }
                )
                .Concat(outOfBoundsPopulationHealthy)
                .ToList();
            var populationSickList = populationSick
                .Aggregate(
                    new List<EntityOnMap<SickEntity>>(),
                    (current, next) =>
                    {
                        current.AddRange(next);
                        return current;
                    }
                )
                .Concat(outOfBoundsPopulationSick)
                .ToList();
            outOfBoundsLock.ReleaseMutex();

            this.SimState = SimulationState.RUNNING;
            for (int i = 0; i < regionManagers.Length; ++i)
            {
                regionManagers[i].SimState = SimulationState.RUNNING;
            }

            var sickPeople = (ushort)populationSickList.Count();
            var healthyPeople = (ushort)populationHealthyList.Count();

            return new GameState
            {
                loopsDone = loopsDone,
                items = (populationSickList, populationHealthyList),
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
                List<EntityOnMap<SickEntity>>[] inboundSick = new List<EntityOnMap<SickEntity>>[regionManagers.Length];
                List<EntityOnMap<HealthyEntity>>[] inboundHealthy = new List<EntityOnMap<HealthyEntity>>[regionManagers.Length];
                for (int i = 0; i < regionManagers.Length; ++i)
                {
                    inboundSick[i] = new List<EntityOnMap<SickEntity>>();
                    inboundHealthy[i] = new List<EntityOnMap<HealthyEntity>>();
                }

                // Iterate over all `outOfBoundsPopulation` and
                // figure out where should each item be placed
                foreach (var item in outOfBoundsPopulationSick)
                {
                    // Normalise the location
                    item.location.Y = Math.Min(Math.Max(item.location.Y, 1), World.MaxCoords.Y - 1);
                    item.location.X = Math.Min(Math.Max(item.location.X, 1), World.MaxCoords.X - 1);
                    // Place each item in its appropriate placeholder data container
                    int index = item.location.X / deltaX;
                    index = Math.Min(Math.Max(index, 0), regionManagers.Length - 1);
                    inboundSick[index].Add(item);
                }
                foreach (var item in outOfBoundsPopulationHealthy)
                {
                    // Normalise the location
                    item.location.Y = Math.Min(Math.Max(item.location.Y, 1), World.MaxCoords.Y - 1);
                    item.location.X = Math.Min(Math.Max(item.location.X, 1), World.MaxCoords.X - 1);
                    // Place each item in its appropriate placeholder data container
                    int index = item.location.X / deltaX;
                    index = Math.Min(Math.Max(index, 0), regionManagers.Length - 1);
                    inboundHealthy[index].Add(item);
                }
                // Console.WriteLine($"outOfBoundsPopulationHealthy {outOfBoundsPopulationHealthy.Count()}");

                outOfBoundsPopulationHealthy.Clear();
                outOfBoundsPopulationSick.Clear();
                // Place each item in its appropriate thread
                for (int i = 0; i < regionManagers.Length; ++i)
                {
                    if ((inboundSick[i].Count() > 0 || inboundHealthy[i].Count() > 0) && regionManagers[i].inboundAccess.WaitOne())
                    {
                        regionManagers[i].inboundSick.AddRange(inboundSick[i]);
                        regionManagers[i].inboundHealthy.AddRange(inboundHealthy[i]);
                        regionManagers[i].inboundAccess.ReleaseMutex();
                    }
                    else
                    {
                        // Try appending the inbound item later
                        outOfBoundsPopulationSick.AddRange(inboundSick[i]);
                        outOfBoundsPopulationHealthy.AddRange(inboundHealthy[i]);
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
