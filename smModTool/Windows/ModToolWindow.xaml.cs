using AvalonDock.Layout;
using AvalonDock.Themes;
using CustomExtensions;
using ICSharpCode.AvalonEdit;
using ModTool.CustomXML;
using ModTool.Font;
using ModTool.Languages;
using ModTool.ScrapMechanic.Json;
using ModTool.User;
using ModTool.User.Controls;
using ModTool.User.Controls.Overview;
using ModTool.Util;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using TextBlock = Wpf.Ui.Controls.TextBlock;

namespace ModTool.Windows
{
    /// <summary>
    /// Interaction logic for ModToolWindow.xaml
    /// </summary>
    public partial class ModToolWindow
    {
        public readonly string SteamPath;
        public readonly string GamePath;
        public readonly bool Installed;
        public readonly bool Running;
        public readonly bool Updating;
        public readonly string SteamDisplayName;
        public readonly string SteamLanguage;
        public readonly string GitPath;

        public readonly ProjectConfiguration userConfiguration = new();

        public readonly ConcurrentDictionary<string, LayoutDocument> OpenDocuments = [];
        public readonly ConcurrentDictionary<string, FontInfo> Fonts = [];
        public readonly ConcurrentDictionary<string, string> SMNamingMap = [];

        private bool runModeDropdownMenuIsOpen;
        private bool targetProjectDropdownMenuIsOpen;

        public DirectoryInfo ProjectDirectory { get; private set; }
        private XmlTextHandler tmp;

        private SolutionExplorer FileExplorer { get; set; }
        private LayoutDocumentPane ActiveDocumentPane { get; set; }
        private LayoutPanel MainDocVerticalStack { get; set; }
        private LayoutAnchorablePane RightSideVerticalStack { get; set; }
        private LayoutAnchorablePane MainDocBottomStack { get; set; }

        public ModToolWindow(string baseDir = null)
        {
            this.InitializeComponent();
            this.InitializeControls();

            this.SteamPath = Utility.GetRegVal<string>("Software\\Valve\\Steam", "SteamPath").ToValidPath();
            this.Installed = Utility.GetRegVal<bool>("Software\\Valve\\Steam\\Apps\\387990", "Installed");
            this.Updating = Utility.GetRegVal<bool>("Software\\Valve\\Steam\\Apps\\387990", "Updating");
            this.Running = Utility.GetRegVal<bool>("Software\\Valve\\Steam\\Apps\\387990", "Running");
            this.SteamDisplayName = Utility.GetRegVal<string>("Software\\Valve\\Steam", "LastGameNameUsed");
            this.SteamLanguage = Utility.GetRegVal<string>("Software\\Valve\\Steam", "Language").CapitilzeFirst();
            this.GamePath = Path.Combine(this.SteamPath, "steamapps", "common", "Scrap Mechanic");
            this.GitPath = ControlUtil.FindCommandSource("git");

            if (!Directory.Exists(this.GamePath))
            {
                this.GamePath = File.ReadAllText(Path.Combine(this.SteamPath, "steamapps", "libraryfolders.vdf"))
                                    .Split("\"")
                                    .Select(path => Path.Combine(path, "steamapps", "common", "Scrap Mechanic"))
                                    .First(Directory.Exists);
            }

            if (!Directory.Exists(this.GamePath))
                throw new Exception("Scrap Mechanic Not Found");


            this.GamePath = this.GamePath.Replace("\\\\", "\\");
            /*
            this.Fonts = FontInfo.LoadFontInformation(this.GamePath);
            
            this.SMNamingMap = Utility.LoadInternalFile.TextFile("InterfaceTags.txt")
                                      .Split(Environment.NewLine)
                                      .Where(line => line.Contains(' '))
                                      .Select(line => (line, index: line.IndexOf(' ')))
                                      .Select(i => (line: i.line[..i.index], index: i.line[i.index..]))
                                      .Select(i => ($"#{{{i.line.Trim()}}}", i.index.Trim()))
                                      .ToDictionary();
            */

            // Load viewable windows into the ViewMenuTab
            for (int i = 0; i < 13; i++)
            {
                MenuItem menuItem = new()
                {
                    Header = new TextBlock() { FontSize = 12, Text = $"{Strings.Window} {i}" },
                    Icon = new SymbolIcon()
                    {
                        Symbol = SymbolRegular.Window16,
                        Margin = new Thickness(0, -20, 10, 0),
                        RenderTransform = new ScaleTransform(1.5, 1.5)
                    }
                };
                this.ViewMenuTab.Items.Add(menuItem);
            }

            if (baseDir is not null)
                this.InitDirectory(baseDir);
        }

