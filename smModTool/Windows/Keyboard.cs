using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _Keyboard = System.Windows.Input.Keyboard;
using System.Windows.Input;

namespace ModTool.Windows
{
    class Keyboard
    {
        public static bool IsCtrlDown => _Keyboard.IsKeyDown(Key.LeftCtrl) || _Keyboard.IsKeyDown(Key.RightCtrl);
        public static bool IsShiftDown => _Keyboard.IsKeyDown(Key.LeftShift) || _Keyboard.IsKeyDown(Key.RightShift);
    }
}
