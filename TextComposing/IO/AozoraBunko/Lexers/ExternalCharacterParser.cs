using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextComposing.IO.AozoraBunko.Lexers
{
    /// <summary>
    /// 外字参照の置き換え処理
    /// </summary>
    ///<remarks>
    /// ルビやその他の注記の前方参照より前に実施する必要がある
    /// </remarks>
    public static class ExternalCharacterParser
    {
        private static readonly UChar UnreplacedPlaceholder = SpecialCharacters.ExternalCharacterPlaceholder;

        public static IEnumerable<UChar> Filter(IEnumerable<UChar> inputStream)
        {
            return ParseNormal(inputStream.GetEnumerator());
        }

        private static LazyList<UChar> ParseNormal(IEnumerator<UChar> inputStream)
        {
            if (!inputStream.MoveNext())
            {
                return LazyList<UChar>.New();
            }
            var c = inputStream.Current;
            if (c == SpecialCharacters.ExternalCharacterPlaceholder)
            {
                return ParseAfterPlaceholder(inputStream);
            }
            else
            {
                return LazyList<UChar>.New(c, () => ParseNormal(inputStream));
            }
        }
        private static LazyList<UChar> ParseAfterPlaceholder(IEnumerator<UChar> inputStream)
        {
            if (!inputStream.MoveNext())
            {
                return LazyList<UChar>.New();
            }
            var c = inputStream.Current;
            if (c == SpecialCharacters.AnnotationOpenBracket)
            {
                return ParseAfterBracket(inputStream);
            }
            else
            {
                return LazyList<UChar>.New(c, () => ParseNormal(inputStream));
            }
        }
        private static LazyList<UChar> ParseAfterBracket(IEnumerator<UChar> inputStream)
        {
            if (!inputStream.MoveNext())
            {
                return LazyList<UChar>.New();
            }
            var c = inputStream.Current;
            if (c == SpecialCharacters.AnnotationInitiatorChar)
            {
                return ParseAnnotation(inputStream);
            }
            else
            {
                return LazyList<UChar>.New(c, () => ParseNormal(inputStream));
            }
        }

        private static LazyList<UChar> ParseAnnotation(IEnumerator<UChar> inputStream)
        {
            var content = new UStringBuilder(64);
            while (inputStream.MoveNext())
            {
                var c = inputStream.Current;
                if (c == SpecialCharacters.AnnotationCloseBracket)
                {
                   string replaced;
                    if (ExternalCharacterDictionary.DoesMatch(content.ToString(), out replaced))
                    {
                        content.Clear();
                        return Seq(new UString(replaced).ToArray(), () => ParseNormal(inputStream));
                    }
                    else
                    {
                        content.Clear();
                        return Seq(new UChar[] { UnreplacedPlaceholder }, () => ParseNormal(inputStream));
                    }
                }
                else
                {
                    content.Append(c);
                }
            }
            content.Clear();
            return Seq(content.ToUString().ToArray());
        }
        private static LazyList<UChar> Seq(UChar[] proceeding, System.Func<LazyList<UChar>> following)
        {
            if (proceeding.Length == 0)
                return following();
            else
            {
                var slice = new UChar[proceeding.Length - 1];
                Array.Copy(proceeding, 1, slice, 0, slice.Length);
                return LazyList<UChar>.New(proceeding[0], () => Seq(slice, following));
            }
        }
        private static LazyList<UChar> Seq(UChar[] proceeding)
        {
            if (proceeding.Length == 0)
                return LazyList<UChar>.New();
            else
            {
                var cdr = new UChar[proceeding.Length - 1];
                Array.Copy(proceeding, 1, cdr, 0, cdr.Length);
                return LazyList<UChar>.New(proceeding[0], () => Seq(cdr));
            }
        }
    }
}