        private void InitDirectory(string baseDir)
        {
            Task.Run(() =>
            {
                this.ProjectDirectory = new DirectoryInfo(baseDir);

                var desc = File.ReadAllText(Path.Combine(baseDir, "description.json"));
                var jdesc = JsonParser.ParseJObject(desc);
                string name = jdesc["name"].GetString();

                this.Dispatcher.Invoke(() =>
                {
                    this.TargetProjectNameHolder.Text = name;
                    this.SolutionNameTb.Text = name;

                    var projectSelector = new MenuItem() { Header = name, FontSize = 16 };
                    //projectSelector.Click += (s, e) => this.RunCurrentTargetProject(s, null);
                    this.TargetProjectDropdownMenu.Items.Add(projectSelector);

                    this.LoadDefaultWindows();
                });
            });
        }

        private void LoadDefaultWindows()
        {
            this.RightSideVerticalStack = new AvalonDock.Layout.LayoutAnchorablePane() { DockWidth = new GridLength(250) };
            this.FileExplorer = new SolutionExplorer(this.ProjectDirectory.FullName,
                    this.FindResource("SimpleExpanderTemp") as ControlTemplate,
                    this.OpenFile);
            this.RightSideVerticalStack.Children.Add(new LayoutAnchorable()
            {
                Title = Strings.FileExplorer,
                Content = this.FileExplorer
            });

            this.MainDocBottomStack = new AvalonDock.Layout.LayoutAnchorablePane() { DockHeight = new GridLength(250) };
            this.ActiveDocumentPane = new LayoutDocumentPane();

            this.MainDocVerticalStack = new LayoutPanel() { Orientation = Orientation.Vertical };
            this.MainDocVerticalStack.Children.Add(this.ActiveDocumentPane);
            this.MainDocVerticalStack.Children.Add(this.MainDocBottomStack);

            var horizontalPanel = new LayoutPanel() { Orientation = Orientation.Horizontal };
            horizontalPanel.Children.Add(this.MainDocVerticalStack);
            horizontalPanel.Children.Add(this.RightSideVerticalStack);

            this.DockLayoutRoot.RootPanel.DockWidth = new GridLength(1, GridUnitType.Star);
            this.DockLayoutRoot.RootPanel.Children.Add(horizontalPanel);

            Task.Run(() => this.OpenFile("description.json")).ContinueWith(async _ =>
            {

                await this.Dispatcher.BeginInvoke(() =>
                    this.ActiveDocumentPane.Children.Add(
                        DefaultPageMaker.CreateDebug(this.ProjectDirectory, this.OpenFile)
                ));
                /*
                await this.Dispatcher.BeginInvoke(() =>
                    this.ActiveDocumentPane.Children.Add(
                        DefaultPageMaker.CreateSteamPreview(this.ProjectDirectory)
                ));
                
                await this.Dispatcher.BeginInvoke(() =>
                    this.ActiveDocumentPane.Children.Add(
                        DefaultPageMaker.CreateBlenderWindow(this.ProjectDirectory)
                ));

                await this.Dispatcher.BeginInvoke(() =>
                    this.ActiveDocumentPane.Children.Add(
                        DefaultPageMaker.CreateKritaWindow(this.ProjectDirectory)
                ));

                await this.Dispatcher.BeginInvoke(() =>
                    this.ActiveDocumentPane.Children.Add(
                        DefaultPageMaker.CreatePaintWindow(this.ProjectDirectory)
                ));

                await this.Dispatcher.BeginInvoke(() =>
                    this.ActiveDocumentPane.Children.Add(
                        DefaultPageMaker.CreateMayaWindow(this.ProjectDirectory)
                ));
                
                await this.Dispatcher.BeginInvoke(() =>
                    this.ActiveDocumentPane.Children.Add(
                        DefaultPageMaker.CreateShitGameWindow(this.ProjectDirectory)
                ));
                
                await this.Dispatcher.BeginInvoke(() =>
                    this.ActiveDocumentPane.Children.Add(
                        DefaultPageMaker.CreateTestWindow(this.ProjectDirectory)
                ));
                
                await this.Dispatcher.BeginInvoke(() =>
                    this.ActiveDocumentPane.Children.Add(
                        DefaultPageMaker.CreateGimpWindow(this.ProjectDirectory)
                ));*/
            });
        }

