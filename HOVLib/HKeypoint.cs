using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HOVLib
{
    public class HKeyPoint
    {
        KeyPoint keyPoint;

        public float X { get { return keyPoint.Pt.X; } set { keyPoint.Pt.X = value; } }
        public float Y { get { return keyPoint.Pt.Y; } set { keyPoint.Pt.Y = value; } }
        public float Size { get { return keyPoint.Size; } set { keyPoint.Size = value; } } 

        public HKeyPoint(float x, float y, float size)
        {
            keyPoint = new KeyPoint(x, y, size);
        }
    }
}
