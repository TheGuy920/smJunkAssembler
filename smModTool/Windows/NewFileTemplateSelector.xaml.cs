using ModTool.User.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;
using TextBlock = System.Windows.Controls.TextBlock;

namespace ModTool.Windows;

/// <summary>
/// Interaction logic for NewFileTemplateSelector.xaml
/// </summary>
public partial class NewFileTemplateSelector : FluentWindow
{
    public string FileName { get; private set; }
    public string FileContent { get; private set; }

    private NewFileItemTemplate FileItemTemplate { get; set; }

    public NewFileTemplateSelector()
    {
        string jsonCategory = Utility.LoadInternalFile.TextFile("smCategoryTemplates.json");
        string jsonTemplates = Utility.LoadInternalFile.TextFile("smNewFileTemplates.json");

        var settings = new JsonSerializerOptions();
        //settings.Converters.Add(new IconTypeConverter());

        List<NewFileCategoryTemplate> Categories
            = JsonSerializer.Deserialize<List<NewFileCategoryTemplate>>(jsonCategory);
        List<NewFileItemTemplate> Templates
            = JsonSerializer.Deserialize<List<NewFileItemTemplate>>(jsonTemplates, settings);

        this.InitializeComponent();

        this.LoadTemplateCategories(Categories);

        this.LoadTemplates(Templates);
    }

    public void LoadTemplateCategories(List<NewFileCategoryTemplate> Categories)
    {
        Categories.Insert(0, new NewFileCategoryTemplate() { Name = "All", FilterTag = "" });
        foreach (var Category in Categories)
        {
            var CategoryButton = new Button()
            {
                Content = new TextBlock() { Text = Category.Name },
                CornerRadius = new CornerRadius(0),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
            };
            CategoryButton.Click += (s, e) =>
            {
                bool FilterAvilable = Category.FilterTag is string tag && tag.Length > 0
                            || Category.LooseFilterTag is string ltag && ltag.Length > 0;
                
                bool FilterCheck(string Tag) =>
                Category.FilterTag is string tag && tag.Contains(Tag, StringComparison.InvariantCultureIgnoreCase)
                || Category.LooseFilterTag is string ltag && ltag.Contains(Tag, StringComparison.InvariantCultureIgnoreCase);


                foreach (Button button in this.TemplateStack.Children.OfType<Button>())
                {
                    if (FilterAvilable && !FilterCheck(button.Tag.ToString()))
                        button.Visibility = Visibility.Collapsed;
                    else
                        button.Visibility = Visibility.Visible;
                }
            };
            this.CategoriesStack.Children.Add(CategoryButton);
        }
    }

    public void LoadTemplates(List<NewFileItemTemplate> Templates)
    {
        foreach (var Template in Templates)
        {
            Grid grid = new()
            {
                Children =
                {
                    Template.Icon,
                    new TextBlock()
                    {
                        Text = Template.Name,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(10,0,0,0)
                    },
                    new TextBlock()
                    {
                        Text = Template.FilterTag,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new Grid()
                },
                Margin = new Thickness(10, 0, 10, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30, GridUnitType.Pixel) });

            Grid.SetColumn(grid.Children[0], 0);
            Grid.SetColumn(grid.Children[1], 1);
            Grid.SetColumn(grid.Children[2], 2);
            Grid.SetColumn(grid.Children[3], 2);

            Button TemplateButton = new()
            {
                Content = grid,
                Tag = Template.FilterTag,
                CornerRadius = new CornerRadius(0),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 50
            };

            TemplateButton.Click += (s, e) =>
            {
                foreach (Button button in this.TemplateStack.Children.OfType<Button>())
                {
                    button.Background = Brushes.Transparent;
                    button.IsEnabled = true;
                }

                TemplateButton.IsEnabled = false;
                TemplateButton.Background = new SolidColorBrush(Color.FromArgb(64,255,255,255));

                this.FileNameTbx.Text = Template.SampleFileName;
                this.FileItemTemplate = Template;
            };

            grid.SetBinding(Grid.HeightProperty, new Binding("ActualHeight") { Source = TemplateButton });
            grid.SetBinding(Grid.WidthProperty, new Binding("ActualWidth") { Source = TemplateButton });

            this.TemplateStack.Children.Add(TemplateButton);
        }
    }

    private void Close(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }

    private void CreateClick(object sender, RoutedEventArgs e)
    {
        this.DialogResult = true;
        
        string fileName = this.FileNameTbx.Text;
        if (!System.IO.Path.HasExtension(fileName))
            fileName += this.FileItemTemplate.FileExtension;

        this.FileName = System.IO.Path.Combine(this.FileItemTemplate.RelativeDirectory, fileName);
        this.FileContent = this.FileItemTemplate.FileContent;
    }
}
