using CaptureSampleCore;
using Composition.WindowsRuntimeHelpers;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.UI.Composition;

namespace WPFCaptureSample
{
    public class MainCaptureCreator
    {
        private IntPtr mainHwnd;
        private Compositor compositor;
        private CompositionTarget target;
        private ContainerVisual root;

        private BasicSampleApplication sample;

        BasicCapture.AskCopy ask;


        public void init(Window window, float offset, BasicCapture.AskCopy ask)
        {
            var interopWindow = new WindowInteropHelper(window);
            mainHwnd = interopWindow.Handle;
            InitComposition(offset);
            this.ask = ask;
        }
        private void InitComposition(float offset)
        {
            // Create the compositor.
            compositor = new Compositor();

            // Create a target for the window.
            target = compositor.CreateDesktopWindowTarget(mainHwnd, true);

            // Attach the root visual.
            root = compositor.CreateContainerVisual();
            root.RelativeSizeAdjustment = Vector2.One;
            root.Size = new Vector2(-offset, 0);
            root.Offset = new Vector3(offset, 0, 0);
            target.Root = root;

            // Setup the rest of the sample application.
            sample = new BasicSampleApplication(compositor);
            root.Children.InsertAtTop(sample.Visual);
        }


        public void StopCapture()
        {
            sample.StopCapture();
        }

        public async Task StartPickerCaptureAsync()
        {
            StopCapture();
            var picker = new GraphicsCapturePicker();
            picker.SetWindow(mainHwnd);
            GraphicsCaptureItem item = await picker.PickSingleItemAsync();

            if (item != null)
            {
                sample.StartCaptureFromItem(item, ask);
            }
        }

        public void StartHwndCapture(IntPtr hwnd)
        {
            GraphicsCaptureItem item = CaptureHelper.CreateItemForWindow(hwnd);
            if (item != null)
            {
                sample.StartCaptureFromItem(item, ask);
            }
        }

        public static async Task<byte[]> ConvertSurfaceToPngCall(IDirect3DSurface surface)
        {
            using (var t = await SoftwareBitmap.CreateCopyFromSurfaceAsync(surface).AsTask())
            {
                using (var memoryStream = new MemoryStream())
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, memoryStream.AsRandomAccessStream());
                    // Set the software bitmap
                    encoder.SetSoftwareBitmap(t);
                    await encoder.FlushAsync();
                    return memoryStream.ToArray();
                }
            }
            
        }
        
    }
}
