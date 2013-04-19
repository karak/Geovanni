﻿using System;
using System.Collections.Generic;
using System.Linq;
using TextComposing.IO.AozoraBunko.Parsers;
using TextComposing.IO.AozoraBunko.Lexers;
using TextComposing.IO.Exchange;
using IParagraphModel = TextComposing.LineBreaking.IParagraphModel<TextComposing.Printing.IPrintableLine, TextComposing.InlineStyle>;

namespace TextComposing.IO
{
    public class AozoraBunkoTextConverter
    {
        public IExchangableText Convert(UString line)
        {
            var buffer = new FormattedTextBuffer();
            var visitor = CreateTokenVisitor(buffer);

            foreach (var token in RubyParser.Parse(ApplyLexers(line)))
            {
                token.Accept(visitor);
            }
            return buffer;
        }

        private static RubyParser.TokenVisitor CreateTokenVisitor(FormattedTextBuffer buffer)
        {
            var visitor = new RubyParser.TokenVisitor
            {
                OnNormal = text =>
                {
                    var tokens2 = AnnotationParser.Parse(text);
                    foreach (var token2 in tokens2)
                    {
                        token2.Accept(new AnnotationParser.TokenVisitor
                        {
                            OnBody = bodyText => buffer.Append(bodyText),
                            OnPlaceholder = () => buffer.Append(AozoraBunko.SpecialCharacters.ExternalCharacterPlaceholder),
                            OnAnnotation = annotationText =>
                            {
                                //見出し
                                var headingText = HeadingParser.Parse(annotationText);
                                if (headingText != null)
                                {
                                    //TODO: 見出しの処理の実装
                                    return;
                                }

                                //圏点
                                var emphasysDottedText = EmphasysDotsParser.Parse(annotationText);
                                if (emphasysDottedText != null)
                                {
                                    buffer.EmphasysDot(buffer.Length - emphasysDottedText.Length, buffer.Length);
                                    return;
                                }

                                //TODO: その他の注記
                            }
                        });
                    };
                },
                OnRuby = (baseText, rubyText) =>
                {
                    buffer.Append(baseText);
                    buffer.Ruby(buffer.Length - baseText.Length, buffer.Length, rubyText);
                }
            };
            return visitor;
        }

        private static IEnumerable<UChar> ApplyLexers(UString line)
        {
            Filter f = Compose(
                ExternalCharacterParser.Filter,
                AccentNotationParser.Filter,
                EmDashReplacer.Filter,
                KanaRepeatingMarkParser.Filter,
                InterletterSpaceRemover.Filter); //TODO: remove ではなく、書式オブジェクトに置き換える。場所ももっと後、（和文区切り文字の後）と（和文欧文間）
            return f(line);
        }

        private delegate IEnumerable<UChar> Filter(IEnumerable<UChar> cs);

        private static Filter Compose(params Filter[] fs)
        {
            return cs =>
            {
                foreach (var f in fs)
                    cs = f(cs);
                return cs;
            };
        }
    }
    public class AozoraBunkoTextImporter
    {
        private bool _isFrozen = false;
        private AozoraBunkoTextConverter _converter;
        private ExchangableTextImporter _exchangableTextImporter;
        private float _fontSizeByPoint;

        public AozoraBunkoTextImporter(ILatinWordMetric latinWordMetric)
        {
            _converter = new AozoraBunkoTextConverter();
            _exchangableTextImporter = new ExchangableTextImporter(latinWordMetric);
        }

        public float FontSizeByPoint
        {
            get
            {
                return _fontSizeByPoint;
            }

            set
            {
                if (_isFrozen) throw new InvalidOperationException("Immutable while importing");
                _fontSizeByPoint = value;
            }
        }

        public IEnumerable<IParagraphModel> Import(string text)
        {
            return Import(text.Split('\n'));
        }

        public IEnumerable<IParagraphModel> Import(IEnumerable<string> lines)
        {
            _isFrozen = true;
            try
            {
                int startIndex;
                string title;
                string[] authors;
                if (ParseHeaderPart(lines, out startIndex, out title, out authors))
                {
                    ;
                }
                var indentParser = new IndentParser();
                foreach (string line in indentParser.ReadLines(lines.Skip(startIndex)))
                {
                    var textIndent = indentParser.CurrentTextIndent;
                    var paragraphIndent = indentParser.CurrentParagraphIndent;
                    var paragraph = BuildParagraph(new UString(line), textIndent, paragraphIndent);
                    yield return paragraph;
                }
            }
            finally
            {
                _isFrozen = false;
            }
        }

        private const string ruler = "-------------------------------------------------------";

        private static bool ParseHeaderPart(IEnumerable<string> lines, out int startIndex, out string title, out string[] authors)
        {
            string titleBuffer = "";
            List<string> authorsBuffer = new List<string>(4);
            int foundCount = 0;
            int lineIndex = 0;
            foreach (var line in lines)
            {
                if (line == ruler)
                {
                    ++foundCount;
                }
                else if (foundCount == 0)
                {
                    if (lineIndex == 0)
                    {
                        titleBuffer = line.TrimStart('　'); ;
                    }
                    else if (line.Length > 0)
                    {
                        authorsBuffer.Add(line.TrimStart('　'));
                    }
                }

                if (foundCount == 2) {
                    startIndex = lineIndex + 1;
                    title = titleBuffer;
                    authors = authorsBuffer.ToArray();
                    return true;
                }
                else if (lineIndex >= 40)
                {
                    break;
                }
                ++lineIndex;
            }
            startIndex = 0;
            title = null;
            authors = null;
            return false;
        }

        private Formatting.ParagraphModel BuildParagraph(UString line, int textIndent, int paragraphIndent)
        {
            var exchangableText = _converter.Convert(line);
            var paragraphStyle = new ParagraphStyle
            {
                FontSize = _fontSizeByPoint,
                RubyFontSizeRatio = 0.5F, //TODO: 共通化が必要
                Indent = new ManualParagraphIndentStyle(textIndent, paragraphIndent)
            };
            var paragraph = _exchangableTextImporter.Import(exchangableText, paragraphStyle);
            return paragraph;
        }
    }
}
