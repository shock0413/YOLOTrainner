using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Alturos.Yolo.Model;

namespace HsrAITrainner
{
    public class StructDetectResult
    {
        public int Position { get; set; }
        public string Model { get; set; }
        public string FileName { get; set; }
        public int TotalLabelCount { get; set; }
        public int HoleLabelCount { get; set; }
        public int BlankLabelCount { get; set; }
        public int PlugLabelCount { get; set; }
        public int DetectCount { get; set; }
        public int MatchCount { get; set; }
        public List<YoloItem> Items { get; set; }
        public int BlankCount { get; set; }
        public int HoleCount { get; set; }
        public int PlugCount { get; set; }

        public bool IsLabelError { get; set; }
        public bool IsMatchError { get; set; }
        public string ErrorString { get; set; }
    }
}
