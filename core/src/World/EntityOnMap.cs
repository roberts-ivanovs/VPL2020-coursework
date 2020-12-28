using System;
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

        static internal bool IsIntersecting(
            Point objectOneCenter, ushort objectOneRadius,
            Point objectTwoCenter, ushort objectTwoRadius
        )
        {
            Point pNew = new Point(objectOneCenter.X - objectTwoCenter.X, objectOneCenter.Y - objectTwoCenter.Y);
            return ((pNew.X * pNew.X) + (pNew.Y * pNew.Y)) <= Math.Pow(objectOneRadius + objectTwoRadius, 2);
        }
    }

}
