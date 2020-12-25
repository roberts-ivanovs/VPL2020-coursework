using System;
using System.Numerics;

namespace DiseaseCore
{
    abstract class AbstractEntity
    {
        /* Static initializers */
        protected readonly Random rnd = new Random();
        public Vector3 direction { get; set; }

        public AbstractEntity()
        {
            direction = new Vector3(rnd.Next(-1, 2), rnd.Next(-1, 2), 0);
        }
        public virtual void Tick(ulong milliseconds)
        {
            // 1/100 chance to get a new direction
            if (rnd.Next(100) == 0)
            {
                direction = new Vector3(rnd.Next(-1, 2), rnd.Next(-1, 2), 0);
            }
        }
    }
}
