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

        public Printing.IPrintableDocument Compose(IEnumerable<string> aozoraText)
        {
            float contentHeight = _setting.FontSize * _setting.NumberOfRows;

            var importer = new IO.AozoraBunkoTextImporter(_latinWordMetric);
            importer.FontSizeByPoint = _setting.FontSize;
            var paragraphs = importer.Import(aozoraText);
            var solver = new Solver();

            var printableLines = solver.Layout(paragraphs, LineBreaking.Frame.Constant(contentHeight));
            paragraphs = null;
            return new LayoutedDocument(printableLines, _setting.Leading, _setting.NumberOfLines);
        }
    }
}
