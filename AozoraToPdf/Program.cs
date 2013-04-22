using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DefaultValueAttribute = System.ComponentModel.DefaultValueAttribute;
using TextComposing;
using ExternalCharacterParser = TextComposing.IO.AozoraBunko.Lexers.ExternalCharacterParser;
using AccentNotationParser = TextComposing.IO.AozoraBunko.Lexers.AccentNotationParser;
using TextComposing.IO.Pdf;

namespace AozoraToPdf
{
    internal class Options
    {
        public string Input { get; set; }
        [DefaultValue(null)]
        public string Output { get; set; }
        [DefaultValue("")]
        public string Title { get; set; }
        [DefaultValue(false)]
        public bool TitleExternalChar { get; set; }
        [DefaultValue("a5pocket")]
        public string Layout { get; set; }
    }

    static class Program
    {
        private static PdfFontSetting fontSetting = new PdfFontSetting
        {
            //Font = PdfFontFamily.HiraginoMincho,
            //Font = PdfFontFamily.IwataMinchoOld,
            Font = PdfFontFamily.KozukaMincho,
            //LatinFont = PdfFontFamily.EBGaramond,
            LatinFont = PdfFontFamily.TimesRoman,
            LatinBaselineOffsetRatio = 0.20F,
            NombreFont = PdfFontFamily.KozukaMincho,
            SymbolFont = PdfFontFamily.MSMincho
        };

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Options options = new Options();
            var cmdParser = new Misuzilla.Utilities.CommandLineParser<Options>();
            if (!cmdParser.TryParse(args, out options))
            {
                Console.WriteLine(@"Options:
--input=<aozorabunko text file>(required)
--output=<pdf file> (default: equal to input's extension is replaced by "".pdf""
--title=<title of your book> (default:"""")
--title-external-char enable replace external chars and accent decomposition also in title
--layout=a5pocket|a4manuscript (defult:a5pocket)");
                return;
            }
            if (options.Output == null)
            {
                options.Output = System.IO.Path.ChangeExtension(options.Input, ".pdf");
            }

            UString title = new UString(options.Title);
            if (options.TitleExternalChar)
            {
                title = new UString(AccentNotationParser.Filter(ExternalCharacterParser.Filter(title)));
            }
            var aozoraText = ReadFromFile(options.Input);
            var layout = (options.Layout == "a4manuscript"?  Layout.A4Manuscript :  Layout.A5Pocket);
            WritePdf(aozoraText, options.Output, layout);
        }

        private static void WritePdf(IEnumerable<string> aozoraText, string output, Layout layout)
        {
            ILatinWordMetric latinMetric = new PdfLatinWordMetric(fontSetting.LatinFont, layout.FontSize);
            var engine = new LayoutEngine(layout, latinMetric);
            var printer = new PdfPrinter(output, layout, fontSetting);
            using (printer)
            {
                engine.SendTo(aozoraText, printer);
            }
        }

        private static IEnumerable<string> ReadFromFile(string path)
        {
            using (var reader = new System.IO.StreamReader(path, Encoding.GetEncoding(932)))
            {
                for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    yield return line;
                }
            }
        }
    }
}
