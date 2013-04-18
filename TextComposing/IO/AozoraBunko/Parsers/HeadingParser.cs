using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace TextComposing.IO.AozoraBunko.Parsers
{
    static class EmphasysDotsParser
    {
        private static Regex _heading = new Regex("^「([^」]*)」に傍点$", RegexOptions.Compiled);

        public static UString Parse(UString annotationText)
        {
            var match = _heading.Match(annotationText.String);
            if (match.Success)
                return new UString(match.Groups[1].Value);
            else
                return null;
        }
    }
}
