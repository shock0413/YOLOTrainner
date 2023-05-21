using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HOVLib
{
    public class HMat
    {
        public new double Height { get { return Mat.Height; } }
        public new double Width { get { return Mat.Width; } }

        internal Mat Mat { get; set; }

        public HMat()
        {

        }

        internal HMat(Mat mat)
        {
            this.Mat = mat;
        }
    }
}
