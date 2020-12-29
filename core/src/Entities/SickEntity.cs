using System;
using System.Numerics;

namespace DiseaseCore
{
    public class SickEntity : AbstractEntity
    {
        private float recovery = 0f; // 0.0-1.0 | 1.0 being recovered
        public float health = 1f; // 0.0-1.0 | 1.0 being no longer sick
        private float recoveryRatePerSecond = 1;
        private static float DISEASE_BASE_DAMAGE = 0.01f;
        public SickEntity(): base()
        {
            recoveryRatePerSecond = 1 / this.age;  // The older the person the harder to recover
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
            recovery += recoveryRatePerSecond * milliseconds / 1000;
            health -= DISEASE_BASE_DAMAGE * this.age * milliseconds / 1000;
        }
    }
}
