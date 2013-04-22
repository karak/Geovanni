using System;
using System.Collections.Generic;

namespace TextComposing.IO.AozoraBunko.Lexers
{
    /// <summary>
    /// 外字置き換え辞書
    /// </summary>
    internal static partial class ExternalCharacterDictionary
    {
        private static System.Text.RegularExpressions.Regex _specialChars =
            new System.Text.RegularExpressions.Regex(@"^[二の字点|ます記号|コト|より|歌記号|濁点付き平仮名う|濁点付き片仮名ヰ|濁点付き片仮名ヱ|濁点付き片仮名ヲ|感嘆符二つ|疑問符二つ|疑問符感嘆符|感嘆符疑問符|ローマ数字\d+小文字|ローマ数字\d+|丸\d+|ファイナルシグマ]、(面区点番号)?(\d+)-(\d+)-(\d+)$");
        private static System.Text.RegularExpressions.Regex _jisX0123 =
            new System.Text.RegularExpressions.Regex(@"^「[^」]+」、(第[34]水準)?(\d+)-(\d+)-(\d+)$");
        private static System.Text.RegularExpressions.Regex _unicode =
            new System.Text.RegularExpressions.Regex(@"^「[^」]+」、U\+([0-9A-F]+)、\d+-\d+$");

        public static bool DoesMatch(string annotationText, out string unicodeChar)
        {
            if (DoesMatchSpecialChars(annotationText, out unicodeChar) ||
                DoesMatchJisX0123(annotationText, out unicodeChar) ||
                DoesMatchUnicode(annotationText, out unicodeChar))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 二の字点などの特殊仮名、記号
        /// </summary>
        private static bool DoesMatchSpecialChars(string annotationText, out string unicodeChar)
        {
            var match = _specialChars.Match(annotationText);
            if (match.Success)
            {
                //面区点
                var plane = Byte.Parse(match.Groups[2].Value);
                var row = Byte.Parse(match.Groups[3].Value);
                var cell = Byte.Parse(match.Groups[4].Value);
                return Jis2Ucs(plane, row, cell, out unicodeChar);
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
                return Jis2Ucs(plane, row, cell, out unicodeChar);
            }
            else
            {
                unicodeChar = default(string);
                return false;
            }
        }

        private static bool Jis2Ucs(byte plane, byte row, byte cell, out string unicodeChar)
        {
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
