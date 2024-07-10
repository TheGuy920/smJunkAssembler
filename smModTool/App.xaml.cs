using smSteamUtility;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using TextBlock = System.Windows.Controls.TextBlock;

namespace ModTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Steam SteamConnection { get; private set; }

        public static event EventHandler<CancelEventArgs> ExitEvent;
        
        /// <summary>Recieves the <see cref="E:System.Windows.Application.Startup"/> event. </summary>
        /// <param name="e">A <see cref="T:System.Windows.StartupEventArgs"/> that contains the event data.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            this.Exit += (s, e) => ExitEvent?.Invoke(s, null);
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
#if !DEBUG
            Microsoft.AppCenter.AppCenter.Start("c435636c-e27c-406e-bc4f-f4da85fae445",
                  typeof(Microsoft.AppCenter.Analytics.Analytics), typeof(Microsoft.AppCenter.Crashes.Crashes));
#endif
            this.LoadSteamFonts();

            // Create a new style for TextBlock
            Style textBlockStyle = new(typeof(TextBlock));

            // Set TextFormattingMode
            textBlockStyle.Setters.Add(new Setter(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display));

            // Set TextRenderingMode
            textBlockStyle.Setters.Add(new Setter(TextOptions.TextRenderingModeProperty, TextRenderingMode.ClearType));

            // Set TextHintingMode
            textBlockStyle.Setters.Add(new Setter(TextOptions.TextHintingModeProperty, TextHintingMode.Fixed));

            // Apply the style globally
            this.Resources.Add(typeof(TextBlock), textBlockStyle);

            if (!File.Exists("steam_appid.txt"))
                File.WriteAllText("steam_appid.txt", "588870");

            SteamConnection = new("Scrap Mechanic", 588870);
            SteamConnection.ConnectToSteam();
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            
        }

        private void LoadSteamFonts()
        {
            // MotivaSans-Black
            var motivaSansBlack = new FontFamily("pack://application:,,,/ModTool;component/Fonts/MotivaSans-Black.ttf#Motiva Sans Black");
            this.Resources.Add("MotivaSansBlack", motivaSansBlack);

            // MotivaSans-BoldItalic
            var motivaSansBoldItalic = new FontFamily("pack://application:,,,/ModTool;component/Fonts/MotivaSans-BoldItalic.ttf#Motiva Sans Bold Italic");
            this.Resources.Add("MotivaSansBoldItalic", motivaSansBoldItalic);

            // MotivaSans-LightItalic
            var motivaSansLightItalic = new FontFamily("pack://application:,,,/ModTool;component/Fonts/MotivaSans-LightItalic.ttf#Motiva Sans Light Italic");
            this.Resources.Add("MotivaSansLightItalic", motivaSansLightItalic);

            // MotivaSans-RegularItalic
            var motivaSansRegularItalic = new FontFamily("pack://application:,,,/ModTool;component/Fonts/MotivaSans-RegularItalic.ttf#Motiva Sans Regular Italic");
            this.Resources.Add("MotivaSansRegularItalic", motivaSansRegularItalic);

            // MotivaSans-Bold
            var motivaSansBold = new FontFamily("pack://application:,,,/ModTool;component/Fonts/MotivaSans-Bold.ttf#Motiva Sans Bold");
            this.Resources.Add("MotivaSansBold", motivaSansBold);

            // MotivaSans-Medium
            var motivaSansMedium = new FontFamily("pack://application:,,,/ModTool;component/Fonts/MotivaSans-Medium.ttf#Motiva Sans Medium");
            this.Resources.Add("MotivaSansMedium", motivaSansMedium);

            // MotivaSans-Light
            var motivaSansLight = new FontFamily("pack://application:,,,/ModTool;component/Fonts/MotivaSans-Light.ttf#Motiva Sans Light");
            this.Resources.Add("MotivaSansLight", motivaSansLight);

            // MotivaSans-Thin
            var motivaSansThin = new FontFamily("pack://application:,,,/ModTool;component/Fonts/MotivaSans-Thin.ttf#Motiva Sans Thin");
            this.Resources.Add("MotivaSansThin", motivaSansThin);

            // MotivaSans-Regular
            var motivaSansRegular = new FontFamily("pack://application:,,,/ModTool;component/Fonts/MotivaSans-Regular.ttf#Motiva Sans Regular");
            this.Resources.Add("MotivaSansRegular", motivaSansRegular);
        }
    }
}
