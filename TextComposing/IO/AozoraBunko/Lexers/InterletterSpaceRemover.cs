using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CC = TextComposing.CharacterClasses;

namespace TextComposing.IO.AozoraBunko.Lexers
{
    /// <summary>
    /// 組版規則によって実現される字間アキの指示を削除する
    /// </summary>
    internal static class InterletterSpaceRemover
    {
        private static UChar fullWidthSpace = new UChar('　');
        public static IEnumerable<UChar> Filter(IEnumerable<UChar> xs)
        {
            bool isPrevDividingPunctuation = false;
            foreach (var x in xs)
            {
                if (isPrevDividingPunctuation)
                {
                    if (x != fullWidthSpace)
                        yield return x;
                }
                else
                {
                    yield return x;
                }
                isPrevDividingPunctuation = CC.Cl04(x);
            }
        }
    }
}
