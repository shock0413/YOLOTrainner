using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HOVLib
{
    public class HPoint2D
    {
        internal Point2d point;

        public HPoint2D(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public HPoint2D(Point2d point)
        {
            this.X = point.X;
            this.Y = point.Y;

            this.point = point;
        }

        public double X { get; set; }
        public double Y { get; set; }

        public System.Windows.Point GetPoint()
        {
            return new System.Windows.Point(X, Y);
        }
    }
}
