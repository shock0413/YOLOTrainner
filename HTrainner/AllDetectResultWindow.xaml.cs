using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using HanseroDisplay;
using HanseroDisplay.Struct;
using MahApps.Metro.Controls;
using static HsrAITrainner.MainWindow;

namespace HsrAITrainner
{
    /// <summary>
    /// AllDetectResultWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AllDetectResultWindow : MetroWindow, INotifyPropertyChanged
    {
        private DataGrid m_DataGrid;
        private Expander m_Expander;

        public ObservableCollection<StructDetectResult> Result { get { return result; } set { result = value; NotifyPropertyChanged("Result"); } }
        private ObservableCollection<StructDetectResult> result;

        private ObservableCollection<ImageInfo> m_OriginSource;

        public BitmapImage Image { get { return image; } set { image = value; NotifyPropertyChanged("Image"); } }
        private BitmapImage image;

        public int AllImageCount { get { return m_AllImageCount; } set { m_AllImageCount = value; NotifyPropertyChanged("AllImageCount"); } }
        private int m_AllImageCount = 0;
        public int AllDetectCount { get { return m_AllDetectCount; } set { m_AllDetectCount = value; NotifyPropertyChanged("AllDetectCount"); } }
        private int m_AllDetectCount = 0;

        public int AllMatchCount { get { return m_AllMatchCount; } set { m_AllMatchCount = value; NotifyPropertyChanged("AllMatchCount"); } }
        private int m_AllMatchCount = 0;

        public double Image_Height { get { return m_Image_Height; } set { m_Image_Height = value; NotifyPropertyChanged("Image_Height"); } }
        private double m_Image_Height = 0;

        public Visibility SaveBtnVisibility { get { return m_SaveBtnVisibility; } set { m_SaveBtnVisibility = value; NotifyPropertyChanged("SaveBtnVisibility"); } }
        private Visibility m_SaveBtnVisibility = Visibility.Hidden;

        public Visibility AdjustBrightnessVisibility { get { return m_AdjustBrightnessVisibility; } set { m_AdjustBrightnessVisibility = value; NotifyPropertyChanged("AdjustBrightnessVisibility"); } }
        private Visibility m_AdjustBrightnessVisibility = Visibility.Hidden;

        public Visibility DisplayVisibility { get { return m_DisplayVisibility; } set { m_DisplayVisibility = value; NotifyPropertyChanged("DisplayVisibility"); } }
        private Visibility m_DisplayVisibility = Visibility.Collapsed;

        public Visibility TrainLabelVisibility { get { return m_TrainLabelVisibility; } set { m_TrainLabelVisibility = value; NotifyPropertyChanged("TrainLabelVisibility"); } }
        private Visibility m_TrainLabelVisibility;

        // 라벨링할 이미지 정보 리턴.
        public StructDetectResult m_SelectDetectResult;

        public bool DetectLabelEnabled
        {
            get
            {
                return m_DetectLabelEnabled;
            }
            set
            {
                m_DetectLabelEnabled = value;

                LabelEnabledCheck();

                NotifyPropertyChanged("LabelEnabled");
            }
        }
        private bool m_DetectLabelEnabled = true;

        public bool TrainLabelEnabled
        {
            get
            {
                return m_TrainLabelEnabled;
            }
            set
            {
                m_TrainLabelEnabled = value;

                LabelEnabledCheck();

                NotifyPropertyChanged("LabelEnabled");
            }
        }
        private bool m_TrainLabelEnabled = false;

        public bool ScoreEnabled
        {
            get
            {
                return m_ScoreEnabled;
            }
            set
            {
                m_ScoreEnabled = value;

                LabelEnabledCheck();

                NotifyPropertyChanged("ScoreEnabled");
            }
        }
        private bool m_ScoreEnabled = true;

        public AllDetectResultWindow(ObservableCollection<StructDetectResult> result)
        {
            InitializeComponent();

            this.DataContext = this;

            Result = result;
        }

        public AllDetectResultWindow(int all_Image_Count, int all_Detect_Count, int all_Match_Count, ObservableCollection<StructDetectResult> result, bool isTrainLabelEnabled = false)
        {
            InitializeComponent();

            this.DataContext = this;

            this.Result = result;

            if (m_OriginSource != null)
            {
                m_OriginSource.Clear();
                m_OriginSource = null;
            }

            if (m_DataGrid != null)
            {
                m_DataGrid.Items.Clear();
            }

            this.AllImageCount = all_Image_Count;
            this.AllDetectCount = all_Detect_Count;
            this.AllMatchCount = all_Match_Count;
            
            if (isTrainLabelEnabled)
            {
                TrainLabelVisibility = Visibility.Visible;
            }
            else
            {
                TrainLabelVisibility = Visibility.Hidden;
            }
        }

        public AllDetectResultWindow(int all_Image_Count, int all_Detect_Count, int all_Match_Count, ObservableCollection<StructDetectResult> result, ObservableCollection<ImageInfo> originSource, bool isTrainLabelEnabled = true)
        {
            InitializeComponent();

            this.DataContext = this;

            this.Result = result;

            this.m_OriginSource = originSource;

            this.AllImageCount = all_Image_Count;
            this.AllDetectCount = all_Detect_Count;
            this.AllMatchCount = all_Match_Count;

            if (isTrainLabelEnabled)
            {
                TrainLabelVisibility = Visibility.Visible;
            }
            else
            {
                TrainLabelVisibility = Visibility.Hidden;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            m_DataGrid = (DataGrid)sender;
        }

        public void DataGrid_Click(object sender, RoutedEventArgs e) 
        {
            
        }

        public void Expander_Loaded(object sender, RoutedEventArgs e)
        {
            m_Expander = (Expander)sender;
        }

        public void Expander_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Image_Height = m_Expander.ActualHeight * 0.92;
        }

        public void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Image_Height = m_Expander.ActualHeight * 0.92;
        }

