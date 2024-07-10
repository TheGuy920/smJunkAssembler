using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ModTool.Windows
{
    public enum RunMode
    {
        Debug = 0x00,
        Release = 0x01
    }

    public class ControlUtil
    {
        public static ConsoleControl.WPF.ConsoleControl CreateNewConsole(string program, string args) =>
            CreateNewConsole(new ProcessStartInfo() { FileName = program, Arguments = args });

        public static ConsoleControl.WPF.ConsoleControl CreateNewConsole(ProcessStartInfo info)
        {
            var console = new ConsoleControl.WPF.ConsoleControl() { Background = Brushes.Transparent, Style = null, BorderBrush = Brushes.Transparent };
            console.StartProcess(info);

            bool timerRunning = false;
            Stopwatch stopwatch = new();
            System.Timers.Timer clearTimer = new(1) { AutoReset = true };
            clearTimer.Elapsed += (sender, args) =>
            {
                console.Dispatcher.Invoke(console.ClearOutput);
                if (stopwatch.ElapsedMilliseconds > 50)
                {
                    clearTimer.Stop();
                    stopwatch.Stop();
                    timerRunning = false;
                    console.ProcessInterface.Process.StandardInput.WriteLine(string.Empty);
                }
            };

            console.OnProcessOutput += (s, e) =>
            {
                if (timerRunning)
                    stopwatch.Restart();
            };

            console.OnProcessInput += (s, e) =>
            {
                string c = e.Content.Trim();
                if (c.Equals("clear", StringComparison.InvariantCultureIgnoreCase)
                || c.Equals("cls", StringComparison.InvariantCultureIgnoreCase))
                {
                    timerRunning = true;
                    stopwatch.Start();
                    clearTimer.Start();
                }
            };

            return console;
        }

        public static string FindCommandSource(string command)
        {
            string where = Environment.OSVersion.Platform == PlatformID.Win32NT ? "where" : "which";

            ProcessStartInfo startInfo = new()
            {
                FileName = where,
                Arguments = command,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = Process.Start(startInfo);
            using StreamReader reader = process.StandardOutput;
            return reader.ReadToEnd().Trim();
        }

        public static async Task FindVisualStudioCode()
        {
            foreach (Process process in Process.GetProcesses())
            {
                //Debug.WriteLine(process.MainWindowTitle);
                if (process.MainWindowTitle.Contains("Visual Studio Code"))
                {
                    // This is the VS Code process
                    // You can get the MainWindowHandle here
                    IntPtr windowHandle = process.MainWindowHandle;

                    // Do something with the handle, such as embedding it in your app
                    // ...
                }

                await Task.Delay(1);
            }
        }
    }
}
