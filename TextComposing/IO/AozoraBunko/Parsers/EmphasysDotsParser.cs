using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace TextComposing.IO.AozoraBunko.Parsers
{
    static class HeadingParser
    {
        private static Regex _heading = new Regex("^「([^」]*)」は([大中小])見出し$", RegexOptions.Compiled);

        public static UString Parse(UString annotationText)
        {
            var match = _heading.Match(annotationText.String);
            if (match.Success)
            {
                var text = match.Groups[1].Value;
                var scale = ParseScale(match.Groups[1].Value[0]);
                return new UString(text);
            }
            else
            {
                return null;
            }
        }

        private static int ParseScale(char scale)
        {
            switch (scale)
            {
                case '大':
                    return 1;
                case '小':
                    return 3;
                case '中':
                default:
                    return 2;
            }
        }
    }
}
