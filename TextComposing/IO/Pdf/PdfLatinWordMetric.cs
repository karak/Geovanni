using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iTextSharp.text.pdf;

namespace TextComposing.IO.Pdf
{
    public class PdfLatinWordMetric : ILatinWordMetric
    {
        private readonly BaseFont _baseFont;
        private readonly float _fontSize;

        public PdfLatinWordMetric(IPdfFontFamily latinFont, float fontSize)
        {
            _baseFont = latinFont.CreateBaseFont(RunDirection.Horizontal, true);
            _fontSize = fontSize;
        }

        float ILatinWordMetric.MeasureText(string latinText)
        {
            return _baseFont.GetWidthPoint(latinText, _fontSize);
        }
    }
}
