using ModTool.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModTool.User
{
    public record ProjectConfiguration
    {
        public RunMode RunMode { get; set; } = RunMode.Debug;
    }
}
