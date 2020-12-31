using System;
using System.Numerics;

namespace DiseaseCore
{

    // TODO define entity interface that can be used in Canvas.razor?
    public abstract class AbstractEntity
    {
        /* Static initializers */
        protected readonly Random rnd = new Random();
        public Vector3 direction { get; set; }
        internal ushort age { get; set; }

        public AbstractEntity()
        {
            // direction = new Vector3(-1, 0, 0);
            direction = new Vector3(rnd.Next(-1, 2), rnd.Next(-1, 2), 0);
            age = (ushort)rnd.Next(1, 70);
        }
        public virtual void Tick(ulong milliseconds)
        {
            // 1/500 chance to get a new direction
            var val = rnd.Next(500);
            if (val == 0)
            {
                direction = new Vector3(rnd.Next(-1, 2), rnd.Next(-1, 2), 0);
            }
            // 1/500 chance to not move
            else if (val == 1)
            {
                direction = new Vector3(0, 0, 0);
            }
        }
    }
}
