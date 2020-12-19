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

    internal class EntityOnMap
    {
        public ulong ID { get; }
        public static ulong IDCounter = 0;
        public Point location;
        public AbstractEntity entity;

        public EntityOnMap(Point location, AbstractEntity entity)
        {
            IDCounter += 1;
            ID = IDCounter;

            this.location = location;
            this.entity = entity;
        }
    }

    public class World
    {
        /* Static initializers */
        private static Random rnd = new Random();

        /* Map bounds */
        private static int maxX = 1000;
        private static int maxY = 1000;
        private static int minX = -1000;
        private static int minY = -1000;

        /* Non defining game state values */
        private uint initialPopulation { get; }
        private uint initialSick { get; }
        private float timeScale { get; set; }
        static int NumberOfCores = Environment.ProcessorCount;


        /* Live population accessors  */
        private Mutex populationAccess = new Mutex();
        private List<EntityOnMap> population = new List<EntityOnMap>();

        /* Multiple threads */
        private Task[] TaskPool;

        /* Game defining state */
        private SimulationState SimState = SimulationState.PAUSED;

        public World(uint initialPopulation, uint initialSick, float timeScale)
        {
            this.initialPopulation = initialPopulation;
            this.initialSick = initialSick;
            this.timeScale = timeScale;

            // Populate the initially healthy `population`
            for (uint _ = 0; _ < initialPopulation; _++)
            {
                var p = new Point(rnd.Next(minX, maxX), rnd.Next(minY, maxY));
                var entity_constructed = new HealthyEntity();
                population.Add(new EntityOnMap(p, entity_constructed));
            }
            // Populate the initially sick `population`
            for (uint _ = 0; _ < initialSick; _++)
            {
                var p = new Point(rnd.Next(minX, maxX), rnd.Next(minY, maxY));
                var entity_constructed = new SickEntity();
                population.Add(new EntityOnMap(p, entity_constructed));
            }

            /* Create different entity managers */
            Task[] TaskPool = new Task[NumberOfCores];
            var set_size = population.Count / NumberOfCores;

            for (int i = 0; i < NumberOfCores; i++)
            {
                int procIndex = i;
                TaskPool[procIndex] = Task.Run(() =>
                {
                    var skip = set_size * i;
                    var manageable = population.Skip(skip).Take(set_size).ToList();
                    var sw = new Stopwatch();
                    sw.Start();
                    var previous = 0L;
                    while (true)
                    {
                        while (SimState == SimulationState.RUNNING)
                        {
                            var current = sw.ElapsedMilliseconds;
                            SimulateSubset(manageable, (ulong)((current - previous) * timeScale));
                            SyncResource(skip, set_size, manageable);
                            previous = current;
                        }
                        Thread.Sleep(100);
                    }
                });
            }
        }

        private void SimulateSubset(List<EntityOnMap> subset, ulong timeDeltaMs)
        {
            foreach (var item in subset)
            {
                item.entity.Tick(timeDeltaMs);
                var scaled = item.entity.direction *= timeDeltaMs / 1000;
                // item.location.X = new Point(1, 1);
                item.location.X = (int)scaled.X;
                item.location.X = (int)scaled.Y;

                // Calculate new position
                // Update new position
            }
        }

        private void SyncResource(int skip, int set_size, List<EntityOnMap> subset)
        {
            if (populationAccess.WaitOne(1000))
            {
                Console.WriteLine("Access IS acquired by thread.");
                // Simulate some work.
                // Thread.Sleep(50);
                // Release the Mutex.
                populationAccess.ReleaseMutex();
            }
            else
            {
                Console.WriteLine("Access NOT acquired by thread.");
            }
        }

        public void Start()
        {
            SimState = SimulationState.RUNNING;
        }

    }
}
