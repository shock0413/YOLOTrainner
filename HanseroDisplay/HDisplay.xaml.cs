
using HanseroDisplay.Struct;
using HCore;
using HCore.HDrawPoints;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;

namespace HanseroDisplay
{
    /// <summary>
    /// HDiaplay.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class HDisplay : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        public bool m_IsConfirm = false;
        public bool m_IsReConfirm = false;
        public bool m_IsCtrlPressed = false;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public Visibility ConfirmButtonVisibility { get { return confirmButtonVisibility; } set { confirmButtonVisibility = value; NotifyPropertyChanged("ConfirmButtonVisibility"); } }
        private Visibility confirmButtonVisibility = Visibility.Collapsed;
        public Visibility CancelButtonVisibility { get { return cancelButtonVisibility; } set { cancelButtonVisibility = value; NotifyPropertyChanged("CancelButtonVisibility"); } }
        private Visibility cancelButtonVisibility = Visibility.Collapsed;

        private HRectangle m_CopyRectangle = null;

        public BitmapSource BitmapImage
        {
            get { return (BitmapSource)GetValue(BitmapImageProperty); }
            set
            {
                GC.Collect();

                if (value != null)
                {
                    SetValue(BitmapImageProperty, value);
                }
                else
                {
                    SetValue(BitmapImageProperty, value);
                }
                RemoveSelectRectangle();
                this.Result = null;
                cv.Bitmap = BitmapImage;

                if (BitmapImage != null)
                {
                    cv.BitmapWidth = BitmapImage.Width;
                    cv.BitmapHeight = BitmapImage.Height;

                    if (!m_IsConfirm && !m_IsReConfirm)
                    {
                        cv.Fit();
                    }
                    else if (m_IsConfirm)
                    {
                        m_IsConfirm = false;
                        m_IsReConfirm = true;
                    }
                    else if (m_IsReConfirm)
                    {
                        m_IsReConfirm = false;
                        m_IsConfirm = false;
                    }
                }
            }
        }

        public static readonly DependencyProperty BitmapImageProperty = DependencyProperty.Register(
            "BitmapImage",
            typeof(BitmapSource),
            typeof(HDisplay),
            new PropertyMetadata(OnBitmapImageChanged));

