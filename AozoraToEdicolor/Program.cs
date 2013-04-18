using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using TextComposing;
using TextComposing.IO;
using TextComposing.IO.AozoraBunko.Lexers;
using TextComposing.IO.AozoraBunko.Parsers;
using TextComposing.IO.Exchange;

namespace AozoraToEdicolor
{
    internal class EdicolorTagConverter : IExchangableTextVisitor
    {
        StringBuilder _buffer = new StringBuilder(512);

        void IExchangableTextVisitor.Letter(UChar letter)
        {
            _buffer.Append(letter);
        }

        void IExchangableTextVisitor.RubyStart(UString rubyText)
        {
            _buffer.AppendFormat("<RBY CHAR=\"{0}\" POS=J>", rubyText);
        }

        void IExchangableTextVisitor.RubyEnd()
        {
            _buffer.Append("</RBY>");
        }

        void IExchangableTextVisitor.EmphasysDotStart()
        {
            _buffer.Append(String.Format("<DCB NUM=U{0}>", "FE45"));
        }

        void IExchangableTextVisitor.EmphasysDotEnd()
        {
            _buffer.Append("</DCB>");
        }

        public string GetText()
        {
            return _buffer.ToString();
        }

        public static IEnumerable<string> Convert(IEnumerable<string> inputLines)
        {
            yield return "<TAG>";

            var converter = new TextComposing.IO.AozoraBunkoTextConverter();
            var indentParser = new IndentParser();

            foreach (var line in indentParser.ReadLines(inputLines))
            {
                bool isSetIndent = indentParser.IsSetIndent;
                double textIndent = indentParser.CurrentTextIndent;
                double paragraphIndent = indentParser.CurrentParagraphIndent;

                //先頭の開始括弧に対してはさらに二分下げる。
                if (line.Length >= 1 && CharacterClasses.Cl01(new UChar(line[0])))
                {
                    isSetIndent = true;
                    textIndent += 0.5;
                }

                IExchangableText text = converter.Convert(new UString(line));

                var tag = new EdicolorTagConverter();
                text.Accept(tag);
                var result = tag.GetText();
                if (isSetIndent)
                    yield return String.Format("<IDT IL=1 UNIT=C IS={0} TS={1} BS=0.0>{2}</IDT>", textIndent, paragraphIndent, result);
                else
                    yield return result;
            }

            yield return "</TAG>";
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2) {
                Console.WriteLine("Usage: .exe <aozorabunko-filepath> <edicolor-filepath>");
                return;
            }
            
            string inputPath = args[0], outputPath = args[1];
            var utf8NoBom = new System.Text.UTF8Encoding(false);
            using (var writer = new StreamWriter(outputPath, false, utf8NoBom, 512))
            {
                var inputLines = ReadShiftJisFile(inputPath);
                foreach (var text in EdicolorTagConverter.Convert(inputLines))
                {
                    writer.WriteLine(text);
                }
            }
        }
        
        private static IEnumerable<string> ReadShiftJisFile(string path)
        {
            using (var reader = new StreamReader(path, Encoding.GetEncoding(932)))
            {
                while (!reader.EndOfStream)
                {
                    yield return reader.ReadLine();
                }
            }

        }
    }
}
