using ModTool.User.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;
using TextBlock = System.Windows.Controls.TextBlock;

namespace ModTool.Windows.Startup
{
    /// <summary>
    /// Interaction logic for NewProjectPage.xaml
    /// </summary>
    public partial class NewProjectPage : Page
    {
        private ProjectSetupFinalize projectSetupFinalize;
        private readonly SolidColorBrush fgColor;

        public NewProjectPage()
        {
            InitializeComponent();
            fgColor = this.FindResource("TextFillColorPrimaryBrush") as SolidColorBrush;

            var bg = this.FindResource("ApplicationBackgroundBrush") as SolidColorBrush;
            var fg = new SolidColorBrush(new Color() { A = 64, R = fgColor.Color.R, G = fgColor.Color.G, B = fgColor.Color.B });
            
            if (bg.Color.R + bg.Color.G + bg.Color.B > 650)
                foreach (var button in this.ProjectTemplateList.Children.Cast<Button>())
                    button.Background = fg;

            foreach (var button in this.ProjectTemplateList.Children.Cast<Button>())
            {
                var border = (button.Content as Grid).Children[0] as Border;
                button.MouseEnter += (s, e) =>
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255));
                    border.Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));
                };
                button.MouseLeave += (s, e) =>
                {
                    border.BorderBrush = Brushes.Transparent;
                    border.Background = Brushes.Transparent;
                };
            }
        }

        private void ChangeTemplateSelection(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            foreach (Button uiButton in this.ProjectTemplateList.Children)
            {
                uiButton.BorderBrush = Brushes.Transparent;
                uiButton.IsEnabled = true;
            }

            button.BorderBrush = fgColor;
            button.IsEnabled = false;

            this.NextButton.IsEnabled = true;
            this.NextButton.Opacity = 1;
        }

        private void BackButtonClick(object sender, RoutedEventArgs e)
        {
            this.NextButton.IsEnabled = false;
            this.NextButton.Opacity = 0.2;

            var frame = StartupWindow.FindParentFrame(this);
            if (frame.CanGoBack)
            {
                frame.GoBack();
                frame.RemoveBackEntry();
            }
            else
            {
                frame.Navigate(new WelcomePage());
            }
        }

        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            var uiButton = this.ProjectTemplateList.Children.Cast<Wpf.Ui.Controls.Button>()
                    .Where(uiButton => !uiButton.IsEnabled)
                    .First();

            int id = int.TryParse(uiButton.Tag.ToString(), out int result) ? result : throw new Exception(":(");
            ProjectTemplateType projectType = (ProjectTemplateType)id;

            if (this.projectSetupFinalize is null)
            {
                this.projectSetupFinalize = new(BurnedProjectBuilder.BuildBurnedProject(projectType));
                StartupWindow.FindParentFrame(this).Navigate(this.projectSetupFinalize);
            }
            else
            {
                this.projectSetupFinalize.ChangeTemplate(BurnedProjectBuilder.BuildBurnedProject(projectType));
                StartupWindow.FindParentFrame(this).GoForward();
            }
        }

        private void SearchBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            foreach (Wpf.Ui.Controls.Button uiButton in this.ProjectTemplateList.Children)
            {
                var visible = FindVisualChildren<TextBlock>(uiButton)
                    .Any(textBlock => textBlock.Text
                    .Contains(sender.Text, StringComparison.CurrentCultureIgnoreCase));

                uiButton.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null)
                yield break;
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child != null && child is T t)
                    yield return t;
                    
                foreach (T childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
            
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            List<string> list = [];
            foreach (Wpf.Ui.Controls.Button uiButton in this.ProjectTemplateList.Children)
            {
                var text = FindVisualChildren<TextBlock>(uiButton)
                    .First(tb => tb.FontWeight == FontWeights.Bold)
                    .Text;
                list.Add(text);
            }
            this.SearchBox.OriginalItemsSource = list;
        }

        private void BackButtonLoaded(object sender, RoutedEventArgs e)
        {
            /*
            Button button = sender as Button;
            var frame = StartupWindow.FindParentFrame(this);
            button.IsEnabled = frame.CanGoBack;
            button.Opacity = frame.CanGoBack ? 1 : 0.2;
            */
        }
    }
}
