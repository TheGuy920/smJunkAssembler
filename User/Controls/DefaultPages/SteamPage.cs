using ModTool.Windows;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace ModTool.User.Controls.Overview
{
    public delegate void ImageLoadedEventHandler();

    public class SteamPage : Grid
    {
        private System.Windows.Controls.Image _previewImage;

        public SymbolIcon HeaderIcon { get; init; }
        public string HeaderTitle { get; init; }

        public System.Windows.Controls.Image PreviewImage => _previewImage;
        public event ImageLoadedEventHandler PreviewImageLoaded;

        public SteamPage(DirectoryInfo ProjectDir)
        {
            // Load mod info from project directory
            var bdfile = ProjectDir.GetFiles();
            if (bdfile.FirstOrDefault(f => f.Name.Equals("preview.jpg")) is FileInfo file)
            {
                Task.Run(() => this.SetPreviewImage(ModToolWindow.LoadImage(file.FullName)));
            }
        }

        public void SetMainContent(UIElement content)
        {
            Children.Clear();
            Children.Add(content);
        }

        public void SetPreviewImage(BitmapImage source)
        {
            Dispatcher.Invoke(() => _previewImage = new System.Windows.Controls.Image() { Source = source });
            Dispatcher.Invoke(PreviewImageLoaded.Invoke);
        }

        public void SetPreviewImage(System.Windows.Controls.Image image)
        {
            _previewImage = image;
            Dispatcher.Invoke(PreviewImageLoaded.Invoke);
        }
    }
}
