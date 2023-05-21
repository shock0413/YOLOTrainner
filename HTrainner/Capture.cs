using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Drawing;
using System.Threading;
using System.Diagnostics;

namespace HsrAITrainner
{
    public class Capture
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern IntPtr ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        public Bitmap CaptureApplication(string procName, string projectName)
        {
            Process proc;

            // Cater for cases when the process can't be located.
            try
            {
                proc = Process.GetProcessesByName(procName)[0];
            }
            catch (IndexOutOfRangeException e)
            {
                return null;
            }

            IntPtr iPtr;

            // You need to focus on the application
            if (proc.MainWindowTitle.Contains(".png"))
            {
                iPtr = proc.MainWindowHandle;
            }
            else
            {
                iPtr = FindWindow(null, "chart_" + projectName + ".png");
            }

            // Thread.Sleep(1000);

            Bitmap bmp = PrintWindow(iPtr);


            return bmp;
        }

        [DllImportAttribute("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]

        [return: MarshalAs(UnmanagedType.Bool)]

        static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);



        [DllImport("user32.dll", SetLastError = true)]

        static extern int GetWindowRgn(IntPtr hWnd, IntPtr hRgn);



        [DllImport("gdi32.dll")]

        static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        public static Bitmap PrintWindow(IntPtr hwnd)

        {

            Rectangle rc = Rectangle.Empty;

            Graphics gfxWin = Graphics.FromHwnd(hwnd);

            rc = Rectangle.Round(gfxWin.VisibleClipBounds);



            Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);



            Graphics gfxBmp = Graphics.FromImage(bmp);

            IntPtr hdcBitmap = gfxBmp.GetHdc();

            bool succeeded = PrintWindow(hwnd, hdcBitmap, 1);

            gfxBmp.ReleaseHdc(hdcBitmap);

            if (!succeeded)

            {

                gfxBmp.FillRectangle(

                    new SolidBrush(Color.Gray),

                    new Rectangle(Point.Empty, bmp.Size));

            }

            IntPtr hRgn = CreateRectRgn(0, 0, 0, 0);

            GetWindowRgn(hwnd, hRgn);

            Region region = Region.FromHrgn(hRgn);

            if (!region.IsEmpty(gfxBmp))

            {

                gfxBmp.ExcludeClip(region);

                gfxBmp.Clear(Color.Transparent);

            }

            gfxBmp.Dispose();

            return bmp;

        }
    }
}
