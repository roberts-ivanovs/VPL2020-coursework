using System;
using System.Numerics;

namespace DiseaseCore
{
    class SickEntity : HealthyEntity
    {
        private float recovery = 0f; // 0.0-1.0 | 1.0 being recovered
        private float recoveryRatePerSecond = 1;
        public SickEntity()
        {
            recoveryRatePerSecond = 1 / this.age;
        }
        override public void Tick(ulong milliseconds)
        {
            base.Tick(milliseconds);
            recovery *= recoveryRatePerSecond * milliseconds / 1000;
        }
    }
}
