using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace coursework
{
    class Simulation
    {
        /* Map bounds */
        private static int maxX = 100;
        private static int maxY = 100;
        private static int minX = -100;
        private static int minY = -100;
        private static Random rnd = new Random();

        private Mutex populationAccess = new Mutex();
        private uint initialPopulation { get; }
        private uint initialSick { get; }
        private short timeScale { get; set; }

        // NOTE: population list items may need an extra id/hash param. only
        // time will tell.
        private List<Tuple<Point, AbstractEntity>> population { get; }


        Simulation(uint initialPopulation, uint initialSick, short timeScale)
        {
            this.initialPopulation = initialPopulation;
            this.initialSick = initialSick;
            this.timeScale = timeScale;

            // Populate the `population`
            for (uint _ = 0; _ < initialPopulation; _++)
            {
                var p = new Point(rnd.Next(maxX, minX), rnd.Next(maxY, minY));
                var entity = new HealthyEntity();
                population.Add(new Tuple<Point, AbstractEntity>(p, entity));
            }
            for (uint _ = 0; _ < initialSick; _++)
            {
                var p = new Point(rnd.Next(maxX, minX), rnd.Next(maxY, minY));
                var entity = new SickEntity();
                population.Add(new Tuple<Point, AbstractEntity>(p, entity));
            }
        }


    }
}
