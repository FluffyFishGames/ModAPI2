using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using ModAPI.ViewModels;
namespace ModAPI.Utils
{
    public class Button
    {
        public static Dictionary<Key, UnityButton> KeyToButton = new Dictionary<Key, UnityButton>()
        {
            { Key.A, UnityButton.A },
            { Key.B, UnityButton.B },
            { Key.C, UnityButton.C },
            { Key.D, UnityButton.D },
            { Key.E, UnityButton.E },
            { Key.F, UnityButton.F },
            { Key.G, UnityButton.G },
            { Key.H, UnityButton.H },
            { Key.I, UnityButton.I },
            { Key.J, UnityButton.J },
            { Key.K, UnityButton.K },
            { Key.L, UnityButton.L },
            { Key.M, UnityButton.M },
            { Key.N, UnityButton.N },
            { Key.O, UnityButton.O },
            { Key.P, UnityButton.P },
            { Key.Q, UnityButton.Q },
            { Key.R, UnityButton.R },
            { Key.S, UnityButton.S },
            { Key.T, UnityButton.T },
            { Key.U, UnityButton.U },
            { Key.V, UnityButton.V },
            { Key.W, UnityButton.W },
            { Key.X, UnityButton.X },
            { Key.Y, UnityButton.Y },
            { Key.Z, UnityButton.Z },
            { Key.Up, UnityButton.UpArrow },
            { Key.Down, UnityButton.DownArrow },
            { Key.Left, UnityButton.LeftArrow },
            { Key.Right, UnityButton.RightArrow },
            { Key.Insert, UnityButton.Insert },
            { Key.Home, UnityButton.Home },
            { Key.PageUp, UnityButton.PageUp },
            { Key.PageDown, UnityButton.PageDown },
            { Key.Delete, UnityButton.Delete },
            { Key.Return, UnityButton.Return },
            { Key.Back, UnityButton.Backspace },
            { Key.D0, UnityButton.Alpha0 },
            { Key.D1, UnityButton.Alpha1 },
            { Key.D2, UnityButton.Alpha2 },
            { Key.D3, UnityButton.Alpha3 },
            { Key.D4, UnityButton.Alpha4 },
            { Key.D5, UnityButton.Alpha5 },
            { Key.D6, UnityButton.Alpha6 },
            { Key.D7, UnityButton.Alpha7 },
            { Key.D8, UnityButton.Alpha8 },
            { Key.D9, UnityButton.Alpha9 },
            { Key.F1, UnityButton.F1 },
            { Key.F2, UnityButton.F2 },
            { Key.F3, UnityButton.F3 },
            { Key.F4, UnityButton.F4 },
            { Key.F5, UnityButton.F5 },
            { Key.F6, UnityButton.F6 },
            { Key.F7, UnityButton.F7 },
            { Key.F8, UnityButton.F8 },
            { Key.F9, UnityButton.F9 },
            { Key.F10, UnityButton.F10 },
            { Key.F11, UnityButton.F11 },
            { Key.F12, UnityButton.F12 },
            { Key.F13, UnityButton.F13 },
            { Key.F14, UnityButton.F14 },
            { Key.F15, UnityButton.F15 },
            { Key.Tab, UnityButton.Tab },
            { Key.NumPad0, UnityButton.Keypad0 },
            { Key.NumPad1, UnityButton.Keypad1 },
            { Key.NumPad2, UnityButton.Keypad2 },
            { Key.NumPad3, UnityButton.Keypad3 },
            { Key.NumPad4, UnityButton.Keypad4 },
            { Key.NumPad5, UnityButton.Keypad5 },
            { Key.NumPad6, UnityButton.Keypad6 },
            { Key.NumPad7, UnityButton.Keypad7 },
            { Key.NumPad8, UnityButton.Keypad8 },
            { Key.NumPad9, UnityButton.Keypad9 },
            { Key.Divide, UnityButton.KeypadDivide },
            { Key.Subtract, UnityButton.KeypadMinus },
            { Key.Multiply, UnityButton.KeypadMultiply },
            { Key.OemPeriod, UnityButton.KeypadPeriod },
            { Key.Add, UnityButton.KeypadPlus },
            { Key.Decimal, UnityButton.KeypadPeriod },
            { Key.OemPipe, UnityButton.Pipe },
            { Key.OemQuotes, UnityButton.Quote },
            { Key.OemBackslash, UnityButton.Backslash },
            { Key.LeftAlt, UnityButton.LeftAlt },
            { Key.LeftCtrl, UnityButton.LeftControl },
            { Key.LeftShift, UnityButton.LeftShift },
            { Key.RightAlt, UnityButton.RightAlt },
            { Key.RightCtrl, UnityButton.RightControl },
            { Key.RightShift, UnityButton.RightShift }
        };

        public static UnityButton GetByKey(Key key)
        {
            if (KeyToButton.ContainsKey(key))
                return KeyToButton[key];
            System.Diagnostics.Debug.WriteLine(key);
            return UnityButton.None;
        }
    }
}
