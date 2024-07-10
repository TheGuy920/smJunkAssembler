using ModTool.User.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModTool.User.Project
{
    public record ProjectTemplateItem
    {
        public string Name { get; set; }
        public string[] Tags { get; set; }
        public string ImagePath { get; set; }
        public string Description { get; set; }

        public ProjectTemplateType TemplateType { get; set; }
        public Description.ValidModType ModType { get; set; }
    }
    
    public static class BurnedProjectBuilder
    {
        public static ProjectTemplateItem BuildBurnedProject(ProjectTemplateType type)
            => type switch
            {
                ProjectTemplateType.BlocksAndParts => new ProjectTemplateItem
                {
                    Name = Languages.Strings.BlocksNParts,
                    Tags = [
                        Languages.Strings.Lua,
                        Languages.Strings.Json,
                        Languages.Strings.Creative,
                        Languages.Strings.Survival
                    ],
                    ImagePath = "pack://application:,,,/ModTool;component/Resources/ProjectTemplates/BlocksAndParts.png",
                    Description = Languages.Strings.BlocksNPartsDesc,
                    TemplateType = ProjectTemplateType.BlocksAndParts,
                    ModType = Description.ValidModType.BlocksAndParts
                },
                ProjectTemplateType.CustomGame => new ProjectTemplateItem
                {
                    Name = Languages.Strings.CustomGame,
                    Tags = [
                        Languages.Strings.Lua,
                        Languages.Strings.Json,
                        Languages.Strings.Creative,
                        Languages.Strings.Survival
                    ],
                    ImagePath = "pack://application:,,,/ModTool;component/Resources/ProjectTemplates/CustomGame.png",
                    Description = Languages.Strings.CustomGameDesc,
                    TemplateType = ProjectTemplateType.CustomGame,
                    ModType = Description.ValidModType.CustomGame
                },
                ProjectTemplateType.TerrainAssets => new ProjectTemplateItem
                {
                    Name = Languages.Strings.TerrainAssets,
                    Tags = [
                        Languages.Strings.Json,
                        Languages.Strings.Creative
                    ],
                    ImagePath = "pack://application:,,,/ModTool;component/Resources/ProjectTemplates/TerrainAssets.png",
                    Description = Languages.Strings.TerrainAssetsDesc,
                    TemplateType = ProjectTemplateType.TerrainAssets,
                    ModType = Description.ValidModType.TerrainAssets
                },
                _ => throw new NotImplementedException()
            };
    }
}
