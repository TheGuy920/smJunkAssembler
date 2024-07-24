using System;
using System.Windows;

namespace ModTool.Windows
{
    /// <summary>
    /// Interaction logic for ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : Window
    {
        public ErrorWindow(Exception e)
        {
            InitializeComponent();
            this.ErrorTitle.Text = e.Message;
            this.ErrorText.Text = e.StackTrace;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
