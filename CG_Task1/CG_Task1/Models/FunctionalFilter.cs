using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace CG_Task1.Models
{
    class FunctionalFilter
    {
        public string Name { get; set; }

        public PointCollection Points { get; private set; }

        public FunctionalFilter(string name)
        {
            Name = name;
            Points = new PointCollection
            {
                new System.Windows.Point(0, 0),
                new System.Windows.Point(255, 255)
            };
        }

        public FunctionalFilter(string name, PointCollection points)
        {
            Name = name;
            Points = points;
        }
        
        public void AddPoint(Point point)
        {
            Points.Add(point);
            PointCollection pointsCopy = new PointCollection();
            pointsCopy = Points.Clone();
            var orderedPoints = pointsCopy.OrderBy(point => point.X);
            Points.Clear();

            foreach(var orderedPoint in orderedPoints.ToList())
            {
                Points.Add(orderedPoint);
            }
        }
    }
}
