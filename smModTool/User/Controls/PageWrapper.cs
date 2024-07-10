using System;
using System.Windows;
using System.Windows.Controls;

namespace ModTool.User.Controls
{
    internal class PageWrapper : Grid
    {
        public static PageWrapper New<T>(object[]? data = null) where T : Page
            => new(typeof(T), data);

        private Type IClass;
        private Page ISelf;
        private object[]? IData;

        private PageWrapper(Type @class, object[]? data)
        {
            this.IClass = @class;
            this.IData = data;
            this.Loaded += PageWrapper_Loaded;
        }

        private void PageWrapper_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.IClass == null)
                throw new Exception("PageWrapper: No class set");
            
            if (this.IData != null)
                this.ISelf ??= (Page)Activator.CreateInstance(this.IClass, this.IData);
            else
                this.ISelf ??= (Page)Activator.CreateInstance(this.IClass);

            UIElement tmp = (UIElement)this.ISelf.Content;
            this.ISelf.Content = null;
            this.Children.Add(tmp);

            this.Loaded -= PageWrapper_Loaded;
        }
    }
}
