using ccAuto2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;

namespace ccauto
{
    public class GenCapture
    {
        public MainCaptureCreator creator = new MainCaptureCreator();
        public IntPtr gameWin { get; private set; }


        class CurWinInfo
        {
            public int x;
            public int y;
            public int width;
            public int height;
            public double DPIX;
            public double DPIY;
        }
        private CurWinInfo curWinInfo = new CurWinInfo();

        public bool isStarted()
        {
            return creator.isStarted();
        }

        public string InitWindowListAndStart(string windowName)
        {
            gameWin = IntPtr.Zero;
            if (ApiInformation.IsApiContractPresent(typeof(Windows.Foundation.UniversalApiContract).FullName, 8))
            {
                var pp = Process.GetProcesses();
                var processesWithWindows = from p in Process.GetProcesses()
                                           where !string.IsNullOrWhiteSpace(p.MainWindowTitle) && WindowEnumerationHelper.IsWindowValidForCapture(p.MainWindowHandle)
                                           && string.Equals(p.MainWindowTitle, windowName)
                                           select p;

                if (processesWithWindows.Count() == 1)
                {
                    creator.StopCapture();
                    var hwnd = processesWithWindows.First().MainWindowHandle;
                    Win32Helper.Rect rect = new Win32Helper.Rect();
                    Win32Helper.GetWindowRect(hwnd, ref rect);
                    //Win32Helper.SetWindowPos(hwnd, 0, rect.Left, rect.Top, CAPTUREW, CAPTUREH, 0);

                    gameWin = hwnd;
                    try
                    {
                        creator.StartHwndCapture(hwnd);
                        return "OK";
                    }
                    catch (Exception)
                    {
                        return ($"Hwnd 0x{hwnd.ToInt32():X8} is not valid for capture!");
                    }
                }
            }
            return "No suitable window";
        }
        public int TranslatePointXToScreen(int x)
        {
            return curWinInfo.x + (int)(x * curWinInfo.DPIX);
        }
        public int TranslatePointYToScreen(int y)
        {
            return curWinInfo.y + (int)(y * curWinInfo.DPIY);
        }
        public Emgu.CV.Mat updateWindowRef(byte[] buf)
        {
            var src = ImageLoader.bufToMat(buf);
            Win32Helper.Rect rect = new Win32Helper.Rect();
            Win32Helper.GetWindowRect(gameWin, ref rect);
            curWinInfo.x = rect.Left;
            curWinInfo.y = rect.Top;
            curWinInfo.width = rect.Right - rect.Left;
            curWinInfo.height = rect.Bottom - rect.Top;
            curWinInfo.DPIX = curWinInfo.width * 1.0 / src.Width;
            curWinInfo.DPIY = curWinInfo.height * 1.0 / src.Height;
            return src;
        }
    }

    
}
