﻿using ModTool.User.Project;
using ModTool.Windows.Startup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ModTool.Windows
{
    /// <summary>
    /// Interaction logic for StartupWindow.xaml
    /// </summary>
    public partial class StartupWindow
    {
        public StartupWindow() : this(StartPage.Default) { }
        public StartupWindow(StartPage page = StartPage.Default, object paramData = null)
        {
            InitializeComponent();

            switch(page)
            {
                case StartPage.NewProject:
                    this.MainFrame.Navigate(new NewProjectPage());
                    break;
                case StartPage.ProjectFinalize:
                    this.MainFrame.Navigate(new ProjectSetupFinalize(paramData as ProjectTemplateItem));
                    break;
                case StartPage.Default:
                case StartPage.Welcome:
                default:
                    this.MainFrame.Navigate(new WelcomePage());
                    break;
            }
        }

        public static Frame FindParentFrame(DependencyObject self)
        {
            DependencyObject current = self;

            // Traverse the visual tree up to find the Frame
            do
            {
                if (current is Frame frame)
                {
                    return frame;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);

            return null;
        }
    }

    public enum StartPage
    {
        Default,
        Welcome,
        NewProject,
        ProjectFinalize
    }
}
