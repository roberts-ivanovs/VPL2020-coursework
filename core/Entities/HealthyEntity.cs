using System;
using System.Numerics;

namespace DiseaseCore
{
    class HealthyEntity : AbstractEntity
    {
        protected readonly ushort age;

        public HealthyEntity()
        {
            age = (ushort)rnd.Next(1, 70);
        }
    }
}
