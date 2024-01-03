using ccauto;
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
        public GenCapture genCapture = new GenCapture();
        
        //public MainCaptureCreator creator = new MainCaptureCreator();
        //public IntPtr gameWin { get; private set; }


        public bool _needToDie { get; private set; }
        private Thread _thread;
        EventRequester.RequestAndResult gameResult;
        private bool disposedValue;
        private ImageLoader imageStore = new ImageLoader();
        private Action<byte[]> showImage;

        //int CAPTUREW = 982;
        //int CAPTUREH = 567;  //1920/1080 240 dpi default

        //float dpiX = 1.5f, dpiY = 1.5f;
        
        //returns OK if fine, else error
        public string InitWindowListAndStart()
        {
            return genCapture.InitWindowListAndStart(BSAP_WindowName);            
        }

        public bool isStarted()
        {
            return genCapture.isStarted();
        }
        public void Init(EventRequester.RequestAndResult gameResult, Action<byte[]> showImage)
        {
            imageStore.LoadAll();
            this.gameResult = gameResult;
            this.showImage = showImage;
            this._thread = new Thread(actionthread);
            this._thread.Start();         
        }

        public EventRequester.RequestAndResult registerNewEvent(string eventName)
        {
            return genCapture.creator.registerNewEvent(eventName);
        }
        public void StopCapture()
        {
            genCapture.creator.StopCapture();
        }

        public Task StartPickerCaptureAsync(System.Windows.Window wnd)
        {
            return genCapture.creator.StartPickerCaptureAsync(wnd);
        }

        public void init(System.Windows.Window wnd, float off)
        {
            genCapture.creator.init(wnd, off);
        }

        private void actionthread()
        {
            while (!_needToDie)
            {
                System.Threading.Thread.Sleep(8000);

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

        Win32Helper.Rect rect = new Win32Helper.Rect();
        private void ActionMouseMoveToStore(ImageStore store, int xOff, int yOff)
        {
            //Win32Helper.Rect rect = new Win32Helper.Rect();
            //Win32Helper.GetWindowRect(genCapture.gameWin, ref rect);
            Console.WriteLine("going to set cursor pos" + rect.Left + "," + rect.Top);
            System.Threading.Thread.Sleep(1000);
            Win32Helper.SetCursorPos(rect.Left, rect.Top);
            System.Threading.Thread.Sleep(1000);
            Console.WriteLine(rect.Left + "," + rect.Top + " adding " + store.rect.Left + "," + store.rect.Top);
            Win32Helper.SetCursorPos(genCapture.TranslatePointXToScreen(store.rect.Left + xOff), genCapture.TranslatePointYToScreen(store.rect.Top+yOff));
            System.Threading.Thread.Sleep(1000);
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
            var src = genCapture.updateWindowRef(buf);
            //Win32Helper.Rect rect = new Win32Helper.Rect();
            Win32Helper.GetWindowRect(genCapture.gameWin, ref rect);
            Console.WriteLine("got buffer and converted to image");            
            foreach (var store in imageStore.stores)
            {
                var diff = ImageLoader.CompareToMat(src, store);

                if (diff > 0.5)
                {
                    Console.WriteLine("for " + store.name + " diff=" + diff.ToString("0.00"));
                    string[] autoClickNames = new string[]
                    {
                        "AnyoneThereReload",
                        "BuilderBase_ReturnHome",
                        "BuilderBase_ReturnHome_2",
                        "BuilderBaseAttack",
                        "BuilderBaseAttack_1",
                        "BuilderBaseAttack_4_8",
                        "BuilderBaseBattleFindNow",
                        "BuilderBase_Battle_BattleMachine",
                        "BuilderBaseBattleSurrender",
                        "BuilderBaseBattleSurrenderOK",
                    };
                    foreach (string name in autoClickNames)
                    {
                        if (store.name.Equals(name))
                        {
                            int offsetX = 20;
                            int offsetY = 20;
                            if (store.name.Equals("AnyoneThereReload"))
                            {
                                offsetX = 50;
                                offsetY = 100;
                            } else if (name.Equals("BuilderBaseBattleSurrenderOK"))
                            {
                                offsetX = 333;
                                offsetY = 216;
                            }
                            Console.WriteLine("Doing auto action for " + name);
                            ActionMoveToStoreAndClick(store, offsetX, offsetY);

                            if (name.Equals("BuilderBase_Battle_BattleMachine"))
                            {
                                Console.WriteLine("clicked battlemachine, click above");
                                Thread.Sleep(1000);
                                ActionMoveToStoreAndClick(store, offsetX, -100);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine(" " + store.name + " diff=" + diff.ToString("0.00"));
                }                
            }
            Win32Helper.SetCursorPos(rect.Left, rect.Top);
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
