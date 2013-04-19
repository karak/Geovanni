using System;
using System.Collections.Generic;
using System.Linq;

namespace TextComposing
{
    internal class LayoutedDocument : Printing.IPrintableDocument
    {
        private UString _title;
        private Printing.IPrintableLine[] _lines;
        private readonly float _leading;
        private readonly int _numberOfLines;

        public LayoutedDocument(UString title, Printing.IPrintableLine[] lines, float leading, int numberOfLines)
        {
            _title = title;
            _lines = lines;
            _leading = leading;
            _numberOfLines = numberOfLines;
        }

        public UString Title
        {
            get { return _title; }
        }

        void Printing.IPrintable.PrintBy(Printing.IPrinter printer)
        {
            int lineNumberInPage = 0;
            foreach (var line in _lines)
            {
                if (lineNumberInPage >= _numberOfLines)
                {
                    printer.PageBreak();
                    lineNumberInPage = 0;
                }
                line.PrintBy(printer);
                printer.LineFeed(_leading);
                printer.CarriageReturn();
                ++lineNumberInPage;
            }
        }
    }
}
