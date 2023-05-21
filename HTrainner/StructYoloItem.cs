using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HsrAITrainner
{
    public class StructYoloItem
    {
        public int ID { get; set; }
        public string Type { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Confidence { get; set; }
    }
}
