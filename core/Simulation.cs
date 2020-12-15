using System;
using System.Collections.Generic;
using System.Threading;

namespace coursework
{
    class Simulation
    {
        private Mutex populationAccess = new Mutex();
        private uint initialPopulation { get; }
        private uint initialSick { get; }
        private short timeScale { get; set; }
        private List<AbstractEntity> population { get; }


        Simulation(uint initialPopulation, uint initialSick, short timeScale)
        {
            this.initialPopulation = initialPopulation;
            this.initialSick = initialSick;
            this.timeScale = timeScale;
        }


    }
}
