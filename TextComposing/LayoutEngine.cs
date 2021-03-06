﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Solver = TextComposing.LineBreaking.Solver<TextComposing.Printing.IPrintableLine, TextComposing.InlineStyle>;

namespace TextComposing
{
    //トップレベルオブジェクト
    public class LayoutEngine
    {
        private ILatinWordMetric _latinWordMetric;
        private Layout _setting;

        public LayoutEngine(Layout setting, ILatinWordMetric latinWordMetric)
        {
            _setting = setting;
            _latinWordMetric = latinWordMetric;
        }

        public void SendTo(IEnumerable<string> aozoraText, TextComposing.IO.Pdf.PdfPrinter printer)
        {
            printer.FontSize = _setting.FontSize;
            var document = this.Compose(aozoraText);
            printer.Header = document.Title;

            printer.Connect();
            document.PrintBy(printer);

        }

        private Printing.IPrintableDocument Compose(IEnumerable<string> aozoraText)
        {
            float contentHeight = _setting.FontSize * _setting.NumberOfRows;

            var importer = new IO.AozoraBunkoTextImporter(_latinWordMetric);
            importer.FontSizeByPoint = _setting.FontSize;
            var metaData = importer.GetMetaData(aozoraText);
            var paragraphs = importer.Import(aozoraText);
            var solver = new Solver();

            var printableLines = solver.Layout(paragraphs, LineBreaking.Frame.Constant(contentHeight));
            paragraphs = null;
            return new DynamicLayoutingData(metaData.Title, printableLines, _setting.Leading, _setting.NumberOfLines);
        }
    }
}