        public void Window_StateChanged(object sender, EventArgs e)
        {
            Image_Height = m_Expander.ActualHeight * 0.92;
        }

        public void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // StructDetectResult r = (StructDetectResult)m_DataGrid.SelectedItem;

            dp.BitmapImage = null;
            dp1.BitmapImage = null;
            dp2.BitmapImage = null;
            dp3.BitmapImage = null;
            dp4.BitmapImage = null;

            int index = m_DataGrid.SelectedIndex;

            if (index < 0)
            {
                return;
            }

            int cnt = 0;
            int length = index + 5;
            
            if (length > m_DataGrid.Items.Count)
            {
                length = m_DataGrid.Items.Count;
            }

            for (int i = index; i < length; i++)
            {
                StructDetectResult r = (StructDetectResult)m_DataGrid.Items[i];

                switch (cnt)
                {
                    case 0:
                        dp.BitmapImage = new BitmapImage(new Uri(r.FileName, UriKind.RelativeOrAbsolute));
                        break;
                    case 1:
                        dp1.BitmapImage = new BitmapImage(new Uri(r.FileName, UriKind.RelativeOrAbsolute));
                        break;
                    case 2:
                        dp2.BitmapImage = new BitmapImage(new Uri(r.FileName, UriKind.RelativeOrAbsolute));
                        break;
                    case 3:
                        dp3.BitmapImage = new BitmapImage(new Uri(r.FileName, UriKind.RelativeOrAbsolute));
                        break;
                    case 4:
                        dp4.BitmapImage = new BitmapImage(new Uri(r.FileName, UriKind.RelativeOrAbsolute));
                        break;
                }

                cnt++;
            }

