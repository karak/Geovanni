using System;
using System.Collections.Generic;

namespace TextComposing.IO.AozoraBunko.Lexers
{
    /// <summary>
    /// 外字置き換え辞書
    /// </summary>
    internal static partial class ExternalCharacterDictionary
    {
        private static System.Text.RegularExpressions.Regex _twoIdeograph =
            new System.Text.RegularExpressions.Regex(@"^二の字点、(面区点番号)?1-2-22$");
        private static System.Text.RegularExpressions.Regex _jisX0123 =
            new System.Text.RegularExpressions.Regex(@"^「[^」]+」、(第[34]水準)?(\d+)-(\d+)-(\d+)$");
        private static System.Text.RegularExpressions.Regex _unicode =
            new System.Text.RegularExpressions.Regex(@"^「[^」]+」、U\+([0-9A-F]+)、\d+-\d+$");

        public static bool DoesMatch(string annotationText, out string unicodeChar)
        {
            if (DoesMatchJisX0123(annotationText, out unicodeChar) ||
                DoesMatchUnicode(annotationText, out unicodeChar) ||
                DoesMatchTwoIdeograph(annotationText, out unicodeChar))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 二の字点
        /// </summary>
        private static bool DoesMatchTwoIdeograph(string annotationText, out string unicodeChar)
        {
            var match = _twoIdeograph.Match(annotationText);
            if (match.Success)
            {
                unicodeChar = "\u303B";
                return true;
            }
            else
            {
                unicodeChar = default(string);
                return false;
            }
        }
        /// <summary>
        /// JIS X 0123 第三、第四水準の字
        /// </summary>
        private static bool DoesMatchJisX0123(string annotationText, out string unicodeChar)
        {
            var match = _jisX0123.Match(annotationText);
            if (match.Success)
            {
                //面区点
                var plane = Byte.Parse(match.Groups[2].Value);
                var row = Byte.Parse(match.Groups[3].Value);
                var cell = Byte.Parse(match.Groups[4].Value);
                int code = JisX213.ToUcs(plane, row, cell);
                if (code == -1)
                {
                    //未対応
                    unicodeChar = default(string);
                    return false;
                }
                unicodeChar = Char.ConvertFromUtf32(code);
                return true;
            }
            else
            {
                unicodeChar = default(string);
                return false;
            }
        }

        /// <summary>
        /// Unicode の字
        /// </summary>
        private static bool DoesMatchUnicode(string annotationText, out string unicodeChar)
        {
            var match = _unicode.Match(annotationText);
            if (match.Success)
            {
                var pointString = match.Groups[1].Value;
                var codePoint = ParseHexNumber(pointString);
                unicodeChar = Char.ConvertFromUtf32(codePoint);
                return true;
            }
            else
            {
                unicodeChar = default(string);
                return false;
            }
        }

        private static int ParseHexNumber(string s)
        {
            int n = 0;
            foreach (char c in s)
            {
                int i = "0123456789ABCDEF".IndexOf(c);
                if (i == -1) throw new ArgumentOutOfRangeException("半角アラビア数字のみ受け付けます");
                n <<= 4;
                n += i;
            }
            return n;
        }
    }
}
