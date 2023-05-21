using OpenCvSharp;
using OpenCvSharp.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HOVLib
{
    public class HBlob
    {
        CvBlobs blobTool;

        public List<KeyValuePair<int, Blob>> Blobs = new List<KeyValuePair<int, Blob>>();

        public HBlobToolFilter Filter { get; set; }

        public enum MergeModeConstant { Biggest, Least }
        public MergeModeConstant MergeMode { get; set; }
        public bool MergeBlob { get { return mergeBlob; } set { mergeBlob = value; } }
        private bool mergeBlob = false;
        public int MergeThreshold { get { return mergeThresold; } set { mergeThresold = value; } }
        private int mergeThresold = 100;

        public HBlob()
        {
            blobTool = new CvBlobs();
            Filter = new HBlobToolFilter();
        }

        public List<KeyValuePair<int, HBlob.Blob>> Run(HMat hMat)
        {
            blobTool.Label(hMat.Mat);
            List<KeyValuePair<int, CvBlob>> allBlobList = blobTool.ToList();
            //검사 결과
            List<KeyValuePair<int, HBlob.Blob>> result = new List<KeyValuePair<int, Blob>>();

            if (allBlobList.Count == 0)
            {
                return result;
            }

            //블랍 합치기 진행
            if(MergeBlob)
            {
                //이곳에 필터링된 결과가 저장됨
                List<CvBlob> findedList = new List<CvBlob>();
                List<CvBlob> blobList = allBlobList.Select(x => x.Value).ToList();

                //가장 큰 블랍으로 첫 기준 블랍 찾기
                int maxIndex = -1;
                int maxSize = 0;
                CvBlob maxBlob = null;

                for (int i = 0; i < allBlobList.Count; i++)
                {
                    if (maxSize < allBlobList[i].Value.Area)
                    {
                        maxSize = allBlobList[i].Value.Area;
                        maxIndex = i;
                        maxBlob = allBlobList[i].Value;
                    }
                }

                findedList.Add(maxBlob);
                blobList.Remove(maxBlob);

                //인접 블랍을 찾아서 붙이기
                bool isFinded = false;
                int combineRange = MergeThreshold;

                while (findedList.Count < allBlobList.Count)
                {
                    isFinded = false;
                    for (int i = 0; i < blobList.Count; i++)
                    {
                        CvBlob currentBlob = blobList[i];

                        for (int j = 0; j < findedList.Count; j++)
                        {
                            CvBlob pibotBlob = findedList[j];

                            if (
                                !(currentBlob.MinY - combineRange > pibotBlob.MaxY + combineRange) &&
                                !(currentBlob.MaxY + combineRange < pibotBlob.MinY - combineRange)
                                )
                            {
                                if (
                                    (currentBlob.MaxX + combineRange <= pibotBlob.MinX - combineRange && currentBlob.MinX - combineRange >= pibotBlob.MinX + combineRange) ||
                                    (currentBlob.MinX - combineRange < pibotBlob.MaxX + combineRange && currentBlob.MaxX + combineRange > pibotBlob.MinX - combineRange)
                                    )
                                {
                                    findedList.Add(currentBlob);
                                    blobList.Remove(currentBlob);
                                    i--;
                                    isFinded = true;

                                    break;
                                }
                                else
                                {

                                }
                            }
                        }
                    }

                    if (!isFinded)
                    {
                        break;
                    }
                }

                allBlobList.Clear();
                for(int i = 0; i < findedList.Count; i++)
                {
                    allBlobList.Add(new KeyValuePair<int, CvBlob>(i, findedList[i]));
                }
            }

            //Area 크기 필터링하여 제거
            for (int i = 0; i < allBlobList.Count; i++)
            {
                if (allBlobList[i].Value != null)
                {
                    int area = allBlobList[i].Value.Area;
                    if (area < Filter.MinArea || area > Filter.MaxArea)
                    {
                        allBlobList.RemoveAt(i);
                        i--;
                    }
                }
            }

            allBlobList.ForEach(x =>
            {
                result.Add(new KeyValuePair<int, Blob>(x.Key, new Blob(x.Value)));
            });

            Blobs = result;

            return result;
        }

        public HPoint[][] FindContours(HMat hMat)
        {
            OpenCvSharp.Point[][] contours;
            OpenCvSharp.HierarchyIndex[] hierarchies;
            Cv2.FindContours(hMat.Mat, out contours, out hierarchies, RetrievalModes.CComp, ContourApproximationModes.ApproxNone);

            List<HPoint[]> result = new List<HPoint[]>();
            contours.ToList().ForEach(x => {
                HPoint[] points = new HPoint[x.Count()];
                for(int i = 0; i < x.Count(); i++)
                {
                    points[i] = new HPoint(x[i]);
                }

                result.Add(points);
            });

            return result.ToArray();
        }

        public HPoint[] ApproxPolyDP(HPoint[] contours, double threshold)
        {
            Point[] openCvContours = contours.Select(x => x.point).ToArray();

            List<HPoint> result = new List<HPoint>();
            Cv2.ApproxPolyDP(openCvContours, Cv2.ArcLength(openCvContours, true) * threshold, true).ToList().ForEach(x => {
                result.Add(new HPoint(x));
            });

            return result.ToArray();
        }

        public double ContourArea(HPoint[] approx)
        {
            Point[] openCvApprox = approx.Select(x => x.point).ToArray();
            return Cv2.ContourArea(openCvApprox);
        }

        public class Blob
        {
            CvBlob cvBlob;

            public int Area { get { return cvBlob.Area; } }
            public double Angle { get { return cvBlob.Angle(); } }
            public int MaxX { get { return cvBlob.MaxX; } }
            public int MinX { get { return cvBlob.MinX; } }
            public int MaxY { get { return cvBlob.MaxY; } }
            public int MinY { get { return cvBlob.MinY; } }
            public int CenterX { get { return (MaxX - MinX) / 2 + MinX; } }
            public int CenterY { get { return (MaxY - MinY) / 2 + MinY; } }

            public Blob(int area, int maxX, int minX, int maxY, int minY)
            {
                cvBlob = new CvBlob();

                cvBlob.Area = area;
                cvBlob.MaxX = maxX;
                cvBlob.MinX = minX;
                cvBlob.MaxY = maxY;
                cvBlob.MinY = minY;
            }

            public Blob(CvBlob cvBlob)
            {
                this.cvBlob = cvBlob;
            }
        }

        public class HBlobToolFilter
        {
            public int MinArea { get { return minArea; }set { minArea = value; } }
            private int minArea = 0;
            public int MaxArea { get { return maxArea; }set { maxArea = value; } }
            private int maxArea = int.MaxValue;
        }
    }
}
