using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModTool.User.Templates
{
    public record NewFileCategoryTemplate
    {
        public string Name { get; init; }
        public string FilterTag { get; init; }
        public string LooseFilterTag { get; init; }
    }
}
