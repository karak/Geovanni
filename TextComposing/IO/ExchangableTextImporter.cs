using System;
using System.Text;
using TextComposing.IO.Exchange;

namespace TextComposing.IO
{
    internal class ExchangableTextImporter
    {
        private ILatinWordMetric _latinWordMetric;

        public ExchangableTextImporter(ILatinWordMetric latinWordMetric)
        {
            _latinWordMetric = latinWordMetric;
        }

        public Formatting.ParagraphModel Import(IExchangableText text, ParagraphStyle paragraphStyle)
        {
            var visitor = new Visitor();
            visitor.BeginParagraph(_latinWordMetric, paragraphStyle);
            text.Accept(visitor);
            return visitor.EndParagraph();
        }

        private class Visitor : IExchangableTextVisitor
        {
            private Formatting.ParagraphBuilder _builder;
            private bool _inRuby;
            private UStringBuilder _rubyBaseText;
            private UString _rubyText;

            public void BeginParagraph(ILatinWordMetric latinMetric, ParagraphStyle style)
            {
                _builder = new Formatting.ParagraphBuilder(latinMetric, style);
                _builder.BeginParagraph();
                _inRuby = false;
                _rubyBaseText = new UStringBuilder(16);
                _rubyText = null;
            }

            public Formatting.ParagraphModel EndParagraph()
            {
                if (_inRuby) throw new Exception("Unexpectedly end in ruby text");

                return _builder.EndParagraph();
            }

            void IExchangableTextVisitor.Letter(UChar letter)
            {
                if (_inRuby)
                {
                    _rubyBaseText.Append(letter);
                }
                else
                {
                    _builder.Text(letter);
                }
            }

            void IExchangableTextVisitor.RubyStart(UString rubyText)
            {
                if (_inRuby) throw new Exception("Already in ruby text");

                _rubyBaseText.Clear();
                _rubyText = rubyText;
                _inRuby = true;
            }

            void IExchangableTextVisitor.RubyEnd()
            {
                if (!_inRuby) throw new Exception("Not in ruby text yet");

                _builder.TextWithGroupRuby(_rubyBaseText.ToUString(), _rubyText);

                _rubyBaseText.Clear();
                _rubyText = null;
                _inRuby = false;
            }

            void IExchangableTextVisitor.EmphasysDotStart()
            {
                _builder.TextEmphasysDot = TextEmphasysDot.Sesami;
            }

            void IExchangableTextVisitor.EmphasysDotEnd()
            {
                _builder.TextEmphasysDot = TextEmphasysDot.None;
            }
        }
    }
}
