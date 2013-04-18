using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextComposing.IO.AozoraBunko.Parsers
{

    //TODO: 小書き仮名の変換オプション追加
    internal static class RubyParser
    {
        #region parent type
        private abstract class ParentType
        {
            public abstract bool IsContinuous(UChar ch);
            public abstract bool IsInitiating(UChar ch);

            internal ParentType()
            {
            }
        }

        private sealed class NoParentType : ParentType
        {
            public override bool IsContinuous(UChar ch)
            {
                return false;
            }

            public override bool IsInitiating(UChar ch)
            {
                return false;
            }
        }

        private sealed class KanjiType : ParentType
        {
            public override bool IsContinuous(UChar ch)
            {
                return IsExCJKIdeograph(ch) ||
                    (new UString("々〆仝\u303b").Contains(ch));
            }

            public override bool IsInitiating(UChar ch)
            {
                return IsExCJKIdeograph(ch);
            }

            private static bool IsExCJKIdeograph(UChar ch)
            {
                return CharacterClasses.IsCJKIdeograph(ch) ||
                       ch == SpecialCharacters.ExternalCharacterPlaceholder;
            }
        }

        private sealed class KatakanaType : ParentType
        {
            public override bool IsContinuous(UChar ch)
            {
                return CharacterClasses.IsKatakana(ch) ||
                    (new UString("ヽヾ").Contains(ch));
            }
            public override bool IsInitiating(UChar ch)
            {
                return CharacterClasses.IsLargeKatakana(ch);
            }
        }

        private sealed class LatinType : ParentType
        {
            public override bool IsContinuous(UChar ch)
            {
                return IsInitiating(ch) || (new UChar('-') == ch);
            }
            public override bool IsInitiating(UChar ch)
            {
                return CharacterClasses.IsLatin(ch);
            }
        }

        private static ParentType[] _someParentTypes = new ParentType[] {
            new KanjiType(), new LatinType()
        };
        private static ParentType _none = new NoParentType();
        static ParentType ClassifyParentType(UChar ch)
        {
            foreach (var type in _someParentTypes)
            {
                if (type.IsInitiating(ch))
                {
                    return type;
                }
            }
            return _none;
        }

        #endregion


        public class TokenVisitor
        {
            public delegate void NormalHandler(UString bodyText);
            public delegate void RubyHandler(UString parentText, UString rubyText);

            private static NormalHandler _noNormalHandler = x => { };
            private static RubyHandler _noRubyHandler = (x, y) => { };

            public NormalHandler OnNormal = _noNormalHandler;
            public RubyHandler OnRuby = _noRubyHandler;
        }

        public abstract class Token
        {
            protected internal Token()
            {
            }

            public abstract void Accept(TokenVisitor visitor);
        }

        private sealed class NormalToken : Token
        {
            public readonly UString Text;

            public NormalToken(UString text)
            {
                Text = text;
            }

            public override void Accept(TokenVisitor visitor)
            {
                visitor.OnNormal(this.Text);
            }

            public override string ToString()
            {
                return Text.ToString();
            }
        }

        private sealed class RubyToken : Token
        {
            public readonly UString RubyParentText;
            public readonly UString RubyText;

            public RubyToken(UString rubyParentText, UString rubyText)
            {
                RubyParentText = rubyParentText;
                RubyText = rubyText;
            }

            public override void Accept(TokenVisitor visitor)
            {
                visitor.OnRuby(RubyParentText, RubyText);
            }

            public override string ToString()
            {
                return String.Format("{0}《{1}》", RubyParentText, RubyText);
            }
        }

        public static IEnumerable<Token> Parse(IEnumerable<UChar> inputText)
        {
            var inputStream = inputText.GetEnumerator();
            return ParseNormal(inputStream);
        }

        private static LazyList<Token> ParseNormal(IEnumerator<UChar> inputStream)
        {
            UStringBuilder _textFragment = new UStringBuilder(1024);
            UStringBuilder _tentativeParent = new UStringBuilder(16);
            ParentType type = _none;

            while (inputStream.MoveNext())
            {
                var c = inputStream.Current;

                if (c == SpecialCharacters.BeforeRubyInitiater)
                {
                    _textFragment.Append(_tentativeParent.ToUString());
                    _tentativeParent.Clear();
                    var token = new NormalToken(_textFragment.ToUString());
                    _textFragment.Clear();
                    _textFragment = null;
                    _tentativeParent = null;
                    return LazyList<Token>.New(token, () => ParseRubyParent(inputStream));
                }
                else if (c == SpecialCharacters.RubyOpen)
                {
                    var token = new NormalToken(_textFragment.ToUString());
                    _textFragment.Clear();
                    _textFragment = null;
                    UString parent = _tentativeParent.ToUString();
                    _tentativeParent.Clear();
                    _tentativeParent = null;
                    return LazyList<Token>.New(token, () => ParseRubyText(inputStream, parent));
                }
                else
                {
                    //暫定親字の継続検証
                    if (type.IsContinuous(c))
                    {
                        _tentativeParent.Append(c);
                    }
                    else
                    {
                        _textFragment.Append(_tentativeParent.ToUString());
                        _tentativeParent.Clear();
                        _tentativeParent.Append(c);
                        type = RubyParser.ClassifyParentType(c);
                    }
                }
            }

            {
                _textFragment.Append(_tentativeParent.ToUString());
                _tentativeParent.Clear();
                var token = new NormalToken(_textFragment.ToUString());
                return LazyList<Token>.New(token);
            }
        }

        private static LazyList<Token> ParseRubyParent(IEnumerator<UChar> inputStream)
        {
            var parent = new UStringBuilder(16);
            while (inputStream.MoveNext())
            {
                var c = inputStream.Current;
                if (c == SpecialCharacters.RubyOpen)
                {
                    return ParseRubyText(inputStream, parent.ToUString());
                }
                else
                {
                    parent.Append(c);
                }
            }
            //WARNING: syntax-error
            return LazyList<Token>.New(new NormalToken(parent.ToUString()));
        }

        private static LazyList<Token> ParseRubyText(IEnumerator<UChar> inputStream, UString rubyParent)
        {
            var text = new UStringBuilder(32);
            while (inputStream.MoveNext())
            {
                var c = inputStream.Current;
                if (c == SpecialCharacters.RubyClose)
                {
                    var token = new RubyToken(rubyParent, text.ToUString());
                    return LazyList<Token>.New(token, () => ParseNormal(inputStream));
                }
                else
                {
                    text.Append(c);
                }
            }
            //WARNING: syntax-error
            //         dispose incomplete ruby
            return LazyList<Token>.New(new NormalToken(rubyParent));
        }
    }
}
