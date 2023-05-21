using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HOVLib
{
    public class HPoint
    {
        internal Point point;

        public HPoint(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public HPoint(Point point)
        {
            this.X = point.X;
            this.Y = point.Y;

            this.point = point;
        }

        public int X { get; set; }
        public int Y { get; set; }
    }
}
