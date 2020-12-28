using System;
using System.Numerics;

namespace DiseaseCore
{
    public class SickEntity : AbstractEntity
    {
        private float recovery = 0f; // 0.0-1.0 | 1.0 being recovered
        private float recoveryRatePerSecond = 1;
        public SickEntity(): base()
        {
            recoveryRatePerSecond = 1 / this.age;
        }

        public static SickEntity ConvertToSick(HealthyEntity entity)
        {
            var sick = new SickEntity();
            sick.age = entity.age;
            return sick;
        }

        override public void Tick(ulong milliseconds)
        {
            base.Tick(milliseconds);
            recovery *= recoveryRatePerSecond * milliseconds / 1000;
        }
    }
}
