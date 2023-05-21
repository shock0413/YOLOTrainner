using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HOVLib
{
    public class HSize
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public HSize(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }
    }
}
