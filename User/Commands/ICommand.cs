using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModTool.User.Commands
{
    public interface ICommand
    {
        string CommandClassName { get; }
        void Execute();
        void Undo();
    }
}