        #region OpenFileWindow
        private void OpenFile(string relativePath)
        {
            string path = Path.Combine(this.ProjectDirectory.FullName, relativePath);

            if (this.OpenDocuments.TryGetValue(relativePath, out LayoutDocument value))
            {
                this.Dispatcher.Invoke(() => value.IsSelected = true);
                return;
            }

            this.OpenDocuments[relativePath] = ExtensionDetector.DetectFileType(path) switch
            {
                ExtensionDetector.FileType.Lua => this.OpenTextFile(relativePath),
                ExtensionDetector.FileType.Json => this.OpenJsonFile(relativePath),
                ExtensionDetector.FileType.Image => this.OpenImageFile(relativePath),
                ExtensionDetector.FileType.Text => this.OpenTextFile(relativePath),
                ExtensionDetector.FileType.Xml => this.OpenXmlFile(relativePath),
                _ => this.OpenTextFile(relativePath)
            };

            this.OpenDocuments[relativePath].Closed += (s, e) => this.OpenDocuments.TryRemove(relativePath, out var _);

            this.Dispatcher.Invoke(this.DockManager.UpdateLayout);
        }

        private LayoutDocument OpenImageFile(string relpath)
        {
            string fname = Path.GetFileName(relpath);
            LayoutDocument doc = null;

            this.Dispatcher.Invoke(() =>
            {
                string path = Path.Combine(this.ProjectDirectory.FullName, relpath);
                var img = new System.Windows.Controls.Image()
                {
                    Source = LoadImage(path),
                    Stretch = Stretch.Uniform,
                };

                var d = new LayoutDocument() { Title = fname, Content = img, IsSelected = true };
                doc = d;
                this.ActiveDocumentPane.Children.Add(doc);
            });

            return doc;
        }

        public static BitmapImage LoadImage(string path)
        {
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad; // This will load the image into memory

            using (FileStream stream = File.OpenRead(path))
            {
                bitmap.StreamSource = stream;
                bitmap.EndInit(); // The image will be loaded here
            } // The FileStream is disposed, releasing the file resource

            bitmap.Freeze(); // Makes the image usable across threads and locks it in memory

            return bitmap;
        }

        private LayoutDocument TextFile(string relpath)
        {
            string fname = Path.GetFileName(relpath);
            var textbody = File.ReadAllText(Path.Combine(this.ProjectDirectory.FullName, relpath)) ?? string.Empty;
            bool ready = false;
            LayoutDocument doc = null;

            this.Dispatcher.Invoke(() =>
            {
                var (d, te) = this.SetupTextDocument(relpath);
                doc = d;
                doc.Title = fname;
                this.tmp = new XmlTextHandler(ref te, () =>
                {
                    if (ready && te.IsModified)
                        doc.Title = $"{fname}*";
                    else
                        doc.Title = fname;
                });
                this.tmp.UpdateText(textbody);
                this.ActiveDocumentPane.Children.Add(doc);
            });
            ready = true;

            return doc;
        }

        private LayoutDocument OpenXmlFile(string relpath) => this.TextFile(relpath);

        private LayoutDocument OpenTextFile(string relpath)
        {
            var d = this.TextFile(relpath);
            this.Dispatcher.Invoke(this.tmp.NoXSHD);
            return d;
        }

