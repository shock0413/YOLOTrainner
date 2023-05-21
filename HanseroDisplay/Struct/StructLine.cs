using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace HanseroDisplay.Struct
{
    public class StructLine : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public System.Windows.Media.Pen Pen { get; set; }
        public Point Point1 { get; set; }
        public Point Point2 { get; set; }
        public System.Drawing.Point RealPosition { get; set; }
        public string Tag { get; set; }
    }
}
