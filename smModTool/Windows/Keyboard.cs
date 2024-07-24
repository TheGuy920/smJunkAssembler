using System.Windows.Input;
using _Keyboard = System.Windows.Input.Keyboard;

namespace ModTool.Windows
{
    class Keyboard
    {
        public static bool IsCtrlDown => _Keyboard.IsKeyDown(Key.LeftCtrl) || _Keyboard.IsKeyDown(Key.RightCtrl);
        public static bool IsShiftDown => _Keyboard.IsKeyDown(Key.LeftShift) || _Keyboard.IsKeyDown(Key.RightShift);
    }
}
