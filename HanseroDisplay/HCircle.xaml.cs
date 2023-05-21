using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HanseroDisplay
{
    /// <summary>
    /// HCircle.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class HCircle : UserControl, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public enum CircleModeConstant
        {
            NORMAL,
            MOVE,
            MOVE_TOP,
            MOVE_BOTTOM,
            MOVE_RIGHT,
            MOVE_Left,
        }
        public CircleModeConstant Mode = CircleModeConstant.NORMAL;

        public int originWidth;
        public int originHeight;

        public HCanvas canvas;

        public double TopMoveValue { get { return topMoveValue; } set { topMoveValue = value; NotifyPropertyChanged("TopMoveValue"); NotifyPropertyChanged("StartY"); NotifyPropertyChanged("SelectedHeight"); } }
        public double BottomMoveValue { get { return bottomMoveValue; } set { bottomMoveValue = value; NotifyPropertyChanged("BottomMoveValue"); NotifyPropertyChanged("StartY"); NotifyPropertyChanged("SelectedHeight"); } }
        public double MoveXValue { get { return moveXValue; } set { moveXValue = value; NotifyPropertyChanged("MoveXValue"); NotifyPropertyChanged("StartX"); NotifyPropertyChanged("SelectedWidth"); } }
        public double MoveYValue { get { return moveYValue; } set { moveYValue = value; NotifyPropertyChanged("MoveYValue"); NotifyPropertyChanged("StartY"); NotifyPropertyChanged("SelectedHeight"); } }
        public double RightMoveValue { get { return rightMoveValue; } set { rightMoveValue = value; NotifyPropertyChanged("RightMoveValue"); NotifyPropertyChanged("StartX"); NotifyPropertyChanged("SelectedWidth"); } }
        public double LeftMoveValue { get { return leftMoveValue; } set { leftMoveValue = value; NotifyPropertyChanged("LeftMoveValue"); NotifyPropertyChanged("StartX"); NotifyPropertyChanged("SelectedWidth"); } }

        public double StartX { get { return Math.Round(-MoveXValue - leftMoveValue); } }
        public double StartY { get { return Math.Round(-MoveYValue - topMoveValue); } }

        public double SelectedWidth { get { return Math.Round((originWidth + RightMoveValue + LeftMoveValue)); } }
        public double SelectedHeight { get { return Math.Round((originHeight + topMoveValue + bottomMoveValue)); } }

        private double topMoveValue = 0;
        private double bottomMoveValue = 0;
        private double moveXValue = 0;
        private double moveYValue = 0;
        private double rightMoveValue = 0;
        private double leftMoveValue = 0;

        public Point startPoint;


        public HCircle(int width, int height, HCanvas canvas)
        {
            InitializeComponent();

            this.originWidth = width;
            this.originHeight = height;

            this.Height = height;
            this.Width = width;

            this.canvas = canvas;
        }

        public HCircle(int width, int height, int x, int y, HCanvas canvas)
        {
            InitializeComponent();

            this.originWidth = width;
            this.originHeight = height;

            this.Height = height;
            this.Width = width;

            this.MoveXValue = x;
            this.MoveYValue = y;

            this.canvas = canvas;
        }

        #region 커서
        private void Label_NS_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.SizeNS;
        }

        private void Label_MouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = null;
        }

        private void Border_Move_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Hand;
        }

        private void Border_Move_MouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = null;
        }

        private void Label_WE_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.SizeWE;
        }

        private void Label_NWSE_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.SizeNWSE;
        }

        private void Label_NESW_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.SizeNESW;
        }

        #endregion

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            canvas.StopMove = false;
            Mode = CircleModeConstant.NORMAL;
        }


        private void Border_TOP_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Mode = CircleModeConstant.MOVE_TOP;
            canvas.StopMove = true;
            canvas.SelectedElement = this;
            canvas.startPoint = e.GetPosition(canvas);
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Mode = CircleModeConstant.MOVE;
            canvas.StopMove = true;
            canvas.SelectedElement = this;
            canvas.startPoint = e.GetPosition(canvas);
        }

        private void Border_Bottom_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Mode = CircleModeConstant.MOVE_BOTTOM;
            canvas.StopMove = true;
            canvas.SelectedElement = this;
            canvas.startPoint = e.GetPosition(canvas);
        }

        private void Border_Right_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Mode = CircleModeConstant.MOVE_RIGHT;
            canvas.StopMove = true;
            canvas.SelectedElement = this;
            canvas.startPoint = e.GetPosition(canvas);
        }


        private void Border_Left_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Mode = CircleModeConstant.MOVE_Left;
            canvas.StopMove = true;
            canvas.SelectedElement = this;
            canvas.startPoint = e.GetPosition(canvas);
        }
    }
}
