using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace TextComposing.IO.Exchange
{

    public partial class FormattedTextBuffer : IExchangableText
    {
        private UStringBuilder _textBuffer = new UStringBuilder(512);
        private Dictionary<int, MetaInfo> _metaInfos = new Dictionary<int, MetaInfo>();
        
        public void Append(UString text)
        {
            _textBuffer.Append(text);
        }
        public void Append(UChar ch)
        {
            _textBuffer.Append(ch);
        }

        public int Length
        {
            get { return _textBuffer.Length; }
        }

        public void Ruby(int start, int end, UString rubyText)
        {
            Upsert(start, MetaInfo.RUBY_START, rubyText);
            Upsert(end, MetaInfo.RUBY_END);
        }

        public void EmphasysDot(int start, int end)
        {
            Upsert(start, MetaInfo.EMPHASYS_DOT_START);
            Upsert(end, MetaInfo.EMPHASYS_DOT_END);
        }

        private void Upsert(int index, uint flag, UString rubyText = null)
        {
            MetaInfo existing;
            if (!_metaInfos.TryGetValue(index, out existing))
            {
                existing = new MetaInfo();
                _metaInfos.Add(index, existing);
            }
            existing.Flags |= flag;
            if (flag == MetaInfo.RUBY_START)
            {
                existing.RubyText = rubyText;
            }
        }

        void IExchangableText.Accept(IExchangableTextVisitor visitor)
        {
            int index = 0;
            foreach (var letter in _textBuffer.ToUString())
            {
                if (index == 19)
                {
                    ;
                }
                VisitAt(index, visitor);
                visitor.Letter(letter);
                ++index;
            }
            VisitAt(index, visitor);
        }

        private void VisitAt(int index, IExchangableTextVisitor visitor)
        {
            MetaInfo metaInfo;
            if (_metaInfos.TryGetValue(index, out metaInfo))
            {
                if ((metaInfo.Flags & MetaInfo.EMPHASYS_DOT_END) != 0u)
                    visitor.EmphasysDotEnd();
                if ((metaInfo.Flags & MetaInfo.RUBY_END) != 0u)
                    visitor.RubyEnd();

                if ((metaInfo.Flags & MetaInfo.RUBY_START) != 0u)
                    visitor.RubyStart(metaInfo.RubyText);
                if ((metaInfo.Flags & MetaInfo.EMPHASYS_DOT_START) != 0u)
                    visitor.EmphasysDotStart();
            }
        }
    }

    public partial class FormattedTextBuffer
    {
        private class MetaInfo
        {
            public const uint EMPHASYS_DOT_END = 0x1u;
            public const uint RUBY_END = 0x1u << 1;

            public const uint RUBY_START = 0x1u << 16;
            public const uint EMPHASYS_DOT_START = 0x1u << 17;

            public uint Flags = 0u;
            public UString RubyText;
        }
    }

}
