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

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // For getting the current thread ID
        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        // Hook-related imports
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, IntPtr lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // Constants for hooks
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WH_GETMESSAGE = 3;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_INPUTLANGCHANGE = 0x0051;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_MOUSEHWHEEL = 0x020E;
        private const int WM_XBUTTONDOWN = 0x020C;
        private const int WM_XBUTTONUP = 0x020D;

        // Our custom extra info value to identify our synthetic input
        private static IntPtr _syntheticInputMarker = new IntPtr(0x12345678);

        // Time of last real user input (managed by us, not Windows)
        private static uint _lastRealInputTime = 0;
        private static IntPtr _keyboardHook = IntPtr.Zero;
        private static IntPtr _mouseHook = IntPtr.Zero;
        private static IntPtr _inputLangHook = IntPtr.Zero;

        // Event for input language changes
        public static event Action<string>? InputLanguageChanged;

        // Delegate for keyboard hook
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        // Delegate for mouse hook
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        // Delegate for event hook (for input language change)
        private delegate IntPtr EventHookProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Keyboard hook procedure
        private static IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int message = wParam.ToInt32();
                
                // Handle input language change
                if (message == WM_INPUTLANGCHANGE)
                {
                    // Get the current system keyboard layout
                    IntPtr newKeyboardLayout = GetKeyboardLayout(0);
                    string langCode = GetLanguageCodeFromKeyboardLayout(newKeyboardLayout);
                    
                    // Update last language and raise event
                    if (_lastLanguage != langCode)
                    {
                        _lastLanguage = langCode;
                        InputLanguageChanged?.Invoke(langCode);
                    }
                    
                    return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
                }
                
                bool isKeyDown = message == WM_KEYDOWN || message == WM_SYSKEYDOWN;
                bool isKeyUp = message == WM_KEYUP || message == WM_SYSKEYUP;
                
                if (isKeyDown || isKeyUp)
                {
                    // Get the extra info from the message
                    MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    
                    // Check if this is NOT our synthetic input
                    if (hookStruct.dwExtraInfo != _syntheticInputMarker)
                    {
                        _lastRealInputTime = (uint)Environment.TickCount;
                    }
                }
            }
            return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        // Mouse hook procedure
        private static IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int message = wParam.ToInt32();
                
                // Check for mouse movement and button events
                bool isRelevantMessage = 
                    message == WM_MOUSEMOVE ||
                    message == WM_LBUTTONDOWN || message == WM_LBUTTONUP ||
                    message == WM_RBUTTONDOWN || message == WM_RBUTTONUP ||
                    message == WM_MBUTTONDOWN || message == WM_MBUTTONUP ||
                    message == WM_MOUSEWHEEL || message == WM_MOUSEHWHEEL ||
                    message == WM_XBUTTONDOWN || message == WM_XBUTTONUP;

                if (isRelevantMessage)
                {
                    // Get the extra info from the message
                    MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    
                    // Check if this is NOT our synthetic input
                    if (hookStruct.dwExtraInfo != _syntheticInputMarker)
                    {
                        _lastRealInputTime = (uint)Environment.TickCount;
                    }
                }
            }
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        // Structure for low-level hooks
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        // Initialize the hooks
        public static void InitializeInputHooks()
        {
            if (_keyboardHook == IntPtr.Zero)
            {
                IntPtr moduleHandle = GetModuleHandle(null);
                LowLevelKeyboardProc keyboardDelegate = KeyboardHookProc;
                _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, Marshal.GetFunctionPointerForDelegate(keyboardDelegate), moduleHandle, 0);
            }
            
            if (_mouseHook == IntPtr.Zero)
            {
                IntPtr moduleHandle = GetModuleHandle(null);
                LowLevelMouseProc mouseDelegate = MouseHookProc;
                _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, Marshal.GetFunctionPointerForDelegate(mouseDelegate), moduleHandle, 0);
            }
            
            // Initialize with current tick count
            if (_lastRealInputTime == 0)
            {
                _lastRealInputTime = (uint)Environment.TickCount;
            }

            // Initialize input language with current setting
            _lastLanguage = GetLanguageCodeFromKeyboardLayout(GetKeyboardLayout(0));
            InputLanguageChanged?.Invoke(_lastLanguage);
        }

        // Cleanup hooks
        public static void CleanupInputHooks()
        {
            if (_keyboardHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
            }
            
            if (_mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }
        }

        // Get time of last REAL user input (not synthetic)
        public static uint GetLastRealInputTime()
        {
            return _lastRealInputTime;
        }

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
                        dwExtraInfo = _syntheticInputMarker // Mark as synthetic
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
                        dwExtraInfo = _syntheticInputMarker // Mark as synthetic
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
                        dwFlags = dwFlags | MOUSEEVENTF_MOVE_NOCOALESCE, // MOUSEEVENTF_MOVE_NOCOALESCE
                        time = 0,
                        dwExtraInfo = _syntheticInputMarker // Mark as synthetic so our hook can ignore it
                    }
                }
            };

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        // Constants for mouse input
        public const uint MOUSEEVENTF_MOVE = 0x0001;
        public const uint MOUSEEVENTF_MOVE_NOCOALESCE = 0x2000;

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

        /// <summary>
        /// Получает код языка активной раскладки клавиатуры для текущего активного окна.
        /// Возвращает "EN", "RU" или код языка (например, "UK" для украинского).
        /// </summary>
        public static string GetCurrentInputLanguage()
        {
            try
            {
                // Get keyboard layout for the system (ID 0)
                // This always returns the current active keyboard layout
                IntPtr keyboardLayout = GetKeyboardLayout(0);
                string langCode = GetLanguageCodeFromKeyboardLayout(keyboardLayout);

                // Raise event if language changed
                if (_lastLanguage != langCode)
                {
                    _lastLanguage = langCode;
                    InputLanguageChanged?.Invoke(langCode);
                }

                return langCode;
            }
            catch
            {
                return _lastLanguage;
            }
        }

        /// <summary>
        /// Converts keyboard layout handle to language code string.
        /// </summary>
        private static string GetLanguageCodeFromKeyboardLayout(IntPtr keyboardLayout)
        {
            uint langId = (uint)(keyboardLayout.ToInt64() & 0xFFFF);
            
            return langId switch
            {
                0x0409 => "EN",  // English (US)
                0x0419 => "RU",  // Russian
                0x0422 => "UA",  // Ukrainian
                0x041D => "SE",  // Swedish
                0x0407 => "DE",  // German
                0x040C => "FR",  // French
                0x0410 => "IT",  // Italian
                0x0809 => "UK",  // English (UK)
                0x0415 => "PL",  // Polish
                0x0418 => "RO",  // Romanian
                0x0424 => "SI",  // Slovenian
                0x041A => "HR",  // Croatian
                0x042E => "ET",  // Estonian
                0x0426 => "LV",  // Latvian
                0x0423 => "LT",  // Lithuanian
                _ => langId.ToString("X4") // Return hex code if language is unknown
            };
        }

        private static string _lastLanguage = "";

    }
}