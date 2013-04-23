using System;

namespace TextComposing.Printing
{
    public delegate void PrinterMemento();

    public interface IPrinter
    {
        float FontSize { get; set; }

        void PrintJapaneseLetter(UChar letter, float length);

        void PrintLatinText(UString text, float length);

        void Space(float length);

        void LineFeed(float leading);

        void CarriageReturn();

        PrinterMemento StorePositionAndFont();

        void PageBreak();

        void SetOutlineHere(int level, UString title);
    }
}
