using System;
using System.Numerics;

namespace DiseaseCore
{
    class SickEntity : HealthyEntity
    {
        private float recovery = 0;
        private float recoveryRatePerSecond = 1;
        public SickEntity()
        {
        }
        override public void Tick(ulong milliseconds)
        {
            base.Tick(milliseconds);
            recovery *= recoveryRatePerSecond * milliseconds / 1000;
        }
    }
}
