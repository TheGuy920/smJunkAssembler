using System;
using System.IO;

namespace ModTool.User.Commands
{
    [Serializable]
    public class RenameCommand : ICommand
    {
        public string FullName { get; set; }
        public string NewName { get; set; }
        public string CommandClassName => this.GetType().Name;

        public void Execute()
        {
            if (File.Exists(FullName))
            {
                File.Move(FullName, NewName);
            }
            else if (Directory.Exists(FullName))
            {
                Directory.Move(FullName, NewName);
            }
        }

        public void Undo()
        {
            if (File.Exists(NewName))
            {
                File.Move(NewName, FullName);
            }
            else if (Directory.Exists(NewName))
            {
                Directory.Move(NewName, FullName);
            }
        }
    }
}
