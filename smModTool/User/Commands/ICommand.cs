namespace ModTool.User.Commands
{
    public interface ICommand
    {
        string CommandClassName { get; }
        void Execute();
        void Undo();
    }
}
