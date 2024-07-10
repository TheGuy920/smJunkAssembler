using HandyControl.Tools.Extension;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using ModTool.Languages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Design.Behavior;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.DataGrid.Views;

namespace ModTool.Pages
{
    /// <summary>
    /// Interaction logic for SearchPanel.xaml
    /// </summary>
    public partial class SearchPanel : Page, ISearchPanel
    {
        SearchResultBackgroundRenderer Renderer;
        SearchPanelAdorner Adorner;
        SearchInputHandler Handler;
        private readonly TextEditor TextEditor;
        private readonly TextArea TextArea;
        private readonly TextBlock MatchCounter;
        private readonly Grid ReplaceControl;
        private readonly Control Self;
        private Regex FindRegex;

        public event EventHandler<SearchOptionsChangedEventArgs> SearchOptionsChanged;

        public string SearchPattern { get; set; } = string.Empty;

        private bool isClosed = true;
        public bool IsClosed => this.isClosed;

        private bool MatchCase = false;
        private bool MatchWord = false;
        private bool UseRegex = false;

        SearchPanel(TextEditor editor) : this(editor.TextArea)
        {
            this.TextEditor = editor;
            this.TextEditor.TextChanged += TextEditorTextChanged;
        }

        SearchPanel(TextArea area)
        {
            this.InitializeComponent();
            this.Self = new()
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                MinWidth = 250,
                MaxWidth = 350,
                MaxHeight = 500,
                Template = this.FindResource("SearchBoxControl") as ControlTemplate
            };

            this.TextArea = area;
            this.Adorner = new SearchPanelAdorner(this.TextArea, this)
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            this.Renderer = new SearchResultBackgroundRenderer();
            this.Handler = new SearchInputHandler(this.TextArea, this);
            this.TextArea.DefaultInputHandler.NestedInputHandlers.Add(this.Handler);

            this.Self.ApplyTemplate();
            this.Self.Template.LoadContent();
            this.MatchCounter = this.Self.Template.FindName("MatchCounter", this.Self) as TextBlock;
            this.ReplaceControl = this.Self.Template.FindName("ReplaceControl", this.Self) as Grid;
        }

        public static SearchPanel Install(TextEditor editor)
        {
            ArgumentNullException.ThrowIfNull(editor);
            ArgumentNullException.ThrowIfNull(editor.TextArea);
            return new SearchPanel(editor);
        }

        public static SearchPanel Install(TextArea area)
        {
            ArgumentNullException.ThrowIfNull(area);
            return new SearchPanel(area);
        }

        public void Close()
        {
            if (!this.IsClosed)
            {
                var layer = AdornerLayer.GetAdornerLayer(this.TextArea);
                layer?.Remove(this.Adorner);
                this.TextArea.TextView.BackgroundRenderers.Remove(this.Renderer);
                this.ClearHighlighting();
                this.isClosed = true;
            }
        }

        private void TextEditorTextChanged(object sender, EventArgs e)
        {
            if (!this.IsClosed)
            {

            }
        }

