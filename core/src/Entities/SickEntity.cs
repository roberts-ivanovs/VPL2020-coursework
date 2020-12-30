using System;
using System.Numerics;

namespace DiseaseCore
{
    public class SickEntity : AbstractEntity
    {
        public float recovery = 0f; // 0.0-1.0 | 1.0 being recovered
        public float health = 1f; // 0.0-1.0 | 1.0 being no longer sick
        private float recoveryRatePerSecond = 0.05f;
        private static float DISEASE_BASE_DAMAGE = 0.005f;
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

        public static HealthyEntity ConvertToHealthy(SickEntity entity)
        {
            var healthy = new HealthyEntity();
            healthy.age = entity.age;
            return healthy;
        }

        override public void Tick(ulong milliseconds)
        {
            base.Tick(milliseconds);
            recovery += recoveryRatePerSecond * this.age * milliseconds / 1000;
            if (this.age > 30)
            {
                health -= DISEASE_BASE_DAMAGE * this.age * milliseconds / 1000;
            }
        }
    }
}
