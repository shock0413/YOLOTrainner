using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace HOVLib
{
    public class HBlurCheck
    {
        public float GetBlurValue(BitmapSource bitmapSource)
        {
            Mat dst = ImageConverter.ToHMat(bitmapSource).Mat;

            float blurIndex = calcBlurriness(dst) * 10000;

            return blurIndex;
        }

        float calcBlurriness(Mat src)
        {
            Mat gx = new Mat();
            Mat gy = new Mat();
            Cv2.Sobel(src, gx, MatType.CV_32F, 1, 0);
            Cv2.Sobel(src, gy, MatType.CV_32F, 0, 1);
            double normGX = Cv2.Norm(gx);
            double normGy = Cv2.Norm(gy);
            double sumSq = normGX * normGX + normGy * normGy;
            gx.Dispose();
            gy.Dispose();
            return (float)(1.0 / (sumSq / (src.Size().Height * src.Size().Width) + 1e-6));
        }
    }
}
