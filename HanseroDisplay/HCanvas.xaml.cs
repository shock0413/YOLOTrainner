using HanseroDisplay.Struct;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace HanseroDisplay
{
    /// <summary>
    /// HCanvas.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class HCanvas : Canvas
    {
        public delegate void OnRectangleMoveEventHandler();
        public event OnRectangleMoveEventHandler OnRectangleMoveEvent = delegate { };

        public delegate void OnCircleMoveEventHandler();
        public event OnCircleMoveEventHandler OnCircleMoveEvent = delegate { };

        public Point startPoint;

        private Point currentPoint;
        public Point RealPoint;

        public BitmapSource Bitmap;
        public double BitmapWidth = 0;
        public double BitmapHeight = 0;

        public Point margin = new Point(0, 0);

        private double zoom = 1;
        private Double zoomSpeed = 0.001;

        public bool StopMove = false;

        public UIElement SelectedElement;

        public StackPanel sp_Comment;

        public double drawWidth;
        public double drawHeight;

        public double startX;
        public double startY;

        private bool isMouseDown = false;

        public List<StructEllipse> ListPoint = new List<StructEllipse>();
        public List<StructLabel> ListLabel = new List<StructLabel>();
        public List<StructLine> ListLine = new List<StructLine>();
        public List<StructRectangle> ListRectangle = new List<StructRectangle>();

        public bool m_IsSettingBrightness = false;

        public HCanvas()
        {
            MouseMove += HCanvas_MouseMove;
            MouseLeftButtonDown += HCanvas_MouseLeftButtonDown;
            MouseLeftButtonUp += HCanvas_MouseLeftButtonUp;
            MouseWheel += HCanvas_MouseWheel;
            MouseLeave += HCanvas_MouseLeave;

            SizeChanged += HCanvas_SizeChanged;

            this.ClipToBounds = true;

            CreateContextMenu();

            sp_Comment = new StackPanel();
            sp_Comment.HorizontalAlignment = HorizontalAlignment.Right;
            this.Children.Add(sp_Comment);
        }



        private void HCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Fit();
        }

        private void CreateContextMenu()
        {
            this.ContextMenu = new ContextMenu();
            MenuItem menuItem = new MenuItem();
            menuItem.Header = "이미지 맞추기";
            menuItem.Click += MenuItem_Fit_Click;

            ContextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "이미지 왼쪽으로 90도 회전";
            menuItem.Click += MenuItem_Left_Rotation_90_Click;

            ContextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "이미지 오른쪽으로 90도 회전";
            menuItem.Click += MenuItem_Right_Rotation_90_Click;

            ContextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "이미지 저장";
            menuItem.Click += MenuItem_Save_Click; ;

            ContextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "레이블 적용";
            menuItem.Click += OnMenuItem_Label_Click;

            ContextMenu.Items.Add(menuItem);
        }

        public event RoutedEventHandler OnMenuItem_Label_Click = delegate { };        

        private void MenuItem_Save_Click(object sender, RoutedEventArgs e)
        {
            /*
            SaveFileDialog dialog = new SaveFileDialog();
            if (dialog.ShowDialog() == true)
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(Bitmap));

                using (var fileStream = new System.IO.FileStream(dialog.FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    encoder.Save(fileStream);
                }
            }
            */

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Bitmap(*.bmp)|*.bmp";
            if (dialog.ShowDialog() == true)
            {

                RenderTargetBitmap bitmap = new RenderTargetBitmap((int)this.ActualWidth, (int)this.ActualHeight, 96d, 96d, PixelFormats.Pbgra32);
                bitmap.Render(this);

                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (var fileStream = new System.IO.FileStream(dialog.FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    encoder.Save(fileStream);
                }
            }
        }

        private void MenuItem_Fit_Click(object sender, RoutedEventArgs e)
        {
            Fit();
        }

        public void Fit()
        {
            try
            {
                if (Bitmap != null)
                {
                    BeginInit();
                    double widthPer = (this.ActualWidth / Bitmap.PixelWidth);
                    double heightPer = (this.ActualHeight / Bitmap.PixelHeight);

                    if (widthPer > heightPer)
                    {
                        zoom = heightPer;

                        margin.X = (this.ActualWidth - (Bitmap.PixelWidth * zoom)) / 2;
                        margin.Y = 0;
                    }
                    else
                    {
                        zoom = widthPer;

                        margin.X = 0;
                        margin.Y = (this.ActualHeight - (Bitmap.PixelHeight * zoom)) / 2;
                    }

                    EndInit();

                    InvalidateVisual();
                }
            }
            catch (Exception e)
            {

            }
        }

        public ICommand LeftRotation90 = null;
        public ICommand RightRotation90 = null;

        private void MenuItem_Left_Rotation_90_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LeftRotation90.Execute(null);
            }
            catch
            {

            }
        }

        private void MenuItem_Right_Rotation_90_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RightRotation90.Execute(null);
            }
            catch
            {

            }
        }

        public void RemoveSelectRectangle()
        {
            Children.Clear();
            InvalidateVisual();
        }

        public void RemoveSelectCircle()
        {
            Children.Clear();
            InvalidateVisual();
        }

        public bool IsMeddled = false;

        private void HCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (m_IsSettingBrightness)
            {
                m_IsSettingBrightness = false;
                return;
            }

            if (SelectedElement == null || !IsMeddled)
            {
                double beforeXPosition = 0;
                double beforeYPosition = 0;
                double beforeWidth = 0;
                double beforeHeight = 0;
                double widthPer = 0;
                double heightPer = 0;
                double beforeZoom = 0;
                Point mousePos = e.GetPosition(this);
                if (Bitmap != null)
                {
                    /*
                    beforeZoom = zoom;
                    beforeWidth = Bitmap.PixelWidth * beforeZoom;
                    beforeHeight = Bitmap.PixelHeight * beforeZoom;
                    beforeXPosition = this.ActualWidth / 2 - beforeWidth / 2 + margin.X;
                    beforeYPosition = this.ActualHeight / 2 - beforeHeight / 2 + margin.Y;

                    widthPer = (((mousePos.X - margin.X) - beforeXPosition) / beforeWidth);
                    heightPer = (((mousePos.Y - margin.Y) - beforeYPosition) / beforeHeight);
                    */
                    /*
                    beforeZoom = zoom;
                    beforeWidth = Bitmap.PixelWidth * beforeZoom;
                    beforeHeight = Bitmap.PixelHeight * beforeZoom;

                    widthPer = (mousePos.X - margin.X) / (beforeWidth - margin.X);
                    heightPer = (mousePos.Y - margin.Y) / (beforeHeight - margin.Y);

                    Console.WriteLine("HeightPer = " + Math.Round(heightPer * 100) + "\t WidthPer = " + Math.Round(widthPer * 100));
                    */
                }

                zoom += zoomSpeed * (e.Delta / 5);

                /*
                if (Bitmap != null)
                {
                    double afterWidth = Bitmap.PixelWidth * zoom;
                    double differenceWidth = (afterWidth - beforeWidth) / 2;
                    double minusWidth = differenceWidth * widthPer;

                    margin.X = margin.X + minusWidth;

                    double afterHeight = Bitmap.PixelHeight * zoom;
                    double differenceHeight = (afterHeight - beforeHeight) / 2;
                    double minusHeight = differenceHeight * heightPer;

                    margin.Y = margin.Y + minusHeight;

                    //Console.WriteLine("marginX = " + Math.Round(margin.X) + "\t MarginY = " + Math.Round(margin.Y));
                }
                */
            }

            if (IsMeddled)
            {
                IsMeddled = false;
            }

            this.InvalidateVisual();
        }

        private void HCanvas_MouseLeave(object sender, EventArgs e)
        {
            if (isMouseDown)
            {
                if (SelectedElement != null)
                {
                    if (SelectedElement.GetType() == typeof(HRectangle))
                    {
                        HRectangle rec = (HRectangle)SelectedElement;

                        int temp = rec.Index;

                        double m_StartX = rec.StartX;
                        double m_StartY = rec.StartY;
                        double m_EndX = rec.StartX + rec.SelectedWidth;
                        double m_EndY = rec.StartY + rec.SelectedHeight;

                        if (m_StartX < 0)
                        {
                            rec.LeftMoveValue = rec.LeftMoveValue + m_StartX;
                        }

                        if (m_StartY < 0)
                        {
                            rec.TopMoveValue = rec.TopMoveValue + m_StartY;
                        }

                        if (m_EndX > BitmapWidth)
                        {
                            double diff = m_EndX - BitmapWidth;
                            rec.originWidth = Convert.ToInt32(rec.originWidth - diff);
                        }

                        if (m_EndY > BitmapHeight)
                        {
                            double diff = m_EndY - BitmapHeight;
                            rec.originHeight = Convert.ToInt32(rec.originHeight - diff);
                        }
                    }

                    isMouseDown = false;
                    StopMove = false;
                }

                this.InvalidateVisual();
            }
        }

        private void HCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = false;
            StopMove = false;

            if (e.LeftButton == MouseButtonState.Released)
            {
                if (SelectedElement != null)
                {
                    if (SelectedElement.GetType() == typeof(HRectangle))
                    {
                        HRectangle rec = (HRectangle)SelectedElement;

                        int temp = rec.Index;

                        double m_StartX = rec.StartX;
                        double m_StartY = rec.StartY;
                        double m_EndX = rec.StartX + rec.SelectedWidth;
                        double m_EndY = rec.StartY + rec.SelectedHeight;

                        if (m_StartX < 0)
                        {
                            rec.LeftMoveValue = rec.LeftMoveValue + m_StartX;
                        }

                        if (m_StartY < 0)
                        {
                            rec.TopMoveValue = rec.TopMoveValue + m_StartY;
                        }

                        if (m_EndX > BitmapWidth)
                        {
                            double diff = m_EndX - BitmapWidth;
                            rec.originWidth = Convert.ToInt32(rec.originWidth - diff);
                        }

                        if (m_EndY > BitmapHeight)
                        {
                            double diff = m_EndY - BitmapHeight;
                            rec.originHeight = Convert.ToInt32(rec.originHeight - diff);
                        }
                    }
                }
            }

            this.InvalidateVisual();
        }

        private void HCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(this);
            isMouseDown = true;
        }

        private void HCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            currentPoint = e.GetPosition(this);
            RealPoint = new Point((currentPoint.X - margin.X)/ zoom, (currentPoint.Y - margin.Y) / zoom);

            if (!isMouseDown)
            {
                return;
            }

            if (!StopMove)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    margin.X = margin.X + currentPoint.X - startPoint.X;
                    margin.Y = margin.Y + currentPoint.Y - startPoint.Y;

                    this.InvalidateVisual();

                    startPoint = currentPoint;
                }
            }
            else
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    if (SelectedElement != null)
                    {
                        if (SelectedElement.GetType() == typeof(HRectangle))
                        {
                            HRectangle rec = (SelectedElement as HRectangle);

                            int temp = rec.Index;

                            if (rec.Mode == HRectangle.RectangleModeConstant.MOVE_TOP)
                            {
                                double m_StartY = rec.StartY;

                                if (m_StartY < 0)
                                {
                                    rec.TopMoveValue = rec.TopMoveValue + m_StartY;
                                }

                                rec.TopMoveValue = (rec.TopMoveValue + (startPoint.Y - currentPoint.Y) / zoom);
                            }
                            else if (rec.Mode == HRectangle.RectangleModeConstant.MOVE)
                            {
                                double m_StartX = rec.StartX;
                                double m_StartY = rec.StartY;
                                double m_EndX = rec.StartX + rec.SelectedWidth;
                                double m_EndY = rec.StartY + rec.SelectedHeight;
                                bool m_IsOver = false;

                                if (m_StartX < 0)
                                {
                                    rec.MoveXValue = rec.MoveXValue + m_StartX;
                                    m_IsOver = true;
                                }
                                
                                if (m_StartY < 0)
                                {
                                    rec.MoveYValue = rec.MoveYValue + m_StartY;
                                    m_IsOver = true;
                                }

                                if (m_EndX > BitmapWidth)
                                {
                                    double diff = m_EndX - BitmapWidth;
                                    rec.MoveXValue = rec.MoveXValue + diff;
                                    m_IsOver = true;
                                }

                                if (m_EndY > BitmapHeight)
                                {
                                    double diff = m_EndY - BitmapHeight;
                                    rec.MoveYValue = rec.MoveYValue + diff;
                                    m_IsOver = true;
                                }

                                if (!m_IsOver)
                                {
                                    rec.MoveXValue = (rec.MoveXValue + (startPoint.X - currentPoint.X) / zoom);
                                    rec.MoveYValue = (rec.MoveYValue + (startPoint.Y - currentPoint.Y) / zoom);
                                }
                            }
                            else if (rec.Mode == HRectangle.RectangleModeConstant.MOVE_BOTTOM)
                            {
                                double m_EndY = rec.StartY + (rec.Height / zoom);

                                if (m_EndY > BitmapHeight)
                                {
                                    double diff = m_EndY - BitmapHeight;
                                    rec.BottomMoveValue = rec.BottomMoveValue - diff;
                                }

                                rec.BottomMoveValue = (rec.BottomMoveValue + (currentPoint.Y - startPoint.Y) / zoom);
                            }
                            else if (rec.Mode == HRectangle.RectangleModeConstant.MOVE_RIGHT)
                            {
                                double m_EndX = rec.StartX + (rec.Width / zoom);

                                if (m_EndX > BitmapWidth)
                                {
                                    double diff = m_EndX - BitmapWidth;
                                    rec.RightMoveValue = rec.RightMoveValue - diff;
                                }

                                rec.RightMoveValue = (rec.RightMoveValue + (currentPoint.X - startPoint.X) / zoom);
                            }
                            else if (rec.Mode == HRectangle.RectangleModeConstant.MOVE_LEFT)
                            {
                                double m_StartX = rec.StartX;

                                if (m_StartX < 0)
                                {
                                    rec.LeftMoveValue = rec.LeftMoveValue + m_StartX;
                                }

                                rec.LeftMoveValue = (rec.LeftMoveValue + (startPoint.X - currentPoint.X) / zoom);
                            }
                            else if (rec.Mode == HRectangle.RectangleModeConstant.MOVE_BOTTOM_RIGHT)
                            {
                                double m_EndX = rec.StartX + (rec.Width / zoom);
                                double m_EndY = rec.StartY + (rec.Height / zoom);

                                if (m_EndX > BitmapWidth)
                                {
                                    double diff = m_EndX - BitmapWidth;
                                    rec.RightMoveValue = rec.RightMoveValue - diff;
                                }

                                if (m_EndY > BitmapHeight)
                                {
                                    double diff = m_EndY - BitmapHeight;
                                    rec.BottomMoveValue = rec.BottomMoveValue - diff;
                                }

                                rec.RightMoveValue = (rec.RightMoveValue + (currentPoint.X - startPoint.X) / zoom);
                                rec.BottomMoveValue = (rec.BottomMoveValue + (currentPoint.Y - startPoint.Y) / zoom);
                            }
                            else if (rec.Mode == HRectangle.RectangleModeConstant.MOVE_BOTTOM_LEFT)
                            {
                                double m_StartX = rec.StartX;
                                double m_EndY = rec.StartY + (rec.Height / zoom);

                                if (m_StartX < 0)
                                {
                                    rec.LeftMoveValue = rec.LeftMoveValue + m_StartX;
                                }

                                if (m_EndY > BitmapHeight)
                                {
                                    double diff = m_EndY - BitmapHeight;
                                    rec.BottomMoveValue = rec.BottomMoveValue - diff;
                                }

                                rec.BottomMoveValue = (rec.BottomMoveValue + (currentPoint.Y - startPoint.Y) / zoom);
                                rec.LeftMoveValue = (rec.LeftMoveValue + (startPoint.X - currentPoint.X) / zoom);
                            }
                            else if (rec.Mode == HRectangle.RectangleModeConstant.MOVE_TOP_RIGHT)
                            {
                                double m_StartY = rec.StartY;
                                double m_EndX = rec.StartX + (rec.Width / zoom);

                                if (m_StartY < 0)
                                {
                                    rec.TopMoveValue = rec.TopMoveValue + m_StartY;
                                }

                                if (m_EndX > BitmapWidth)
                                {
                                    double diff = m_EndX - BitmapWidth;
                                    rec.RightMoveValue = rec.RightMoveValue - diff;
                                }

                                rec.TopMoveValue = (rec.TopMoveValue + (startPoint.Y - currentPoint.Y) / zoom);
                                rec.RightMoveValue = (rec.RightMoveValue + (currentPoint.X - startPoint.X) / zoom);
                            }
                            else if (rec.Mode == HRectangle.RectangleModeConstant.MOVE_TOP_LEFT)
                            {
                                double m_StartX = rec.StartX;
                                double m_StartY = rec.StartY;

                                if (m_StartX < 0)
                                {
                                    rec.LeftMoveValue = rec.LeftMoveValue + m_StartX;
                                }

                                if (m_StartY < 0)
                                {
                                    rec.TopMoveValue = rec.TopMoveValue + m_StartY;
                                }

                                rec.TopMoveValue = (rec.TopMoveValue + (startPoint.Y - currentPoint.Y) / zoom);
                                rec.LeftMoveValue = (rec.LeftMoveValue + (startPoint.X - currentPoint.X) / zoom);
                            }

                            OnRectangleMoveEvent();
                            this.InvalidateVisual();
                            startPoint = currentPoint;
                        }
                        else if (SelectedElement.GetType() == typeof(HCircle))
                        {
                            HCircle circle = (SelectedElement as HCircle);

                            if (circle.Mode == HCircle.CircleModeConstant.MOVE_TOP)
                            {
                                double moveValue = (startPoint.Y - currentPoint.Y) / zoom;
                                circle.TopMoveValue = (circle.TopMoveValue + moveValue);
                                circle.BottomMoveValue = (circle.BottomMoveValue + moveValue);
                                circle.LeftMoveValue = (circle.LeftMoveValue + moveValue);
                                circle.RightMoveValue = (circle.RightMoveValue + moveValue);
                            }
                            else if (circle.Mode == HCircle.CircleModeConstant.MOVE)
                            {
                                circle.MoveXValue = (circle.MoveXValue + (startPoint.X - currentPoint.X) / zoom);
                                circle.MoveYValue = (circle.MoveYValue + (startPoint.Y - currentPoint.Y) / zoom);
                            }
                            else if (circle.Mode == HCircle.CircleModeConstant.MOVE_BOTTOM)
                            {
                                double moveValue = (startPoint.Y - currentPoint.Y) / zoom;
                                circle.TopMoveValue = (circle.TopMoveValue - moveValue);
                                circle.BottomMoveValue = (circle.BottomMoveValue - moveValue);
                                circle.LeftMoveValue = (circle.LeftMoveValue - moveValue);
                                circle.RightMoveValue = (circle.RightMoveValue - moveValue);
                            }
                            else if (circle.Mode == HCircle.CircleModeConstant.MOVE_RIGHT)
                            {
                                double moveValue = (currentPoint.X - startPoint.X) / zoom;
                                circle.TopMoveValue = (circle.TopMoveValue + moveValue);
                                circle.BottomMoveValue = (circle.BottomMoveValue + moveValue);
                                circle.LeftMoveValue = (circle.LeftMoveValue + moveValue);
                                circle.RightMoveValue = (circle.RightMoveValue + moveValue);
                            }
                            else if (circle.Mode == HCircle.CircleModeConstant.MOVE_Left)
                            {
                                double moveValue = (currentPoint.X - startPoint.X) / zoom;
                                circle.TopMoveValue = (circle.TopMoveValue - moveValue);
                                circle.BottomMoveValue = (circle.BottomMoveValue - moveValue);
                                circle.LeftMoveValue = (circle.LeftMoveValue - moveValue);
                                circle.RightMoveValue = (circle.RightMoveValue - moveValue);
                            }

                            OnRectangleMoveEvent();
                            this.InvalidateVisual();
                            startPoint = currentPoint;
                        }
                    }
                }
                else
                {
                    SelectedElement = null;
                    StopMove = false;
                }
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            try
            {
                if (Bitmap != null)
                {
                    drawWidth = Bitmap.PixelWidth * zoom;
                    drawHeight = Bitmap.PixelHeight * zoom;

                    startX = margin.X;
                    startY = margin.Y;

                    if (drawWidth < 0)
                    {
                        drawWidth = 1;
                    }
                    if (drawHeight < 0)
                    {
                        drawHeight = 1;
                    }

                    dc.DrawImage(Bitmap, new Rect(startX, startY, drawWidth, drawHeight));

                    //포인트 표시
                    ListPoint.ForEach(x =>
                    {
                        Ellipse ellipse = new Ellipse();
                        double height = x.ellipse.Height * zoom;
                        double width = x.ellipse.Width * zoom;
                        if (height < 0)
                        {
                            height = 0;
                        }
                        if (width < 0)
                        {
                            width = 0;
                        }

                        ellipse.Height = height;
                        ellipse.Width = width;
                        ellipse.Fill = x.ellipse.Fill;
                        ellipse.StrokeThickness = x.ellipse.StrokeThickness;
                        ellipse.Stroke = x.ellipse.Stroke;
                        ellipse.ToolTip = x.ellipse.ToolTip;

                        Point point = new Point() { X = x.RealPosition.X * zoom + startX, Y = x.RealPosition.Y * zoom + startY };

                        dc.DrawEllipse(ellipse.Fill, new Pen(ellipse.Stroke, ellipse.StrokeThickness), point, ellipse.Width / 2, ellipse.Height / 2);
                    });

                    //라인 표시
                    ListLine.ForEach(x =>
                    {
                        double penSize = x.Pen.Thickness * zoom;
                        if (penSize <= 1)
                        {
                            penSize = 1;
                        }

                        dc.DrawLine(new Pen(x.Pen.Brush, penSize), new Point(x.Point1.X * zoom + startX, x.Point1.Y * zoom + startY), new Point(x.Point2.X * zoom + startX, x.Point2.Y * zoom + startY));
                    });

                    //글자 표시
                    ListLabel.ForEach(x =>
                    {
                        if (x.DrawLabelAlign == HCore.HDrawPoints.DrawLabel.DrawLabelAlign.LEFT)
                        {
                            dc.DrawText(x.Text, new Point(x.RealPosition.X * zoom + startX, x.RealPosition.Y * zoom + startY));
                        }
                        else if (x.DrawLabelAlign == HCore.HDrawPoints.DrawLabel.DrawLabelAlign.RIGHT)
                        {
                            dc.DrawText(x.Text, new Point(x.RealPosition.X * zoom + startX - x.Text.Width, x.RealPosition.Y * zoom + startY));
                        }

                    });

                    //사각형 표시
                    ListRectangle.ForEach(x =>
                    {
                        if (x.Tag == "Blank")
                        {

                        }

                        if (!x.IsHide)
                        {
                            double recStartX = x.X * zoom + startX;
                            double recStartY = x.Y * zoom + startY;

                            double width = x.Width * zoom;
                            double height = x.Height * zoom;
                            
                            /*
                            if (recStartX > 0)
                            {
                                recStartX = recStartX + 1;
                            }

                            if (recStartY > 0)
                            {
                                recStartY = recStartY + 1;
                            }

                            if (width > 0)
                            {
                                width = width - 1;
                            }

                            if (height > 0)
                            {
                                height = height - 1;
                            }
                            */

                            if (width > 0 && height > 0)
                            {
                                dc.DrawRectangle(x.Brush, x.Pen, new Rect(recStartX, recStartY, width, height));
                            }
                        }
                    });

                    Children.Cast<UIElement>().ToList().ForEach(x =>
                    {

                        if (x.GetType() == typeof(HRectangle))
                        {
                            HRectangle rec = x as HRectangle; 
 
                            //가로 좌표
                            double leftValue = margin.X;
                            leftValue -= rec.MoveXValue * zoom;
                            leftValue -= rec.LeftMoveValue * zoom;

                            Canvas.SetLeft(x, leftValue);
                            Canvas.SetTop(x, margin.Y - rec.TopMoveValue * zoom - rec.MoveYValue * zoom);

                            //높이
                            double recHeight = (rec.originHeight) * zoom;
                            recHeight += rec.TopMoveValue * zoom;
                            recHeight += rec.BottomMoveValue * zoom;

                            if (recHeight > 0)
                            {
                                rec.Height = recHeight;
                            }
                            else
                            {
                                rec.Height = 1;
                            }

                            //가로

                            double recWidth = rec.originWidth * zoom;
                            recWidth += rec.RightMoveValue * zoom;
                            recWidth += rec.LeftMoveValue * zoom;

                            if (recWidth > 0)
                            {
                                rec.Width = recWidth;
                            }
                            else
                            {
                                rec.Width = 1;
                            }
                        }
                        else if (x.GetType() == typeof(HCircle))
                        {
                            HCircle circle = x as HCircle;

                            //가로 좌표
                            double leftValue = margin.X;
                            leftValue -= circle.MoveXValue * zoom;
                            leftValue -= circle.LeftMoveValue * zoom;

                            Canvas.SetLeft(x, leftValue);
                            Canvas.SetTop(x, margin.Y - circle.TopMoveValue * zoom - circle.MoveYValue * zoom);

                            //높이
                            double recHeight = (circle.originHeight) * zoom;
                            recHeight += circle.TopMoveValue * zoom;
                            recHeight += circle.BottomMoveValue * zoom;

                            if (recHeight > 0)
                            {
                                circle.Height = recHeight;
                            }
                            else
                            {
                                circle.Height = 1;
                            }

                            //가로

                            double recWidth = circle.originWidth * zoom;
                            recWidth += circle.RightMoveValue * zoom;
                            recWidth += circle.LeftMoveValue * zoom;

                            if (recWidth > 0)
                            {
                                circle.Width = recWidth;
                            }
                            else
                            {
                                circle.Width = 1;
                            }
                        }
                    });
                }
            }
            catch
            {

            }
        }

        private void Canvas_KeyDown(object sender, KeyEventArgs e)
        {
            this.Focus();
        }
    }
}