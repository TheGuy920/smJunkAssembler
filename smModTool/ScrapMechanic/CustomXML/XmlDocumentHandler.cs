using ICSharpCode.AvalonEdit;
using System;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

namespace ModTool.CustomXML
{
    public class XmlDocumentHandler
    {
        private Canvas RootCanvas;
        private bool ScaleUpdated = false;
        //private StackPanel PropertiesWindow;
        private bool UpdateMouseStart = false;
        private readonly XmlTextHandler TextHandler;

        public XmlDocumentHandler(ref TextEditor textEditor/*, ref Canvas Workspace*/)
        {
            //this.RootCanvas = Workspace;
            //this.PropertiesWindow = prop;
            this.TextHandler = new(ref textEditor, this.TextChanged);
        }

        private void TextChanged()
        {
            
        }

        public void SetGridSize(double grid)
        {

        }

        public void ChangeScale(object sender, double e)
        {
            
        }

        private void LayoutUpdated(object sender, EventArgs e)
        {
            
        }

        public void MouseEnter(object sender, MouseEventArgs e)
        {
            
        }

        public void MouseLeave(object sender, MouseEventArgs e)
        {
            
        }

        public void MouseUp(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void MouseDown(object sender, MouseButtonEventArgs e)
        {
            
        }

        public void MouseMove(object sender, MouseEventArgs e)
        {

        }

        public void Undo()
        {
            this.TextHandler?.Undo();
        }

        public void Redo()
        {
            this.TextHandler?.Redo();
        }

        public override string ToString()
        {
            StringBuilder DocumentString = new();
            //DocumentString.Append($"<?xml version=\"{this.Version.ToString("0.0#")}\" encoding=\"{this.Encoding.WebName.ToUpper()}\"?>");
            DocumentString.Append(Environment.NewLine);
            //DocumentString.Append(this.Document[RootXml].ToString());
            return DocumentString.ToString();
        }

        public void SaveAs(string fileName, bool prettyPrint = true)
        {
            StreamWriter sw = new(
                fileName,
                System.Text.Encoding.Default,
                new FileStreamOptions()
                {
                    Access = FileAccess.Write,
                    Mode = FileMode.Create,
                    Options = FileOptions.SequentialScan
                })
            {
                NewLine = Environment.NewLine
            };
            sw.Write(this.ToString());
            sw.Close();
            sw.Dispose();
        }

        public void LoadString(string xml)
        {
            XElement.Parse(xml);

            this.TextHandler.UpdateText(xml);
        }

        public void LoadFile(string path)
        {
            XElement.Load(path);

            this.TextHandler.UpdateText(File.ReadAllText(path));
        }

        public void UnSelectTextBox()
        {
            this.TextHandler.RemoveCaret();
        }

        internal void AddTextSize(int delta)
        {
            this.TextHandler.AddTextSize(delta);
        }
    }
}
