using ICSharpCode.WpfDesign.Designer.Controls;
using ModTool.Languages;
using ModTool.User.Commands;
using ModTool.Windows;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;
using MenuItem = System.Windows.Controls.MenuItem;
using SearchOption = System.IO.SearchOption;
using TextBlock = System.Windows.Controls.TextBlock;
using TextBox = System.Windows.Controls.TextBox;

namespace ModTool.User.Controls
{
    internal class SolutionExplorer : Grid
    {
        private readonly ConcurrentDictionary<string, Border> FileIndicators = [];
        private readonly ConcurrentDictionary<string, Button> FileButtons = [];
        private readonly ConcurrentDictionary<string, CardExpander> FolderExpanders = [];
        private readonly List<Commands.ICommand> _actions = [];
        private readonly FileSystemWatcher fileSystemWatcher;
        private readonly ControlTemplate Template;
        private readonly Action<string> _openFile;
        private readonly string _path;

        private readonly System.Windows.Controls.ScrollViewer scrollViewer = new()
        {
            Margin = new System.Windows.Thickness(0, 50, 0, 0),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Brushes.Transparent
        };

        private readonly StackPanel container = new()
        {
            Margin = new System.Windows.Thickness(15, 0, 0, 50),
            Orientation = Orientation.Vertical,
            Background = Brushes.Transparent
        };

        public SolutionExplorer(string path, ControlTemplate temp, Action<string> openFile)
        {
            this._path = path;
            this._openFile = openFile;
            this.scrollViewer.Content = this.container;
            var search = SearchBar;
            search.TextChanged += SearchTextChanged;
            this.Children.Add(search);
            this.Children.Add(this.scrollViewer);
            this.Background = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));
            this.Template = temp;

            Task.Run(() => this.InitializeComponent(this.Template));

            this.fileSystemWatcher = new()
            {
                Path = path,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = "*",
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
            };

