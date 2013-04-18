using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextComposing.IO.AozoraBunko.Lexers
{
    /// <summary>
    /// 全角ダッシュの置き換え
    /// </summary>
    /// <remarks>
    /// UnicodeのShift-JIS および Microsoftのcp932では
    /// U+2014エムダッシュがU+2015ホリゾンタルバーに化けている。
    /// よってShift-Jisベースの青空文庫形式でもそうなる。
    /// </remarks>
    internal static class EmDashReplacer
    {
        private static UChar hbar = UChar.FromCodePoint(0x2015);
        private static UChar emdash = UChar.FromCodePoint(0x2014);

        public static IEnumerable<UChar> Filter(IEnumerable<UChar> inputStream)
        {
            return from x in inputStream select (x == hbar ? emdash : x);
        }
    }
}
