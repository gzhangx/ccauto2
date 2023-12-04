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

namespace ccAuto2
{
    public class MainCaptureCreator: BasicCapture.AskCopy
    {
        private Compositor compositor;
        private CompositionTarget target;
        private ContainerVisual root;

        private BasicSampleApplication sample;

        private EventRequester eventRequester = new EventRequester();
        public void init(Window window, float offset)
        {
            var interopWindow = new WindowInteropHelper(window);
            InitComposition(interopWindow.Handle, offset);
        }
        private void InitComposition(IntPtr mainHwnd, float offset)
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


        public EventRequester.RequestAndResult registerNewEvent(string name)
        {
            return eventRequester.registerNewEvent(name);
        }

        public async Task StartPickerCaptureAsync(Window window)
        {
            var interopWindow = new WindowInteropHelper(window);
            StopCapture();
            var picker = new GraphicsCapturePicker();
            picker.SetWindow(interopWindow.Handle);
            GraphicsCaptureItem item = await picker.PickSingleItemAsync();

            if (item != null)
            {
                sample.StartCaptureFromItem(item, this);
            }
        }

        public void StartHwndCapture(IntPtr hwnd)
        {
            GraphicsCaptureItem item = CaptureHelper.CreateItemForWindow(hwnd);
            if (item != null)
            {
                sample.StartCaptureFromItem(item, this);
            }
        }

        //matchTemplate, minMaxLoc
        static async Task<byte[]> ConvertSurfaceToPngCall(IDirect3DSurface surface)
        {
            using (var t = await SoftwareBitmap.CreateCopyFromSurfaceAsync(surface).AsTask().ConfigureAwait(false))
            {
                using (var memoryStream = new MemoryStream())
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, memoryStream.AsRandomAccessStream()).AsTask().ConfigureAwait(false);
                    // Set the software bitmap
                    encoder.SetSoftwareBitmap(t);
                    await encoder.FlushAsync().AsTask().ConfigureAwait(false);
                    return memoryStream.ToArray();
                }
            }
            
        }        

        async void BasicCapture.AskCopy.doSurface(IDirect3DSurface surface)
        {
            if (eventRequester.canProcessRequest())
            {
                var buf = await MainCaptureCreator.ConvertSurfaceToPngCall(surface).ConfigureAwait(false);
                eventRequester.processRequest(buf);
            }
        }
    }
}