        private static readonly SolidColorBrush BrightTanslucent = new(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF));

        private void ToggleCaseClick(object sender, RoutedEventArgs e)
        {
            this.MatchCase = !this.MatchCase;
            Button button = (Button)sender;
            button.BorderBrush = this.MatchCase ? BrightTanslucent : Brushes.Transparent;

            this.UpdateSearch(this.SearchPattern);
        }

        private void ToggleWordClick(object sender, RoutedEventArgs e)
        {
            this.MatchWord = !this.MatchWord;
            Button button = (Button)sender;
            button.BorderBrush = this.MatchWord ? BrightTanslucent : Brushes.Transparent;

            this.UpdateSearch(this.SearchPattern);
        }

        private void ToggleRegexClick(object sender, RoutedEventArgs e)
        {
            this.UseRegex = !this.UseRegex;
            Button button = (Button)sender;
            button.BorderBrush = this.UseRegex ? BrightTanslucent : Brushes.Transparent;

            this.UpdateSearch(this.SearchPattern);
        }

        private void ReplaceClick(object sender, RoutedEventArgs e)
        {

        }

        private void ReplaceAllClick(object sender, RoutedEventArgs e)
        {

        }

        private void FindTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox FindTextBox = (TextBox)sender;
            this.SearchPattern = FindTextBox.Text ?? "";

            this.UpdateSearch(this.SearchPattern);
        }

        private void UpdateSearch(string Text)
        {
            if (string.IsNullOrWhiteSpace(Text))
            {
                this.ClearHighlighting();
                return;
            }

            string mw = this.MatchWord ? @"\b" : "";
            string center = this.UseRegex ? Text : Regex.Escape(Text);
            this.FindRegex = new Regex($"{mw}{center}{mw}");
            this.HighlightMatches();
        }

        private void ClearHighlighting()
        {
            this.Renderer.CurrentResults.Clear();
            this.TextArea.ClearSelection();
            this.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
        }

        private static string GetMatchCountText(int count, int index = -1)
        {
            return count > 0 ? $"{index + 1} {Strings.Of} {count}" : Strings.NoMatchesFound;
        }

        private void HighlightMatches()
        {
            int offset = this.TextArea.Caret.Offset;
            this.TextArea.ClearSelection();
            this.Renderer.CurrentResults.Clear();

            var matches = this.FindRegex.Matches(this.TextEditor.Text);
            foreach (Match match in matches.Cast<Match>())
            {
                this.Renderer.CurrentResults.Add(new SearchResult { StartOffset = match.Index, Length = match.Length });
            }

            if (matches.FirstOrDefault(i => i.Index >= offset, null) is Match m)
            {
                this.TextArea.Caret.Offset = m.Index;
                this.TextArea.Caret.BringCaretToView();
                this.TextArea.Caret.Show();
                this.MatchCounter.Text = GetMatchCountText(matches.Count, Array.IndexOf([.. matches], m));
            }
            else
            {
                this.MatchCounter.Text = GetMatchCountText(matches.Count);
            }


            this.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
        }

        public Control GetControl() => this.Self;

        public void Open(bool replace = false)
        {
            if (this.IsClosed)
            {
                var layer = AdornerLayer.GetAdornerLayer(this.TextArea);
                layer?.Add(this.Adorner);
                this.TextArea.TextView.BackgroundRenderers.Add(this.Renderer);
                this.isClosed = false;
            }
        }

        public void ReplacePanel(bool open_close)
        {
            if (open_close)
                this.ReplaceControl.Visibility = Visibility.Visible;
            else
                this.ReplaceControl.Visibility = Visibility.Collapsed;
        }

        public void Reactivate()
        {
            //.Focus();
            //searchTextBox.SelectAll();
        }

        public void FindNext()
        {
            SearchResult result = this.Renderer.CurrentResults.FindFirstSegmentWithStartAfter(this.TextArea.Caret.Offset + 1);
            result ??= this.Renderer.CurrentResults.FirstSegment;
            if (result != null)
                SelectResult(result);

            this.MatchCounter.Text = GetMatchCountText(this.Renderer.CurrentResults.Count,
                Array.IndexOf([.. this.Renderer.CurrentResults], result));
        }

        public void FindPrevious()
        {
            SearchResult result = this.Renderer.CurrentResults.FindFirstSegmentWithStartAfter(this.TextArea.Caret.Offset);
            if (result != null)
                result = this.Renderer.CurrentResults.GetPreviousSegment(result);

            result ??= this.Renderer.CurrentResults.LastSegment;
            if (result != null)
                SelectResult(result);

            this.MatchCounter.Text = GetMatchCountText(this.Renderer.CurrentResults.Count,
                Array.IndexOf([.. this.Renderer.CurrentResults], result));
        }

        void SelectResult(SearchResult result)
        {
            this.TextArea.Caret.Offset = result.StartOffset;
            this.TextArea.Selection = Selection.Create(this.TextArea, result.StartOffset, result.EndOffset);
            this.TextArea.Caret.BringCaretToView();
            // show caret even if the editor does not have the Keyboard Focus
            this.TextArea.Caret.Show();
        }

        private void FindNextClick(object sender, RoutedEventArgs e)
        {
            this.FindNext();
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