            LabelEnabledCheck();
        }

        public void Window_Closing(object sender, CancelEventArgs e)
        {
            GC.Collect();
        }

        public void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            topRow.Height = new GridLength(1, GridUnitType.Star);
            bottomRow.Height = new GridLength(1, GridUnitType.Auto);
        }

        public void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            bottomRow.Height = new GridLength(1, GridUnitType.Star);
        }

        private void LabelEnabledCheck()
        {
            dp.Clear();
            dp1.Clear();
            dp2.Clear();
            dp3.Clear();
            dp4.Clear();

            int index = m_DataGrid.SelectedIndex;

            if (m_DetectLabelEnabled)
            {
                if (m_DataGrid != null)
                {
                    int cnt = 0;
                    int length = index + 5;

                    if (length > m_DataGrid.Items.Count)
                    {
                        length = m_DataGrid.Items.Count;
                    }

                    for (int i = index; i < length; i++)
                    {
                        StructDetectResult r = (StructDetectResult)m_DataGrid.Items[i];

                        switch (cnt)
                        {
                            case 0:
                                if (dp.BitmapImage != null)
                                {
                                    r.Items.ForEach(y =>
                                    {
                                        Pen pen = new Pen(Brushes.LimeGreen, 2);

                                        dp.DrawRectangle(y.Type, Brushes.Transparent, pen, y.X, y.Y, y.Width, y.Height);

                                        if (ScoreEnabled)
                                        {
                                            dp.DrawLabel(y.Type, new System.Drawing.Point(y.X, y.Y + y.Height), new FormattedText(y.Type, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                                        }
                                        else
                                        {
                                            dp.DrawLabel(y.Type, new System.Drawing.Point(y.X, y.Y + y.Height), new FormattedText(y.Type, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                                        }
                                        // dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                                    });
                                }
                                break;
                            case 1:
                                if (dp1.BitmapImage != null)
                                {
                                    r.Items.ForEach(y =>
                                    {
                                        Pen pen = new Pen(Brushes.LimeGreen, 2);

                                        dp1.DrawRectangle(y.Type, Brushes.Transparent, pen, y.X, y.Y, y.Width, y.Height);

                                        if (ScoreEnabled)
                                        {
                                            dp1.DrawLabel(y.Type, new System.Drawing.Point(y.X, y.Y + y.Height), new FormattedText(y.Type, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                                        }
                                        else
                                        {
                                            dp1.DrawLabel(y.Type, new System.Drawing.Point(y.X, y.Y + y.Height), new FormattedText(y.Type, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                                        }
                                        // dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                                    });
                                }
                                break;
                            case 2:
                                if (dp2.BitmapImage != null)
                                {
                                    r.Items.ForEach(y =>
                                    {
                                        Pen pen = new Pen(Brushes.LimeGreen, 2);

                                        dp2.DrawRectangle(y.Type, Brushes.Transparent, pen, y.X, y.Y, y.Width, y.Height);

                                        if (ScoreEnabled)
                                        {
                                            dp2.DrawLabel(y.Type, new System.Drawing.Point(y.X, y.Y + y.Height), new FormattedText(y.Type, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                                        }
                                        else
                                        {
                                            dp2.DrawLabel(y.Type, new System.Drawing.Point(y.X, y.Y + y.Height), new FormattedText(y.Type, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                                        }
                                        // dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                                    });
                                }
                                break;
                            case 3:
                                if (dp3.BitmapImage != null)
                                {
                                    r.Items.ForEach(y =>
                                    {
                                        Pen pen = new Pen(Brushes.LimeGreen, 2);

                                        dp3.DrawRectangle(y.Type, Brushes.Transparent, pen, y.X, y.Y, y.Width, y.Height);

                                        if (ScoreEnabled)
                                        {
                                            dp3.DrawLabel(y.Type, new System.Drawing.Point(y.X, y.Y + y.Height), new FormattedText(y.Type, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                                        }
                                        else
                                        {
                                            dp3.DrawLabel(y.Type, new System.Drawing.Point(y.X, y.Y + y.Height), new FormattedText(y.Type, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                                        }
                                        // dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                                    });
                                }
                                break;
                            case 4:
                                if (dp4.BitmapImage != null)
                                {
                                    r.Items.ForEach(y =>
                                    {
                                        Pen pen = new Pen(Brushes.LimeGreen, 2);

                                        dp4.DrawRectangle(y.Type, Brushes.Transparent, pen, y.X, y.Y, y.Width, y.Height);

                                        if (ScoreEnabled)
                                        {
                                            dp4.DrawLabel(y.Type, new System.Drawing.Point(y.X, y.Y + y.Height), new FormattedText(y.Type, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                                        }
                                        else
                                        {
                                            dp4.DrawLabel(y.Type, new System.Drawing.Point(y.X, y.Y + y.Height), new FormattedText(y.Type, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                                        }
                                        // dp.DrawLabel("", new System.Drawing.Point((int)(x.Finded_X * dp.BitmapImage.Width - (x.Finded_Width * dp.BitmapImage.Width) / 2), (int)(x.Finded_Y * dp.BitmapImage.Height - (x.Finded_Height * dp.BitmapImage.Height) / 2)), new FormattedText(x.Finded_Name + ":" + x.Finded_Score.ToString("0.00"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("돋움"), 20, Brushes.Lime, 20), HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT);
                                    });
                                }
                                break;
                        }

                        cnt++;
                    }

                    m_Expander.IsExpanded = true;
                }
            }
            else
            {
                dp.Clear();
            }

            if (m_TrainLabelEnabled)
            {
                if (m_DataGrid != null)
                {
                    int cnt = 0;
                    int length = index + 5;

                    if (length > m_DataGrid.Items.Count)
                    {
                        length = m_DataGrid.Items.Count;
                    }

                    for (int _i = index; _i < length; _i++)
                    {
                        StructDetectResult r = (StructDetectResult)m_DataGrid.Items[_i];

                        switch (cnt)
                        {
                            case 0:
                                if (dp.BitmapImage != null)
                                {
                                    for (int i = 0; i < m_OriginSource.Count; i++)
                                    {
                                        if (r.FileName == m_OriginSource[i].FileName)
                                        {
                                            Pen pen = new Pen(Brushes.Red, 1);

                                            m_OriginSource[i].CharList.ForEach(x =>
                                            {
                                                dp.DrawRectangle("", Brushes.Transparent, pen, x.Labeled_X * dp.BitmapImage.Width - (x.Labeled_Width * dp.BitmapImage.Width / 2), x.Labeled_Y * dp.BitmapImage.Height - (x.Labeled_Height * dp.BitmapImage.Height / 2), x.Labeled_Width * dp.BitmapImage.Width, x.Labeled_Height * dp.BitmapImage.Height);
                                            });

                                            break;
                                        }
                                    }
                                }
                                break;
                            case 1:
                                if (dp1.BitmapImage != null)
                                {
                                    for (int i = 0; i < m_OriginSource.Count; i++)
                                    {
                                        if (r.FileName == m_OriginSource[i].FileName)
                                        {
                                            Pen pen = new Pen(Brushes.Red, 1);

                                            m_OriginSource[i].CharList.ForEach(x =>
                                            {
                                                dp1.DrawRectangle("", Brushes.Transparent, pen, x.Labeled_X * dp1.BitmapImage.Width - (x.Labeled_Width * dp1.BitmapImage.Width / 2), x.Labeled_Y * dp1.BitmapImage.Height - (x.Labeled_Height * dp1.BitmapImage.Height / 2), x.Labeled_Width * dp1.BitmapImage.Width, x.Labeled_Height * dp1.BitmapImage.Height);
                                            });

                                            break;
                                        }
                                    }
                                }
                                break;
                            case 2:
                                if (dp2.BitmapImage != null)
                                {
                                    for (int i = 0; i < m_OriginSource.Count; i++)
                                    {
                                        if (r.FileName == m_OriginSource[i].FileName)
                                        {
                                            Pen pen = new Pen(Brushes.Red, 1);

                                            m_OriginSource[i].CharList.ForEach(x =>
                                            {
                                                dp2.DrawRectangle("", Brushes.Transparent, pen, x.Labeled_X * dp2.BitmapImage.Width - (x.Labeled_Width * dp2.BitmapImage.Width / 2), x.Labeled_Y * dp2.BitmapImage.Height - (x.Labeled_Height * dp2.BitmapImage.Height / 2), x.Labeled_Width * dp2.BitmapImage.Width, x.Labeled_Height * dp2.BitmapImage.Height);
                                            });

                                            break;
                                        }
                                    }
                                }
                                break;
                            case 3:
                                if (dp3.BitmapImage != null)
                                {
                                    for (int i = 0; i < m_OriginSource.Count; i++)
                                    {
                                        if (r.FileName == m_OriginSource[i].FileName)
                                        {
                                            Pen pen = new Pen(Brushes.Red, 1);

                                            m_OriginSource[i].CharList.ForEach(x =>
                                            {
                                                dp3.DrawRectangle("", Brushes.Transparent, pen, x.Labeled_X * dp3.BitmapImage.Width - (x.Labeled_Width * dp3.BitmapImage.Width / 2), x.Labeled_Y * dp3.BitmapImage.Height - (x.Labeled_Height * dp3.BitmapImage.Height / 2), x.Labeled_Width * dp3.BitmapImage.Width, x.Labeled_Height * dp3.BitmapImage.Height);
                                            });

                                            break;
                                        }
                                    }
                                }
                                break;
                            case 4:
                                if (dp4.BitmapImage != null)
                                {
                                    for (int i = 0; i < m_OriginSource.Count; i++)
                                    {
                                        if (r.FileName == m_OriginSource[i].FileName)
                                        {
                                            Pen pen = new Pen(Brushes.Red, 1);

                                            m_OriginSource[i].CharList.ForEach(x =>
                                            {
                                                dp4.DrawRectangle("", Brushes.Transparent, pen, x.Labeled_X * dp4.BitmapImage.Width - (x.Labeled_Width * dp4.BitmapImage.Width / 2), x.Labeled_Y * dp4.BitmapImage.Height - (x.Labeled_Height * dp4.BitmapImage.Height / 2), x.Labeled_Width * dp4.BitmapImage.Width, x.Labeled_Height * dp4.BitmapImage.Height);
                                            });

                                            break;
                                        }
                                    }
                                }
                                break;
                        }

                        cnt++;
                    }

                    m_Expander.IsExpanded = true;
                }
            }
        }

        public void UpButton_Click(object sender, RoutedEventArgs e)
        {
            int index = m_DataGrid.SelectedIndex - 5;

            if (index < 0)
            {
                index = 0;
            }

            m_DataGrid.SelectedIndex = index;
        }

        public void DownButton_Click(object sender, RoutedEventArgs e)
        {
            int index = m_DataGrid.SelectedIndex + 5;

            if (index > m_DataGrid.Items.Count - 1)
            {
                index = m_DataGrid.Items.Count - 1;
            }

            m_DataGrid.SelectedIndex = index;
        }

        public void DataGrid_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (m_DataGrid.SelectedItem != null)
            {
                m_SelectDetectResult = (StructDetectResult)m_DataGrid.SelectedItem;
                this.Close();
            }
        }
    }
}
