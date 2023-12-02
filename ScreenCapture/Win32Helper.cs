using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace ccAuto2
{
    public static class Win32Helper
    {
        [DllImportAttribute("User32.dll")]
        public static extern IntPtr FindWindow(String ClassName, String WindowName);

        [DllImportAttribute("User32.dll")]
        public static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        public const uint WM_KEYDOWN = 0x0100;
        public const uint WM_KEYUP = 0x0101;
        //https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        //https://www.codeproject.com/Articles/5264831/How-to-Send-Inputs-using-Csharp
        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardInput
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct MouseInput
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public MouseInput mi;
            [FieldOffset(0)] public KeyboardInput ki;
            //[FieldOffset(0)] public HardwareInput hi;
        }
        public struct Input
        {
            public int type;
            public InputUnion u;
        }
        [Flags]
        public enum InputType
        {
            Mouse = 0,
            Keyboard = 1,
            Hardware = 2
        }
        [Flags]
        public enum MouseEventF
        {
            Absolute = 0x8000,
            HWheel = 0x01000,
            Move = 0x0001,
            MoveNoCoalesce = 0x2000,
            LeftDown = 0x0002,
            LeftUp = 0x0004,
            RightDown = 0x0008,
            RightUp = 0x0010,
            MiddleDown = 0x0020,
            MiddleUp = 0x0040,
            VirtualDesk = 0x4000,
            Wheel = 0x0800,
            XDown = 0x0080,
            XUp = 0x0100
        }
        [Flags]
        public enum KeyEventF
        {
            KeyDown = 0x0000,
            ExtendedKey = 0x0001,
            KeyUp = 0x0002,
            Unicode = 0x0004,
            Scancode = 0x0008
        }
        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);
        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();


        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }
        [DllImport("User32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        [DllImport("user32.dll")]
        public static extern UInt32 MapVirtualKey(UInt32 uCode, UInt32 uMapType);

        const uint SCAN_VK_CONTROL = 0x11;
        const uint SCAN_VK_LCONTROL = 0xA2; ///A3 rcontrol
        const uint SCAN_VK_LSHIFT = 0xA0;

        //VK_Shift 0x10
        //VK_RSHIFT 0xA1
        public static void SendKey(ushort vk, ushort scan, bool isKeyDown)
        {
            uint flag = (uint)(isKeyDown?KeyEventF.KeyDown: KeyEventF.KeyUp);
            if (scan != 0)
            {
                var oldScan = scan;
                scan = (UInt16)(MapVirtualKey((UInt32)scan, 0) & 0xFFU);
                flag |= (uint)KeyEventF.Scancode;
                Console.WriteLine("from " + oldScan.ToString("X") + " to " + scan.ToString("X"));
            }
            else
            {
                flag |= (uint)KeyEventF.Unicode;
            }            
            Input[] inputs = new Input[]
            {
                new Input
                {
                    type = (int)InputType.Keyboard,
                    u = new InputUnion
                    {
                        ki = new KeyboardInput
                        {
                            wVk = vk,
                            wScan = scan,
                            dwFlags = flag,//(uint)(KeyEventF.KeyDown | KeyEventF.Scancode),
                            time = 0,
                            dwExtraInfo = GetMessageExtraInfo(),
                        }
                    }
                }
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
        }


        public static void SendMouseClick()
        {
            Input[] inputs = new Input[]
            {
                new Input
                {
                    type = (int)InputType.Mouse,
                    u = new InputUnion
                    {
                        mi = new MouseInput
                        {
                            dx = 0,
                            dy = 0,
                            dwFlags = (uint)MouseEventF.LeftDown,
                            time = 0,
                            dwExtraInfo = GetMessageExtraInfo(),
                        }
                    },
                },
                new Input
                {
                    type = (int)InputType.Mouse,
                    u = new InputUnion
                    {
                        mi = new MouseInput
                        {
                            dx = 0,
                            dy = 0,
                            dwFlags = (uint)MouseEventF.LeftUp,
                            time = 0,
                            dwExtraInfo = GetMessageExtraInfo(),
                        }
                    },
                }
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
        }
    }
}
