using System.Drawing;

namespace DiseaseCore
{

    internal class EntityOnMap
    {
        public ulong ID { get; }
        public static ulong IDCounter = 0;
        public Point location;
        public AbstractEntity entity;

        public EntityOnMap(Point location, AbstractEntity entity)
        {
            IDCounter += 1;
            ID = IDCounter;

            this.location = location;
            this.entity = entity;
        }
    }

}
