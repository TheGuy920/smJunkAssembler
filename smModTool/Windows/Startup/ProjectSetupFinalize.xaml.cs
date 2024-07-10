using Microsoft.Win32;
using ModTool.Languages;
using ModTool.User;
using ModTool.User.Project;
using ModTool.User.Templates;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Wpf.Ui.Controls;

namespace ModTool.Windows.Startup
{
    /// <summary>
    /// Interaction logic for ProjectSetupFinalize.xaml
    /// </summary>
    public partial class ProjectSetupFinalize : Page
    {
        private ProjectTemplateItem projectTemplate;

        public ProjectSetupFinalize(ProjectTemplateItem projectTemplate)
        {
            this.InitializeComponent();
            this.ChangeTemplate(projectTemplate);

            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string basePath = Path.Combine(appdata, "Axolot Games", "Scrap Mechanic", "User");
            string modPath = Path.Combine(Directory.GetDirectories(basePath).First(), "Mods");

            this.SetCreationPath(modPath);
        }

        private void BackButtonClick(object sender, RoutedEventArgs e)
        {
            this.NextButton.IsEnabled = false;
            this.NextButton.Opacity = 0.2;

            var frame = StartupWindow.FindParentFrame(this);
            frame.GoBack();
        }

        public void ChangeTemplate(ProjectTemplateItem projectTemplate)
        {
            this.projectTemplate = projectTemplate;
            this.ProjTempName.Text = this.projectTemplate.Name;

            this.TemplateTags.Children.Clear();
            foreach(string tag in projectTemplate.Tags)
            {
                this.TemplateTags.Children.Add(new Card()
                {
                    Padding = new Thickness(7, 3, 7, 3),
                    Margin = new Thickness(5, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Content = tag
                });
            }
        }

        /// <summary>
        /// Initializes the template in the mod path
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            string baseDir = this.CreationPath;
            string modName = this.ModNameTb.Text.Length > 0 ? this.ModNameTb.Text : this.ModNameTb.PlaceholderText;
            Directory.CreateDirectory(baseDir);
            Guid modUuid = Guid.NewGuid();

            string desc = new User.Templates.Description.Default()
            {
                Name = modName,
                Description = this.ProjectDescriptionTb.Text,
                AllowAddMods = true,
                CustomIcons = false,
                Type = this.projectTemplate.ModType,
                Version = 1,
                Uuid = modUuid
            }.ToJson();

            File.WriteAllText(Path.Combine(baseDir, "description.json"), desc);

            var img = Utility.LoadInternalFile.BinaryFile("preview.jpg");
            File.WriteAllBytes(Path.Combine(baseDir, "preview.jpg"), img);

            FilePopulator.PopulateByType(baseDir, this.projectTemplate.ModType);

            new ModToolWindow(baseDir).Show();
            Window.GetWindow(this)?.Close();
        }

        private void SelectFolder(object sender, RoutedEventArgs e)
        {
            string dir = Directory.GetParent(this.CreationPath).FullName;
            OpenFolderDialog openFolderDialog = new() 
            { 
                Title = Strings.SelectFolder,
                RootDirectory = Directory.GetParent(dir).Parent.FullName,
                InitialDirectory = dir,
                Multiselect = false,
            };

            if (openFolderDialog.ShowDialog() == true)
                this.SetCreationPath(openFolderDialog.FolderName);
        }

        private void SetCreationPath(string fullPath)
        {
            string name = (this.ModNameTb.Text.Length > 0 ? this.ModNameTb.Text : this.ModNameTb.PlaceholderText)
                .Trim().Replace(" ", "");
            string fpath = Path.Combine(fullPath, name);
            if (Directory.Exists(fpath))
                fpath += "1";

            this.CreationPathTb.Text = fpath.Replace("Matthew", "Junk");
        }

        private string CreationPath => this.CreationPathTb.Text.Replace("Junk", "Matthew");

        private void OpenFolder(object sender, RoutedEventArgs e) =>
            Process.Start("explorer.exe", Directory.GetParent(this.CreationPath).FullName);

        private void ModNameTextChanged(object sender, TextChangedEventArgs e) =>
            this.SetCreationPath(Directory.GetParent(this.CreationPath).FullName);
    }
}
