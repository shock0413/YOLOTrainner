using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace HanseroDisplay.Struct
{
    public class StructRectangle
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public SolidColorBrush Brush { get; set; }
        public Pen Pen { get; set; }
        public string Tag { get; set; }

        public bool IsHide { get; set; }
    }
}
