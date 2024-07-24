using Microsoft.VisualBasic.FileIO;
using Shell32;
using System;
using System.IO;

namespace ModTool.User.Commands
{
    public enum FileType
    {
        File,
        Directory
    }

    [Serializable]
    public class DeleteCommand : ICommand
    {
        public string FullFileName { get; set; }
        public FileType FileType { get; set; }
        public string CommandClassName => this.GetType().Name;

        public void Execute()
        {
            switch(FileType)
            {
                case FileType.File:
                    FileSystem.DeleteFile(FullFileName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    break;
                case FileType.Directory:
                    FileSystem.DeleteDirectory(FullFileName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    break;
            }
        }

        public void Undo()
        {
            this.Restore(this.FullFileName);
        }

        private bool Restore(string Item)
        {
            var Shl = new Shell();
            Folder Recycler = Shl.NameSpace(10);
            for (int i = 0; i < Recycler.Items().Count; i++)
            {
                FolderItem FI = Recycler.Items().Item(i);
                string FileName = Recycler.GetDetailsOf(FI, 0);
                if (Path.GetExtension(FileName) == "")
                    FileName += Path.GetExtension(FI.Path);
                // Necessary for systems with hidden file extensions.
                string FilePath = Recycler.GetDetailsOf(FI, 1);
                if (Item == Path.Combine(FilePath, FileName))
                {
                    DoVerb(FI, "ESTORE");
                    return true;
                }
            }
            return false;
        }

        private static bool DoVerb(FolderItem Item, string Verb)
        {
            foreach (FolderItemVerb FIVerb in Item.Verbs())
            {
                if (FIVerb.Name.Contains(Verb, StringComparison.CurrentCultureIgnoreCase))
                {
                    FIVerb.DoIt();
                    return true;
                }
            }
            return false;
        }
    }
}
