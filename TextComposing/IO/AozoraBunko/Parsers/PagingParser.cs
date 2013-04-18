using System;
using System.Text.RegularExpressions;

namespace TextComposing.IO.AozoraBunko.Parsers
{
    public static class PagingParser
    {
        private static Regex _newPage = new Regex("^［＃(改ページ)|(改丁)］$");
        //ここでは同義とみなす。実際は「改丁」は奇数ページあわせ

        public static bool Parse(string line)
        {
            return _newPage.IsMatch(line);
        }
    }
}
