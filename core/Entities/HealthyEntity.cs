using System;
using System.Numerics;

namespace DiseaseCore
{
    class HealthyEntity : AbstractEntity
    {

        private ushort age;
        private ushort timesGottenSick;
        private ushort health;

        public HealthyEntity()
        {
            health = 100;
            timesGottenSick = 0;
            age = 50;  // TODO make the age random in a range
        }
    }
}
