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
        private Mutex outOfBoundsLock = new Mutex();
        private List<EntityOnMap> outOfBoundsPopulation = new List<EntityOnMap>();

        /* Multiple threads */
        Region[] regionManagers;
        Task[] tasks;

        /* Game defining state */
        private SimulationState SimState = SimulationState.PAUSED;

        public World(uint initialPopulation, uint initialSick, float timeScale)
        {
            this.initialPopulation = initialPopulation;
            this.initialSick = initialSick;
            this.timeScale = timeScale;

            /* Create different entity managers */
            regionManagers = new Region[NumberOfCores];
            int deltaX = maxX / NumberOfCores;
            long singleSick = initialSick / NumberOfCores;
            long singleHealthy = initialPopulation / NumberOfCores - singleSick;
            for (int i = 0; i < NumberOfCores; i++)
            {
                List<EntityOnMap> population = new List<EntityOnMap>();
                // Populate the initially healthy population
                for (uint _ = 0; _ < singleHealthy; _++)
                {
                    var p = new Point(rnd.Next(minX, maxX), rnd.Next(minY, maxY));
                    var entity_constructed = new HealthyEntity();
                    outOfBoundsPopulation.Add(new EntityOnMap(p, entity_constructed));
                }
                // Populate the initially sick population
                for (uint _ = 0; _ < singleSick; _++)
                {
                    var p = new Point(rnd.Next(minX, maxX), rnd.Next(minY, maxY));
                    var entity_constructed = new SickEntity();
                    outOfBoundsPopulation.Add(new EntityOnMap(p, entity_constructed));
                }
                int localMaxX = i * deltaX + deltaX;
                int localMinX = i * deltaX;
                regionManagers[i] = new Region(
                    population,
                    SimState,
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
            foreach (var item in regionManagers)
            {
                item.SimState = SimulationState.RUNNING;
                item.StartLooping(this.timeScale);
            }

            // TODO Spawn a task to sync the out-of-bounds population
        }
    }
}
