using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CC = TextComposing.CharacterClasses;

namespace TextComposing.Formatting
{
    /// <summary>
    /// 字間調整済みの一行
    /// </summary>
    internal class JustifiedLine : Printing.IPrintableLine
    {
        private InlineElement[] _texts;

        internal JustifiedLine(IEnumerable<InlineElement> source)
        {
            _texts = source.ToArray();
        }

        public override string ToString()
        {
            return String.Join("", Array.ConvertAll(_texts, x => x.ToString()));
        }

        void Printing.IPrintable.PrintBy(Printing.IPrinter printer)
        {
            foreach (var t in _texts)
            {
                t.PrintBy(printer);
            }
        }
    }

    internal abstract class InlineElement
    {
        public abstract void PrintBy(Printing.IPrinter printer);
    }

    /// <summary>
    /// 一行内の文章
    /// </summary>
    internal abstract class InlineText : InlineElement
    {
        public abstract float Length { get; }
        public abstract float Offset { get; }
        public abstract UString Text { get; }

        public InlineText WithEmphasysDots()
        {
            return new InlineLetterJpWithEmphasysDots(this);
        }
    }

    internal class InlineLetterJp : InlineText
    {
        internal InlineLetterJp(float length, float offset, UChar letter)
        {
            _length = length;
            _offset = offset;
            _letter = letter;
        }

        private float _length;
        private float _offset;
        private UChar _letter;


        public override float Length
        {
            get { return _length; }
        }

        public override float Offset
        {
            get { return _offset; }
        }

        public UChar Letter
        {
            get { return _letter; }
        }

        public override UString Text
        {
            get { return _letter.ToUString(); }
        }

        public override void PrintBy(Printing.IPrinter printer)
        {
            printer.Space(_offset);
            var amount = _length - _offset;

            printer.PrintJapaneseLetter(_letter, amount);
        }

        public override string ToString()
        {
            return _letter.ToString();
        }
    }

    internal class InlineLetterJpWithEmphasysDots : InlineText
    {
        private InlineText _decoratee;

        internal InlineLetterJpWithEmphasysDots(InlineText decoratee)
        {
            _decoratee = decoratee;
        }

        public override float Length
        {
            get { return _decoratee.Length; }
        }

        public override float Offset
        {
            get { return _decoratee.Offset; }
        }

        public override UString Text
        {
            get { return _decoratee.Text; }
        }

        private const float correction = 2; //親文字からの距離の補正値（単位：ポイント）。正で離れる。
        private static readonly UChar sesami = UChar.FromCodePoint(0xFE45);

        public override void PrintBy(Printing.IPrinter printer)
        {
            Printing.PrinterMemento restoreStart = null;
            bool canAddEmphasisDots = CanAddEmphasisDots();

            if (canAddEmphasisDots) restoreStart = printer.StorePositionAndFont();

            _decoratee.PrintBy(printer);
            
            if (!canAddEmphasisDots) return;

            var restoreEnd = printer.StorePositionAndFont();
            {
                restoreStart();
                var baseFontSize = printer.FontSize;
                printer.LineFeed(-baseFontSize / 2 - correction);
                printer.Space(_decoratee.Offset);
                printer.PrintJapaneseLetter(sesami, baseFontSize);
            }
            restoreEnd();
        }

        private bool CanAddEmphasisDots()
        {
            var letterJp = _decoratee as InlineLetterJp;
            if (letterJp == null) return false;

            var letter = letterJp.Letter;

            return CC.IsHiragana(letter) || CC.IsCJKIdeograph(letter) || CC.IsKatakana(letter);
        }
    }

    internal class InlineWordPartLatin : InlineText
    {
        internal InlineWordPartLatin(float length, float offset, UString text)
        {
            _length = length;
            _offset = offset;
            _text = text;
        }

        private float _length;
        private float _offset;
        private UString _text;


        public override float Length
        {
            get { return _length; }
        }

        public override float Offset
        {
            get { return _offset; }
        }

        public override UString Text
        {
            get { return _text; }
        }

        public override void PrintBy(Printing.IPrinter printer)
        {
            printer.Space(_offset);
            var letter = _text;
            var amount = _length - _offset;

            printer.PrintLatinText(letter, amount);
        }

        public override string ToString()
        {
            return _text.ToString();
        }
    }

    internal class InlineTextWithRuby : InlineElement
    {
        private float _appendingLength;
        private InlineText[] _baseText;
        private InlineText[] _rubyText;

        internal InlineTextWithRuby(double length, InlineText[] baseText, InlineText[] rubyText)
        {
            _appendingLength = (float)(length - (baseText.Aggregate(0.0, (lhs, rhs) => lhs + rhs.Length)));
            _baseText = baseText;
            _rubyText = rubyText;
        }

        public override void PrintBy(Printing.IPrinter printer)
        {
            var restoreStart = printer.StorePositionAndFont();
            Array.ForEach(_baseText, x => x.PrintBy(printer));
            printer.Space(_appendingLength);
            PrintRuby(printer, restoreStart);
        }

        private void PrintRuby(Printing.IPrinter printer, Printing.PrinterMemento restoreStart)
        {
            var restoreEnd = printer.StorePositionAndFont();
            try
            {
                restoreStart();
                float bodyFontSize = printer.FontSize;
                float rubyFontSize = bodyFontSize / 2; //TODO: 設定を統一するか、書式情報として運搬
                printer.LineFeed(-(bodyFontSize + rubyFontSize) / 2);
                printer.FontSize = rubyFontSize;
                Array.ForEach(_rubyText, x => x.PrintBy(printer));
            }
            finally
            {
                restoreEnd();
            }
        }

        public override string ToString()
        {
            return String.Format("{0}（{1}）", String.Join("", (Object[])_baseText), String.Join("", (Object[])_rubyText));
        }
    }
}
