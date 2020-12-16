using System;
using System.Numerics;

namespace coursework
{
    abstract class AbstractEntity
    {
        private Vector3 direction { get; }

        public AbstractEntity()
        {
            direction = new Vector3(0, 0, 0);
        }
        public virtual void Tick(uint milliseconds)
        {
            // TODO Update direction (or dont)
        }
    }
}
