using System;
using System.Runtime.InteropServices;

namespace MouseJigglerPro.Core
{
    internal static class PInvokeHelper
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowRect(IntPtr hWnd, out RECT lpRect);

        public static uint GetLastInputTime()
        {
            var lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            GetLastInputInfo(ref lastInputInfo);
            return lastInputInfo.dwTime;
        }

        public static bool IsForegroundWindowFullScreen()
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero) return false;

            GetWindowRect(foregroundWindow, out RECT windowRect);
            
            // Compare window size with screen size
            var screen = System.Windows.Forms.Screen.FromHandle(foregroundWindow);
            return windowRect.Left == screen.Bounds.Left &&
                   windowRect.Top == screen.Bounds.Top &&
                   windowRect.Right == screen.Bounds.Right &&
                   windowRect.Bottom == screen.Bounds.Bottom;
        }

        public static void SendPhantomKeystroke()
        {
            INPUT[] inputs = new INPUT[2];
            
            // Press the key
            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = VK_F15,
                        wScan = 0,
                        dwFlags = 0, // 0 for key press
                        time = 0,
                        dwExtraInfo = GetMessageExtraInfo()
                    }
                }
            };

            // Release the key
            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = VK_F15,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP, // 2 for key up
                        time = 0,
                        dwExtraInfo = GetMessageExtraInfo()
                    }
                }
            };

            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void SendMouseInput(int dx, int dy, uint dwFlags)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0] = new INPUT
            {
                type = INPUT_MOUSE,
                u = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = dx,
                        dy = dy,
                        mouseData = 0,
                        dwFlags = dwFlags,
                        time = 0,
                        dwExtraInfo = GetMessageExtraInfo()
                    }
                }
            };

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        // Constants for mouse input
        public const uint MOUSEEVENTF_MOVE = 0x0001;

        // Structures for input simulation
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private const int INPUT_MOUSE = 0;
        private const int INPUT_KEYBOARD = 1;
        private const int INPUT_HARDWARE = 2;

        // Virtual Key Codes
        private const ushort VK_F15 = 0x7E;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}