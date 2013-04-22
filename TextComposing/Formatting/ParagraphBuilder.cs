using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CC = TextComposing.CharacterClasses;


namespace TextComposing.Formatting
{
    //TODO: 欧文前後の半角空白の置き換え
    internal class ParagraphBuilder
    {
        private readonly float _zwSize;
        private readonly float _rubyZwSize;
        private WordWrapStrategy _wordWrap;
        private AdvancingStrategy _advancing;
        private ILatinWordMetric _latinMetric;

        private TextBuffer _buffer;

        private class TextBuffer
        {
            private LatinMode _latinMode;
            private WordWrapStrategy _wordWrap;
            private AdvancingStrategy _advancing;
            private ILatinWordMetric _latinMetric;
            private List<IFormatObject> _buffer;
            private float _currentZwSize;
            private UChar _lastLetter;
            private float _lastLetterZwSize;

            public TextBuffer(float zwSize, WordWrapStrategy wordWrap, AdvancingStrategy advancing, ILatinWordMetric latinMetric, int capacity)
            {
                _wordWrap = wordWrap;
                _advancing = advancing;
                _latinMetric = latinMetric;
                _buffer = new List<IFormatObject>(capacity);
                _lastLetter = default(UChar);
                _currentZwSize = _lastLetterZwSize = zwSize;

                SetLatinModeObject(new LatinMode());
            }

            private void SetLatinModeObject(LatinMode @object)
            {
                _latinMode = @object;
                _latinMode.BeforeLangChange += _latinMode_BeforeLangChange;
                _latinMode.Flush += _latinMode_Flush;
            }

            public void Clear()
            {
                //TODO: _latinMode.Clear();
                _lastLetter = default(UChar);
                _lastLetterZwSize = 0F;
                _buffer.Clear();
            }

            public int Count { get { return _buffer.Count; } }

            public IFormatObject[] ToArray()
            {
                this.FlushLatinBuffer();
                return _buffer.ToArray();
            }

            #region low-level manipulations

            public void AppendObject(IFormatObject @object)
            {
                _buffer.Add(@object);
            }

            /**
             * <remarks>自身は以後無効になります！</remarks>
             */
            public void MoveLastLetterStateTo(TextBuffer other)
            {
                _latinMode.ForceFlush();

                other._lastLetter = _lastLetter;
                other._lastLetterZwSize = _lastLetterZwSize;
                _latinMode.BeforeLangChange -= this._latinMode_BeforeLangChange;
                _latinMode.Flush -= this._latinMode_Flush;
                other.SetLatinModeObject(_latinMode);
                this._latinMode = null;
            }

            #endregion

            public void Append(UString text)
            {
                foreach (UChar letter in text)
                {
                    Append(letter);
                }
            }

            public void Append(UChar letter)
            {
                _latinMode.Send(letter);

                if (_latinMode.CurrentLang != Lang.Latin)
                {
                    if (IsParagraphHead())
                    {
                        AppendHeadIndentGlue(letter);
                    }
                    else
                    {
                        AppendInterspaceJP(letter);
                    }
                    AppendLetterJP(letter);
                }
                else
                {
                    if (IsParagraphHead())
                    {
                        AppendHeadIndentGlue(letter);
                    }
                    _lastLetter = letter;
                    _lastLetterZwSize = _currentZwSize;
                }
            }

            public void EndOfLine()
            {
                var lastGlue = _advancing.LineTailGlueJP(_lastLetter, _lastLetterZwSize);
                _buffer.Add(new JapaneseEndOfLineSpace(lastGlue));
                _lastLetter = default(UChar);
                _lastLetterZwSize = 0F;
            }

            private static LatinWord space = new LatinWord(new UString(" "), 0.5f);

            private void _latinMode_Flush(object sender, LatinMode.FlushEventArgs e)
            {
                var wordBuffer = new UStringBuilder(16);
                var latinText = e.LatinText;
                foreach (var c in e.LatinText)
                {
                    if (c.CodePoint == (int)' ')
                    {
                        FlushLatinWord(wordBuffer);

                        _buffer.Add(new LatinInterwordSpace(new GlueProperty(0.5F * _currentZwSize, 0.2F * _currentZwSize, 0.25F * _currentZwSize)));
                    }
                    else
                    {
                        wordBuffer.Append(c);
                    }
                    //TODO: Sentence space: after period except after some words "Mr", "Mrs"
                }
                FlushLatinWord(wordBuffer);
            }

