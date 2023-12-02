//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using CaptureSampleCore;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;



namespace WPFCaptureSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, BasicCapture.AskCopy
    {
        const string BSAP_WindowName = "BlueStacks App Player";
        private ObservableCollection<Process> processes;

        private MainCaptureCreator creator = new MainCaptureCreator();
        private ImageLoader imageStore = new ImageLoader();
        private bool _needToDie = false;
        private Thread _thread;

        private Window imgWin = new Window();
        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            // Force graphicscapture.dll to load.
            var picker = new GraphicsCapturePicker();
#endif
            imageStore.LoadAll();
            _thread = new Thread(actionthread);
            _thread.Start();

            imgWin.Width= 20;
            imgWin.Height= 20;
            imgWin.Show();
            //imgWin.Hide();
        }


        private async void PickerButton_Click(object sender, RoutedEventArgs e)
        {
            creator.StopCapture();
            WindowComboBox.SelectedIndex = -1;
            await creator.StartPickerCaptureAsync(this);
        }

        double dpiX = 1.0;
        double dpiY = 1.0;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //var interopWindow = new WindowInteropHelper(this);
            //hwnd = interopWindow.Handle;            
            var presentationSource = PresentationSource.FromVisual(this);
            
            if (presentationSource != null)
            {
                dpiX = presentationSource.CompositionTarget.TransformToDevice.M11;
                dpiY = presentationSource.CompositionTarget.TransformToDevice.M22;
            }
            var controlsWidth = (float)(ControlsGrid.ActualWidth * dpiX);
            controlsWidth = 0;
            creator.init(imgWin, controlsWidth, this);
            //InitComposition(controlsWidth + 100);
            //InitComposition(0);
            InitWindowListAndStart();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            var curText = StopButton.Content as string;
            if (String.Equals(curText, "Stop Capture"))
            {
                creator.StopCapture();
                WindowComboBox.SelectedIndex = -1;
                StopButton.Content = "Start Capture";                
            }
            else
            {
                InitWindowListAndStart();
            }
        }

        private void WindowComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var process = (Process)comboBox.SelectedItem;

            if (process != null)
            {
                
            }
        }

        private IntPtr gameWin = IntPtr.Zero;
        private void InitWindowListAndStart()
        {
            if (ApiInformation.IsApiContractPresent(typeof(Windows.Foundation.UniversalApiContract).FullName, 8))
            {
                var pp = Process.GetProcesses();
                var processesWithWindows = from p in Process.GetProcesses()
                                           where !string.IsNullOrWhiteSpace(p.MainWindowTitle) && WindowEnumerationHelper.IsWindowValidForCapture(p.MainWindowHandle)
                                           && string.Equals(p.MainWindowTitle, BSAP_WindowName)
                                           select p;
                processes = new ObservableCollection<Process>(processesWithWindows);
                WindowComboBox.ItemsSource = processes;
                if (processes.Count == 1)
                {
                    WindowComboBox.SelectedIndex = 0;
                    creator.StopCapture();
                    var hwnd = processesWithWindows.First().MainWindowHandle;
                    gameWin = hwnd;
                    try
                    {
                        creator.StartHwndCapture(hwnd);
                        StopButton.Content = "Stop Capture";
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine($"Hwnd 0x{hwnd.ToInt32():X8} is not valid for capture!");                        
                    }
                } 
            }
            else
            {
                WindowComboBox.IsEnabled = false;
            }
        }


        

                


        

        bool captureOne = false;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            btnCaptureAndSeg.IsEnabled = false;
            //ConvertToBitmap(sample.Visual);
            captureOne = true;
        }

        bool autoItCaptureOne = false;
        bool autoItCaptureInFlight = false;
        byte[] autoItCapturedData = null;
        static readonly object syncObj = new object();
        async void BasicCapture.AskCopy.doSurface(IDirect3DSurface surface)
        {
            byte[] autoItBuf = null;
            if (autoItCaptureOne)
            {
                autoItCaptureOne = false;
                autoItCaptureInFlight = true;
                autoItBuf = await MainCaptureCreator.ConvertSurfaceToPngCall(surface).ConfigureAwait(false);
            }
            lock (syncObj)
            {
                if (autoItBuf!= null)
                {
                    autoItCaptureInFlight = false;
                    autoItCapturedData = autoItBuf;
                }   
            }
            if (captureOne)
            {
                captureOne = false;
                var tbf = await MainCaptureCreator.ConvertSurfaceToPngCall(surface);
                var tmStr = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
                var fileName = "d:\\segan\\out\\test\\test" + tmStr + ".png";
                File.WriteAllBytes(fileName, tbf);
                var command = "d:\\segan\\testwithfile.bat " + fileName;
                ExecuteCmd(command);
                Console.WriteLine("cmd.exe /c " + command);
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    btnCaptureAndSeg.IsEnabled = true;
                }));
            }
        }

        void ExecuteCmd(string command)
        {
            int ExitCode;
            ProcessStartInfo ProcessInfo;
            Process Process;

            ProcessInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            ProcessInfo.CreateNoWindow = false;
            ProcessInfo.UseShellExecute = true;

            Process = Process.Start(ProcessInfo);
            Process.WaitForExit();

            ExitCode = Process.ExitCode;
            //Process.Close();
        }

        bool checkMouse = false;
        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            //new ImageLoader().LoadAll();
            //Task.Run(() =>
            {
                
                var wnd = Win32Helper.FindWindow(null, BSAP_WindowName);
                Win32Helper.SetForegroundWindow(wnd);
                System.Threading.Thread.Sleep(1000);
                //System.Windows.Forms.SendKeys.SendWait("DDD");

                Win32Helper.SendKey('W', 0, true);
                System.Threading.Thread.Sleep(100);
                Win32Helper.SendKey('W', 0, false);

                Win32Helper.Rect rect = new Win32Helper.Rect();
                Win32Helper.GetWindowRect(wnd, ref rect);
                Win32Helper.SetCursorPos(rect.Left, rect.Top);
            }
            //);

            if (!checkMouse)
            {
                checkMouse = true;
                Task.Run(() =>
                {
                    int count = 0;
                    while(checkMouse)
                    {
                        System.Threading.Thread.Sleep(100);
                        Win32Helper.POINT pt = new Win32Helper.POINT();
                        Win32Helper.GetCursorPos(out pt);

                        
                        count++;
                        if (count >  10) {
                            pt.X++;
                            count = 0;
                            Win32Helper.SetCursorPos(pt.X, pt.Y);
                        }
                        Dispatcher.Invoke(() =>
                        {
                            txtInfo.Text = "pt" + pt.X + "," + pt.Y;
                        });
                    }

                });
            } else
            {
                checkMouse = false;
            }

        }


        private void actionthread()
        {
            while(!_needToDie)
            {
                System.Threading.Thread.Sleep(5000);
                if (autoItCaptureInFlight) return;
                if (autoItCaptureOne) return;

                if (autoItCapturedData != null)
                {
                    processBuffer(autoItCapturedData);
                }
                autoItCaptureOne = true;
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
            Dispatcher.Invoke(() =>
            {
                if (_needToDie) return;
                using (MemoryStream memory = new MemoryStream(buf))
                {
                    memory.Position = 0;
                    BitmapImage bitmapimage = new BitmapImage();
                    bitmapimage.BeginInit();
                    bitmapimage.StreamSource = memory;
                    bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapimage.EndInit();
                    canvImg.Source = bitmapimage;
                }
            });

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
                        Console.WriteLine(rect.Left+","+rect.Top+" adding "+store.rect.Left+","+store.rect.Top);
                        Win32Helper.SetCursorPos(rect.Left + (int)((store.rect.Left + 50)*dpiX), rect.Top+ (int)((store.rect.Top+100)*dpiY));
                        System.Threading.Thread.Sleep(200);
                        Console.WriteLine("moving to "+rect.Right +","+ rect.Bottom);
                        Win32Helper.SetCursorPos(rect.Right , rect.Bottom);
                        Win32Helper.SendMouseClick();
                    } 
                    if (store.name.Equals("BuilderBase_ReturnHome"))
                    {
                        ActionMoveToStoreAndClick(store, 20, 20);
                    }
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _needToDie = true;
            _thread.Join();
            imgWin.Close();
        }
    }
}
