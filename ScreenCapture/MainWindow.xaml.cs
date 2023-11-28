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
using Composition.WindowsRuntimeHelpers;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.UI.Composition;


namespace WPFCaptureSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, BasicCapture.AskCopy
    {
        private IntPtr hwnd;
        private Compositor compositor;
        private CompositionTarget target;
        private ContainerVisual root;

        private BasicSampleApplication sample;
        private ObservableCollection<Process> processes;
        private ObservableCollection<MonitorInfo> monitors;

        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            // Force graphicscapture.dll to load.
            var picker = new GraphicsCapturePicker();
#endif
        }

        private async void PickerButton_Click(object sender, RoutedEventArgs e)
        {
            StopCapture();
            WindowComboBox.SelectedIndex = -1;
            MonitorComboBox.SelectedIndex = -1;
            await StartPickerCaptureAsync();
        }

        private void PrimaryMonitorButton_Click(object sender, RoutedEventArgs e)
        {
            StopCapture();
            WindowComboBox.SelectedIndex = -1;
            MonitorComboBox.SelectedIndex = -1;
            StartPrimaryMonitorCapture();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var interopWindow = new WindowInteropHelper(this);
            hwnd = interopWindow.Handle;

            var presentationSource = PresentationSource.FromVisual(this);
            double dpiX = 1.0;
            double dpiY = 1.0;
            if (presentationSource != null)
            {
                dpiX = presentationSource.CompositionTarget.TransformToDevice.M11;
                dpiY = presentationSource.CompositionTarget.TransformToDevice.M22;
            }
            var controlsWidth = (float)(ControlsGrid.ActualWidth * dpiX);

            InitComposition(controlsWidth);
            InitWindowList();
            InitMonitorList();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopCapture();
            WindowComboBox.SelectedIndex = -1;
            MonitorComboBox.SelectedIndex = -1;
        }

        private void WindowComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var process = (Process)comboBox.SelectedItem;

            if (process != null)
            {
                StopCapture();
                MonitorComboBox.SelectedIndex = -1;
                var hwnd = process.MainWindowHandle;
                try
                {
                    StartHwndCapture(hwnd);
                }
                catch (Exception)
                {
                    Debug.WriteLine($"Hwnd 0x{hwnd.ToInt32():X8} is not valid for capture!");
                    processes.Remove(process);
                    comboBox.SelectedIndex = -1;
                }
            }
        }

        private void MonitorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var monitor = (MonitorInfo)comboBox.SelectedItem;

            if (monitor != null)
            {
                StopCapture();
                WindowComboBox.SelectedIndex = -1;
                var hmon = monitor.Hmon;
                try
                {
                    StartHmonCapture(hmon);
                }
                catch (Exception)
                {
                    Debug.WriteLine($"Hmon 0x{hmon.ToInt32():X8} is not valid for capture!");
                    monitors.Remove(monitor);
                    comboBox.SelectedIndex = -1;
                }
            }
        }

        private void InitComposition(float controlsWidth)
        {
            // Create the compositor.
            compositor = new Compositor();

            // Create a target for the window.
            target = compositor.CreateDesktopWindowTarget(hwnd, true);

            // Attach the root visual.
            root = compositor.CreateContainerVisual();
            root.RelativeSizeAdjustment = Vector2.One;
            root.Size = new Vector2(-controlsWidth, 0);
            root.Offset = new Vector3(controlsWidth, 0, 0);
            target.Root = root;

            // Setup the rest of the sample application.
            sample = new BasicSampleApplication(compositor);
            root.Children.InsertAtTop(sample.Visual);
        }

        public static System.Windows.Media.Imaging.RenderTargetBitmap ConvertToBitmap(UIElement uiElement)
        {
            //double resolution
            var scale = 1;// resolution / 96d;

            uiElement.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            var sz = uiElement.DesiredSize;
            var rect = new Rect(sz);
            uiElement.Arrange(rect);

            var bmp = new System.Windows.Media.Imaging.RenderTargetBitmap((int)(scale * (rect.Width)), (int)(scale * (rect.Height)), scale * 96, scale * 96, System.Windows.Media.PixelFormats.Default);
            bmp.Render(uiElement);

            var bmpFrame = System.Windows.Media.Imaging.BitmapFrame.Create(bmp);
            var en = new System.Windows.Media.Imaging.PngBitmapEncoder();

            en.Frames.Add(bmpFrame);
            using (var memoryStream = new MemoryStream())
            {
                en.Save(memoryStream);
                File.WriteAllBytes("d:\\temp\\test.png", memoryStream.ToArray());
            }
            
            return bmp;
        }

        private void InitWindowList()
        {
            if (ApiInformation.IsApiContractPresent(typeof(Windows.Foundation.UniversalApiContract).FullName, 8))
            {
                var processesWithWindows = from p in Process.GetProcesses()
                                           where !string.IsNullOrWhiteSpace(p.MainWindowTitle) && WindowEnumerationHelper.IsWindowValidForCapture(p.MainWindowHandle)
                                           select p;
                processes = new ObservableCollection<Process>(processesWithWindows);
                WindowComboBox.ItemsSource = processes;
            }
            else
            {
                WindowComboBox.IsEnabled = false;
            }
        }

        private void InitMonitorList()
        {
            if (ApiInformation.IsApiContractPresent(typeof(Windows.Foundation.UniversalApiContract).FullName, 8))
            {
                monitors = new ObservableCollection<MonitorInfo>(MonitorEnumerationHelper.GetMonitors());
                MonitorComboBox.ItemsSource = monitors;
            }
            else
            {
                MonitorComboBox.IsEnabled = false;
                PrimaryMonitorButton.IsEnabled = false;
            }
        }

        private async Task StartPickerCaptureAsync()
        {
            var picker = new GraphicsCapturePicker();
            picker.SetWindow(hwnd);
            GraphicsCaptureItem item = await picker.PickSingleItemAsync();

            if (item != null)
            {
                sample.StartCaptureFromItem(item, this);
            }
        }

        private void StartHwndCapture(IntPtr hwnd)
        {
            GraphicsCaptureItem item = CaptureHelper.CreateItemForWindow(hwnd);
            if (item != null)
            {
                sample.StartCaptureFromItem(item, this);
            }
        }

        private void StartHmonCapture(IntPtr hmon)
        {
            GraphicsCaptureItem item = CaptureHelper.CreateItemForMonitor(hmon);
            if (item != null)
            {
                sample.StartCaptureFromItem(item, this);
            }
        }

        private void StartPrimaryMonitorCapture()
        {
            MonitorInfo monitor = (from m in MonitorEnumerationHelper.GetMonitors()
                           where m.IsPrimary
                           select m).First();
            StartHmonCapture(monitor.Hmon);
        }

        private void StopCapture()
        {
            sample.StopCapture();
        }

        bool captureOne = false;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //ConvertToBitmap(sample.Visual);
            captureOne = true;
        }

        void BasicCapture.AskCopy.doSurface(IDirect3DSurface surface)
        {
            if (captureOne){
                captureOne = false;
                SoftwareBitmap.CreateCopyFromSurfaceAsync(surface).AsTask().ContinueWith(async t =>
                {
                    
                    using (var memoryStream = new MemoryStream())
                    {
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, memoryStream.AsRandomAccessStream());

                        // Set the software bitmap
                        encoder.SetSoftwareBitmap(t.Result);

                                                
                        await encoder.FlushAsync();
                        File.WriteAllBytes("d:\\temp\\test.png", memoryStream.ToArray());
                    }
                    
                    //t.Result.
                });
            }
        }
    }
}
