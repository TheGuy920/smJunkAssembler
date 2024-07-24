using CustomExtensions;
using System.IO;

namespace ModTool.User.Templates
{
    internal class FilePopulator
    {
        public static void PopulateByType(string basedir, Description.ValidModType modType)
        {
            switch(modType)
            {
                case Description.ValidModType.BlocksAndParts:
                    PopulateBlocksAndParts(basedir);
                    break;
                case Description.ValidModType.CustomGame:
                    PopulateCustomGame(basedir);
                    break;
                case Description.ValidModType.TerrainAssets:
                    PopulateTerrainAssets(basedir);
                    break;
            }
        }

        private static void PopulateBlocksAndParts(string basedir)
        {
            string language = App.SteamConnection.SteamLanguage.CapitilzeFirst();

            Directory.CreateDirectory(Path.Combine(basedir, "Gui"));
            Directory.CreateDirectory(Path.Combine(basedir, "Cache"));

            Directory.CreateDirectory(Path.Combine(basedir, "Effects"));
            Directory.CreateDirectory(Path.Combine(basedir, "Objects"));

            Directory.CreateDirectory(Path.Combine(basedir, "Particles"));

            Directory.CreateDirectory(Path.Combine(basedir, "Gui", "Language"));
            Directory.CreateDirectory(Path.Combine(basedir, "Gui", "Language", language));
        }

        private static void PopulateCustomGame(string basedir)
        {
            string language = App.SteamConnection.SteamLanguage.CapitilzeFirst();

            Directory.CreateDirectory(Path.Combine(basedir, "Gui"));

            Directory.CreateDirectory(Path.Combine(basedir, "Cache"));
            Directory.CreateDirectory(Path.Combine(basedir, "Nodes"));
            Directory.CreateDirectory(Path.Combine(basedir, "Tools"));

            Directory.CreateDirectory(Path.Combine(basedir, "Scripts"));
            Directory.CreateDirectory(Path.Combine(basedir, "Effects"));
            Directory.CreateDirectory(Path.Combine(basedir, "Objects"));
            Directory.CreateDirectory(Path.Combine(basedir, "Terrain"));

            Directory.CreateDirectory(Path.Combine(basedir, "Textures"));
            Directory.CreateDirectory(Path.Combine(basedir, "Particles"));

            Directory.CreateDirectory(Path.Combine(basedir, "Blueprints"));
            Directory.CreateDirectory(Path.Combine(basedir, "Characters"));
            Directory.CreateDirectory(Path.Combine(basedir, "Kinematics"));

            Directory.CreateDirectory(Path.Combine(basedir, "Projectiles"));
            Directory.CreateDirectory(Path.Combine(basedir, "MeleeAttacks"));
            Directory.CreateDirectory(Path.Combine(basedir, "Harvestables"));
            Directory.CreateDirectory(Path.Combine(basedir, "ScriptableObjects"));

            Directory.CreateDirectory(Path.Combine(basedir, "Gui", "Language"));
            Directory.CreateDirectory(Path.Combine(basedir, "Gui", "Language", language));
        }

        private static void PopulateTerrainAssets(string basedir)
        {
            Directory.CreateDirectory(Path.Combine(basedir, "Terrain"));
            Directory.CreateDirectory(Path.Combine(basedir, "Textures"));
            Directory.CreateDirectory(Path.Combine(basedir, "Blueprints"));
            Directory.CreateDirectory(Path.Combine(basedir, "Kinematics"));
            Directory.CreateDirectory(Path.Combine(basedir, "Harvestables"));
        }
    }
}
