using System.Drawing;

namespace DiseaseCore
{

    public class EntityOnMap
    {
        public ulong ID { get; }
        public static ulong IDCounter = 0;
        public Point location;
        public AbstractEntity entity;

        internal EntityOnMap(Point location, AbstractEntity entity)
        {
            IDCounter += 1;
            ID = IDCounter;

            this.location = location;
            this.entity = entity;
        }
    }

}
