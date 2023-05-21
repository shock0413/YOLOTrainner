using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace HOVLib
{
    public class HTemplateMatching
    {

        public enum MatchModeConstant { CCoeffNormed, CCorrNormed, SqDiffNormed };
        public MatchModeConstant MatchMode {get;set;}

        public TemplateMatchResult Run(HMat inputImage, TemplateMatch match)
        {
            Mat outputImage = new Mat();

            double minVal = 0;
            double maxVal = 0;

            Point minLoc;
            Point maxLoc;

            TemplateMatchModes openCVMode = GetOpenCVMatchMode(MatchMode);

            Cv2.MatchTemplate(inputImage.Mat, match.TemplitImage.Mat, outputImage, openCVMode);
            Cv2.MinMaxLoc(outputImage, out minVal, out maxVal, out minLoc, out maxLoc);

            TemplateMatchResult matchResult = new TemplateMatchResult();
            matchResult.Score = Math.Round(maxVal * 100);
            matchResult.Location = new HPoint(maxLoc);
            matchResult.Size = new HSizeD(match.TemplitImage.Width, match.TemplitImage.Height);

            return matchResult;
        }

        public static List<TemplateMatch> LoadTemplit(string path)
        {
            List<TemplateMatch> result = new List<TemplateMatch>();

            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path);
                files.ToList().ForEach(x =>
                {
                    if (x.EndsWith("templit"))
                    {
                        TemplateMatch match = new TemplateMatch();
                        match.TemplitImage = new HMat(new Mat(x));
                        match.Path = x;
                        match.Title = x.Split('\\')[x.Split('\\').Length - 1].Replace("templit", "");
                        //흑백 이미지 전환
                        result.Add(match);
                    }
                });
            }

            return result;
        }

        public static void AddTemplit(CroppedBitmap croppedBitmap, string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            int i = 0;
            while (true)
            {
                i++;

                if (!File.Exists(path + "\\" + i.ToString() + ".templit"))
                {
                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    Guid photoID = System.Guid.NewGuid();
                    string photolocation = path + "\\" + i.ToString() + ".templit";

                    encoder.Frames.Add(BitmapFrame.Create(croppedBitmap));

                    using (var filestream = new FileStream(photolocation, FileMode.Create))
                        encoder.Save(filestream);
                    return;
                }
            }
        }

        public static void RemoveTemplit(string path)
        {
            if(File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private TemplateMatchModes GetOpenCVMatchMode(MatchModeConstant mode)
        {
            switch (mode)
            {
                case MatchModeConstant.CCoeffNormed :
                    return TemplateMatchModes.CCoeffNormed;
                case MatchModeConstant.CCorrNormed:
                    return TemplateMatchModes.CCorrNormed;
                case MatchModeConstant.SqDiffNormed:
                    return TemplateMatchModes.SqDiffNormed;
                default:
                    return TemplateMatchModes.CCoeffNormed;
            }
        }

        public class TemplateMatch
        {
            public HMat TemplitImage { get; set; }
            public string Path;
            public string Title;
        }
        public class TemplateMatchResult
        {
            public double Score { get; set; }
            public HPoint Location { get; set; }
            public HSizeD Size { get; set; }
        }
    }
}