        private static readonly JsonSerializerOptions jOptions = new() 
        { 
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private static readonly JsonWriterOptions jsonWriter = new() 
        { 
            Indented = true,
        };


        private LayoutDocument OpenJsonFile(string relpath)
        {
            string fname = Path.GetFileName(relpath);
            string textbody = File.ReadAllText(Path.Combine(this.ProjectDirectory.FullName, relpath));

            try
            {
                var config = JsonDocument.Parse(textbody);
                // Do something for customizing json indentation
                textbody = JsonSerializer.Serialize(config.RootElement, jOptions);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
            }

            bool ready = false;
            LayoutDocument doc = null;

            this.Dispatcher.Invoke(() =>
            {
                var (d, te) = this.SetupTextDocument(relpath);
                doc = d;
                doc.Title = fname;
                this.tmp = new XmlTextHandler(ref te, () =>
                {
                    if (ready && te.IsModified)
                        doc.Title = $"{fname}*";
                    else
                        doc.Title = fname;
                });
                this.tmp.UpdateText(textbody);
                this.ActiveDocumentPane.Children.Add(doc);
                ready = true;

                this.tmp.ChangeXSHD("Json.xshd");
            });

            return doc;
        }

        private (LayoutDocument LD, TextEditor TE) SetupTextDocument(string relpath)
        {
            var te = new TextEditor()
            {
                Background = this.FindResource("ApplicationBackgroundBrush") as SolidColorBrush,
                Tag = relpath,
            };
            return (new LayoutDocument() { Content = te, IsSelected = true }, te);
        }
        #endregion


        private void InitializeControls()
        {
            this.RunModeDropdownMenu.PlacementTarget = this.RunModeDropdown;
            this.RunModeDropdownMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            this.RunModeDropdownMenu.Closed += (s, e) => this.runModeDropdownMenuIsOpen = false;

            this.RunModeDropdown.Click += (s, e) =>
            {
                this.RunModeDropdownMenu.IsOpen = !this.runModeDropdownMenuIsOpen;
                this.runModeDropdownMenuIsOpen = !this.runModeDropdownMenuIsOpen;
            };

            this.TargetProjectDropdownMenu.PlacementTarget = this.TargetProjectDropdown;
            this.TargetProjectDropdownMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            this.TargetProjectDropdownMenu.Closed += (s, e) => this.targetProjectDropdownMenuIsOpen = false;

            this.TargetProjectDropdown.Click += (s, e) =>
            {
                this.TargetProjectDropdownMenu.IsOpen = !this.targetProjectDropdownMenuIsOpen;
                this.targetProjectDropdownMenuIsOpen = !this.targetProjectDropdownMenuIsOpen;
            };

            this.DockManager.Theme = new Vs2013DarkTheme();
            this.HeaderControls.Children.OfType<Button>().ToList().ForEach(b => b.Click += this.SaveButtonClick);

            this.TargetProjectDropdownMenu.Items.Clear();
            this.DockLayoutRoot.RootPanel.Children.Clear();
        }

        private LayoutDocument GetActiveDocument() =>
            this.DockLayoutRoot.RootPanel.Descendents().OfType<LayoutDocument>().First(d => d.IsSelected);

        private void EntireWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Your code here
        }

        private void EntireWindowMouseMove(object sender, MouseEventArgs e)
        {
            // Your code here
        }

        private void EntireWindowContentRendered(object sender, EventArgs e)
        {
            // Your code here
        }

        private void WindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Your code here
            if (e.Key == Key.F5)
            {
                this.RunCurrentTargetProject(sender, null);
            }

            if (e.Key == Key.Escape)
            {
                this.targetProjectDropdownMenuIsOpen = false;
                this.runModeDropdownMenuIsOpen = false;
            }

            if (e.Key == Key.S && Keyboard.IsCtrlDown)
            {
                this.SaveButtonClick(null, null);
            }
        }

        private void WindowPreviewKeyUp(object sender, KeyEventArgs e)
        {
            // Your code here
        }

