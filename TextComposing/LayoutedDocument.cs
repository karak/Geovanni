using System;
using System.Collections.Generic;
using System.Linq;

namespace TextComposing
{
    internal class DynamicLayoutingData : Printing.IPrintableDocument
    {
        private UString _title;
        private IEnumerable<Printing.IPrintableLine> _lineEnum;
        private readonly float _leading;
        private readonly int _numberOfLines;

        public DynamicLayoutingData(UString title, IEnumerable<Printing.IPrintableLine> lineEnum, float leading, int numberOfLines)
        {
            _title = title;
            _lineEnum = lineEnum;
            _leading = leading;
            _numberOfLines = numberOfLines;
        }

        public UString Title
        {
            get { return _title; }
        }

        void Printing.IPrintable.PrintBy(Printing.IPrinter printer)
        {
            int pageNumber = 0;
            int lineNumberInPage = 0;
            foreach (var line in _lineEnum)
            {
                if (lineNumberInPage >= _numberOfLines)
                {
                    printer.PageBreak();
                    lineNumberInPage = 0;
                    ++pageNumber;
                    Progress(pageNumber);
                }
                line.PrintBy(printer);
                printer.LineFeed(_leading);
                printer.CarriageReturn();
                ++lineNumberInPage;
            }
            Progress(pageNumber + 1);
        }

        private static void Progress(int pageNumber)
        {

            Console.WriteLine("P{0:D4}", pageNumber);
        }
    }
}
