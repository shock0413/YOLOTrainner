using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HOVLib
{
    public class HSizeD
    {
        public double Width { get; set; }
        public double Height { get; set; }

        public HSizeD(double width, double height)
        {
            this.Width = width;
            this.Height = height;
        }
    }
}
