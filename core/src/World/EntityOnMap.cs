using System;
using System.Drawing;

namespace DiseaseCore
{

    public class EntityOnMap<T> where T: AbstractEntity
    {
        public ulong ID { get; internal set; }
        public static ulong IDCounter = 0;
        public Point location;
        public T entity;

        internal EntityOnMap(Point location, T entity)
        {
            IDCounter += 1;
            ID = IDCounter;

            this.location = location;
            this.entity = entity;
        }

        internal static bool IsIntersecting(
            Point objectOneCenter, ushort objectOneRadius,
            Point objectTwoCenter, ushort objectTwoRadius
        )
        {
            Point pNew = new Point(objectOneCenter.X - objectTwoCenter.X, objectOneCenter.Y - objectTwoCenter.Y);
            return ((pNew.X * pNew.X) + (pNew.Y * pNew.Y)) <= Math.Pow(objectOneRadius + objectTwoRadius, 2);
        }

        internal static double calculateDistance(Point first, Point second)
        {
            var deltaX = second.X - first.X;
            var deltaY = second.Y - first.Y;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }
    }

    internal class EntityConverterUtility<T, K>
        where T : AbstractEntity
        where K : AbstractEntity {
        internal static EntityOnMap<K> ConvertInnerEntities(EntityOnMap<T> mapItem, K entityToInject)
        {
            var mapitemNew = new EntityOnMap<K>(mapItem.location, entityToInject);
            mapitemNew.ID = mapItem.ID;
            return mapitemNew;
        }
    }

}
