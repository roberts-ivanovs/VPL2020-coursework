using System;
using System.Numerics;

namespace DiseaseCore
{
    class HealthyEntity : AbstractEntity
    {
        protected ushort age { get; set; }

        public HealthyEntity()
        {
            age = (ushort)rnd.Next(1, 70);
        }

        public SickEntity ConvertToSick()
        {
            var sick = new SickEntity();
            sick.age = this.age;
            return sick;
        }
    }
}
