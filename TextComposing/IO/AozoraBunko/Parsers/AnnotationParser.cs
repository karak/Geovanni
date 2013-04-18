using System;
using System.Collections.Generic;
using System.Text;

namespace TextComposing.IO.AozoraBunko.Parsers
{
    //注記パーサー
    //WARNING: 入れ子注記は最上位のみ処理されます。例：［＃「豌豆」は底本では「※［＃「足＋宛」、第3水準1-92-36］豆」］
    internal static class AnnotationParser
    {
        private static readonly UChar _externalCharacterPlaceholder = SpecialCharacters.ExternalCharacterPlaceholder;

        public class TokenVisitor
        {
            public System.Action<UString> OnBody;
            public System.Action<UString> OnAnnotation;
            public System.Action OnPlaceholder;
        }

        public interface IToken
        {
            void Accept(TokenVisitor visitor);
        }

        private sealed class BodyToken : IToken
        {
            public UString Text;

            void IToken.Accept(TokenVisitor visitor)
            {
                visitor.OnBody(Text);
            }
        }

        private sealed class AnnotationToken : IToken
        {
            public UString Text;

            void IToken.Accept(TokenVisitor visitor)
            {
                visitor.OnAnnotation(Text);
            }
        }

        /// <summary>
        /// 外字指定注記のためのプレースホルダ文字
        /// </summary>
        private sealed class PlaceholderToken : IToken
        {
            void IToken.Accept(TokenVisitor visitor)
            {
                visitor.OnPlaceholder();
            }
        }

        public static IEnumerable<IToken> Parse(IEnumerable<UChar> inputString)
        {
            return ParseRoot(inputString.GetEnumerator());
        }

        private static LazyList<IToken> ParseRoot(IEnumerator<UChar> inputStream)
        {
            var buffer = new UStringBuilder(512);
            while (inputStream.MoveNext())
            {
                UChar c1 = inputStream.Current;
                if (c1 == SpecialCharacters.AnnotationOpenBracket)
                {
                    if (inputStream.MoveNext())
                    {
                        UChar c2 = inputStream.Current;
                        if (c2 == SpecialCharacters.AnnotationInitiatorChar)
                        {
                            return LazyList<IToken>.New(
                                new BodyToken { Text = buffer.ToUString() },
                                () => ParseAnnotation(inputStream)
                            );
                        }
                        else
                        {
                            buffer.Append(c1);
                            buffer.Append(c2);
                        }
                    }
                    else
                    {
                        buffer.Append(c1);
                    }
                }
                else if (c1 == AnnotationParser._externalCharacterPlaceholder)
                {
                    return LazyList<IToken>.New(
                        new PlaceholderToken(),
                        () => ParseRoot(inputStream)
                    );
                } else{
                    buffer.Append(c1);
                }
            }

            return LazyList<IToken>.New(
                new BodyToken { Text = buffer.ToUString() }
            );
        }
        private static LazyList<IToken> ParseAnnotation(IEnumerator<UChar> inputStream)
        {
            int depth = 0; //入れ子を無視する
            var buffer = new UStringBuilder(128);
            while (inputStream.MoveNext())
            {
                if (inputStream.Current == SpecialCharacters.AnnotationCloseBracket)
                {
                    if (depth > 0)
                    {
                        --depth;
                        buffer.Append(inputStream.Current);
                    }
                    else
                    {
                        return LazyList<IToken>.New(
                            new AnnotationToken { Text = buffer.ToUString() },
                            () => ParseRoot(inputStream)
                        );
                    }
                }
                else
                {
                    if (inputStream.Current == SpecialCharacters.AnnotationOpenBracket)
                    {
                        ++depth;
                    }
                    buffer.Append(inputStream.Current);
                }
            }
            //syntax-error
            return LazyList<IToken>.New(new AnnotationToken { Text = buffer.ToUString() });
        }
    }
}
