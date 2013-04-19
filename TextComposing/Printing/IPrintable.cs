using System;
using System.Collections.Generic;
using System.Linq;

namespace TextComposing.Printing
{
    public interface IPrintable
    {
        void PrintBy(IPrinter printer);
    }

    public interface IPrintableLine : IPrintable
    {
    }

    public interface IPrintableDocument : IPrintable
    {
        UString Title { get; }
    }
}
