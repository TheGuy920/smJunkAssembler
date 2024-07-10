using AvalonDock.Layout;
using MahApps.Metro.Controls;
using ModTool.Languages;
using ModTool.User.Controls.DefaultPages;
using ModTool.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using WindowWeasel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ModTool.User.Controls.Overview
{
    public static class DefaultPageMaker
    {
        public static LayoutDocument CreateSteamPreview(DirectoryInfo dir)
        {
            JObject json = JObject.Parse(File.ReadAllText(Path.Combine(dir.FullName, "description.json")));
            string title = json["name"].Value<string>();
            
            FlowDocument document = FlowdocumentGenerator.GenerateFlowDocument(json["description"].Value<string>());
            SteamPage page = new(dir)
            {
                Background = Brushes.Transparent,
                HeaderTitle = Strings.SteamPg,
                HeaderIcon = new Wpf.Ui.Controls.SymbolIcon(Wpf.Ui.Controls.SymbolRegular.AppGeneric24)
            };

            var dviewer = new RichTextBox()
            {
                Document = document,
                Style = null,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                IsReadOnly = true,
                IsDocumentEnabled = true,
                SelectionTextBrush = Brushes.Red,
                AutoWordSelection = false,
                IsInactiveSelectionHighlightEnabled = true,
                CaretBrush = Brushes.White,
            };

            void onload(object s, RoutedEventArgs e)
            {
                dviewer?.Focus();
                dviewer.Loaded -= onload;
            }

            dviewer.Loaded += onload;
            page.Children.Add(new Border()
            {
                Background = new SolidColorBrush(Color.FromRgb(27, 40, 56)),
                CornerRadius = new CornerRadius(5),
                BorderBrush = Brushes.Transparent,
            });
            
            ScrollViewer viewer = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
            StackPanel stack = new() { HorizontalAlignment = HorizontalAlignment.Center };
            Image prev = new() { MaxWidth = 630, Margin = new Thickness(0, 20, 0, 30) };
            
            stack.Children.Add(prev);
            stack.Children.Add(dviewer);
            viewer.Content = stack;
            page.Children.Add(viewer);

            page.PreviewImageLoaded += () =>
            {
                prev.Source = page.PreviewImage.Source;
                prev.Width = page.PreviewImage.Source.Width;
                Task.Delay(100).ContinueWith(_ => viewer.Dispatcher.Invoke(() => viewer.ScrollToTop()));
            };

            return new LayoutDocument()
            {
                Title = Strings.SteamPreview,
                Content = page,
                IsSelected = true,
            };
        }

        public static LayoutDocument CreateDebug(DirectoryInfo dir)
        {
            return new LayoutDocument()
            {
                Title = Strings.Debug,
                Content = PageWrapper.New<DebugPage>([dir]),
                IsSelected = true,
            };
        }

        public static LayoutDocument CreateBlenderWindow(DirectoryInfo dir)
            => CreateTitleWindow("Blender", @"C:\Program Files\Blender Foundation\blender-4.0.2-windows-x64\blender.exe");

        public static LayoutDocument CreateMayaWindow(DirectoryInfo projectDirectory)
            => CreateTitleWindow(Strings.Maya, @"E:\Programs\Autodesk\Maya2024\bin\maya.exe");

        public static LayoutContent CreateKritaWindow(DirectoryInfo projectDirectory)
            => CreateTitleWindow(Strings.Krita, @"E:\Programs\Krita (x64)\bin\krita.exe");

        public static LayoutDocument CreateGimpWindow(DirectoryInfo projectDirectory)
            => CreateTitleWindow(Strings.GIMP, @"E:\Programs\GIMP 2\bin\gimp-2.10.exe");

        public static LayoutDocument CreatePaintWindow(DirectoryInfo projectDirectory)
            => CreateTitleWindow(Strings.MsPaint, @"C:\Program Files\WindowsApps\Microsoft.Paint_11.2404.1020.0_x64__8wekyb3d8bbwe\PaintApp\mspaint.exe");

        public static LayoutContent CreateShitGameWindow(DirectoryInfo projectDirectory)
            => CreateTitleWindow("Shit Game", @"F:\SteamLibrary\steamapps\common\Scrap Mechanic\Release\ScrapMechanic.exe");

        public static LayoutContent CreateTestWindow(DirectoryInfo projectDirectory)
            => CreateTitleWindow("Test Window", @"C:\Windows\explorer.exe");

        private static LayoutDocument CreateTitleWindow(string title, string programPath)
        {
            var ldoc = new LayoutDocument()
            {
                Title = title,
                IsSelected = true,
                Content = ProcessWeasel.LaunchProcess(programPath),
            };

            return ldoc;
        }

        
    }
}
