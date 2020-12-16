using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace coursework
{

    internal enum SimulationState {
        PAUSED,
        RUNNING,
        SYNCYNG
    }

    class World
    {
        /* Static initializers */
        private static Random rnd = new Random();

        /* Map bounds */
        private static int maxX = 100;
        private static int maxY = 100;
        private static int minX = -100;
        private static int minY = -100;

        /* Non defining game state values */
        private uint initialPopulation { get; }
        private uint initialSick { get; }
        private short timeScale { get; set; }
        static int NumberOfCores = Environment.ProcessorCount;


        /* Live population accessors  */
        private Mutex populationAccess = new Mutex();
        private List<Tuple<Point, AbstractEntity>> population { get; set; }

        /* Multiple threads */
        private Task[] TaskPool;

        /* Game defining state */
        private SimulationState SimState = SimulationState.PAUSED;


        World(uint initialPopulation, uint initialSick, short timeScale)
        {
            this.initialPopulation = initialPopulation;
            this.initialSick = initialSick;
            this.timeScale = timeScale;

            // Populate the initially healthy `population`
            for (uint _ = 0; _ < initialPopulation; _++)
            {
                var p = new Point(rnd.Next(maxX, minX), rnd.Next(maxY, minY));
                var entity = new HealthyEntity();
                population.Add(new Tuple<Point, AbstractEntity>(p, entity));
            }
            // Populate the initially sick `population`
            for (uint _ = 0; _ < initialSick; _++)
            {
                var p = new Point(rnd.Next(maxX, minX), rnd.Next(maxY, minY));
                var entity = new SickEntity();
                population.Add(new Tuple<Point, AbstractEntity>(p, entity));
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
                    while (true) {
                        while (SimState == SimulationState.RUNNING) {
                            var current = sw.ElapsedMilliseconds;
                            SimulateSubset(manageable, (ulong) (current - previous));
                            previous = current;
                        }
                        Thread.Sleep(100);
                    }
                });
            }
        }

        private void SimulateSubset(List<Tuple<Point, AbstractEntity>> subset, ulong timeDeltaMs) {
            foreach (var item in subset)
            {
                item.Item2.Tick(timeDeltaMs);
            }
        }


    }
}
