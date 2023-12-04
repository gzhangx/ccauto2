using ccAuto2;
using Emgu.CV.OCR;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Windows.Foundation.Metadata;

namespace ccAuto2
{
    public class CocCapture : IDisposable
    {
        public const string BSAP_WindowName = "BlueStacks App Player";
        public MainCaptureCreator creator = new MainCaptureCreator();
        public IntPtr gameWin { get; private set; }


        public bool _needToDie { get; private set; }
        private Thread _thread;
        EventRequester.RequestAndResult gameResult;
        private bool disposedValue;
        private ImageLoader imageStore = new ImageLoader();
        private Action<byte[]> showImage;

        int CAPTUREW = 982;
        int CAPTUREH = 567;

        float dpiX = 1.5f, dpiY = 1.5f;
        //returns OK if fine, else error
        public string InitWindowListAndStart()
        {
            gameWin = IntPtr.Zero;
            if (ApiInformation.IsApiContractPresent(typeof(Windows.Foundation.UniversalApiContract).FullName, 8))
            {
                var pp = Process.GetProcesses();
                var processesWithWindows = from p in Process.GetProcesses()
                                           where !string.IsNullOrWhiteSpace(p.MainWindowTitle) && WindowEnumerationHelper.IsWindowValidForCapture(p.MainWindowHandle)
                                           && string.Equals(p.MainWindowTitle, BSAP_WindowName)
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

        public void Init(EventRequester.RequestAndResult gameResult, Action<byte[]> showImage)
        {
            imageStore.LoadAll();
            this.gameResult = gameResult;
            this.showImage = showImage;
            this._thread = new Thread(actionthread);
            this._thread.Start();         
        }

        private void actionthread()
        {
            while (!_needToDie)
            {
                System.Threading.Thread.Sleep(5000);

                gameResult.doRequest(tb => { });

                while (!_needToDie)
                {
                    var buf = gameResult.waitFor(2000);
                    if (buf != null)
                    {
                        processBuffer(buf);
                        break;
                    }
                }
            }
        }

        private void ActionMouseMoveToStore(ImageStore store, int xOff, int yOff)
        {
            Win32Helper.Rect rect = new Win32Helper.Rect();
            Win32Helper.GetWindowRect(gameWin, ref rect);
            Win32Helper.SetCursorPos(rect.Left, rect.Top);
            System.Threading.Thread.Sleep(200);
            Console.WriteLine(rect.Left + "," + rect.Top + " adding " + store.rect.Left + "," + store.rect.Top);
            Win32Helper.SetCursorPos(rect.Left + (int)((store.rect.Left + xOff) * dpiX), rect.Top + (int)((store.rect.Top + yOff) * dpiY));
            System.Threading.Thread.Sleep(200);
            //Console.WriteLine("moving to " + rect.Right + "," + rect.Bottom);
            //Win32Helper.SetCursorPos(rect.Right, rect.Bottom);
            //Win32Helper.SendMouseClick();
        }
        private void ActionMoveToStoreAndClick(ImageStore store, int xOff, int yOff)
        {
            ActionMouseMoveToStore(store, xOff, yOff);
            Win32Helper.SendMouseClick();
        }
        int debugPos = 0;
        private void processBuffer(byte[] buf)
        {            
            if (_needToDie) return;
            showImage(buf);

            Console.WriteLine("got buffer " + (debugPos++));
            var src = ImageLoader.bufToMat(buf);
            Console.WriteLine("got buffer and converted to image");
            foreach (var store in imageStore.stores)
            {
                var diff = ImageLoader.CompareToMat(src, store);

                if (diff > 0.9)
                {
                    Console.WriteLine("for " + store.name + " diff=" + diff);
                    if (store.name.Equals("AnyoneThereReload"))
                    {
                        Win32Helper.Rect rect = new Win32Helper.Rect();
                        Win32Helper.GetWindowRect(gameWin, ref rect);
                        Win32Helper.SetCursorPos(rect.Left, rect.Top);
                        System.Threading.Thread.Sleep(200);
                        Console.WriteLine(rect.Left + "," + rect.Top + " adding " + store.rect.Left + "," + store.rect.Top);
                        Win32Helper.SetCursorPos(rect.Left + (int)((store.rect.Left + 50) * dpiX), rect.Top + (int)((store.rect.Top + 100) * dpiY));
                        System.Threading.Thread.Sleep(200);
                        Console.WriteLine("moving to " + rect.Right + "," + rect.Bottom);
                        Win32Helper.SetCursorPos(rect.Right, rect.Bottom);
                        Win32Helper.SendMouseClick();
                    }

                    string[] autoClickNames = new string[]
                    {
                        "BuilderBase_ReturnHome",
                        "BuilderBaseAttack",
                        "BuilderBaseBattleFindNow",
                        "BuilderBase_Battle_BattleMachine",
                    };
                    foreach (string name in autoClickNames)
                    {
                        if (store.name.Equals(name))
                        {
                            Console.WriteLine("Doing auto action for " + name);
                            ActionMoveToStoreAndClick(store, 20, 20);
                        }
                    }                    
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~CocCapture()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            _needToDie = true;
            _thread.Join();
            
        }
    }
}