        static void OnBitmapImageChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (BitmapImageChanged != null)
            {
                BitmapImageChanged(obj);
            }
        }

        public delegate void BitmapImageChangeHandler(object sender);
        public static BitmapImageChangeHandler BitmapImageChanged = delegate { };

        public delegate void OnDoubleClickEventHandler();
        public event OnDoubleClickEventHandler OnDoubleClickEvent = delegate { };

        //Rectangle
        public ObservableCollection<StructRectangle> Rectangles
        {
            get { return (ObservableCollection<StructRectangle>)GetValue(RectanglesProperty); }
            set
            {
                if (value != null)
                {
                    cv.ListRectangle.Clear();
                    value.CollectionChanged += Rectangle_CollectionChanged;
                    SetValue(RectanglesProperty, value);
                }

                cv.InvalidateVisual();
            }
        }

        private void Rectangle_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (StructRectangle obj in e.NewItems)
                {
                    cv.ListRectangle.Add(obj);
                }
                cv.InvalidateVisual();
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                foreach (StructRectangle obj in e.NewItems)
                {
                    cv.ListRectangle.Remove(obj);
                }
                cv.InvalidateVisual();
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                cv.ListRectangle.Clear();
                cv.InvalidateVisual();
            }
        }

        public static readonly DependencyProperty RectanglesProperty = DependencyProperty.Register(
            "Rectangles",
            typeof(ObservableCollection<StructRectangle>),
            typeof(HDisplay),
            new PropertyMetadata(OnRectanglesChanged));

        static void OnRectanglesChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (RectanglesChanged != null)
            {
                RectanglesChanged(obj);
            }
        }

        public delegate void RectanglesChangeHandler(object sender);
        public static RectanglesChangeHandler RectanglesChanged = delegate { };

        public ObservableCollection<StructLine> Lines
        {
            get { return (ObservableCollection<StructLine>)GetValue(LinesProperty); }
            set
            {
                if (value != null)
                {
                    cv.ListLine.Clear();
                    value.CollectionChanged += Lines_CollectionChanged;
                    SetValue(LinesProperty, value);
                }

                cv.InvalidateVisual();
            }
        }

        private void Lines_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (StructLine obj in e.NewItems)
                {
                    cv.ListLine.Add(obj);
                }
                cv.InvalidateVisual();
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                foreach (StructLine obj in e.NewItems)
                {
                    cv.ListLine.Remove(obj);
                }
                cv.InvalidateVisual();
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                cv.ListLine.Clear();
                cv.InvalidateVisual();
            }
        }

        public static readonly DependencyProperty LinesProperty = DependencyProperty.Register(
            "Lines",
            typeof(ObservableCollection<StructLine>),
            typeof(HDisplay),
            new PropertyMetadata(OnLinesChanged));

        static void OnLinesChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (LinesChanged != null)
            {
                LinesChanged(obj);
            }
        }

        public delegate void LinesChangeHandler(object sender);
        public static LinesChangeHandler LinesChanged = delegate { };

        public void Clear()
        {
            cv.ListPoint.Clear();
            cv.ListLabel.Clear();
            cv.ListLine.Clear();
            cv.ListRectangle.Clear();
            cv.InvalidateVisual();
        }

        public void DrawDrawPoints()
        {
            cv.ListPoint.Clear();
            cv.ListLabel.Clear();
            cv.ListLine.Clear();
            cv.ListRectangle.Clear();

            cv.InvalidateVisual();

            if (Result != null)
            {
                DrawManager drawManager = Result.GetDrawManager();
                if (drawManager != null)
                {
                    drawManager.DrawPoints.ToList().ForEach(x =>
                    {
                        DrawPoint("", new System.Drawing.Point((int)x.X, (int)x.Y), x.Size, x.StrokeColor, x.ToolTip);
                    });

                    drawManager.DrawLabels.ToList().ForEach(x =>
                    {
                        DrawLabel(
                                "",
                                new System.Drawing.Point((int)x.X, (int)x.Y),
                                new FormattedText(x.Text, CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, new Typeface("돋움"), x.Size, x.Foreground)
                                , x.TextAlign);
                    });

                    drawManager.DrawLines.ToList().ForEach(x =>
                    {
                        DrawLine("",
                            new Pen(x.StrokeColor, x.Size),
                            new System.Drawing.Point((int)x.StartX, (int)x.StartY),
                            new System.Drawing.Point((int)x.EndX, (int)x.EndY));
                    });

                    drawManager.DrawCross.ToList().ForEach(x =>
                    {
                        DrawLine("",
                              new Pen(x.StrokeColor, x.Size),
                              new System.Drawing.Point((int)(x.X - x.Size * 3), (int)(x.Y)),
                              new System.Drawing.Point((int)(x.X + x.Size * 3), (int)(x.Y)));

                        DrawLine("",
                            new Pen(x.StrokeColor, x.Size),
                            new System.Drawing.Point((int)x.X, (int)(x.Y - x.Size * 3)),
                            new System.Drawing.Point((int)x.X, (int)(x.Y + x.Size * 3)));
                    });

                    drawManager.DrawRectangle.ToList().ForEach(x =>
                    {
                        DrawRectangle("",
                            x.Fill,
                            new Pen(x.StrokeColor, x.Size),
                            x.CenterX,
                            x.CenterY,
                            x.Height,
                            x.Width
                            );
                    });
                }
            }
        }

        public HCanvas canvas { get { return cv; } }

        public HDisplay()
        {
            InitializeComponent();

            sp.DataContext = this;

            BitmapImageChanged += new BitmapImageChangeHandler((object sender) =>
            {
                if (this == ((HDisplay)sender))
                {
                    BitmapImage = this.BitmapImage;
                }
            });

            ResultChanged += new ResultChangeHandler((object sender) =>
            {
                if (this == ((HDisplay)sender))
                {
                    Result = this.Result;
                }
            });

            RectanglesChanged += new RectanglesChangeHandler((object sender) =>
            {
                if (this == ((HDisplay)sender))
                {
                    Rectangles = this.Rectangles;
                }
            });

            LinesChanged += new LinesChangeHandler((object sender) =>
            {
                if (this == ((HDisplay)sender))
                {
                    Lines = this.Lines;
                }
            });

            IsShowRectangleChanged += new IsShowRectangleChangeHandler((sender, args) =>
            {
                if (sender == this)
                {
                    if ((bool)args.NewValue)
                    {
                        ConfirmButtonVisibility = Visibility.Visible;
                        CancelButtonVisibility = Visibility.Visible;
                        //ShowRectangle();
                    }
                    else if (!(bool)args.NewValue)
                    {
                        ConfirmButtonVisibility = Visibility.Collapsed;
                        CancelButtonVisibility = Visibility.Collapsed;
                        //RemoveSelectRectangle();
                    }
                }
            });

            IsShowCircleChanged += new IsShowCircleChangeHandler((sender, args) =>
            {
                if (sender == this)
                {
                    if ((bool)args.NewValue)
                    {
                        ShowCircle();
                    }
                    else
                    {
                        RemoveSelectRectangle();
                    }
                }
            });


            /*
            ConfirmedChanged += new ConfirmedHandler((sender, args) =>
            {
                if(sender == this)
                {
                    RemoveSelectRectangle();
                    ConfirmButtonVisibility = Visibility.Collapsed;
                }
            });*/
        }

        public void RemoveSelectRectangle()
        {
            cv.RemoveSelectRectangle();
        }

        public void RemoveSelectCircle()
        {
            cv.RemoveSelectCircle();
        }

        public void LoadImagePath(string path)
        {
            try
            {
                BitmapImage bi = new BitmapImage();
                Mat mat = Cv2.ImRead(path);
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                // bi.UriSource = new Uri(imageInfo.FileName);
                bi.StreamSource = mat.ToMemoryStream();
                bi.EndInit();
                mat.Dispose();
                mat = null;
                // BitmapImage = new BitmapImage(new Uri(path));
                BitmapImage = bi;
            }
            catch (Exception e)
            {

            }
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //cv.Fit();
        }


        public HRectangle ShowRectangle()
        {
            if (BitmapImage != null)
            {
                cv.RemoveSelectRectangle();
                if (SelectRectangle == null)
                {
                    IsShowRectangle = true;

                    int width = (int)BitmapImage.PixelWidth / 5;
                    int height = (int)BitmapImage.PixelHeight / 5;
                    int x = (int)((BitmapImage.PixelWidth / 2) - (width / 2)) * -1;
                    int y = (int)((BitmapImage.PixelHeight / 2) - (height / 2)) * -1;
                    SelectRectangle = new HRectangle(x, y, width, height, cv);
                }

                cv.Children.Add(SelectRectangle);

                return SelectRectangle;
            }
            else
            {
                return null;
            }
        }

        public HRectangle ShowRectangle(int x, int y, int width, int height)
        {
            //cv.RemoveSelectRectangle();
            HRectangle rec = new HRectangle(x * -1, y * -1, width, height, cv);

            rec.bolderColor = Brushes.Blue;

            rec.LabeledCharName = "?";

            cv.Children.Add(rec);
            SelectRectangle = rec;

            IsShowRectangle = true;

            //다시 그리기
            cv.InvalidateVisual();

            return rec;
        }

        public HRectangle ShowRectangle(int x, int y, int width, int height, string name, SolidColorBrush sb)
        {
            //cv.RemoveSelectRectangle();
            HRectangle rec = new HRectangle(x * -1, y * -1, width, height, name, cv);

            rec.bolderColor = sb;

            cv.Children.Add(rec);
            SelectRectangle = rec;

            IsShowRectangle = true;

            //다시 그리기
            cv.InvalidateVisual();

            return rec;
        }

        public HRectangle ShowRectangle(double x, double y, double width, double height, string name, SolidColorBrush sb)
        {
            HRectangle rec = new HRectangle(x * -1, y * -1, width, height, name, cv);

            rec.bolderColor = sb;

            cv.Children.Add(rec);
            SelectRectangle = rec;

            IsShowRectangle = true;

            //다시 그리기
            cv.InvalidateVisual();

            return rec;
        }

        public HCircle ShowCircle()
        {
            cv.RemoveSelectRectangle();
            if (SelectCircle == null)
            {
                int width = (int)BitmapImage.PixelWidth / 5;
                int height = width;
                int x = (int)((BitmapImage.PixelWidth / 2) - (width / 2)) * -1;
                int y = (int)((BitmapImage.PixelHeight / 2) - (height / 2)) * -1;
                SelectCircle = new HCircle(x, y, width, height, cv);
            }

            cv.Children.Add(SelectCircle);

            return SelectCircle;
        }

        public void ShowOK()
        {
            cv.RemoveSelectRectangle();

            TextBlock textBlock = new TextBlock();
            textBlock.Background = Brushes.Black;
            textBlock.FontSize = this.ActualHeight / 10;
            textBlock.Foreground = Brushes.LightGreen;
            textBlock.Text = "OK";
            textBlock.TextAlignment = TextAlignment.Center;

            cv.Children.Add(textBlock);

            textBlock.Loaded += (s, e) =>
            {
                textBlock.Margin = new Thickness(cv.ActualWidth - textBlock.ActualWidth, 0, 0, 0);
            };
        }


        public void ShowNG()
        {
            cv.RemoveSelectRectangle();

            TextBlock textBlock = new TextBlock();
            textBlock.Background = Brushes.Black;
            textBlock.FontSize = this.ActualHeight / 10;
            textBlock.Foreground = Brushes.Red;
            textBlock.Text = "NG";
            textBlock.TextAlignment = TextAlignment.Center;

            cv.Children.Add(textBlock);

            textBlock.Loaded += (s, e) =>
            {
                textBlock.Margin = new Thickness(cv.ActualWidth - textBlock.ActualWidth, 0, 0, 0);
            };
        }

        public void ClearComment()
        {
            cv.sp_Comment.Children.Clear();
        }

        public void ShowComment(string str, int left, int top, int right, int bottom)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Background = Brushes.Black;
            textBlock.FontSize = this.ActualHeight / 10;
            textBlock.Foreground = Brushes.White;
            textBlock.Text = str;
            textBlock.TextAlignment = TextAlignment.Center;

            cv.Children.Add(textBlock);
        }

        public void DrawPoint(string tag, System.Drawing.Point realPosition, Ellipse ellipse)
        {
            canvas.ListPoint.Add(new StructEllipse() { ellipse = ellipse, RealPosition = realPosition });
            canvas.InvalidateVisual();
        }

        public void DrawPoint(string tag, System.Drawing.Point realPosition, double size, Brush brush, string toolTip)
        {
            Ellipse ellipse = new Ellipse();
            ellipse.ToolTip = toolTip;
            ellipse.Height = size;
            ellipse.Width = size;
            ellipse.Stroke = brush;
            ellipse.StrokeThickness = 1;
            canvas.ListPoint.Add(new StructEllipse() { ellipse = ellipse, RealPosition = realPosition });
            canvas.InvalidateVisual();
        }

        public void DrawLabel(string tag, System.Drawing.Point realPosition, FormattedText text, DrawLabel.DrawLabelAlign align)
        {
            canvas.ListLabel.Add(new StructLabel() { Tag = tag, RealPosition = realPosition, Text = text, DrawLabelAlign = align });
            canvas.InvalidateVisual();
        }

        public void DrawLine(string tag, Pen pen, System.Drawing.Point point1, System.Drawing.Point point2)
        {
            canvas.ListLine.Add(new StructLine() { Tag = tag, Point1 = point1, Point2 = point2, Pen = pen });
            canvas.InvalidateVisual();
        }

        public void DrawRectangle(string tag, SolidColorBrush brush, Pen pen, double X, double Y, double Width, double Height)
        {
            canvas.ListRectangle.Add(new StructRectangle() { Tag = tag, Brush = brush, Pen = pen, X = X, Y = Y, Width = Width, Height = Height });
            canvas.InvalidateVisual();
        }

        public void SaveImage(string filePath)
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(BitmapImage));

            using (var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                encoder.Save(fileStream);
            }
        }

        private void Cv_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                OnDoubleClickEvent();
            }
        }

        private void Btn_Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Saved.Execute(null);
            }
            catch
            {

            }
        }

        private void Btn_Confirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Confirmed.Execute(null);
                // SelectRectangle = null;
                // canvas.SelectedElement = null;
            }
            catch (Exception ex)
            {

            }
        }

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectRectangle();
            IsShowRectangle = false;
            ConfirmButtonVisibility = Visibility.Collapsed;
            CancelButtonVisibility = Visibility.Collapsed;

            try
            {
                Canceled.Execute(null);
                SelectRectangle = null;
                canvas.SelectedElement = null;
            }
            catch (Exception ex)
            {

            }
        }

        private void Btn_Delete_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectRectangle();
            IsShowRectangle = false;
            ConfirmButtonVisibility = Visibility.Collapsed;
            CancelButtonVisibility = Visibility.Collapsed;

            try
            {
                if (Deleted != null)
                {
                    Deleted.Execute(null);
                    SelectRectangle = null;
                    canvas.SelectedElement = null;
                }
            }
            catch (Exception ex)
            {

            }
        }


        //Result
        public IHResult Result
        {
            get { return (IHResult)GetValue(ResultProperty); }
            set
            {
                SetValue(ResultProperty, value);

                DrawDrawPoints();
            }
        }
        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
            "Result",
            typeof(IHResult),
            typeof(HDisplay),
            new PropertyMetadata(OnResultChanged));

        static void OnResultChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (ResultChanged != null)
            {
                ResultChanged(obj);
            }
        }


        public delegate void ResultChangeHandler(object sender);
        public static ResultChangeHandler ResultChanged = delegate { };

        //사각형 표시
        public bool IsShowRectangle
        {
            get { return (bool)GetValue(IsShowRectangleProperty); }
            set
            {
                SetValue(IsShowRectangleProperty, value);
            }
        }

        public static readonly DependencyProperty IsShowRectangleProperty = DependencyProperty.Register(
            "IsShowRectangle",
            typeof(bool),
            typeof(HDisplay),
            new PropertyMetadata(OnIsShowRectangleChanged)
            );

        static void OnIsShowRectangleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (IsShowRectangleChanged != null)
            {
                IsShowRectangleChanged(obj, args);
            }
        }

        public delegate void IsShowRectangleChangeHandler(object sender, DependencyPropertyChangedEventArgs args);
        public static IsShowRectangleChangeHandler IsShowRectangleChanged = delegate { };

        //원 표시
        public bool IsShowCircle
        {
            get { return (bool)GetValue(IsShowCircleProperty); }
            set
            {
                SetValue(IsShowCircleProperty, value);
            }
        }

        public static readonly DependencyProperty IsShowCircleProperty = DependencyProperty.Register(
            "IsShowCircle",
            typeof(bool),
            typeof(HDisplay),
            new PropertyMetadata(OnIsShowCircleChanged)
            );

        static void OnIsShowCircleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (IsShowCircleChanged != null)
            {
                IsShowCircleChanged(obj, args);
            }
        }

        public delegate void IsShowCircleChangeHandler(object sender, DependencyPropertyChangedEventArgs args);
        public static IsShowCircleChangeHandler IsShowCircleChanged = delegate { };

        public ICommand Saved
        {
            get { return (ICommand)GetValue(SavedProperty); }
            set
            {
                SetValue(SavedProperty, value);
            }
        }

        public static readonly DependencyProperty SavedProperty = DependencyProperty.Register(
            "Saved",
            typeof(ICommand),
            typeof(HDisplay),
            new PropertyMetadata(OnSavedChanged)
            );

        static void OnSavedChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (SavedChanged != null)
            {
                SavedChanged(obj, args);
            }
        }

        public delegate void SavedHandler(object sender, DependencyPropertyChangedEventArgs args);
        public static SavedHandler SavedChanged = delegate { };

        //Confirm

        public ICommand Confirmed
        {
            get { return (ICommand)GetValue(ConfirmedProperty); }
            set
            {
                SetValue(ConfirmedProperty, value);
            }
        }

        public static readonly DependencyProperty ConfirmedProperty = DependencyProperty.Register(
            "Confirmed",
            typeof(ICommand),
            typeof(HDisplay),
            new PropertyMetadata(OnConfirmedChanged)
            );

        static void OnConfirmedChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (ConfirmedChanged != null)
            {
                ConfirmedChanged(obj, args);
            }
        }

        public delegate void ConfirmedHandler(object sender, DependencyPropertyChangedEventArgs args);
        public static ConfirmedHandler ConfirmedChanged = delegate { };

        public ICommand Canceled
        {
            get { return (ICommand)GetValue(CanceledProperty); }
            set
            {
                SetValue(CanceledProperty, value);
            }
        }

        public static readonly DependencyProperty CanceledProperty = DependencyProperty.Register(
            "Canceled",
            typeof(ICommand),
            typeof(HDisplay),
            new PropertyMetadata(OnCanceledChanged)
            );

        static void OnCanceledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (CanceledChanged != null)
            {
                CanceledChanged(obj, args);
            }
        }

        public delegate void CanceledHandler(object sender, DependencyPropertyChangedEventArgs args);
        public static CanceledHandler CanceledChanged = delegate { };

        public ICommand Deleted
        {
            get { return (ICommand)GetValue(DeletedProperty); }
            set
            {
                SetValue(DeletedProperty, value);
            }
        }

        public static readonly DependencyProperty DeletedProperty = DependencyProperty.Register(
            "Deleted",
            typeof(ICommand),
            typeof(HDisplay),
            new PropertyMetadata(OnDeletedChanged)
            );

        static void OnDeletedChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (DeletedChanged != null)
            {
                DeletedChanged(obj, args);
            }
        }

        public delegate void DeletedHandler(object sender, DependencyPropertyChangedEventArgs args);
        public static CanceledHandler DeletedChanged = delegate { };

        //
        //

        public HRectangle SelectRectangle
        {
            get { return (HRectangle)GetValue(SelectRectangleProperty); }
            set
            {
                SetValue(SelectRectangleProperty, value);
            }
        }

        public static readonly DependencyProperty SelectRectangleProperty = DependencyProperty.Register(
            "SelectRectangle",
            typeof(HRectangle),
            typeof(HDisplay),
            new PropertyMetadata(OnSelectRectangleChanged)
            );

        static void OnSelectRectangleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (SelectRectangleChanged != null)
            {
                SelectRectangleChanged(obj, args);
            }
        }

        public delegate void SelectRectangleHandler(object sender, DependencyPropertyChangedEventArgs args);
        public static SelectRectangleHandler SelectRectangleChanged = delegate { };

        public ObservableCollection<HRectangle> SelectRectangles
        {
            get { return (ObservableCollection<HRectangle>)GetValue(SelectRectanglesProperty); }
            set
            {
                SetValue(SelectRectanglesProperty, value);
            }
        }

        public static readonly DependencyProperty SelectRectanglesProperty = DependencyProperty.Register(
            "SelectRectangles",
            typeof(ObservableCollection<HRectangle>),
            typeof(HDisplay),
            new PropertyMetadata(OnSelectRectanglesChanged)
            );

        static void OnSelectRectanglesChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (SelectRectanglesChanged != null)
            {
                SelectRectanglesChanged(obj, args);
            }
        }

        public delegate void SelectRectanglesHandler(object sender, DependencyPropertyChangedEventArgs args);
        public static SelectRectanglesHandler SelectRectanglesChanged = delegate { };

        //Circle
        public HCircle SelectCircle
        {
            get { return (HCircle)GetValue(SelectCircleProperty); }
            set
            {
                SetValue(SelectCircleProperty, value);
            }
        }

        public static readonly DependencyProperty SelectCircleProperty = DependencyProperty.Register(
            "SelectCircle",
            typeof(HCircle),
            typeof(HDisplay),
            new PropertyMetadata(OnSelectCircleChanged)
            );

        static void OnSelectCircleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (SelectCircleChanged != null)
            {
                SelectCircleChanged(obj, args);
            }
        }

        public delegate void SelectCircleHandler(object sender, DependencyPropertyChangedEventArgs args);
        public static SelectCircleHandler SelectCircleChanged = delegate { };

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Dp_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            int delta = e.Delta;

            if (SelectRectangle != null)
            {
                double startX = SelectRectangle.StartX;
                double startY = SelectRectangle.StartY;
                double endX = SelectRectangle.StartX + SelectRectangle.originWidth;
                double endY = SelectRectangle.StartY + SelectRectangle.originHeight;
                System.Windows.Point pt = cv.RealPoint;
                double curX = pt.X;
                double curY = pt.Y;

                if (startX <= curX && endX >= curX && startY <= curY && endY >= curY)
                {
                    cv.IsMeddled = true;
                    cv.SelectedElement = SelectRectangle;
                    int originWidth = SelectRectangle.originWidth;
                    int originHeight = SelectRectangle.originHeight;

                    if (delta > 0)
                    {
                        SelectRectangle.originWidth = Convert.ToInt32(SelectRectangle.originWidth * 0.9);
                        SelectRectangle.originHeight = Convert.ToInt32(SelectRectangle.originHeight * 0.9);
                        SelectRectangle.MoveXValue -= Math.Abs(SelectRectangle.originWidth - originWidth) / 2;
                        SelectRectangle.MoveYValue -= Math.Abs(SelectRectangle.originHeight - originHeight) / 2;
                    }
                    else
                    {
                        SelectRectangle.originWidth = Convert.ToInt32(SelectRectangle.originWidth * 1.1);
                        SelectRectangle.originHeight = Convert.ToInt32(SelectRectangle.originHeight * 1.1);
                        SelectRectangle.MoveXValue += Math.Abs(SelectRectangle.originWidth - originWidth) / 2;
                        SelectRectangle.MoveYValue += Math.Abs(SelectRectangle.originHeight - originHeight) / 2;
                    }
                }
            }
        }

        private void Dp_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            
        }
        
        private void Dp_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            
        }

        public Visibility SaveBtnVisibility
        {
            get { return (Visibility)GetValue(SaveBtnVisibilityProperty); }
            set
            {
                SetValue(SaveBtnVisibilityProperty, value);
            }
        }

        public static readonly DependencyProperty SaveBtnVisibilityProperty = DependencyProperty.Register(
            "SaveBtnVisibility",
            typeof(Visibility),
            typeof(HDisplay),
            new PropertyMetadata(OnSaveBtnVisibilityChanged)
            );

        static void OnSaveBtnVisibilityChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (SaveBtnVisibilityChanged != null)
            {
                SaveBtnVisibilityChanged(obj, args);
            }
        }

        public delegate void SaveBtnVisibilityHandler(object sender, DependencyPropertyChangedEventArgs args);
        public static SaveBtnVisibilityHandler SaveBtnVisibilityChanged = delegate { };

        public Visibility AdjustBrightnessVisibility
        {
            get { return (Visibility)GetValue(AdjustBrightnessProperty); }
            set
            {
                SetValue(AdjustBrightnessProperty, value);
            }
        }

        public static readonly DependencyProperty AdjustBrightnessProperty = DependencyProperty.Register(
            "AdjustBrightnessVisibility",
            typeof(Visibility),
            typeof(HDisplay),
            new PropertyMetadata(OnAdjustBrightnessChanged)
            );

        static void OnAdjustBrightnessChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (AdjustBrightnessChanged != null)
            {
                AdjustBrightnessChanged(obj, args);
            }
        }

        public delegate void AdjustBrightnessHandler(object sender, DependencyPropertyChangedEventArgs args);
        public static AdjustBrightnessHandler AdjustBrightnessChanged = delegate { };
    }
}