using CustomExtensions;
using ModTool.ScrapMechanic.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Wpf.Ui.Controls;
using static ModTool.User.Templates.Description;
using Button = Wpf.Ui.Controls.Button;
using TextBlock = System.Windows.Controls.TextBlock;

namespace ModTool.Windows.Startup
{
    /// <summary>
    /// Interaction logic for WelcomePage.xaml
    /// </summary>
    public partial class WelcomePage : Page
    {
        private readonly Dictionary<string, Button> Mods = [];
        private NewProjectPage newProjectPage;

        public WelcomePage()
        {
            InitializeComponent();

            this.LoadAvailableProjects();

            this.ExtraButtonsPanel.Children.Cast<UIElement>()
                .OfType<Button>().ToList().ForEach(b => 
                {
                    Border br = (b.Content as Grid).Children.Cast<UIElement>().First(c => c is Border) as Border;
                    var opac = br.Opacity;
                    b.MouseEnter += (s, e) =>
                    {
                        b.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 113, 96, 232));
                        br.Background = new SolidColorBrush(Color.FromArgb(255, 34, 29, 70));
                        br.Opacity = 1;
                    };
                    b.MouseLeave += (s, e) =>
                    {
                        b.BorderBrush = Brushes.Transparent;
                        br.Background = Brushes.White;
                        br.Opacity = opac;
                    };
                });
        }

        private void OpenNoCode(object sender, RoutedEventArgs e)
        {
            new ModToolWindow().Show();
            Window.GetWindow(this)?.Close();
        }

        private void NewProjectButtonClick(object sender, RoutedEventArgs e)
        {
            this.newProjectPage ??= new NewProjectPage();
            StartupWindow.FindParentFrame(this).Navigate(this.newProjectPage);
        }

        private void LoadAvailableProjects()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string basePath = Path.Combine(appdata, "Axolot Games", "Scrap Mechanic", "User");
            
            string modPath = Path.Combine($"User_{App.SteamConnection.SteamId}", "Mods");
            if (!Directory.Exists(modPath))
                modPath = Path.Combine(Directory.GetDirectories(basePath).First(), "Mods");

            var elements = Directory.GetDirectories(modPath)
                .Where(d => !Path.GetFileName(d).StartsWith('.'))
                .Where(p => Directory.GetFiles(p, "*", SearchOption.AllDirectories).Length > 0)
                .Where(p => File.Exists(Path.Combine(p, "description.json")))
                .ToDictionary(p => p, GetFolderActivityTime)
                .OrderByDescending(t => t.Value);
            
            this.TodayFiles.Children.Clear();
            this.YesterdayFiles.Children.Clear();
            this.ThisWeekFiles.Children.Clear();
            this.ThisMonthFiles.Children.Clear();

            this.Mods.Clear();
            foreach (var item in elements)
            {
                var desc = File.ReadAllText(Path.Combine(item.Key, "description.json"));
                var jdesc = JsonParser.ParseJObject(desc);

                ValidModType type = jdesc["type"].GetString().ToStringLowerInvariant() switch
                {
                    "blocks and parts" => ValidModType.BlocksAndParts,
                    "custom game" => ValidModType.CustomGame,
                    "terrain assets" => ValidModType.TerrainAssets,
                    _ => throw new ArgumentException("Invalid mod type")
                };

                string name = jdesc["name"].ToString();
                
                var mitem = BuildFileItem(name, item.Key, item.Value, type);
                this.Mods.Add(name, mitem);

                if ((DateTime.UtcNow - item.Value).Days == 0)
                    this.TodayFiles.Children.Add(mitem);
                else if ((DateTime.UtcNow - item.Value).Days == 1)
                    this.YesterdayFiles.Children.Add(mitem);
                else if ((DateTime.UtcNow - item.Value).Days < 7)
                    this.ThisWeekFiles.Children.Add(mitem);
                else
                    this.ThisMonthFiles.Children.Add(mitem);
            }

            this.SearchBox.OriginalItemsSource = this.Mods.Keys.ToList();
            this.TodayExpander.Visibility = this.TodayFiles.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            this.YesterdayExpander.Visibility = this.YesterdayFiles.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            this.ThisWeekExpander.Visibility = this.ThisWeekFiles.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            this.ThisMonthExpander.Visibility = this.ThisMonthFiles.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public static DateTime GetFolderActivityTime(string directoryPath)
        {
            var directory = new DirectoryInfo(directoryPath);

            // Get all files in the directory and subdirectories
            // var files = directory.GetFiles("*", SearchOption.AllDirectories);

            // Order the files by LastWriteTime and get the most recent one
            // return files.OrderByDescending(f => f.LastWriteTime).FirstOrDefault().LastWriteTime;

            return directory.LastWriteTime;
        }

        private static Button BuildFileItem(string name, string fpath, DateTime utc, ValidModType type)
        {
            DockPanel dock = new()
            {
                Margin = new Thickness(0),
                FlowDirection = FlowDirection.LeftToRight,
            };

            SymbolIcon fileSymbol = new()
            {
                Symbol = type switch 
                {
                    ValidModType.BlocksAndParts => SymbolRegular.WrenchScrewdriver20,
                    ValidModType.CustomGame => SymbolRegular.Album20,
                    ValidModType.TerrainAssets => SymbolRegular.TreeEvergreen20,
                    _ => SymbolRegular.Document48
                },
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(10, 5, 20, 0),
                Foreground = type switch
                {
                    ValidModType.BlocksAndParts => Brushes.DarkOrange,
                    ValidModType.CustomGame => Brushes.IndianRed,
                    ValidModType.TerrainAssets => Brushes.Green,
                    _ => Brushes.MediumPurple
                },
                RenderTransform = new ScaleTransform(1.75, 1.75)
            };
            DockPanel.SetDock(fileSymbol, Dock.Left);

            TextBlock date = new()
            {
                Text = utc.ToString("MM/dd/yy"),
                Margin = new Thickness(10,0,10,0)
            };
            TextBlock time = new() { Text = utc.ToString("h:mm tt"), };
            DockPanel.SetDock(time, Dock.Right);

            StackPanel stackPanelTime = new() 
            { 
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            stackPanelTime.Children.Add(date);
            stackPanelTime.Children.Add(time);

            TextBlock fileName = new() { Text = name };
            DockPanel.SetDock(fileName, Dock.Left);

            Grid grid = new() 
            { 
                Margin = new Thickness(0,0,20,0),
            };

            grid.Children.Add(fileName);
            grid.Children.Add(stackPanelTime);

            TextBlock path = new() 
            { 
                Text = fpath.Replace("Matthew", "Junk"),
                Margin = new Thickness(0,5,0,0),
                FontSize = 10.5,
                TextWrapping = TextWrapping.WrapWithOverflow,
                Opacity = 0.5
            };

            StackPanel stackPanel = new();
            stackPanel.Children.Add(grid);
            stackPanel.Children.Add(path);

            dock.Children.Add(fileSymbol);
            dock.Children.Add(stackPanel);

            Grid container = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent
            };
            container.Children.Add(dock);
            var border = new Border
            {
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(0,-10,0,-10),
            };
            container.Children.Add(border);

            Button button = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Padding = new Thickness(0, 10, 0, 10),
                Margin = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Content = container
            };
            dock.SetBinding(DockPanel.WidthProperty, new Binding("ActualWidth") { Source = button });
            button.Click += (s, e) =>
            {
                Button bt = s as Button;
                new ModToolWindow(fpath).Show();
                Window.GetWindow(bt)?.Close();
            };
            button.MouseEnter += (s, e) =>
            {
                border.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                border.Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));
            };
            button.MouseLeave += (s, e) =>
            {
                border.BorderBrush = Brushes.Transparent;
                border.Background = Brushes.Transparent;
            };
            return button;
        }

        private void SearchBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var list = this.Mods
                .Where(n => n.Key.Contains(sender.Text, StringComparison.CurrentCultureIgnoreCase))
                .ToList();
            
            foreach(var item in this.Mods)
                item.Value.Visibility = list.Contains(item) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