            this.fileSystemWatcher.Created += this.FileSystemItemChanged;
            this.fileSystemWatcher.Deleted += this.FileSystemItemChanged;
            this.fileSystemWatcher.Renamed += this.FileSystemItemChanged;
        }

        private void FileSystemItemChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                string relPath = Path.GetRelativePath(this._path, e.FullPath);

                if (this.FileIndicators.TryRemove(relPath, out var border))
                {
                    this.FileButtons.TryRemove(relPath, out _);
                    this.Dispatcher.Invoke(() => 
                    {
                        var dparent = border.Parent as FrameworkElement;
                        var parent = dparent.Parent as StackPanel;
                        parent.Children.Remove(dparent);
                    });
                }

                if (this.FolderExpanders.TryRemove(relPath, out var expander))
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        var parent = expander.Parent as StackPanel;
                        parent.Children.Remove(expander);
                    });
                }
            }

            if (e.ChangeType == WatcherChangeTypes.Renamed || e.ChangeType == WatcherChangeTypes.Created)
            {
                bool isFile = File.Exists(e.FullPath);
                bool isFolder = Directory.Exists(e.FullPath);
                string relPath = Path.GetRelativePath(this._path, e.FullPath);
                string name = Path.GetFileName(relPath);
                StackPanel stack = this.container;
                FrameworkElement element = null;

                if (isFile && !this.FileButtons.ContainsKey(relPath))
                {
                    string relDir = Path.GetDirectoryName(relPath);
                    if (relDir.Length > 0)
                    {
                        var asrt = this.FolderExpanders.TryGetValue(relDir, out var expander);
                        Debug.Assert(asrt);
                        this.Dispatcher.Invoke(() => stack = expander.Content as StackPanel);
                    }

                    int depth = relPath.Split(Path.DirectorySeparatorChar).Length - 1;
                    var (container, border, button) = this.BuildFileItem(e.FullPath, depth);
                    element = container;
                    this.FileIndicators.TryAdd(relPath, border);
                    this.FileButtons.TryAdd(relPath, button);
                }

                if (isFolder && !this.FolderExpanders.ContainsKey(relPath))
                {
                    string relDir = Path.GetDirectoryName(relPath);
                    if (relDir.Length > 0)
                    {
                        var asrt = this.FolderExpanders.TryGetValue(relDir, out var expander);
                        Debug.Assert(asrt);
                        this.Dispatcher.Invoke(() => stack = expander.Content as StackPanel);
                    }

                    int depth = relPath.Split(Path.DirectorySeparatorChar).Length - 1;
                    var card = this.BuildFolderItem(e.FullPath, this.Template, depth);
                    this.FolderExpanders.TryAdd(relPath, card);
                    element = card;
                }

                this.Dispatcher.Invoke(() =>
                {
                    var items = stack.Children.Cast<FrameworkElement>().ToList();
                    foreach (FrameworkElement item in items)
                    {
                        string itemPath = item.Tag.ToString();
                        string relPath = Path.GetRelativePath(this._path, itemPath);

                        if (!File.Exists(itemPath) && item is Grid)
                        {
                            stack.Children.Remove(item);
                            this.FileButtons.TryRemove(relPath, out _);
                            this.FileIndicators.TryRemove(relPath, out _);
                        }

                        if (!Directory.Exists(itemPath) && item is CardExpander)
                        {
                            stack.Children.Remove(item);
                            this.FolderExpanders.TryRemove(relPath, out _);
                        }
                    }

                    int i;
                    for (i = 0; i < stack.Children.Count;)
                    {
                        string tname = (stack.Children[i] as FrameworkElement).Tag.ToString();
                        int comparisonResult = String.Compare(name, Path.GetFileName(tname), StringComparison.OrdinalIgnoreCase);

                        if (comparisonResult < 0)
                        {
                            stack.Children.Insert(i, element);
                            break;
                        }

                        i++;
                    }

                    if (i == stack.Children.Count)
                        stack.Children.Add(element);

                    this.Children.RemoveAt(0);
                    var search = SearchBar;
                    search.TextChanged += SearchTextChanged;
                    this.Children.Insert(0, search);
                });
            }
        }

        private void SearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            List<string> list = [];
            foreach (var (n, button) in this.FileButtons)
            {
                button.Visibility = Path.GetFileName(n).Contains(sender.Text) ? Visibility.Visible : Visibility.Collapsed;
                if (button.Visibility == Visibility.Visible)
                    list.Add(n);
            }

            foreach (var (n, expander) in this.FolderExpanders)
            {
                expander.Visibility = Path.GetFileName(n).Contains(sender.Text) || list.Any(f => f.StartsWith(n))
                    ? Visibility.Visible : Visibility.Collapsed;

                if (expander.Visibility == Visibility.Visible)
                    expander.IsExpanded = true;
            }
        }

        private AutoSuggestBox SearchBar => new()
        {
            PlaceholderText = "Search",
            Margin = new System.Windows.Thickness(10),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            OriginalItemsSource = this.FileButtons.Keys.Select(Path.GetFileName)
                .Concat(this.FolderExpanders.Keys.Select(Path.GetFileName)).ToList()
        };

        private void InitializeComponent(ControlTemplate temp)
        {
            var items = this.BuildFileFolderStructure(this._path, temp);
            items.OrderBy(i => i.Name).OrderBy(i => i.Type).ToList().ForEach(i =>
                this.container.Dispatcher.Invoke(() => this.container.Children.Add(i.Elem)));
        }

        private IEnumerable<(string Name, FrameworkElement Elem, FileSystemType Type)> BuildFileFolderStructure
            (string path, ControlTemplate temp, int depth = 0)
        {
            var files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
            var folders = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                string name = Path.GetFileName(file);
                if (name.StartsWith('.'))
                    continue;

                string relPath = Path.GetRelativePath(this._path, file);
                var (container, border, butt) = this.BuildFileItem(file, depth);

                this.FileIndicators.TryAdd(relPath, border);
                this.FileButtons.TryAdd(relPath, butt);

                yield return (name, container, FileSystemType.File);
            }

            foreach (var folder in folders)
            {
                string name = Path.GetFileName(folder);
                if (name.StartsWith('.'))
                    continue;

                string relPath = Path.GetRelativePath(this._path, folder);
                var card = this.BuildFolderItem(folder, temp, depth);

                this.FolderExpanders.TryAdd(relPath, card);

                yield return (name, card, FileSystemType.Directory);
            }
        }

        private CardExpander BuildFolderItem(string folder, ControlTemplate temp, int depth)
        {
            StackPanel stack = null;
            CardExpander card = null;

            this.Dispatcher.Invoke(() => stack = new StackPanel() { Background = Brushes.Transparent, Margin = new Thickness(-20, 0, 0, 0) });

            foreach (var item in this.BuildFileFolderStructure(folder, temp, depth + 1))
                this.Dispatcher.Invoke(() => stack.Children.Add(item.Elem));

            this.Dispatcher.Invoke(() =>
            {
                var header = BuildHeader(new SymbolIcon
                {
                    Foreground = Brushes.Gold/*new SolidColorBrush(Color.FromRgb(255, 200, 40))*/,
                    Filled = true,
                    Symbol = SymbolRegular.Folder20,
                    Margin = new System.Windows.Thickness(15, 0, 0, 0),
                }, Path.GetFileName(folder), folder);

                card = new CardExpander
                {
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    Padding = new System.Windows.Thickness(20 * depth, 0, 0, 0),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                    Template = temp,
                    Header = header,
                    Tag = folder,
                    Content = stack
                };

                card.Expanded += (s, e) =>
                {
                    foreach (var (n, border) in this.FileIndicators)
                        border.Background = Brushes.Transparent;
                };

                card.Collapsed += (s, e) =>
                {
                    foreach (var (n, border) in this.FileIndicators)
                        border.Background = Brushes.Transparent;
                };

                card.ContextMenu = new ContextMenu()
                {
                    Margin = new Thickness(0),
                    Padding = new Thickness(0),
                    ItemsSource = this.GetStandardMenuOptions(header, folder)
                };
            });

            return card;
        }

        private (Grid, Border, Button) BuildFileItem(string file, int depth)
        {
            Grid container = null;
            Border border = null;
            Button butt = null;
            string relPath = Path.GetRelativePath(this._path, file);
            string name = Path.GetFileName(file);

            this.Dispatcher.Invoke(() =>
            {
                var grid = BuildHeader(new SymbolIcon
                {
                    Foreground = Brushes.MediumPurple,
                    Symbol = SymbolRegular.Document24,
                    Filled = true,
                    Margin = new System.Windows.Thickness(25 + (20 * depth), 0, 0, 0),
                }, name, file);

                butt = new Button()
                {
                    CornerRadius = new System.Windows.CornerRadius(0),
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    Padding = new System.Windows.Thickness(0),
                    Margin = new System.Windows.Thickness(4, 0, 4, 0),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Content = grid
                };

                container = new Grid() { Background = Brushes.Transparent, Tag = file };
                border = new Border()
                {
                    Opacity = 0.1,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.Transparent,
                    Margin = new Thickness(3, 0, 3, 0),
                };
                container.Children.Add(border);
                container.Children.Add(butt);

                void tmpFunc (object sender, object e)
                {
                    foreach (var (n, border) in this.FileIndicators)
                        border.Background = Brushes.Transparent;

                    this.FileIndicators.First(kv => kv.Key == relPath)
                        .Value.Background = Brushes.White;
                };

                butt.PreviewMouseLeftButtonDown += tmpFunc;
                butt.PreviewMouseRightButtonDown += tmpFunc;

                butt.PreviewMouseDoubleClick += (sender, e) =>
                {
                    this._openFile(relPath);
                };

                container.ContextMenu = new ContextMenu()
                {
                    Margin = new Thickness(0),
                    Padding = new Thickness(0),
                    Tag = file,
                    ItemsSource = new List<MenuItem>()
                    {
                        new()
                        {
                            Header = Strings.Open,
                            FontSize = 12,
                            Padding = new Thickness(0),
                            Margin = new Thickness(0),
                            Icon = new SymbolIcon()
                            {
                                Symbol = SymbolRegular.Document24,
                                Foreground = Brushes.MediumPurple,
                                Margin = new Thickness(-5,-8,3,0),
                                RenderTransform = new ScaleTransform(1.5, 1.5)
                            },
                            Command = new RelayCommand(() => this._openFile(relPath))
                        }
                    }.Concat(this.GetStandardMenuOptions(grid, file))
                };
            });

            return (container, border, butt);
        }

        private List<MenuItem> GetStandardMenuOptions(Grid gridItem, string item) => new()
        {
            new()
            {
                Header = Strings.Rename,
                FontSize = 12,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                Icon = new SymbolIcon()
                {
                    Symbol = SymbolRegular.Rename16,
                    Foreground = Brushes.CornflowerBlue,
                    Margin = new Thickness(-5,-8,3,0),
                    RenderTransform = new ScaleTransform(1.5, 1.5)
                },
                Command = new RelayCommand(() => this.RenameItem(gridItem))
            },
            new()
            {
                Header = Strings.Delete,
                FontSize = 12,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                Icon = new SymbolIcon()
                {
                    Symbol = SymbolRegular.Delete24,
                    Foreground = Brushes.IndianRed,
                    Margin = new Thickness(-5,-8,3,0),
                    RenderTransform = new ScaleTransform(1.5, 1.5)
                },
                Command = new RelayCommand(() => this.DeleteItem(item))
            },
            new()
            {
                Header = Strings.OpenInExplor,
                FontSize = 12,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                Icon = new SymbolIcon()
                {
                    Symbol = SymbolRegular.FolderOpen24,
                    Foreground = Brushes.Goldenrod,
                    Margin = new Thickness(-5,-8,3,0),
                    RenderTransform = new ScaleTransform(1.5, 1.5)
                },
                Command = new RelayCommand(() => Process.Start("explorer.exe", "/select, " + item))
            }
        };

        private void DeleteItem(string item)
        {
            bool isFile = File.Exists(item);
            var cmd = new DeleteCommand { FullFileName = item, FileType = isFile ? FileType.File : FileType.Directory };
            this._actions.Add(cmd);
            cmd.Execute();
        }

        private void RenameItem(Grid gridItem)
        {
            TextBlock tb = gridItem.Children.OfType<TextBlock>().First();
            string oldPath = tb.Tag.ToString();
            string oldName = tb.Text;
            tb.Visibility = Visibility.Collapsed;
            TextBox tbx = gridItem.Children.OfType<TextBox>().FirstOrDefault();
            tbx.Text = oldName;
            tbx.Visibility = Visibility.Visible;
            tbx.Focus();
            tbx.SelectAll();

            void doRename(object s, RoutedEventArgs e)
            {
                try
                {
                    if (e is System.Windows.Input.KeyEventArgs keyEventArgs)
                        if (keyEventArgs.Key != Key.Enter)
                            return;

                    string newName = tbx.Text;
                    string newPath = newName;

                    if (File.Exists(oldPath))
                        newPath = Path.Combine(Path.GetDirectoryName(oldPath), newName);
                    else if (Directory.Exists(oldPath))
                        newPath = Path.Combine(Directory.GetParent(oldPath).FullName, newName);
                    
                    if (!newPath.Equals(oldPath))
                    {
                        var cmd = new RenameCommand { FullName = oldPath, NewName = newPath };
                        this._actions.Add(cmd);
                        cmd.Execute();

                        tb.Tag = newPath;

                        if (this.FileIndicators.TryRemove(oldPath, out var b))
                            this.FileIndicators.TryAdd(newPath, b);

                        if (this.FileButtons.TryRemove(oldPath, out var bb))
                            this.FileButtons.TryAdd(newPath, bb);

                        if (this.FolderExpanders.TryRemove(oldPath, out var bbb))
                            this.FolderExpanders.TryAdd(newPath, bbb);
                    }

                    tbx.Visibility = Visibility.Collapsed;
                    tb.Text = newName;
                    tb.Visibility = Visibility.Visible;

                    tbx.PreviewKeyDown -= doRename;
                    tbx.LostFocus -= doRename;
                }
                catch(Exception err) 
                {
                    new ErrorWindow(err).ShowDialog();
                }
            }

            tbx.PreviewKeyDown += doRename;
            tbx.LostFocus += doRename;
        }

        private static Grid BuildHeader(SymbolIcon icon, string name, string fullName)
        {
            var header = new Grid() { Background = Brushes.Transparent };
            header.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Auto)
            });
            header.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star)
            });
            header.Children.Add(icon);
            Grid.SetColumn(header.Children[0], 0);
            header.Children.Add(new TextBlock
            {
                Text = name,
                Background = Brushes.Transparent,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Padding = new System.Windows.Thickness(6, 2, 6, 2),
                Tag = fullName,
                FontSize = 11,
                Margin = new System.Windows.Thickness(0)
            });
            header.Children.Add(new TextBox
            {
                Text = name,
                Background = Brushes.Transparent,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Padding = new System.Windows.Thickness(6, 2, 6, 2),
                FontSize = 11,
                Visibility = Visibility.Collapsed,
                AcceptsReturn = false,
                Margin = new System.Windows.Thickness(0)
            });
            Grid.SetColumn(header.Children[1], 1);
            Grid.SetColumn(header.Children[2], 1);
            return header;
        }
    }

    public enum FileSystemType
    {
        Directory,
        File
    }
}