            private void FlushLatinWord(UStringBuilder wordBuffer)
            {
                if (wordBuffer.Length > 0)
                {
                    var word = wordBuffer.ToUString();
                    var length = _latinMetric.MeasureText(word.String);
                    this.AppendObject(new LatinWord(word, length));
                    wordBuffer.Clear();
                }
            }

            private void _latinMode_BeforeLangChange(object sender, LatinMode.LangChangeEventArgs e)
            {
                if ((e.New == Lang.Latin && e.Old == Lang.Japanese) ||
                    (e.New == Lang.Japanese && e.Old == Lang.Latin))
                {
                    //TODO: InterspaceBetweenLatinAndJp
                    //TODO: beforeGlue and indent
                    var standardSpaceZw = 1.0F / 3 ;
                    _buffer.Add(new JapaneseInterletterspace(
                        new GlueProperty(standardSpaceZw * _currentZwSize, (1.0F / 2 - standardSpaceZw) * _currentZwSize, (standardSpaceZw - 1.0F / 4) * _currentZwSize),
                        default(GlueProperty),
                        0F,
                        false));
                }
            }

            private void FlushLatinBuffer()
            {
                _latinMode.ForceFlush();
            }

            private void AppendInterspaceJP(UChar letter)
            {
                var isProhibited = _wordWrap.IsProhibited(_lastLetter, letter);
                var glue = _advancing.InterletterGlueJP(_lastLetter, _lastLetterZwSize, letter, _currentZwSize);
                var beforeGlue = _advancing.LineTailGlueJP(_lastLetter, _lastLetterZwSize);
                var indent = _advancing.FollowingLineIndent(letter, _currentZwSize);
                _buffer.Add(new JapaneseInterletterspace(glue, beforeGlue, indent, isProhibited));
            }

            private void AppendHeadIndentGlue(UChar letter)
            {
                var indent = _advancing.FirstLineIndent(letter, _currentZwSize);
                _buffer.Add(new ParagraphHeadIndent(indent));
            }

            private void AppendLetterJP(UChar letter)
            {
                _buffer.Add(new JapaneseLetter(letter, _advancing.LengthJPByZw(letter, _currentZwSize)));
                _lastLetter = letter;
                _lastLetterZwSize = _currentZwSize;
            }
            
            private bool IsParagraphHead()
            {
                return _lastLetter == default(UChar);
            }
        }

        public ParagraphBuilder(ILatinWordMetric latinMetric, ParagraphStyle style)
        {
            _wordWrap = new WordWrapStrategy();
            _advancing = new AdvancingStrategy(style.Indent);
            _latinMetric = latinMetric;
            _zwSize = style.FontSize;
            _rubyZwSize = style.FontSize * style.RubyFontSizeRatio;
        }

        public void BeginParagraph()
        {
            _buffer = new TextBuffer(_zwSize, _wordWrap, _advancing, _latinMetric, 256);
        }

        public TextEmphasysDot TextEmphasysDot
        {
            set
            {
                _buffer.AppendObject(new TextEmphasysDotChange(value));
            }
        }

        public void Text(UString text)
        {
            _buffer.Append(text);
        }

        public void Text(UChar letter)
        {
            _buffer.Append(letter);
        }

        public void TextWithGroupRuby(UString baseText, UString rubyText)
        {
            var baseBuffer = new TextBuffer(_zwSize, _wordWrap, _advancing, _latinMetric, 64);
            var rubyBuffer = new TextBuffer(_rubyZwSize, _wordWrap, _advancing, _latinMetric, 128);
            _buffer.MoveLastLetterStateTo(baseBuffer);
            baseBuffer.Append(baseText);
            rubyBuffer.Append(rubyText);
            _buffer.AppendObject(new GroupRuby(baseBuffer.ToArray(), rubyBuffer.ToArray()));
            baseBuffer.MoveLastLetterStateTo(_buffer);
            baseBuffer.Clear();
            rubyBuffer.Clear();
        }

        public ParagraphModel EndParagraph()
        {
            _buffer.EndOfLine();
            var retval = new ParagraphModel(_buffer.ToArray());
            _buffer.Clear();
            _buffer = null;
            return retval;
        }
    }
}