using Alturos.Yolo;
using Alturos.Yolo.Model;
using HanseroDisplay;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Rect = System.Windows.Rect;
using Utill;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Controls;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Controls.Primitives;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;
using System.Xml;
using System.Net;
using System.Net.Sockets;
using HanseroDisplay.Struct;
using Ionic.Zip;
using Cudafy;
using Cudafy.Host;
using Cudafy.Translator;
using AsyncFrameSocket;

namespace HsrAITrainner
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        private AsyncSocketServer server = null;
        private int port = 9962;
        private IniFile config = new IniFile("config.ini");
        private IniFile m_DetectConfig = new IniFile(AppDomain.CurrentDomain.BaseDirectory + "Detector\\Config.ini");
        private IniFile projectConfig = null;
        private bool isRunning = true;
        private bool isTrainning = false;
        private int m_DetectImageWidth = 0;
        private int m_DetectImageHeight = 0;
        private bool m_ChannelsError = false;
        private string m_WeightsFile = "";
        // private ProgressDialogController m_TrainController;
        private ImageInfo m_PreSaveImageInfo;
        private bool m_IsSaved = false;
        private bool m_LoadThreadFinished = false;

        private Visibility m_SaveBtnVisibility = Visibility.Hidden;
        public Visibility SaveBtnVisibility { get { return m_SaveBtnVisibility; } set { m_SaveBtnVisibility = value; NotifyPropertyChanged("SaveBtnVisibility"); } }

        private Visibility m_PreviewModeVisibility = Visibility.Visible;
        public Visibility PreviewModeVisibility { get { return m_PreviewModeVisibility; } set { m_PreviewModeVisibility = value; NotifyPropertyChanged("PreviewModeVisibility"); } }

        private Visibility m_ListModeVisibility = Visibility.Collapsed;
        public Visibility ListviewModeVisibility { get { return m_ListModeVisibility; } set { m_ListModeVisibility = value; NotifyPropertyChanged("ListviewModeVisibility"); } }

        private double m_Gamma = 1.0;
        private string m_FileName = "";

        public int DecodePixelWidth { get { return m_DecodePixelWidth; } set { m_DecodePixelWidth = value; NotifyPropertyChanged("DecodePixelWidth"); } }
        private int m_DecodePixelWidth = 160;
        public int DecodePixelHeight { get { return m_DecodePixelHeight; } set { m_DecodePixelHeight = value; NotifyPropertyChanged("DecodePixelHeight"); } }
        private int m_DecodePixelHeight = 120;

        private int m_CntPerPage { get { return config.GetInt32("INFO", "CntPerPage", 20); } }
        private int m_PagePerGroup { get { return config.GetInt32("INFO", "PagePerGroup", 10); } }
        private bool m_IsImageListLoadPaused = false;
        private int m_StartPage = 0;
        private int m_EndPage = 10;
        private Process m_Process;
        private List<string> m_Process_Recv = new List<string>();
        private ImageInfo m_OldSelectedImageInfo;

        public int LimitPage { get { return m_LimitPage; } set { m_LimitPage = value; NotifyPropertyChanged("LimitPage"); } }
        private int m_LimitPage = 0;
        private StackPanel m_PageStack;
        private TextBlock m_OldPageTextBlock;

        public int SelectedPage { get { return m_SelectedPage; } set { m_SelectedPage = value; NotifyPropertyChanged("SelectedPage"); } }
        private int m_SelectedPage;

        private bool m_IsCtrlPressed = false;
        // private bool m_IsAltPressed = false;
        private bool m_IsShiftPressed = false;
        private bool m_IsLeftMouseBtnPressed = false;
        private bool m_IsMouseDragged = false;
        private System.Drawing.Point m_DragRectangleStartPoint;
        private System.Drawing.Point m_DragRectangleEndPoint;
        private ImageInfo m_SelectedImageInfo;
        private ScrollViewer m_ScrollViewer;
        private int m_Total_Img_Cnt = 0;
        private bool m_lbImageList_MouseLeftButtonDown = false;

        private HRectangle m_CopyRectangle = null;

        public bool IsOpenAllImageDetectResultEnabled { get { return m_IsOpenAllImageDetectResultEnabled; } set { m_IsOpenAllImageDetectResultEnabled = value; NotifyPropertyChanged("IsOpenAllImageDetectResultEnabled"); } }
        private bool m_IsOpenAllImageDetectResultEnabled = false;

        public Visibility AllImageDetectLoading { get { return m_AllImageDetectLoading; } set { m_AllImageDetectLoading = value; NotifyPropertyChanged("AllImageDetectLoading"); } }
        private Visibility m_AllImageDetectLoading = Visibility.Hidden;

        public Visibility AllImageDetectText { get { return m_AllImageDetectText; } set { m_AllImageDetectText = value; NotifyPropertyChanged("AllImageDetectText"); } }
        private Visibility m_AllImageDetectText = Visibility.Visible;

        public bool IsOpenAllTestResultEnabled { get { return m_IsOpenAllTestResultEnabled; } set { m_IsOpenAllTestResultEnabled = value; NotifyPropertyChanged("IsOpenAllTestResultEnabled"); } }
        private bool m_IsOpenAllTestResultEnabled = false;

        public Visibility AllTestLoading { get { return m_AllTestLoading; } set { m_AllTestLoading = value; NotifyPropertyChanged("AllTestLoading"); } }
        private Visibility m_AllTestLoading = Visibility.Hidden;

        public Visibility AllTestText { get { return m_AllTestText; } set { m_AllTestText = value; NotifyPropertyChanged("AllTestText"); } }
        private Visibility m_AllTestText = Visibility.Visible;

        public event PropertyChangedEventHandler PropertyChanged;

        public string YoloVersionStr
        {
            get
            {
                if (projectConfig != null)
                {
                    return projectConfig.GetString("Info", "Yolo Version", "v4");
                }
                else
                {
                    return "v4";
                }
            }
            set
            {
                projectConfig.WriteValue("Info", "Yolo Version", value);
                NotifyPropertyChanged("YoloVersionStr");
            }
        }

        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private BackgroundWorker loadingThread;

        public ObservableCollection<StructResult> LabelList { get { return m_LabelList; } set { m_LabelList = value; NotifyPropertyChanged("LabelList"); } }
        private ObservableCollection<StructResult> m_LabelList = new ObservableCollection<StructResult>();

        public int m_ShortMoveX { get { return config.GetInt32("Move", "ShortMoveX", 1); } }
        public int m_ShortMoveY { get { return config.GetInt32("Move", "ShortMoveY", 1); } }
        public int m_LongMoveX { get { return config.GetInt32("Move", "LongMoveX", 50); } }
        public int m_LongMoveY { get { return config.GetInt32("Move", "LongMoveX", 50); } }

        FileStream m_ProjectStream;

        private List<string> SelectedFileLIst = new List<string>();
        private List<bool> IsFindList = new List<bool>();

        private string m_ProjectDefaultPath;
        private string m_ProjectPath;
        private string m_ProjectName;
        private string m_ProjectImagePath;
        private bool m_IsTrained;
        private int m_SelectedImageIndex;
        private int m_SelectedCharIndex;
        private int m_DefaultWidth = 0;
        private int m_DefaultHeight = 0;

        public int Score { get { return m_DetectConfig.GetInt32("Info", "Score", 0); } set { m_DetectConfig.WriteValue("Info", "Score", value); NotifyPropertyChanged("Score"); } }
        private CharInfo m_TempCharInfo;
        private bool m_IsTempCharInfo = false;
        private ImageInfo m_TempImageinfo = new ImageInfo("");

        private string m_SearchFilter;

        private Int32Rect mcurrentROI = new Int32Rect(140, 160, 1000, 300);
        private bool m_IsFullROI = true;

        public long a;

        public long time
        {
            get { return a; }
            set { a = value; NotifyPropertyChanged("time"); }
        }

        public enum ImageShowType { All, Labeled, Not_Labeled, Trained, Finded, Miss_Match, Search }
        public enum CharType { Finded = 0, Labeled = 1, Both = 2, None = 3 }

        // TRAINED는 LABELED와 같은 의미를 가짐.
        public enum TrainType { NONE = 0, LABELED = 1, TRAINED = 2, NAVER = 3 }

        public struct CharInfo
        {
            public string Label_FileName;
            public bool Is_Labeled;
            public string Labeled_Name;
            public bool Is_Finded;
            public string Finded_Name;
            public CharType type;
            //public CroppedBitmap Labeledcrbmp;
            //public CroppedBitmap Findedcrbmp;
            public double Labeled_X;
            public double Labeled_Y;
            public double Labeled_Width;
            public double Labeled_Height;
            public double Finded_X;
            public double Finded_Y;
            public double Finded_Width;
            public double Finded_Height;
            public double Finded_Score;

            public CharInfo(string name)
            {
                Label_FileName = "";
                Is_Labeled = false;
                Labeled_Name = "";
                Is_Finded = false;
                Finded_Name = "";
                type = CharType.None;
                //Labeledcrbmp = null;
                //Findedcrbmp = null;
                Labeled_X = 0;
                Labeled_Y = 0;
                Labeled_Width = 0;
                Labeled_Height = 0;
                Finded_X = 0;
                Finded_Y = 0;
                Finded_Width = 0;
                Finded_Height = 0;
                Finded_Score = 0;
            }
        }

        public struct ImageInfo
        {
            public string FileName { get; set; }
            public string Thumnail_FileName { get; set; }
            public string DateTime { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public List<CharInfo> CharList { get; set; }
            public string Find_String { get; set; }
            public bool IsVisble { get; set; }
            public TrainType TrainType { get; set; }

            public ImageInfo(string path)
            {
                string[] _s = path.Split('\\');

                FileName = path;
                Thumnail_FileName = _s[_s.Length - 1].Trim().ToString();

                if (FileName != null && FileName != "")
                {
                    DateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    DateTime = "";
                }
                
                Find_String = "";
                Width = 640;
                Height = 480;
                CharList = new List<CharInfo>();
                IsVisble = false;
                TrainType = TrainType.NAVER;
            }
        }

        // 로드한 이미지 리스트
        public ObservableCollection<ImageInfo> ImageInfoList { get { return m_ImageInfoList; } set { m_ImageInfoList = value; NotifyPropertyChanged("ImageInfoList"); } }
        private ObservableCollection<ImageInfo> m_ImageInfoList = new ObservableCollection<ImageInfo>();
        // 나눈 페이지에서 선택한 페이지의 이미지 리스트
        public ObservableCollection<ImageInfo> PartImageInfoList { get { return partImageInfoList; } set { partImageInfoList = value; NotifyPropertyChanged("PartImageInfoList"); } }
        private ObservableCollection<ImageInfo> partImageInfoList = new ObservableCollection<ImageInfo>();
        // 테스트 이미지 리스트
        private ObservableCollection<ImageInfo> m_TestImageInfoList = new ObservableCollection<ImageInfo>();
        // 레이블링 된 Item 리스트
        //private List<CharInfo> m_LabeledList = new List<CharInfo>();

        private IniFile cfgFile = null;

        public int NetBatch
        {
            get
            {
                if (cfgFile != null)
                {
                    return cfgFile.GetInt32("net", "batch", 32);
                }
                else
                {
                    return 32;
                }
            }
            set
            {
                cfgFile.WriteValue("net", "batch", value);
                NotifyPropertyChanged("NetBatch");
            }
        }

        public int NetSubdivisions
        {
            get
            {
                if (cfgFile != null)
                {
                    return cfgFile.GetInt32("net", "subdivisions", 16);
                }
                else
                {
                    return 16;
                }
            }
            set
            {
                cfgFile.WriteValue("net", "subdivisions", value);
                NotifyPropertyChanged("NetSubdivisions");
            }
        }

        public int NetWidth
        {
            get
            {
                if (cfgFile != null)
                {
                    return cfgFile.GetInt32("net", "width", 416);
                }
                else
                {
                    return 416;
                }
            }
            set
            {
                cfgFile.WriteValue("net", "width", value);
                NotifyPropertyChanged("NetWidth");
            }
        }

        public int NetHeight
        {
            get
            {
                if (cfgFile != null)
                {
                    return cfgFile.GetInt32("net", "height", 416);
                }
                else
                {
                    return 416;
                }
            }
            set
            {
                cfgFile.WriteValue("net", "height", value);
                NotifyPropertyChanged("NetHeight");
            }
        }

        public string NetSize
        {
            get
            {
                return NetWidth + "x" + NetHeight;
            }
            set
            {
                netSize = value;

                string[] spt = value.Split('x');
                NetWidth = Convert.ToInt32(spt[0]);
                NetHeight = Convert.ToInt32(spt[1]);

                NotifyPropertyChanged("NetSize");
            }
        }
        private string netSize;

        public int NetChannels
        {
            get
            {
                if (cfgFile != null)
                {
                    return cfgFile.GetInt32("net", "channels", 3);
                }
                else
                {
                    return 3;
                }
            }
            set
            {
                cfgFile.WriteValue("net", "channels", value);
                NotifyPropertyChanged("NetChannels");
                IsProjectParamsChanged = true;
            }
        }

        public double NetMomentum
        {
            get
            {
                if (cfgFile != null)
                {
                    return cfgFile.GetDouble("net", "momentum", 0.949);
                }
                else
                {
                    return 0.949;
                }
            }
            set
            {
                cfgFile.WriteValue("net", "momentum", value);
                NotifyPropertyChanged("NetMomentum");
                IsProjectParamsChanged = true;

            }
        }

        public double NetDecay
        {
            get
            {
                if (cfgFile != null)
                {
                    return cfgFile.GetDouble("net", "decay", 0.0005);
                }
                else
                {
                    return 0.0005;
                }
            }
            set
            {
                cfgFile.WriteValue("net", "decay", value);
                NotifyPropertyChanged("NetDecay");
                IsProjectParamsChanged = true;

            }
        }

        public int NetAngle
        {
            get
            {
                if (cfgFile != null)
                {
                    return cfgFile.GetInt32("net", "angle", 0);
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                cfgFile.WriteValue("net", "angle", value);
                NotifyPropertyChanged("NetAngle");
                IsProjectParamsChanged = true;
            }
        }

        public double NetSaturation
        {
            get
            {
                if (cfgFile != null)
                {
                    return cfgFile.GetDouble("net", "saturation", 1.5);
                }
                else
                {
                    return 1.5;
                }
            }
            set
            {
                cfgFile.WriteValue("net", "saturation", value);
                NotifyPropertyChanged("NetSaturation");
                IsProjectParamsChanged = true;
            }
        }

        public double NetExposure
        {
            get
            {
                if (cfgFile != null)
                {
                    return cfgFile.GetDouble("net", "exposure", 1.5);
                }
                else
                {
                    return 1.5;
                }
            }
            set
            {
                cfgFile.WriteValue("net", "exposure", value);
                NotifyPropertyChanged("NetExposure");
                IsProjectParamsChanged = true;
            }
        }

        public double NetHue
        {
            get
            {
                if (cfgFile != null)
                {
                    return cfgFile.GetDouble("net", "hue", .1);
                }
                else
                {
                    return .1;
                }
            }
            set
            {
                cfgFile.WriteValue("net", "hue", value);
                NotifyPropertyChanged("NetHue");
                IsProjectParamsChanged = true;
            }
        }

        public double NetLearningRate
        {
            get
            {
                if (cfgFile != null)
                {
                    return cfgFile.GetDouble("net", "learning_rate", 0.001);
                }
                else
                {
                    return 0.001;
                }
            }
            set
            {
                cfgFile.WriteValue("net", "learning_rate", value);
                NotifyPropertyChanged("NetLearningRate");
                IsProjectParamsChanged = true;
            }
        }

        public int NetBurnIn
        {
            get
            {
                if (cfgFile != null)
                {
                    return cfgFile.GetInt32("net", "burn_in", 1000);
                }
                else
                {
                    return 1000;
                }
            }
            set
            {
                cfgFile.WriteValue("net", "burn_in", value);
                NotifyPropertyChanged("NetBurnIn");
                IsProjectParamsChanged = true;
            }
        }

        public int NetMaxBatches
        {
            get
            {
                if (cfgFile != null)
                {
                    return cfgFile.GetInt32("net", "max_batches", 10000);
                }
                else
                {
                    return 10000;
                }
            }
            set
            {
                cfgFile.WriteValue("net", "max_batches", value);
                NotifyPropertyChanged("NetMaxBatches");

                NetSteps = (NetMaxBatches * 0.8) + "," + (NetMaxBatches * 0.9);
                NotifyPropertyChanged("NetSteps");

                IsProjectParamsChanged = true;
            }
        }

        public string NetSteps
        {
            get
            {
                if (cfgFile != null)
                {
                    string value = cfgFile.GetString("net", "steps", (NetMaxBatches * 0.8).ToString() + "," + (NetMaxBatches * 0.9).ToString());
                    return value;
                }
                else
                {
                    string value = (NetMaxBatches * 0.8).ToString() + "," + (NetMaxBatches * 0.9).ToString();
                    return value;
                }
            }
            set
            {
                cfgFile.WriteValue("net", "steps", value);
                NotifyPropertyChanged("NetSteps");
                IsProjectParamsChanged = true;
            }
        }

        public string NetScales
        {
            get
            {
                if (cfgFile != null)
                {
                    return cfgFile.GetString("net", "scales", ".1,.1");
                }
                else
                {
                    return ".1,.1";
                }
            }
            set
            {
                cfgFile.WriteValue("net", "scales", value);
                NotifyPropertyChanged("NetScales");
                IsProjectParamsChanged = true;
            }
        }

        public bool IsProjectParamsChanged
        {
            get
            {
                return isProjectParamsChanged;
            }
            set
            {
                isProjectParamsChanged = value;
            }
        }
        private bool isProjectParamsChanged = false;

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            LogManager.Action("프로그램 실행");

            Init();

            dg.ItemsSource = m_LabelList;

            m_LabelList.CollectionChanged += ResultListcollectionChanged;
            dp.canvas.OnMenuItem_Label_Click += Apply_Labeling;

            new Thread(new ThreadStart(() =>
            {
                while (isRunning)
                {
                    DateTime dateTime = DateTime.Now;

                    string reserveStartTimeStr = config.GetString("Train", "ReserveStartTime", "01:00");
                    string[] reserveStartTimeSpt = reserveStartTimeStr.Split(':');
                    string reserveEndTimeStr = config.GetString("Train", "ReserveEndTime", "06:00");
                    string[] reserveEndTimeSpt = reserveEndTimeStr.Split(':');

                    int curHour = dateTime.Hour;
                    int curMinute = dateTime.Minute;

                    int reserveStartHour = Convert.ToInt32(reserveStartTimeSpt[0]);
                    int reserveStartMinute = Convert.ToInt32(reserveStartTimeSpt[1]);
                    int curTotalMin = curHour * 60 + curMinute;
                    int reserveStartTotalMin = reserveStartHour * 60 + reserveStartMinute;

                    int reserveEndHour = Convert.ToInt32(reserveEndTimeSpt[0]);
                    int reserveEndMinute = Convert.ToInt32(reserveEndTimeSpt[1]);
                    int reserveEndTotalMin = reserveEndHour * 60 + reserveEndMinute;

                    bool enableReserveEnd = config.GetBoolian("Train", "EnableReserveEnd", false);
                    bool enableReserveDate = config.GetBoolian("Train", "EnableReserveDate", false);

                    DateTime reserveStartDateTime = DateTime.Parse(config.GetString("Train", "ReserveStartDate", "2021-12-02"));
                    DateTime reserveEndDateTime = DateTime.Parse(config.GetString("Train", "ReserveEndDate", "2021-12-02"));

                    if (curTotalMin == reserveStartTotalMin && !isTrainning)
                    {
                        if (enableReserveDate)
                        {
                            if (dateTime.Year == reserveStartDateTime.Year && dateTime.Month == reserveStartDateTime.Month && dateTime.Day == reserveStartDateTime.Day)
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    LogManager.Action("예약 학습 시작 (시작 시간 : " + reserveStartDateTime.Year + "-" + reserveStartDateTime.Month + "-" + reserveStartDateTime.Day + " " + reserveStartHour + ":" + reserveStartMinute + ", 날짜 사용 여부 : ON)");

                                    // 예약 학습 시작
                                    Button_Click_1(null, null);
                                });
                            }
                        }
                        else
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                LogManager.Action("예약 학습 시작 (시작 시간 : " + reserveStartDateTime.Year + "-" + reserveStartDateTime.Month + "-" + reserveStartDateTime.Day + " " + reserveStartHour + ":" + reserveStartMinute + ", 날짜 사용 여부 : OFF)");

                                // 예약 학습 시작
                                Button_Click_1(null, null);
                            });
                        }
                    }

                    if (curTotalMin == reserveEndTotalMin && isTrainning && enableReserveEnd)
                    {
                        string h_str = reserveEndHour.ToString();
                        string m_str = reserveEndMinute.ToString();

                        if (reserveEndHour < 10)
                        {
                            h_str = "0" + h_str;
                        }

                        if (reserveEndMinute < 10)
                        {
                            m_str = "0" + m_str;
                        }

                        if (enableReserveDate)
                        {
                            if (dateTime.Year == reserveEndDateTime.Year && dateTime.Month == reserveEndDateTime.Month && dateTime.Day == reserveEndDateTime.Day)
                            {
                                LogManager.Action("예약 학습 종료 (종료 시간 : " + reserveEndDateTime.Year + "-" + reserveEndDateTime.Month + "-" + reserveEndDateTime.Day + " " + h_str + ":" + m_str + ", 날짜 사용 여부 : ON)");

                                CloseReserveTrain();
                            }
                        }
                        else
                        {
                            LogManager.Action("예약 학습 종료 (종료 시간 : " + reserveEndDateTime.Year + "-" + reserveEndDateTime.Month + "-" + reserveEndDateTime.Day + " " + h_str + ":" + m_str + ", 날짜 사용 여부 : OFF)");

                            CloseReserveTrain();
                        }
                    }

                    Thread.Sleep(1000);
                }
            })).Start();

            dp.canvas.LeftRotation90 = LeftRotation90_Command;
            dp.canvas.RightRotation90 = RightRotation90_Command;

            if (server == null)
            {
                server = new AsyncSocketServer(port);
                server.OnConnet += new AsyncSocketConnectEventHandler(OnConnect);
                server.OnAccept += new AsyncSocketAcceptEventHandler(OnAccept);
                server.OnClose += new AsyncSocketCloseEventHandler(OnClose);
                server.OnError += new AsyncSocketErrorEventHandler(OnError);
                server.Listen(IPAddress.Parse("127.0.0.1"));
            }
        }

        private AsyncSocketClient client = null;
        private int client_id = 0;

        private void OnAccept(object sender, AsyncSocketAcceptEventArgs e)
        {
            if (client != null)
            {
                client.Close();
                client = null;
            }

            client = new AsyncSocketClient(client_id++, e.Worker);
            client.OnConnet += new AsyncSocketConnectEventHandler(OnConnect);
            client.OnClose += new AsyncSocketCloseEventHandler(OnClose);
            client.OnError += new AsyncSocketErrorEventHandler(OnError);
            client.OnSend += new AsyncSocketSendEventHandler(OnSend);
            client.OnReceive += new AsyncSocketReceiveEventHandler(OnReceive);
            client.Receive();

            string sendStr = "PARAMS," + m_ProjectPath + "\\" + m_ProjectName + ".cfg," + m_WeightsFile + "," +
                    m_ProjectPath + "\\" + m_ProjectName + ".names," + Convert.ToString(NetWidth) + "," +
                    Convert.ToString(NetHeight) + "," + Convert.ToString(NetChannels);

            byte[] buf = Encoding.Default.GetBytes(sendStr);
            int len = buf.Length + 4;
            byte[] sendBytes = new byte[len];

            Buffer.BlockCopy(BitConverter.GetBytes(len - 4), 0, sendBytes, 0, 4);
            Buffer.BlockCopy(buf, 0, sendBytes, 4, buf.Length);

            client.Send(sendBytes);

            Console.WriteLine("OnAccept");
        }

        private void OnConnect(object sender, AsyncSocketConnectionEventArgs e)
        {
            Console.WriteLine("OnConnect");
        }

        private void OnReceive(object sender, AsyncSocketReceiveEventArgs e)
        {
            byte[] receiveData = e.ReceiveData;

            byte[] bitConvert = new byte[4] { receiveData[0], receiveData[1], receiveData[2], receiveData[3] };

            int len = BitConverter.ToInt32(bitConvert, 0);

            Console.WriteLine("받은 실제 데이터 길이 : " + len);

            byte[] data = new byte[len];

            Buffer.BlockCopy(receiveData, 4, data, 0, len);

            string recvStr = Encoding.Default.GetString(data);

            Console.WriteLine("OnReceive : " + recvStr);

            if (recvStr.StartsWith("Echo,"))
            {

            }
            else
            {
                m_Process_Recv.Add(recvStr);
            }
        }

        private void OnSend(object sender, AsyncSocketSendEventArgs e)
        {
            Console.WriteLine("OnSend");
        }

        private void OnClose(object sender, AsyncSocketConnectionEventArgs e)
        {
            Console.WriteLine("OnClose");
        }

        private void OnError(object sender, AsyncSocketErrorEventArgs e)
        {
            Console.WriteLine("OnError : " + e.AsyncSocketException.Message);
        }

        private void CloseReserveTrain()
        {
            isTrainning = false;

            Dispatcher.Invoke(() =>
            {
                if (p != null)
                {
                    Capture capture = new Capture();
                    System.Drawing.Bitmap b = capture.CaptureApplication("darknet", m_ProjectName);

                    if (!Directory.Exists(m_ProjectPath + "\\학습그래프"))
                    {
                        Directory.CreateDirectory(m_ProjectPath + "\\학습그래프");
                    }

                    if (!Directory.Exists(m_ProjectPath + "\\학습그래프\\" + DateTime.Now.ToString("yyyyMMdd")))
                    {
                        Directory.CreateDirectory(m_ProjectPath + "\\학습그래프\\" + DateTime.Now.ToString("yyyyMMdd"));
                    }

                    b.Save(m_ProjectPath + "\\학습그래프\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + DateTime.Now.ToString("HHmmss") + ".png");

                    p.Kill();
                    p.Close();
                    p.Dispose();
                    p = null;

                    if (Directory.Exists(m_ProjectPath + "\\temp"))
                    {
                        try
                        {
                            Directory.Delete(m_ProjectPath + "\\temp", true);
                        }
                        catch
                        {

                        }
                    }
                }
            });
        }

        private void InitBW()
        {
             
            loadingThread = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            loadingThread.DoWork += Thread_DoWork;
            loadingThread.ProgressChanged += Thread_ProgressChanged;
            loadingThread.RunWorkerCompleted += Thread_RunWorkerCompleted;
        }

        private void Thread_DoWork(object sender, DoWorkEventArgs e)
        {

        }

        private void Thread_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int per = e.ProgressPercentage;
        }

        private void Thread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {

            }
            else if (e.Error != null)
            {

            }
            else
            {

            }
        }

        private void Apply_Labeling(object sender, RoutedEventArgs e)
        {

        }

        private void Labelchanged(object sender, KeyEventArgs e)
        {
            dp.SelectRectangle.LabeledCharName = e.Key.ToString();
        }

        private void ResultListcollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("resultList");
        }

        public void Init()
        {
            LogManager.Action("Init() 실행");

            InitBW();
            Load_Project();
        }

        //Display완료 버튼 선택시
        private ICommand saved;
        public ICommand SaveCommand
        {
            get { return (this.saved) ?? (this.saved = new DelegateCommand(Save)); }
        }

        private ICommand confirm;

        public ICommand ConfirmCommand
        {
            get { return (this.confirm) ?? (this.confirm = new DelegateCommand(Confirm)); }
        }

        private ICommand cancel;
        public ICommand CancelCommand
        {
            get { return (this.cancel) ?? (this.cancel = new DelegateCommand(Cancel)); }
        }

        private ICommand delete;
        public ICommand DeleteCommand
        {
            get { return (this.delete) ?? (this.delete = new DelegateCommand(Delete)); }
        }

        public void Save()
        {
            if (m_IsSaved)
            {
                return;
            }

            m_IsSaved = true;
            SaveBtnVisibility = Visibility.Hidden;

            int index = lbImageList.SelectedIndex;

            if (PreviewModeVisibility == Visibility.Visible)
            {
                if (lbImageList.SelectedIndex > 0)
                {
                    index = lbImageList.SelectedIndex;
                }
            }

            if (ListviewModeVisibility == Visibility.Visible)
            {
                if (dgImageList.SelectedIndex > 0)
                {
                    index = dgImageList.SelectedIndex;
                }
            }

            ImageInfo imageInfo = m_SelectedImageInfo;

            if (imageInfo.CharList.Count > 0)
            {
                if (imageInfo.TrainType != TrainType.LABELED)
                {
                    string filename = Path.GetFileName(imageInfo.FileName);
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(imageInfo.FileName);
                    string trainType = imageInfo.TrainType.ToString();

                    if (trainType == "NONE")
                    {
                        trainType = "NotLabeled";
                    }
                    else if (trainType == "LABELED")
                    {
                        trainType = "Labeled";
                    }
                    else if (trainType == "TRAINED")
                    {
                        trainType = "Trained";
                    }

                    if (!Directory.Exists(m_ProjectPath + "\\Images\\Labeled"))
                    {
                        Directory.CreateDirectory(m_ProjectPath + "\\Images\\Labeled");
                    }

                    if (File.Exists(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename))
                    {
                        File.Copy(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename, m_ProjectPath + "\\Images\\Labeled\\" + filename, true);
                        File.Delete(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename);
                    }

                    if (File.Exists(m_ProjectPath + "\\Images\\" + trainType + "\\" + fileNameWithoutExtension + ".hsr"))
                    {
                        if (!File.Exists(m_ProjectPath + "\\Images\\Labeled\\" + fileNameWithoutExtension + ".hsr"))
                        {
                            File.Copy(m_ProjectPath + "\\Images\\" + trainType + "\\" + fileNameWithoutExtension + ".hsr", m_ProjectPath + "\\Images\\Labeled\\" + fileNameWithoutExtension + ".hsr", true);
                        }

                        File.Delete(m_ProjectPath + "\\Images\\" + trainType + "\\" + fileNameWithoutExtension + ".hsr");
                    }

                    imageInfo.FileName = m_ProjectPath + "\\Images\\Labeled\\" + filename;

                    imageInfo.TrainType = TrainType.LABELED;
                }
            }

            if (imageInfo.CharList.Count == 0)
            {
                if (imageInfo.TrainType != TrainType.NONE)
                {
                    string filename = Path.GetFileName(imageInfo.FileName);
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(imageInfo.FileName);
                    string trainType = imageInfo.TrainType.ToString();

                    if (trainType == "NONE")
                    {
                        trainType = "NotLabeled";
                    }
                    else if (trainType == "LABELED")
                    {
                        trainType = "Labeled";
                    }
                    else if (trainType == "TRAINED")
                    {
                        trainType = "Trained";
                    }

                    if (!Directory.Exists(m_ProjectPath + "\\Images\\NotLabeled"))
                    {
                        Directory.CreateDirectory(m_ProjectPath + "\\Images\\NotLabeled");
                    }

                    if (File.Exists(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename))
                    {
                        File.Copy(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename, m_ProjectPath + "\\Images\\NotLabeled\\" + filename, true);
                        File.Delete(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename);
                    }

                    if (File.Exists(m_ProjectPath + "\\Images\\" + trainType + "\\" + fileNameWithoutExtension + ".hsr"))
                    {
                        File.Copy(m_ProjectPath + "\\Images\\" + trainType + "\\" + fileNameWithoutExtension + ".hsr", m_ProjectPath + "\\Images\\NotLabeled\\" + fileNameWithoutExtension + ".hsr", true);
                        File.Delete(m_ProjectPath + "\\Images\\" + trainType + "\\" + fileNameWithoutExtension + ".hsr");
                    }

                    imageInfo.FileName = m_ProjectPath + "\\Images\\NotLabeled\\" + filename;
                }

                imageInfo.TrainType = TrainType.NONE;
            }

            if (m_IsOriginImageInfoList)
            {
                for (int i = 0; i < m_ImageInfoList_Temp.Count; i++)
                {
                    if (Path.GetFileName(m_ImageInfoList_Temp[i].FileName) == Path.GetFileName(imageInfo.FileName))
                    {
                        m_ImageInfoList_Temp[i] = imageInfo;
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < m_ImageInfoList.Count; i++)
                {
                    if (Path.GetFileName(m_ImageInfoList[i].FileName) == Path.GetFileName(imageInfo.FileName))
                    {
                        m_ImageInfoList[i] = imageInfo;
                        break;
                    }
                }
            }

            for (int i = 0; i < PartImageInfoList.Count; i++)
            {
                if (Path.GetFileName(PartImageInfoList[i].FileName) == Path.GetFileName(imageInfo.FileName))
                {
                    PartImageInfoList[i] = imageInfo;
                    break;
                }
            }

            // 파일 정보 그리드 내 라벨 수 갱신
            DependencyObject item = (DependencyObject)lbImageList.ItemContainerGenerator.ContainerFromIndex(index);

            List<DependencyObject> list = item.GetChildObjects(true).ToList();

            Grid grid = (Grid)list[0];

            for (int i = 0; i < grid.Children.Count; i++)
            {
                List<DependencyObject> _list = grid.Children[i].GetChildObjects(true).ToList();

                for (int l = 0; l < _list.Count; l++)
                {
                    if (_list[l].GetType() == typeof(ContentPresenter))
                    {
                        ContentPresenter cp = (ContentPresenter)_list[l];
                        List<DependencyObject> _l = cp.GetChildObjects(true).ToList();

                        for (int k = 0; k < _l.Count; k++)
                        {
                            if (_l[k].GetType() == typeof(Grid))
                            {
                                Grid _grid = (Grid)_l[k];

                                for (int j = 0; j < _grid.Children.Count; j++)
                                {
                                    if (_grid.Children[j].GetType() == typeof(StackPanel))
                                    {
                                        StackPanel sp = (StackPanel)_grid.Children[j];

                                        for (int o = 0; o < sp.Children.Count; o++)
                                        {
                                            if (sp.Children[o].GetType() == typeof(Grid))
                                            {
                                                Grid _g = (Grid)sp.Children[o];

                                                for (int p = 0; p < _g.Children.Count; p++)
                                                {
                                                    if (_g.Children[p].GetType() == typeof(TextBlock))
                                                    {
                                                        TextBlock tb = (TextBlock)_g.Children[p];

                                                        if (tb.Name == "CharList_Count")
                                                        {
                                                            tb.Text = imageInfo.CharList.Count.ToString();
                                                            break;
                                                        }
                                                    }
                                                }

                                                break;
                                            }
                                        }

                                        break;
                                    }
                                }

                                break;
                            }
                        }

                        break;
                    }

                    break;
                }
            }

            Refresh_ImageList(m_ImageShowType);

            DrawImageLabel();
            Save_NameFile();

            Save_ImageList_Information(imageInfo);

            Load_LabelList();

            RefreshcharInfo();

            // PageStackRefresh();

            dp.m_IsConfirm = true;

            lbImageList.SelectedIndex = index;

            // ImageList_MouseDoubleClick(null, null);

            this.Focus();

            dp.SelectRectangle = null;
        }

        public void Confirm()
        {
            if (dp.SelectRectangle == null)
            {
                return;
            }

            if (m_CopyRectangle != null)
            {
                m_CopyRectangle.MoveXValue = dp.SelectRectangle.MoveXValue;
                m_CopyRectangle.MoveYValue = dp.SelectRectangle.MoveYValue;
                m_CopyRectangle.LeftMoveValue = dp.SelectRectangle.LeftMoveValue;
                m_CopyRectangle.RightMoveValue = dp.SelectRectangle.RightMoveValue;
                m_CopyRectangle.TopMoveValue = dp.SelectRectangle.TopMoveValue;
                m_CopyRectangle.BottomMoveValue = dp.SelectRectangle.BottomMoveValue;
                m_CopyRectangle.originWidth = dp.SelectRectangle.originWidth;
                m_CopyRectangle.originHeight = dp.SelectRectangle.originHeight;
            }

            if (m_IsConfirm)
            {
                m_IsConfirm = false;
                return;
            }

            if (dp.SelectRectangle.LabeledCharName == "" || dp.SelectRectangle.LabeledCharName == "?")
            {
                MessageBox.Show("라벨명을 지정해주십시오.", "확인");
                m_IsConfirm = true;
                return;
            }

            ImageInfo imageInfo = m_SelectedImageInfo;

            m_IsSaved = false;
            SaveBtnVisibility = Visibility.Visible;

            int index = lbImageList.SelectedIndex;

            if (PreviewModeVisibility == Visibility.Visible)
            {
                index = lbImageList.SelectedIndex;
            }

            if (ListviewModeVisibility == Visibility.Visible)
            {
                index = dgImageList.SelectedIndex;
            }

            //선택형 사각형 위치 및 크기
            dp.ConfirmButtonVisibility = Visibility.Collapsed;
            dp.IsShowRectangle = false;
            dp.RemoveSelectRectangle();
            //MessageBox.Show("startX : " + startX + "\nstartY : " + startY + "\nwidth : " + width + "\nheight" + height);

            CharInfo labeledinfo = new CharInfo();

            //이미지 크롭후 저장
            labeledinfo = m_TempCharInfo;

            labeledinfo.Labeled_Name = dp.SelectRectangle.LabeledCharName;
            labeledinfo.Labeled_Width = dp.SelectRectangle.SelectedWidth / dp.BitmapImage.Width;
            labeledinfo.Labeled_Height = dp.SelectRectangle.SelectedHeight / dp.BitmapImage.Height;
            labeledinfo.Labeled_X = dp.SelectRectangle.StartX / dp.BitmapImage.Width + labeledinfo.Labeled_Width / 2;
            labeledinfo.Labeled_Y = dp.SelectRectangle.StartY / dp.BitmapImage.Height + labeledinfo.Labeled_Height / 2;

            m_DefaultWidth = (int)dp.SelectRectangle.SelectedWidth;
            m_DefaultHeight = (int)dp.SelectRectangle.SelectedHeight;

            //cb = null;

            labeledinfo.Is_Labeled = true;

            if (m_SelectedCharIndex >= 0)
            {
                labeledinfo.type = CharType.Labeled;
                labeledinfo.Is_Finded = false;
                labeledinfo.Finded_Name = "";
                labeledinfo.Finded_X = 0;
                labeledinfo.Finded_Y = 0;
                labeledinfo.Finded_Width = 0;
                labeledinfo.Finded_Height = 0;
                labeledinfo.Finded_Score = 0;

                imageInfo.CharList.Add(labeledinfo);

                if (Check_Label_Exist(labeledinfo.Labeled_Name) == false)
                {
                    m_LabelList.Add(new StructResult(labeledinfo.Labeled_Name));
                }
            }
            else
            {
                labeledinfo.Labeled_Name = "?";

                imageInfo.CharList.Add(labeledinfo);
            }

            DrawImageLabel();

            dp.m_IsConfirm = true;

            dp.SelectRectangle = null;
        }

        public void Cancel()
        {
            if (m_IsTempCharInfo)
            {
                m_SelectedImageInfo.CharList.Add(m_TempCharInfo);
                m_IsTempCharInfo = false;
            }

            DrawImageLabel();

            dp.RemoveSelectRectangle();

            dp.ConfirmButtonVisibility = Visibility.Hidden;

            dp.SelectRectangle = null;
        }

        public void Delete()
        {
            int index = lbImageList.SelectedIndex;

            if (PreviewModeVisibility == Visibility.Visible)
            {
                index = lbImageList.SelectedIndex;
            }

            if (ListviewModeVisibility == Visibility.Visible)
            {
                index = dgImageList.SelectedIndex;
            }

            m_IsTempCharInfo = false;

            ImageInfo imageInfo = m_SelectedImageInfo;

            m_IsSaved = false;
            SaveBtnVisibility = Visibility.Visible;

            int m_MatchCount = 0;

            if (Path.GetFileName(m_PreSaveImageInfo.FileName) == Path.GetFileName(imageInfo.FileName))
            {
                for (int i = 0; i < m_PreSaveImageInfo.CharList.Count; i++)
                {
                    for (int j = 0; j < imageInfo.CharList.Count; j++)
                    {
                        CharInfo c = m_PreSaveImageInfo.CharList[i];
                        CharInfo _c = imageInfo.CharList[j];

                        if (c.Finded_Height == _c.Finded_Height && c.Finded_Width == _c.Finded_Width && c.Finded_Name == _c.Finded_Name &&
                            c.Finded_Score == _c.Finded_Score && c.Finded_X == _c.Finded_X && c.Finded_Y == _c.Finded_Y && c.Is_Finded == _c.Is_Finded &&
                            c.Labeled_Height == _c.Labeled_Height && c.Labeled_Width == _c.Labeled_Width && c.Labeled_Name == _c.Labeled_Name &&
                            c.Labeled_X == _c.Labeled_X && c.Labeled_Y == _c.Labeled_Y && c.Is_Labeled == _c.Is_Labeled)
                        {
                            m_MatchCount++;
                        }
                    }
                }

                if (m_PreSaveImageInfo.CharList.Count == imageInfo.CharList.Count && m_PreSaveImageInfo.CharList.Count == m_MatchCount)
                {
                    m_IsSaved = true;
                    SaveBtnVisibility = Visibility.Hidden;
                }
            }

            /*
            if (imageInfo.CharList.Count == 0)
            {
                if (imageInfo.TrainType != TrainType.NONE)
                {
                    string filename = Path.GetFileName(imageInfo.FileName);
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(imageInfo.FileName);
                    string trainType = imageInfo.TrainType.ToString();

                    if (trainType == "NONE")
                    {
                        trainType = "NotLabeled";
                    }
                    else if (trainType == "LABELED")
                    {
                        trainType = "Labeled";
                    }
                    else if (trainType == "TRAINED")
                    {
                        trainType = "Trained";
                    }

                    if (!Directory.Exists(m_ProjectPath + "\\Images\\NotLabeled"))
                    {
                        Directory.CreateDirectory(m_ProjectPath + "\\Images\\NotLabeled");
                    }

                    if (File.Exists(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename))
                    {
                        File.Copy(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename, m_ProjectPath + "\\Images\\NotLabeled\\" + filename, true);
                        File.Delete(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename);
                    }

                    if (File.Exists(m_ProjectPath + "\\Images\\" + trainType + "\\" + fileNameWithoutExtension + ".hsr"))
                    {
                        File.Copy(m_ProjectPath + "\\Images\\" + trainType + "\\" + fileNameWithoutExtension + ".hsr", m_ProjectPath + "\\Images\\NotLabeled\\" + fileNameWithoutExtension + ".hsr", true);
                        File.Delete(m_ProjectPath + "\\Images\\" + trainType + "\\" + fileNameWithoutExtension + ".hsr");
                    }

                    imageInfo.FileName = m_ProjectPath + "\\Images\\NotLabeled\\" + filename;
                }

                imageInfo.TrainType = TrainType.NONE;
            }
            */

            /*
            for (int i = 0; i < m_ImageInfoList_Temp.Count; i++)
            {
                if (Path.GetFileName(m_ImageInfoList_Temp[i].FileName) == Path.GetFileName(imageInfo.FileName))
                {
                    m_ImageInfoList_Temp[i] = imageInfo;
                }
            }

            for (int i = 0; i < PartImageInfoList.Count; i++)
            {
                if (Path.GetFileName(PartImageInfoList[i].FileName) == Path.GetFileName(imageInfo.FileName))
                {
                    PartImageInfoList[i] = imageInfo;
                }
            }

            DependencyObject item = (DependencyObject)lbImageList.ItemContainerGenerator.ContainerFromIndex(index);

            List<DependencyObject> list = item.GetChildObjects(true).ToList();

            Grid grid = (Grid)list[0];
            
            for (int i = 0; i < grid.Children.Count; i++)
            {
                List<DependencyObject> _list = grid.Children[i].GetChildObjects(true).ToList();

                for (int l = 0; l < _list.Count; l++)
                {
                    if (_list[l].GetType() == typeof(ContentPresenter))
                    {
                        ContentPresenter cp = (ContentPresenter)_list[l];
                        List<DependencyObject> _l = cp.GetChildObjects(true).ToList();

                        for (int k = 0; k < _l.Count; k++)
                        {
                            if (_l[k].GetType() == typeof(Grid))
                            {
                                Grid _grid = (Grid)_l[k];

                                for (int j = 0; j < _grid.Children.Count; j++)
                                {
                                    if (_grid.Children[j].GetType() == typeof(StackPanel))
                                    {
                                        StackPanel sp = (StackPanel)_grid.Children[j];

                                        for (int o = 0; o < sp.Children.Count; o++)
                                        {
                                            if (sp.Children[o].GetType() == typeof(Grid))
                                            {
                                                Grid _g = (Grid)sp.Children[o];

                                                for (int p = 0; p < _g.Children.Count; p++)
                                                {
                                                    if (_g.Children[p].GetType() == typeof(TextBlock))
                                                    {
                                                        TextBlock tb = (TextBlock)_g.Children[p];

                                                        if (tb.Name == "CharList_Count")
                                                        {
                                                            tb.Text = imageInfo.CharList.Count.ToString();
                                                            break;
                                                        }
                                                    }
                                                }

                                                break;
                                            }
                                        }
                                        
                                        break;
                                    }
                                }

                                break;
                            }
                        }

                        break;
                    }

                    break;
                }
            }
            */


            Refresh_ImageList(m_ImageShowType);

            DrawImageLabel();

            Save_NameFile();
            // Save_ImageList_Information(imageInfo);

            Load_LabelList();
            RefreshcharInfo();

            // PageStackRefresh();

            lbImageList.SelectedIndex = index;

            this.Focus();

            dp.SelectRectangle = null;
        }

        private void Dp_LeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            m_IsLeftMouseBtnPressed = true;

            if (m_IsCtrlPressed)
            {
                System.Windows.Point _point = dp.canvas.RealPoint;

                m_DragRectangleStartPoint = new System.Drawing.Point((int)_point.X, (int)_point.Y);
            }

            dp.Focus();
        }

        private void Dp_LeftMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (m_IsCtrlPressed && m_IsLeftMouseBtnPressed && dp.Lines != null && dp.SelectRectangle == null)
            {
                int minX = int.MaxValue;
                int minY = int.MaxValue;
                int maxX = int.MinValue;
                int maxY = int.MinValue;

                dp.Lines.ToList().ForEach(x =>
                {
                    if (minX > x.Point1.X)
                    {
                        minX = x.Point1.X;
                    }
                    if (minX > x.Point2.X)
                    {
                        minX = x.Point2.X;
                    }
                    if (minY > x.Point1.Y)
                    {
                        minY = x.Point1.Y;
                    }
                    if (minY > x.Point2.Y)
                    {
                        minY = x.Point2.Y;
                    }
                    if (maxX < x.Point1.X)
                    {
                        maxX = x.Point1.X;
                    }
                    if (maxX < x.Point2.X)
                    {
                        maxX = x.Point2.X;
                    }
                    if (maxY < x.Point1.Y)
                    {
                        maxY = x.Point1.Y;
                    }
                    if (maxY < x.Point2.Y)
                    {
                        maxY = x.Point2.Y;
                    }
                });

                int count = 0;
                List<CharInfo> _infos = new List<CharInfo>();

                m_SelectedImageInfo.CharList.ForEach(x =>
                {
                    if (minX <= (dp.BitmapImage.Width * x.Labeled_X) - (x.Labeled_Width * dp.BitmapImage.Width / 2) && maxX >= (dp.BitmapImage.Width * x.Labeled_X) - (x.Labeled_Width * dp.BitmapImage.Width / 2) && minY <= (dp.BitmapImage.Height * x.Labeled_Y) - (x.Labeled_Height * dp.BitmapImage.Height / 2) && maxY >= (dp.BitmapImage.Height * x.Labeled_Y) - (x.Labeled_Height * dp.BitmapImage.Height / 2))
                    {
                        m_SelectedCharIndex = count;
                        CharInfo c = m_SelectedImageInfo.CharList[m_SelectedCharIndex];
                        _infos.Add(c);
                    }

                    count++;
                });

                _infos.ForEach(x =>
                {
                    m_SelectedImageInfo.CharList.Remove(x);
                    dp.ShowRectangle((int)(dp.BitmapImage.Width * x.Labeled_X - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(dp.BitmapImage.Height * x.Labeled_Y - (dp.BitmapImage.Height * x.Labeled_Height) / 2), (int)(dp.BitmapImage.Width * x.Labeled_Width), (int)(dp.BitmapImage.Height * x.Labeled_Height), x.Labeled_Name, Brushes.Red);
                });

                dp.Lines.Clear();
            }

            if (dp.BitmapImage == null)
            {
                return;
            }

            if (!m_IsMouseDragged)
            {
                dp.ConfirmButtonVisibility = Visibility.Visible;

                bool IsSelected = false;
                int Count = 0;

                // 현재 마우스 위치에서 사각형 크기 계산
                int CurrentX = (int)dp.canvas.RealPoint.X;
                int CurrentY = (int)dp.canvas.RealPoint.Y;

                #region 기존 영역에 클릭 했는지 확인...
                m_SelectedImageInfo.CharList.ForEach(x =>
                {
                    if (m_IsFullROI)
                    {
                        if (x.Is_Labeled)
                        {
                            if (CurrentX >= (dp.BitmapImage.Width * x.Labeled_X) - (x.Labeled_Width * dp.BitmapImage.Width / 2) && CurrentX <= (dp.BitmapImage.Width * x.Labeled_X) + (x.Labeled_Width * dp.BitmapImage.Width / 2) && CurrentY >= (dp.BitmapImage.Height * x.Labeled_Y) - (x.Labeled_Height * dp.BitmapImage.Height / 2) && CurrentY <= (dp.BitmapImage.Height * x.Labeled_Y) + (x.Labeled_Height * dp.BitmapImage.Height / 2))
                            {
                                if (IsSelected)
                                {
                                    if (x.Labeled_Width < m_SelectedImageInfo.CharList[m_SelectedCharIndex].Labeled_Width || x.Labeled_Height < m_SelectedImageInfo.CharList[m_SelectedCharIndex].Labeled_Height)
                                    {
                                        m_SelectedCharIndex = Count;
                                        IsSelected = true;
                                    }
                                }
                                else
                                {
                                    m_SelectedCharIndex = Count;
                                    IsSelected = true;
                                }
                            }
                        }
                        else if (x.Is_Finded)
                        {
                            if (CurrentX > (dp.BitmapImage.Width * x.Finded_X) - (dp.BitmapImage.Width * x.Finded_Width / 2) && CurrentX < (dp.BitmapImage.Width * x.Finded_X) + (dp.BitmapImage.Width * x.Finded_Width / 2) && CurrentY > (dp.BitmapImage.Height * x.Finded_Y) - (dp.BitmapImage.Height * x.Finded_Height / 2) && CurrentY < (dp.BitmapImage.Height * x.Finded_Y) + (dp.BitmapImage.Height * x.Finded_Height / 2))
                            {
                                if (IsSelected)
                                {
                                    if (x.Finded_Width < m_SelectedImageInfo.CharList[m_SelectedCharIndex].Finded_Width || x.Finded_Height < m_SelectedImageInfo.CharList[m_SelectedCharIndex].Finded_Height)
                                    {
                                        m_SelectedCharIndex = Count;
                                        IsSelected = true;
                                    }
                                }
                                else
                                {
                                    m_SelectedCharIndex = Count;
                                    IsSelected = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (x.Is_Labeled)
                        {
                            if ((CurrentX > dp.BitmapImage.Width * x.Labeled_X - dp.BitmapImage.Width * x.Labeled_Width / 2)
                                  && (CurrentX < dp.BitmapImage.Width * x.Labeled_X + dp.BitmapImage.Width * x.Labeled_Width / 2)
                                  && (CurrentY > dp.BitmapImage.Height * x.Labeled_Y - dp.BitmapImage.Height * x.Labeled_Height / 2)
                                  && (CurrentY < dp.BitmapImage.Height * x.Labeled_Y + dp.BitmapImage.Height * x.Labeled_Height / 2))
                            {
                                m_SelectedCharIndex = Count;
                                IsSelected = true;
                            }
                        }
                        else if (x.Is_Finded)
                        {
                            if ((CurrentX > mcurrentROI.Width * x.Finded_X - mcurrentROI.Width * x.Finded_Width + mcurrentROI.X / 2)
                                && (CurrentX < mcurrentROI.Width * x.Finded_X + mcurrentROI.Width * x.Finded_Width + mcurrentROI.X / 2)
                                && (CurrentY > mcurrentROI.Height * x.Finded_Y - mcurrentROI.Height * x.Finded_Height + mcurrentROI.Y / 2)
                                && (CurrentY < mcurrentROI.Height * x.Finded_Y + mcurrentROI.Height * x.Finded_Height + mcurrentROI.Y / 2))
                            {
                                m_SelectedCharIndex = Count;
                                IsSelected = true;
                            }
                        }
                    }

                    Count++;
                });
                #endregion

                if (!IsSelected && dp.SelectRectangle == null)
                {
                    //사각형 기본 사이즈
                    if (m_DefaultWidth <= 0)
                    {
                        m_DefaultWidth = Convert.ToInt32(dp.BitmapImage.Width * 0.1);
                    }

                    if (m_DefaultHeight <= 0)
                    {
                        m_DefaultHeight = Convert.ToInt32(dp.BitmapImage.Width * 0.1);
                    }

                    dp.ShowRectangle(CurrentX - (m_DefaultWidth / 2), CurrentY - (m_DefaultHeight / 2), (int)(m_DefaultWidth), (int)(m_DefaultHeight), "?", Brushes.Red);
                    IsSelected = true;
                }
                else if (!IsSelected)
                {
                    Confirm();

                    DrawImageLabel();
                }
                else
                {
                    Confirm();

                    // 선택한 영역 임시 저장
                    m_TempCharInfo = m_SelectedImageInfo.CharList[m_SelectedCharIndex];
                    m_IsTempCharInfo = true;

                    // 선택한 영역 리스트에서 삭제
                    m_SelectedImageInfo.CharList.RemoveAt(m_SelectedCharIndex);

                    if (m_TempCharInfo.Is_Labeled == true)
                    {
                        // 편집 가능한 사각형 표시
                        dp.ShowRectangle((int)(dp.BitmapImage.Width * m_TempCharInfo.Labeled_X - (m_TempCharInfo.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(dp.BitmapImage.Height * m_TempCharInfo.Labeled_Y - (dp.BitmapImage.Height * m_TempCharInfo.Labeled_Height) / 2), (int)(dp.BitmapImage.Width * m_TempCharInfo.Labeled_Width), (int)(dp.BitmapImage.Height * m_TempCharInfo.Labeled_Height), m_TempCharInfo.Labeled_Name, Brushes.Red);
                    }
                    else
                    {
                        if (m_IsFullROI == true)
                        {
                            dp.ShowRectangle((int)(dp.BitmapImage.Width * m_TempCharInfo.Finded_X - (m_TempCharInfo.Finded_Width * dp.BitmapImage.Width) / 2), (int)(dp.BitmapImage.Height * m_TempCharInfo.Finded_Y - (dp.BitmapImage.Height * m_TempCharInfo.Finded_Height) / 2), (int)(dp.BitmapImage.Width * m_TempCharInfo.Finded_Width), (int)(dp.BitmapImage.Height * m_TempCharInfo.Finded_Height), m_TempCharInfo.Finded_Name, Brushes.Red);
                        }
                        else
                        {
                            dp.ShowRectangle((int)(mcurrentROI.Width * m_TempCharInfo.Finded_X - (m_TempCharInfo.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X
                                           , (int)(mcurrentROI.Height * m_TempCharInfo.Finded_Y - (m_TempCharInfo.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y
                                           , (int)(mcurrentROI.Width * m_TempCharInfo.Finded_Width)
                                           , (int)(mcurrentROI.Height * m_TempCharInfo.Finded_Height)
                                           , m_TempCharInfo.Finded_Name, Brushes.Red);
                        }
                    }

                    // 사각형 영역 갱신
                    DrawImageLabel();
                }

                if (dp.SelectRectangle != null)
                {
                    dp.SelectRectangle.KeyUp += Labelchanged;
                }
                
            }

            m_IsLeftMouseBtnPressed = false;
            m_IsMouseDragged = false;
        }

        public void Dp_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_IsCtrlPressed && m_IsLeftMouseBtnPressed && dp.SelectRectangle == null)
            {
                System.Windows.Point _point = dp.canvas.RealPoint;

                m_DragRectangleEndPoint = new System.Drawing.Point((int)_point.X, (int)_point.Y);

                Pen _pen = new Pen(Brushes.Yellow, 1);

                StructLine _top = new StructLine();
                _top.Pen = _pen;
                _top.Point1 = m_DragRectangleStartPoint;
                _top.Point2 = new System.Drawing.Point(m_DragRectangleEndPoint.X, m_DragRectangleStartPoint.Y);

                StructLine _bottom = new StructLine();
                _bottom.Pen = _pen;
                _bottom.Point1 = new System.Drawing.Point(m_DragRectangleStartPoint.X, m_DragRectangleEndPoint.Y);
                _bottom.Point2 = m_DragRectangleEndPoint;

                StructLine _left = new StructLine();
                _left.Pen = _pen;
                _left.Point1 = m_DragRectangleStartPoint;
                _left.Point2 = new System.Drawing.Point(m_DragRectangleStartPoint.X, m_DragRectangleEndPoint.Y);

                StructLine _right = new StructLine();
                _right.Pen = _pen;
                _right.Point1 = new System.Drawing.Point(m_DragRectangleEndPoint.X, m_DragRectangleStartPoint.Y);
                _right.Point2 = m_DragRectangleEndPoint;

                if (dp.Lines == null)
                {
                    dp.Lines = new ObservableCollection<StructLine>();
                }

                dp.Lines.Clear();
                dp.Lines.Add(_top);
                dp.Lines.Add(_bottom);
                dp.Lines.Add(_left);
                dp.Lines.Add(_right);
            }
            
            if (m_IsLeftMouseBtnPressed)
            {
                m_IsMouseDragged = true;
            }
        }

        private void Dp_MouseLeave(object sender, MouseEventArgs e)
        {
            if (m_IsCtrlPressed && m_IsLeftMouseBtnPressed && dp.Lines != null && dp.SelectRectangle == null)
            {
                int minX = int.MaxValue;
                int minY = int.MaxValue;
                int maxX = int.MinValue;
                int maxY = int.MinValue;

                dp.Lines.ToList().ForEach(x =>
                {
                    if (minX > x.Point1.X)
                    {
                        minX = x.Point1.X;
                    }
                    if (minX > x.Point2.X)
                    {
                        minX = x.Point2.X;
                    }
                    if (minY > x.Point1.Y)
                    {
                        minY = x.Point1.Y;
                    }
                    if (minY > x.Point2.Y)
                    {
                        minY = x.Point2.Y;
                    }
                    if (maxX < x.Point1.X)
                    {
                        maxX = x.Point1.X;
                    }
                    if (maxX < x.Point2.X)
                    {
                        maxX = x.Point2.X;
                    }
                    if (maxY < x.Point1.Y)
                    {
                        maxY = x.Point1.Y;
                    }
                    if (maxY < x.Point2.Y)
                    {
                        maxY = x.Point2.Y;
                    }
                });

                int count = 0;
                List<CharInfo> _infos = new List<CharInfo>();

                m_SelectedImageInfo.CharList.ForEach(x =>
                {
                    if (minX <= (dp.BitmapImage.Width * x.Labeled_X) - (x.Labeled_Width * dp.BitmapImage.Width / 2) && maxX >= (dp.BitmapImage.Width * x.Labeled_X) - (x.Labeled_Width * dp.BitmapImage.Width / 2) && minY <= (dp.BitmapImage.Height * x.Labeled_Y) - (x.Labeled_Height * dp.BitmapImage.Height / 2) && maxY >= (dp.BitmapImage.Height * x.Labeled_Y) - (x.Labeled_Height * dp.BitmapImage.Height / 2))
                    {
                        m_SelectedCharIndex = count;
                        CharInfo c = m_SelectedImageInfo.CharList[m_SelectedCharIndex];
                        _infos.Add(c);
                    }

                    count++;
                });

                _infos.ForEach(x =>
                {
                    m_SelectedImageInfo.CharList.Remove(x);
                    dp.ShowRectangle((int)(dp.BitmapImage.Width * x.Labeled_X - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(dp.BitmapImage.Height * x.Labeled_Y - (dp.BitmapImage.Height * x.Labeled_Height) / 2), (int)(dp.BitmapImage.Width * x.Labeled_Width), (int)(dp.BitmapImage.Height * x.Labeled_Height), x.Labeled_Name, Brushes.Red);
                });

                dp.Lines.Clear();

                m_IsLeftMouseBtnPressed = false;
            }
        }

        private void Load_ProjectParams()
        {
            LogManager.Action("Load_ProjectParams() 실행");

            NotifyPropertyChanged("YoloVersionStr");
            NotifyPropertyChanged("NetBatch");
            NotifyPropertyChanged("NetSubdivisions");
            NotifyPropertyChanged("NetWidth");
            NotifyPropertyChanged("NetHeight");
            NotifyPropertyChanged("NetSize");
            NotifyPropertyChanged("NetChannels");
            NotifyPropertyChanged("NetAngle");
            NotifyPropertyChanged("NetSaturation");
            NotifyPropertyChanged("NetExposure");
            NotifyPropertyChanged("NetHue");
            NotifyPropertyChanged("NetLearningRate");
            NotifyPropertyChanged("NetMaxBatches");
            NotifyPropertyChanged("NetSteps");
        }

        private ProgressDialogController m_Controller;

        public async void Load_Project()
        {
            LogManager.Action("Load_Project() 실행");

            m_IsFullROI = true;
            mcurrentROI = new Int32Rect(140, 160, 1000, 300);

            // ch_IsFullROI.IsChecked = m_IsFullROI;

            // 프로젝트 디폴트 경로 설정
            // m_ProjectDefaultPath = @"d:\yolo\Projects";
            m_ProjectDefaultPath = config.GetString("Info", "Default Project Path", @"d:\yolo\Projects");

            // 프로젝트 선택 대화 상자 열기
            ProjectManager prjManager = new ProjectManager(m_ProjectDefaultPath);

            prjManager.tbProjectPath.Text = m_ProjectPath;

            prjManager.ShowDialog();

            if (prjManager.ProjectName == "")
            {
                prjManager.Close();
                prjManager = null;
                Environment.Exit(0);
            }

            // 프로그램 실행 직후 GPU 초기화
            new Thread(new ThreadStart(() =>
            {
                GPUMemoryClear();
            })).Start();

            // 프로젝트 이름 지정
            m_ProjectName = prjManager.ProjectName;
            // 프로젝트 경로 설정
            m_ProjectPath = prjManager.ProjectPath + "\\" + prjManager.ProjectName;
            // 프로젝트 이미지 경로 설정
            m_ProjectImagePath = prjManager.ProjectPath + "\\" + m_ProjectName + "\\Images";

            LogManager.Trace("프로젝트명 : " + m_ProjectName);
            LogManager.Trace("프로젝트 경로 : " + m_ProjectPath);

            this.Title = this.Title + " [" + m_ProjectPath + "]";

            // 프로젝트 폴더 확인 후 생성
            if (Directory.Exists(m_ProjectPath) == false)
            {
                Directory.CreateDirectory(m_ProjectPath);
            }
            if (Directory.Exists(m_ProjectImagePath) == false)
            {
                Directory.CreateDirectory(m_ProjectImagePath);
            }

            config.WriteValue("Info", "Default Project Path", prjManager.ProjectPath);

            prjManager.Close();
            prjManager = null;

            cfgFile = new IniFile(m_ProjectPath + "\\" + m_ProjectName + ".cfg");
            projectConfig = new IniFile(m_ProjectPath + "\\" + m_ProjectName + ".ini");

            Load_ProjectParams();

            m_Controller = await this.ShowProgressAsync("불러오는 중...", "불러오는 중입니다...", true);
            m_Controller.SetIndeterminate();
            m_Controller.SetCancelable(false);

            await Task.Run(() =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    Load_LabelList();
                    FuncStackPanel_Init();
                });

                // 기존 트레인 데이터 확인
                try
                {
                    MakecfgFile("Training");

                    if (File.Exists(m_ProjectPath + "\\" + m_ProjectName + ".weights"))
                    {
                        StreamReader sr = new StreamReader(m_ProjectPath + "\\" + m_ProjectName + ".cfg");

                        string s = null;

                        string content = "";

                        while ((s = sr.ReadLine()) != null)
                        {
                            if (s.Contains("classes"))
                            {
                                s = "classes=" + LabelList.Count;
                            }

                            content += s + Environment.NewLine;
                        }

                        sr.Close();
                        sr.Dispose();
                        sr = null;

                        File.WriteAllText(m_ProjectPath + "\\" + m_ProjectName + ".cfg", content);

                        if (m_Process_Recv != null)
                        {
                            m_Process_Recv = new List<string>();
                        }

                        LogManager.Action("인식 엔진 프로그램 실행 시도");
                        ProcessStartInfo _i = new ProcessStartInfo();
                        _i.FileName = Environment.CurrentDirectory + "\\Detector\\Detector.exe";
                        m_Process = Process.Start(_i);
                        LogManager.Action("인식 엔진 프로그램 실행 완료");

                        if (m_WeightsFile == "")
                        {
                            m_WeightsFile = m_ProjectPath + "\\" + m_ProjectName + ".weights";
                        }

                        LogManager.Action("인식 엔진 프로그램 파라미터 전송 완료");

                        m_IsTrained = true;
                    }
                    else
                    {
                        File.Create(m_ProjectPath + "\\" + m_ProjectName + ".weights");
                        m_IsTrained = false;
                    }

                    Load_ImageList_Information();
                }
                catch (Exception ex)
                {
                    LogManager.Error(ex.Message);
                    m_IsTrained = false;
                }
            });

            new Thread(new ThreadStart(() =>
            {
                while (!m_LoadThreadFinished)
                {
                    Thread.Sleep(1);
                }

                m_Controller.CloseAsync();
            })).Start();
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (m_Process_Recv != null)
            {
                LogManager.Trace("Process_OutputDataReceived() 수신");
                LogManager.Trace(e.Data);
                m_Process_Recv.Add(e.Data);
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            LogManager.Error(e.Data);
        }

        //public void Save_Labeling_Data(string filename, ImageInfo imageInfo)
        //{
        //    #region 라벨링 파일 생성
        //    string temp = filename.Substring(0, filename.Length - 4) + ".txt";

        //    // 기존 레이블링 파일 삭제
        //    if (File.Exists(temp) == true)
        //    {
        //        File.Delete(temp);
        //    }

        //    FileStream fileStream = new FileStream(temp, FileMode.CreateNew);
        //    StreamWriter writer = new StreamWriter(fileStream);
        //    imageInfo.CharList.ForEach(x =>
        //    {
        //        if (x.Is_Labeled)
        //        {
        //            writer.Write(Get_LabelIndex(x.Labeled_Name).ToString() + " ");
        //            writer.Write(x.Labeled_X.ToString() + " ");
        //            writer.Write(x.Labeled_Y.ToString() + " ");
        //            writer.Write(x.Labeled_Width.ToString() + " ");
        //            writer.WriteLine(x.Labeled_Height.ToString());
        //        }
        //    });

        //    writer.Close();
        //    writer = null;

        //    fileStream.Close();
        //    fileStream = null;

        //    Save_ImageList();
        //    #endregion
        //}

        private bool Check_Label_Exist(string name)
        {
            bool IsFind = false;

            //for (int i = 0; i < m_LabeledList.Count; i++)
            //{
            //    if (m_LabeledList[i].Labeled_Name == name)
            //    {
            //        IsFind = true;
            //        break;
            //    }
            //}

            for (int i = 0; i < m_LabelList.Count; i++)
            {
                if (m_LabelList[i].LabelName == name)
                {
                    IsFind = true;
                    break;
                }
            }

            return IsFind;
        }

        private BitmapImage ImageCropToSource(string filename, Int32Rect rec)
        {
            //BitmapImage bmp = new BitmapImage(new Uri(filename));
            //CroppedBitmap cb = new CroppedBitmap(bmp, rec);
            //bmp.Freeze();
            //bmp = null;

            // GC.Collect();

            BitmapImage bmp = null;

            try
            {
                //cb = new CroppedBitmap(wbmp, new Int32Rect((int)(wwidth * sx), (int)(wheight * sy), (int)(wwidth * width), (int)(wheight * height)));

                Mat src = Cv2.ImRead(filename);
                Mat roi = src.SubMat(new OpenCvSharp.Rect(rec.X, rec.Y, rec.Width, rec.Height));
                src.Dispose();
                src = null;

                bmp = (BitmapImage)OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(roi);

                roi.Dispose();
                roi = null;
            }
            catch (Exception e)
            {
                LogManager.Error(e.Message);
            }

            //BitmapImage bmp = new BitmapImage();
            //FileStream stream = new FileStream(filename, FileMode.Open);

            //bmp.BeginInit();
            //bmp.CacheOption = BitmapCacheOption.OnLoad;
            //bmp.StreamSource = stream;
            //bmp.EndInit();
            //bmp.Freeze();
            //BitmapSource source = new FormatConvertedBitmap(bmp, PixelFormats.Pbgra32, null, 0);
            //stream.Close();
            //stream = null;

            //WriteableBitmap wbmp = new WriteableBitmap(source);

            //int wwidth = wbmp.PixelWidth;
            //int wheight = wbmp.PixelHeight;

            //int[] pixelArray = new int[wwidth * wheight];

            //int stride = wbmp.PixelWidth * (wbmp.Format.BitsPerPixel / 8);
            //wbmp.CopyPixels(pixelArray, stride, 0);
            //wbmp.WritePixels(new Int32Rect(0, 0, wwidth, wheight), pixelArray, stride, 0);

            //bmp = null;

            //CroppedBitmap cb = null;

            //try
            //{
            //    cb = new CroppedBitmap(wbmp, rec);
            //}
            //catch
            //{

            //}

            //wbmp.Freeze();
            //wbmp = null;

            //GC.Collect();
            //System.Threading.Thread.Sleep(1);

            //return cb;
            return bmp;
        }

        //private CroppedBitmap ImageCrop(string filename, Int32Rect rec)
        private System.Drawing.Bitmap ImageCrop(string filename, Int32Rect rec)
        {
            //BitmapImage bmp = new BitmapImage(new Uri(filename));
            //CroppedBitmap cb = new CroppedBitmap(bmp, rec);
            //bmp.Freeze();
            //bmp = null;

            // GC.Collect();

            System.Drawing.Bitmap bmp = null;

            try
            {
                //cb = new CroppedBitmap(wbmp, new Int32Rect((int)(wwidth * sx), (int)(wheight * sy), (int)(wwidth * width), (int)(wheight * height)));

                Mat src = Cv2.ImRead(filename);
                Mat roi = src.SubMat(new OpenCvSharp.Rect(rec.X, rec.Y, rec.Width, rec.Height));
                src.Dispose();
                src = null;

                bmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(roi);

                roi.Dispose();
                roi = null;
            }
            catch (Exception e)
            {
                LogManager.Error(e.Message);
            }

            //BitmapImage bmp = new BitmapImage();
            //FileStream stream = new FileStream(filename, FileMode.Open);

            //bmp.BeginInit();
            //bmp.CacheOption = BitmapCacheOption.OnLoad;
            //bmp.StreamSource = stream;
            //bmp.EndInit();
            //bmp.Freeze();
            //BitmapSource source = new FormatConvertedBitmap(bmp, PixelFormats.Pbgra32, null, 0);
            //stream.Close();
            //stream = null;

            //WriteableBitmap wbmp = new WriteableBitmap(source);

            //int wwidth = wbmp.PixelWidth;
            //int wheight = wbmp.PixelHeight;

            //int[] pixelArray = new int[wwidth * wheight];

            //int stride = wbmp.PixelWidth * (wbmp.Format.BitsPerPixel / 8);
            //wbmp.CopyPixels(pixelArray, stride, 0);
            //wbmp.WritePixels(new Int32Rect(0, 0, wwidth, wheight), pixelArray, stride, 0);

            //bmp = null;

            //CroppedBitmap cb = null;

            //try
            //{
            //    cb = new CroppedBitmap(wbmp, rec);
            //}
            //catch
            //{

            //}

            //wbmp.Freeze();
            //wbmp = null;

            //GC.Collect();
            //System.Threading.Thread.Sleep(1);

            //return cb;
            return bmp;
        }


        private BitmapImage ImageCropToSource(string filename, double sx, double sy, double width, double height)
        {
            if (sx < 0)
            {
                sx = 0;
            }

            if (sy < 0)
            {
                sy = 0;
            }

            if (sx + width > 1)
            {
                sx -= (sx + width) - 1;
            }

            if (sy + height > 1)
            {
                sy -= (sy + height) - 1;
            }

            // GC.Collect();

            //CroppedBitmap cb = null;
            BitmapImage bmp = null;

            try
            {
                //cb = new CroppedBitmap(wbmp, new Int32Rect((int)(wwidth * sx), (int)(wheight * sy), (int)(wwidth * width), (int)(wheight * height)));

                Mat src = Cv2.ImRead(filename);
                Mat roi = src.SubMat(new OpenCvSharp.Rect((int)(src.Width * sx), (int)(src.Height * sy), (int)(src.Width * width), (int)(src.Height * height)));
                src.Dispose();
                src = null;

                bmp = MatToBitmapImage(roi);
                roi.Dispose();
                roi = null;
            }
            catch (Exception e)
            {
                LogManager.Error(e.Message);
            }

            //wbmp.Freeze();
            //wbmp = null;

            // GC.Collect();

            //return cb;
            return bmp;
        }

        private BitmapImage MatToBitmapImage(Mat mat)
        {
            // GC.Collect();

            BitmapSource bs = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(mat);

            BitmapImage bi = new BitmapImage();

            WriteableBitmap wb = new WriteableBitmap(bs);

            using (MemoryStream ms = new MemoryStream())
            {
                PngBitmapEncoder pbe = new PngBitmapEncoder();
                pbe.Frames.Add(BitmapFrame.Create(wb));
                pbe.Save(ms);

                ms.Seek(0, SeekOrigin.Begin);

                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.StreamSource = ms;
                bi.EndInit();
                bi.Freeze();
            }

            // GC.Collect();

            return bi;
        }

        //private CroppedBitmap ImageCrop(string filename, double sx, double sy, double width, double height)
        private System.Drawing.Bitmap ImageCrop(string filename, double sx, double sy, double width, double height)
        {
            //BitmapImage bmp = new BitmapImage();
            //FileStream stream = new FileStream(filename, FileMode.Open);

            //bmp.BeginInit();
            //bmp.CacheOption = BitmapCacheOption.OnLoad;
            //bmp.StreamSource = stream;
            //bmp.EndInit();
            //bmp.Freeze();
            //BitmapSource source = new FormatConvertedBitmap(bmp, PixelFormats.Pbgra32, null, 0);
            //stream.Close();
            //stream = null;

            //WriteableBitmap wbmp = new WriteableBitmap(source);

            //int wwidth = wbmp.PixelWidth;
            //int wheight = wbmp.PixelHeight;

            //int[] pixelArray = new int[wwidth * wheight];

            //int stride = wbmp.PixelWidth * (wbmp.Format.BitsPerPixel / 8);
            //wbmp.CopyPixels(pixelArray, stride, 0);
            //wbmp.WritePixels(new Int32Rect(0, 0, wwidth, wheight), pixelArray, stride, 0);

            //source.Freeze();
            //source = null;
            //bmp = null;

            //pixelArray = null;

            //GC.Collect();
            //GC.Collect();
            //System.Threading.Thread.Sleep(10);

            /*
            if (sx < 0)
            {
                sx = 0;
            }

            if (sy < 0)
            {
                sy = 0;
            }

            if (sx + width > 1)
            {
                sx -= (sx + width) - 1;
            }

            if (sy + height > 1)
            {
                sy -= (sy + height) - 1;
            }
            */

            System.Drawing.Bitmap bmp = null;

            double temp_x = 0;
            double temp_y = 0;
            double temp_width = 0;
            double temp_height = 0;

            try
            {
                Mat src = Cv2.ImRead(filename);

                temp_width = width * src.Width;
                temp_height = height * src.Height;
                temp_x = sx * src.Width;
                temp_y = sy * src.Height;

                if (temp_x < 0)
                {
                    temp_x = 0;
                }

                if (temp_y < 0)
                {
                    temp_y = 0;
                }

                if (temp_x + temp_width > src.Width - 1)
                {
                    temp_width -= temp_x + temp_width - (src.Width - 1);
                }

                if (temp_y + temp_height > src.Height - 1)
                {
                    temp_height -= temp_y + temp_height - (src.Height - 1);
                }

                // Mat roi = src.SubMat(new OpenCvSharp.Rect((int)(src.Width * sx), (int)(src.Height * sy), (int)(src.Width * width), (int)(src.Height * height)));
                Mat roi = src.SubMat(new OpenCvSharp.Rect((int)temp_x, (int)temp_y, (int)temp_width, (int)temp_height));
                src.Dispose();
                src = null;

                bmp = BitmapConverter.ToBitmap(roi);
                roi.Dispose();
                roi = null;
            }
            catch (Exception e)
            {
                LogManager.Error(e.Message);
            }

            //wbmp.Freeze();
            //wbmp = null;

            //GC.Collect();

            //return cb;
            return bmp;
        }

        private void Load_LabelList()
        {
            // m_LabelList.Clear();

            // 기존 데이터 파일 삭제
            if (File.Exists(m_ProjectPath + "\\" + m_ProjectName + ".names") == true)
            {
                FileStream fileStream = new FileStream(m_ProjectPath + "\\" + m_ProjectName + ".names", FileMode.Open);
                StreamReader reader = new StreamReader(fileStream);
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    StructResult structResult = new StructResult(line);

                    // 공백일 경우 라인 넘김
                    if (structResult.LabelName == "")
                        continue;

                    bool isExisted = false;

                    // 중복 체크 알고리즘 필요
                    m_LabelList.ToList().ForEach(x =>
                    {
                        if (x.LabelName == structResult.LabelName)
                        {
                            isExisted = true;
                        }
                    });

                    if (!isExisted)
                    {
                        m_LabelList.Add(structResult);
                    }
                }

                reader.Close();
                reader = null;
                fileStream.Close();
                fileStream = null;
            }
        }

        private string Get_LabelName(int index)
        {
            if (index < 0)
                return "?";

            return m_LabelList[index].LabelName;
        }

        private int Get_LabelIndex(string name)
        {
            for (int i = 0; i < m_LabelList.Count; i++)
            {
                if (m_LabelList[i].LabelName == name)
                    return i;
            }

            return -1;
        }

        private void Load_ImageList()
        {
            string line;

            //m_LabeledList.Clear();
            //m_LabelList.Clear();
            ImageInfoList.Clear();

            // 프로젝트 파일 열기
            m_ProjectStream = new FileStream(m_ProjectPath + "\\" + m_ProjectName + ".hap", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

            using (StreamReader sr = new StreamReader(m_ProjectStream))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    string filename, traintype;
                    //string filename = line;

                    try
                    {
                        filename = line.Split('\t')[0];
                    }
                    catch
                    {
                        continue;
                    }

                    try
                    {
                        traintype = line.Split('\t')[1];
                    }
                    catch
                    {
                        traintype = "NONE";
                    }

                    ImageInfo imageInfo = new ImageInfo(filename);

                    System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(filename);
                    imageInfo.Width = bmp.Width;
                    imageInfo.Height = bmp.Height;
                    bmp.Dispose();
                    bmp = null;

                    switch (traintype)
                    {
                        case "NONE":
                            imageInfo.TrainType = TrainType.NONE;
                            break;

                        case "NAVER":
                            imageInfo.TrainType = TrainType.NAVER;
                            break;

                        case "LABELED":
                            imageInfo.TrainType = TrainType.LABELED;
                            break;

                        case "TRAINED":
                            imageInfo.TrainType = TrainType.TRAINED;
                            break;

                        default:
                            imageInfo.TrainType = TrainType.NONE;
                            break;
                    }

                    //filename = filename.Replace("F:\\Yolo\\HTranner\\HTrainner\\bin\\Debug\\", "");

                    //filename = Directory.GetCurrentDirectory() + "\\" + filename;

                    #region Label 된 문자 정보 저장
                    if (File.Exists(filename.Substring(0, filename.Length - 4) + ".txt") == true)
                    {
                        FileStream streamlabel = File.Open(filename.Substring(0, filename.Length - 4) + ".txt", FileMode.Open, FileAccess.Read, FileShare.Read);

                        string line2;

                        using (StreamReader srl = new StreamReader(streamlabel))
                        {
                            while ((line2 = srl.ReadLine()) != null)
                            {
                                string[] temp = line2.Split(' ');
                                CharInfo labeledinfo = new CharInfo();

                                labeledinfo.Labeled_Name = Get_LabelName(int.Parse(temp[0]));

                                labeledinfo.type = CharType.Labeled;

                                if (Check_Label_Exist(labeledinfo.Labeled_Name) == false)
                                {
                                    //m_LabelList.Add(labeledinfo);
                                    m_LabelList.Add(new StructResult(labeledinfo.Labeled_Name));
                                }

                                labeledinfo.Labeled_X = double.Parse(temp[1]);
                                labeledinfo.Labeled_Y = double.Parse(temp[2]);
                                labeledinfo.Labeled_Width = double.Parse(temp[3]);
                                labeledinfo.Labeled_Height = double.Parse(temp[4]);

                                try
                                {
                                    //CroppedBitmap cb = ImageCrop(filename, labeledinfo.Labeled_X - labeledinfo.Labeled_Width / 2, labeledinfo.Labeled_Y - labeledinfo.Labeled_Height / 2
                                    //                  , labeledinfo.Labeled_Width, labeledinfo.Labeled_Height);
                                    System.Drawing.Bitmap cb = ImageCrop(filename, labeledinfo.Labeled_X - labeledinfo.Labeled_Width / 2, labeledinfo.Labeled_Y - labeledinfo.Labeled_Height / 2
                                                      , labeledinfo.Labeled_Width, labeledinfo.Labeled_Height);

                                    if (Directory.Exists("D:\\Temp\\Labeled\\" + labeledinfo.Labeled_Name))
                                    {
                                        Directory.Delete("D:\\Temp\\Labeled\\" + labeledinfo.Labeled_Name, true);
                                    }

                                    Directory.CreateDirectory("D:\\Temp\\Labeled\\" + labeledinfo.Labeled_Name);

                                    //SaveCroppedBitmap(cb, "D:\\Temp\\Labeled\\" + labeledinfo.Labeled_Name + "\\" + Math.Abs(DateTime.Now.ToBinary()).ToString() + ".bmp");

                                    //string[] s = filename.Split('\\');

                                    ////Directory.CreateDirectory("D:\\Temp");
                                    //Directory.CreateDirectory("D:\\Temp\\" + s[5].Substring(0, s[5].Length - 4));
                                    //SaveCroppedBitmap(cb, "D:\\Temp\\" + s[5].Substring(0, s[5].Length - 4) + "\\" + labeledinfo.Labeled_Name + "_" + Math.Abs(DateTime.Now.ToBinary()).ToString() + ".bmp");

                                    //labeledinfo.Labeledcrbmp = cb.Clone();
                                    cb.Dispose();
                                    cb = null;
                                }
                                catch (Exception e)
                                {
                                    LogManager.Error(e.Message);
                                }

                                labeledinfo.Is_Labeled = true;
                                imageInfo.CharList.Add(labeledinfo);
                            }
                        }

                        streamlabel.Close();
                        streamlabel = null;
                    }
                    #endregion

                    try
                    {
                        imageInfo = Detect(imageInfo);

                        imageInfo.IsVisble = true;

                        ImageInfoList.Add(imageInfo);
                    }
                    catch (Exception e)
                    {
                        LogManager.Error("인식 실패 :" + e.Message);
                    }
                }

                RefreshcharInfo();
                Refresh_ImageList(m_ImageShowType);
            }

            m_ProjectStream.Close();
        }

        private void Load_TrainImageList()
        {
            //m_LabeledList.Clear();
            m_LabelList.Clear();
            m_ImageInfoList.Clear();

            string[] files = Directory.GetFiles("D:\\temp\\Train", "*.bmp");

            foreach (string filename in files)
            {
                ImageInfo imageInfo = new ImageInfo(filename);

                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(filename);
                imageInfo.Width = bmp.Width;
                imageInfo.Height = bmp.Height;
                bmp.Dispose();
                bmp = null;

                imageInfo.TrainType = TrainType.LABELED;

                #region Label 된 문자 정보 저장
                if (File.Exists(filename.Substring(0, filename.Length - 4) + ".txt") == true)
                {
                    FileStream streamlabel = File.Open(filename.Substring(0, filename.Length - 4) + ".txt", FileMode.Open, FileAccess.Read, FileShare.Read);

                    string line2;

                    using (StreamReader srl = new StreamReader(streamlabel))
                    {
                        while ((line2 = srl.ReadLine()) != null)
                        {
                            string[] temp = line2.Split(' ');
                            CharInfo labeledinfo = new CharInfo();

                            labeledinfo.Labeled_Name = temp[0];

                            labeledinfo.type = CharType.Labeled;

                            if (Check_Label_Exist(labeledinfo.Labeled_Name) == false)
                            {
                                //m_LabeledList.Add(labeledinfo);
                                m_LabelList.Add(new StructResult(labeledinfo.Labeled_Name));
                            }

                            labeledinfo.Labeled_X = double.Parse(temp[1]);
                            labeledinfo.Labeled_Y = double.Parse(temp[2]);
                            labeledinfo.Labeled_Width = double.Parse(temp[3]);
                            labeledinfo.Labeled_Height = double.Parse(temp[4]);

                            try
                            {
                                //CroppedBitmap cb = ImageCrop(filename, labeledinfo.Labeled_X - labeledinfo.Labeled_Width / 2, labeledinfo.Labeled_Y - labeledinfo.Labeled_Height / 2
                                //                  , labeledinfo.Labeled_Width, labeledinfo.Labeled_Height);

                                System.Drawing.Bitmap cb = ImageCrop(filename, labeledinfo.Labeled_X - labeledinfo.Labeled_Width / 2, labeledinfo.Labeled_Y - labeledinfo.Labeled_Height / 2
                                                  , labeledinfo.Labeled_Width, labeledinfo.Labeled_Height);


                                string[] s = filename.Split('\\');

                                //Directory.CreateDirectory("D:\\Temp");
                                Directory.CreateDirectory("D:\\Temp\\" + s[5].Substring(0, s[5].Length - 4));
                                //SaveCroppedBitmap(cb, "D:\\Temp\\" + s[5].Substring(0, s[5].Length - 4) + "\\" + labeledinfo.Labeled_Name + "_" + Math.Abs(DateTime.Now.ToBinary()).ToString() + ".bmp");

                                //labeledinfo.Labeledcrbmp = cb.Clone();
                                cb.Dispose();
                                cb = null;
                            }
                            catch (Exception e)
                            {
                                LogManager.Error(e.Message);
                            }

                            labeledinfo.Is_Labeled = true;
                            imageInfo.CharList.Add(labeledinfo);
                        }
                    }

                    streamlabel.Close();
                    streamlabel = null;
                }
                #endregion

                try
                {
                    /*
                    if (yoloWrapper != null)
                    {
                        Detect(imageInfo);
                    }
                    */

                    Detect(imageInfo);

                    imageInfo.IsVisble = true;

                    m_ImageInfoList.Add(imageInfo);
                }
                catch (Exception e)
                {
                    LogManager.Error("감지 실패 : " + e.Message);
                }
            }

            RefreshcharInfo();
            Refresh_ImageList(m_ImageShowType);
        }

        private void SaveCroppedBitmap(CroppedBitmap image, string path)
        {
            if (image == null)
                return;

            FileStream mStream = new FileStream(path, FileMode.Create);

            BitmapEncoder bitmapEncoder = new PngBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(image));
            bitmapEncoder.Save(mStream);

            //JpegBitmapEncoder jEncoder = new JpegBitmapEncoder();
            //jEncoder.Frames.Add(BitmapFrame.Create(image));
            //jEncoder.Save(mStream);
            //jEncoder = null;            
            mStream.Dispose();
            mStream.Close();
            mStream = null;

            GC.Collect();
        }

        private void SaveCroppedBitmap(System.Drawing.Bitmap image, string path)
        {
            if (image == null)
                return;

            image.Save(path);
        }

        private void Save_ImageList()
        {
            if (File.Exists(m_ProjectPath + "\\" + m_ProjectName + ".hap"))
            {
                File.Delete(m_ProjectPath + "\\" + m_ProjectName + ".hap");
            }

            // 프로젝트 파일 열기
            m_ProjectStream = new FileStream(m_ProjectPath + "\\" + m_ProjectName + ".hap", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

            using (StreamWriter wr = new StreamWriter(m_ProjectStream))
            {
                for (int i = 0; i < m_ImageInfoList.Count; i++)
                {
                    wr.Write(m_ImageInfoList[i].FileName);
                    wr.WriteLine("\t" + m_ImageInfoList[i].TrainType.ToString());
                }
            }

            m_ProjectStream.Close();
            m_ProjectStream = null;
        }

        /// <summary>
        /// 찾은 문자 위치가 레이블링 한 문자 위치와 겹치는 지 확인하는 함수
        /// </summary>
        /// <param name="s">레이블 이름</param>
        /// <param name="X">찾은 X 좌표</param>
        /// <param name="Y">찾은 Y 좌표</param>
        /// <param name="Width">찾은 넓이</param>
        /// <param name="Height">찾은 높이</param>
        /// <param name="imageinfo">해당 이미지에 대한 정보</param>
        /// <returns></returns>
        private int Check_FindArea_in_LabelArea(string s, double X, double Y, double Width, double Height, ImageInfo imageinfo)
        {
            int count = 0;
            int findIndex = -1;

            imageinfo.CharList.ForEach(x =>
            {
                if (x.Is_Labeled)
                {
                    // 상대 좌표를 실 자표로 변환
                    double InputX, InputY, InputWidth, InputHeight;
                    double LabelX, LabelY, LabelWidth, LabelHeight;

                    LabelX = x.Labeled_X * imageinfo.Width;
                    LabelY = x.Labeled_Y * imageinfo.Height;
                    LabelWidth = x.Labeled_Width * imageinfo.Width;
                    LabelHeight = x.Labeled_Height * imageinfo.Height;

                    if (m_IsFullROI)
                    {
                        InputX = X * imageinfo.Width;
                        InputY = Y * imageinfo.Height;
                        InputWidth = Width * imageinfo.Width;
                        InputHeight = Height * imageinfo.Height;
                    }
                    else
                    {
                        InputX = X * mcurrentROI.Width + mcurrentROI.X;
                        InputY = Y * mcurrentROI.Height + mcurrentROI.Y;
                        InputWidth = Width * mcurrentROI.Width;
                        InputHeight = Height * mcurrentROI.Height;
                    }

                    double unionsx, unionsy, unionex, unioney;

                    if (InputX - InputWidth / 2 > LabelX + LabelWidth / 2 || InputX + InputWidth / 2 < LabelX - LabelWidth / 2
                       || InputY - InputHeight / 2 > LabelY + LabelHeight / 2 || InputY + InputHeight / 2 < LabelY - LabelHeight / 2)
                    {

                    }
                    else
                    {
                        unionsx = Math.Max(InputX - InputWidth / 2, LabelX - LabelWidth / 2);
                        unionsy = Math.Max(InputY - InputHeight / 2, LabelY - LabelHeight / 2);
                        unionex = Math.Min(InputX + InputWidth / 2, LabelX + LabelWidth / 2);
                        unioney = Math.Min(InputY + InputHeight / 2, LabelY + LabelHeight / 2);

                        double unionarea = (unionex - unionsx) * (unioney - unionsy);
                        double lablearea = (x.Labeled_Width * imageinfo.Width) * (x.Labeled_Height * imageinfo.Height);

                        //if ((Math.Abs((double)(Width * Height) / (double)unionarea) - 1) < 0.5)
                        if (unionarea > 0 && Math.Abs((double)(LabelWidth * LabelHeight) / (double)unionarea - 1) < 0.3)
                        {
                            findIndex = count;
                        }
                    }
                }

                count++;
            });

            return findIndex;
        }

        private int Check_FindArea_in_FindArea(string s, double X, double Y, double Width, double Height, ImageInfo imageinfo)
        {
            int count = 0;
            int findIndex = -1;

            imageinfo.CharList.ForEach(x =>
            {
                if (x.type == CharType.Finded || x.type == CharType.Both)
                {
                    // 상대 좌표를 실 자표로 변환
                    double InputX, InputY, InputWidth, InputHeight;
                    double FindX, FindY, FindWidth, FindHeight;

                    if (m_IsFullROI)
                    {
                        FindX = x.Finded_X * imageinfo.Width;
                        FindY = x.Finded_Y * imageinfo.Height;
                        FindWidth = x.Finded_Width * imageinfo.Width;
                        FindHeight = x.Finded_Height * imageinfo.Height;

                        InputX = X * imageinfo.Width;
                        InputY = Y * imageinfo.Height;
                        InputWidth = Width * imageinfo.Width;
                        InputHeight = Height * imageinfo.Height;
                    }
                    else
                    {
                        FindX = x.Finded_X * mcurrentROI.Width + mcurrentROI.X;
                        FindY = x.Finded_Y * mcurrentROI.Height + mcurrentROI.Y;
                        FindWidth = x.Finded_Width * mcurrentROI.Width;
                        FindHeight = x.Finded_Height * mcurrentROI.Height;

                        InputX = X * mcurrentROI.Width + mcurrentROI.X;
                        InputY = Y * mcurrentROI.Height + mcurrentROI.Y;
                        InputWidth = Width * mcurrentROI.Width;
                        InputHeight = Height * mcurrentROI.Height;
                    }

                    double unionsx, unionsy, unionex, unioney;

                    if (InputX - InputWidth / 2 > FindX + FindWidth / 2 || InputX + InputWidth / 2 < FindX - FindWidth / 2
                       || InputY - InputHeight / 2 > FindY + FindHeight / 2 || InputY + InputHeight / 2 < FindY - FindHeight / 2)
                    {

                    }
                    else
                    {
                        unionsx = Math.Max(InputX - InputWidth / 2, FindX - FindWidth / 2);
                        unionsy = Math.Max(InputY - InputHeight / 2, FindY - FindHeight / 2);
                        unionex = Math.Min(InputX + InputWidth / 2, FindX + FindWidth / 2);
                        unioney = Math.Min(InputY + InputHeight / 2, FindY + FindHeight / 2);

                        double unionarea = (unionex - unionsx) * (unioney - unionsy);
                        double lablearea = (x.Labeled_Width * imageinfo.Width) * (x.Labeled_Height * imageinfo.Height);

                        //if ((Math.Abs((double)(Width * Height) / (double)unionarea) - 1) < 0.5)
                        if (unionarea > 0 && Math.Abs((double)(FindWidth * FindHeight) / (double)unionarea - 1) < 0.3)
                        {
                            findIndex = count;
                        }
                    }
                }

                count++;
            });

            return findIndex;
        }

        private int sendImageIndex = 0;

        private List<YoloItem> DetectEngine(string path)
        {
            LogManager.Action("DetectEngine() 실행 (경로 : " + path + ")");
            Stopwatch sw = new Stopwatch();

            List<YoloItem> items = new List<YoloItem>();

            /*
            if (m_ChannelsError)
            {
                int channels = cfgFile.GetInt32("net", "channels", 1);

                if (channels == 1)
                {
                    cfgFile.WriteValue("net", "channels", 3);
                    NetChannels = 3;
                }
                else
                {
                    cfgFile.WriteValue("net", "channels", 1);
                    NetChannels = 1;
                }

                LogManager.Action("채널 수 변환 완료");

                LogManager.Action("인식 엔진 프로그램 실행 시도");

                if (m_Process == null)
                {
                    ProcessStartInfo _i = new ProcessStartInfo();
                    _i.FileName = AppDomain.CurrentDomain.BaseDirectory + "Detector\\Detector.exe";
                    m_Process = new Process();
                    m_Process.StartInfo = _i;
                    // m_Process.Start();
                    LogManager.Action("인식 엔진 프로그램 실행 완료");
                }

                if (m_WeightsFile == "")
                {
                    m_WeightsFile = m_ProjectPath + "\\" + m_ProjectName + ".weights";
                }

                LogManager.Action("인식 엔진 프로그램 파라미터 전송 완료");

                Thread.Sleep(2000);

                m_ChannelsError = false;
            }
            */

            bool isConnected = client.Connection.Connected;
            bool isAliveSocket = client.IsAliveSocket();

            if (client != null && isAliveSocket)
            {
                // m_Process.StandardInput.WriteLine(path);

                // 이미지 바이트 배열
                string ext = Path.GetExtension(path);
                Mat mat;

                if (NetChannels == 1)
                {
                    mat = Cv2.ImRead(path, ImreadModes.Grayscale);
                }
                else
                {
                    mat = Cv2.ImRead(path, ImreadModes.AnyColor);
                }

                // System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(path);
                int width = mat.Width;
                int height = mat.Height;
                int channels = mat.Channels();
                byte[] output = new byte[width * height * channels];

                Cv2.ImEncode(ext, mat, out output);

                // byte[] output = mat.ImEncode();

                // 커맨드 바이트 배열
                string sendStr = "IMAGE," + sendImageIndex + ",";
                sendImageIndex++;

                byte[] buf = Encoding.Default.GetBytes(sendStr);
                int len = buf.Length + output.Length + 16;
                byte[] merge = new byte[len];

                Buffer.BlockCopy(BitConverter.GetBytes(len - 4), 0, merge, 0, 4);
                Buffer.BlockCopy(buf, 0, merge, 4, buf.Length);
                Buffer.BlockCopy(BitConverter.GetBytes(height), 0, merge, buf.Length + 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(width), 0, merge, buf.Length + 8, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(channels), 0, merge, buf.Length + 12, 4);
                Buffer.BlockCopy(output, 0, merge, buf.Length + 16, output.Length);

                client.Send(merge);

                sw.Start();

                LogManager.Action("인식 엔진 프로그램 경로 전송 완료 (" + path + ")");
            }

            bool m_IsFind = false;
            int index = -1;
            int m_LimitTime = config.GetInt32("Detect", "LimitTime", 3000);
            int m_LimitDetectCount = config.GetInt32("Detect", "LimitDetectCount", 30);

            while (true)
            {
                /*
                isConnected = client.Connection.Connected;
                isAliveSocket = client.IsAliveSocket();

                if (client == null && !isConnected && !isAliveSocket)
                {
                    LogManager.Info("인식 엔진 프로그램이 실행 중이지 않습니다. 학습 데이터를 변경하거나 학습 프로그램을 재실행해주십시오.");
                    break;
                }

                if (items.Count > m_LimitDetectCount)
                {
                    LogManager.Info("리밋 인식 개수를 초과하였습니다. Config.ini 파일의 LimitDetectCount 항목의 값을 설정해주십시오.");
                    break;
                }

                if (sw.ElapsedMilliseconds > m_LimitTime)
                {
                    sw.Stop();
                    LogManager.Info("인식 가능 시간 범위를 벗어났습니다. Config.ini 파일의 LimitTime 항목의 값을 설정해주십시오. (소요시간 : " + sw.ElapsedMilliseconds + "ms)");
                    break;
                }
                */

                if (m_Process_Recv != null && m_Process_Recv.Count > 0)
                {
                    for (int i = 0; i < m_Process_Recv.Count; i++)
                    {
                        string data = m_Process_Recv[i];

                        if (data != null)
                        {
                            List<string> list = data.Split('/').ToList();

                            if (list[0] == Convert.ToString(sendImageIndex - 1))
                            {
                                list.GetRange(1, list.Count - 1).ForEach(x =>
                                {
                                    if (x != "")
                                    {
                                        string[] arr = x.Split(',');

                                        YoloItem item = new YoloItem()
                                        {
                                            Type = arr[0],
                                            Confidence = Convert.ToDouble(arr[1]),
                                            X = (int)Convert.ToDouble(arr[2]),
                                            Y = (int)Convert.ToDouble(arr[3]),
                                            Width = (int)Convert.ToDouble(arr[4]),
                                            Height = (int)Convert.ToDouble(arr[5])
                                        };

                                        item.X = item.X - item.Width / 2;
                                        item.Y = item.Y - item.Height / 2;

                                        items.Add(item);
                                    }
                                });

                                m_Process_Recv.RemoveAt(i);

                                m_IsFind = true;

                                sw.Stop();
                                LogManager.Action("인식 완료 - " + path + " (" + sw.ElapsedMilliseconds + "ms)");

                                break;
                            }
                        }
                    }
                }

                if (m_IsFind)
                {
                    break;
                }
            }

            LogManager.Action("DetectEngine() 종료");

            return items;
        }

        private ImageInfo Detect(ImageInfo imageInfo)
        {
            LogManager.Action("Detect() 실행");

            if (imageInfo.Width == 0 && imageInfo.Height == 0 && imageInfo.DateTime == null && imageInfo.FileName == null)
            {
                return imageInfo;
            }

            // 기존 찾은 문자 정보 클리어
            for (int i = 0; i < imageInfo.CharList.Count; i++)
            {
                CharInfo c = imageInfo.CharList[i];

                c.Finded_Name = "";
                c.Finded_X = 0;
                c.Finded_Y = 0;
                c.Finded_Width = 0;
                c.Finded_Height = 0;
                c.Finded_Score = 0;
                c.Is_Finded = false;

                imageInfo.CharList.RemoveAt(i);
                imageInfo.CharList.Insert(i, c);
            }

            if (!Directory.Exists(m_ProjectPath + "\\temp"))
            {
                Directory.CreateDirectory(m_ProjectPath + "\\temp");
            }

            string random_Name = m_ProjectPath + "\\temp\\" + Math.Abs(DateTime.Now.ToBinary()).ToString() + ".bmp";

            // 검사 영역이 화면 전체이면 원본 그대로 복사하여 임시 파일 생성
            if (m_IsFullROI)
            {
                File.Copy(imageInfo.FileName, random_Name, true);
            }
            // 검사 영역이 설정 되어 있으면 일부만 크롭하여 임시 파일 생성
            else
            {
                System.Drawing.Bitmap tempcb = ImageCrop(imageInfo.FileName, new Int32Rect(mcurrentROI.X, mcurrentROI.Y, mcurrentROI.Width, mcurrentROI.Height));
                SaveCroppedBitmap(tempcb, random_Name);
                tempcb.Dispose();
                tempcb = null;
            }

            m_DetectImageWidth = imageInfo.Width;
            m_DetectImageHeight = imageInfo.Height;

            List<YoloItem> items = DetectEngine(random_Name);

            /*
            if (items.Count == 0)
            {
                m_ChannelsError = true;

                if (m_Process != null)
                {
                    m_Process.Kill();
                    m_Process.Close();
                    m_Process.Dispose();
                    m_Process = null;
                    LogManager.Action("인식 엔진 프로그램 종료");
                }

                LogManager.Action("채널 수 오류로 인한 재인식 실행");
                // items = DetectEngine(random_Name);
                LogManager.Action("채널 수 오류로 인한 재인식 종료");

                if (items.Count == 0)
                {
                    return imageInfo;
                }
            }
            */

            int m_LimitDetectCount = config.GetInt32("Detect", "LimitDetectCount", 30);

            if (items.Count > m_LimitDetectCount)
            {
                items.Clear();
                LogManager.Action("인식 리밋 개수 초과하여 인식 라벨 모두 제거. (현재 인식 리밋 개수 : " + m_LimitDetectCount + ")");
            }

            int FindCount = 0;

            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(imageInfo.FileName);
            double w = bmp.Width;
            double h = bmp.Height;
            bmp.Dispose();
            bmp = null;

            #region 찾은 문자 정보 저장
            items.ToList().OrderBy(x => x.X).ToList().ForEach(x =>
            {
                if (x.Confidence * 100 > Score)
                {
                    double temp_width = x.Width / w;
                    double temp_height = x.Height / h;
                    double temp_x = (x.X + (x.Width / 2)) / w;
                    double temp_y = (x.Y + (x.Height / 2)) / h;

                    //if (!m_IsFullROI)
                    //{
                    //    temp_width = (temp_width * mcurrentROI.Width) / imageInfo.Width;
                    //    temp_height = (temp_height * mcurrentROI.Height) / imageInfo.Height;
                    //    temp_x = ((temp_x * mcurrentROI.Width) + mcurrentROI.X) / imageInfo.Width;
                    //    temp_y = ((temp_y * mcurrentROI.Height) + mcurrentROI.Y) / imageInfo.Height;
                    //}

                    double tempconfidence = x.Confidence;
                    string temp_type = x.Type;

                    CharInfo findcinfo = new CharInfo();
                    findcinfo.Finded_Name = temp_type;
                    findcinfo.Finded_X = temp_x;
                    findcinfo.Finded_Y = temp_y;
                    findcinfo.Finded_Width = temp_width;
                    findcinfo.Finded_Height = temp_height;
                    findcinfo.Finded_Score = tempconfidence;
                    findcinfo.Is_Finded = true;

                    //CroppedBitmap cb = ImageCrop(random_Name, findcinfo.Finded_X - findcinfo.Finded_Width / 2, findcinfo.Finded_Y - findcinfo.Finded_Height / 2
                    //                                 , findcinfo.Finded_Width, findcinfo.Finded_Height);

                    System.Drawing.Bitmap cb = ImageCrop(random_Name, findcinfo.Finded_X, findcinfo.Finded_Y, findcinfo.Finded_Width, findcinfo.Finded_Height);

                    if (!Directory.Exists(m_ProjectPath + "\\Temp\\Finded\\" + findcinfo.Finded_Name))
                    {
                        Directory.CreateDirectory(m_ProjectPath + "\\Temp\\Finded\\" + findcinfo.Finded_Name);
                    }

                    string source_name = imageInfo.FileName.Split('\\')[imageInfo.FileName.Split('\\').Length - 1];

                    SaveCroppedBitmap(cb, m_ProjectPath + "\\Temp\\Finded\\" + findcinfo.Finded_Name + "\\" + findcinfo.Finded_Score.ToString("0.00") + "_" + source_name + "_" + Math.Abs(DateTime.Now.ToBinary()).ToString() + ".bmp");

                    if (cb != null)
                    {
                        cb.Dispose();
                    }
                    
                    cb = null;

                    findcinfo.type = CharType.Finded;
                    imageInfo.CharList.Add(findcinfo);
                }
            });
            #endregion

            imageInfo.Find_String = "";

            imageInfo.CharList.OrderBy(x => x.Finded_X).ToList().ForEach(y =>
            {
                imageInfo.Find_String += y.Finded_Name;
            });

            // 배포 전 아래 주석 제거 할 것
            try
            {
                File.Delete(random_Name);
            }
            catch (Exception ex)
            {
                LogManager.Error(ex.Message);
            }

            return imageInfo;
        }

        private int m_AllTestCount = 0;
        private int m_AllTestDetectCount = 0;
        private ObservableCollection<StructDetectResult> m_AllTestResult = new ObservableCollection<StructDetectResult>();
        private bool m_IsAllTesting = false;

        private void Detect(string dir)
        {
            m_IsAllTesting = true;
            IsOpenAllTestResultEnabled = false;
            AllTestLoading = Visibility.Visible;
            AllTestText = Visibility.Hidden;
            m_AllTestResult.Clear();

            string[] files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);

            int total = files.Length;
            m_AllTestCount = files.Length;
            m_AllTestDetectCount = 0;
            int count = 0;
            bool m_IsError = false;
            int m_ErrorCount = 0;

            new Thread(new ThreadStart(() =>
            {
                int cnt = 0;

                files.ToList().ForEach(x =>
                {
                    string ext = Path.GetExtension(x);

                    if (ext.ToLower().Contains("jpg") || ext.ToLower().Contains("jpeg") || ext.ToLower().Contains("bmp") || ext.ToLower().Contains("png"))
                    {
                        string model = "";
                        int position = 0;

                        try
                        {
                            string filename = Path.GetFileNameWithoutExtension(x);
                            string[] filenameSplit = filename.Split('_');
                            model = filenameSplit[1];
                            string positionStr = filenameSplit[2];
                            position = Convert.ToInt32(positionStr);
                        }
                        catch
                        {

                        }

                        count++;

                        if (m_IsError)
                        {
                            return;
                        }

                        List<YoloItem> items = DetectEngine(x);

                        /*
                        if (items.Count == 0)
                        {
                            LogManager.Error("채널 수 오류 발생");
                            m_ChannelsError = true;

                            if (m_Process != null)
                            {
                                if (!m_Process.HasExited)
                                {
                                    m_Process.Kill();
                                }
                                
                                m_Process.Close();
                                m_Process.Dispose();
                                m_Process = null;
                                LogManager.Action("인식 엔진 프로그램 종료");
                            }

                            LogManager.Action("채널 수 오류로 인한 재인식 실행");
                            items = DetectEngine(x);
                            LogManager.Action("채널 수 오류로 인한 재인식 종료");

                            m_ErrorCount++;

                            if (m_ErrorCount > 5)
                            {
                                m_IsError = true;
                            }
                        }
                        */

                        string _ext = Path.GetExtension(x);

                        m_AllTestDetectCount += items.Count;
                        int _detect_count = items.Count;

                        GC.Collect();

                        StructDetectResult result = new StructDetectResult()
                        {
                            FileName = x,
                            DetectCount = _detect_count,
                            MatchCount = 0,
                            Items = items,
                            Position = position,
                            Model = model
                        };

                        Dispatcher.Invoke(() =>
                        {
                            m_AllTestResult.Add(result);
                        });
                    }

                    cnt++;
                });

                m_IsAllTesting = false;
                IsOpenAllTestResultEnabled = true;
                AllTestLoading = Visibility.Hidden;
                AllTestText = Visibility.Visible;

                for (int i = 0; i < m_AllTestResult.Count; i++)
                {
                    // nt m_BlankCount = 0;
                    int m_PlugCount = 0;
                    int m_HoleCount = 0;
                    /*
                    double m_BlankStartX = 0;
                    double m_BlankEndX = 0;
                    double m_BlankStartY = 0;
                    double m_BlankEndY = 0;
                    */
                    double m_PlugStartX = 0;
                    double m_PlugEndX = 0;
                    double m_PlugStartY = 0;
                    double m_PlugEndY = 0;
                    bool m_IsFindBlank = false;

                    m_AllTestResult[i].Items.ForEach(x =>
                    {
                        if (x.Type == "Plug")
                        {
                            if (!m_IsFindBlank)
                            {
                                /*
                                m_BlankStartX = x.X;
                                m_BlankEndX = x.X + x.Width;
                                m_BlankStartY = x.Y;
                                m_BlankEndY = x.Y + x.Height;
                                */
                                m_PlugStartX = x.X;
                                m_PlugEndX = x.X + x.Width;
                                m_PlugStartY = x.Y;
                                m_PlugEndY = x.Y + x.Height;
                            }

                            m_IsFindBlank = true;

                            m_PlugCount++;
                        }

                        if (x.Type == "Hole")
                        {
                            /*
                            if (!m_AllTestResult[i].IsLabelError)
                            {
                                if (m_PlugStartX <= x.X || m_PlugEndX >= x.X + x.Width || m_PlugStartY <= x.Y || m_PlugEndY >= x.Y + x.Height)
                                {

                                }
                                else
                                {
                                    m_AllTestResult[i].IsLabelError = true;
                                    m_AllTestResult[i].ErrorString += "플러그 영역에서 벗어난 홀 존재, ";
                                }
                            }
                            */

                            m_HoleCount++;
                        }
                    });

                    m_AllTestResult[i].PlugCount = m_PlugCount;
                    m_AllTestResult[i].HoleCount = m_HoleCount;

                    if (m_AllTestResult[i].Position > 100)
                    {
                        bool isError = false;

                        switch (m_AllTestResult[i].Model)
                        {
                            case "CN7":
                                if (m_PlugCount != 2)
                                {
                                    isError = true;
                                }

                                break;
                            case "CN7E":
                                if (m_PlugCount != 1)
                                {
                                    isError = true;
                                }

                                break;
                            case "PD":
                                if (m_PlugCount != 1)
                                {
                                    isError = true;
                                }

                                break;
                        }

                        if (isError)
                        {
                            m_AllTestResult[i].IsLabelError = true;
                            m_AllTestResult[i].ErrorString += "플러그 인식 개수 이상, ";
                        }
                    }

                    if (m_AllTestResult[i].Position < 100 && m_HoleCount == 0)
                    {
                        bool isError = false;

                        switch (m_AllTestResult[i].Model)
                        {
                            case "CN7":
                                if (m_HoleCount != 2)
                                {
                                    isError = true;
                                }

                                break;
                            case "CN7E":
                                if (m_HoleCount != 2)
                                {
                                    isError = true;
                                }

                                break;
                            case "PD":
                                if (m_HoleCount != 1)
                                {
                                    isError = true;
                                }

                                break;
                        }

                        if (isError)
                        {
                            m_AllTestResult[i].IsLabelError = true;
                            m_AllTestResult[i].ErrorString += "홀 인식 개수 이상, ";
                        }
                    }

                    if (m_AllTestResult[i].ErrorString != null)
                    {
                        if (m_AllTestResult[i].ErrorString.EndsWith(", "))
                        {
                            string str = m_AllTestResult[i].ErrorString.Substring(0, m_AllTestResult[i].ErrorString.Length - (", ").Length);
                            m_AllTestResult[i].ErrorString = str;
                        }
                    }
                }
            })).Start();
        }

        public void AllTestButton_Click(object sender, RoutedEventArgs e)
        {
            AllDetectResultWindow window = new AllDetectResultWindow(m_AllTestCount, m_AllTestDetectCount, 0, m_AllTestResult);
            window.ShowDialog();
        }

        private List<StructYoloItem> GetYoloResult(string path)
        {
            List<StructYoloItem> items = new List<StructYoloItem>();

            StreamReader sr = new StreamReader(path);
            StringBuilder sb = new StringBuilder(sr.ReadToEnd());
            sb = sb.Replace("\\", "/");

            string txt = sb.ToString();

            JArray jArr = JArray.Parse(txt);

            for (int i = 0; i < jArr.Count; i++)
            {
                JToken jToken = jArr[i];

                string filename = jToken.SelectToken("filename").ToString();

                Mat mat = Cv2.ImRead(filename);
                int imagecols = mat.Cols;
                int image_rows = mat.Rows;
                int col_interval = imagecols / 3;
                int row_interval = image_rows / 3;
                int image_width = mat.Width;
                int image_height = mat.Height;
                mat.Dispose();
                mat = null;

                List<JToken> objects = jToken.SelectToken("objects").ToList();

                for (int j = 0; j < objects.Count; j++)
                {

                    JToken t = objects[j];
                    int id = Convert.ToInt32(t.SelectToken("class_id").ToString());
                    string type = t.SelectToken("name").ToString();
                    JToken relativecoordinates = t.SelectToken("relativecoordinates");
                    double center_x = (Convert.ToDouble(relativecoordinates.SelectToken("center_x").ToString()) * col_interval) + col_interval;
                    double center_y = (Convert.ToDouble(relativecoordinates.SelectToken("center_y").ToString()) * row_interval) + row_interval;
                    double width = Convert.ToDouble(relativecoordinates.SelectToken("width").ToString()) * image_width;
                    double height = Convert.ToDouble(relativecoordinates.SelectToken("height").ToString()) * image_height;
                    double confidence = Convert.ToDouble(t.SelectToken("confidence").ToString());

                    StructYoloItem item = new StructYoloItem()
                    {
                        ID = id,
                        Type = type,
                        X = Convert.ToInt32(center_x - (width / 2)),
                        Y = Convert.ToInt32(center_y - (height / 2)),
                        Width = Convert.ToInt32(width),
                        Height = Convert.ToInt32(height),
                        Confidence = confidence
                    };

                    items.Add(item);
                }
            }

            return items;
        }

        private static BitmapFrame CreateResizedImage(ImageSource source, int width, int height, int margin)
        {
            var rect = new Rect(margin, margin, width - margin * 2, height - margin * 2);

            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
            group.Children.Add(new ImageDrawing(source, rect));

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
                drawingContext.DrawDrawing(group);

            var resizedImage = new RenderTargetBitmap(
                width, height,         // Resized dimensions
                96, 96,                // Default DPI values
                PixelFormats.Default); // Default pixel format
            resizedImage.Render(drawingVisual);

            return BitmapFrame.Create(resizedImage);
        }

        private bool m_IsOriginImageInfoList = false;

        public void Refresh_ImageList(ImageShowType isType)
        {
            LogManager.Action("Refresh_ImageList() 실행");

            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (m_ImageInfoList_Temp == null)
                    {
                        m_ImageInfoList_Temp = new ObservableCollection<ImageInfo>();
                    }

                    if (!m_IsOriginImageInfoList && isType == ImageShowType.All)
                    {
                        if (m_ImageInfoList.Count > 0)
                        {
                            m_ImageInfoList.ToList().ForEach(x =>
                            {
                                m_ImageInfoList_Temp.Add(x);
                            });

                            m_IsOriginImageInfoList = true;
                        }
                    }

                    m_ImageInfoList.Clear();

                    m_ImageInfoList_Temp.ToList().ForEach(x =>
                    {
                        switch (isType)
                        {
                            case ImageShowType.All:
                                m_ImageInfoList.Add(x);
                                break;
                            case ImageShowType.Labeled:
                                if (x.TrainType == TrainType.LABELED)
                                {
                                    m_ImageInfoList.Add(x);
                                }

                                break;
                            case ImageShowType.Trained:
                                if (x.TrainType == TrainType.TRAINED)
                                {
                                    m_ImageInfoList.Add(x);
                                }

                                break;
                            case ImageShowType.Not_Labeled:
                                if (x.TrainType == TrainType.NONE)
                                {
                                    m_ImageInfoList.Add(x);
                                }

                                break;
                            case ImageShowType.Search:
                                if (x.FileName.Contains(m_SearchFilter))
                                {
                                    m_ImageInfoList.Add(x);
                                }

                                break;
                        }
                    });

                    List<ImageInfo> ob = m_ImageInfoList.ToList().OrderBy(x => x.DateTime).ToList();

                    m_ImageInfoList.Clear();

                    ob.ToList().ForEach(x =>
                    {
                        m_ImageInfoList.Add(x);
                    });

                    ob.Clear();

                    if (Image_Count != null)
                    {
                        Image_Count.Content = m_ImageInfoList.Count + "/" + m_Total_Img_Cnt;
                    }
                }
                catch (Exception e)
                {
                    LogManager.Error(e.Message);
                }
            });
        }

        public void RefreshcharInfo()
        {
            LogManager.Action("RefreshcharInfo() 실행");

            m_LabelList.ToList().ForEach(z =>
            {
                z.LabledCount = 0;
                z.FindedCount = 0;
            });

            int Labelcount = 0, Findcount = 0;

            if (m_IsOriginImageInfoList)
            {
                m_ImageInfoList_Temp.ToList().ForEach(x =>
                {
                    if (x.CharList.Count > 0)
                    {
                        x.CharList.ForEach(y =>
                        {
                            m_LabelList.ToList().ForEach(z =>
                            {
                                if (z.LabelName == y.Labeled_Name)
                                {
                                    z.LabledCount++;
                                    Labelcount++;
                                }

                                if (z.LabelName == y.Finded_Name)
                                {
                                    z.FindedCount++;
                                    Findcount++;
                                }
                            });
                        });
                    }
                });
            }
            else
            {
                m_ImageInfoList.ToList().ForEach(x =>
                {
                    if (x.CharList.Count > 0)
                    {
                        x.CharList.ForEach(y =>
                        {
                            m_LabelList.ToList().ForEach(z =>
                            {
                                if (z.LabelName == y.Labeled_Name)
                                {
                                    z.LabledCount++;
                                    Labelcount++;
                                }

                                if (z.LabelName == y.Finded_Name)
                                {
                                    z.FindedCount++;
                                    Findcount++;
                                }
                            });
                        });
                    }
                });
            }

            //m_LabelList = new ObservableCollection<StructResult>(m_LabelList.OrderBy(x => x.LabelName));

            //dg.ItemsSource = null;
            // dg.ItemsSource = resultList;

            Dispatcher.Invoke(() =>
            {
                NotifyPropertyChanged("LabelList");
                dg.ItemsSource = LabelList;
                dg.UpdateLayout();
                dg.Items.Refresh();
            });
        }

        public void Refresh_ImageList(string TargetChar, CharType type)
        {
            List<List<BitmapImage>> ImageList = new List<List<BitmapImage>>();

            int imageindex = 0;

            m_ImageInfoList.ToList().ForEach(imageInfo =>
            {
                List<BitmapImage> CharList = new List<BitmapImage>();

                imageInfo.CharList.ForEach(x =>
                {
                    if (type == CharType.Labeled)
                    {
                        if (TargetChar == x.Labeled_Name && x.Is_Labeled)
                        {
                            //CharList.Add(x.Labeledcrbmp);
                            CharList.Add(ImageCropToSource(imageInfo.FileName, x.Labeled_X - x.Labeled_Width / 2, x.Labeled_Y - x.Labeled_Height / 2, x.Labeled_Width, x.Labeled_Height));
                        }
                    }
                    else
                    {
                        if (TargetChar == x.Finded_Name && x.Is_Finded)
                        {
                            //CharList.Add(x.Findedcrbmp);
                            if (m_IsFullROI == true)
                            {
                                CharList.Add(ImageCropToSource(imageInfo.FileName, x.Finded_X - x.Finded_Width / 2, x.Finded_Y - x.Finded_Height / 2, x.Finded_Width, x.Finded_Height));
                            }
                            else
                            {
                                CharList.Add(ImageCropToSource(imageInfo.FileName, new Int32Rect((int)((mcurrentROI.Width * x.Finded_X) - mcurrentROI.Width * x.Finded_Width / 2 + mcurrentROI.X)
                                                                                        , (int)((mcurrentROI.Height * x.Finded_Y) - mcurrentROI.Height * x.Finded_Height / 2 + mcurrentROI.Y)
                                                                                        , (int)(mcurrentROI.Width * x.Finded_Width), (int)(mcurrentROI.Height * x.Finded_Height))));
                            }
                        }
                    }
                });

                if (CharList.Count > 0)
                {
                    ImageInfo imginfo = imageInfo;
                    imginfo.IsVisble = true;
                    ImageInfoList.RemoveAt(imageindex);
                    ImageInfoList.Insert(imageindex, imginfo);
                    ImageList.Add(CharList);
                }
                else
                {
                    ImageInfo imginfo = imageInfo;
                    imginfo.IsVisble = false;
                    ImageInfoList.RemoveAt(imageindex);
                    ImageInfoList.Insert(imageindex, imginfo);
                }

                imageindex++;
            }
            );

            // lbImageList.ItemsSource = ImageList;
        }

        public static void SaveClipboardImageToFile(string filePath, BitmapSource bitmapSource)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(fileStream);
                fileStream.Close();
            }
        }

        private void Dg_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dg.SelectedItem != null)
            {
                dp.BitmapImage = null;

                if (dg.CurrentCell.Column.DisplayIndex == 1)
                {
                    Refresh_ImageList(((StructResult)dg.SelectedItem).LabelName, CharType.Labeled);
                }
                else if (dg.CurrentCell.Column.DisplayIndex == 2)
                {
                    Refresh_ImageList(((StructResult)dg.SelectedItem).LabelName, CharType.Finded);
                }
            }
        }

        private void ImageList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                dp.BitmapImage = null;

                m_lbImageList_MouseLeftButtonDown = false;

                if (lbImageList.SelectedItem == null)
                {
                    return;
                }

                m_SelectedImageIndex = -1;
                int visblecount = 0;
                int count = 0;

                PartImageInfoList.ToList().ForEach(x =>
                {
                    if (x.IsVisble)
                    {
                        if (visblecount == lbImageList.SelectedIndex)
                        {
                            m_SelectedImageIndex = count;
                        }

                        visblecount++;
                    }

                    count++;
                });

                if (dp.SelectRectangle != null)
                {
                    if (m_IsTempCharInfo)
                    {
                        m_SelectedImageInfo.CharList.Add(m_TempCharInfo);
                        m_IsTempCharInfo = false;
                    }

                    DrawImageLabel();

                    dp.SelectRectangle = null;
                }

                m_SelectedImageInfo = PartImageInfoList[m_SelectedImageIndex];
                m_OldSelectedImageInfo = m_SelectedImageInfo;

                dp.ConfirmButtonVisibility = Visibility.Collapsed;
                dp.IsShowRectangle = false;
                dp.SelectRectangle = null;
                dp.RemoveSelectRectangle();

                /*
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(m_SelectedImageInfo.FileName);
                bi.EndInit();
                dp.BitmapImage = bi;
                */

                BitmapImage bi = new BitmapImage();
                Mat mat = Cv2.ImRead(m_SelectedImageInfo.FileName);
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                // bi.UriSource = new Uri(imageInfo.FileName);
                bi.StreamSource = mat.ToMemoryStream();
                bi.EndInit();
                mat.Dispose();
                mat = null;

                dp.BitmapImage = bi;

                m_FileName = m_SelectedImageInfo.FileName;

                DrawImageLabel();

                dp.Focus();
            }
            catch (Exception ex)
            {
                LogManager.Error(ex.Message);
            }
        }

        private void DrawImageLabel()
        {
            int index = 0;

            Pen p;

            p = new Pen(Brushes.Yellow, 3);

            dp.Clear();

            if (m_SelectedImageInfo.FileName == null || m_SelectedImageInfo.FileName == "")
            {
                return;
            }

            dp.DrawLabel("File Name", new System.Drawing.Point(5, 5), new FormattedText(m_SelectedImageInfo.FileName, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 15, Brushes.Black, 15), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);

            if (m_IsFullROI == false)
            {
                dp.DrawRectangle("ROI", Brushes.Transparent, p, mcurrentROI.X, mcurrentROI.Y, mcurrentROI.Width, mcurrentROI.Height);
            }

            m_SelectedImageInfo.CharList.ForEach(x =>
            {
                if (x.Is_Finded && x.Is_Labeled)
                {
                    //dp.ShowRectangle(x.X, x.Y, x.Width, x.Height, index, x.Name, Brushes.Blue);
                    //p = new Pen(Brushes.Black, 1);
                    //dp.DrawRectangle("", Brushes.Black, p, (int)(int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2), 100, 50);
                    if (x.Finded_Score * 100 >= Score)
                    {
                        p = new Pen(Brushes.Blue, 3);

                        if (m_IsFullROI)
                        {
                            dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                            //dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(x.Labeled_Y * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2), (int)(x.Labeled_Width * dp.BitmapImage.Width), (int)(x.Labeled_Height * dp.BitmapImage.Height));
                        }
                        else
                        {
                            dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * mcurrentROI.Width - (x.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X, (int)(x.Finded_Y * mcurrentROI.Height - (x.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                            //dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Finded_X * mcurrentROI.Width - (x.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X, (int)(x.Finded_Y * mcurrentROI.Height - (x.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y, (int)(x.Finded_Width * mcurrentROI.Width), (int)(x.Finded_Height * mcurrentROI.Height));
                        }

                        dp.DrawRectangle(x.Labeled_Name, Brushes.Transparent, p, x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2, x.Labeled_Y * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2, x.Labeled_Width * dp.BitmapImage.Width, x.Labeled_Height * dp.BitmapImage.Height);
                    }
                    else
                    {
                        p = new Pen(Brushes.Red, 1);
                        dp.DrawLabel("", new System.Drawing.Point((int)(x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(x.Labeled_Y * dp.BitmapImage.Height + x.Labeled_Height * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Labeled_Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Red, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                        dp.DrawRectangle(x.Labeled_Name, Brushes.Transparent, p, x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2, x.Labeled_Y * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2, x.Labeled_Width * dp.BitmapImage.Width, x.Labeled_Height * dp.BitmapImage.Height);
                    }

                    dp.DrawLabel("", new System.Drawing.Point((int)(x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(x.Labeled_Y * dp.BitmapImage.Height + x.Labeled_Height * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Labeled_Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Red, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                }
                else if (x.Is_Finded)
                {
                    //dp.ShowRectangle(x.X, x.Y, x.Width, x.Height, index, x.Name, Brushes.Lime);
                    if (x.Finded_Score * 100 >= Score)
                    {
                        p = new Pen(Brushes.Lime, 2);

                        if (m_IsFullROI)
                        {
                            dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                            dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2, x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2, x.Finded_Width * dp.BitmapImage.Width, x.Finded_Height * dp.BitmapImage.Height);
                        }
                        else
                        {
                            dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * mcurrentROI.Width - (x.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X, (int)(x.Finded_Y * mcurrentROI.Height - (x.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                            dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Finded_X * mcurrentROI.Width - (x.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X, (int)(x.Finded_Y * mcurrentROI.Height - (x.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y, (int)(x.Finded_Width * mcurrentROI.Width), (int)(x.Finded_Height * mcurrentROI.Height));
                        }
                    }
                    else
                    {
                        p = new Pen(Brushes.Yellow, 2);

                        if (m_IsFullROI)
                        {
                            dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                            dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2, x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2, x.Finded_Width * dp.BitmapImage.Width, x.Finded_Height * dp.BitmapImage.Height);
                        }
                        else
                        {
                            dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * mcurrentROI.Width - (x.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X, (int)(x.Finded_Y * mcurrentROI.Height - (x.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                            dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Finded_X * mcurrentROI.Width - (x.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X, (int)(x.Finded_Y * mcurrentROI.Height - (x.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y, (int)(x.Finded_Width * mcurrentROI.Width), (int)(x.Finded_Height * mcurrentROI.Height));
                        }
                    }
                    // dp.DrawLabel("", new System.Drawing.Point((int)(x.Find_X * dp.BitmapImage.Width - (x.Find_Width * dp.BitmapImage.Width) / 2), (int)(x.Find_Y * dp.BitmapImage.Height + x.Find_Height * dp.BitmapImage.Height - (x.Find_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Find_Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);

                    //dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Finded_X * dp.BitmapImage.Width) - (x.Finded_Width * dp.BitmapImage.Width) / 2, (int)(x.Finded_Y * dp.BitmapImage.Height) - (x.Finded_Height * dp.BitmapImage.Height) / 2, (int)(x.Finded_Width * dp.BitmapImage.Width), (int)(x.Finded_Height * dp.BitmapImage.Height));
                }
                else if (x.Is_Labeled && dp.BitmapImage != null)
                {
                    //dp.ShowRectangle(x.X, x.Y, x.Width, x.Height, index, x.Name, Brushes.Red);

                    p = new Pen(Brushes.Red, 1);
                    dp.DrawLabel("", new System.Drawing.Point((int)(x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(x.Labeled_Y * dp.BitmapImage.Height + x.Labeled_Height * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Labeled_Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Red, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                    dp.DrawRectangle(x.Labeled_Name, Brushes.Transparent, p, x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2, x.Labeled_Y * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2, x.Labeled_Width * dp.BitmapImage.Width, x.Labeled_Height * dp.BitmapImage.Height);
                }

                index++;
            });

            // dp.DrawLabel("", new System.Drawing.Point(5, 20), new FormattedText(m_ImageInfoList[m_SelectedImageIndex].Find_String, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Red, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
        }

        private void DrawImageLabel(List<CharInfo> CharList)
        {
            int index = 0;

            Pen p;

            p = new Pen(Brushes.Yellow, 3);

            dp.Clear();

            dp.DrawLabel("File Name", new System.Drawing.Point(5, 5), new FormattedText(m_ImageInfoList[m_SelectedImageIndex].FileName, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 15, Brushes.Black, 15), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);

            if (m_IsFullROI == false)
            {
                dp.DrawRectangle("ROI", Brushes.Transparent, p, mcurrentROI.X, mcurrentROI.Y, mcurrentROI.Width, mcurrentROI.Height);
            }

            CharList.ForEach(x =>
            {
                if (x.Finded_Name == "A")
                { }

                if (x.Is_Finded && x.Is_Labeled)
                {
                    //dp.ShowRectangle(x.X, x.Y, x.Width, x.Height, index, x.Name, Brushes.Blue);
                    //p = new Pen(Brushes.Black, 1);
                    //dp.DrawRectangle("", Brushes.Black, p, (int)(int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2), 100, 50);
                    if (x.Finded_Score * 100 >= Score)
                    {
                        p = new Pen(Brushes.Blue, 3);

                        if (m_IsFullROI)
                        {
                            dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                            //dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(x.Labeled_Y * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2), (int)(x.Labeled_Width * dp.BitmapImage.Width), (int)(x.Labeled_Height * dp.BitmapImage.Height));
                        }
                        else
                        {
                            dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * mcurrentROI.Width - (x.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X, (int)(x.Finded_Y * mcurrentROI.Height - (x.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                            //dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Finded_X * mcurrentROI.Width - (x.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X, (int)(x.Finded_Y * mcurrentROI.Height - (x.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y, (int)(x.Finded_Width * mcurrentROI.Width), (int)(x.Finded_Height * mcurrentROI.Height));
                        }

                        dp.DrawRectangle(x.Labeled_Name, Brushes.Transparent, p, (int)(x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(x.Labeled_Y * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2), (int)(x.Labeled_Width * dp.BitmapImage.Width), (int)(x.Labeled_Height * dp.BitmapImage.Height));
                    }
                    else
                    {
                        p = new Pen(Brushes.Red, 1);
                        dp.DrawLabel("", new System.Drawing.Point((int)(x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(x.Labeled_Y * dp.BitmapImage.Height + x.Labeled_Height * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Labeled_Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Red, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);

                        dp.DrawRectangle(x.Labeled_Name, Brushes.Transparent, p, (int)(x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(x.Labeled_Y * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2), (int)(x.Labeled_Width * dp.BitmapImage.Width), (int)(x.Labeled_Height * dp.BitmapImage.Height));
                    }

                    dp.DrawLabel("", new System.Drawing.Point((int)(x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(x.Labeled_Y * dp.BitmapImage.Height + x.Labeled_Height * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Labeled_Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Red, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                }
                else if (x.Is_Finded)
                {
                    //dp.ShowRectangle(x.X, x.Y, x.Width, x.Height, index, x.Name, Brushes.Lime);
                    if (x.Finded_Score * 100 >= Score)
                    {
                        p = new Pen(Brushes.Lime, 2);

                        if (m_IsFullROI)
                        {
                            dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                            dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2), (int)(x.Finded_Width * dp.BitmapImage.Width), (int)(x.Finded_Height * dp.BitmapImage.Height));
                        }
                        else
                        {
                            dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * mcurrentROI.Width - (x.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X, (int)(x.Finded_Y * mcurrentROI.Height - (x.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                            dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Finded_X * mcurrentROI.Width - (x.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X, (int)(x.Finded_Y * mcurrentROI.Height - (x.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y, (int)(x.Finded_Width * mcurrentROI.Width), (int)(x.Finded_Height * mcurrentROI.Height));
                        }
                    }
                    else
                    {
                        p = new Pen(Brushes.Yellow, 2);

                        if (m_IsFullROI)
                        {
                            dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                            dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2), (int)(x.Finded_Width * dp.BitmapImage.Width), (int)(x.Finded_Height * dp.BitmapImage.Height));
                        }
                        else
                        {
                            dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * mcurrentROI.Width - (x.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X, (int)(x.Finded_Y * mcurrentROI.Height - (x.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                            dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Finded_X * mcurrentROI.Width - (x.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X, (int)(x.Finded_Y * mcurrentROI.Height - (x.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y, (int)(x.Finded_Width * mcurrentROI.Width), (int)(x.Finded_Height * mcurrentROI.Height));
                        }
                    }
                    // dp.DrawLabel("", new System.Drawing.Point((int)(x.Find_X * dp.BitmapImage.Width - (x.Find_Width * dp.BitmapImage.Width) / 2), (int)(x.Find_Y * dp.BitmapImage.Height + x.Find_Height * dp.BitmapImage.Height - (x.Find_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Find_Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);

                    //dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Finded_X * dp.BitmapImage.Width) - (x.Finded_Width * dp.BitmapImage.Width) / 2, (int)(x.Finded_Y * dp.BitmapImage.Height) - (x.Finded_Height * dp.BitmapImage.Height) / 2, (int)(x.Finded_Width * dp.BitmapImage.Width), (int)(x.Finded_Height * dp.BitmapImage.Height));
                }
                else if (x.Is_Labeled)
                {
                    //dp.ShowRectangle(x.X, x.Y, x.Width, x.Height, index, x.Name, Brushes.Red);

                    p = new Pen(Brushes.Red, 1);
                    dp.DrawLabel("", new System.Drawing.Point((int)(x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(x.Labeled_Y * dp.BitmapImage.Height + x.Labeled_Height * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Labeled_Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Red, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);

                    dp.DrawRectangle(x.Labeled_Name, Brushes.Transparent, p, (int)(x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(x.Labeled_Y * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2), (int)(x.Labeled_Width * dp.BitmapImage.Width), (int)(x.Labeled_Height * dp.BitmapImage.Height));
                    //dp.DrawRectangle(x.Labeled_Name, Brushes.Transparent, p, (int)(x.Labeled_X * dp.BitmapImage.Width), (int)(x.Labeled_Y * dp.BitmapImage.Height), (int)(x.Labeled_Width * dp.BitmapImage.Width), (int)(x.Labeled_Height * dp.BitmapImage.Height));
                }

                index++;
            });

            dp.DrawLabel("", new System.Drawing.Point(5, 20), new FormattedText(m_ImageInfoList[m_SelectedImageIndex].Find_String, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Red, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
        }

        private void DrawImageLabel(List<CharInfo> CharList, ImageInfo imageInfo)
        {
            int index = 0;

            Pen p;

            p = new Pen(Brushes.Yellow, 3);

            dp.Clear();

            dp.DrawLabel("File Name", new System.Drawing.Point(5, 5), new FormattedText(imageInfo.FileName, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 15, Brushes.Black, 15), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);

            if (m_IsFullROI == false)
            {
                dp.DrawRectangle("ROI", Brushes.Transparent, p, mcurrentROI.X, mcurrentROI.Y, mcurrentROI.Width, mcurrentROI.Height);
            }

            CharList.ForEach(x =>
            {
                if (x.Finded_Name == "A")
                { }

                if (x.Is_Finded && x.Is_Labeled)
                {
                    //dp.ShowRectangle(x.X, x.Y, x.Width, x.Height, index, x.Name, Brushes.Blue);
                    //p = new Pen(Brushes.Black, 1);
                    //dp.DrawRectangle("", Brushes.Black, p, (int)(int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2), 100, 50);
                    if (x.Finded_Score * 100 >= Score)
                    {
                        p = new Pen(Brushes.Blue, 3);

                        if (m_IsFullROI)
                        {
                            dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                            //dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(x.Labeled_Y * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2), (int)(x.Labeled_Width * dp.BitmapImage.Width), (int)(x.Labeled_Height * dp.BitmapImage.Height));
                        }
                        else
                        {
                            dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * mcurrentROI.Width - (x.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X, (int)(x.Finded_Y * mcurrentROI.Height - (x.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                            //dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Finded_X * mcurrentROI.Width - (x.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X, (int)(x.Finded_Y * mcurrentROI.Height - (x.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y, (int)(x.Finded_Width * mcurrentROI.Width), (int)(x.Finded_Height * mcurrentROI.Height));
                        }

                        dp.DrawRectangle(x.Labeled_Name, Brushes.Transparent, p, (int)(x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(x.Labeled_Y * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2), (int)(x.Labeled_Width * dp.BitmapImage.Width), (int)(x.Labeled_Height * dp.BitmapImage.Height));
                    }
                    else
                    {
                        p = new Pen(Brushes.Red, 1);
                        dp.DrawLabel("", new System.Drawing.Point((int)(x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(x.Labeled_Y * dp.BitmapImage.Height + x.Labeled_Height * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Labeled_Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Red, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);

                        dp.DrawRectangle(x.Labeled_Name, Brushes.Transparent, p, (int)(x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(x.Labeled_Y * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2), (int)(x.Labeled_Width * dp.BitmapImage.Width), (int)(x.Labeled_Height * dp.BitmapImage.Height));
                    }

                    dp.DrawLabel("", new System.Drawing.Point((int)(x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(x.Labeled_Y * dp.BitmapImage.Height + x.Labeled_Height * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Labeled_Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Red, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                }
                else if (x.Is_Finded)
                {
                    //dp.ShowRectangle(x.X, x.Y, x.Width, x.Height, index, x.Name, Brushes.Lime);
                    if (x.Finded_Score * 100 >= Score)
                    {
                        p = new Pen(Brushes.Lime, 2);

                        if (m_IsFullROI)
                        {
                            dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                            dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2), (int)(x.Finded_Width * dp.BitmapImage.Width), (int)(x.Finded_Height * dp.BitmapImage.Height));
                        }
                        else
                        {
                            dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * mcurrentROI.Width - (x.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X, (int)(x.Finded_Y * mcurrentROI.Height - (x.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                            dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Finded_X * mcurrentROI.Width - (x.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X, (int)(x.Finded_Y * mcurrentROI.Height - (x.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y, (int)(x.Finded_Width * mcurrentROI.Width), (int)(x.Finded_Height * mcurrentROI.Height));
                        }
                    }
                    else
                    {
                        p = new Pen(Brushes.Yellow, 2);

                        if (m_IsFullROI)
                        {
                            dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                            dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2), (int)(x.Finded_Width * dp.BitmapImage.Width), (int)(x.Finded_Height * dp.BitmapImage.Height));
                        }
                        else
                        {
                            dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * mcurrentROI.Width - (x.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X, (int)(x.Finded_Y * mcurrentROI.Height - (x.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                            dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Finded_X * mcurrentROI.Width - (x.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X, (int)(x.Finded_Y * mcurrentROI.Height - (x.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y, (int)(x.Finded_Width * mcurrentROI.Width), (int)(x.Finded_Height * mcurrentROI.Height));
                        }
                    }
                    //dp.DrawLabel("", new System.Drawing.Point((int)(x.Find_X * dp.BitmapImage.Width - (x.Find_Width * dp.BitmapImage.Width) / 2), (int)(x.Find_Y * dp.BitmapImage.Height + x.Find_Height * dp.BitmapImage.Height - (x.Find_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Find_Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);

                    //dp.DrawRectangle(x.Finded_Name, Brushes.Transparent, p, (int)(x.Finded_X * dp.BitmapImage.Width) - (x.Finded_Width * dp.BitmapImage.Width) / 2, (int)(x.Finded_Y * dp.BitmapImage.Height) - (x.Finded_Height * dp.BitmapImage.Height) / 2, (int)(x.Finded_Width * dp.BitmapImage.Width), (int)(x.Finded_Height * dp.BitmapImage.Height));
                }
                else if (x.Is_Labeled)
                {
                    //dp.ShowRectangle(x.X, x.Y, x.Width, x.Height, index, x.Name, Brushes.Red);

                    p = new Pen(Brushes.Red, 1);
                    dp.DrawLabel("", new System.Drawing.Point((int)(x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(x.Labeled_Y * dp.BitmapImage.Height + x.Labeled_Height * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Labeled_Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Red, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);

                    dp.DrawRectangle(x.Labeled_Name, Brushes.Transparent, p, (int)(x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(x.Labeled_Y * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2), (int)(x.Labeled_Width * dp.BitmapImage.Width), (int)(x.Labeled_Height * dp.BitmapImage.Height));
                    //dp.DrawRectangle(x.Labeled_Name, Brushes.Transparent, p, (int)(x.Labeled_X * dp.BitmapImage.Width), (int)(x.Labeled_Y * dp.BitmapImage.Height), (int)(x.Labeled_Width * dp.BitmapImage.Width), (int)(x.Labeled_Height * dp.BitmapImage.Height));
                }

                index++;
            });

            dp.DrawLabel("", new System.Drawing.Point(5, 20), new FormattedText(imageInfo.Find_String, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Red, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
        }

        private void dg_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        public void dgImageList_Drop(object sender, DragEventArgs e)
        {
            #region .cfg File 생성
            // MakecfgFile("Training");
            #endregion

            //CompareCfg(m_ProjectPath + "\\" + m_ProjectName + "_Source.cfg", m_ProjectPath + "\\" + m_ProjectName + ".cfg");

            #region .Data File 생성
            // Train Data 구조체 필요...

            // 기존 데이터 파일 삭제
            if (File.Exists(m_ProjectPath + "\\" + m_ProjectName + ".data") == true)
            {
                File.Delete(m_ProjectPath + "\\" + m_ProjectName + ".data");
            }

            FileStream fileStream = new FileStream(m_ProjectPath + "\\" + m_ProjectName + ".data", FileMode.CreateNew);
            StreamWriter writer = new StreamWriter(fileStream);

            // classes 항목 작성
            writer.WriteLine("classes = " + m_LabelList.Count.ToString());
            // Train 항목 작성
            writer.WriteLine("train = " + m_ProjectPath + "\\train.txt");
            writer.WriteLine("valid = " + m_ProjectPath + "\\valid.txt");
            writer.WriteLine("names = " + m_ProjectPath + "\\" + m_ProjectName + ".names");
            writer.WriteLine("backup = " + m_ProjectPath + "\\Weights");

            writer.Close();
            writer = null;

            fileStream.Close();
            fileStream = null;
            #endregion

            /*
            try
            {
                m_MaxFindCount = int.Parse(Findcount.Text);
            }
            catch
            {
                m_MaxFindCount = 17;
            }
            */

            if (Directory.Exists(m_ProjectPath + "\\Temp\\Finded"))
            {
                Directory.Delete(m_ProjectPath + "\\Temp\\Finded", true);
            }

            Directory.CreateDirectory(m_ProjectPath + "\\Temp\\Finded");

            var list = ((IEnumerable<string>)e.Data.GetData(DataFormats.FileDrop)).ToList();

            int count = 0;

            // 드래그 된 이미지에서 문자 검출 및 학습 리스트에 추가
            list.ForEach(filename =>
            {
                if (filename.ToLower().EndsWith(".bmp") || filename.ToLower().EndsWith(".jpg") || filename.ToLower().EndsWith(".jpeg") || filename.ToLower().EndsWith(".png"))
                {
                    string name = filename.Split('\\')[filename.Split('\\').Length - 1];

                    if (!Directory.Exists(m_ProjectPath + "\\Images\\NotLabeled"))
                    {
                        Directory.CreateDirectory(m_ProjectPath + "\\Images\\NotLabeled");
                    }

                    string new_name = m_ProjectPath + "\\Images\\NotLabeled\\" + name.Substring(0, name.Length - 4) + "_" + Math.Abs(DateTime.Now.ToBinary()).ToString() + name.Substring(name.Length - 4);
                    File.Copy(filename, new_name, true);

                    ImageInfo imageInfo = new ImageInfo(new_name);

                    System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(new_name);
                    imageInfo.Width = bmp.Width;
                    imageInfo.Height = bmp.Height;
                    bmp.Dispose();
                    bmp = null;

                    imageInfo.IsVisble = true;
                    imageInfo.TrainType = TrainType.NONE;

                    if (m_IsOriginImageInfoList)
                    {
                        m_ImageInfoList_Temp.Add(imageInfo);
                    }
                    else
                    {
                        m_ImageInfoList.Add(imageInfo);
                    }

                    Save_ImageList_Information(imageInfo);

                    count++;
                }
                else
                {
                    MessageBox.Show("BMP 형식만 지원합니다.");
                }
            });

            //Refresh_ImageList(ImageShowType.All);
            //Save_ImageList();

            // DrawImageLabel();

            Save_NameFile();

            Load_LabelList();
            RefreshcharInfo();
            Refresh_ImageList(m_ImageShowType);

            if (m_IsOriginImageInfoList)
            {
                LimitPage = m_ImageInfoList_Temp.Count / m_CntPerPage;

                if (m_ImageInfoList_Temp.Count % m_CntPerPage > 0)
                {
                    LimitPage++;
                }
            }
            else
            {
                LimitPage = m_ImageInfoList.Count / m_CntPerPage;

                if (m_ImageInfoList.Count % m_CntPerPage > 0)
                {
                    LimitPage++;
                }
            }

            m_StartPage = m_SelectedPage / m_PagePerGroup * m_PagePerGroup;
            m_EndPage = (m_SelectedPage / m_PagePerGroup * m_PagePerGroup) + m_PagePerGroup;

            if (m_EndPage > LimitPage)
            {
                m_EndPage = LimitPage;
            }

            PartImageInfoList.Clear();

            if (m_SelectedPage == 0)
            {
                m_SelectedPage = 1;
            }

            int _start = m_CntPerPage * (m_SelectedPage - 1);
            int _end = _start + m_CntPerPage;

            if (_end > m_ImageInfoList.Count)
            {
                _end = m_ImageInfoList.Count;
            }

            for (int i = _start; i < _end; i++)
            {
                PartImageInfoList.Add(m_ImageInfoList[i]);
            }

            m_PageStack.Children.RemoveRange(2, m_PageStack.Children.Count - 4);

            for (int i = m_StartPage + 1; i <= m_EndPage; i++)
            {
                TextBlock m_tb = new TextBlock();
                m_tb.Margin = new Thickness(5);
                m_tb.FontSize = 16;
                m_tb.Width = 20;

                if (i >= 100)
                {
                    m_tb.Width = 30;
                }

                m_tb.VerticalAlignment = VerticalAlignment.Center;
                Hyperlink _hyper = new Hyperlink();
                _hyper.Click += ImageInfoListPage_Click;
                TextBlock _tb = new TextBlock();
                _tb.Text = Convert.ToString(i);

                if (i == m_SelectedPage)
                {
                    _tb.FontWeight = FontWeights.UltraBold;
                    _tb.IsEnabled = false;
                    m_OldPageTextBlock = _tb;
                }

                _hyper.Inlines.Add(_tb);
                m_tb.Inlines.Add(_hyper);
                m_PageStack.Children.Insert(m_PageStack.Children.Count - 2, m_tb);
            }

            if (m_IsOriginImageInfoList)
            {
                m_Total_Img_Cnt = m_ImageInfoList_Temp.Count;
                Image_Count.Content = m_ImageInfoList.Count + "/" + m_Total_Img_Cnt;
            }
            else
            {
                m_Total_Img_Cnt = m_ImageInfoList.Count;
                Image_Count.Content = m_ImageInfoList.Count + "/" + m_Total_Img_Cnt;
            }
        }

        private void ImageList_Drop(object sender, DragEventArgs e)
        {
            #region .cfg File 생성
            // MakecfgFile("Training");
            #endregion

            //CompareCfg(m_ProjectPath + "\\" + m_ProjectName + "_Source.cfg", m_ProjectPath + "\\" + m_ProjectName + ".cfg");

            #region .Data File 생성
            // Train Data 구조체 필요...

            // 기존 데이터 파일 삭제
            if (File.Exists(m_ProjectPath + "\\" + m_ProjectName + ".data") == true)
            {
                File.Delete(m_ProjectPath + "\\" + m_ProjectName + ".data");
            }

            FileStream fileStream = new FileStream(m_ProjectPath + "\\" + m_ProjectName + ".data", FileMode.CreateNew);
            StreamWriter writer = new StreamWriter(fileStream);

            // classes 항목 작성
            writer.WriteLine("classes = " + m_LabelList.Count.ToString());
            // Train 항목 작성
            writer.WriteLine("train = " + m_ProjectPath + "\\train.txt");
            writer.WriteLine("valid = " + m_ProjectPath + "\\valid.txt");
            writer.WriteLine("names = " + m_ProjectPath + "\\" + m_ProjectName + ".names");
            writer.WriteLine("backup = " + m_ProjectPath + "\\Weights");

            writer.Close();
            writer = null;

            fileStream.Close();
            fileStream = null;
            #endregion

            /*
            try
            {
                m_MaxFindCount = int.Parse(Findcount.Text);
            }
            catch
            {
                m_MaxFindCount = 17;
            }
            */

            if (Directory.Exists(m_ProjectPath + "\\Temp\\Finded"))
            {
                Directory.Delete(m_ProjectPath + "\\Temp\\Finded", true);
            }

            Directory.CreateDirectory(m_ProjectPath + "\\Temp\\Finded");

            var list = ((IEnumerable<string>)e.Data.GetData(DataFormats.FileDrop)).ToList();

            int count = 0;

            // 드래그 된 이미지에서 문자 검출 및 학습 리스트에 추가
            list.ForEach(filename =>
            {
                if (filename.ToLower().EndsWith(".bmp") || filename.ToLower().EndsWith(".jpg") || filename.ToLower().EndsWith(".jpeg") || filename.ToLower().EndsWith(".png"))
                {
                    string name = filename.Split('\\')[filename.Split('\\').Length - 1];

                    if (!Directory.Exists(m_ProjectPath + "\\Images\\NotLabeled"))
                    {
                        Directory.CreateDirectory(m_ProjectPath + "\\Images\\NotLabeled");
                    }

                    string new_name = m_ProjectPath + "\\Images\\NotLabeled\\" + name.Substring(0, name.Length - 4) + "_" + Math.Abs(DateTime.Now.ToBinary()).ToString() + name.Substring(name.Length - 4);
                    File.Copy(filename, new_name, true);

                    ImageInfo imageInfo = new ImageInfo(new_name);

                    System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(new_name);
                    imageInfo.Width = bmp.Width;
                    imageInfo.Height = bmp.Height;
                    bmp.Dispose();
                    bmp = null;

                    imageInfo.IsVisble = true;
                    imageInfo.TrainType = TrainType.NONE;

                    if (m_IsOriginImageInfoList)
                    {
                        m_ImageInfoList_Temp.Add(imageInfo);
                    }
                    else
                    {
                        m_ImageInfoList.Add(imageInfo);
                    }

                    Save_ImageList_Information(imageInfo);

                    count++;
                }
                else
                {
                    MessageBox.Show("BMP 형식만 지원합니다.");
                }
            });

            //Refresh_ImageList(ImageShowType.All);
            //Save_ImageList();

            // DrawImageLabel();

            Save_NameFile();

            Load_LabelList();
            RefreshcharInfo();
            Refresh_ImageList(m_ImageShowType);

            if (m_IsOriginImageInfoList)
            {
                LimitPage = m_ImageInfoList_Temp.Count / m_CntPerPage;

                if (m_ImageInfoList_Temp.Count % m_CntPerPage > 0)
                {
                    LimitPage++;
                }
            }
            else
            {
                LimitPage = m_ImageInfoList.Count / m_CntPerPage;

                if (m_ImageInfoList.Count % m_CntPerPage > 0)
                {
                    LimitPage++;
                }
            }

            m_StartPage = m_SelectedPage / m_PagePerGroup * m_PagePerGroup;
            m_EndPage = (m_SelectedPage / m_PagePerGroup * m_PagePerGroup) + m_PagePerGroup;

            if (m_EndPage > LimitPage)
            {
                m_EndPage = LimitPage;
            }

            PartImageInfoList.Clear();

            if (m_SelectedPage == 0)
            {
                m_SelectedPage = 1;
            }

            int _start = m_CntPerPage * (m_SelectedPage - 1);
            int _end = _start + m_CntPerPage;

            if (_end > m_ImageInfoList.Count)
            {
                _end = m_ImageInfoList.Count;
            }

            for (int i = _start; i < _end; i++)
            {
                PartImageInfoList.Add(m_ImageInfoList[i]);
            }

            m_PageStack.Children.RemoveRange(2, m_PageStack.Children.Count - 4);

            for (int i = m_StartPage + 1; i <= m_EndPage; i++)
            {
                TextBlock m_tb = new TextBlock();
                m_tb.Margin = new Thickness(5);
                m_tb.FontSize = 16;
                m_tb.Width = 20;

                if (i >= 100)
                {
                    m_tb.Width = 30;
                }

                m_tb.VerticalAlignment = VerticalAlignment.Center;
                Hyperlink _hyper = new Hyperlink();
                _hyper.Click += ImageInfoListPage_Click;
                TextBlock _tb = new TextBlock();
                _tb.Text = Convert.ToString(i);

                if (i == m_SelectedPage)
                {
                    _tb.FontWeight = FontWeights.UltraBold;
                    _tb.IsEnabled = false;
                    m_OldPageTextBlock = _tb;
                }

                _hyper.Inlines.Add(_tb);
                m_tb.Inlines.Add(_hyper);
                m_PageStack.Children.Insert(m_PageStack.Children.Count - 2, m_tb);
            }

            if (m_IsOriginImageInfoList)
            {
                m_Total_Img_Cnt = m_ImageInfoList_Temp.Count;
                Image_Count.Content = m_ImageInfoList.Count + "/" + m_Total_Img_Cnt;
            }
            else
            {
                m_Total_Img_Cnt = m_ImageInfoList.Count;
                Image_Count.Content = m_ImageInfoList.Count + "/" + m_Total_Img_Cnt;
            }
        }

        private void dp_KeyDown(object sender, KeyEventArgs e)
        {
            if (m_IsCtrlPressed)
            {
                if (e.Key == Key.C)
                {
                    if (dp.SelectRectangle != null)
                    {
                        m_CopyRectangle = dp.SelectRectangle;
                    }
                }

                if (e.Key == Key.V)
                {
                    if (m_CopyRectangle != null)
                    {
                        Confirm();

                        dp.ShowRectangle(m_CopyRectangle.StartX + 10, m_CopyRectangle.StartY + 10, m_CopyRectangle.originWidth, m_CopyRectangle.originHeight, m_CopyRectangle.LabeledCharName, Brushes.Red);
                    }
                }
            }
        }

        private void dp_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                dp.canvas.StopMove = true;
                m_IsCtrlPressed = true;
                dp.m_IsCtrlPressed = true;
            }

            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                m_IsShiftPressed = true;
            }

            if (e.Key == Key.S)
            {
                if (m_IsCtrlPressed)
                {
                    Save();
                }
            }

            if (dp.SelectRectangle == null)
            {
                return;
            }

            if (e.Key == Key.Back)
            {
                string s = dp.SelectRectangle.LabeledCharName;

                if (s == null || s == "")
                {
                    return;
                }

                List<char> list = s.ToList();
                list.RemoveAt(list.Count - 1);
                dp.SelectRectangle.LabeledCharName = "";

                for (int i = 0; i < list.Count; i++)
                {
                    dp.SelectRectangle.LabeledCharName += list[i];
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (dp.SelectRectangle != null)
                {
                    if (m_IsCtrlPressed && m_IsShiftPressed)
                    {
                        if (dp.SelectRectangle != null)
                        {
                            int originHeight = dp.SelectRectangle.originHeight;

                            dp.SelectRectangle.originHeight = Convert.ToInt32(dp.SelectRectangle.originHeight + 2);
                            dp.SelectRectangle.MoveYValue += Math.Abs(dp.SelectRectangle.originHeight - originHeight) / 2;
                        }
                    }
                    else if (m_IsCtrlPressed)
                    {
                        dp.SelectRectangle.MoveYValue += m_ShortMoveY;
                    }
                    else if (m_IsShiftPressed)
                    {
                        if (dp.SelectRectangle != null)
                        {
                            // int originWidth = dp.SelectRectangle.originWidth;
                            int originHeight = dp.SelectRectangle.originHeight;

                            dp.SelectRectangle.originHeight = Convert.ToInt32(dp.SelectRectangle.originHeight * 1.1);
                            dp.SelectRectangle.MoveYValue += Math.Abs(dp.SelectRectangle.originHeight - originHeight) / 2;
                        }
                    }
                    else
                    {
                        dp.SelectRectangle.MoveYValue += m_LongMoveY;
                    }

                    DrawImageLabel();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Down)
            {
                if (dp.SelectRectangle != null)
                {
                    if (m_IsCtrlPressed && m_IsShiftPressed)
                    {
                        if (dp.SelectRectangle != null)
                        {
                            int originHeight = dp.SelectRectangle.originHeight;

                            dp.SelectRectangle.originHeight = Convert.ToInt32(dp.SelectRectangle.originHeight - 2);
                            dp.SelectRectangle.MoveYValue -= Math.Abs(dp.SelectRectangle.originHeight - originHeight) / 2;
                        }
                    }
                    else if (m_IsCtrlPressed)
                    {
                        dp.SelectRectangle.MoveYValue -= m_ShortMoveY;
                    }
                    else if (m_IsShiftPressed)
                    {
                        if (dp.SelectRectangle != null)
                        {
                            int originHeight = dp.SelectRectangle.originHeight;

                            dp.SelectRectangle.originHeight = Convert.ToInt32(dp.SelectRectangle.originHeight * 0.9);
                            dp.SelectRectangle.MoveYValue -= Math.Abs(dp.SelectRectangle.originHeight - originHeight) / 2;
                        }
                    }
                    else
                    {
                        dp.SelectRectangle.MoveYValue -= m_LongMoveY;
                    }

                    DrawImageLabel();
                    e.Handled = true;
                }
                else if (dgImageList.SelectedIndex < m_CntPerPage)
                {
                    dgImageList.SelectedIndex--;
                }
            }
            else if (e.Key == Key.Left)
            {
                if (dp.SelectRectangle != null)
                {
                    if (m_IsCtrlPressed && m_IsShiftPressed)
                    {
                        if (dp.SelectRectangle != null)
                        {
                            int originWidth = dp.SelectRectangle.originWidth;

                            dp.SelectRectangle.originWidth = Convert.ToInt32(dp.SelectRectangle.originWidth - 2);
                            dp.SelectRectangle.MoveXValue -= Math.Abs(dp.SelectRectangle.originWidth - originWidth) / 2;
                        }
                    }
                    else if (m_IsCtrlPressed)
                    {
                        dp.SelectRectangle.MoveXValue += m_ShortMoveX;
                    }
                    else if (m_IsShiftPressed)
                    {
                        if (dp.SelectRectangle != null)
                        {
                            int originWidth = dp.SelectRectangle.originWidth;

                            dp.SelectRectangle.originWidth = Convert.ToInt32(dp.SelectRectangle.originWidth * 0.9);
                            dp.SelectRectangle.MoveXValue -= Math.Abs(dp.SelectRectangle.originWidth - originWidth) / 2;
                        }
                    }
                    else
                    {
                        dp.SelectRectangle.MoveXValue += m_LongMoveX;
                    }
                    
                    DrawImageLabel();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Right)
            {
                if (dp.SelectRectangle != null)
                {
                    if (m_IsCtrlPressed && m_IsShiftPressed)
                    {
                        int originWidth = dp.SelectRectangle.originWidth;

                        dp.SelectRectangle.originWidth = Convert.ToInt32(dp.SelectRectangle.originWidth + 2);
                        dp.SelectRectangle.MoveXValue += Math.Abs(dp.SelectRectangle.originWidth - originWidth) / 2;
                    }
                    else if (m_IsCtrlPressed)
                    {
                        dp.SelectRectangle.MoveXValue -= m_ShortMoveX;
                    }
                    else if (m_IsShiftPressed)
                    {
                        int originWidth = dp.SelectRectangle.originWidth;

                        dp.SelectRectangle.originWidth = Convert.ToInt32(dp.SelectRectangle.originWidth * 1.1);
                        dp.SelectRectangle.MoveXValue += Math.Abs(dp.SelectRectangle.originWidth - originWidth) / 2;
                    }
                    else
                    {
                        dp.SelectRectangle.MoveXValue -= m_LongMoveX;
                    }

                    DrawImageLabel();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Delete)
            {
                try
                {
                    dp.RemoveSelectRectangle();
                    dp.IsShowRectangle = false;
                    dp.ConfirmButtonVisibility = Visibility.Collapsed;
                    dp.CancelButtonVisibility = Visibility.Collapsed;

                    dp.SelectRectangle = null;
                    dp.canvas.SelectedElement = null;

                    Delete();
                }
                catch (Exception ex)
                {
                    LogManager.Error(ex.Message);
                }
            }
            else if (e.Key == Key.Enter || e.Key == Key.Return)
            {

            }
            else if (m_IsCtrlPressed)
            {
                int result = 0;
                string s = ((char)KeyInterop.VirtualKeyFromKey(e.Key)).ToString();
                bool isCompleted = int.TryParse(s, out result);

                if (isCompleted)
                {
                    if (result <= m_LabelList.Count)
                    {
                        dp.SelectRectangle.LabeledCharName = m_LabelList[result - 1].LabelName;
                    }
                }
            }
            else if (e.Key == Key.Escape)
            {
                Cancel();
            }
            else
            {
                // dp.SelectRectangle.LabeledCharName += ((char)KeyInterop.VirtualKeyFromKey(e.Key)).ToString();
                e.Handled = true;
            }

            //dp.SelectRectangle.CharName = ((char)((int)e.Key)).ToString();
        }

        private bool m_IsConfirm = false;

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                dp.canvas.StopMove = false;
                m_IsCtrlPressed = false;
                m_IsLeftMouseBtnPressed = false;
                dp.m_IsCtrlPressed = false;
            }
            
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                m_IsShiftPressed = false;
            }
            
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (!m_IsConfirm)
                {
                    Confirm();
                    e.Handled = true;
                }
                else
                {
                    m_IsConfirm = false;
                }
            }

            if (dp.SelectRectangle == null)
            {
                return;
            }
        }

        private ObservableCollection<ImageInfo> m_ImageInfoList_Temp = null;
        private ImageShowType m_ImageShowType = ImageShowType.All;

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_ImageShowType = (ImageShowType)cb_ImageListMode.SelectedIndex;

            Refresh_ImageList(m_ImageShowType);

            LimitPage = m_ImageInfoList.Count / m_CntPerPage;

            if (m_ImageInfoList.Count % m_CntPerPage > 0)
            {
                LimitPage++;
            }

            m_StartPage = 0;

            if (m_LimitPage < m_PagePerGroup)
            {
                m_EndPage = m_LimitPage;
            }
            else
            {
                m_EndPage = m_PagePerGroup;
            }

            if (m_PageStack != null)
            {
                Dispatcher.Invoke(() =>
                {
                    m_PageStack.Children.RemoveRange(2, m_PageStack.Children.Count - 4);

                    PartImageInfoList.Clear();

                    int _start = m_CntPerPage * 0;
                    int _end = _start + m_CntPerPage;

                    if (_end > m_ImageInfoList.Count)
                    {
                        _end = m_ImageInfoList.Count;
                    }

                    for (int i = _start; i < _end; i++)
                    {
                        PartImageInfoList.Add(m_ImageInfoList[i]);
                    }

                    for (int i = m_EndPage; i > m_StartPage; i--)
                    {
                        TextBlock m_tb = new TextBlock();
                        m_tb.Margin = new Thickness(5);
                        m_tb.FontSize = 16;
                        m_tb.Width = 20;

                        if (i >= 100)
                        {
                            m_tb.Width = 30;
                        }

                        m_tb.VerticalAlignment = VerticalAlignment.Center;
                        Hyperlink _hyper = new Hyperlink();
                        _hyper.Click += ImageInfoListPage_Click;
                        TextBlock _tb = new TextBlock();
                        _tb.Text = Convert.ToString(i);

                        if (i == 1)
                        {
                            _tb.FontWeight = FontWeights.UltraBold;
                            _tb.IsEnabled = false;
                            m_OldPageTextBlock = _tb;
                        }

                        _hyper.Inlines.Add(_tb);
                        m_tb.Inlines.Add(_hyper);
                        m_PageStack.Children.Insert(2, m_tb);
                    }
                });
            }
        }

        private void lbImageList_KeyDown(object sender, KeyEventArgs e)
        {
            if (dp.SelectRectangle != null && dp.canvas.SelectedElement != null)
            {
                return;
            }
        }

        private void lbImageList_GotFocus(object sender, RoutedEventArgs e)
        {
            if (dp.SelectRectangle != null && !m_lbImageList_MouseLeftButtonDown)
            {
                e.Handled = false;
                dp.Focus();
                return;
            }
        }

        private void lbImageList_GotKeyboardFocus(object sender, RoutedEventArgs e)
        {

        }

        private void lbImage_GotFocus(object sender, RoutedEventArgs e)
        {
            if (dp.SelectRectangle != null)
            {
                return;
            }
        }

        private void lbImage_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void lbImage_GotKeyboardFocus(object sender, RoutedEventArgs e)
        {

        }

        private void Save_NameFile()
        {
            #region .names File 생성

            FileStream fileStream;
            StreamWriter writer;

            // 기존 데이터 파일 삭제
            if (File.Exists(m_ProjectPath + "\\" + m_ProjectName + ".names") == true)
            {
                File.Delete(m_ProjectPath + "\\" + m_ProjectName + ".names");
            }

            fileStream = new FileStream(m_ProjectPath + "\\" + m_ProjectName + ".names", FileMode.CreateNew);
            writer = new StreamWriter(fileStream);

            // classes 항목 작성
            m_LabelList.ToList().ForEach(x =>
            {
                writer.WriteLine(x.LabelName);
            });

            writer.Close();
            writer = null;

            fileStream.Close();
            fileStream = null;
            #endregion
        }

        private void MakecfgFile(string Mode)
        {
            if (File.Exists(m_ProjectPath + "\\temp.cfg"))
            {
                File.Delete(m_ProjectPath + "\\temp.cfg");
            }

            FileStream fileStream = new FileStream(m_ProjectPath + "\\temp.cfg", FileMode.OpenOrCreate);
            StreamWriter writer = new StreamWriter(fileStream);

            writer.WriteLine("[net]");                                          // 1
            writer.WriteLine("batch=" + NetBatch);                              // 2
            writer.WriteLine("subdivisions=" + NetSubdivisions);                // 3
            writer.WriteLine("width=" + NetWidth);                              // 4
            writer.WriteLine("height=" + NetHeight);                            // 5
            writer.WriteLine("channels=" + NetChannels);                        // 6
            writer.WriteLine("momentum=" + NetMomentum);                        // 7
            writer.WriteLine("decay=" + NetDecay);                              // 8
            writer.WriteLine("angle=" + NetAngle);                              // 9
            writer.WriteLine("saturation=" + NetSaturation);                    // 10
            writer.WriteLine("exposure=" + NetExposure);                        // 11
            writer.WriteLine("hue=" + NetHue);                                  // 12
            writer.WriteLine("");                                               // 13
            writer.WriteLine("learning_rate=" + NetLearningRate);               // 14
            writer.WriteLine("burn_in=" + NetBurnIn);                           // 15
            writer.WriteLine("max_batches = " + NetMaxBatches);                 // 16
            writer.WriteLine("policy=steps");                                   // 17
            writer.WriteLine("steps=" + NetSteps);                              // 18
            writer.WriteLine("scales=" + NetScales);                            // 19
            writer.WriteLine("");                                               // 20
            writer.WriteLine("#cutmix=1");                                      // 21
            writer.WriteLine("mosaic=1");                                       // 22
            writer.WriteLine("");                                               // 23
            writer.WriteLine("#:104x104 54:52x52 85:26x26 104:13x13 for 416");  // 24
            writer.WriteLine("");                                               // 25
            writer.WriteLine("[convolutional]");                                // 26
            writer.WriteLine("batch_normalize=1");                              // 27
            writer.WriteLine("filters=32");                                     // 28
            writer.WriteLine("size=3");                                         // 29
            writer.WriteLine("stride=1");                                       // 30
            writer.WriteLine("pad=1");                                          // 31
            writer.WriteLine("activation=mish");                                // 32
            writer.WriteLine("");                                               // 33
            writer.WriteLine("# Downsample");                                   // 34
            writer.WriteLine("");                                               // 35
            writer.WriteLine("[convolutional]");                                // 36
            writer.WriteLine("batch_normalize=1");                              // 37
            writer.WriteLine("filters=64");                                     // 38
            writer.WriteLine("size=3");                                         // 39
            writer.WriteLine("stride=2");                                       // 40
            writer.WriteLine("pad=1");                                          // 41
            writer.WriteLine("activation=mish");                                // 42
            writer.WriteLine("");                                               // 43
            writer.WriteLine("[convolutional]");                                // 44
            writer.WriteLine("batch_normalize=1");                              // 45
            writer.WriteLine("filters=64");                                     // 46
            writer.WriteLine("size=1");                                         // 47
            writer.WriteLine("stride=1");                                       // 48
            writer.WriteLine("pad=1");                                          // 49
            writer.WriteLine("activation=mish");                                // 50
            writer.WriteLine("");                                               // 51
            writer.WriteLine("[route]");                                        // 52
            writer.WriteLine("layers = -2");                                    // 53
            writer.WriteLine("");                                               // 54
            writer.WriteLine("[convolutional]");                                // 55
            writer.WriteLine("batch_normalize=1");                              // 56
            writer.WriteLine("filters=64");                                     // 57
            writer.WriteLine("size=1");                                         // 58
            writer.WriteLine("stride=1");                                       // 59
            writer.WriteLine("pad=1");                                          // 60
            writer.WriteLine("activation=mish");                                // 61
            writer.WriteLine("");                                               // 62
            writer.WriteLine("[convolutional]");                                // 63
            writer.WriteLine("batch_normalize=1");                              // 64
            writer.WriteLine("filters=32");                                     // 65
            writer.WriteLine("size=1");                                         // 66
            writer.WriteLine("stride=1");                                       // 67
            writer.WriteLine("pad=1");                                          // 68
            writer.WriteLine("activation=mish");                                // 69
            writer.WriteLine("");                                               // 70
            writer.WriteLine("[convolutional]");                                // 71
            writer.WriteLine("batch_normalize=1");                              // 72
            writer.WriteLine("filters=64");                                     // 73
            writer.WriteLine("size=3");                                         // 74
            writer.WriteLine("stride=1");                                       // 75
            writer.WriteLine("pad=1");                                          // 76
            writer.WriteLine("activation=mish");                                // 77
            writer.WriteLine("");                                               // 78
            writer.WriteLine("[shortcut]");                                     // 79
            writer.WriteLine("from=-3");                                        // 80
            writer.WriteLine("activation=linear");                              // 81
            writer.WriteLine("");                                               // 82
            writer.WriteLine("[convolutional]");                                // 83
            writer.WriteLine("batch_normalize=1");                              // 84
            writer.WriteLine("filters=64");                                     // 85
            writer.WriteLine("size=1");                                         // 86
            writer.WriteLine("stride=1");                                       // 87
            writer.WriteLine("pad=1");                                          // 88
            writer.WriteLine("activation=mish");                                // 89
            writer.WriteLine("");                                               // 90
            writer.WriteLine("[route]");                                        // 91
            writer.WriteLine("layers = -1,-7");                                 // 92
            writer.WriteLine("");                                               // 93
            writer.WriteLine("[convolutional]");                                // 94
            writer.WriteLine("batch_normalize=1");                              // 95
            writer.WriteLine("filters=64");                                     // 96
            writer.WriteLine("size=1");                                         // 97
            writer.WriteLine("stride=1");                                       // 98
            writer.WriteLine("pad=1");                                          // 99
            writer.WriteLine("activation=mish");                                // 100
            writer.WriteLine("");                                               // 101
            writer.WriteLine("# Downsample");                                   // 102
            writer.WriteLine("");                                               // 103
            writer.WriteLine("[convolutional]");                                // 104
            writer.WriteLine("batch_normalize=1");                              // 105
            writer.WriteLine("filters=128");                                    // 106
            writer.WriteLine("size=3");                                         // 107
            writer.WriteLine("stride=2");                                       // 108
            writer.WriteLine("pad=1");                                          // 109
            writer.WriteLine("activation=mish");                                // 110
            writer.WriteLine("");                                               // 111
            writer.WriteLine("[convolutional]");                                // 112
            writer.WriteLine("batch_normalize=1");                              // 113
            writer.WriteLine("filters=64");                                     // 114
            writer.WriteLine("size=1");                                         // 115
            writer.WriteLine("stride=1");                                       // 116
            writer.WriteLine("pad=1");                                          // 117
            writer.WriteLine("activation=mish");                                // 118
            writer.WriteLine("");                                               // 119
            writer.WriteLine("[route]");                                        // 120
            writer.WriteLine("layers = -2");                                    // 121
            writer.WriteLine("");                                               // 122
            writer.WriteLine("[convolutional]");                                // 123
            writer.WriteLine("batch_normalize=1");                              // 124
            writer.WriteLine("filters=64");                                     // 125
            writer.WriteLine("size=1");                                         // 126
            writer.WriteLine("stride=1");                                       // 127
            writer.WriteLine("pad=1");                                          // 128
            writer.WriteLine("activation=mish");                                // 129
            writer.WriteLine("");                                               // 130
            writer.WriteLine("[convolutional]");                                // 131
            writer.WriteLine("batch_normalize=1");                              // 132
            writer.WriteLine("filters=64");                                     // 133
            writer.WriteLine("size=1");                                         // 134
            writer.WriteLine("stride=1");                                       // 135
            writer.WriteLine("pad=1");                                          // 136
            writer.WriteLine("activation=mish");                                // 137
            writer.WriteLine("");                                               // 138
            writer.WriteLine("[convolutional]");                                // 139
            writer.WriteLine("batch_normalize=1");                              // 140
            writer.WriteLine("filters=64");                                     // 141
            writer.WriteLine("size=3");                                         // 142
            writer.WriteLine("stride=1");                                       // 143
            writer.WriteLine("pad=1");                                          // 144
            writer.WriteLine("activation=mish");                                // 145
            writer.WriteLine("");                                               // 146
            writer.WriteLine("[shortcut]");                                     // 147
            writer.WriteLine("from=-3");                                        // 148
            writer.WriteLine("activation=linear");                              // 149
            writer.WriteLine("");                                               // 150
            writer.WriteLine("[convolutional]");                                // 151
            writer.WriteLine("batch_normalize=1");                              // 152
            writer.WriteLine("filters=64");                                     // 153
            writer.WriteLine("size=1");                                         // 154
            writer.WriteLine("stride=1");                                       // 155
            writer.WriteLine("pad=1");                                          // 156
            writer.WriteLine("activation=mish");                                // 157
            writer.WriteLine("");                                               // 158
            writer.WriteLine("[convolutional]");                                // 159
            writer.WriteLine("batch_normalize=1");                              // 160
            writer.WriteLine("filters=64");                                     // 161
            writer.WriteLine("size=3");                                         // 162
            writer.WriteLine("stride=1");                                       // 163
            writer.WriteLine("pad=1");                                          // 164
            writer.WriteLine("activation=mish");                                // 165
            writer.WriteLine("");                                               // 166
            writer.WriteLine("[shortcut]");                                     // 167
            writer.WriteLine("from=-3");                                        // 168
            writer.WriteLine("activation=linear");                              // 169
            writer.WriteLine("");                                               // 170
            writer.WriteLine("[convolutional]");                                // 171
            writer.WriteLine("batch_normalize=1");                              // 172
            writer.WriteLine("filters=64");                                     // 173
            writer.WriteLine("size=1");                                         // 174
            writer.WriteLine("stride=1");                                       // 175
            writer.WriteLine("pad=1");                                          // 176
            writer.WriteLine("activation=mish");                                // 177
            writer.WriteLine("");                                               // 178
            writer.WriteLine("[route]");                                        // 179
            writer.WriteLine("layers = -1,-10");                                // 180
            writer.WriteLine("");                                               // 181
            writer.WriteLine("[convolutional]");                                // 182
            writer.WriteLine("batch_normalize=1");                              // 183
            writer.WriteLine("filters=128");                                    // 184
            writer.WriteLine("size=1");                                         // 185
            writer.WriteLine("stride=1");                                       // 186
            writer.WriteLine("pad=1");                                          // 187
            writer.WriteLine("activation=mish");                                // 188
            writer.WriteLine("");                                               // 189
            writer.WriteLine("# Downsample");                                   // 190
            writer.WriteLine("");                                               // 191
            writer.WriteLine("[convolutional]");                                // 192
            writer.WriteLine("batch_normalize=1");                              // 193
            writer.WriteLine("filters=256");                                    // 194
            writer.WriteLine("size=3");                                         // 195
            writer.WriteLine("stride=2");                                       // 196
            writer.WriteLine("pad=1");                                          // 197
            writer.WriteLine("activation=mish");                                // 198
            writer.WriteLine("");                                               // 199
            writer.WriteLine("[convolutional]");                                // 200
            writer.WriteLine("batch_normalize=1");                              // 201
            writer.WriteLine("filters=128");                                    // 202
            writer.WriteLine("size=1");                                         // 203
            writer.WriteLine("stride=1");                                       // 204
            writer.WriteLine("pad=1");                                          // 205
            writer.WriteLine("activation=mish");                                // 206
            writer.WriteLine("");                                               // 207
            writer.WriteLine("[route]");                                        // 208
            writer.WriteLine("layers = -2");                                    // 209
            writer.WriteLine("");                                               // 210
            writer.WriteLine("[convolutional]");                                // 211
            writer.WriteLine("batch_normalize=1");                              // 212
            writer.WriteLine("filters=128");                                    // 213
            writer.WriteLine("size=1");                                         // 214
            writer.WriteLine("stride=1");                                       // 215
            writer.WriteLine("pad=1");                                          // 216
            writer.WriteLine("activation=mish");                                // 217
            writer.WriteLine("");                                               // 218
            writer.WriteLine("[convolutional]");                                // 219
            writer.WriteLine("batch_normalize=1");                              // 220
            writer.WriteLine("filters=128");                                    // 221
            writer.WriteLine("size=1");                                         // 222
            writer.WriteLine("stride=1");                                       // 223
            writer.WriteLine("pad=1");                                          // 224
            writer.WriteLine("activation=mish");                                // 225
            writer.WriteLine("");                                               // 226
            writer.WriteLine("[convolutional]");                                // 227
            writer.WriteLine("batch_normalize=1");                              // 228
            writer.WriteLine("filters=128");                                    // 229
            writer.WriteLine("size=3");                                         // 230
            writer.WriteLine("stride=1");                                       // 231
            writer.WriteLine("pad=1");                                          // 232
            writer.WriteLine("activation=mish");                                // 233
            writer.WriteLine("");                                               // 234
            writer.WriteLine("[shortcut]");                                     // 235
            writer.WriteLine("from=-3");                                        // 236
            writer.WriteLine("activation=linear");                              // 237
            writer.WriteLine("");                                               // 238
            writer.WriteLine("[convolutional]");                                // 239
            writer.WriteLine("batch_normalize=1");                              // 240
            writer.WriteLine("filters=128");                                    // 241
            writer.WriteLine("size=1");                                         // 242
            writer.WriteLine("stride=1");                                       // 243
            writer.WriteLine("pad=1");                                          // 244
            writer.WriteLine("activation=mish");                                // 245
            writer.WriteLine("");                                               // 246
            writer.WriteLine("[convolutional]");                                // 247
            writer.WriteLine("batch_normalize=1");                              // 248
            writer.WriteLine("filters=128");                                    // 249
            writer.WriteLine("size=3");                                         // 250
            writer.WriteLine("stride=1");                                       // 251
            writer.WriteLine("pad=1");                                          // 252
            writer.WriteLine("activation=mish");                                // 253
            writer.WriteLine("");                                               // 254
            writer.WriteLine("[shortcut]");                                     // 255
            writer.WriteLine("from=-3");                                        // 256
            writer.WriteLine("activation=linear");                              // 257
            writer.WriteLine("");                                               // 258
            writer.WriteLine("[convolutional]");                                // 259
            writer.WriteLine("batch_normalize=1");                              // 260
            writer.WriteLine("filters=128");                                    // 261
            writer.WriteLine("size=1");                                         // 262
            writer.WriteLine("stride=1");                                       // 263
            writer.WriteLine("pad=1");                                          // 264
            writer.WriteLine("activation=mish");                                // 265
            writer.WriteLine("");                                               // 266
            writer.WriteLine("[convolutional]");                                // 267
            writer.WriteLine("batch_normalize=1");                              // 268
            writer.WriteLine("filters=128");                                    // 269
            writer.WriteLine("size=3");                                         // 270
            writer.WriteLine("stride=1");                                       // 271
            writer.WriteLine("pad=1");                                          // 272
            writer.WriteLine("activation=mish");                                // 273
            writer.WriteLine("");                                               // 274
            writer.WriteLine("[shortcut]");                                     // 275
            writer.WriteLine("from=-3");                                        // 276
            writer.WriteLine("activation=linear");                              // 277
            writer.WriteLine("");                                               // 278
            writer.WriteLine("[convolutional]");                                // 279
            writer.WriteLine("batch_normalize=1");                              // 280
            writer.WriteLine("filters=128");                                    // 281
            writer.WriteLine("size=1");                                         // 282
            writer.WriteLine("stride=1");                                       // 283
            writer.WriteLine("pad=1");                                          // 284
            writer.WriteLine("activation=mish");                                // 285
            writer.WriteLine("");                                               // 286
            writer.WriteLine("[convolutional]");                                // 287
            writer.WriteLine("batch_normalize=1");                              // 288
            writer.WriteLine("filters=128");                                    // 289
            writer.WriteLine("size=3");                                         // 290
            writer.WriteLine("stride=1");                                       // 291
            writer.WriteLine("pad=1");                                          // 292
            writer.WriteLine("activation=mish");                                // 293
            writer.WriteLine("");                                               // 294
            writer.WriteLine("[shortcut]");                                     // 295
            writer.WriteLine("from=-3");                                        // 296
            writer.WriteLine("activation=linear");                              // 297
            writer.WriteLine("");                                               // 298
            writer.WriteLine("");                                               // 299
            writer.WriteLine("[convolutional]");                                // 300
            writer.WriteLine("batch_normalize=1");                              // 301
            writer.WriteLine("filters=128");                                    // 302
            writer.WriteLine("size=1");                                         // 303
            writer.WriteLine("stride=1");                                       // 304
            writer.WriteLine("pad=1");                                          // 305
            writer.WriteLine("activation=mish");                                // 306
            writer.WriteLine("");                                               // 307
            writer.WriteLine("[convolutional]");                                // 308
            writer.WriteLine("batch_normalize=1");                              // 309
            writer.WriteLine("filters=128");                                    // 310
            writer.WriteLine("size=3");                                         // 311
            writer.WriteLine("stride=1");                                       // 312
            writer.WriteLine("pad=1");                                          // 313
            writer.WriteLine("activation=mish");                                // 314
            writer.WriteLine("");                                               // 315
            writer.WriteLine("[shortcut]");                                     // 316
            writer.WriteLine("from=-3");                                        // 317
            writer.WriteLine("activation=linear");                              // 318
            writer.WriteLine("");                                               // 319
            writer.WriteLine("[convolutional]");                                // 320
            writer.WriteLine("batch_normalize=1");                              // 321
            writer.WriteLine("filters=128");                                    // 322
            writer.WriteLine("size=1");                                         // 323
            writer.WriteLine("stride=1");                                       // 324
            writer.WriteLine("pad=1");                                          // 325
            writer.WriteLine("activation=mish");                                // 326
            writer.WriteLine("");                                               // 327
            writer.WriteLine("[convolutional]");                                // 328
            writer.WriteLine("batch_normalize=1");                              // 329
            writer.WriteLine("filters=128");                                    // 330
            writer.WriteLine("size=3");                                         // 331
            writer.WriteLine("stride=1");                                       // 332
            writer.WriteLine("pad=1");                                          // 333
            writer.WriteLine("activation=mish");                                // 334
            writer.WriteLine("");                                               // 335
            writer.WriteLine("[shortcut]");                                     // 336
            writer.WriteLine("from=-3");                                        // 337
            writer.WriteLine("activation=linear");                              // 338
            writer.WriteLine("");                                               // 339
            writer.WriteLine("[convolutional]");                                // 340
            writer.WriteLine("batch_normalize=1");                              // 341
            writer.WriteLine("filters=128");                                    // 342
            writer.WriteLine("size=1");                                         // 343
            writer.WriteLine("stride=1");                                       // 344
            writer.WriteLine("pad=1");                                          // 345
            writer.WriteLine("activation=mish");                                // 346
            writer.WriteLine("");                                               // 347
            writer.WriteLine("[convolutional]");                                // 348
            writer.WriteLine("batch_normalize=1");                              // 349
            writer.WriteLine("filters=128");                                    // 350
            writer.WriteLine("size=3");                                         // 351
            writer.WriteLine("stride=1");                                       // 352
            writer.WriteLine("pad=1");                                          // 353
            writer.WriteLine("activation=mish");                                // 354
            writer.WriteLine("");                                               // 355
            writer.WriteLine("[shortcut]");                                     // 356
            writer.WriteLine("from=-3");                                        // 357
            writer.WriteLine("activation=linear");                              // 358
            writer.WriteLine("");                                               // 359
            writer.WriteLine("[convolutional]");                                // 360
            writer.WriteLine("batch_normalize=1");                              // 361
            writer.WriteLine("filters=128");                                    // 362
            writer.WriteLine("size=1");                                         // 363
            writer.WriteLine("stride=1");                                       // 364
            writer.WriteLine("pad=1");                                          // 365
            writer.WriteLine("activation=mish");                                // 366
            writer.WriteLine("");                                               // 367
            writer.WriteLine("[convolutional]");                                // 368
            writer.WriteLine("batch_normalize=1");                              // 369
            writer.WriteLine("filters=128");                                    // 370
            writer.WriteLine("size=3");                                         // 371
            writer.WriteLine("stride=1");                                       // 372
            writer.WriteLine("pad=1");                                          // 373
            writer.WriteLine("activation=mish");                                // 374
            writer.WriteLine("");                                               // 375
            writer.WriteLine("[shortcut]");                                     // 376
            writer.WriteLine("from=-3");                                        // 377
            writer.WriteLine("activation=linear");                              // 378
            writer.WriteLine("");                                               // 379
            writer.WriteLine("[convolutional]");                                // 380
            writer.WriteLine("batch_normalize=1");                              // 381
            writer.WriteLine("filters=128");                                    // 382
            writer.WriteLine("size=1");                                         // 383
            writer.WriteLine("stride=1");                                       // 384
            writer.WriteLine("pad=1");                                          // 385
            writer.WriteLine("activation=mish");                                // 386
            writer.WriteLine("");                                               // 387
            writer.WriteLine("[route]");                                        // 388
            writer.WriteLine("layers = -1,-28");                                // 389
            writer.WriteLine("");                                               // 390
            writer.WriteLine("[convolutional]");                                // 391
            writer.WriteLine("batch_normalize=1");                              // 392
            writer.WriteLine("filters=256");                                    // 393
            writer.WriteLine("size=1");                                         // 394
            writer.WriteLine("stride=1");                                       // 395
            writer.WriteLine("pad=1");                                          // 396
            writer.WriteLine("activation=mish");                                // 397
            writer.WriteLine("");                                               // 398
            writer.WriteLine("# Downsample");                                   // 399
            writer.WriteLine("");                                               // 400
            writer.WriteLine("[convolutional]");                                // 401
            writer.WriteLine("batch_normalize=1");                              // 402
            writer.WriteLine("filters=512");                                    // 403
            writer.WriteLine("size=3");                                         // 404
            writer.WriteLine("stride=2");                                       // 405
            writer.WriteLine("pad=1");                                          // 406
            writer.WriteLine("activation=mish");                                // 407
            writer.WriteLine("");                                               // 408
            writer.WriteLine("[convolutional]");                                // 409
            writer.WriteLine("batch_normalize=1");                              // 410
            writer.WriteLine("filters=256");                                    // 411
            writer.WriteLine("size=1");                                         // 412
            writer.WriteLine("stride=1");                                       // 413
            writer.WriteLine("pad=1");                                          // 414
            writer.WriteLine("activation=mish");                                // 415
            writer.WriteLine("");                                               // 416
            writer.WriteLine("[route]");                                        // 417
            writer.WriteLine("layers = -2");                                    // 418
            writer.WriteLine("");                                               // 419
            writer.WriteLine("[convolutional]");                                // 420
            writer.WriteLine("batch_normalize=1");                              // 421
            writer.WriteLine("filters=256");                                    // 422
            writer.WriteLine("size=1");                                         // 423
            writer.WriteLine("stride=1");                                       // 424
            writer.WriteLine("pad=1");                                          // 425
            writer.WriteLine("activation=mish");                                // 426
            writer.WriteLine("");                                               // 427
            writer.WriteLine("[convolutional]");                                // 428
            writer.WriteLine("batch_normalize=1");                              // 429
            writer.WriteLine("filters=256");                                    // 430
            writer.WriteLine("size=1");                                         // 431
            writer.WriteLine("stride=1");                                       // 432
            writer.WriteLine("pad=1");                                          // 433
            writer.WriteLine("activation=mish");                                // 434
            writer.WriteLine("");                                               // 435
            writer.WriteLine("[convolutional]");                                // 436
            writer.WriteLine("batch_normalize=1");                              // 437
            writer.WriteLine("filters=256");                                    // 438
            writer.WriteLine("size=3");                                         // 439
            writer.WriteLine("stride=1");                                       // 440
            writer.WriteLine("pad=1");                                          // 441
            writer.WriteLine("activation=mish");                                // 442
            writer.WriteLine("");                                               // 443
            writer.WriteLine("[shortcut]");                                     // 444
            writer.WriteLine("from=-3");                                        // 445
            writer.WriteLine("activation=linear");                              // 446
            writer.WriteLine("");                                               // 447
            writer.WriteLine("");                                               // 448
            writer.WriteLine("[convolutional]");                                // 449
            writer.WriteLine("batch_normalize=1");                              // 450
            writer.WriteLine("filters=256");                                    // 451
            writer.WriteLine("size=1");                                         // 452
            writer.WriteLine("stride=1");                                       // 453
            writer.WriteLine("pad=1");                                          // 454
            writer.WriteLine("activation=mish");                                // 455
            writer.WriteLine("");                                               // 456
            writer.WriteLine("[convolutional]");                                // 457
            writer.WriteLine("batch_normalize=1");                              // 458
            writer.WriteLine("filters=256");                                    // 459
            writer.WriteLine("size=3");                                         // 460
            writer.WriteLine("stride=1");                                       // 461
            writer.WriteLine("pad=1");                                          // 462
            writer.WriteLine("activation=mish");                                // 463
            writer.WriteLine("");                                               // 464
            writer.WriteLine("[shortcut]");                                     // 465
            writer.WriteLine("from=-3");                                        // 466
            writer.WriteLine("activation=linear");                              // 467
            writer.WriteLine("");                                               // 468
            writer.WriteLine("");                                               // 469
            writer.WriteLine("[convolutional]");                                // 470
            writer.WriteLine("batch_normalize=1");                              // 471
            writer.WriteLine("filters=256");                                    // 472
            writer.WriteLine("size=1");                                         // 473
            writer.WriteLine("stride=1");                                       // 474
            writer.WriteLine("pad=1");                                          // 475
            writer.WriteLine("activation=mish");                                // 476
            writer.WriteLine("");                                               // 477
            writer.WriteLine("[convolutional]");                                // 478
            writer.WriteLine("batch_normalize=1");                              // 479
            writer.WriteLine("filters=256");                                    // 480
            writer.WriteLine("size=3");                                         // 481
            writer.WriteLine("stride=1");                                       // 482
            writer.WriteLine("pad=1");                                          // 483
            writer.WriteLine("activation=mish");                                // 484
            writer.WriteLine("");                                               // 485
            writer.WriteLine("[shortcut]");                                     // 486
            writer.WriteLine("from=-3");                                        // 487
            writer.WriteLine("activation=linear");                              // 488
            writer.WriteLine("");                                               // 489
            writer.WriteLine("");                                               // 490
            writer.WriteLine("[convolutional]");                                // 491
            writer.WriteLine("batch_normalize=1");                              // 492
            writer.WriteLine("filters=256");                                    // 493
            writer.WriteLine("size=1");                                         // 494
            writer.WriteLine("stride=1");                                       // 495
            writer.WriteLine("pad=1");                                          // 496
            writer.WriteLine("activation=mish");                                // 497
            writer.WriteLine("");                                               // 498
            writer.WriteLine("[convolutional]");                                // 499
            writer.WriteLine("batch_normalize=1");                              // 500
            writer.WriteLine("filters=256");                                    // 501
            writer.WriteLine("size=3");                                         // 502
            writer.WriteLine("stride=1");                                       // 503
            writer.WriteLine("pad=1");                                          // 504
            writer.WriteLine("activation=mish");                                // 505
            writer.WriteLine("");                                               // 506
            writer.WriteLine("[shortcut]");                                     // 507
            writer.WriteLine("from=-3");                                        // 508
            writer.WriteLine("activation=linear");                              // 509
            writer.WriteLine("");                                               // 510
            writer.WriteLine("");                                               // 511
            writer.WriteLine("[convolutional]");                                // 512
            writer.WriteLine("batch_normalize=1");                              // 513
            writer.WriteLine("filters=256");                                    // 514
            writer.WriteLine("size=1");                                         // 515
            writer.WriteLine("stride=1");                                       // 516
            writer.WriteLine("pad=1");                                          // 517
            writer.WriteLine("activation=mish");                                // 518
            writer.WriteLine("");                                               // 519
            writer.WriteLine("[convolutional]");                                // 520
            writer.WriteLine("batch_normalize=1");                              // 521
            writer.WriteLine("filters=256");                                    // 522
            writer.WriteLine("size=3");                                         // 523
            writer.WriteLine("stride=1");                                       // 524
            writer.WriteLine("pad=1");                                          // 525
            writer.WriteLine("activation=mish");                                // 526
            writer.WriteLine("");                                               // 527
            writer.WriteLine("[shortcut]");                                     // 528
            writer.WriteLine("from=-3");                                        // 529
            writer.WriteLine("activation=linear");                              // 530
            writer.WriteLine("");                                               // 531
            writer.WriteLine("");                                               // 532
            writer.WriteLine("[convolutional]");                                // 533
            writer.WriteLine("batch_normalize=1");                              // 534
            writer.WriteLine("filters=256");                                    // 535
            writer.WriteLine("size=1");                                         // 536
            writer.WriteLine("stride=1");                                       // 537
            writer.WriteLine("pad=1");                                          // 538
            writer.WriteLine("activation=mish");                                // 539
            writer.WriteLine("");                                               // 540
            writer.WriteLine("[convolutional]");                                // 541
            writer.WriteLine("batch_normalize=1");                              // 542
            writer.WriteLine("filters=256");                                    // 543
            writer.WriteLine("size=3");                                         // 544
            writer.WriteLine("stride=1");                                       // 545
            writer.WriteLine("pad=1");                                          // 546
            writer.WriteLine("activation=mish");                                // 547
            writer.WriteLine("");                                               // 548
            writer.WriteLine("[shortcut]");                                     // 549
            writer.WriteLine("from=-3");                                        // 550
            writer.WriteLine("activation=linear");                              // 551
            writer.WriteLine("");                                               // 552
            writer.WriteLine("");                                               // 553
            writer.WriteLine("[convolutional]");                                // 554
            writer.WriteLine("batch_normalize=1");                              // 555
            writer.WriteLine("filters=256");                                    // 556
            writer.WriteLine("size=1");                                         // 557
            writer.WriteLine("stride=1");                                       // 558
            writer.WriteLine("pad=1");                                          // 559
            writer.WriteLine("activation=mish");                                // 560
            writer.WriteLine("");                                               // 561
            writer.WriteLine("[convolutional]");                                // 562
            writer.WriteLine("batch_normalize=1");                              // 563
            writer.WriteLine("filters=256");                                    // 564
            writer.WriteLine("size=3");                                         // 565
            writer.WriteLine("stride=1");                                       // 566
            writer.WriteLine("pad=1");                                          // 567
            writer.WriteLine("activation=mish");                                // 568
            writer.WriteLine("");                                               // 569
            writer.WriteLine("[shortcut]");                                     // 570
            writer.WriteLine("from=-3");                                        // 571
            writer.WriteLine("activation=linear");                              // 572
            writer.WriteLine("");                                               // 573
            writer.WriteLine("[convolutional]");                                // 574
            writer.WriteLine("batch_normalize=1");                              // 575
            writer.WriteLine("filters=256");                                    // 576
            writer.WriteLine("size=1");                                         // 577
            writer.WriteLine("stride=1");                                       // 578
            writer.WriteLine("pad=1");                                          // 579
            writer.WriteLine("activation=mish");                                // 580
            writer.WriteLine("");                                               // 581
            writer.WriteLine("[convolutional]");                                // 582
            writer.WriteLine("batch_normalize=1");                              // 583
            writer.WriteLine("filters=256");                                    // 584
            writer.WriteLine("size=3");                                         // 585
            writer.WriteLine("stride=1");                                       // 586
            writer.WriteLine("pad=1");                                          // 587
            writer.WriteLine("activation=mish");                                // 588
            writer.WriteLine("");                                               // 589
            writer.WriteLine("[shortcut]");                                     // 590
            writer.WriteLine("from=-3");                                        // 591
            writer.WriteLine("activation=linear");                              // 592
            writer.WriteLine("");                                               // 593
            writer.WriteLine("[convolutional]");                                // 594
            writer.WriteLine("batch_normalize=1");                              // 595
            writer.WriteLine("filters=256");                                    // 596
            writer.WriteLine("size=1");                                         // 597
            writer.WriteLine("stride=1");                                       // 598
            writer.WriteLine("pad=1");                                          // 599
            writer.WriteLine("activation=mish");                                // 600
            writer.WriteLine("");                                               // 601
            writer.WriteLine("[route]");                                        // 602
            writer.WriteLine("layers = -1,-28");                                // 603
            writer.WriteLine("");                                               // 604
            writer.WriteLine("[convolutional]");                                // 605
            writer.WriteLine("batch_normalize=1");                              // 606
            writer.WriteLine("filters=512");                                    // 607
            writer.WriteLine("size=1");                                         // 608
            writer.WriteLine("stride=1");                                       // 609
            writer.WriteLine("pad=1");                                          // 610
            writer.WriteLine("activation=mish");                                // 611
            writer.WriteLine("");                                               // 612
            writer.WriteLine("# Downsample");                                   // 613
            writer.WriteLine("");                                               // 614
            writer.WriteLine("[convolutional]");                                // 615
            writer.WriteLine("batch_normalize=1");                              // 616
            writer.WriteLine("filters=1024");                                   // 617
            writer.WriteLine("size=3");                                         // 618
            writer.WriteLine("stride=2");                                       // 619
            writer.WriteLine("pad=1");                                          // 620
            writer.WriteLine("activation=mish");                                // 621
            writer.WriteLine("");                                               // 622
            writer.WriteLine("[convolutional]");                                // 623
            writer.WriteLine("batch_normalize=1");                              // 624
            writer.WriteLine("filters=512");                                    // 625
            writer.WriteLine("size=1");                                         // 626
            writer.WriteLine("stride=1");                                       // 627
            writer.WriteLine("pad=1");                                          // 628
            writer.WriteLine("activation=mish");                                // 629
            writer.WriteLine("");                                               // 630
            writer.WriteLine("[route]");                                        // 631
            writer.WriteLine("layers = -2");                                    // 632
            writer.WriteLine("");                                               // 633
            writer.WriteLine("[convolutional]");                                // 634
            writer.WriteLine("batch_normalize=1");                              // 635
            writer.WriteLine("filters=512");                                    // 636
            writer.WriteLine("size=1");                                         // 637
            writer.WriteLine("stride=1");                                       // 638
            writer.WriteLine("pad=1");                                          // 639
            writer.WriteLine("activation=mish");                                // 640
            writer.WriteLine("");                                               // 641
            writer.WriteLine("[convolutional]");                                // 642
            writer.WriteLine("batch_normalize=1");                              // 643
            writer.WriteLine("filters=512");                                    // 644
            writer.WriteLine("size=1");                                         // 645
            writer.WriteLine("stride=1");                                       // 646
            writer.WriteLine("pad=1");                                          // 647
            writer.WriteLine("activation=mish");                                // 648
            writer.WriteLine("");                                               // 649
            writer.WriteLine("[convolutional]");                                // 650
            writer.WriteLine("batch_normalize=1");                              // 651
            writer.WriteLine("filters=512");                                    // 652
            writer.WriteLine("size=3");                                         // 653
            writer.WriteLine("stride=1");                                       // 654
            writer.WriteLine("pad=1");                                          // 655
            writer.WriteLine("activation=mish");                                // 656
            writer.WriteLine("");                                               // 657
            writer.WriteLine("[shortcut]");                                     // 658
            writer.WriteLine("from=-3");                                        // 659
            writer.WriteLine("activation=linear");                              // 660
            writer.WriteLine("");                                               // 661
            writer.WriteLine("[convolutional]");                                // 662
            writer.WriteLine("batch_normalize=1");                              // 663
            writer.WriteLine("filters=512");                                    // 664
            writer.WriteLine("size=1");                                         // 665
            writer.WriteLine("stride=1");                                       // 666
            writer.WriteLine("pad=1");                                          // 667
            writer.WriteLine("activation=mish");                                // 668
            writer.WriteLine("");                                               // 669
            writer.WriteLine("[convolutional]");                                // 670
            writer.WriteLine("batch_normalize=1");                              // 671
            writer.WriteLine("filters=512");                                    // 672
            writer.WriteLine("size=3");                                         // 673
            writer.WriteLine("stride=1");                                       // 674
            writer.WriteLine("pad=1");                                          // 675
            writer.WriteLine("activation=mish");                                // 676
            writer.WriteLine("");                                               // 677
            writer.WriteLine("[shortcut]");                                     // 678
            writer.WriteLine("from=-3");                                        // 679
            writer.WriteLine("activation=linear");                              // 680
            writer.WriteLine("");                                               // 681
            writer.WriteLine("[convolutional]");                                // 682
            writer.WriteLine("batch_normalize=1");                              // 683
            writer.WriteLine("filters=512");                                    // 684
            writer.WriteLine("size=1");                                         // 685
            writer.WriteLine("stride=1");                                       // 686
            writer.WriteLine("pad=1");                                          // 687
            writer.WriteLine("activation=mish");                                // 688
            writer.WriteLine("");                                               // 689
            writer.WriteLine("[convolutional]");                                // 690
            writer.WriteLine("batch_normalize=1");                              // 691
            writer.WriteLine("filters=512");                                    // 692
            writer.WriteLine("size=3");                                         // 693
            writer.WriteLine("stride=1");                                       // 694
            writer.WriteLine("pad=1");                                          // 695
            writer.WriteLine("activation=mish");                                // 696
            writer.WriteLine("");                                               // 697
            writer.WriteLine("[shortcut]");                                     // 698
            writer.WriteLine("from=-3");                                        // 699
            writer.WriteLine("activation=linear");                              // 700
            writer.WriteLine("");                                               // 701
            writer.WriteLine("[convolutional]");                                // 702
            writer.WriteLine("batch_normalize=1");                              // 703
            writer.WriteLine("filters=512");                                    // 704
            writer.WriteLine("size=1");                                         // 705
            writer.WriteLine("stride=1");                                       // 706
            writer.WriteLine("pad=1");                                          // 707
            writer.WriteLine("activation=mish");                                // 708
            writer.WriteLine("");                                               // 709
            writer.WriteLine("[convolutional]");                                // 710
            writer.WriteLine("batch_normalize=1");                              // 711
            writer.WriteLine("filters=512");                                    // 712
            writer.WriteLine("size=3");                                         // 713
            writer.WriteLine("stride=1");                                       // 714
            writer.WriteLine("pad=1");                                          // 715
            writer.WriteLine("activation=mish");                                // 716
            writer.WriteLine("");                                               // 717
            writer.WriteLine("[shortcut]");                                     // 718
            writer.WriteLine("from=-3");                                        // 719
            writer.WriteLine("activation=linear");                              // 720
            writer.WriteLine("");                                               // 721
            writer.WriteLine("[convolutional]");                                // 722
            writer.WriteLine("batch_normalize=1");                              // 723
            writer.WriteLine("filters=512");                                    // 724
            writer.WriteLine("size=1");                                         // 725
            writer.WriteLine("stride=1");                                       // 726
            writer.WriteLine("pad=1");                                          // 727
            writer.WriteLine("activation=mish");                                // 728
            writer.WriteLine("");                                               // 729
            writer.WriteLine("[route]");                                        // 730
            writer.WriteLine("layers = -1,-16");                                // 731
            writer.WriteLine("");                                               // 732
            writer.WriteLine("[convolutional]");                                // 733
            writer.WriteLine("batch_normalize=1");                              // 734
            writer.WriteLine("filters=1024");                                   // 735
            writer.WriteLine("size=1");                                         // 736
            writer.WriteLine("stride=1");                                       // 737
            writer.WriteLine("pad=1");                                          // 738
            writer.WriteLine("activation=mish");                                // 739
            writer.WriteLine("");                                               // 740
            writer.WriteLine("##########################");                     // 741
            writer.WriteLine("");                                               // 742
            writer.WriteLine("[convolutional]");                                // 743
            writer.WriteLine("batch_normalize=1");                              // 744
            writer.WriteLine("filters=512");                                    // 745
            writer.WriteLine("size=1");                                         // 746
            writer.WriteLine("stride=1");                                       // 747
            writer.WriteLine("pad=1");                                          // 748
            writer.WriteLine("activation=leaky");                               // 749
            writer.WriteLine("");                                               // 750
            writer.WriteLine("[convolutional]");                                // 751
            writer.WriteLine("batch_normalize=1");                              // 752
            writer.WriteLine("size=3");                                         // 753
            writer.WriteLine("stride=1");                                       // 754
            writer.WriteLine("pad=1");                                          // 755
            writer.WriteLine("filters=1024");                                   // 756
            writer.WriteLine("activation=leaky");                               // 757
            writer.WriteLine("");                                               // 758
            writer.WriteLine("[convolutional]");                                // 759
            writer.WriteLine("batch_normalize=1");                              // 760
            writer.WriteLine("filters=512");                                    // 761
            writer.WriteLine("size=1");                                         // 762
            writer.WriteLine("stride=1");                                       // 763
            writer.WriteLine("pad=1");                                          // 764
            writer.WriteLine("activation=leaky");                               // 765
            writer.WriteLine("");                                               // 766
            writer.WriteLine("### SPP ###");                                    // 767
            writer.WriteLine("[maxpool]");                                      // 768
            writer.WriteLine("stride=1");                                       // 769
            writer.WriteLine("size=5");                                         // 770
            writer.WriteLine("");                                               // 771
            writer.WriteLine("[route]");                                        // 772
            writer.WriteLine("layers=-2");                                      // 773
            writer.WriteLine("");                                               // 774
            writer.WriteLine("[maxpool]");                                      // 775
            writer.WriteLine("stride=1");                                       // 776
            writer.WriteLine("size=9");                                         // 777
            writer.WriteLine("");                                               // 778
            writer.WriteLine("[route]");                                        // 779
            writer.WriteLine("layers=-4");                                      // 780
            writer.WriteLine("");                                               // 781
            writer.WriteLine("[maxpool]");                                      // 782
            writer.WriteLine("stride=1");                                       // 783
            writer.WriteLine("size=13");                                        // 784
            writer.WriteLine("");                                               // 785
            writer.WriteLine("[route]");                                        // 786
            writer.WriteLine("layers=-1,-3,-5,-6");                             // 787
            writer.WriteLine("### End SPP ###");                                // 788
            writer.WriteLine("");                                               // 789
            writer.WriteLine("[convolutional]");                                // 790
            writer.WriteLine("batch_normalize=1");                              // 791
            writer.WriteLine("filters=512");                                    // 792
            writer.WriteLine("size=1");                                         // 793
            writer.WriteLine("stride=1");                                       // 794
            writer.WriteLine("pad=1");                                          // 795
            writer.WriteLine("activation=leaky");                               // 796
            writer.WriteLine("");                                               // 797
            writer.WriteLine("[convolutional]");                                // 798
            writer.WriteLine("batch_normalize=1");                              // 799
            writer.WriteLine("size=3");                                         // 800
            writer.WriteLine("stride=1");                                       // 801
            writer.WriteLine("pad=1");                                          // 802
            writer.WriteLine("filters=1024");                                   // 803
            writer.WriteLine("activation=leaky");                               // 804
            writer.WriteLine("");                                               // 805
            writer.WriteLine("[convolutional]");                                // 806
            writer.WriteLine("batch_normalize=1");                              // 807
            writer.WriteLine("filters=512");                                    // 808
            writer.WriteLine("size=1");                                         // 809
            writer.WriteLine("stride=1");                                       // 810
            writer.WriteLine("pad=1");                                          // 811
            writer.WriteLine("activation=leaky");                               // 812
            writer.WriteLine("");                                               // 813
            writer.WriteLine("[convolutional]");                                // 814
            writer.WriteLine("batch_normalize=1");                              // 815
            writer.WriteLine("filters=256");                                    // 816
            writer.WriteLine("size=1");                                         // 817
            writer.WriteLine("stride=1");                                       // 818
            writer.WriteLine("pad=1");                                          // 819
            writer.WriteLine("activation=leaky");                               // 820
            writer.WriteLine("");                                               // 821
            writer.WriteLine("[upsample]");                                     // 822
            writer.WriteLine("stride=2");                                       // 823
            writer.WriteLine("");                                               // 824
            writer.WriteLine("[route]");                                        // 825
            writer.WriteLine("layers = 85");                                    // 826
            writer.WriteLine("");                                               // 827
            writer.WriteLine("[convolutional]");                                // 828
            writer.WriteLine("batch_normalize=1");                              // 829
            writer.WriteLine("filters=256");                                    // 830
            writer.WriteLine("size=1");                                         // 831
            writer.WriteLine("stride=1");                                       // 832
            writer.WriteLine("pad=1");                                          // 833
            writer.WriteLine("activation=leaky");                               // 834
            writer.WriteLine("");                                               // 835
            writer.WriteLine("[route]");                                        // 836
            writer.WriteLine("layers = -1, -3");                                // 837
            writer.WriteLine("");                                               // 838
            writer.WriteLine("[convolutional]");                                // 839
            writer.WriteLine("batch_normalize=1");                              // 840
            writer.WriteLine("filters=256");                                    // 841
            writer.WriteLine("size=1");                                         // 842
            writer.WriteLine("stride=1");                                       // 843
            writer.WriteLine("pad=1");                                          // 844
            writer.WriteLine("activation=leaky");                               // 845
            writer.WriteLine("");                                               // 846
            writer.WriteLine("[convolutional]");                                // 847
            writer.WriteLine("batch_normalize=1");                              // 848
            writer.WriteLine("size=3");                                         // 849
            writer.WriteLine("stride=1");                                       // 850
            writer.WriteLine("pad=1");                                          // 851
            writer.WriteLine("filters=512");                                    // 852
            writer.WriteLine("activation=leaky");                               // 853
            writer.WriteLine("");                                               // 854
            writer.WriteLine("[convolutional]");                                // 855
            writer.WriteLine("batch_normalize=1");                              // 856
            writer.WriteLine("filters=256");                                    // 857
            writer.WriteLine("size=1");                                         // 858
            writer.WriteLine("stride=1");                                       // 859
            writer.WriteLine("pad=1");                                          // 860
            writer.WriteLine("activation=leaky");                               // 861
            writer.WriteLine("");                                               // 862
            writer.WriteLine("[convolutional]");                                // 863
            writer.WriteLine("batch_normalize=1");                              // 864
            writer.WriteLine("size=3");                                         // 865
            writer.WriteLine("stride=1");                                       // 866
            writer.WriteLine("pad=1");                                          // 867
            writer.WriteLine("filters=512");                                    // 868
            writer.WriteLine("activation=leaky");                               // 869
            writer.WriteLine("");                                               // 870
            writer.WriteLine("[convolutional]");                                // 871
            writer.WriteLine("batch_normalize=1");                              // 872
            writer.WriteLine("filters=256");                                    // 873
            writer.WriteLine("size=1");                                         // 874
            writer.WriteLine("stride=1");                                       // 875
            writer.WriteLine("pad=1");                                          // 876
            writer.WriteLine("activation=leaky");                               // 877
            writer.WriteLine("");                                               // 878
            writer.WriteLine("[convolutional]");                                // 879
            writer.WriteLine("batch_normalize=1");                              // 880
            writer.WriteLine("filters=128");                                    // 881
            writer.WriteLine("size=1");                                         // 882
            writer.WriteLine("stride=1");                                       // 883
            writer.WriteLine("pad=1");                                          // 884
            writer.WriteLine("activation=leaky");                               // 885
            writer.WriteLine("");                                               // 886
            writer.WriteLine("[upsample]");                                     // 887
            writer.WriteLine("stride=2");                                       // 888
            writer.WriteLine("");                                               // 889
            writer.WriteLine("[route]");                                        // 890
            writer.WriteLine("layers = 54");                                    // 891
            writer.WriteLine("");                                               // 892
            writer.WriteLine("[convolutional]");                                // 893
            writer.WriteLine("batch_normalize=1");                              // 894
            writer.WriteLine("filters=128");                                    // 895
            writer.WriteLine("size=1");                                         // 896
            writer.WriteLine("stride=1");                                       // 897
            writer.WriteLine("pad=1");                                          // 898
            writer.WriteLine("activation=leaky");                               // 899
            writer.WriteLine("");                                               // 900
            writer.WriteLine("[route]");                                        // 901
            writer.WriteLine("layers = -1, -3");                                // 902
            writer.WriteLine("");                                               // 903
            writer.WriteLine("[convolutional]");                                // 904
            writer.WriteLine("batch_normalize=1");                              // 905
            writer.WriteLine("filters=128");                                    // 906
            writer.WriteLine("size=1");                                         // 907
            writer.WriteLine("stride=1");                                       // 908
            writer.WriteLine("pad=1");                                          // 909
            writer.WriteLine("activation=leaky");                               // 910
            writer.WriteLine("");                                               // 911
            writer.WriteLine("[convolutional]");                                // 912
            writer.WriteLine("batch_normalize=1");                              // 913
            writer.WriteLine("size=3");                                         // 914
            writer.WriteLine("stride=1");                                       // 915
            writer.WriteLine("pad=1");                                          // 916
            writer.WriteLine("filters=256");                                    // 917
            writer.WriteLine("activation=leaky");                               // 918
            writer.WriteLine("");                                               // 919
            writer.WriteLine("[convolutional]");                                // 920
            writer.WriteLine("batch_normalize=1");                              // 921
            writer.WriteLine("filters=128");                                    // 922
            writer.WriteLine("size=1");                                         // 923
            writer.WriteLine("stride=1");                                       // 924
            writer.WriteLine("pad=1");                                          // 925
            writer.WriteLine("activation=leaky");                               // 926
            writer.WriteLine("");                                               // 927
            writer.WriteLine("[convolutional]");                                // 928
            writer.WriteLine("batch_normalize=1");                              // 929
            writer.WriteLine("size=3");                                         // 930
            writer.WriteLine("stride=1");                                       // 931
            writer.WriteLine("pad=1");                                          // 932
            writer.WriteLine("filters=256");                                    // 933
            writer.WriteLine("activation=leaky");                               // 934
            writer.WriteLine("");                                               // 935
            writer.WriteLine("[convolutional]");                                // 936
            writer.WriteLine("batch_normalize=1");                              // 937
            writer.WriteLine("filters=128");                                    // 938
            writer.WriteLine("size=1");                                         // 939
            writer.WriteLine("stride=1");                                       // 940
            writer.WriteLine("pad=1");                                          // 941
            writer.WriteLine("activation=leaky");                               // 942
            writer.WriteLine("");                                               // 943
            writer.WriteLine("##########################");                     // 944
            writer.WriteLine("");                                               // 945
            writer.WriteLine("[convolutional]");                                // 946
            writer.WriteLine("batch_normalize=1");                              // 947
            writer.WriteLine("size=3");                                         // 948
            writer.WriteLine("stride=1");                                       // 949
            writer.WriteLine("pad=1");                                          // 950
            writer.WriteLine("filters=256");                                    // 951
            writer.WriteLine("activation=leaky");                               // 952
            writer.WriteLine("");                                               // 953
            writer.WriteLine("[convolutional]");                                // 954
            writer.WriteLine("size=1");                                         // 955
            writer.WriteLine("stride=1");                                       // 956
            writer.WriteLine("pad=1");                                          // 957
            writer.WriteLine("filters=" + ((m_LabelList.Count + 4 + 1) * 3));   // 958
            writer.WriteLine("activation=linear");                              // 959
            writer.WriteLine("");                                               // 960
            writer.WriteLine("");                                               // 961
            writer.WriteLine("[yolo]");                                         // 962
            writer.WriteLine("mask = 0,1,2");                                   // 963
            writer.WriteLine("anchors = 12, 16, 19, 36, 40, 28, 36, 75, 76, 55, 72, 146, 142, 110, 192, 243, 459, 401"); // 964
            // writer.WriteLine("anchors =" + YoloAnchors);                        // 964
            writer.WriteLine("classes=" + m_LabelList.Count);                   // 965
            writer.WriteLine("num=9");                                          // 966
            writer.WriteLine("jitter=.3");                                      // 967
            writer.WriteLine("ignore_thresh = .7");                             // 968
            writer.WriteLine("truth_thresh = 1");                               // 969
            writer.WriteLine("scale_x_y = 1.2");                                // 970
            writer.WriteLine("iou_thresh=0.213");                               // 971
            writer.WriteLine("cls_normalizer=1.0");                             // 972
            writer.WriteLine("iou_normalizer=0.07");                            // 973
            writer.WriteLine("iou_loss=ciou");                                  // 974
            writer.WriteLine("nms_kind=greedynms");                             // 975
            writer.WriteLine("beta_nms=0.6");                                   // 976
            writer.WriteLine("max_delta=5");                                    // 976
            writer.WriteLine("");                                               // 977
            writer.WriteLine("");                                               // 978
            writer.WriteLine("[route]");                                        // 979
            writer.WriteLine("layers = -4");                                    // 980
            writer.WriteLine("");                                               // 981
            writer.WriteLine("[convolutional]");                                // 982
            writer.WriteLine("batch_normalize=1");                              // 983
            writer.WriteLine("size=3");                                         // 984
            writer.WriteLine("stride=2");                                       // 985
            writer.WriteLine("pad=1");                                          // 986
            writer.WriteLine("filters=256");                                    // 987
            writer.WriteLine("activation=leaky");                               // 988
            writer.WriteLine("");                                               // 989
            writer.WriteLine("[route]");                                        // 990
            writer.WriteLine("layers = -1, -16");                               // 991
            writer.WriteLine("");                                               // 992
            writer.WriteLine("[convolutional]");                                // 993
            writer.WriteLine("batch_normalize=1");                              // 994
            writer.WriteLine("filters=256");                                    // 995
            writer.WriteLine("size=1");                                         // 996
            writer.WriteLine("stride=1");                                       // 997
            writer.WriteLine("pad=1");                                          // 998
            writer.WriteLine("activation=leaky");                               // 999
            writer.WriteLine("");                                               // 1000
            writer.WriteLine("[convolutional]");                                // 1001
            writer.WriteLine("batch_normalize=1");                              // 1002
            writer.WriteLine("size=3");                                         // 1003
            writer.WriteLine("stride=1");                                       // 1004
            writer.WriteLine("pad=1");                                          // 1005
            writer.WriteLine("filters=512");                                    // 1006
            writer.WriteLine("activation=leaky");                               // 1007
            writer.WriteLine("");                                               // 1008
            writer.WriteLine("[convolutional]");                                // 1009
            writer.WriteLine("batch_normalize=1");                              // 1010
            writer.WriteLine("filters=256");                                    // 1011
            writer.WriteLine("size=1");                                         // 1012
            writer.WriteLine("stride=1");                                       // 1013
            writer.WriteLine("pad=1");                                          // 1014
            writer.WriteLine("activation=leaky");                               // 1015
            writer.WriteLine("");                                               // 1016
            writer.WriteLine("[convolutional]");                                // 1017
            writer.WriteLine("batch_normalize=1");                              // 1018
            writer.WriteLine("size=3");                                         // 1019
            writer.WriteLine("stride=1");                                       // 1020
            writer.WriteLine("pad=1");                                          // 1021
            writer.WriteLine("filters=512");                                    // 1022
            writer.WriteLine("activation=leaky");                               // 1023
            writer.WriteLine("");                                               // 1024
            writer.WriteLine("[convolutional]");                                // 1025
            writer.WriteLine("batch_normalize=1");                              // 1026
            writer.WriteLine("filters=256");                                    // 1027
            writer.WriteLine("size=1");                                         // 1028
            writer.WriteLine("stride=1");                                       // 1029
            writer.WriteLine("pad=1");                                          // 1030
            writer.WriteLine("activation=leaky");                               // 1031
            writer.WriteLine("");                                               // 1032
            writer.WriteLine("[convolutional]");                                // 1033
            writer.WriteLine("batch_normalize=1");                              // 1034
            writer.WriteLine("size=3");                                         // 1035
            writer.WriteLine("stride=1");                                       // 1036
            writer.WriteLine("pad=1");                                          // 1037
            writer.WriteLine("filters=512");                                    // 1038
            writer.WriteLine("activation=leaky");                               // 1039
            writer.WriteLine("");                                               // 1040
            writer.WriteLine("[convolutional]");                                // 1041
            writer.WriteLine("size=1");                                         // 1042
            writer.WriteLine("stride=1");                                       // 1043
            writer.WriteLine("pad=1");                                          // 1044
            writer.WriteLine("filters=" + ((m_LabelList.Count + 4 + 1) * 3));   // 1045
            writer.WriteLine("activation=linear");                              // 1046
            writer.WriteLine("");                                               // 1047
            writer.WriteLine("");                                               // 1048
            writer.WriteLine("[yolo]");                                         // 1049
            writer.WriteLine("mask = 3,4,5");                                   // 1050
            writer.WriteLine("anchors = 12, 16, 19, 36, 40, 28, 36, 75, 76, 55, 72, 146, 142, 110, 192, 243, 459, 401"); // 1051
            // writer.WriteLine("anchors =" + YoloAnchors); // 1051
            writer.WriteLine("classes=" + m_LabelList.Count);                   // 1051
            writer.WriteLine("num=9");                                          // 1052
            writer.WriteLine("jitter=.3");                                      // 1053
            writer.WriteLine("ignore_thresh = .7");                             // 1054
            writer.WriteLine("truth_thresh = 1");                               // 1055
            writer.WriteLine("scale_x_y = 1.1");                                // 1056
            writer.WriteLine("iou_thresh=0.213");                               // 1057
            writer.WriteLine("cls_normalizer=1.0");                             // 1058
            writer.WriteLine("iou_normalizer=0.07");                            // 1059
            writer.WriteLine("iou_loss=ciou");                                  // 1060
            writer.WriteLine("nms_kind=greedynms");                             // 1061
            writer.WriteLine("beta_nms=0.6");                                   // 1062
            writer.WriteLine("max_delta=5");
            writer.WriteLine("");                                               // 1063
            writer.WriteLine("");                                               // 1064
            writer.WriteLine("[route]");                                        // 1065
            writer.WriteLine("layers = -4");                                    // 1066
            writer.WriteLine("");                                               // 1067
            writer.WriteLine("[convolutional]");                                // 1068
            writer.WriteLine("batch_normalize=1");                              // 1069
            writer.WriteLine("size=3");                                         // 1070
            writer.WriteLine("stride=2");                                       // 1071
            writer.WriteLine("pad=1");                                          // 1072
            writer.WriteLine("filters=512");                                    // 1073
            writer.WriteLine("activation=leaky");                               // 1074
            writer.WriteLine("");                                               // 1075
            writer.WriteLine("[route]");                                        // 1076
            writer.WriteLine("layers = -1, -37");                               // 1077
            writer.WriteLine("");                                               // 1078
            writer.WriteLine("[convolutional]");                                // 1079
            writer.WriteLine("batch_normalize=1");                              // 1080
            writer.WriteLine("filters=512");                                    // 1081
            writer.WriteLine("size=1");                                         // 1082
            writer.WriteLine("stride=1");                                       // 1083
            writer.WriteLine("pad=1");                                          // 1084
            writer.WriteLine("activation=leaky");                               // 1085
            writer.WriteLine("");                                               // 1086
            writer.WriteLine("[convolutional]");                                // 1087
            writer.WriteLine("batch_normalize=1");                              // 1088
            writer.WriteLine("size=3");                                         // 1089
            writer.WriteLine("stride=1");                                       // 1090
            writer.WriteLine("pad=1");                                          // 1091
            writer.WriteLine("filters=1024");                                   // 1092
            writer.WriteLine("activation=leaky");                               // 1093
            writer.WriteLine("");                                               // 1094
            writer.WriteLine("[convolutional]");                                // 1095
            writer.WriteLine("batch_normalize=1");                              // 1096
            writer.WriteLine("filters=512");                                    // 1097
            writer.WriteLine("size=1");                                         // 1098
            writer.WriteLine("stride=1");                                       // 1099
            writer.WriteLine("pad=1");                                          // 1100
            writer.WriteLine("activation=leaky");                               // 1101
            writer.WriteLine("");                                               // 1102
            writer.WriteLine("[convolutional]");                                // 1103
            writer.WriteLine("batch_normalize=1");                              // 1104
            writer.WriteLine("size=3");                                         // 1105
            writer.WriteLine("stride=1");                                       // 1106
            writer.WriteLine("pad=1");                                          // 1107
            writer.WriteLine("filters=1024");                                   // 1108
            writer.WriteLine("activation=leaky");                               // 1109
            writer.WriteLine("");                                               // 1110
            writer.WriteLine("[convolutional]");                                // 1111
            writer.WriteLine("batch_normalize=1");                              // 1112
            writer.WriteLine("filters=512");                                    // 1113
            writer.WriteLine("size=1");                                         // 1114
            writer.WriteLine("stride=1");                                       // 1115
            writer.WriteLine("pad=1");                                          // 1116
            writer.WriteLine("activation=leaky");                               // 1117
            writer.WriteLine("");                                               // 1118
            writer.WriteLine("[convolutional]");                                // 1119
            writer.WriteLine("batch_normalize=1");                              // 1120
            writer.WriteLine("size=3");                                         // 1121
            writer.WriteLine("stride=1");                                       // 1122
            writer.WriteLine("pad=1");                                          // 1123
            writer.WriteLine("filters=1024");                                   // 1124
            writer.WriteLine("activation=leaky");                               // 1125
            writer.WriteLine("");                                               // 1126
            writer.WriteLine("[convolutional]");                                // 1127
            writer.WriteLine("size=1");                                         // 1128
            writer.WriteLine("stride=1");                                       // 1129
            writer.WriteLine("pad=1");                                          // 1130
            writer.WriteLine("filters=" + ((m_LabelList.Count + 4 + 1) * 3));   // 1131
            writer.WriteLine("activation=linear");                              // 1132
            writer.WriteLine("");                                               // 1133
            writer.WriteLine("");                                               // 1134
            writer.WriteLine("[yolo]");                                         // 1135
            writer.WriteLine("mask = 6,7,8");                                   // 1136
            writer.WriteLine("anchors = 12, 16, 19, 36, 40, 28, 36, 75, 76, 55, 72, 146, 142, 110, 192, 243, 459, 401"); // 1137
            // writer.WriteLine("anchors =" + YoloAnchors); // 1137
            writer.WriteLine("classes=" + m_LabelList.Count);                   // 1138
            writer.WriteLine("num=9");                                          // 1139
            writer.WriteLine("jitter=.3");                                      // 1140
            writer.WriteLine("ignore_thresh = .7");                             // 1141
            writer.WriteLine("truth_thresh = 1");                               // 1142
            writer.WriteLine("random=1");                                       // 1143
            writer.WriteLine("scale_x_y = 1.05");                               // 1144
            writer.WriteLine("iou_thresh=0.213");                               // 1145
            writer.WriteLine("cls_normalizer=1.0");                             // 1146
            writer.WriteLine("iou_normalizer=0.07");                            // 1147
            writer.WriteLine("iou_loss=ciou");                                  // 1148
            writer.WriteLine("nms_kind=greedynms");                             // 1149
            writer.WriteLine("beta_nms=0.6");                                   // 1150
            writer.WriteLine("max_delta=5");

            writer.Close();
            writer.Dispose();
            writer = null;
            fileStream.Close();
            fileStream.Dispose();
            fileStream = null;

            // 기존 데이터 파일 삭제
            if (File.Exists(m_ProjectPath + "\\" + m_ProjectName + ".cfg"))
            {
                try
                {
                    File.Delete(m_ProjectPath + "\\" + m_ProjectName + ".cfg");
                }
                catch
                {

                }
            }

            File.Copy(m_ProjectPath + "\\" + "temp.cfg", m_ProjectPath + "\\" + m_ProjectName + ".cfg", true);

            if (File.Exists(m_ProjectPath + "\\" + "temp.cfg"))
            {
                File.Delete(m_ProjectPath + "\\" + "temp.cfg");
            }

            /*
            if (yoloWrapper == null)
            {
                if (!File.Exists(m_ProjectPath + "\\" + m_ProjectName + ".weights"))
                {
                    File.Create(m_ProjectPath + "\\" + m_ProjectName + ".weights");
                }

                GpuConfig gpuConfig = new GpuConfig();
                gpuConfig.GpuIndex = 0;
                yoloWrapper = new YoloWrapper(m_ProjectPath + "\\" + m_ProjectName + ".cfg", m_ProjectPath + "\\" + m_ProjectName + ".weights", m_ProjectPath + "\\" + m_ProjectName + ".names", gpuConfig);
            }
            */
        }

        private void CompareCfg(string source, string target)
        {
            FileStream Source_Stream = new FileStream(source, FileMode.Open);
            StreamReader Source_Reader = new StreamReader(Source_Stream);
            FileStream Target_Stream = new FileStream(target, FileMode.Open);
            StreamReader Target_Reader = new StreamReader(Target_Stream);

            string Source_Line, Target_Line;
            int line_num = 1;

            while ((Target_Line = Target_Reader.ReadLine()) != null)
            {
                Source_Line = Source_Reader.ReadLine();

                line_num++;
            }

            Source_Reader.Close();
            Source_Stream.Close();
            Target_Reader.Close();
            Target_Stream.Close();
        }

        private void Save_ImageList_Information()
        {
            for (int i = 0; i < m_ImageInfoList.Count; i++)
            {
                ImageInfo x = m_ImageInfoList[i];

                string[] _s = x.FileName.Split('\\');
                string ext = Path.GetExtension(_s[_s.Length - 1]);
                string dirName = _s[_s.Length - 2];
                string filename = _s[_s.Length - 1].Substring(0, _s[_s.Length - 1].Length - ext.Length);

                string trainType = x.TrainType.ToString();

                if (trainType == "NONE")
                {
                    trainType = "NotLabeled";
                }
                else if (trainType == "LABELED")
                {
                    trainType = "Labeled";
                }
                else if (trainType == "TRAINED")
                {
                    trainType = "Trained";
                }

                if (dirName != trainType)
                {
                    trainType = dirName;

                    switch (trainType)
                    {
                        case "NotLabeled":
                            x.TrainType = TrainType.NONE;
                            break;
                        case "Labeled":
                            x.TrainType = TrainType.LABELED;
                            break;
                        case "Trained":
                            x.TrainType = TrainType.TRAINED;
                            break;
                    }
                }

                if (!Directory.Exists(m_ProjectPath + "\\Images\\" + trainType))
                {
                    Directory.CreateDirectory(m_ProjectPath + "\\Images\\" + trainType);
                }

                if (File.Exists(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename + ".hsr"))
                {
                    File.Delete(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename + ".hsr");
                }

                FileStream fileStream = new FileStream(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename + ".hsr", FileMode.CreateNew);
                StreamWriter streamWriter = new StreamWriter(fileStream);

                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(x.FileName);

                if (!File.Exists(m_ProjectPath + "\\Images\\" + trainType + "\\" + Path.GetFileName(x.FileName)))
                {
                    File.Copy(x.FileName, m_ProjectPath + "\\Images\\" + trainType + "\\" + Path.GetFileName(x.FileName), true);
                }

                streamWriter.WriteLine(bmp.Width);
                streamWriter.WriteLine(bmp.Height);
                streamWriter.WriteLine(x.CharList.Count);
                streamWriter.WriteLine(x.DateTime);

                if (x.CharList.Count == 0)
                {
                    x.TrainType = TrainType.NONE;
                }

                streamWriter.WriteLine(x.TrainType.ToString());

                for (int j = 0; j < x.CharList.Count; j++)
                {
                    CharInfo c = x.CharList[j];

                    if (c.Labeled_X == 0 && c.Labeled_Y == 0 && c.Labeled_Width == 0 && c.Labeled_Height == 0 &&
                    c.Finded_X == 0 && c.Finded_Y == 0 && c.Finded_Width == 0 && c.Finded_Height == 0)
                    {
                        continue;
                    }
                    else
                    {
                        streamWriter.WriteLine(c.Is_Labeled.ToString());
                        streamWriter.WriteLine(c.Labeled_Name);
                        streamWriter.WriteLine(c.Labeled_X);
                        streamWriter.WriteLine(c.Labeled_Y);
                        streamWriter.WriteLine(c.Labeled_Width);
                        streamWriter.WriteLine(c.Labeled_Height);
                        streamWriter.WriteLine(c.Is_Finded.ToString());
                        streamWriter.WriteLine(c.Finded_Name);
                        streamWriter.WriteLine(c.Finded_X);
                        streamWriter.WriteLine(c.Finded_Y);
                        streamWriter.WriteLine(c.Finded_Width);
                        streamWriter.WriteLine(c.Finded_Height);
                        streamWriter.WriteLine(c.Finded_Score);
                    }
                }

                streamWriter.Close();
                fileStream.Close();
                streamWriter = null;
                fileStream = null;
                bmp.Dispose();
                bmp = null;
            }
        }

        private void Save_ImageList_Information(ImageInfo x)
        {
            string[] _s = x.FileName.Split('\\');
            string ext = Path.GetExtension(_s[_s.Length - 1]);
            string filename = _s[_s.Length - 1].Substring(0, _s[_s.Length - 1].Length - ext.Length);
            string trainType = x.TrainType.ToString();

            if (trainType == "NONE")
            {
                trainType = "NotLabeled";
            }
            else if (trainType == "LABELED")
            {
                trainType = "Labeled";
            }
            else if (trainType == "TRAINED")
            {
                trainType = "Trained";
            }

            if (File.Exists(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename + ".hsr"))
            {
                File.Delete(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename + ".hsr");
            }

            if (File.Exists(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename + ".hsr"))
            {
                File.Delete(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename + ".hsr");
            }

            FileStream fileStream = new FileStream(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename + ".hsr", FileMode.CreateNew);
            StreamWriter streamWriter = new StreamWriter(fileStream);

            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(x.FileName);

            streamWriter.WriteLine(bmp.Width);
            streamWriter.WriteLine(bmp.Height);
            streamWriter.WriteLine(x.CharList.Count);
            streamWriter.WriteLine(x.DateTime);

            if (x.CharList.Count == 0)
            {
                x.TrainType = TrainType.NONE;
            }
            else
            {
                if (x.TrainType != TrainType.TRAINED)
                {
                    x.TrainType = TrainType.LABELED;
                }
            }

            streamWriter.WriteLine(x.TrainType.ToString());

            for (int j = 0; j < x.CharList.Count; j++)
            {
                CharInfo c = x.CharList[j];

                if (c.Labeled_X == 0 && c.Labeled_Y == 0 && c.Labeled_Width == 0 && c.Labeled_Height == 0 &&
                    c.Finded_X == 0 && c.Finded_Y == 0 && c.Finded_Width == 0 && c.Finded_Height == 0)
                {
                    continue;
                }
                else
                {
                    streamWriter.WriteLine(c.Is_Labeled.ToString());
                    streamWriter.WriteLine(c.Labeled_Name);
                    streamWriter.WriteLine(c.Labeled_X);
                    streamWriter.WriteLine(c.Labeled_Y);
                    streamWriter.WriteLine(c.Labeled_Width);
                    streamWriter.WriteLine(c.Labeled_Height);
                    streamWriter.WriteLine(c.Is_Finded.ToString());
                    streamWriter.WriteLine(c.Finded_Name);
                    streamWriter.WriteLine(c.Finded_X);
                    streamWriter.WriteLine(c.Finded_Y);
                    streamWriter.WriteLine(c.Finded_Width);
                    streamWriter.WriteLine(c.Finded_Height);
                    streamWriter.WriteLine(c.Finded_Score);
                }
            }

            streamWriter.Close();
            fileStream.Close();
            streamWriter = null;
            fileStream = null;
            bmp.Dispose();
            bmp = null;
        }

        /*
        private void Save_ImageList_Information(ImageInfo x)
        {
            if (x.FileName == null && x.Width == 0 && x.Height == 0)
            {
                return;
            }

            string[] _s = x.FileName.Split('\\');
            string ext = Path.GetExtension(_s[_s.Length - 1]);
            string filename = _s[_s.Length - 1].Substring(0, _s[_s.Length - 1].Length - ext.Length);

            string trainType = x.TrainType.ToString();

            if (trainType == "NONE")
            {
                trainType = "NotLabeled";
            }
            else if (trainType == "LABELED")
            {
                trainType = "Labeled";
            }
            else if (trainType == "TRAINED")
            {
                trainType = "Trained";
            }

            if (File.Exists(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename + ".hsr"))
            {
                File.Delete(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename + ".hsr");
            }

            FileStream fileStream = new FileStream(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename + ".hsr", FileMode.CreateNew);
            StreamWriter streamWriter = new StreamWriter(fileStream);

            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(x.FileName);

            streamWriter.WriteLine(bmp.Width);
            streamWriter.WriteLine(bmp.Height);
            streamWriter.WriteLine(x.CharList.Count);
            streamWriter.WriteLine(x.DateTime);

            if (x.CharList.Count == 0)
            {
                x.TrainType = TrainType.NONE;
            }
            else
            {
                if (x.TrainType != TrainType.TRAINED)
                {
                    x.TrainType = TrainType.LABELED;
                }
            }

            streamWriter.WriteLine(x.TrainType.ToString());

            for (int j = 0; j < x.CharList.Count; j++)
            {
                CharInfo c = x.CharList[j];

                if (c.Labeled_X == 0 && c.Labeled_Y == 0 && c.Labeled_Width == 0 && c.Labeled_Height == 0 &&
                    c.Finded_X == 0 && c.Finded_Y == 0 && c.Finded_Width == 0 && c.Finded_Height == 0)
                {
                    continue;
                }
                else
                {
                    streamWriter.WriteLine(c.Is_Labeled.ToString());
                    streamWriter.WriteLine(c.Labeled_Name);
                    streamWriter.WriteLine(c.Labeled_X);
                    streamWriter.WriteLine(c.Labeled_Y);
                    streamWriter.WriteLine(c.Labeled_Width);
                    streamWriter.WriteLine(c.Labeled_Height);
                    streamWriter.WriteLine(c.Is_Finded.ToString());
                    streamWriter.WriteLine(c.Finded_Name);
                    streamWriter.WriteLine(c.Finded_X);
                    streamWriter.WriteLine(c.Finded_Y);
                    streamWriter.WriteLine(c.Finded_Width);
                    streamWriter.WriteLine(c.Finded_Height);
                    streamWriter.WriteLine(c.Finded_Score);
                }
            }

            streamWriter.Close();
            fileStream.Close();
            streamWriter = null;
            fileStream = null;
            bmp.Dispose();
            bmp = null;

            if (x.CharList.Count > 0)
            {
                string originFileName = x.
                string fileName = Path.GetFileName(x.FileName);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(x.FileName);
                x.TrainType = TrainType.LABELED;
                x.FileName = m_ProjectPath + "\\Images\\Labeled\\" + fileName;
                trainType = x.TrainType.ToString();

                if (trainType == "NONE")
                {
                    trainType = "NotLabeled";
                }
                else if (trainType == "LABELED")
                {
                    trainType = "Labeled";
                }
                else if (trainType == "TRAINED")
                {
                    trainType = "Trained";
                }

                if (!Directory.Exists(m_ProjectPath + "\\Images\\NotLabeled"))
                {
                    Directory.CreateDirectory(m_ProjectPath + "\\Images\\NotLabeled");
                }

                if (!Directory.Exists(m_ProjectPath + "\\Images\\Labeled"))
                {
                    Directory.CreateDirectory(m_ProjectPath + "\\Images\\Labeled");
                }

                if (File.Exists(x.FileName))
                {
                    File.Copy(x.FileName, m_ProjectPath + "\\Images\\Labeled\\" + fileName);
                    File.Delete(x.FileName);
                }

                if (File.Exists(m_ProjectPath + "\\Images\\NotLabeled\\" + fileNameWithoutExtension + ".hsr"))
                {
                    File.Copy(m_ProjectPath + "\\Images\\NotLabeled\\" + fileNameWithoutExtension + ".hsr", m_ProjectPath + "\\Images\\Labeled\\" + fileNameWithoutExtension + ".hsr");
                    File.Delete(m_ProjectPath + "\\Images\\NotLabeled\\" + fileNameWithoutExtension + ".hsr");
                }

                if (m_IsOriginImageInfoList)
                {
                    for (int i = 0; i < m_ImageInfoList_Temp.Count; i++)
                    {
                        if (m_ImageInfoList_Temp[i].FileName == x.FileName)
                        {
                            m_ImageInfoList_Temp[i] = x;
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < m_ImageInfoList.Count; i++)
                    {
                        if (m_ImageInfoList[i].FileName == x.FileName)
                        {
                            m_ImageInfoList[i] = x;
                            break;
                        }
                    }
                }
            }
        }
        */

        private void Load_ImageList_Information()
        {
            LogManager.Action("Load_ImageList_Information() 실행");

            m_LoadThreadFinished = false;

            new Thread(new ThreadStart(() =>
            {
                if (Directory.GetFiles(m_ProjectPath + "\\Images").Length > 0)
                {
                    List<string> files = Directory.GetFiles(m_ProjectPath + "\\Images").ToList();

                    files.ForEach(x =>
                    {
                        string ext = Path.GetExtension(x);

                        if (ext == ".bmp" || ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif")
                        {
                            string datafile = x.Substring(0, x.Length - ext.Length) + ".hsr";
                            string[] _spt = x.Split('\\');
                            string source_name = _spt[_spt.Length - 1];
                            source_name = source_name.Substring(0, source_name.Length - ext.Length);

                            ImageInfo _i = new ImageInfo(x);
                            _i.CharList = new List<CharInfo>();

                            if (!File.Exists(datafile))
                            {
                                FileStream fs = File.Create(datafile);
                                fs.Close();
                                fs.Dispose();
                                fs = null;
                            }

                            if (File.Exists(datafile))
                            {
                                FileStream fs = new FileStream(datafile, FileMode.Open);
                                StreamReader sr = new StreamReader(fs);

                                string line = "";

                                try
                                {
                                    while ((line = sr.ReadLine()) != null)
                                    {
                                        string width = line;
                                        string height = sr.ReadLine();
                                        int count = int.Parse(sr.ReadLine());
                                        string dateTime = sr.ReadLine();
                                        string type = sr.ReadLine();
                                        _i.DateTime = dateTime;
                                        _i.Width = Convert.ToInt32(width);
                                        _i.Height = Convert.ToInt32(height);

                                        switch (type)
                                        {
                                            case "LABELED":
                                                _i.TrainType = TrainType.LABELED;
                                                break;
                                            case "TRAINED":
                                                _i.TrainType = TrainType.TRAINED;
                                                break;
                                            case "NAVER":
                                                _i.TrainType = TrainType.NAVER;
                                                break;
                                            case "NONE":
                                                _i.TrainType = TrainType.NONE;
                                                break;
                                            default:
                                                _i.TrainType = TrainType.NONE;
                                                break;
                                        }

                                        for (int i = 0; i < count; i++)
                                        {
                                            string is_label = sr.ReadLine();
                                            string labeled_name = sr.ReadLine();
                                            string label_x = sr.ReadLine();
                                            string label_y = sr.ReadLine();
                                            string labeled_width = sr.ReadLine();
                                            string labeled_height = sr.ReadLine();
                                            string is_finded = sr.ReadLine();
                                            string finded_name = sr.ReadLine();
                                            string finded_x = sr.ReadLine();
                                            string finded_y = sr.ReadLine();
                                            string finded_width = sr.ReadLine();
                                            string finded_height = sr.ReadLine();
                                            string finded_score = sr.ReadLine();

                                            CharInfo c = new CharInfo();
                                            c.Is_Labeled = Convert.ToBoolean(is_label);
                                            c.Labeled_Name = labeled_name;
                                            c.Labeled_X = Convert.ToDouble(label_x);
                                            c.Labeled_Y = Convert.ToDouble(label_y);
                                            c.Labeled_Width = Convert.ToDouble(labeled_width);
                                            c.Labeled_Height = Convert.ToDouble(labeled_height);
                                            c.Is_Finded = Convert.ToBoolean(is_finded);
                                            c.Finded_Name = finded_name;
                                            c.Finded_X = Convert.ToDouble(finded_x);
                                            c.Finded_Y = Convert.ToDouble(finded_y);
                                            c.Finded_Width = Convert.ToDouble(finded_width);
                                            c.Finded_Height = Convert.ToDouble(finded_height);

                                            _i.CharList.Add(c);
                                        }
                                    }

                                    sr.Close();
                                    sr.Dispose();
                                    sr = null;

                                    fs.Close();
                                    fs.Dispose();
                                    fs = null;
                                }
                                catch (Exception e)
                                {
                                    // 이전 버전 데이터 형식 사용 프로젝트일 경우, 새로운 버전의 데이터 형식으로 변환하는 프로그램 실행
                                    if (sr != null)
                                    {
                                        sr.Close();
                                        sr.Dispose();
                                        sr = null;
                                    }

                                    if (fs != null)
                                    {
                                        fs.Close();
                                        fs.Dispose();
                                        fs = null;
                                    }

                                    if (m_Process != null && !m_Process.HasExited)
                                    {
                                        /*
                                        m_Process.Kill();
                                        m_Process.Close();
                                        m_Process.Dispose();
                                        m_Process = null;
                                        */
                                    }

                                    Process p = Process.Start("ConsoleApp1.exe", m_ProjectPath + "\\Images");

                                    Dispatcher.Invoke(() =>
                                    {
                                        this.Close();
                                    });

                                    return;
                                }
                            }
                        }
                    });

                    // 데이터 형식에는 문제 없으나, 라벨링 타입에 따른 폴더 분류 필요시 실행
                    files = Directory.GetFiles(m_ProjectPath + "\\Images").ToList();

                    if (files.Count > 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            m_Controller.SetMessage("폴더 분류 중 입니다...");
                        });

                        files.ForEach(x =>
                        {
                            if (Path.GetExtension(x).Contains(".jpg") || Path.GetExtension(x).Contains(".jpeg") || Path.GetExtension(x).Contains(".bmp") || Path.GetExtension(x).Contains(".png"))
                            {
                                string filename = Path.GetFileName(x);
                                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(x);

                                StreamReader sr = new StreamReader(m_ProjectPath + "\\Images\\" + fileNameWithoutExtension + ".hsr");
                                string line = null;

                                string width = sr.ReadLine();
                                string height = sr.ReadLine();
                                int count = int.Parse(sr.ReadLine());
                                string dateTime = sr.ReadLine();
                                string type = sr.ReadLine();

                                sr.Close();
                                sr.Dispose();
                                sr = null;

                                string trainType = type;

                                if (trainType == "NONE")
                                {
                                    trainType = "NotLabeled";
                                }
                                else if (trainType == "LABELED")
                                {
                                    trainType = "Labeled";
                                }
                                else if (trainType == "TRAINED")
                                {
                                    trainType = "Trained";
                                }

                                if (!Directory.Exists(m_ProjectPath + "\\Images\\" + trainType))
                                {
                                    Directory.CreateDirectory(m_ProjectPath + "\\Images\\" + trainType);
                                }

                                if (File.Exists(m_ProjectPath + "\\Images\\" + fileNameWithoutExtension + ".hsr"))
                                {
                                    File.Copy(m_ProjectPath + "\\Images\\" + fileNameWithoutExtension + ".hsr", m_ProjectPath + "\\Images\\" + trainType + "\\" + fileNameWithoutExtension + ".hsr", true);
                                    File.Delete(m_ProjectPath + "\\Images\\" + fileNameWithoutExtension + ".hsr");
                                }

                                if (File.Exists(m_ProjectPath + "\\Images\\" + filename))
                                {
                                    File.Copy(m_ProjectPath + "\\Images\\" + filename, m_ProjectPath + "\\Images\\" + trainType + "\\" + filename, true);
                                    File.Delete(m_ProjectPath + "\\Images\\" + filename);
                                }
                            }
                        });
                    }
                }
                
                List<string> dirs = Directory.GetDirectories(m_ProjectPath + "\\Images").ToList();

                int _cnt = 0;
                double _w = 0;
                double _h = 0;

                dirs.ForEach(d =>
                {
                    List<string> files = Directory.GetFiles(d).ToList();

                    files.ForEach(x =>
                    {
                        string ext = Path.GetExtension(x);

                        if (ext == ".bmp" || ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif")
                        {
                            string datafile = x.Substring(0, x.Length - ext.Length) + ".hsr";
                            string[] _spt = x.Split('\\');
                            string source_name = _spt[_spt.Length - 1];
                            String dirName = _spt[_spt.Length - 2];
                            source_name = source_name.Substring(0, source_name.Length - ext.Length);

                            ImageInfo _i = new ImageInfo(x);
                            _i.CharList = new List<CharInfo>();

                            if (File.Exists(datafile))
                            {
                                FileStream fs = new FileStream(datafile, FileMode.Open);
                                StreamReader sr = new StreamReader(fs);

                                string line = "";
                                bool error = false;

                                while ((line = sr.ReadLine()) != null)
                                {
                                    string width = line;
                                    string height = sr.ReadLine();
                                    int count = int.Parse(sr.ReadLine());
                                    string dateTime = sr.ReadLine();
                                    string type = sr.ReadLine();
                                    _i.DateTime = dateTime;
                                    _i.Width = Convert.ToInt32(width);
                                    _i.Height = Convert.ToInt32(height);

                                    string trainType = "";

                                    switch (type)
                                    {
                                        case "LABELED":
                                            _i.TrainType = TrainType.LABELED;
                                            trainType = "Labeled";
                                            break;
                                        case "TRAINED":
                                            _i.TrainType = TrainType.TRAINED;
                                            trainType = "Trained";
                                            break;
                                        case "NAVER":
                                            _i.TrainType = TrainType.NAVER;
                                            break;
                                        case "NONE":
                                            _i.TrainType = TrainType.NONE;
                                            trainType = "NotLabeled";
                                            break;
                                        default:
                                            _i.TrainType = TrainType.NONE;
                                            trainType = "NotLabeled";
                                            break;
                                    }

                                    if (dirName != trainType)
                                    {
                                        error = true;
                                        trainType = dirName;

                                        switch (trainType)
                                        {
                                            case "NotLabeled":
                                                _i.TrainType = TrainType.NONE;
                                                break;
                                            case "Labeled":
                                                _i.TrainType = TrainType.LABELED;
                                                break;
                                            case "Trained":
                                                _i.TrainType = TrainType.TRAINED;
                                                break;
                                        }
                                    }

                                    for (int i = 0; i < count; i++)
                                    {
                                        string is_label = sr.ReadLine();
                                        string labeled_name = sr.ReadLine();
                                        string label_x = sr.ReadLine();
                                        string label_y = sr.ReadLine();
                                        string labeled_width = sr.ReadLine();
                                        string labeled_height = sr.ReadLine();
                                        string is_finded = sr.ReadLine();
                                        string finded_name = sr.ReadLine();
                                        string finded_x = sr.ReadLine();
                                        string finded_y = sr.ReadLine();
                                        string finded_width = sr.ReadLine();
                                        string finded_height = sr.ReadLine();
                                        string finded_score = sr.ReadLine();

                                        CharInfo c = new CharInfo();
                                        c.Is_Labeled = Convert.ToBoolean(is_label);
                                        c.Labeled_Name = labeled_name;
                                        c.Labeled_X = Convert.ToDouble(label_x);
                                        c.Labeled_Y = Convert.ToDouble(label_y);
                                        c.Labeled_Width = Convert.ToDouble(labeled_width);
                                        c.Labeled_Height = Convert.ToDouble(labeled_height);
                                        c.Is_Finded = Convert.ToBoolean(is_finded);
                                        c.Finded_Name = finded_name;
                                        c.Finded_X = Convert.ToDouble(finded_x);
                                        c.Finded_Y = Convert.ToDouble(finded_y);
                                        c.Finded_Width = Convert.ToDouble(finded_width);
                                        c.Finded_Height = Convert.ToDouble(finded_height);

                                        _i.CharList.Add(c);

                                        _w += c.Labeled_Width * Convert.ToDouble(width);
                                        _h += c.Labeled_Height * Convert.ToDouble(height);
                                        _cnt++;
                                    }
                                }

                                sr.Close();
                                sr.Dispose();
                                sr = null;

                                fs.Close();
                                fs.Dispose();
                                fs = null;

                                if (error && _i.CharList.Count == 0)
                                {
                                    string trainType = "";

                                    switch (_i.TrainType)
                                    {
                                        case TrainType.NONE:
                                            trainType = "NotLabeled";
                                            break;
                                        case TrainType.LABELED:
                                            trainType = "Labeled";
                                            break;
                                        case TrainType.TRAINED:
                                            trainType = "Trained";
                                            break;
                                    }

                                    if (File.Exists(_i.FileName))
                                    {
                                        File.Copy(_i.FileName, m_ProjectPath + "\\Images\\NotLabeled\\" + Path.GetFileName(_i.FileName), true);
                                        File.Delete(_i.FileName);
                                    }

                                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(_i.FileName);

                                    if (File.Exists(m_ProjectPath + "\\Images\\" + trainType + "\\" + fileNameWithoutExtension + ".hsr"))
                                    {
                                        File.Copy(m_ProjectPath + "\\Images\\" + trainType + "\\" + fileNameWithoutExtension + ".hsr", m_ProjectPath + "\\Images\\NotLabeled\\" + fileNameWithoutExtension + ".hsr", true);
                                        File.Delete(m_ProjectPath + "\\Images\\" + trainType + "\\" + fileNameWithoutExtension + ".hsr");
                                    }

                                    _i.TrainType = TrainType.NONE;
                                    _i.FileName = m_ProjectPath + "\\Images\\NotLabeled\\" + Path.GetFileName(_i.FileName);
                                }

                                if (error)
                                {
                                    Save_ImageList_Information(_i);
                                }

                                Dispatcher.Invoke(() =>
                                {
                                    _i.IsVisble = true;
                                    m_ImageInfoList.Add(_i);
                                });
                            }
                            else
                            {
                                FileStream fileStream = new FileStream(datafile, FileMode.CreateNew);
                                StreamWriter streamWriter = new StreamWriter(fileStream);

                                Mat mat = new Mat(x);

                                _i.Width = mat.Width;
                                _i.Height = mat.Height;
                                _i.IsVisble = true;

                                streamWriter.WriteLine(_i.Width);
                                streamWriter.WriteLine(_i.Height);
                                streamWriter.WriteLine(_i.CharList.Count);
                                streamWriter.WriteLine(_i.DateTime);

                                if (_i.CharList.Count == 0)
                                {
                                    _i.TrainType = TrainType.NONE;
                                }
                                else
                                {
                                    if (_i.TrainType != TrainType.TRAINED)
                                    {
                                        _i.TrainType = TrainType.LABELED;
                                    }
                                }

                                streamWriter.WriteLine(_i.TrainType.ToString());

                                for (int j = 0; j < _i.CharList.Count; j++)
                                {
                                    CharInfo c = _i.CharList[j];

                                    if (c.Labeled_X == 0 && c.Labeled_Y == 0 && c.Labeled_Width == 0 && c.Labeled_Height == 0 &&
                                    c.Finded_X == 0 && c.Finded_Y == 0 && c.Finded_Width == 0 && c.Finded_Height == 0)
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        streamWriter.WriteLine(c.Is_Labeled.ToString());
                                        streamWriter.WriteLine(c.Labeled_Name);
                                        streamWriter.WriteLine(c.Labeled_X);
                                        streamWriter.WriteLine(c.Labeled_Y);
                                        streamWriter.WriteLine(c.Labeled_Width);
                                        streamWriter.WriteLine(c.Labeled_Height);
                                        streamWriter.WriteLine(c.Is_Finded.ToString());
                                        streamWriter.WriteLine(c.Finded_Name);
                                        streamWriter.WriteLine(c.Finded_X);
                                        streamWriter.WriteLine(c.Finded_Y);
                                        streamWriter.WriteLine(c.Finded_Width);
                                        streamWriter.WriteLine(c.Finded_Height);
                                        streamWriter.WriteLine(c.Finded_Score);
                                    }
                                }

                                streamWriter.Close();
                                fileStream.Close();
                                streamWriter = null;
                                fileStream = null;

                                m_ImageInfoList.Add(_i);
                            }
                        }
                    });
                });

                m_Total_Img_Cnt = m_ImageInfoList.Count;

                m_DefaultWidth = (int)(_w / _cnt);
                m_DefaultHeight = (int)(_h / _cnt);

                if (m_DefaultWidth < 20)
                {
                    try
                    {
                        if (dp.BitmapImage != null)
                        {
                            m_DefaultWidth = Convert.ToInt32(dp.BitmapImage.Width * 0.1);
                        }
                    }
                    catch
                    {

                    }
                }

                if (m_DefaultHeight < 20)
                {
                    try
                    {
                        if (dp.BitmapImage != null)
                        {
                            m_DefaultHeight = Convert.ToInt32(dp.BitmapImage.Width * 0.1);
                        }
                    }
                    catch
                    {

                    }
                }

                Refresh_ImageList(m_ImageShowType);
                RefreshcharInfo();

                if (m_IsOriginImageInfoList)
                {
                    LimitPage = m_ImageInfoList_Temp.Count / m_CntPerPage;

                    if (m_ImageInfoList_Temp.Count % m_CntPerPage > 0)
                    {
                        LimitPage++;
                    }
                }
                else
                {
                    LimitPage = m_ImageInfoList.Count / m_CntPerPage;

                    if (m_ImageInfoList.Count % m_CntPerPage > 0)
                    {
                        LimitPage++;
                    }
                }

                m_StartPage = 0;

                if (m_LimitPage < m_PagePerGroup)
                {
                    m_EndPage = m_LimitPage;
                }
                else
                {
                    m_EndPage = m_PagePerGroup;
                }

                if (m_ImageInfoList.Count > 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        PartImageInfoList.Clear();

                        int _start = m_CntPerPage * 0;
                        int _end = _start + m_CntPerPage;

                        if (m_ImageInfoList.Count < _end)
                        {
                            _end = m_ImageInfoList.Count;
                        }

                        for (int i = _start; i < _end; i++)
                        {
                            PartImageInfoList.Add(m_ImageInfoList[i]);
                        }

                        for (int i = m_EndPage; i > m_StartPage; i--)
                        {
                            TextBlock m_tb = new TextBlock();
                            m_tb.Margin = new Thickness(5);
                            m_tb.FontSize = 16;
                            m_tb.Width = 20;

                            if (i >= 100)
                            {
                                m_tb.Width = 30;
                            }

                            m_tb.VerticalAlignment = VerticalAlignment.Center;
                            Hyperlink _hyper = new Hyperlink();
                            _hyper.Click += ImageInfoListPage_Click;
                            TextBlock _tb = new TextBlock();
                            _tb.Text = Convert.ToString(i);

                            if (i == 1)
                            {
                                _tb.FontWeight = FontWeights.UltraBold;
                                _tb.IsEnabled = false;
                                m_OldPageTextBlock = _tb;
                                SelectedPage = 1;
                            }

                            _hyper.Inlines.Add(_tb);
                            m_tb.Inlines.Add(_hyper);
                            m_PageStack.Children.Insert(2, m_tb);
                        }
                    });
                }

                RefreshcharInfo();

                m_LoadThreadFinished = true;
            })).Start();
        }

        private Process p = null;

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DateTime curDateTime = DateTime.Now;
            isTrainning = true;
            int m_TrainImageCount = 0;

            // 인식 엔진 프로그램 Kill.
            if (m_Process != null)
            {
                /*
                if (!m_Process.HasExited)
                {
                    m_Process.Kill();
                }

                m_Process.Close();
                m_Process.Dispose();
                m_Process = null;
                */
            }

            // 학습 시작 직전 GPU 메모리 초기화
            GPUMemoryClear();

            #region Train Data 생성
            if (!Directory.Exists(m_ProjectPath + "\\temp"))
            {
                Directory.CreateDirectory(m_ProjectPath + "\\temp");
            }

            if (Directory.Exists(m_ProjectPath + "\\temp\\Train"))
            {
                try
                {
                    Directory.Delete(m_ProjectPath + "\\temp\\Train", true);
                }
                catch
                {

                }
            }

            TrainDialog dialog = new TrainDialog();
            dialog.SetMessage("학습을 진행하고 있습니다.");
            await this.ShowMetroDialogAsync(dialog);

            dialog.Settinged += delegate
            {
                SettingWindow window = new SettingWindow();

                if (window.ShowDialog() == true)
                {
                    bool enableReserveEnd = config.GetBoolian("Train", "EnableReserveEnd", false);

                    if (enableReserveEnd)
                    {
                        string reserveEndTime = config.GetString("Train", "ReserveEndTime", "06:00");
                        string[] split = reserveEndTime.Split(':');
                        int h = Convert.ToInt32(split[0]);
                        int m = Convert.ToInt32(split[1]);

                        string h_str = h.ToString();
                        string m_str = m.ToString();

                        if (h < 10)
                        {
                            h_str = "0" + h_str;
                        }

                        if (m < 10)
                        {
                            m_str = "0" + m_str;
                        }
                        
                        DateTime dt = new DateTime(curDateTime.Year, curDateTime.Month, curDateTime.Day, h, m, 0);
                        DateTime endTime = DateTime.Parse(config.GetString("Train", "ReserveEndDate", "2021-12-02"));

                        // m_TrainController.SetMessage("학습을 진행하고 있습니다..." + Environment.NewLine + "학습 시작시간 : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                        //    "학습 종료시간 : " + dt.ToString("yyyy-MM-dd HH:mm:--") + Environment.NewLine + "학습이미지 수량 : " + m_TrainImageCount + "개");

                        bool enableReserveDate = config.GetBoolian("Train", "EnableReserveDate", false);

                        if (enableReserveDate)
                        {
                            dialog.SetMessage("학습을 진행하고 있습니다." + Environment.NewLine + "학습 시작시간 : " + curDateTime.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                            "학습 종료시간 : " + endTime.ToString("yyyy-MM-dd") + " " + h_str + ":" + m_str + ":--" + Environment.NewLine + "학습이미지 수량 : " + m_TrainImageCount + "개");
                        }
                        else
                        {
                            dialog.SetMessage("학습을 진행하고 있습니다." + Environment.NewLine + "학습 시작시간 : " + curDateTime.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                            "학습 종료시간 : " + dt.ToString("yyyy-MM-dd") + " " + h_str + ":" + m_str + ":--" + Environment.NewLine + "학습이미지 수량 : " + m_TrainImageCount + "개");
                        }
                    }
                    else
                    {
                        // m_TrainController.SetMessage("학습을 진행하고 있습니다..." + Environment.NewLine + "학습 시작시간 : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + "학습이미지 수량 : " + m_TrainImageCount + "개");
                        dialog.SetMessage("학습을 진행하고 있습니다." + Environment.NewLine + "학습 시작시간 : " + curDateTime.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + "학습이미지 수량 : " + m_TrainImageCount + "개");
                    }
                }
            };

            dialog.Canceled += delegate
            {
                dialog.SetTitle("학습 취소 중...");

                if (p != null)
                {
                    Capture capture = new Capture();
                    System.Drawing.Bitmap b = capture.CaptureApplication("darknet", m_ProjectName);

                    if (!Directory.Exists(m_ProjectPath + "\\학습그래프"))
                    {
                        Directory.CreateDirectory(m_ProjectPath + "\\학습그래프");
                    }

                    if (!Directory.Exists(m_ProjectPath + "\\학습그래프\\" + DateTime.Now.ToString("yyyyMMdd")))
                    {
                        Directory.CreateDirectory(m_ProjectPath + "\\학습그래프\\" + DateTime.Now.ToString("yyyyMMdd"));
                    }

                    b.Save(m_ProjectPath + "\\학습그래프\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + DateTime.Now.ToString("HHmmss") + ".png");

                    p.Kill();
                    p.Close();
                    p.Dispose();
                    p = null;

                    if (Directory.Exists(m_ProjectPath + "\\temp"))
                    {
                        try
                        {
                            Directory.Delete(m_ProjectPath + "\\temp", true);
                        }
                        catch
                        {

                        }
                    }
                }
            };

            string cmd = "";

            await Task.Run(() =>
            {
                #region .cfg File 생성
                MakecfgFile("Training");
                #endregion

                #region .Data File 생성
                // Train Data 구조체 필요...

                // 기존 데이터 파일 삭제
                if (File.Exists(m_ProjectPath + "\\" + m_ProjectName + ".data") == true)
                {
                    File.Delete(m_ProjectPath + "\\" + m_ProjectName + ".data");
                }

                FileStream fileStream = new FileStream(m_ProjectPath + "\\" + m_ProjectName + ".data", FileMode.CreateNew);
                StreamWriter writer = new StreamWriter(fileStream);

                // classes 항목 작성
                writer.WriteLine("classes = " + m_LabelList.Count.ToString());
                // Train 항목 작성
                writer.WriteLine("train = " + m_ProjectPath + "\\train.txt");
                writer.WriteLine("valid = " + m_ProjectPath + "\\valid.txt");
                writer.WriteLine("names = " + m_ProjectPath + "\\" + m_ProjectName + ".names");
                writer.WriteLine("backup = " + m_ProjectPath + "\\Weights");

                writer.Close();
                writer.Dispose();
                writer = null;

                fileStream.Close();
                fileStream.Dispose();
                fileStream = null;
                #endregion

                Directory.CreateDirectory(m_ProjectPath + "\\temp\\Train");

                string TrainFolder = m_ProjectPath + "\\temp\\Train";

                List<string> temp = new List<string>();

                // train File 항목 작성
                List<ImageInfo> list;

                if (m_IsOriginImageInfoList)
                {
                    list = m_ImageInfoList_Temp.ToList();
                }
                else
                {
                    list = m_ImageInfoList.ToList();
                }

                list.ToList().ForEach(x =>
                {
                    try
                    {
                        string filename = Path.GetFileName(x.FileName);
                        string ext = Path.GetExtension(x.FileName);
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(x.FileName);

                        string random_Name = fileNameWithoutExtension + "_" + Math.Abs(DateTime.Now.ToBinary()).ToString();

                        string temp_name = TrainFolder + "\\" + random_Name + ".txt";

                        bool is_labeled = false;

                        if (File.Exists(temp_name))
                        {
                            File.Delete(temp_name);
                        }

                        FileStream fs;

                        if (x.CharList.Count > 0)
                        {
                            m_TrainImageCount++;

                            string trainType = x.TrainType.ToString();

                            if (trainType == "NONE")
                            {
                                trainType = "NotLabeled";
                            }
                            else if (trainType == "LABELED")
                            {
                                trainType = "Labeled";
                            }
                            else if (trainType == "TRAINED")
                            {
                                trainType = "Trained";
                            }

                            if (!Directory.Exists(m_ProjectPath + "\\Images\\Trained"))
                            {
                                Directory.CreateDirectory(m_ProjectPath + "\\Images\\Trained");
                            }

                            if (trainType != "Trained")
                            {
                                try
                                {
                                    File.Copy(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename, m_ProjectPath + "\\Images\\Trained\\" + filename, true);
                                    File.Delete(m_ProjectPath + "\\Images\\" + trainType + "\\" + filename);
                                }
                                catch (Exception ex)
                                {
                                    LogManager.Error(ex.Message);
                                }

                                if (File.Exists(m_ProjectPath + "\\Images\\" + trainType + "\\" + fileNameWithoutExtension + ".hsr"))
                                {
                                    try
                                    {
                                        File.Copy(m_ProjectPath + "\\Images\\" + trainType + "\\" + fileNameWithoutExtension + ".hsr", m_ProjectPath + "\\Images\\Trained\\" + fileNameWithoutExtension + ".hsr", true);
                                        File.Delete(m_ProjectPath + "\\Images\\" + trainType + "\\" + fileNameWithoutExtension + ".hsr");
                                    }
                                    catch
                                    {

                                    }
                                }

                                x.FileName = m_ProjectPath + "\\Images\\Trained\\" + filename;

                                x.TrainType = TrainType.TRAINED;

                                Save_ImageList_Information(x);
                            }
                        }

                        if (m_IsOriginImageInfoList)
                        {
                            for (int _i = 0; _i < m_ImageInfoList_Temp.Count; _i++)
                            {
                                if (Path.GetFileName(m_ImageInfoList_Temp[_i].FileName) == Path.GetFileName(x.FileName))
                                {
                                    m_ImageInfoList_Temp[_i] = x;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            for (int _i = 0; _i < m_ImageInfoList.Count; _i++)
                            {
                                if (Path.GetFileName(m_ImageInfoList[_i].FileName) == Path.GetFileName(x.FileName))
                                {
                                    m_ImageInfoList[_i] = x;
                                    break;
                                }
                            }
                        }

                        for (int _i = 0; _i < PartImageInfoList.Count; _i++)
                        {
                            if (Path.GetFileName(PartImageInfoList[_i].FileName) == Path.GetFileName(x.FileName))
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    PartImageInfoList[_i] = x;
                                });
                            }
                        }

                        x.CharList.ToList().ForEach(y =>
                        {
                            if (y.Is_Labeled && y.Labeled_Name != "")
                            {
                                if (File.Exists(temp_name))
                                    fs = new FileStream(temp_name, FileMode.Append);
                                else
                                    fs = new FileStream(temp_name, FileMode.Create);

                                StreamWriter w = new StreamWriter(fs);

                                double Crop_X, Crop_Y, Crop_Width, Crop_Height;

                                if (m_IsFullROI)
                                {
                                    Crop_X = y.Labeled_X;
                                    Crop_Y = y.Labeled_Y;
                                    Crop_Width = y.Labeled_Width;
                                    Crop_Height = y.Labeled_Height;
                                }
                                else
                                {
                                    Crop_X = ((y.Labeled_X * (double)x.Width) - (double)mcurrentROI.X) / (double)mcurrentROI.Width;
                                    Crop_Y = ((y.Labeled_Y * (double)x.Height) - (double)mcurrentROI.Y) / (double)mcurrentROI.Height;
                                    Crop_Width = (x.Width * y.Labeled_Width) / mcurrentROI.Width;
                                    Crop_Height = (x.Height * y.Labeled_Height) / mcurrentROI.Height;
                                }

                                // 크롭된 이미지에 맞게 영역 변경
                                w.Write(Get_LabelIndex(y.Labeled_Name) + " ");
                                w.Write(Crop_X.ToString() + " ");
                                w.Write(Crop_Y.ToString() + " ");
                                w.Write(Crop_Width.ToString() + " ");
                                w.WriteLine(Crop_Height.ToString());

                                is_labeled = true;

                                w.Close();
                                w.Dispose();
                                fs.Close();
                                fs.Dispose();
                                w = null;
                                fs = null;
                            }
                        });

                        if (is_labeled)
                        {
                            if (m_IsFullROI)
                            {
                                File.Copy(x.FileName, TrainFolder + "\\" + random_Name + ".bmp", true);
                            }
                            else
                            {
                                #region ROI 설정한 이미지 파일 생성...                    
                                SaveCroppedBitmap(ImageCrop(x.FileName, new Int32Rect(mcurrentROI.X, mcurrentROI.Y, mcurrentROI.Width, mcurrentROI.Height)), TrainFolder + "\\" + random_Name + ".bmp");
                                #endregion
                            }

                            string s = File.ReadAllText(TrainFolder + "\\" + random_Name + ".txt");

                            if (s != "" && s != null)
                            {
                                temp.Add(TrainFolder + "\\" + random_Name + ".bmp");
                            }
                            else
                            {
                                File.Delete(TrainFolder + "\\" + random_Name + ".txt");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error(ex.Message);
                    }
                });

                #region 리스트 랜덤으로 섞기
                Random rng = new Random();
                int n = temp.Count;
                while (n > 1)
                {
                    n--;
                    int k = rng.Next(n + 1);
                    string value = temp[k];
                    temp[k] = temp[n];
                    temp[n] = value;
                }
                #endregion

                // 기존 데이터 파일 삭제
                if (File.Exists(m_ProjectPath + "\\train.txt"))
                {
                    File.Delete(m_ProjectPath + "\\train.txt");
                }

                // 기존 데이터 파일 삭제
                if (File.Exists(m_ProjectPath + "\\valid.txt"))
                {
                    File.Delete(m_ProjectPath + "\\valid.txt");
                }

                FileStream train_fileStream = new FileStream(m_ProjectPath + "\\train.txt", FileMode.CreateNew);
                StreamWriter train_writer = new StreamWriter(train_fileStream);
                FileStream validation_fileStream = new FileStream(m_ProjectPath + "\\valid.txt", FileMode.CreateNew);
                StreamWriter validation_writer = new StreamWriter(validation_fileStream);

                int count = (int)(temp.Count * 0.7);

                bool random = true;

                int i = 0;

                if (random)
                {
                    for (; i < count; i++)
                    {
                        train_writer.WriteLine(temp[i]);
                    }

                    for (; i < temp.Count; i++)
                    {
                        validation_writer.WriteLine(temp[i]);
                    }
                }
                else
                {
                    for (i = 0; i < temp.Count; i++)
                    {
                        train_writer.WriteLine(temp[i]);
                    }

                    for (i = 0; i < temp.Count; i++)
                    {
                        validation_writer.WriteLine(temp[i]);
                    }
                }

                train_writer.Close();
                train_writer = null;
                train_fileStream.Close();
                train_fileStream = null;

                validation_writer.Close();
                validation_writer = null;
                validation_fileStream.Close();
                validation_fileStream = null;
                #endregion

                if (Directory.Exists(m_ProjectPath + "\\weights") == true)
                {
                    Directory.Delete(m_ProjectPath + "\\weights", true);
                }

                Directory.CreateDirectory(m_ProjectPath + "\\weights");

                if (YoloVersionStr == "v3")
                {
                    cmd = "detector train " + m_ProjectPath + "\\" + m_ProjectName + ".data " + m_ProjectPath + "\\" + m_ProjectName + ".cfg " + m_ProjectPath + "\\" + m_ProjectName + ".weights " + AppDomain.CurrentDomain.BaseDirectory + "yolov3\\darknet53.conv.74 -map";

                    ProcessStartInfo info = new ProcessStartInfo();
                    info.FileName = AppDomain.CurrentDomain.BaseDirectory + "yolov3\\darknet.exe";
                    info.WindowStyle = ProcessWindowStyle.Hidden;
                    info.CreateNoWindow = true;
                    info.Arguments = cmd;

                    info.UseShellExecute = false;
                    info.RedirectStandardOutput = true;

                    p = new Process();
                    p.StartInfo = info;

                    p.Start();
                    p.WaitForExit();

                    if (p != null)
                    {
                        string result = p.StandardOutput.ReadToEnd();
                        LogManager.Info(result);
                        p.Close();
                        p.Dispose();
                        p = null;
                    }

                    // Process.Start(AppDomain.CurrentDomain.BaseDirectory + "yolov3\\darknet.exe", cmd);
                }
                else if (YoloVersionStr == "v4")
                {
                    string c = "detector train \"" + m_ProjectPath + "\\" + m_ProjectName + ".data\" \"" + m_ProjectPath + "\\" + m_ProjectName + ".cfg\" \"" + AppDomain.CurrentDomain.BaseDirectory + "yolov4\\yolov4.conv.137\" -map";

                    ProcessStartInfo _info = new ProcessStartInfo();
                    _info.FileName = AppDomain.CurrentDomain.BaseDirectory + "yolov4\\darknet.exe";
                    _info.CreateNoWindow = false;
                    _info.UseShellExecute = false;
                    _info.Arguments = c;

                    bool enableReserveEnd = config.GetBoolian("Train", "EnableReserveEnd", false);

                    if (enableReserveEnd)
                    {
                        bool enableReserveDate = config.GetBoolian("Train", "EnableReserveDate", false);
                        string reserveEndTime = config.GetString("Train", "ReserveEndTime", "06:00");
                        string[] split = reserveEndTime.Split(':');
                        int h = Convert.ToInt32(split[0]);
                        int m = Convert.ToInt32(split[1]);

                        string h_str = h.ToString();
                        string m_str = m.ToString();

                        if (h < 10)
                        {
                            h_str = "0" + h_str;
                        }

                        if (m < 10)
                        {
                            m_str = "0" + m_str;
                        }

                        DateTime curTime = DateTime.Now;
                        DateTime endTime = DateTime.Parse(config.GetString("Train", "ReserveEndDate", "2021-12-02"));

                        // m_TrainController.SetMessage("학습을 진행하고 있습니다..." + Environment.NewLine + "학습 시작시간 : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                        //    "학습 종료시간 : " + dt.ToString("yyyy-MM-dd HH:mm:--") + Environment.NewLine + "학습이미지 수량 : " + m_TrainImageCount + "개");

                        if (enableReserveDate)
                        {
                            dialog.SetMessage("학습을 진행하고 있습니다." + Environment.NewLine + "학습 시작시간 : " + curDateTime.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                            "학습 종료시간 : " + endTime.ToString("yyyy-MM-dd") + " " + h_str + ":" + m_str + ":--" + Environment.NewLine + "학습이미지 수량 : " + m_TrainImageCount + "개");
                        }
                        else
                        {
                            dialog.SetMessage("학습을 진행하고 있습니다." + Environment.NewLine + "학습 시작시간 : " + curDateTime.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                            "학습 종료시간 : " + curDateTime.ToString("yyyy-MM-dd") + " " + h_str + ":" + m_str + ":--" + Environment.NewLine + "학습이미지 수량 : " + m_TrainImageCount + "개");
                        }
                    }
                    else
                    {
                        // m_TrainController.SetMessage("학습을 진행하고 있습니다..." + Environment.NewLine + "학습 시작시간 : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + "학습이미지 수량 : " + m_TrainImageCount + "개");
                        dialog.SetMessage("학습을 진행하고 있습니다." + Environment.NewLine + "학습 시작시간 : " + curDateTime.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + "학습이미지 수량 : " + m_TrainImageCount + "개");
                    }

                    p = Process.Start(_info);
                    p.WaitForExit();

                    if (!dialog.IsCanceled && File.Exists(AppDomain.CurrentDomain.BaseDirectory + "chart_" + m_ProjectName + ".png"))
                    {
                        DateTime lastWriteTime = File.GetLastWriteTime(AppDomain.CurrentDomain.BaseDirectory + "chart_" + m_ProjectName + ".png");
                        
                        if (lastWriteTime.Year == curDateTime.Year && lastWriteTime.Month == curDateTime.Month && (lastWriteTime.Day == curDateTime.Day ||
                            (lastWriteTime.Day + 1) == (curDateTime.Day + 1)))
                        {
                            if (!Directory.Exists(m_ProjectPath + "\\학습그래프"))
                            {
                                Directory.CreateDirectory(m_ProjectPath + "\\학습그래프");
                            }

                            if (!Directory.Exists(m_ProjectPath + "\\학습그래프\\" + DateTime.Now.ToString("yyyyMMdd")))
                            {
                                Directory.CreateDirectory(m_ProjectPath + "\\학습그래프\\" + DateTime.Now.ToString("yyyyMMdd"));
                            }

                            File.Copy(AppDomain.CurrentDomain.BaseDirectory + "chart_" + m_ProjectName + ".png", m_ProjectPath + "\\학습그래프\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + DateTime.Now.ToString("HHmmss") + ".png");
                        }
                    }

                    if (MessageBox.Show("학습 완료된 데이터로 적용하시겠습니까?", "확인", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        if (File.Exists(m_ProjectPath + "\\weights\\" + m_ProjectName + "_last.weights"))
                        {
                            File.Copy(m_ProjectPath + "\\weights\\" + m_ProjectName + "_last.weights", m_ProjectPath + "\\" + m_ProjectName + ".weights", true);
                        }
                    }

                    // 학습 종료 직후 GPU 메모리 초기화
                    GPUMemoryClear();

                    dialog.SetTitle("학습 종료 중...");
                    dialog.SetEnableSettingButton(false);
                    dialog.SetEnableCancelButton(false);
                    dialog.SetMessage("이미지 리스트를 정리하고 있습니다.");
                    Refresh_ImageList(m_ImageShowType);

                    /*
                    Dispatcher.Invoke(() =>
                    {
                        PageStackRefresh();
                    });
                    */

                    dialog.SetMessage("데이터를 저장 중 입니다.");
                    Save_ImageList_Information();

                    isTrainning = false;

                    dialog.SetMessage("인식 엔진 프로그램을 실행하는 중 입니다.");
                    // 인식 엔진 프로그램 재실행.
                    LogManager.Action("인식 엔진 프로그램 재실행 시도");
                    ProcessStartInfo _i = new ProcessStartInfo();
                    _i.FileName = AppDomain.CurrentDomain.BaseDirectory + "Detector\\Detector.exe";
                    /*
                    _i.UseShellExecute = false;
                    _i.RedirectStandardInput = true;
                    _i.RedirectStandardOutput = true;
                    _i.RedirectStandardError = true;
                    _i.CreateNoWindow = true;
                    _i.WindowStyle = ProcessWindowStyle.Hidden;
                    */
                    m_Process = new Process();
                    // m_Process.EnableRaisingEvents = false;
                    m_Process.StartInfo = _i;
                    /*
                    m_Process.OutputDataReceived += Process_OutputDataReceived;
                    m_Process.ErrorDataReceived += Process_ErrorDataReceived;
                    */
                    // m_Process.Start();
                    /*
                    m_Process.BeginOutputReadLine();
                    m_Process.BeginErrorReadLine();
                    */
                    LogManager.Action("인식 엔진 프로그램 재실행 완료");
                    dialog.SetMessage("인식 엔진 프로그램을 실행 완료");

                    if (m_WeightsFile == "")
                    {
                        m_WeightsFile = m_ProjectPath + "\\" + m_ProjectName + ".weights";
                    }

                    dialog.SetMessage("인식 엔진 프로그램에 파라미터를 전송하는 중 입니다.");

                    /*
                    m_Process.StandardInput.WriteLine(m_ProjectPath + "\\" + m_ProjectName + ".cfg" + Environment.NewLine + m_WeightsFile +
                        Environment.NewLine + m_ProjectPath + "\\" + m_ProjectName + ".names" + Environment.NewLine + Convert.ToString(NetWidth) + Environment.NewLine +
                        Convert.ToString(NetHeight) + Environment.NewLine + Convert.ToString(NetChannels));
                    */

                    LogManager.Action("인식 엔진 프로그램 파라미터 전송 완료");
                    dialog.SetMessage("인식 엔진 프로그램에 파라미터 전송하였습니다.");
                }
            });

            await this.HideMetroDialogAsync(dialog);
        }

        private void CharList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            //m_Network_imageWidth = 608;
            //m_Network_imageHeight = 608;

            #region .cfg File 생성
            MakecfgFile("Training");
            #endregion

            //CompareCfg(m_ProjectPath + "\\" + m_ProjectName + "_Source.cfg", m_ProjectPath + "\\" + m_ProjectName + ".cfg");

            #region .Data File 생성
            // Train Data 구조체 필요...

            // 기존 데이터 파일 삭제
            if (File.Exists(m_ProjectPath + "\\" + m_ProjectName + ".data") == true)
            {
                File.Delete(m_ProjectPath + "\\" + m_ProjectName + ".data");
            }

            FileStream fileStream = new FileStream(m_ProjectPath + "\\" + m_ProjectName + ".data", FileMode.CreateNew);
            StreamWriter writer = new StreamWriter(fileStream);

            // classes 항목 작성
            writer.WriteLine("classes = " + m_LabelList.Count.ToString());
            // Train 항목 작성
            writer.WriteLine("train = " + m_ProjectPath + "\\train.txt");
            writer.WriteLine("valid = " + m_ProjectPath + "\\valid.txt");
            writer.WriteLine("names = " + m_ProjectPath + "\\" + m_ProjectName + ".names");
            writer.WriteLine("backup = " + m_ProjectPath + "\\Weights");

            writer.Close();
            writer = null;

            fileStream.Close();
            fileStream = null;
            #endregion

            /*
            try
            {
                m_MaxFindCount = int.Parse(Findcount.Text);
            }
            catch
            {
                m_MaxFindCount = 17;
            }
            */

            if (Directory.Exists(m_ProjectPath + "\\Temp\\Finded"))
            {
                Directory.Delete(m_ProjectPath + "\\Temp\\Finded", true);
            }

            Directory.CreateDirectory(m_ProjectPath + "\\Temp\\Finded");

            for (int i = 0; i < m_ImageInfoList.Count; i++)
            {
                ImageInfo imageInfo = m_ImageInfoList[i];

                try
                {
                    imageInfo = Detect(imageInfo);

                    m_ImageInfoList.RemoveAt(i);
                    m_ImageInfoList.Insert(i, imageInfo);
                }
                catch (Exception ex)
                {
                    LogManager.Error("감지 실패 : " + ex.Message);
                }
            }

            Save_ImageList_Information();

            RefreshcharInfo();
        }

        private void Dp_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void LbImageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (dp.BitmapImage != null)
                {
                    if (!m_IsSaved)
                    {
                        if (m_IsOriginImageInfoList)
                        {
                            for (int i = 0; i < m_ImageInfoList_Temp.Count; i++)
                            {
                                if (Path.GetFileName(m_ImageInfoList_Temp[i].FileName) == Path.GetFileName(m_PreSaveImageInfo.FileName))
                                {
                                    m_ImageInfoList_Temp[i] = m_PreSaveImageInfo;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < m_ImageInfoList.Count; i++)
                            {
                                if (Path.GetFileName(m_ImageInfoList[i].FileName) == Path.GetFileName(m_PreSaveImageInfo.FileName))
                                {
                                    m_ImageInfoList[i] = m_PreSaveImageInfo;
                                    break;
                                }
                            }
                        }

                        for (int i = 0; i < PartImageInfoList.Count; i++)
                        {
                            if (Path.GetFileName(PartImageInfoList[i].FileName) == Path.GetFileName(m_PreSaveImageInfo.FileName))
                            {
                                PartImageInfoList[i] = m_PreSaveImageInfo;
                                break;
                            }
                        }
                    }
                }

                m_lbImageList_MouseLeftButtonDown = false;

                if (lbImageList.SelectedItem == null)
                {
                    return;
                }

                m_SelectedImageIndex = -1;
                int visblecount = 0;
                int count = 0;

                PartImageInfoList.ToList().ForEach(x =>
                {
                    if (x.IsVisble)
                    {
                        if (visblecount == lbImageList.SelectedIndex)
                        {
                            m_SelectedImageIndex = count;
                        }

                        visblecount++;
                    }

                    count++;
                });

                if (dp.SelectRectangle != null)
                {
                    if (m_IsTempCharInfo)
                    {
                        // m_SelectedImageInfo.CharList.Add(m_TempCharInfo);
                        m_IsTempCharInfo = false;
                    }

                    DrawImageLabel();

                    dp.SelectRectangle = null;
                }

                m_SelectedImageInfo = PartImageInfoList[m_SelectedImageIndex];

                if (Path.GetFileName(m_SelectedImageInfo.FileName) != Path.GetFileName(m_PreSaveImageInfo.FileName))
                {
                    m_IsSaved = true;
                    SaveBtnVisibility = Visibility.Hidden;
                }

                m_PreSaveImageInfo = new ImageInfo()
                {
                    FileName = m_SelectedImageInfo.FileName,
                    CharList = new List<CharInfo>(),
                    Width = m_SelectedImageInfo.Width,
                    Height = m_SelectedImageInfo.Height,
                    DateTime = m_SelectedImageInfo.DateTime,
                    IsVisble = m_SelectedImageInfo.IsVisble,
                    Find_String = m_SelectedImageInfo.Find_String,
                    Thumnail_FileName = m_SelectedImageInfo.Thumnail_FileName,
                    TrainType = m_SelectedImageInfo.TrainType
                };

                for (int i = 0; i < m_SelectedImageInfo.CharList.Count; i++)
                {
                    m_PreSaveImageInfo.CharList.Add(m_SelectedImageInfo.CharList[i]);
                }

                if (!File.Exists(m_SelectedImageInfo.FileName))
                {
                    return;
                }

                m_OldSelectedImageInfo = m_SelectedImageInfo;

                dp.ConfirmButtonVisibility = Visibility.Collapsed;
                dp.IsShowRectangle = false;
                dp.SelectRectangle = null;
                dp.RemoveSelectRectangle();

                /*
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(m_SelectedImageInfo.FileName);
                bi.EndInit();
                dp.BitmapImage = bi;
                */

                BitmapImage bi = new BitmapImage();
                Mat mat = Cv2.ImRead(m_SelectedImageInfo.FileName);
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                // bi.UriSource = new Uri(imageInfo.FileName);
                bi.StreamSource = mat.ToMemoryStream();
                bi.EndInit();
                mat.Dispose();
                mat = null;

                dp.BitmapImage = bi;

                m_FileName = m_SelectedImageInfo.FileName;
                m_Gamma = 1.0;

                DrawImageLabel();

                dp.Focus();
            }
            catch (Exception ex)
            {
                LogManager.Error(ex.Message);
            }
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < m_ImageInfoList.Count; i++)
            {
                ImageInfo imageinfo = m_ImageInfoList[i];

                for (int j = 0; j < m_ImageInfoList[i].CharList.Count; j++)
                {
                    if (!m_ImageInfoList[i].CharList[j].Is_Finded)
                    {
                        m_ImageInfoList[i].CharList.RemoveAt(j);
                    }
                }

                for (int j = 0; j < m_ImageInfoList[i].CharList.Count; j++)
                {
                    CharInfo charinfo = m_ImageInfoList[i].CharList[j];

                    charinfo.Is_Labeled = true;
                    charinfo.Labeled_Name = charinfo.Finded_Name;

                    if (m_IsFullROI == true)
                    {
                        charinfo.Labeled_X = charinfo.Finded_X;
                        charinfo.Labeled_Y = charinfo.Finded_Y;
                        charinfo.Labeled_Width = charinfo.Finded_Width;
                        charinfo.Labeled_Height = charinfo.Finded_Height;
                    }
                    else
                    {
                        charinfo.Labeled_X = (mcurrentROI.Width * charinfo.Finded_X + mcurrentROI.X) / m_ImageInfoList[i].Width;
                        charinfo.Labeled_Y = (mcurrentROI.Height * charinfo.Finded_Y + mcurrentROI.Y) / m_ImageInfoList[i].Height;
                        charinfo.Labeled_Width = (mcurrentROI.Width * charinfo.Finded_Width) / m_ImageInfoList[i].Width;
                        charinfo.Labeled_Height = (mcurrentROI.Height * charinfo.Finded_Height) / m_ImageInfoList[i].Height;
                    }

                    charinfo.Is_Finded = false;
                    charinfo.Finded_X = 0;
                    charinfo.Finded_Y = 0;
                    charinfo.Finded_Width = 0;
                    charinfo.Finded_Height = 0;
                    charinfo.Finded_Score = 0;

                    m_ImageInfoList[i].CharList.RemoveAt(j);
                    m_ImageInfoList[i].CharList.Insert(j, charinfo);

                }

                m_ImageInfoList.RemoveAt(i);
                m_ImageInfoList.Insert(i, imageinfo);
            }
            m_ImageInfoList.ToList().ForEach(x =>
            {
                //Save_Labeling_Data(x.FileName, x);
            });

            Save_NameFile();
            Save_ImageList_Information();

            Load_LabelList();
            RefreshcharInfo();
        }

        public async void Button_Click_5(object sender, RoutedEventArgs e)
        {
            ProgressDialogController controller = await this.ShowProgressAsync("저장 중...", "프로젝트를 저장 중 입니다...");
            controller.SetIndeterminate();

            await Task.Run(() =>
            {
                Save_ImageList_Information();
            });

            await controller.CloseAsync();
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            m_SearchFilter = Filter.Text;
            m_ImageShowType = ImageShowType.Search;
            Refresh_ImageList(m_ImageShowType);

            /*
            m_PageStack.Children.RemoveRange(2, m_PageStack.Children.Count - 4);

            m_StartPage = m_LimitPage - (m_LimitPage % m_PagePerGroup);
            m_EndPage = m_LimitPage;

            ObservableCollection<ImageInfo> temp = m_ImageInfoList;

            for (int i = m_EndPage; i > m_StartPage; i--)
            {
                TextBlock m_tb = new TextBlock();
                m_tb.Margin = new Thickness(5);
                m_tb.FontSize = 16;
                m_tb.Width = 20;

                if (i >= 100)
                {
                    m_tb.Width = 30;
                }

                m_tb.VerticalAlignment = VerticalAlignment.Center;
                Hyperlink _hyper = new Hyperlink();
                _hyper.Click += ImageInfoListPage_Click;
                TextBlock _tb = new TextBlock();
                _tb.Text = Convert.ToString(i);

                if (Convert.ToInt32(m_OldPageTextBlock.Text) == i)
                {
                    _tb.FontWeight = FontWeights.UltraBold;
                    _tb.IsEnabled = false;
                    _tb.FontSize = 16;
                }

                if (i == m_StartPage + 1)
                {
                    _tb.FontWeight = FontWeights.UltraBold;
                    _tb.FontSize = 16;
                    _tb.IsEnabled = false;
                    m_OldPageTextBlock = _tb;
                    SelectedPage = i;

                    if (temp.Count > 0)
                    {
                        PartImageInfoList.Clear();

                        int _start = m_CntPerPage * (i - 1);
                        int _end = _start + m_CntPerPage;

                        if (_end > temp.Count)
                        {
                            _end = temp.Count;
                        }

                        for (int j = _start; j < _end; j++)
                        {
                            PartImageInfoList.Add(temp[j]);
                        }
                    }
                }

                _hyper.Inlines.Add(_tb);
                m_tb.Inlines.Add(_hyper);
                m_PageStack.Children.Insert(2, m_tb);
            }
            */

            SelectedPage = 1;

            LimitPage = m_ImageInfoList.Count / m_CntPerPage;

            if (m_ImageInfoList.Count % m_CntPerPage > 0)
            {
                LimitPage++;
            }

            m_StartPage = 0;

            if (m_LimitPage < m_PagePerGroup)
            {
                m_EndPage = m_LimitPage;
            }
            else
            {
                m_EndPage = m_PagePerGroup;
            }

            if (m_PageStack != null)
            {
                Dispatcher.Invoke(() =>
                {
                    m_PageStack.Children.RemoveRange(2, m_PageStack.Children.Count - 4);

                    PartImageInfoList.Clear();

                    int _start = m_CntPerPage * 0;
                    int _end = _start + m_CntPerPage;

                    if (_end > m_ImageInfoList.Count)
                    {
                        _end = m_ImageInfoList.Count;
                    }

                    for (int i = _start; i < _end; i++)
                    {
                        PartImageInfoList.Add(m_ImageInfoList[i]);
                    }

                    for (int i = m_EndPage; i > m_StartPage; i--)
                    {
                        TextBlock m_tb = new TextBlock();
                        m_tb.Margin = new Thickness(5);
                        m_tb.FontSize = 16;
                        m_tb.Width = 20;

                        if (i >= 100)
                        {
                            m_tb.Width = 30;
                        }

                        m_tb.VerticalAlignment = VerticalAlignment.Center;
                        Hyperlink _hyper = new Hyperlink();
                        _hyper.Click += ImageInfoListPage_Click;
                        TextBlock _tb = new TextBlock();
                        _tb.Text = Convert.ToString(i);

                        if (i == 1)
                        {
                            _tb.FontWeight = FontWeights.UltraBold;
                            _tb.IsEnabled = false;
                            m_OldPageTextBlock = _tb;
                        }

                        _hyper.Inlines.Add(_tb);
                        m_tb.Inlines.Add(_hyper);
                        m_PageStack.Children.Insert(2, m_tb);
                    }
                });
            }

            m_ScrollViewer.ScrollToVerticalOffset(0);
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            LogManager.Action("전체 테스트 시작");

            if (m_IsAllTesting)
            {
                return;
            }

            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
            {
                Detect(dialog.FileName);
            }

            dialog.Dispose();
            dialog = null;

            LogManager.Action("전체 테스트 종료");
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            try
            {
                if (m_TempImageinfo.CharList != null && m_TempImageinfo.CharList.Count > 0)
                    m_TempImageinfo.CharList.Clear();

                m_ImageInfoList[m_SelectedImageIndex].CharList.ToList().ForEach(x =>
                 {
                     m_TempImageinfo.CharList.Add(x);
                 });
            }
            catch (Exception ex)
            {
                LogManager.Error(ex.Message);
            }
        }

        static int lineCount = 0;
        static StringBuilder output = new StringBuilder();

        static double max_map = 0;
        static string max_weights = "";
        static int totalcount = 0;
        static string current_weight = "";
        static int count = 0;

        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            // Process.Start("D:\\Yolo\\marker\\yolo_mark.exe", m_ProjectPath + "\\temp\\Train " + m_ProjectPath + "\\train.txt " + m_ProjectPath + "\\" + m_ProjectName + ".names");
            // Process.Start("yolov4\\", m_ProjectPath + "\\temp\\Train " + m_ProjectPath + "\\train.txt " + m_ProjectPath + "\\" + m_ProjectName + ".names");
        }

        private static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine.Data != null)
            {

                if (outLine.Data.Contains("mean average precision (mAP@0.50)"))
                {
                    double map = double.Parse(outLine.Data.Substring(outLine.Data.IndexOf('=') + 2, 7));

                    if (map > max_map)
                    {
                        max_map = map;
                        max_weights = current_weight;
                    }
                }
            }
        }

        private void Button_Click_10(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            string weightsFilePath = "";

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                weightsFilePath = dialog.FileName;

                /*
                if (yoloWrapper != null)
                {
                    yoloWrapper.Dispose();
                    yoloWrapper = null;

                    GpuConfig gpuConfig = new GpuConfig();
                    gpuConfig.GpuIndex = 0;
                    yoloWrapper = new YoloWrapper(m_ProjectPath + "\\" + m_ProjectName + ".cfg", weightsFilePath, m_ProjectPath + "\\" + m_ProjectName + ".names", gpuConfig);

                    m_IsTrained = true;
                }
                */
            }
        }

        private void Button_Click_11(object sender, RoutedEventArgs e)
        {
            m_ImageInfoList[m_SelectedImageIndex].CharList.Clear();

            m_TempImageinfo.CharList.ToList().ForEach(x =>
            {
                m_ImageInfoList[m_SelectedImageIndex].CharList.Add(x);
            });

            DrawImageLabel();
        }

        private void Button_Click_13(object sender, RoutedEventArgs e)
        {
            // OpenFileDialog dlg = new OpenFileDialog();
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();

            string filename = "";

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                filename = dlg.FileName;
            }
            else
            {
                return;
            }

            Score = 40;

            #region .cfg File 생성
            MakecfgFile("Testing");
            #endregion

            //CompareCfg(m_ProjectPath + "\\" + m_ProjectName + "_Source.cfg", m_ProjectPath + "\\" + m_ProjectName + ".cfg");

            #region .Data File 생성
            // Train Data 구조체 필요...

            // 기존 데이터 파일 삭제
            if (File.Exists(m_ProjectPath + "\\" + m_ProjectName + ".data") == true)
            {
                File.Delete(m_ProjectPath + "\\" + m_ProjectName + ".data");
            }

            FileStream fileStream = new FileStream(m_ProjectPath + "\\" + m_ProjectName + ".data", FileMode.CreateNew);
            StreamWriter writer = new StreamWriter(fileStream);

            // classes 항목 작성
            writer.WriteLine("classes = " + m_LabelList.Count.ToString());
            // Train 항목 작성
            writer.WriteLine("train = " + m_ProjectPath + "\\train.txt");
            writer.WriteLine("valid = " + m_ProjectPath + "\\valid.txt");
            writer.WriteLine("names = " + m_ProjectPath + "\\" + m_ProjectName + ".names");
            writer.WriteLine("backup = " + m_ProjectPath + "\\Weights");

            writer.Close();
            writer = null;

            fileStream.Close();
            fileStream = null;
            #endregion

            /*
            try
            {
                m_MaxFindCount = int.Parse(Findcount.Text);
            }
            catch
            {
                m_MaxFindCount = 17;
            }
            */

            if (Directory.Exists(m_ProjectPath + "\\Temp\\Finded"))
            {
                Directory.Delete(m_ProjectPath + "\\Temp\\Finded", true);
            }

            Directory.CreateDirectory(m_ProjectPath + "\\Temp\\Finded");

            ImageInfo imageInfo = new ImageInfo(filename);

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                imageInfo = Detect(imageInfo);

                sw.Stop();

                dp.BitmapImage = new BitmapImage(new Uri(filename));
                DrawImageLabel(imageInfo.CharList, imageInfo);
            }
            catch (Exception ex)
            {
                LogManager.Error("감지 실패 : " + ex.Message);
            }
        }

        private void Label_Delete_Click(object sender, RoutedEventArgs e)
        {
            StructResult label = (StructResult)dg.SelectedItem;

            if (label == null)
            {
                return;
            }

            m_LabelList.Remove(label);

            if (File.Exists(m_ProjectPath + "\\" + m_ProjectName + ".names"))
            {
                string[] arr = File.ReadAllLines(m_ProjectPath + "\\" + m_ProjectName + ".names", Encoding.Default);

                List<string> lines = arr.ToList();

                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i] == label.LabelName)
                    {
                        lines.Remove(lines[i]);
                    }
                }

                arr = lines.ToArray();

                File.WriteAllLines(m_ProjectPath + "\\" + m_ProjectName + ".names", arr);
            }

            m_ImageInfoList.ToList().ForEach(x =>
            {
                string _ext = Path.GetExtension(x.FileName);
                string datafile = x.FileName.Substring(0, x.FileName.Length - _ext.Length) + ".hsr";

                if (File.Exists(datafile))
                {
                    FileStream fs = new FileStream(datafile, FileMode.Open, FileAccess.Read);
                    StreamReader sr = new StreamReader(fs);

                    string line = "";
                    string content = "";

                    while ((line = sr.ReadLine()) != null)
                    {
                        string width = line;
                        content += width + Environment.NewLine;

                        string height = sr.ReadLine();
                        content += height + Environment.NewLine;

                        int count = int.Parse(sr.ReadLine());
                        content += count + Environment.NewLine;

                        string type = sr.ReadLine();
                        content += type + Environment.NewLine;

                        for (int i = 0; i < count; i++)
                        {
                            string is_label = sr.ReadLine();
                            string labeled_name = sr.ReadLine();
                            string label_x = sr.ReadLine();
                            string label_y = sr.ReadLine();
                            string labeled_width = sr.ReadLine();
                            string labeled_height = sr.ReadLine();
                            string is_finded = sr.ReadLine();
                            string finded_name = sr.ReadLine();
                            string finded_x = sr.ReadLine();
                            string finded_y = sr.ReadLine();
                            string finded_width = sr.ReadLine();
                            string finded_height = sr.ReadLine();
                            string finded_score = sr.ReadLine();

                            if (label.LabelName != labeled_name)
                            {
                                for (int j = 0; j < ImageInfoList.Count; j++)
                                {
                                    for (int k = 0; k < ImageInfoList[j].CharList.Count; k++)
                                    {
                                        if (ImageInfoList[j].CharList[k].Labeled_Name == label.LabelName)
                                        {
                                            ImageInfoList[j].CharList.Remove(ImageInfoList[j].CharList[k]);
                                        }
                                    }
                                }

                                content += is_label + Environment.NewLine + labeled_name + Environment.NewLine + label_x + Environment.NewLine + label_y + Environment.NewLine + labeled_width + Environment.NewLine + labeled_height + Environment.NewLine +
                                    is_finded + Environment.NewLine + finded_name + Environment.NewLine + finded_x + Environment.NewLine + finded_y + Environment.NewLine + finded_width + Environment.NewLine + finded_height + Environment.NewLine + finded_score + Environment.NewLine;
                            }
                        }
                    }

                    sr.Close();
                    sr.Dispose();
                    fs.Close();
                    fs.Dispose();
                    sr = null;
                    fs = null;

                    File.WriteAllText(datafile, content);
                }
            });

            DrawImageLabel();

            Save_NameFile();
            Save_ImageList_Information();

            Load_LabelList();
            RefreshcharInfo();
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (m_Process != null)
            {
                if (!m_Process.HasExited)
                {
                    m_Process.Kill();
                }
                
                m_Process.Close();
                m_Process.Dispose();
                m_Process = null;

                LogManager.Action("인식 엔진 프로그램 종료");
            }

            isRunning = false;

            Environment.Exit(0);
        }

        private void SelectImageDetectButton_Click(object sender, RoutedEventArgs e)
        {
            /*
            string sendStr = "IMAGE,";
            Mat mat = null;

            AppDomain.CurrentDomain.Dispatcher.Invoke(() =>
            {
                try
                {
                    BitmapSource bs = new BitmapImage(new Uri(Environment.CurrentDirectory + "\\appImage.bmp", UriKind.RelativeOrAbsolute));
                    mat = BitmapSourceConverter.ToMat(bs);
                    // mat = inspectionInfo.appImage.Clone().ToMat();
                }
                catch (Exception exc)
                {

                }
            });

            int width = mat.Cols;
            int height = mat.Rows;
            int channels = mat.Channels();

            byte[] output = new byte[width * height * channels];
            OpenCvSharp.Cv2.ImEncode(".bmp", mat, out output);


            string rpyStr = command;
            List<byte> buffer = new List<byte>(Encoding.ASCII.GetBytes("RPY_SCAN_IMAGE,"));
            buffer.Add(0x01);
            buffer.AddRange(BitConverter.GetBytes(width));
            buffer.AddRange(BitConverter.GetBytes(height));
            buffer.AddRange(BitConverter.GetBytes(channels));
            buffer.AddRange(output);
            Console.WriteLine("scan Image Replay");

            AsyncFrameSocket.AsyncSocketClient sock = sender as AsyncFrameSocket.AsyncSocketClient;
            sock.Send(buffer.ToArray());
            */

            LogManager.Action("자동 라벨링 시작");

            if (dp.BitmapImage == null)
            {
                LogManager.Action("존재하는 디스플레이 이미지 없음.");
                return;
            }

            if (IsProjectParamsChanged)
            {
                MakecfgFile("Testing");
            }

            m_IsSaved = false;
            SaveBtnVisibility = Visibility.Visible;

            if (Directory.Exists(m_ProjectPath + "\\Temp\\Finded"))
            {
                Directory.Delete(m_ProjectPath + "\\Temp\\Finded", true);
            }

            Directory.CreateDirectory(m_ProjectPath + "\\Temp\\Finded");

            ImageInfo imageInfo = m_SelectedImageInfo;
            int index = lbImageList.SelectedIndex;

            if (ListviewModeVisibility == Visibility.Visible)
            {
                index = dgImageList.SelectedIndex;
            }

            if (PreviewModeVisibility == Visibility.Visible)
            {
                index = lbImageList.SelectedIndex;
            }

            try
            {
                imageInfo = Detect(imageInfo);

                RefreshcharInfo();
            }
            catch (Exception ex)
            {
                LogManager.Error("감지 실패 : " + ex.Message);
            }

            // imageInfo.TrainType = TrainType.LABELED;

            for (int j = 0; j < imageInfo.CharList.Count; j++)
            {
                if (!imageInfo.CharList[j].Is_Finded)
                {
                    imageInfo.CharList.RemoveAt(j);
                }
            }

            for (int j = 0; j < imageInfo.CharList.Count; j++)
            {
                CharInfo charinfo = imageInfo.CharList[j];

                charinfo.Is_Labeled = true;
                charinfo.Labeled_Name = charinfo.Finded_Name;

                if (m_IsFullROI == true)
                {
                    charinfo.Labeled_X = charinfo.Finded_X;
                    charinfo.Labeled_Y = charinfo.Finded_Y;
                    charinfo.Labeled_Width = charinfo.Finded_Width;
                    charinfo.Labeled_Height = charinfo.Finded_Height;
                }
                else
                {
                    charinfo.Labeled_X = (mcurrentROI.Width * charinfo.Finded_X + mcurrentROI.X) / imageInfo.Width;
                    charinfo.Labeled_Y = (mcurrentROI.Height * charinfo.Finded_Y + mcurrentROI.Y) / imageInfo.Height;
                    charinfo.Labeled_Width = (mcurrentROI.Width * charinfo.Finded_Width) / imageInfo.Width;
                    charinfo.Labeled_Height = (mcurrentROI.Height * charinfo.Finded_Height) / imageInfo.Height;
                }

                int w = imageInfo.Width;
                int h = imageInfo.Height;

                if (charinfo.Labeled_X * w - charinfo.Labeled_Width * w / 2 < 0)
                {
                    double label_x = charinfo.Labeled_X * w;
                    double label_width = charinfo.Labeled_Width * w;

                    charinfo.Labeled_X = charinfo.Labeled_X - ((label_x - (label_width / 2)) / w);
                }

                if (charinfo.Labeled_Y * h - charinfo.Labeled_Height * h / 2 < 0)
                {
                    double label_y = charinfo.Labeled_Y * h;
                    double label_height = charinfo.Labeled_Height * h;

                    charinfo.Labeled_Y = charinfo.Labeled_Y - ((label_y - (label_height / 2)) / h);
                }

                if (charinfo.Labeled_X * w + charinfo.Labeled_Width * w / 2 > w)
                {
                    double label_x = charinfo.Labeled_X * w;
                    double label_width = charinfo.Labeled_Width * w;

                    charinfo.Labeled_Width = (label_width - (label_x + label_width / 2 - w) * 2) / w;
                }

                if (charinfo.Labeled_Y * h + charinfo.Labeled_Height * h / 2 > h)
                {
                    double label_y = charinfo.Labeled_Y * h;
                    double label_height = charinfo.Labeled_Height * h;

                    charinfo.Labeled_Height = (label_height - (label_y + label_height / 2 - h) * 2) / h;
                }

                charinfo.type = CharType.Labeled;
                charinfo.Is_Finded = false;
                charinfo.Finded_X = 0;
                charinfo.Finded_Y = 0;
                charinfo.Finded_Width = 0;
                charinfo.Finded_Height = 0;
                charinfo.Finded_Score = 0;
                charinfo.Finded_Name = "";
                charinfo.Is_Labeled = true;

                imageInfo.CharList.RemoveAt(j);
                imageInfo.CharList.Insert(j, charinfo);
            }

            List<CharInfo> removeList = new List<CharInfo>();

            imageInfo.CharList.ForEach(x =>
            {
                if (x.Labeled_X == 0 && x.Labeled_Y == 0 && x.Labeled_Width == 0 && x.Labeled_Height == 0 &&
                    x.Finded_X == 0 && x.Finded_Y == 0 && x.Finded_Width == 0 && x.Finded_Height == 0)
                {
                    removeList.Add(x);
                }
            });

            removeList.ForEach(x =>
            {
                imageInfo.CharList.Remove(x);
            });

            Refresh_ImageList(m_ImageShowType);

            DrawImageLabel();
            Save_NameFile();
            // Save_ImageList_Information(imageInfo);

            Load_LabelList();
            RefreshcharInfo();

            // ImageList_MouseDoubleClick(null, null);

            dp.Focus();

            LogManager.Action("자동 라벨링 종료");
        }

        private void SelectedImageConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            ImageInfo imageInfo = (ImageInfo)lbImageList.SelectedItem;
            int index = lbImageList.SelectedIndex;

            if (PreviewModeVisibility == Visibility.Visible)
            {
                index = lbImageList.SelectedIndex;
            }

            if (ListviewModeVisibility == Visibility.Visible)
            {
                index = dgImageList.SelectedIndex;
            }

            imageInfo.TrainType = TrainType.LABELED;

            for (int j = 0; j < imageInfo.CharList.Count; j++)
            {
                if (!imageInfo.CharList[j].Is_Finded)
                {
                    imageInfo.CharList.RemoveAt(j);
                }
            }

            for (int j = 0; j < imageInfo.CharList.Count; j++)
            {
                CharInfo charinfo = imageInfo.CharList[j];

                charinfo.Is_Labeled = true;
                charinfo.Labeled_Name = charinfo.Finded_Name;

                if (m_IsFullROI == true)
                {
                    charinfo.Labeled_X = charinfo.Finded_X;
                    charinfo.Labeled_Y = charinfo.Finded_Y;
                    charinfo.Labeled_Width = charinfo.Finded_Width;
                    charinfo.Labeled_Height = charinfo.Finded_Height;
                }
                else
                {
                    charinfo.Labeled_X = (mcurrentROI.Width * charinfo.Finded_X + mcurrentROI.X) / imageInfo.Width;
                    charinfo.Labeled_Y = (mcurrentROI.Height * charinfo.Finded_Y + mcurrentROI.Y) / imageInfo.Height;
                    charinfo.Labeled_Width = (mcurrentROI.Width * charinfo.Finded_Width) / imageInfo.Width;
                    charinfo.Labeled_Height = (mcurrentROI.Height * charinfo.Finded_Height) / imageInfo.Height;
                }

                charinfo.type = CharType.Labeled;
                charinfo.Is_Finded = false;
                charinfo.Finded_X = 0;
                charinfo.Finded_Y = 0;
                charinfo.Finded_Width = 0;
                charinfo.Finded_Height = 0;
                charinfo.Finded_Score = 0;
                charinfo.Finded_Name = "";
                charinfo.Is_Labeled = true;

                imageInfo.CharList.RemoveAt(j);
                imageInfo.CharList.Insert(j, charinfo);
            }

            List<CharInfo> removeList = new List<CharInfo>();

            imageInfo.CharList.ForEach(x =>
            {
                if (x.Labeled_X == 0 && x.Labeled_Y == 0 && x.Labeled_Width == 0 && x.Labeled_Height == 0 &&
                    x.Finded_X == 0 && x.Finded_Y == 0 && x.Finded_Width == 0 && x.Finded_Height == 0)
                {
                    removeList.Add(x);
                }
            });

            removeList.ForEach(x =>
            {
                imageInfo.CharList.Remove(x);
            });

            if (m_IsOriginImageInfoList)
            {
                for (int i = 0; i < m_ImageInfoList_Temp.Count; i++)
                {
                    if (m_ImageInfoList_Temp[i].FileName == imageInfo.FileName)
                    {
                        m_ImageInfoList_Temp[i] = imageInfo;
                    }
                }
            }
            else
            {
                for (int i = 0; i < m_ImageInfoList_Temp.Count; i++)
                {
                    if (m_ImageInfoList[i].FileName == imageInfo.FileName)
                    {
                        m_ImageInfoList[i] = imageInfo;
                    }
                }
            }

            PartImageInfoList.RemoveAt(index);
            PartImageInfoList.Insert(index, imageInfo);

            Refresh_ImageList(m_ImageShowType);

            DrawImageLabel();
            Save_NameFile();
            Save_ImageList_Information(imageInfo);

            Load_LabelList();
            RefreshcharInfo();

            lbImageList.SelectedIndex = index;

            ImageList_MouseDoubleClick(null, null);

            dp.Focus();
        }

        public void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double bias = -0.03 * Application.Current.MainWindow.Height + 36.7;

            if (Application.Current.MainWindow.Height >= 1080)
            {
                bias = 4.3;
            }

            // lbImageList.Height = Application.Current.MainWindow.Height / bias;
            // dgImageList.Height = Application.Current.MainWindow.Height / bias;
            // dg.Height = Application.Current.MainWindow.Height / bias;
        }

        public void lbImageList_Loaded(object sender, RoutedEventArgs e)
        {
            if (PreviewModeVisibility == Visibility.Visible)
            {
                Border border = (Border)VisualTreeHelper.GetChild(lbImageList, 0);
                m_ScrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
            }

            double bias = -0.03 * Application.Current.MainWindow.Height + 36.7;

            if (Application.Current.MainWindow.Height >= 1080)
            {
                bias = 4.3;
            }

            // lbImageList.Height = Application.Current.MainWindow.Height / bias;
        }

        public void dg_Loaded(object sender, RoutedEventArgs e)
        {
            double bias = -0.03 * Application.Current.MainWindow.Height + 36.7;

            if (Application.Current.MainWindow.Height >= 1080)
            {
                bias = 4.3;
            }

            // dg.Height = Application.Current.MainWindow.Height / bias;
        }

        public void dgImageList_Loaded(object sender, RoutedEventArgs e)
        {
            double bias = -0.03 * Application.Current.MainWindow.Height + 36.7;

            if (Application.Current.MainWindow.Height >= 1080)
            {
                bias = 4.3;
            }

            // dgImageList.Height = Application.Current.MainWindow.Height / bias;
        }

        private void ImageList_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            double scrollableHeight = 0;

            if (m_ScrollViewer != null)
            {
                scrollableHeight = m_ScrollViewer.ScrollableHeight;
                // m_ScrollViewer.ScrollToVerticalOffset(60.0);

                if (scrollableHeight == e.VerticalOffset)
                {
                    if (m_IsImageListLoadPaused)
                    {
                        m_IsImageListLoadPaused = false;
                    }
                }
            }
        }

        public void WindowSettingCommand_Click(object sender, RoutedEventArgs e)
        {
            SettingWindow window = new SettingWindow();
            window.ShowDialog();
        }

        public void ImageInfoListPage_Click(object sender, RoutedEventArgs e)
        {
            if (m_OldPageTextBlock != null)
            {
                m_OldPageTextBlock.IsEnabled = true;
                m_OldPageTextBlock.FontWeight = FontWeights.Normal;
                m_OldPageTextBlock.FontSize = 16;
            }

            Refresh_ImageList(m_ImageShowType);
            RefreshcharInfo();

            Type _type = sender.GetType();

            if (_type == typeof(Hyperlink))
            {
                Hyperlink _hyper = (Hyperlink)sender;
                InlineUIContainer _iu = (InlineUIContainer)_hyper.Inlines.FirstInline;
                TextBlock _tb = (TextBlock)_iu.Child;
                _tb.FontWeight = FontWeights.UltraBold;
                _tb.IsEnabled = false;
                _tb.FontSize = 16;
                m_OldPageTextBlock = _tb;

                SelectedPage = Convert.ToInt32(_tb.Text);
                int _i  = Convert.ToInt32(_tb.Text) - 1;

                PartImageInfoList.Clear();

                int _start = m_CntPerPage * _i;
                int _end = _start + m_CntPerPage;

                if (m_ImageInfoList.Count < _end)
                {
                    _end = m_ImageInfoList.Count;
                }

                for (int i = _start; i < _end; i++)
                {
                    PartImageInfoList.Add(m_ImageInfoList[i]);
                }
            }

            if (PreviewModeVisibility == Visibility.Visible)
            {
                if (m_ScrollViewer == null)
                {
                    Border border = (Border)VisualTreeHelper.GetChild(lbImageList, 0);
                    m_ScrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
                }

                m_ScrollViewer.ScrollToVerticalOffset(0);
            }
        }

        public void FirstPageBtn_Click(object sender, RoutedEventArgs e)
        {
            if (m_StartPage == 0)
            {
                return;
            }

            Refresh_ImageList(m_ImageShowType);
            RefreshcharInfo();

            m_PageStack.Children.RemoveRange(2, m_PageStack.Children.Count - 4);

            m_StartPage = 0;

            if (m_LimitPage < m_PagePerGroup)
            {
                m_EndPage = m_LimitPage;
            }
            else
            {
                m_EndPage = m_PagePerGroup;
            }

            for (int i = m_EndPage; i > m_StartPage; i--)
            {
                TextBlock m_tb = new TextBlock();
                m_tb.Margin = new Thickness(5);
                m_tb.FontSize = 16;
                m_tb.Width = 20;

                if (i >= 100)
                {
                    m_tb.Width = 30;
                }

                m_tb.VerticalAlignment = VerticalAlignment.Center;
                Hyperlink _hyper = new Hyperlink();
                _hyper.Click += ImageInfoListPage_Click;
                TextBlock _tb = new TextBlock();
                _tb.Text = Convert.ToString(i);

                if (Convert.ToInt32(m_OldPageTextBlock.Text) == i)
                {
                    _tb.FontWeight = FontWeights.UltraBold;
                    _tb.IsEnabled = false;
                    _tb.FontSize = 16;
                }

                if (i == m_StartPage + 1)
                {
                    _tb.FontWeight = FontWeights.UltraBold;
                    _tb.FontSize = 16;
                    _tb.IsEnabled = false;
                    m_OldPageTextBlock = _tb;
                    SelectedPage = i;

                    if (m_ImageInfoList.Count > 0)
                    {
                        PartImageInfoList.Clear();

                        int _start = m_CntPerPage * (i - 1);
                        int _end = _start + m_CntPerPage;

                        for (int j = _start; j < _end; j++)
                        {
                            PartImageInfoList.Add(m_ImageInfoList[j]);
                        }
                    }
                }

                _hyper.Inlines.Add(_tb);
                m_tb.Inlines.Add(_hyper);
                m_PageStack.Children.Insert(2, m_tb);
            }

            if (PreviewModeVisibility == Visibility.Visible)
            {
                if (m_ScrollViewer == null)
                {
                    Border border = (Border)VisualTreeHelper.GetChild(lbImageList, 0);
                    m_ScrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
                }

                m_ScrollViewer.ScrollToVerticalOffset(0);
            }
        }

        public void PrePageBtn_Click(object sender, RoutedEventArgs e)
        {
            Refresh_ImageList(m_ImageShowType);
            RefreshcharInfo();

            if (m_StartPage == 0)
            {
                return;
            }

            m_PageStack.Children.RemoveRange(2, m_PageStack.Children.Count - 4);

            if (m_EndPage == m_LimitPage)
            {
                int _res = m_EndPage % m_PagePerGroup;

                if (_res > 0)
                {
                    m_EndPage -= _res;
                    m_EndPage += m_PagePerGroup;
                }
            }

            m_StartPage -= m_PagePerGroup;
            m_EndPage -= m_PagePerGroup;

            for (int i = m_EndPage; i > m_StartPage; i--)
            {
                TextBlock m_tb = new TextBlock();
                m_tb.Margin = new Thickness(5);
                m_tb.FontSize = 16;
                m_tb.VerticalAlignment = VerticalAlignment.Center;
                m_tb.Width = 20;

                if (i >= 100)
                {
                    m_tb.Width = 30;
                }

                Hyperlink _hyper = new Hyperlink();
                _hyper.Click += ImageInfoListPage_Click;
                TextBlock _tb = new TextBlock();
                _tb.Text = Convert.ToString(i);

                if (Convert.ToInt32(m_OldPageTextBlock.Text) == i)
                {
                    _tb.FontWeight = FontWeights.UltraBold;
                    _tb.IsEnabled = false;
                    _tb.FontSize = 16;
                }

                if (i == m_EndPage)
                {
                    _tb.FontWeight = FontWeights.UltraBold;
                    _tb.FontSize = 16;
                    _tb.IsEnabled = false;
                    m_OldPageTextBlock = _tb;
                    SelectedPage = i;

                    if (m_ImageInfoList.Count > 0)
                    {
                        PartImageInfoList.Clear();

                        int _start = m_CntPerPage * (i - 1);
                        int _end = _start + m_CntPerPage;

                        if (_end > m_ImageInfoList.Count)
                        {
                            _end = m_ImageInfoList.Count;
                        }

                        for (int j = _start; j < _end; j++)
                        {
                            PartImageInfoList.Add(m_ImageInfoList[j]);
                        }
                    }
                }

                _hyper.Inlines.Add(_tb);
                m_tb.Inlines.Add(_hyper);
                m_PageStack.Children.Insert(2, m_tb);
            }

            if (PreviewModeVisibility == Visibility.Visible)
            {
                if (m_ScrollViewer == null)
                {
                    Border border = (Border)VisualTreeHelper.GetChild(lbImageList, 0);
                    m_ScrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
                }

                m_ScrollViewer.ScrollToVerticalOffset(0);
            }
        }

        public void NextPageBtn_Click(object sender, RoutedEventArgs e)
        {
            Refresh_ImageList(m_ImageShowType);
            RefreshcharInfo();

            if (m_EndPage == m_LimitPage)
            {
                return;
            }

            m_PageStack.Children.RemoveRange(2, m_PageStack.Children.Count - 4);

            m_StartPage += m_PagePerGroup;
            m_EndPage += m_PagePerGroup;

            if (m_EndPage > m_LimitPage)
            {
                m_EndPage = m_LimitPage;
            }

            for (int i = m_EndPage; i > m_StartPage; i--)
            {
                TextBlock m_tb = new TextBlock();
                m_tb.Margin = new Thickness(5);
                m_tb.FontSize = 16;
                m_tb.Width = 20;

                if (i >= 100)
                {
                    m_tb.Width = 30;
                }

                m_tb.VerticalAlignment = VerticalAlignment.Center;
                Hyperlink _hyper = new Hyperlink();
                _hyper.Click += ImageInfoListPage_Click;
                TextBlock _tb = new TextBlock();
                _tb.Text = Convert.ToString(i);

                if (Convert.ToInt32(m_OldPageTextBlock.Text) == i)
                {
                    _tb.FontWeight = FontWeights.UltraBold;
                    _tb.IsEnabled = false;
                    _tb.FontSize = 16;
                }

                if (i == m_StartPage + 1)
                {
                    _tb.FontWeight = FontWeights.UltraBold;
                    _tb.FontSize = 16;
                    _tb.IsEnabled = false;
                    m_OldPageTextBlock = _tb;
                    SelectedPage = i;

                    if (m_ImageInfoList.Count > 0)
                    {
                        PartImageInfoList.Clear();

                        int _start = m_CntPerPage * (i - 1);
                        int _end = _start + m_CntPerPage;

                        if (_end > m_ImageInfoList.Count)
                        {
                            _end = m_ImageInfoList.Count;
                        }

                        for (int j = _start; j < _end; j++)
                        {
                            PartImageInfoList.Add(m_ImageInfoList[j]);
                        }
                    }
                }

                _hyper.Inlines.Add(_tb);
                m_tb.Inlines.Add(_hyper);
                m_PageStack.Children.Insert(2, m_tb);
            }

            if (PreviewModeVisibility == Visibility.Visible)
            {
                if (m_ScrollViewer == null)
                {
                    Border border = (Border)VisualTreeHelper.GetChild(lbImageList, 0);
                    m_ScrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
                }

                m_ScrollViewer.ScrollToVerticalOffset(0);
            }
        }

        public void LastPageBtn_Click(object sender, RoutedEventArgs e)
        {
            Refresh_ImageList(m_ImageShowType);
            RefreshcharInfo();

            if (m_EndPage == m_LimitPage)
            {
                return;
            }

            m_PageStack.Children.RemoveRange(2, m_PageStack.Children.Count - 4);

            if (m_LimitPage % m_PagePerGroup == 0)
            {
                m_StartPage = m_LimitPage - m_PagePerGroup;
            }
            else
            {
                m_StartPage = m_LimitPage - (m_LimitPage % m_PagePerGroup);
            }

            m_EndPage = m_LimitPage;

            ObservableCollection<ImageInfo> temp = null;

            if (m_IsOriginImageInfoList)
            {
                temp = m_ImageInfoList_Temp;
            }
            else
            {
                temp = m_ImageInfoList;
            }

            for (int i = m_EndPage; i > m_StartPage; i--)
            {
                TextBlock m_tb = new TextBlock();
                m_tb.Margin = new Thickness(5);
                m_tb.FontSize = 16;
                m_tb.Width = 20;

                if (i >= 100)
                {
                    m_tb.Width = 30;
                }

                m_tb.VerticalAlignment = VerticalAlignment.Center;
                Hyperlink _hyper = new Hyperlink();
                _hyper.Click += ImageInfoListPage_Click;
                TextBlock _tb = new TextBlock();
                _tb.Text = Convert.ToString(i);

                if (m_OldPageTextBlock.Text != "" && Convert.ToInt32(m_OldPageTextBlock.Text) == i)
                {
                    _tb.FontWeight = FontWeights.UltraBold;
                    _tb.IsEnabled = false;
                    _tb.FontSize = 16;
                }

                if (i == m_StartPage + 1)
                {
                    _tb.FontWeight = FontWeights.UltraBold;
                    _tb.FontSize = 16;
                    _tb.IsEnabled = false;
                    m_OldPageTextBlock = _tb;
                    SelectedPage = i;

                    if (temp.Count > 0)
                    {
                        PartImageInfoList.Clear();

                        int _start = m_CntPerPage * (i - 1);
                        int _end = _start + m_CntPerPage;

                        if (_end > temp.Count)
                        {
                            _end = temp.Count;
                        }

                        for (int j = _start; j < _end; j++)
                        {
                            PartImageInfoList.Add(temp[j]);
                        }
                    }
                }

                _hyper.Inlines.Add(_tb);
                m_tb.Inlines.Add(_hyper);
                m_PageStack.Children.Insert(2, m_tb);
            }

            if (PreviewModeVisibility == Visibility.Visible)
            {
                if (m_ScrollViewer == null)
                {
                    Border border = (Border)VisualTreeHelper.GetChild(lbImageList, 0);
                    m_ScrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
                }

                m_ScrollViewer.ScrollToVerticalOffset(0);
            }
        }

        public void PageStack_Loaded(object sender, RoutedEventArgs e)
        {
            m_PageStack = (StackPanel)sender;
        }

        public void dgImageList_DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            if (dgImageList.SelectedItems.Count == 0)
            {
                MessageBox.Show("삭제할 이미지를 선택하세요.");
                return;
            }

            if (dgImageList.SelectedItems.Count > 0)
            {
                dgImageList.SelectedItems.Cast<ImageInfo>().ToList().ForEach(_x =>
                {
                    int m_dgImageList_SelectedIndex = dgImageList.SelectedIndex;

                    dp.BitmapImage = null;
                    ImageInfo selectedItem = _x;

                    List<ImageInfo> list;

                    if (m_IsOriginImageInfoList)
                    {
                        list = m_ImageInfoList_Temp.ToList();
                    }
                    else
                    {
                        list = m_ImageInfoList.ToList();
                    }

                    int index = 0;

                    list.ForEach((x) =>
                    {
                        if (x.FileName == selectedItem.FileName && x.Find_String == selectedItem.Find_String && x.Width == selectedItem.Width && x.Height == selectedItem.Height)
                        {
                            if (m_IsOriginImageInfoList)
                            {
                                m_ImageInfoList_Temp.RemoveAt(index);
                            }
                            else
                            {
                                ImageInfoList.RemoveAt(index);
                            }

                            try
                            {
                                PartImageInfoList.RemoveAt(m_dgImageList_SelectedIndex);
                                File.Delete(x.FileName);
                                string ext = Path.GetExtension(x.FileName);
                                string m_DataName = x.FileName.Substring(0, x.FileName.Length - ext.Length) + ".hsr";
                                File.Delete(m_DataName);
                            }
                            catch (Exception ex)
                            {
                                LogManager.Error(ex.Message);
                            }
                        }

                        index++;
                    });
                });

                NotifyPropertyChanged("ImageInfoList");

                // DrawImageLabel();

                Save_NameFile();
                // Save_ImageList_Information();

                Load_LabelList();
                RefreshcharInfo();
                Refresh_ImageList(m_ImageShowType);

                if (m_IsOriginImageInfoList)
                {
                    m_Total_Img_Cnt = m_ImageInfoList_Temp.Count;
                    Image_Count.Content = m_ImageInfoList.Count + "/" + m_Total_Img_Cnt;
                }
                else
                {
                    m_Total_Img_Cnt = m_ImageInfoList.Count;
                    Image_Count.Content = m_ImageInfoList.Count + "/" + m_Total_Img_Cnt;
                }

                if (PartImageInfoList.Count == 0)
                {
                    // m_PageStack.Children.RemoveAt(m_PageStack.Children.Count - 3);

                    LimitPage--;
                    SelectedPage--;

                    m_PageStack.Children.RemoveRange(2, m_PageStack.Children.Count - 4);

                    m_StartPage = SelectedPage % 10 > 0 ? SelectedPage - (SelectedPage % 10) : SelectedPage - 10;
                    m_EndPage = m_StartPage + 10;

                    if (m_ImageInfoList.Count == 0)
                    {
                        SelectedPage = 0;
                        m_StartPage = 0;
                        m_EndPage = 0;
                        m_LimitPage = 0;
                    }

                    if (m_EndPage > m_LimitPage)
                    {
                        m_EndPage = m_LimitPage;
                    }

                    for (int i = m_EndPage; i > m_StartPage; i--)
                    {
                        TextBlock m_tb = new TextBlock();
                        m_tb.Margin = new Thickness(5);
                        m_tb.FontSize = 16;
                        m_tb.Width = 20;

                        if (i >= 100)
                        {
                            m_tb.Width = 30;
                        }

                        m_tb.VerticalAlignment = VerticalAlignment.Center;
                        Hyperlink _hyper = new Hyperlink();
                        _hyper.Click += ImageInfoListPage_Click;
                        TextBlock _tb = new TextBlock();
                        _tb.Text = Convert.ToString(i);

                        if (Convert.ToInt32(m_OldPageTextBlock.Text) == i)
                        {
                            _tb.FontWeight = FontWeights.UltraBold;
                            _tb.IsEnabled = false;
                            _tb.FontSize = 16;
                            m_OldPageTextBlock = _tb;
                        }

                        if (i == m_EndPage)
                        {
                            _tb.FontWeight = FontWeights.UltraBold;
                            _tb.FontSize = 16;
                            _tb.IsEnabled = false;
                            m_OldPageTextBlock = _tb;
                            SelectedPage = i;

                            if (m_ImageInfoList.Count > 0)
                            {
                                PartImageInfoList.Clear();

                                int _start = m_CntPerPage * (i - 1);
                                int _end = _start + m_CntPerPage;

                                if (_end > m_ImageInfoList.Count)
                                {
                                    _end = m_ImageInfoList.Count;
                                }

                                for (int j = _start; j < _end; j++)
                                {
                                    PartImageInfoList.Add(m_ImageInfoList[j]);
                                }
                            }
                        }

                        _hyper.Inlines.Add(_tb);
                        m_tb.Inlines.Add(_hyper);
                        m_PageStack.Children.Insert(2, m_tb);
                    }
                }
            }
        }

        public void ListBox_DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            if (lbImageList.SelectedItems.Count == 0)
            {
                MessageBox.Show("삭제할 이미지를 선택하세요.");
                return;
            }

            if (lbImageList.SelectedItems.Count > 0)
            {
                lbImageList.SelectedItems.Cast<ImageInfo>().ToList().ForEach(_x =>
                {
                    int m_lbImageList_SelectedIndex = lbImageList.SelectedIndex;

                    dp.BitmapImage = null;
                    ImageInfo selectedItem = _x;

                    List<ImageInfo> list;

                    if (m_IsOriginImageInfoList)
                    {
                        list = m_ImageInfoList_Temp.ToList();
                    }
                    else
                    {
                        list = m_ImageInfoList.ToList();
                    }

                    int index = 0;

                    list.ForEach((x) =>
                    {
                        if (x.FileName == selectedItem.FileName && x.Find_String == selectedItem.Find_String && x.Width == selectedItem.Width && x.Height == selectedItem.Height)
                        {
                            if (m_IsOriginImageInfoList)
                            {
                                m_ImageInfoList_Temp.RemoveAt(index);
                            }
                            else
                            {
                                ImageInfoList.RemoveAt(index);
                            }

                            try
                            {
                                PartImageInfoList.RemoveAt(m_lbImageList_SelectedIndex);
                                File.Delete(x.FileName);
                                string ext = Path.GetExtension(x.FileName);
                                string m_DataName = x.FileName.Substring(0, x.FileName.Length - ext.Length) + ".hsr";
                                File.Delete(m_DataName);
                            }
                            catch (Exception ex)
                            {
                                LogManager.Error(ex.Message);
                            }
                        }

                        index++;
                    });
                });

                NotifyPropertyChanged("ImageInfoList");

                // DrawImageLabel();

                Save_NameFile();
                // Save_ImageList_Information();

                Load_LabelList();
                RefreshcharInfo();
                Refresh_ImageList(m_ImageShowType);

                if (m_IsOriginImageInfoList)
                {
                    m_Total_Img_Cnt = m_ImageInfoList_Temp.Count;
                    Image_Count.Content = m_ImageInfoList.Count + "/" + m_Total_Img_Cnt;
                }
                else
                {
                    m_Total_Img_Cnt = m_ImageInfoList.Count;
                    Image_Count.Content = m_ImageInfoList.Count + "/" + m_Total_Img_Cnt;
                }

                if (PartImageInfoList.Count == 0)
                {
                    // m_PageStack.Children.RemoveAt(m_PageStack.Children.Count - 3);

                    LimitPage--;
                    SelectedPage--;

                    m_PageStack.Children.RemoveRange(2, m_PageStack.Children.Count - 4);

                    m_StartPage = SelectedPage % 10 > 0 ? SelectedPage - (SelectedPage % 10) : SelectedPage - 10;
                    m_EndPage = m_StartPage + 10;

                    if (m_ImageInfoList.Count == 0)
                    {
                        SelectedPage = 0;
                        m_StartPage = 0;
                        m_EndPage = 0;
                        m_LimitPage = 0;
                    }

                    if (m_EndPage > m_LimitPage)
                    {
                        m_EndPage = m_LimitPage;
                    }

                    for (int i = m_EndPage; i > m_StartPage; i--)
                    {
                        TextBlock m_tb = new TextBlock();
                        m_tb.Margin = new Thickness(5);
                        m_tb.FontSize = 16;
                        m_tb.Width = 20;

                        if (i >= 100)
                        {
                            m_tb.Width = 30;
                        }

                        m_tb.VerticalAlignment = VerticalAlignment.Center;
                        Hyperlink _hyper = new Hyperlink();
                        _hyper.Click += ImageInfoListPage_Click;
                        TextBlock _tb = new TextBlock();
                        _tb.Text = Convert.ToString(i);

                        if (Convert.ToInt32(m_OldPageTextBlock.Text) == i)
                        {
                            _tb.FontWeight = FontWeights.UltraBold;
                            _tb.IsEnabled = false;
                            _tb.FontSize = 16;
                            m_OldPageTextBlock = _tb;
                        }

                        if (i == m_EndPage)
                        {
                            _tb.FontWeight = FontWeights.UltraBold;
                            _tb.FontSize = 16;
                            _tb.IsEnabled = false;
                            m_OldPageTextBlock = _tb;
                            SelectedPage = i;

                            if (m_ImageInfoList.Count > 0)
                            {
                                PartImageInfoList.Clear();

                                int _start = m_CntPerPage * (i - 1);
                                int _end = _start + m_CntPerPage;

                                if (_end > m_ImageInfoList.Count)
                                {
                                    _end = m_ImageInfoList.Count;
                                }

                                for (int j = _start; j < _end; j++)
                                {
                                    PartImageInfoList.Add(m_ImageInfoList[j]);
                                }
                            }
                        }

                        _hyper.Inlines.Add(_tb);
                        m_tb.Inlines.Add(_hyper);
                        m_PageStack.Children.Insert(2, m_tb);
                    }
                }
            }
        }

        private ProgressDialogController backupController;

        public async void BackUpBtn_Click(object sender, RoutedEventArgs e)
        {
            CommonSaveFileDialog dialog = new CommonSaveFileDialog();
            dialog.Filters.Add(new CommonFileDialogFilter("압축 파일", "*.zip"));
            dialog.Filters.Add(new CommonFileDialogFilter("모든 파일", "*.*"));
            dialog.DefaultFileName = "Backup.zip";
            
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string zipPath = dialog.FileName;

                backupController = await this.ShowProgressAsync("백업 중...", "백업을 진행하고 있습니다.", true);
                backupController.SetIndeterminate();

                await Task.Run(() =>
                {
                    ZipFile zip = new ZipFile();
                    zip.AddDirectory(m_ProjectPath);
                    zip.SaveProgress += ZipSaveProgress;
                    zip.Save(zipPath);
                    zip.Dispose();
                    zip = null;
                });

                await backupController.CloseAsync();
            }
        }

        private int total = 0;
        private int save = 0;

        private async void ZipSaveProgress(object sender, SaveProgressEventArgs e)
        {
            if (e.EventType == ZipProgressEventType.Saving_BeforeWriteEntry)
            {
                if (total == 0)
                {
                    total = e.EntriesTotal;
                    backupController.Minimum = 0;
                    backupController.Maximum = total;
                }

                save = e.EntriesSaved;

                string msg = save.ToString() + " / " + total.ToString();
                backupController.SetMessage(msg);
                backupController.SetProgress(save);
                await Task.Delay(1);
            }
        }

        ProgressDialogController rollbackController;

        public async void RollbackBtn_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Filters.Add(new CommonFileDialogFilter("압축 파일", "*.zip"));
            dialog.Filters.Add(new CommonFileDialogFilter("모든 파일", "*.*"));

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                rollbackController = await this.ShowProgressAsync("롤백 중...", "롤백을 진행하고 있습니다.", true);
                rollbackController.SetIndeterminate();

                await Task.Run(() =>
                {
                    rollbackController.SetMessage("프로젝트 내용 삭제 중 ...");

                    string[] dirs = Directory.GetDirectories(m_ProjectPath);

                    dirs.ToList().ForEach(y =>
                    {
                        Directory.Delete(y, true);
                    });

                    string[] files = Directory.GetFiles(m_ProjectPath);

                    files.ToList().ForEach(y =>
                    {
                        File.Delete(y);
                    });

                    rollbackController.SetMessage("백업 데이터 압축 해제 중 ...");

                    string zipPath = dialog.FileName;

                    ZipFile zipFile = new ZipFile(zipPath);

                    int total = zipFile.Entries.Count;
                    int count = 0;
                    string msg;
                    rollbackController.Minimum = 0;
                    rollbackController.Maximum = total;

                    zipFile.Entries.ToList().ForEach(x =>
                    {
                        msg = "(" + count.ToString() + " / " + total.ToString() + ")";

                        rollbackController.SetMessage("백업 데이터 압축 해제 중 ..." + msg);
                        rollbackController.SetProgress(count);

                        Task.Delay(1);

                        try
                        {
                            x.Extract(m_ProjectPath, ExtractExistingFileAction.OverwriteSilently);
                        }
                        catch
                        {

                        }

                        count++;
                    });

                    m_IsOriginImageInfoList = false;
                    m_ImageInfoList_Temp.Clear();
                    m_ImageInfoList.Clear();

                    Dispatcher.Invoke(() =>
                    {
                        PartImageInfoList.Clear();
                        dp.BitmapImage = null;
                    });

                    Reload();
                });

                await rollbackController.CloseAsync();
            }
        }

        private void Reload()
        {
            Dispatcher.Invoke(() =>
            {
                m_PageStack.Children.RemoveRange(2, m_PageStack.Children.Count - 4);
            });

            Reload_Project();
        }

        private void Reload_Project()
        {
            Load_ProjectParams();

            new Thread(new ThreadStart(() =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    Load_LabelList();
                });

                // 기존 트레인 데이터 확인
                try
                {
                    MakecfgFile("Training");

                    if (File.Exists(m_ProjectPath + "\\" + m_ProjectName + ".weights"))
                    {
                        m_IsTrained = true;
                    }
                    else
                    {
                        File.Create(m_ProjectPath + "\\" + m_ProjectName + ".weights");
                        m_IsTrained = false;
                    }

                    Load_ImageList_Information();
                }
                catch (Exception ex)
                {
                    LogManager.Error(ex.Message);
                    m_IsTrained = false;
                }
            })).Start();
        }

        public void SelectedPageMoveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPage == 0)
            {
                return;
            }

            m_PageStack.Children.RemoveRange(2, m_PageStack.Children.Count - 4);

            m_StartPage = SelectedPage % 10 > 0 ? SelectedPage - (SelectedPage % 10) : SelectedPage - 10;
            m_EndPage = m_StartPage + 10;

            if (m_EndPage > m_LimitPage)
            {
                m_EndPage = m_LimitPage;
            }

            for (int i = m_EndPage; i > m_StartPage; i--)
            {
                TextBlock m_tb = new TextBlock();
                m_tb.Margin = new Thickness(5);
                m_tb.FontSize = 16;
                m_tb.Width = 20;

                if (i >= 100)
                {
                    m_tb.Width = 30;
                }

                m_tb.VerticalAlignment = VerticalAlignment.Center;
                Hyperlink _hyper = new Hyperlink();
                _hyper.Click += ImageInfoListPage_Click;
                TextBlock _tb = new TextBlock();
                _tb.Text = Convert.ToString(i);

                if (Convert.ToInt32(m_OldPageTextBlock.Text) == i)
                {
                    _tb.FontWeight = FontWeights.UltraBold;
                    _tb.IsEnabled = false;
                    _tb.FontSize = 16;
                    m_OldPageTextBlock = _tb;
                }

                if (i == SelectedPage)
                {
                    _tb.FontWeight = FontWeights.UltraBold;
                    _tb.FontSize = 16;
                    _tb.IsEnabled = false;
                    m_OldPageTextBlock = _tb;
                    SelectedPage = i;

                    if (m_ImageInfoList.Count > 0)
                    {
                        PartImageInfoList.Clear();

                        int _start = m_CntPerPage * (i - 1);
                        int _end = _start + m_CntPerPage;

                        if (_end > m_ImageInfoList.Count)
                        {
                            _end = m_ImageInfoList.Count;
                        }

                        for (int j = _start; j < _end; j++)
                        {
                            PartImageInfoList.Add(m_ImageInfoList[j]);
                        }
                    }
                }

                _hyper.Inlines.Add(_tb);
                m_tb.Inlines.Add(_hyper);
                m_PageStack.Children.Insert(2, m_tb);
            }
        }

        private ObservableCollection<StructDetectResult> m_AllImageDetectResults = new ObservableCollection<StructDetectResult>();
        private List<ImageInfo> m_ImageList;
        private int m_DetectCount = 0;
        private int m_MatchCount = 0;
        private bool m_IsAllImageDetecting = false;

        public void AllImageDetectButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_IsAllImageDetecting)
            {
                return;
            }

            LogManager.Action("전체 검증 시작");

            m_IsAllImageDetecting = true;
            IsOpenAllImageDetectResultEnabled = false;
            AllImageDetectLoading = Visibility.Visible;
            AllImageDetectText = Visibility.Hidden;
            m_AllImageDetectResults.Clear();
            double m_PassRange = config.GetDouble("Detect", "PassRange", 15);

            if (m_IsOriginImageInfoList)
            {
                m_ImageList = m_ImageInfoList_Temp.ToList();
            }
            else
            {
                m_ImageList = m_ImageInfoList.ToList();
            }

            m_DetectCount = 0;
            m_MatchCount = 0;
            int count = 0;
            bool m_IsError = false;
            int m_ErrorCount = 0;

            new Thread(new ThreadStart(() =>
            {
                try
                {
                    m_ImageList.ForEach(x =>
                    {
                        if (m_IsError)
                        {
                            return;
                        }

                        List<YoloItem> items = DetectEngine(x.FileName);

                        /*
                        if (items.Count == 0)
                        {
                            m_ChannelsError = true;

                            if (m_Process != null)
                            {
                                m_Process.Kill();
                                m_Process.Close();
                                m_Process.Dispose();
                                m_Process = null;

                                LogManager.Action("인식 엔진 프로그램 종료");
                            }

                            LogManager.Action("채널 수 오류로 인한 재인식 실행");
                            items = DetectEngine(x.FileName);
                            LogManager.Action("채널 수 오류로 인한 재인식 종료");

                            m_ErrorCount++;

                            if (m_ErrorCount > 5)
                            {
                                m_IsError = true;
                            }
                        }
                        */

                        m_DetectCount += items.Count;
                        int _detect_count = items.Count;
                        int _match_count = 0;
                        int total_label_count = 0;
                        int hole_label_count = 0;
                        int blank_label_count = 0;
                        int plug_label_count = 0;

                        Dispatcher.Invoke(() =>
                        {
                            total_label_count = x.CharList.Count;

                            x.CharList.ForEach(y =>
                            {
                                if (y.Labeled_Name.Contains("Blank"))
                                {
                                    blank_label_count++;
                                }
                                else if (y.Labeled_Name == "Plug")
                                {
                                    plug_label_count++;
                                }
                                else if (y.Labeled_Name.Contains("Hole"))
                                {
                                    hole_label_count++;
                                }
                            });

                            items.ForEach(y =>
                            {
                                x.CharList.ForEach(z =>
                                {
                                    double loss_x = Math.Abs(y.X - ((z.Labeled_X - (z.Labeled_Width / 2)) * x.Width));
                                    double loss_y = Math.Abs(y.Y - ((z.Labeled_Y - (z.Labeled_Height / 2)) * x.Height));
                                    double loss_width = Math.Abs(y.Width - (z.Labeled_Width * x.Width));
                                    double loss_height = Math.Abs(y.Height - (z.Labeled_Height * x.Height));

                                    if (loss_x <= m_PassRange && loss_y <= m_PassRange && loss_width <= m_PassRange && loss_height <= m_PassRange)
                                    {
                                        _match_count++;
                                    }

                                    m_MatchCount += _match_count;

                                    string msg = "파일명 : " + x.FileName + Environment.NewLine + "인식 수 : " + _detect_count + Environment.NewLine + "라벨, 인식 매칭 수 : " + _match_count;
                                });
                            });
                        });

                        if (!Directory.Exists(m_ProjectPath + "\\Test"))
                        {
                            Directory.CreateDirectory(m_ProjectPath + "\\Test");
                        }

                        StructDetectResult result = new StructDetectResult()
                        {
                            FileName = x.FileName,
                            TotalLabelCount = total_label_count,
                            HoleLabelCount = hole_label_count,
                            BlankLabelCount = blank_label_count,
                            PlugLabelCount = plug_label_count,
                            DetectCount = _detect_count,
                            MatchCount = _match_count,
                            Items = items
                        };

                        if (x.TrainType == TrainType.LABELED && _detect_count != _match_count)
                        {
                            result.ErrorString += "라벨링 미스매치, ";
                            result.IsMatchError = true;
                        }

                        Dispatcher.Invoke(() =>
                        {
                            m_AllImageDetectResults.Add(result);
                        });

                        count++;
                    });

                    m_IsAllImageDetecting = false;
                    AllImageDetectLoading = Visibility.Hidden;
                    AllImageDetectText = Visibility.Visible;
                    IsOpenAllImageDetectResultEnabled = true;

                    for (int i = 0; i < m_AllImageDetectResults.Count; i++)
                    {
                        int m_PlugCount = 0;
                        int m_BlankCount = 0;
                        int m_HoleCount = 0;
                        double m_BlankStartX = 0;
                        double m_BlankEndX = 0;
                        double m_BlankStartY = 0;
                        double m_BlankEndY = 0;
                        bool m_IsFindBlank = false;
                        bool m_IsFindPlug = false;

                        m_AllImageDetectResults[i].Items.ForEach(x =>
                        {
                            if (x.Type == "Blank")
                            {
                                if (!m_IsFindBlank)
                                {
                                    m_BlankStartX = x.X;
                                    m_BlankEndX = x.X + x.Width;
                                    m_BlankStartY = x.Y;
                                    m_BlankEndY = x.Y + x.Height;
                                }

                                m_IsFindBlank = true;

                                m_BlankCount++;
                            }

                            if (x.Type == "Plug")
                            {
                                /*
                                if (!m_IsFindPlug)
                                {
                                    m_BlankStartX = x.X;
                                    m_BlankEndX = x.X + x.Width;
                                    m_BlankStartY = x.Y;
                                    m_BlankEndY = x.Y + x.Height;
                                }
                                */

                                m_IsFindPlug = true;

                                m_BlankCount++;
                            }

                            if (x.Type == "Hole")
                            {
                                /*
                                if (!m_AllImageDetectResults[i].IsLabelError)
                                {
                                    if (m_BlankStartX <= x.X || m_BlankEndX >= x.X + x.Width || m_BlankStartY <= x.Y || m_BlankEndY >= x.Y + x.Height)
                                    {

                                    }
                                    else
                                    {
                                        m_AllImageDetectResults[i].IsLabelError = true;
                                        m_AllImageDetectResults[i].ErrorString += "블랭크 영역에서 벗어난 홀 존재, ";
                                    }
                                }
                                */

                                m_HoleCount++;
                            }
                        });

                        m_AllImageDetectResults[i].HoleCount = m_HoleCount;
                        m_AllImageDetectResults[i].BlankCount = m_BlankCount;
                        m_AllImageDetectResults[i].PlugCount = m_PlugCount;

                        if (m_PlugCount == 0 || m_PlugCount > 1)
                        {
                            m_AllImageDetectResults[i].IsLabelError = true;
                            m_AllImageDetectResults[i].ErrorString += "플러그 인식 개수 " + m_PlugCount + ", ";
                        }

                        if (m_HoleCount == 0)
                        {
                            m_AllImageDetectResults[i].IsLabelError = true;
                            m_AllImageDetectResults[i].ErrorString += "홀 인식 개수 " + m_HoleCount + ", ";
                        }

                        if (m_AllImageDetectResults[i].ErrorString != null)
                        {
                            if (m_AllImageDetectResults[i].ErrorString.EndsWith(", "))
                            {
                                string str = m_AllImageDetectResults[i].ErrorString.Substring(0, m_AllImageDetectResults[i].ErrorString.Length - (", ").Length);
                                m_AllImageDetectResults[i].ErrorString = str;
                            }
                        }
                    }

                    LogManager.Action("전체 검증 종료");
                }
                catch (Exception ex)
                {
                    LogManager.Error(ex.Message);
                    m_IsAllImageDetecting = false;
                    IsOpenAllImageDetectResultEnabled = true;
                    AllImageDetectLoading = Visibility.Hidden;
                    AllImageDetectText = Visibility.Visible;
                }
            })).Start();
        }

        public void OpenAllImageDetectResultButton_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<ImageInfo> m_List;

            if (!m_IsOriginImageInfoList)
            {
                m_List = m_ImageInfoList;
            }
            else
            {
                m_List = m_ImageInfoList_Temp;
            }

            AllDetectResultWindow window = new AllDetectResultWindow(m_List.Count, m_DetectCount, m_MatchCount, m_AllImageDetectResults, m_List);

            Dispatcher.Invoke(() =>
            {
                window.ShowDialog();
                window.Close();
                StructDetectResult m_SelectedDetectResult =  window.m_SelectDetectResult;

                if (m_SelectedDetectResult != null)
                {
                    for (int _i = 0; _i < ImageInfoList.Count; _i++)
                    {
                        if (ImageInfoList[_i].FileName == m_SelectedDetectResult.FileName)
                        {
                            SelectedPage = (_i + 1) / m_CntPerPage;

                            if ((_i + 1) % m_CntPerPage != 0)
                            {
                                SelectedPage++;
                            }

                            m_PageStack.Children.RemoveRange(2, m_PageStack.Children.Count - 4);

                            m_StartPage = SelectedPage % 10 > 0 ? SelectedPage - (SelectedPage % 10) : SelectedPage - 10;
                            m_EndPage = m_StartPage + 10;

                            if (m_EndPage > m_LimitPage)
                            {
                                m_EndPage = m_LimitPage;
                            }

                            for (int i = m_EndPage; i > m_StartPage; i--)
                            {
                                TextBlock m_tb = new TextBlock();
                                m_tb.Margin = new Thickness(5);
                                m_tb.FontSize = 16;
                                m_tb.Width = 20;

                                if (i >= 100)
                                {
                                    m_tb.Width = 30;
                                }

                                m_tb.VerticalAlignment = VerticalAlignment.Center;
                                Hyperlink _hyper = new Hyperlink();
                                _hyper.Click += ImageInfoListPage_Click;
                                TextBlock _tb = new TextBlock();
                                _tb.Text = Convert.ToString(i);

                                if (Convert.ToInt32(m_OldPageTextBlock.Text) == i)
                                {
                                    _tb.FontWeight = FontWeights.UltraBold;
                                    _tb.IsEnabled = false;
                                    _tb.FontSize = 16;
                                    m_OldPageTextBlock = _tb;
                                }

                                if (i == SelectedPage)
                                {
                                    _tb.FontWeight = FontWeights.UltraBold;
                                    _tb.FontSize = 16;
                                    _tb.IsEnabled = false;
                                    m_OldPageTextBlock = _tb;
                                    SelectedPage = i;

                                    if (m_ImageInfoList.Count > 0)
                                    {
                                        PartImageInfoList.Clear();

                                        int _start = m_CntPerPage * (i - 1);
                                        int _end = _start + m_CntPerPage;

                                        if (_end > m_ImageInfoList.Count)
                                        {
                                            _end = m_ImageInfoList.Count;
                                        }

                                        for (int j = _start; j < _end; j++)
                                        {
                                            PartImageInfoList.Add(m_ImageInfoList[j]);
                                        }
                                    }
                                }

                                _hyper.Inlines.Add(_tb);
                                m_tb.Inlines.Add(_hyper);
                                m_PageStack.Children.Insert(2, m_tb);

                                for (int j = 0; j < PartImageInfoList.Count; j++)
                                {
                                    if (PartImageInfoList[j].FileName == m_SelectedDetectResult.FileName)
                                    {
                                        lbImageList.SelectedIndex = j;

                                        break;
                                    }
                                }
                            }

                            break;
                        }
                    }
                }

                window = null;
            });
        }

        public ICommand LeftRotation90_Command
        {
            get { return (this.leftRotation90) ?? (this.leftRotation90 = new DelegateCommand(Left_Rotation_90)); }
        }
        private ICommand leftRotation90;

        private unsafe void Left_Rotation_90()
        {
            int index = lbImageList.SelectedIndex;

            if (PreviewModeVisibility == Visibility.Visible)
            {
                index = lbImageList.SelectedIndex;
            }

            if (ListviewModeVisibility == Visibility.Visible)
            {
                index = dgImageList.SelectedIndex;
            }

            ImageInfo imageInfo = PartImageInfoList[index];
            Mat mat = Cv2.ImRead(imageInfo.FileName, ImreadModes.Unchanged);
            int width = mat.Width;
            int height = mat.Height;
            Mat rotationImage = new Mat(width, height, mat.Type());

            for (int i = 0; i < height; i++)
            {
                if (mat.Channels() == 1)
                {
                    byte* ptr1 = (byte*)mat.Ptr(i).ToPointer();

                    for (int j = 0; j < width; j++)
                    {
                        byte* ptr2 = (byte*)rotationImage.Ptr(width - 1 - j).ToPointer();
                        ptr2[i] = ptr1[j];
                    }
                }
                else if (mat.Channels() == 3)
                {
                    Vec3b* ptr1 = (Vec3b*)mat.Ptr(i).ToPointer();

                    for (int j = 0; j < width; j++)
                    {
                        Vec3b* ptr2 = (Vec3b*)rotationImage.Ptr(width - 1 - j).ToPointer();
                        ptr2[i] = ptr1[j];
                    }
                }
            }

            rotationImage.SaveImage(imageInfo.FileName);
            mat.Dispose();
            rotationImage.Dispose();
            mat = null;
            rotationImage = null;

            if (imageInfo.CharList.Count > 0)
            {
                for (int i = 0; i < imageInfo.CharList.Count; i++)
                {
                    CharInfo x = imageInfo.CharList[i];

                    imageInfo.CharList[i] = new CharInfo()
                    {
                        type = x.type,
                        Label_FileName = x.Label_FileName,
                        Labeled_Name = x.Labeled_Name,
                        Labeled_X = x.Labeled_Y,
                        Labeled_Y = 1 - x.Labeled_X,
                        Labeled_Width = x.Labeled_Height,
                        Labeled_Height = x.Labeled_Width,
                        Is_Labeled = x.Is_Labeled,
                        Finded_Name = x.Finded_Name,
                        Finded_X = x.Finded_Y,
                        Finded_Y = 1 - x.Finded_X,
                        Finded_Width = x.Finded_Height,
                        Finded_Height = x.Finded_Width,
                        Finded_Score = x.Finded_Score,
                        Is_Finded = x.Is_Finded
                    };
                }
            }

            if (m_IsOriginImageInfoList)
            {
                for (int i = 0; i < m_ImageInfoList_Temp.Count; i++)
                {
                    if (m_ImageInfoList_Temp[i].FileName == imageInfo.FileName)
                    {
                        m_ImageInfoList_Temp.RemoveAt(i);
                        m_ImageInfoList_Temp.Insert(i, imageInfo);
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < m_ImageInfoList.Count; i++)
                {
                    if (m_ImageInfoList[i].FileName == imageInfo.FileName)
                    {
                        m_ImageInfoList.RemoveAt(i);
                        m_ImageInfoList.Insert(i, imageInfo);
                        break;
                    }
                }
            }

            PartImageInfoList.RemoveAt(index);
            PartImageInfoList.Insert(index, imageInfo);
            NotifyPropertyChanged("PartImageInfoList");

            lbImageList.SelectedIndex = index;

            Refresh_ImageList(m_ImageShowType);
            DrawImageLabel();
            RefreshcharInfo();
            Save_ImageList_Information(imageInfo);

            ImageList_MouseDoubleClick(null, null);
        }

        public ICommand RightRotation90_Command
        {
            get { return (this.rightRotation90) ?? (this.rightRotation90 = new DelegateCommand(Right_Rotation_90)); }
        }
        private ICommand rightRotation90;

        private unsafe void Right_Rotation_90()
        {
            int index = lbImageList.SelectedIndex;

            if (PreviewModeVisibility == Visibility.Visible)
            {
                index = lbImageList.SelectedIndex;
            }

            if (ListviewModeVisibility == Visibility.Visible)
            {
                index = dgImageList.SelectedIndex;
            }

            ImageInfo imageInfo = PartImageInfoList[index];
            Mat mat = Cv2.ImRead(imageInfo.FileName, ImreadModes.Unchanged);
            int width = mat.Width;
            int height = mat.Height;
            Mat rotationImage = new Mat(width, height, mat.Type());
            
            for (int i = 0; i < height; i++)
            {
                if (mat.Channels() == 1)
                {
                    byte* ptr1 = (byte*)mat.Ptr(i).ToPointer();

                    for (int j = 0; j < width; j++)
                    {
                        byte* ptr2 = (byte*)rotationImage.Ptr(j).ToPointer();
                        ptr2[height - 1 - i] = ptr1[j];
                    }
                }
                else if (mat.Channels() == 3)
                {
                    Vec3b* ptr1 = (Vec3b*)mat.Ptr(i).ToPointer();

                    for (int j = 0; j < width; j++)
                    {
                        Vec3b* ptr2 = (Vec3b*)rotationImage.Ptr(j).ToPointer();
                        ptr2[height - 1 - i] = ptr1[j];
                    }
                }
            }

            rotationImage.SaveImage(imageInfo.FileName);
            mat.Dispose();
            rotationImage.Dispose();
            mat = null;
            rotationImage = null;

            if (imageInfo.CharList.Count > 0)
            {
                for (int i = 0; i < imageInfo.CharList.Count; i++)
                {
                    CharInfo x = imageInfo.CharList[i];

                    imageInfo.CharList[i] = new CharInfo()
                    {
                        type = x.type,
                        Label_FileName = x.Label_FileName,
                        Labeled_Name = x.Labeled_Name,
                        Labeled_X = 1 - x.Labeled_Y,
                        Labeled_Y = x.Labeled_X,
                        Labeled_Width = x.Labeled_Height,
                        Labeled_Height = x.Labeled_Width,
                        Is_Labeled = x.Is_Labeled,
                        Finded_Name = x.Finded_Name,
                        Finded_X = 1 - x.Finded_Y,
                        Finded_Y = x.Finded_X,
                        Finded_Width = x.Finded_Height,
                        Finded_Height = x.Finded_Width,
                        Finded_Score = x.Finded_Score,
                        Is_Finded = x.Is_Finded
                    };
                }
            }

            if (m_IsOriginImageInfoList)
            {
                for (int i = 0; i < m_ImageInfoList_Temp.Count; i++)
                {
                    if (m_ImageInfoList_Temp[i].FileName == imageInfo.FileName)
                    {
                        m_ImageInfoList_Temp.RemoveAt(i);
                        m_ImageInfoList_Temp.Insert(i, imageInfo);
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < m_ImageInfoList.Count; i++)
                {
                    if (m_ImageInfoList[i].FileName == imageInfo.FileName)
                    {
                        m_ImageInfoList.RemoveAt(i);
                        m_ImageInfoList.Insert(i, imageInfo);
                        break;
                    }
                }
            }

            PartImageInfoList.RemoveAt(index);
            PartImageInfoList.Insert(index, imageInfo);
            NotifyPropertyChanged("PartImageInfoList");

            lbImageList.SelectedIndex = index;

            Refresh_ImageList(m_ImageShowType);
            DrawImageLabel();
            RefreshcharInfo();
            Save_ImageList_Information(imageInfo);

            ImageList_MouseDoubleClick(null, null);
        }

        public void lbImageList_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            m_lbImageList_MouseLeftButtonDown = true;
        }

        private StackPanel m_FuncStackPanel;

        public void FuncStackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            m_FuncStackPanel = (StackPanel)sender;
        }

        private void FuncStackPanel_Init()
        {
            int cnt = 0;

            m_LabelList.ToList().ForEach(_x =>
            {
                Button _btn = new Button();
                _btn.Content = _x.LabelName + " 라벨 추가";
                _btn.ToolTip = _x.LabelName + " 라벨 추가";
                _btn.Height = 38;
                _btn.Margin = new Thickness(5);
                _btn.Click += delegate
                {
                    if (m_IsCtrlPressed && m_IsLeftMouseBtnPressed && dp.Lines != null && dp.SelectRectangle == null)
                    {
                        int minX = int.MaxValue;
                        int minY = int.MaxValue;
                        int maxX = int.MinValue;
                        int maxY = int.MinValue;

                        dp.Lines.ToList().ForEach(x =>
                        {
                            if (minX > x.Point1.X)
                            {
                                minX = x.Point1.X;
                            }
                            if (minX > x.Point2.X)
                            {
                                minX = x.Point2.X;
                            }
                            if (minY > x.Point1.Y)
                            {
                                minY = x.Point1.Y;
                            }
                            if (minY > x.Point2.Y)
                            {
                                minY = x.Point2.Y;
                            }
                            if (maxX < x.Point1.X)
                            {
                                maxX = x.Point1.X;
                            }
                            if (maxX < x.Point2.X)
                            {
                                maxX = x.Point2.X;
                            }
                            if (maxY < x.Point1.Y)
                            {
                                maxY = x.Point1.Y;
                            }
                            if (maxY < x.Point2.Y)
                            {
                                maxY = x.Point2.Y;
                            }
                        });

                        int count = 0;
                        List<CharInfo> _infos = new List<CharInfo>();

                        m_SelectedImageInfo.CharList.ForEach(x =>
                        {
                            if (minX <= (dp.BitmapImage.Width * x.Labeled_X) - (x.Labeled_Width * dp.BitmapImage.Width / 2) && maxX >= (dp.BitmapImage.Width * x.Labeled_X) - (x.Labeled_Width * dp.BitmapImage.Width / 2) && minY <= (dp.BitmapImage.Height * x.Labeled_Y) - (x.Labeled_Height * dp.BitmapImage.Height / 2) && maxY >= (dp.BitmapImage.Height * x.Labeled_Y) - (x.Labeled_Height * dp.BitmapImage.Height / 2))
                            {
                                m_SelectedCharIndex = count;
                                CharInfo c = m_SelectedImageInfo.CharList[m_SelectedCharIndex];
                                _infos.Add(c);
                            }

                            count++;
                        });

                        _infos.ForEach(x =>
                        {
                            m_SelectedImageInfo.CharList.Remove(x);
                            dp.ShowRectangle((int)(dp.BitmapImage.Width * x.Labeled_X - (x.Labeled_Width * dp.BitmapImage.Width) / 2), (int)(dp.BitmapImage.Height * x.Labeled_Y - (dp.BitmapImage.Height * x.Labeled_Height) / 2), (int)(dp.BitmapImage.Width * x.Labeled_Width), (int)(dp.BitmapImage.Height * x.Labeled_Height), x.Labeled_Name, Brushes.Red);
                        });

                        dp.Lines.Clear();
                    }

                    if (dp.BitmapImage == null)
                    {
                        return;
                    }

                    if (!dp.IsShowRectangle && !m_IsMouseDragged)
                    {
                        dp.ConfirmButtonVisibility = Visibility.Visible;

                        bool IsSelected = false;
                        int Count = 0;

                        // 현재 마우스 위치에서 사각형 크기 계산
                        int CurrentX = (int)dp.canvas.RealPoint.X;
                        int CurrentY = (int)dp.canvas.RealPoint.Y;

                        #region 기존 영역에 클릭 했는지 확인...
                        m_SelectedImageInfo.CharList.ForEach(x =>
                        {
                            if (m_IsFullROI)
                            {
                                if (x.Is_Labeled)
                                {
                                    if (CurrentX >= (dp.BitmapImage.Width * x.Labeled_X) - (x.Labeled_Width * dp.BitmapImage.Width / 2) && CurrentX <= (dp.BitmapImage.Width * x.Labeled_X) + (x.Labeled_Width * dp.BitmapImage.Width / 2) && CurrentY >= (dp.BitmapImage.Height * x.Labeled_Y) - (x.Labeled_Height * dp.BitmapImage.Height / 2) && CurrentY <= (dp.BitmapImage.Height * x.Labeled_Y) + (x.Labeled_Height * dp.BitmapImage.Height / 2))
                                    {
                                        if (IsSelected)
                                        {
                                            if (x.Labeled_Width < m_SelectedImageInfo.CharList[m_SelectedCharIndex].Labeled_Width || x.Labeled_Height < m_SelectedImageInfo.CharList[m_SelectedCharIndex].Labeled_Height)
                                            {
                                                m_SelectedCharIndex = Count;
                                                IsSelected = true;
                                            }
                                        }
                                        else
                                        {
                                            m_SelectedCharIndex = Count;
                                            IsSelected = true;
                                        }
                                    }
                                }
                                else if (x.Is_Finded)
                                {
                                    if (CurrentX > (dp.BitmapImage.Width * x.Finded_X) - (dp.BitmapImage.Width * x.Finded_Width / 2) && CurrentX < (dp.BitmapImage.Width * x.Finded_X) + (dp.BitmapImage.Width * x.Finded_Width / 2) && CurrentY > (dp.BitmapImage.Height * x.Finded_Y) - (dp.BitmapImage.Height * x.Finded_Height / 2) && CurrentY < (dp.BitmapImage.Height * x.Finded_Y) + (dp.BitmapImage.Height * x.Finded_Height / 2))
                                    {
                                        if (IsSelected)
                                        {
                                            if (x.Finded_Width < m_SelectedImageInfo.CharList[m_SelectedCharIndex].Finded_Width || x.Finded_Height < m_SelectedImageInfo.CharList[m_SelectedCharIndex].Finded_Height)
                                            {
                                                m_SelectedCharIndex = Count;
                                                IsSelected = true;
                                            }
                                        }
                                        else
                                        {
                                            m_SelectedCharIndex = Count;
                                            IsSelected = true;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (x.Is_Labeled)
                                {
                                    if ((CurrentX > dp.BitmapImage.Width * x.Labeled_X - dp.BitmapImage.Width * x.Labeled_Width / 2)
                                          && (CurrentX < dp.BitmapImage.Width * x.Labeled_X + dp.BitmapImage.Width * x.Labeled_Width / 2)
                                          && (CurrentY > dp.BitmapImage.Height * x.Labeled_Y - dp.BitmapImage.Height * x.Labeled_Height / 2)
                                          && (CurrentY < dp.BitmapImage.Height * x.Labeled_Y + dp.BitmapImage.Height * x.Labeled_Height / 2))
                                    {
                                        m_SelectedCharIndex = Count;
                                        IsSelected = true;
                                    }
                                }
                                else if (x.Is_Finded)
                                {
                                    if ((CurrentX > mcurrentROI.Width * x.Finded_X - mcurrentROI.Width * x.Finded_Width + mcurrentROI.X / 2)
                                        && (CurrentX < mcurrentROI.Width * x.Finded_X + mcurrentROI.Width * x.Finded_Width + mcurrentROI.X / 2)
                                        && (CurrentY > mcurrentROI.Height * x.Finded_Y - mcurrentROI.Height * x.Finded_Height + mcurrentROI.Y / 2)
                                        && (CurrentY < mcurrentROI.Height * x.Finded_Y + mcurrentROI.Height * x.Finded_Height + mcurrentROI.Y / 2))
                                    {
                                        m_SelectedCharIndex = Count;
                                        IsSelected = true;
                                    }
                                }
                            }

                            Count++;
                        });
                        #endregion

                        if (IsSelected == false)
                        {
                            //사각형 기본 사이즈
                            dp.ShowRectangle((int)(dp.BitmapImage.Width / 2) - (m_DefaultWidth / 2), (int)(dp.BitmapImage.Height / 2) - (m_DefaultHeight / 2), (int)(m_DefaultWidth), (int)(m_DefaultHeight), "?", Brushes.Red);
                            dp.SelectRectangle.LabeledCharName = _x.LabelName;
                            IsSelected = true;
                        }
                        else
                        {
                            // 선택한 영역 임시 저장
                            m_TempCharInfo = m_SelectedImageInfo.CharList[m_SelectedCharIndex];
                            m_IsTempCharInfo = true;

                            // 선택한 영역 리스트에서 삭제
                            m_SelectedImageInfo.CharList.RemoveAt(m_SelectedCharIndex);

                            if (m_TempCharInfo.Is_Labeled == true)
                            {
                                // 편집 가능한 사각형 표시
                                // dp.ShowRectangle(dp.BitmapImage.Width * m_TempCharInfo.Labeled_X - (m_TempCharInfo.Labeled_Width * dp.BitmapImage.Width) / 2, dp.BitmapImage.Height * m_TempCharInfo.Labeled_Y - (dp.BitmapImage.Height * m_TempCharInfo.Labeled_Height) / 2, dp.BitmapImage.Width * m_TempCharInfo.Labeled_Width, dp.BitmapImage.Height * m_TempCharInfo.Labeled_Height, m_TempCharInfo.Labeled_Name, Brushes.Red);
                                // x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width) / 2, x.Labeled_Y * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height) / 2, x.Labeled_Width * dp.BitmapImage.Width, x.Labeled_Height * dp.BitmapImage.Height
                                dp.ShowRectangle(m_TempCharInfo.Labeled_X * dp.BitmapImage.Width - (m_TempCharInfo.Labeled_Width * dp.BitmapImage.Width) / 2, m_TempCharInfo.Labeled_Y * dp.BitmapImage.Height - (m_TempCharInfo.Labeled_Height * dp.BitmapImage.Height) / 2, m_TempCharInfo.Labeled_Width * dp.BitmapImage.Width, m_TempCharInfo.Labeled_Height * dp.BitmapImage.Height, m_TempCharInfo.Labeled_Name, Brushes.Red);
                            }
                            else
                            {
                                if (m_IsFullROI == true)
                                {
                                    dp.ShowRectangle((int)(dp.BitmapImage.Width * m_TempCharInfo.Finded_X - (m_TempCharInfo.Finded_Width * dp.BitmapImage.Width) / 2), (int)(dp.BitmapImage.Height * m_TempCharInfo.Finded_Y - (dp.BitmapImage.Height * m_TempCharInfo.Finded_Height) / 2), (int)(dp.BitmapImage.Width * m_TempCharInfo.Finded_Width), (int)(dp.BitmapImage.Height * m_TempCharInfo.Finded_Height), m_TempCharInfo.Finded_Name, Brushes.Red);
                                }
                                else
                                {
                                    dp.ShowRectangle((int)(mcurrentROI.Width * m_TempCharInfo.Finded_X - (m_TempCharInfo.Finded_Width * mcurrentROI.Width) / 2) + mcurrentROI.X
                                                   , (int)(mcurrentROI.Height * m_TempCharInfo.Finded_Y - (m_TempCharInfo.Finded_Height * mcurrentROI.Height) / 2) + mcurrentROI.Y
                                                   , (int)(mcurrentROI.Width * m_TempCharInfo.Finded_Width)
                                                   , (int)(mcurrentROI.Height * m_TempCharInfo.Finded_Height)
                                                   , m_TempCharInfo.Finded_Name, Brushes.Red);
                                }
                            }

                            // 사각형 영역 갱신
                            DrawImageLabel();
                        }

                        dp.SelectRectangle.KeyUp += Labelchanged;
                    }

                    m_IsLeftMouseBtnPressed = false;
                    m_IsMouseDragged = false;
                };

                m_FuncStackPanel.Children.Insert(cnt, _btn);
                cnt++;
            });
        }

        public void AddImageList_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Filters.Add(new CommonFileDialogFilter("이미지 파일", "*.jpg;*.jpeg;*.png;*.bmp"));
            dialog.Filters.Add(new CommonFileDialogFilter("모든 파일", "*.*"));
            dialog.Multiselect = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                #region .cfg File 생성
                // MakecfgFile("Training");
                #endregion

                //CompareCfg(m_ProjectPath + "\\" + m_ProjectName + "_Source.cfg", m_ProjectPath + "\\" + m_ProjectName + ".cfg");

                #region .Data File 생성
                // Train Data 구조체 필요...

                // 기존 데이터 파일 삭제
                if (File.Exists(m_ProjectPath + "\\" + m_ProjectName + ".data") == true)
                {
                    File.Delete(m_ProjectPath + "\\" + m_ProjectName + ".data");
                }

                FileStream fileStream = new FileStream(m_ProjectPath + "\\" + m_ProjectName + ".data", FileMode.CreateNew);
                StreamWriter writer = new StreamWriter(fileStream);

                // classes 항목 작성
                writer.WriteLine("classes = " + m_LabelList.Count.ToString());
                // Train 항목 작성
                writer.WriteLine("train = " + m_ProjectPath + "\\train.txt");
                writer.WriteLine("valid = " + m_ProjectPath + "\\valid.txt");
                writer.WriteLine("names = " + m_ProjectPath + "\\" + m_ProjectName + ".names");
                writer.WriteLine("backup = " + m_ProjectPath + "\\Weights");

                writer.Close();
                writer = null;

                fileStream.Close();
                fileStream = null;
                #endregion

                /*
                try
                {
                    m_MaxFindCount = int.Parse(Findcount.Text);
                }
                catch
                {
                    m_MaxFindCount = 17;
                }
                */

                if (Directory.Exists(m_ProjectPath + "\\Temp\\Finded"))
                {
                    Directory.Delete(m_ProjectPath + "\\Temp\\Finded", true);
                }

                Directory.CreateDirectory(m_ProjectPath + "\\Temp\\Finded");

                List<string> list = new List<string>();
                if (dialog.FileNames.ToList().Count > 0)
                {
                    list = dialog.FileNames.ToList();
                }

                int count = 0;

                // 드래그 된 이미지에서 문자 검출 및 학습 리스트에 추가
                list.ForEach(filename =>
                {
                    if (filename.ToLower().EndsWith(".bmp") || filename.ToLower().EndsWith(".jpg") || filename.ToLower().EndsWith(".jpeg") || filename.ToLower().EndsWith(".png"))
                    {
                        string name = filename.Split('\\')[filename.Split('\\').Length - 1];
                        string new_name = m_ProjectPath + "\\Images\\NotLabeled\\" + name.Substring(0, name.Length - 4) + "_" + Math.Abs(DateTime.Now.ToBinary()).ToString() + name.Substring(name.Length - 4);

                        if (!Directory.Exists(m_ProjectPath + "\\Images\\NotLabeled"))
                        {
                            Directory.CreateDirectory(m_ProjectPath + "\\Images\\NotLabeled");
                        }

                        File.Copy(filename, new_name, true);

                        ImageInfo imageInfo = new ImageInfo(new_name);

                        System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(new_name);
                        imageInfo.Width = bmp.Width;
                        imageInfo.Height = bmp.Height;
                        bmp.Dispose();
                        bmp = null;

                        imageInfo.IsVisble = true;
                        imageInfo.TrainType = TrainType.NONE;

                        if (m_IsOriginImageInfoList)
                        {
                            m_ImageInfoList_Temp.Add(imageInfo);
                        }
                        else
                        {
                            m_ImageInfoList.Add(imageInfo);
                        }

                        Save_ImageList_Information(imageInfo);

                        count++;
                    }
                    else
                    {
                        MessageBox.Show("BMP 형식만 지원합니다.");
                    }
                });

                //Refresh_ImageList(ImageShowType.All);
                //Save_ImageList();

                // DrawImageLabel();

                Save_NameFile();

                Load_LabelList();
                RefreshcharInfo();
                Refresh_ImageList(m_ImageShowType);

                if (SelectedPage == 0)
                {
                    SelectedPage = 1;
                }

                if (m_IsOriginImageInfoList)
                {
                    LimitPage = m_ImageInfoList_Temp.Count / m_CntPerPage;

                    if (m_ImageInfoList_Temp.Count % m_CntPerPage > 0)
                    {
                        LimitPage++;
                    }
                }
                else
                {
                    LimitPage = m_ImageInfoList.Count / m_CntPerPage;

                    if (m_ImageInfoList.Count % m_CntPerPage > 0)
                    {
                        LimitPage++;
                    }
                }

                if (m_LimitPage < m_PagePerGroup)
                {
                    m_EndPage = m_LimitPage;
                }
                /*
                else
                {
                    if (m_SelectedPage % 10 > 0)
                    {
                        m_EndPage = (m_SelectedPage - (m_SelectedPage % 10)) + m_PagePerGroup;
                    }
                }
                */

                PartImageInfoList.Clear();

                int _start = m_CntPerPage * (m_SelectedPage - 1);
                int _end = _start + m_CntPerPage;

                if (_end > m_ImageInfoList.Count)
                {
                    _end = m_ImageInfoList.Count;
                }

                for (int i = _start; i < _end; i++)
                {
                    PartImageInfoList.Add(m_ImageInfoList[i]);
                }

                m_PageStack.Children.RemoveRange(2, m_PageStack.Children.Count - 4);

                for (int i = m_StartPage + 1; i <= m_EndPage; i++)
                {
                    TextBlock m_tb = new TextBlock();
                    m_tb.Margin = new Thickness(5);
                    m_tb.FontSize = 16;
                    m_tb.Width = 20;

                    if (i >= 100)
                    {
                        m_tb.Width = 30;
                    }

                    m_tb.VerticalAlignment = VerticalAlignment.Center;
                    Hyperlink _hyper = new Hyperlink();
                    _hyper.Click += ImageInfoListPage_Click;
                    TextBlock _tb = new TextBlock();
                    _tb.Text = Convert.ToString(i);

                    if (i == m_SelectedPage)
                    {
                        _tb.FontWeight = FontWeights.UltraBold;
                        _tb.IsEnabled = false;
                        m_OldPageTextBlock = _tb;
                    }

                    _hyper.Inlines.Add(_tb);
                    m_tb.Inlines.Add(_hyper);
                    m_PageStack.Children.Insert(m_PageStack.Children.Count - 2, m_tb);
                }

                if (m_IsOriginImageInfoList)
                {
                    m_Total_Img_Cnt = m_ImageInfoList_Temp.Count;
                    Image_Count.Content = m_ImageInfoList.Count + "/" + m_Total_Img_Cnt;
                }
                else
                {
                    m_Total_Img_Cnt = m_ImageInfoList.Count;
                    Image_Count.Content = m_ImageInfoList.Count + "/" + m_Total_Img_Cnt;
                }
            }
        }

        public void BrightnessUp_Click(object sender, RoutedEventArgs e)
        {
            m_Gamma -= 0.05;

            if (m_Gamma < 0)
            {
                m_Gamma = 0;
            }

            Mat mat = Cv2.ImRead(m_FileName);

            mat = SetBitmapGamma(mat, Math.Round(m_Gamma, 2));

            dp.canvas.Bitmap = mat.ToBitmapSource();
            dp.canvas.InvalidateVisual();
        }

        public void BrightnessDown_Click(object sender, RoutedEventArgs e)
        {
            m_Gamma += 0.05;

            Mat mat = Cv2.ImRead(m_FileName);

            mat = SetBitmapGamma(mat, Math.Round(m_Gamma, 2));

            dp.canvas.Bitmap = mat.ToBitmapSource();
            dp.canvas.InvalidateVisual();
        }

        private Mat SetBitmapGamma(Mat mat, double gamma)
        {
            Mat dst = mat.Clone();

            CorrectGamma(mat, dst, gamma);

            return dst;
        }

        public void CorrectGamma(Mat src, Mat dst, double gamma)
        {
            byte[] lut = new byte[256];
            for (int i = 0; i < lut.Length; i++)
            {
                lut[i] = (byte)(Math.Pow(i / 255.0, gamma) * 255.0);
            }

            Cv2.LUT(src, lut, dst);
        }

        public async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            ProgressDialogController controller = await this.ShowProgressAsync("새로고침 중...", "새로고침하는 중 입니다.", false);
            controller.SetIndeterminate();

            Thread.Sleep(200);

            await Task.Run(() =>
            {
                Refresh_ImageList(m_ImageShowType);

                LimitPage = m_ImageInfoList.Count / m_CntPerPage;

                if (m_ImageInfoList.Count % m_CntPerPage > 0)
                {
                    LimitPage++;
                }

                m_StartPage = 0;

                if (m_LimitPage < m_PagePerGroup)
                {
                    m_EndPage = m_LimitPage;
                }
                else
                {
                    m_EndPage = m_PagePerGroup;
                }

                if (m_PageStack != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        m_PageStack.Children.RemoveRange(2, m_PageStack.Children.Count - 4);

                        PartImageInfoList.Clear();

                        int _start = m_CntPerPage * 0;
                        int _end = _start + m_CntPerPage;

                        if (_end > m_ImageInfoList.Count)
                        {
                            _end = m_ImageInfoList.Count;
                        }

                        for (int i = _start; i < _end; i++)
                        {
                            PartImageInfoList.Add(m_ImageInfoList[i]);
                        }

                        for (int i = m_EndPage; i > m_StartPage; i--)
                        {
                            TextBlock m_tb = new TextBlock();
                            m_tb.Margin = new Thickness(5);
                            m_tb.FontSize = 16;
                            m_tb.Width = 20;

                            if (i >= 100)
                            {
                                m_tb.Width = 30;
                            }

                            m_tb.VerticalAlignment = VerticalAlignment.Center;
                            Hyperlink _hyper = new Hyperlink();
                            _hyper.Click += ImageInfoListPage_Click;
                            TextBlock _tb = new TextBlock();
                            _tb.Text = Convert.ToString(i);

                            if (i == 1)
                            {
                                _tb.FontWeight = FontWeights.UltraBold;
                                _tb.IsEnabled = false;
                                m_OldPageTextBlock = _tb;
                            }

                            _hyper.Inlines.Add(_tb);
                            m_tb.Inlines.Add(_hyper);
                            m_PageStack.Children.Insert(2, m_tb);
                        }
                    });
                }
            });

            await controller.CloseAsync();
        }

        public void Search_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Button_Click_6(null, null);
            }
        }

        public void ListViewModeBtn_Click(object sender, RoutedEventArgs e)
        {
            PreviewModeVisibility = Visibility.Collapsed;
            ListviewModeVisibility = Visibility.Visible;
        }

        public void PreviewModeBtn_Click(object sender, RoutedEventArgs e)
        {
            ListviewModeVisibility = Visibility.Collapsed;
            PreviewModeVisibility = Visibility.Visible;
        }

        public void ImportBtn_Click(object sender, RoutedEventArgs e)
        {
            ImportWindow window = new ImportWindow(m_LabelList);
            window.ShowDialog();

            if (window.ShowDialogResult)
            {
                window.Items.ToList().ForEach(x =>
                {
                    if (x.IsUse)
                    {
                        string filename = x.ImageInfo.FileName;
                        string name = filename.Split('\\')[filename.Split('\\').Length - 1];
                        string new_name = m_ProjectPath + "\\Images\\Labeled\\" + name.Substring(0, name.Length - 4) + "_" + Math.Abs(DateTime.Now.ToBinary()).ToString() + name.Substring(name.Length - 4);
                        File.Copy(filename, new_name, true);

                        ImageInfo imageInfo = new ImageInfo()
                        {
                            FileName = new_name,
                            CharList = x.ImageInfo.CharList,
                            Width = x.ImageInfo.Width,
                            Height = x.ImageInfo.Height,
                            DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            TrainType = TrainType.LABELED,
                            IsVisble = true
                        };

                        if (m_IsOriginImageInfoList)
                        {
                            m_ImageInfoList_Temp.Add(imageInfo);
                        }
                        else
                        {
                            ImageInfoList.Add(imageInfo);
                        }

                        Save_ImageList_Information(imageInfo);
                    }
                });

                //Refresh_ImageList(ImageShowType.All);
                //Save_ImageList();

                // DrawImageLabel();

                Save_NameFile();

                Load_LabelList();
                RefreshcharInfo();
                Refresh_ImageList(m_ImageShowType);

                if (m_IsOriginImageInfoList)
                {
                    LimitPage = m_ImageInfoList_Temp.Count / m_CntPerPage;

                    if (m_ImageInfoList_Temp.Count % m_CntPerPage > 0)
                    {
                        LimitPage++;
                    }
                }
                else
                {
                    LimitPage = m_ImageInfoList.Count / m_CntPerPage;

                    if (m_ImageInfoList.Count % m_CntPerPage > 0)
                    {
                        LimitPage++;
                    }
                }

                m_StartPage = m_SelectedPage / m_PagePerGroup * m_PagePerGroup;
                m_EndPage = (m_SelectedPage / m_PagePerGroup * m_PagePerGroup) + m_PagePerGroup;

                if (m_EndPage > LimitPage)
                {
                    m_EndPage = LimitPage;
                }

                PartImageInfoList.Clear();

                if (m_SelectedPage == 0)
                {
                    m_SelectedPage = 1;
                }

                int _start = m_CntPerPage * (m_SelectedPage - 1);
                int _end = _start + m_CntPerPage;

                if (_end > m_ImageInfoList.Count)
                {
                    _end = m_ImageInfoList.Count;
                }

                for (int i = _start; i < _end; i++)
                {
                    PartImageInfoList.Add(m_ImageInfoList[i]);
                }

                m_PageStack.Children.RemoveRange(2, m_PageStack.Children.Count - 4);

                for (int i = m_StartPage + 1; i <= m_EndPage; i++)
                {
                    TextBlock m_tb = new TextBlock();
                    m_tb.Margin = new Thickness(5);
                    m_tb.FontSize = 16;
                    m_tb.Width = 20;

                    if (i >= 100)
                    {
                        m_tb.Width = 30;
                    }

                    m_tb.VerticalAlignment = VerticalAlignment.Center;
                    Hyperlink _hyper = new Hyperlink();
                    _hyper.Click += ImageInfoListPage_Click;
                    TextBlock _tb = new TextBlock();
                    _tb.Text = Convert.ToString(i);

                    if (i == m_SelectedPage)
                    {
                        _tb.FontWeight = FontWeights.UltraBold;
                        _tb.IsEnabled = false;
                        m_OldPageTextBlock = _tb;
                    }

                    _hyper.Inlines.Add(_tb);
                    m_tb.Inlines.Add(_hyper);
                    m_PageStack.Children.Insert(m_PageStack.Children.Count - 2, m_tb);
                }

                if (m_IsOriginImageInfoList)
                {
                    m_Total_Img_Cnt = m_ImageInfoList_Temp.Count;
                    Image_Count.Content = m_ImageInfoList.Count + "/" + m_Total_Img_Cnt;
                }
                else
                {
                    m_Total_Img_Cnt = m_ImageInfoList.Count;
                    Image_Count.Content = m_ImageInfoList.Count + "/" + m_Total_Img_Cnt;
                }
            }
        }

        public void dgImageList_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (dp.BitmapImage != null)
                {
                    if (!m_IsSaved)
                    {
                        if (m_IsOriginImageInfoList)
                        {
                            for (int i = 0; i < m_ImageInfoList_Temp.Count; i++)
                            {
                                if (Path.GetFileName(m_ImageInfoList_Temp[i].FileName) == Path.GetFileName(m_PreSaveImageInfo.FileName))
                                {
                                    m_ImageInfoList_Temp[i] = m_PreSaveImageInfo;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < m_ImageInfoList.Count; i++)
                            {
                                if (Path.GetFileName(m_ImageInfoList[i].FileName) == Path.GetFileName(m_PreSaveImageInfo.FileName))
                                {
                                    m_ImageInfoList[i] = m_PreSaveImageInfo;
                                    break;
                                }
                            }
                        }

                        for (int i = 0; i < PartImageInfoList.Count; i++)
                        {
                            if (Path.GetFileName(PartImageInfoList[i].FileName) == Path.GetFileName(m_PreSaveImageInfo.FileName))
                            {
                                PartImageInfoList[i] = m_PreSaveImageInfo;
                                break;
                            }
                        }
                    }
                }

                // m_lbImageList_MouseLeftButtonDown = false;

                if (dgImageList.SelectedItem == null)
                {
                    return;
                }

                m_SelectedImageIndex = -1;
                int visblecount = 0;
                int count = 0;

                PartImageInfoList.ToList().ForEach(x =>
                {
                    if (x.IsVisble)
                    {
                        if (visblecount == dgImageList.SelectedIndex)
                        {
                            m_SelectedImageIndex = count;
                        }

                        visblecount++;
                    }

                    count++;
                });

                if (dp.SelectRectangle != null)
                {
                    if (m_IsTempCharInfo)
                    {
                        // m_SelectedImageInfo.CharList.Add(m_TempCharInfo);
                        m_IsTempCharInfo = false;
                    }

                    DrawImageLabel();

                    dp.SelectRectangle = null;
                }

                m_SelectedImageInfo = PartImageInfoList[m_SelectedImageIndex];

                if (Path.GetFileName(m_SelectedImageInfo.FileName) != Path.GetFileName(m_PreSaveImageInfo.FileName))
                {
                    m_IsSaved = true;
                    SaveBtnVisibility = Visibility.Hidden;
                }

                m_PreSaveImageInfo = new ImageInfo()
                {
                    FileName = m_SelectedImageInfo.FileName,
                    CharList = new List<CharInfo>(),
                    Width = m_SelectedImageInfo.Width,
                    Height = m_SelectedImageInfo.Height,
                    DateTime = m_SelectedImageInfo.DateTime,
                    IsVisble = m_SelectedImageInfo.IsVisble,
                    Find_String = m_SelectedImageInfo.Find_String,
                    Thumnail_FileName = m_SelectedImageInfo.Thumnail_FileName,
                    TrainType = m_SelectedImageInfo.TrainType
                };

                for (int i = 0; i < m_SelectedImageInfo.CharList.Count; i++)
                {
                    m_PreSaveImageInfo.CharList.Add(m_SelectedImageInfo.CharList[i]);
                }

                if (!File.Exists(m_SelectedImageInfo.FileName))
                {
                    return;
                }

                m_OldSelectedImageInfo = m_SelectedImageInfo;

                dp.ConfirmButtonVisibility = Visibility.Collapsed;
                dp.IsShowRectangle = false;
                dp.SelectRectangle = null;
                dp.RemoveSelectRectangle();

                /*
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(m_SelectedImageInfo.FileName);
                bi.EndInit();
                dp.BitmapImage = bi;
                */

                BitmapImage bi = new BitmapImage();
                Mat mat = Cv2.ImRead(m_SelectedImageInfo.FileName);
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                // bi.UriSource = new Uri(imageInfo.FileName);
                bi.StreamSource = mat.ToMemoryStream();
                bi.EndInit();
                mat.Dispose();
                mat = null;

                dp.BitmapImage = bi;

                m_FileName = m_SelectedImageInfo.FileName;
                m_Gamma = 1.0;

                DrawImageLabel();

                dp.Focus();
                /*

                if (dgImageList.SelectedItem == null)
                {
                    return;
                }


                m_SelectedImageIndex = -1;
                int visblecount = 0;
                int count = 0;

                PartImageInfoList.ToList().ForEach(x =>
                {
                    if (x.IsVisble)
                    {
                        if (visblecount == dgImageList.SelectedIndex)
                        {
                            m_SelectedImageIndex = count;
                        }

                        visblecount++;
                    }

                    count++;
                });

                if (dp.SelectRectangle != null)
                {
                    if (m_IsTempCharInfo)
                    {
                        // m_SelectedImageInfo.CharList.Add(m_TempCharInfo);
                        m_IsTempCharInfo = false;
                    }

                    DrawImageLabel();

                    dp.SelectRectangle = null;
                }

                m_SelectedImageInfo = PartImageInfoList[m_SelectedImageIndex];

                if (!File.Exists(m_SelectedImageInfo.FileName))
                {
                    return;
                }

                m_OldSelectedImageInfo = m_SelectedImageInfo;

                dp.ConfirmButtonVisibility = Visibility.Collapsed;
                dp.IsShowRectangle = false;
                dp.SelectRectangle = null;
                dp.RemoveSelectRectangle();

                BitmapImage bi = new BitmapImage();
                Mat mat = Cv2.ImRead(m_SelectedImageInfo.FileName);
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                // bi.UriSource = new Uri(imageInfo.FileName);
                bi.StreamSource = mat.ToMemoryStream();
                bi.EndInit();
                mat.Dispose();
                mat = null;

                dp.BitmapImage = bi;

                m_FileName = m_SelectedImageInfo.FileName;
                m_Gamma = 1.0;

                DrawImageLabel();
                */
            }
            catch (Exception ex)
            {
                LogManager.Error(ex.Message);
            }
        }

        public void dgImageList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                if (dgImageList.SelectedIndex > 0)
                {
                    dgImageList.SelectedIndex--;
                }
            }
        }

        public void dgImageList_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            dgImageList.Focusable = true;
            dgImageList.Focus();
        }

        private void PageStackRefresh()
        {
            if (m_IsOriginImageInfoList)
            {
                LimitPage = m_ImageInfoList_Temp.Count / m_CntPerPage;

                if (m_ImageInfoList_Temp.Count % m_CntPerPage > 0)
                {
                    LimitPage++;
                }
            }
            else
            {
                LimitPage = m_ImageInfoList.Count / m_CntPerPage;

                if (m_ImageInfoList.Count % m_CntPerPage > 0)
                {
                    LimitPage++;
                }
            }

            m_StartPage = m_SelectedPage / m_PagePerGroup * m_PagePerGroup;
            m_EndPage = (m_SelectedPage / m_PagePerGroup * m_PagePerGroup) + m_PagePerGroup;

            if (m_EndPage > LimitPage)
            {
                m_EndPage = LimitPage;
            }

            PartImageInfoList.Clear();

            if (m_SelectedPage == 0)
            {
                m_SelectedPage = 1;
            }

            int _start = m_CntPerPage * (m_SelectedPage - 1);
            int _end = _start + m_CntPerPage;

            if (_end > m_ImageInfoList.Count)
            {
                _end = m_ImageInfoList.Count;
            }

            for (int i = _start; i < _end; i++)
            {
                PartImageInfoList.Add(m_ImageInfoList[i]);
            }

            m_PageStack.Children.RemoveRange(2, m_PageStack.Children.Count - 4);

            for (int i = m_StartPage + 1; i <= m_EndPage; i++)
            {
                TextBlock m_tb = new TextBlock();
                m_tb.Margin = new Thickness(5);
                m_tb.FontSize = 16;
                m_tb.Width = 20;

                if (i >= 100)
                {
                    m_tb.Width = 30;
                }

                m_tb.VerticalAlignment = VerticalAlignment.Center;
                Hyperlink _hyper = new Hyperlink();
                _hyper.Click += ImageInfoListPage_Click;
                TextBlock _tb = new TextBlock();
                _tb.Text = Convert.ToString(i);

                if (i == m_SelectedPage)
                {
                    _tb.FontWeight = FontWeights.UltraBold;
                    _tb.IsEnabled = false;
                    m_OldPageTextBlock = _tb;
                }

                _hyper.Inlines.Add(_tb);
                m_tb.Inlines.Add(_hyper);
                m_PageStack.Children.Insert(m_PageStack.Children.Count - 2, m_tb);
            }
        }

        public void ChangeWeightsFile_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            CommonFileDialogFilter filter = new CommonFileDialogFilter("*.weights", "*.weights");
            dialog.Filters.Add(filter);

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                LogManager.Action("학습 데이터 변경 시작");

                if (m_Process != null && m_Process.HasExited)
                {
                    /*
                    m_Process.Kill();
                    m_Process.Close();
                    m_Process.Dispose();
                    m_Process = null;
                    LogManager.Action("인식 엔진 프로그램 종료");
                    */
                }

                LogManager.Action("인식 엔진 프로그램 실행 시도");
                ProcessStartInfo _i = new ProcessStartInfo();
                _i.FileName = AppDomain.CurrentDomain.BaseDirectory + "Detector\\Detector.exe";
                /*
                _i.UseShellExecute = false;
                _i.RedirectStandardInput = true;
                _i.RedirectStandardOutput = true;
                _i.RedirectStandardError = true;
                _i.CreateNoWindow = true;
                _i.WindowStyle = ProcessWindowStyle.Hidden;
                */
                m_Process = new Process();
                // m_Process.EnableRaisingEvents = false;
                m_Process.StartInfo = _i;
                /*
                m_Process.OutputDataReceived += Process_OutputDataReceived;
                m_Process.ErrorDataReceived += Process_ErrorDataReceived;
                */
                // m_Process.Start();
                /*
                m_Process.BeginOutputReadLine();
                m_Process.BeginErrorReadLine();
                */
                LogManager.Action("인식 엔진 프로그램 실행 완료");

                m_WeightsFile = dialog.FileName;
                /*
                m_Process.StandardInput.WriteLine(m_ProjectPath + "\\" + m_ProjectName + ".cfg" + Environment.NewLine + m_WeightsFile +
                    Environment.NewLine + m_ProjectPath + "\\" + m_ProjectName + ".names" + Environment.NewLine + Convert.ToString(NetWidth) + Environment.NewLine +
                    Convert.ToString(NetHeight) + Environment.NewLine + Convert.ToString(NetChannels));
                LogManager.Action("인식 엔진 프로그램 파라미터 전송 완료");
                */

                LogManager.Action("학습 데이터 변경 완료");
            }
        }

        public void AddLabel_Click(object sender, RoutedEventArgs e)
        {
            
        }

        public DataGridCell GetCell(int row, int column)
        {
            DataGridRow rowContainer = GetRow(row);

            if (rowContainer != null)
            {
                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(rowContainer);

                // try to get the cell but it may possibly be virtualized
                DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                if (cell == null)
                {
                    // now try to bring into view and retreive the cell
                    dg.ScrollIntoView(rowContainer, dg.Columns[column]);
                    cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                }
                return cell;
            }
            return null;
        }

        public DataGridRow GetRow(int index)
        {
            DataGridRow row = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(index);
            if (row == null)
            {
                // may be virtualized, bring into view and try again
                dg.ScrollIntoView(dg.Items[index]);
                row = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(index);
            }
            return row;
        }

        static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

        private void dg_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            DataGridCell cell = GetCell(dg.Items.Count - 1, 0);
            TextBox item = cell.Content as TextBox;

            if (item.Text == "")
            {
                MessageBox.Show("추가할 라벨명을 입력해주세요.");
                e.Cancel = true;
            }
            else
            {
                string content = "";

                LabelList[LabelList.Count - 1].LabelName = item.Text;

                LabelList.ToList().ForEach(x =>
                {
                    content += x.LabelName + Environment.NewLine;
                });

                File.WriteAllText(m_ProjectPath + "\\" + m_ProjectName + ".names", content);
            }
        }

        private void GPUMemoryClear()
        {
            LogManager.Action("GPUMemoryClear() 실행");

            CudafyModes.Target = eGPUType.Cuda;
            CudafyModes.DeviceId = 0;
            CudafyTranslator.Language = CudafyModes.Target == eGPUType.OpenCL ? eLanguage.OpenCL : eLanguage.Cuda;

            if (CudafyHost.GetDeviceCount(CudafyModes.Target) == 0)
            {
                throw new System.ArgumentException("No suitable devices found.", "original");
            }

            GPGPU gpu = CudafyHost.GetDevice(CudafyModes.Target, CudafyModes.DeviceId);

            LogManager.Action("메모리 해제 전 TotalMemory : " + gpu.TotalMemory);
            LogManager.Action("메모리 해제 전 FreeMemory : " + gpu.FreeMemory);

            gpu.FreeAll();
            gpu.HostFreeAll();

            LogManager.Action("메모리 해제 후 TotalMemory : " + gpu.TotalMemory);
            LogManager.Action("메모리 해제 후 FreeMemory : " + gpu.FreeMemory);

            gpu.Dispose();
            gpu = null;
            LogManager.Action("GPUMemoryClear() 종료");
        }

        private void Button_Click14(object sender, RoutedEventArgs e)
        {

        }

        private byte[] CreateSendBuffer(byte[] data)
        {
            byte[] sendData = data;
            byte[] buffer = new byte[4 + sendData.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(sendData.Length), 0, buffer, 0, 4);
            Buffer.BlockCopy(sendData, 0, buffer, 4, sendData.Length);

            return buffer;
        }
    }
}
