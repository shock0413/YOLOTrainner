using HCore.HDrawPoints;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace HanseroDisplay.Struct
{
    public class StructLabel
    {
        public Point RealPosition { get; set; }
        public FormattedText Text { get; set; }
        public object Tag { get; set; }
        public DrawLabel.DrawLabelAlign DrawLabelAlign { get; set; }
    }
}
