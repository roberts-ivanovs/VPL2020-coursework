using System;
using System.Numerics;

namespace coursework
{
    abstract class AbstractEntity
    {
        private Vector3 direction { get; }

        AbstractEntity()
        {
            direction = new Vector3(0, 0, 0);
        }
        public void Tick()
        {
            // TODO Update direction (or dont)
        }
    }
}