        private void RunCurrentTargetProject(object sender, RoutedEventArgs e)
        {
            // This will reset the state to ensure the dropdown does not open
            // Similar to e.Handled = true
            // Instead this will allow the click event to be handled by the button (animation)
            this.targetProjectDropdownMenuIsOpen = true;
            Button runButton = sender as Button;

            string workingDir = Path.Combine(this.GamePath, "Release");
            string appidPath = Path.Combine(workingDir, "steam_appid.txt");
            bool appidTxt = File.Exists(appidPath);

            if (!appidTxt)
                File.WriteAllText(appidPath, "387990");

            string exePath = Path.Combine(workingDir, "ScrapMechanic.exe");
            string debugFlag = this.userConfiguration.RunMode == RunMode.Debug ? "-dev" : string.Empty;

            //runButton.IsEnabled = false;
            this.TargetProjectRunDebugIcon.Opacity = 0.25;
            this.RunNoDebugIcon.Opacity = 0.25;
            this.TargetProjectDropdown.IsEnabled = false;
            this.RunNoDebugButton.IsEnabled = false;

            ProcessStartInfo processInfo = new()
            {
                FileName = exePath,
                Arguments = string.Join(' ', debugFlag),
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            Process scrapMechanicProcess = Process.Start(processInfo);
            scrapMechanicProcess.EnableRaisingEvents = true;
            scrapMechanicProcess.Exited += (s, e) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    //runButton.IsEnabled = true;
                    this.TargetProjectRunDebugIcon.Opacity = 1;
                    this.RunNoDebugIcon.Opacity = 1;
                    this.TargetProjectDropdown.IsEnabled = true;
                    this.RunNoDebugButton.IsEnabled = true;
                });
            };
        }

        private void ChangeToDebugClick(object sender, RoutedEventArgs e)
        {
            this.RunModeText.Text = Strings.Debug;
            this.userConfiguration.RunMode = RunMode.Debug;
        }

        private void ChangeToReleaseClick(object sender, RoutedEventArgs e)
        {
            this.RunModeText.Text = Strings.Release;
        }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            var adoc = this.GetActiveDocument();
            if (adoc.Content is TextEditor te)
            {
                string path = Path.Combine(this.ProjectDirectory.FullName, te.Tag.ToString());
                Stream fstream = File.Create(path);

                te.Save(fstream);
                fstream.Flush();
                fstream.Close();

                adoc.Title = Path.GetFileName(te.Tag.ToString());
            }
        }

        private void OpenGitCmd(object sender, RoutedEventArgs e)
        {
            /*
            int gitTerminalType = int.Parse((sender as FrameworkElement).Tag.ToString());
            string gitDir = Path.GetDirectoryName(this.GitPath);
            string gitParentDir = Directory.GetParent(gitDir).FullName;
            string gitCmdPath = Path.Combine(gitParentDir, "git-cmd.exe");
            string gitBashPath = Path.Combine(gitParentDir, "git-bash.exe");
            string gitPath = gitTerminalType == 0 ? gitCmdPath : gitBashPath;
            */
            string gitPath = $"cmd.exe";

            this.AddToBottomStack(new LayoutAnchorable()
            {
                Title = Strings.Window,
                Content = ControlUtil.CreateNewConsole(gitPath, $"/k cd /D \"{this.ProjectDirectory.FullName}\""),
                IsSelected = true,
            });
        }

        private void AddToBottomStack(LayoutAnchorable control)
        {
            if (this.MainDocBottomStack.Parent is null)
                this.MainDocVerticalStack.Children.Add(this.MainDocBottomStack);

            this.MainDocBottomStack.Children.Add(control);
        }

        private void OpenFileExplorer(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", this.ProjectDirectory.FullName);
        }

        private void NewProjectClick(object sender, RoutedEventArgs e)
        {
            new StartupWindow(StartPage.NewProject).Show();
            this.Close();
        }

        private static void EnsureDirectories(string fpath)
        {
            var dname = Path.GetDirectoryName(fpath);
            if (Directory.Exists(dname))
                return;

            string tpath = string.Empty;
            fpath.Split(Path.DirectorySeparatorChar).ToList().ForEach(dir =>
            {
                tpath = Path.Combine(tpath, dir);
                if (tpath == fpath)
                    return;
                if (!Directory.Exists(tpath))
                    Directory.CreateDirectory(tpath);
            });
        }

        private void NewFileClick(object sender, RoutedEventArgs e)
        {
            var win = new NewFileTemplateSelector();
            if (win.ShowDialog() == true)
            {
                string relativeFilePath = win.FileName;
                string fullPath = Path.Combine(this.ProjectDirectory.FullName, relativeFilePath);

                EnsureDirectories(fullPath);
                File.WriteAllText(fullPath, win.FileContent);
                this.OpenFile(relativeFilePath);
            }
        }
    }
}
