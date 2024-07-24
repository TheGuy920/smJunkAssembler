using ModTool.Windows;

namespace ModTool.User
{
    public record ProjectConfiguration
    {
        public RunMode RunMode { get; set; } = RunMode.Debug;
    }
}
