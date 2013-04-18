using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace TextComposing.Printing
{
    public partial class Form1 : Form
    {
        private string _inputFilepath;
        private TextComposing.Layout _layout;
        private volatile IPrintable _document;
        private Task _buildingTask;

        public Form1(string inputFilepath)
        {
            _inputFilepath = inputFilepath;
            _layout = TextComposing.Layout.A5Pocket;
            InitializeComponent();
        }

        private IEnumerable<string> ReadFromFile()
        {
            using (var reader = new System.IO.StreamReader(_inputFilepath, Encoding.GetEncoding(932)))
            {
                for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    yield return line;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _buildingTask = Task.Factory.StartNew(BuildDocument);
            _buildingTask.ContinueWith(task => this.Invalidate());
        }

        private void BuildDocument()
        {
            if (_document != null) return;

            try
            {
                throw new NotImplementedException("ラテン対応してません");
                ILatinWordMetric latinWordMetric; //TODO: 実装
                var engine = new LayoutEngine(_layout, latinWordMetric);
                var aozoraText = ReadFromFile();
                _document = engine.Compose(aozoraText);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debugger.Break();
                throw ex;
            }
        }


        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (!_buildingTask.IsCompleted) return;
            var g = e.Graphics;
            var printer = new WinFormPrinter(g, _layout.FontSize);
            g.Clear(Color.White);
            _document.PrintBy(printer);
        }


    }

    internal static class WinFormFontManager
    {
        public static JapaneseFontMetric Metric(Font font)
        {
            var h = font.Height;
            return new JapaneseFontMetric { VerticalHeight = h, VerticalWidth = h };
        }
    }

}
