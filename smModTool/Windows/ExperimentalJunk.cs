using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows;
using Panel = System.Windows.Forms.Panel;
using ModTool.Languages;
using AvalonDock.Layout;
using System.Windows.Forms.Integration;
using System.Windows.Media;

namespace ModTool.Windows
{
    /*class ExperimentalJunk : FrameworkElement
    {
        private Grid GimpDocumentPage = new();
        private IStolenWindow BlenderWindowWin32;
        private Grid BlenderWindow = new();

        private void Init()
        {
            // muahahah
            // this.BlenderWindowWin32 = 
            //     WindowStealer.StealProcessWindow(@"C:\Program Files\Blender Foundation\blender-4.0.2-windows-x64\blender.exe")
            //     //.SetupWindow(window => this.BlenderWindow. += window.OnShutdown)
            //     .SetupPanel(grid => this.BlenderWindow.Children.Add(grid))
            //     .AttachToPanel();


            /*
            var doc = new LayoutDocument() { Title = Strings.VisualStudioCode, IsSelected = true };

            string path = Utility.GetRegVal<string>("Software\\Classes\\Applications\\Code.exe\\shell\\open\\command", null)
                .Replace("%1", this.ProjectDirectory.FullName);

            WindowStealer.StealProcessByName(path,
                f => f.Contains(Strings.VisualStudioCode))
                .SetupWindow(window => this.Closing += window.OnShutdown)
                .SetupPanel(grid => doc.Content = grid)
                .AttachToPanel();

            var dpane = this.GetActiveDocumentPane;
            dpane.Children.Add(doc);
            *
            //this.ExperimentalWindowSelector();
        }

        private void ExperimentalWindowSelector()
        {
            this.GimpDocumentPage.ColumnDefinitions.Add(
                new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            this.GimpDocumentPage.ColumnDefinitions.Add(
                new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            this.GimpDocumentPage.ColumnDefinitions.Add(
                new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            this.GimpDocumentPage.RowDefinitions.Add(
                new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            this.GimpDocumentPage.RowDefinitions.Add(
                new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            this.GimpDocumentPage.RowDefinitions.Add(
                new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            this.GimpDocumentPage.RowDefinitions.Add(
                new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            this.GimpDocumentPage.Width = 500;
            this.GimpDocumentPage.Height = 550;

            this.GimpDocumentPage.HorizontalAlignment = HorizontalAlignment.Center;
            this.GimpDocumentPage.VerticalAlignment = VerticalAlignment.Center;

            for (int i = 0; i < 9; i++)
            {
                this.GimpDocumentPage.Children.Add(new ToggleButton()
                {
                    Content = new TextBlock()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.WrapWithOverflow
                    },
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                });
                Grid.SetRow(this.GimpDocumentPage.Children[i], i / 3);
                Grid.SetColumn(this.GimpDocumentPage.Children[i], i % 3);
            }

            var button = new Button()
            {
                Content = new TextBlock()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = "Load Selected Windows"
                },
                Height = 50,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            Grid.SetColumnSpan(button, 3);
            Grid.SetRow(button, 4);
            this.GimpDocumentPage.Children.Add(button);

            var doc = new LayoutDocument() { Title = Strings.WindowSelector, IsSelected = true, Content = this.GimpDocumentPage };
            //this.ActiveDocumentPane.Children.Add(doc);

            Task.Run(ExperimentalWindowSelectorTask, CancellationToken.None);
        }

        private async Task ExperimentalWindowSelectorTask()
        {
            //string program = @"C:\Program Files\WindowsApps\Microsoft.MSPaint_6.2310.24037.0_x64__8wekyb3d8bbwe\PaintStudio.View.exe";
            //string program = @"C:\Program Files\WindowsApps\Microsoft.Paint_11.2404.1020.0_x64__8wekyb3d8bbwe\PaintApp\mspaint.exe";
            //string program = @"E:\Programs\Paint.NET\paintdotnet.exe";
            //string program = @"E:\Programs\Krita (x64)\bin\krita.exe";
            string program = @"E:\Programs\Autodesk\Maya2024\bin\maya.exe";
            //string program = @"E:\Programs\GIMP 2\bin\gimp-2.10.exe";
            Process process = Process.Start(program);
            await Task.Delay(250);
            bool button_registered = false;
            bool button_clicked = false;

            //this.Closed += (s, e) => { process.Kill(); };

            while (!process.HasExited && !button_clicked)
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (this.GimpDocumentPage.Children[^1] is Button butt && !button_registered)
                    {
                        button_registered = true;
                        butt.Click += (s, e) =>
                        {
                            button_clicked = true;
                            this.LoadSelectedWindows();
                        };
                    }

                    int i = 0;
                    for (i = 0; i < 9; i++)
                        ((this.GimpDocumentPage.Children[i] as ToggleButton).Content as TextBlock)
                            .Text = string.Empty;

                    i = 0;
                    foreach (var winHndl in Win32WindowInterop.EnumerateProcessWindows(process.Id))
                    {
                        if (i >= 9)
                        {
                            Debug.WriteLine($"Skipped: {winHndl}: {process.ProcessName}: {Win32WindowInterop.GetWindowTitle(winHndl)}");
                            continue;
                        }

                        var button = this.GimpDocumentPage.Children[i] as ToggleButton;
                        (button.Content as TextBlock)
                            .Text = $"{winHndl}: {process.ProcessName}: {Win32WindowInterop.GetWindowTitle(winHndl)}";
                        i++;
                    }
                });
                await Task.Delay(500);
            }
        }

        private void LoadSelectedWindows()
        {
            var wnHndls = this.GimpDocumentPage.Children.Cast<UIElement>()
                .OfType<ToggleButton>()
                .Where(tb => tb.IsChecked == true)
                .Select(tb => tb.Content).OfType<TextBlock>()
                .Select(tb => tb.Text.Split(':')[0])
                .Select(nint.Parse).ToList();

            this.GimpDocumentPage.Children.Clear();
            //this.ActiveDocumentPane.Children.First(d => d.IsSelected).Close();
            this.GimpDocumentPage = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            this.GimpDocumentPage.Loaded += (j, u) =>
            {
                foreach (var wnHndl in wnHndls)
                {
                    var host = new WindowsFormsHost()
                    {
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                        Background = Brushes.Transparent
                    };
                    var panel = new Panel
                    {
                        Dock = System.Windows.Forms.DockStyle.Fill,
                        BackColor = System.Drawing.Color.Transparent
                    };

                    Debug.WriteLine($"Sizing: {this.GimpDocumentPage.ActualWidth}:{this.GimpDocumentPage.ActualHeight}");

                    Win32WindowInterop.SetParent(wnHndl, panel.Handle);
                    Win32WindowInterop.RemoveTitleBar(wnHndl);
                    //Win32WindowInterop.MakeWindowTransparent(wnHndl);
                    Win32WindowInterop.MoveWindow(wnHndl, 0, 0,
                        (int)this.GimpDocumentPage.ActualWidth,
                        (int)this.GimpDocumentPage.ActualHeight, true);

                    host.Child = panel;
                    this.GimpDocumentPage.Children.Add(host);

                    /*Win32WindowInterop.RegisterWindowMoveResizeEvent(wnHndl, () =>
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            Debug.WriteLine($"Sizing Event: {wnHndl}");
                            Win32WindowInterop.MoveWindow(wnHndl, 0, 0,
                                (int)this.GimpDocumentPage.ActualWidth,
                                (int)this.GimpDocumentPage.ActualHeight, true);
                        });
                    });*
                }
            };

            this.GimpDocumentPage.SizeChanged += (s, e) =>
            {
                foreach (var wnHndl in wnHndls)
                    Win32WindowInterop.MoveWindow(wnHndl, 0, 0, (int)e.NewSize.Width, (int)e.NewSize.Height, true);
            };

            var doc = new LayoutDocument() { Title = Strings.Maya, IsSelected = true, Content = this.GimpDocumentPage };
            //var dpane = this.ActiveDocumentPane;
            //dpane.Children.Add(doc);
        }
    }*/
}
