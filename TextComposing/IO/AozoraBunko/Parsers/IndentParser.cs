using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TextComposing.IO.AozoraBunko.Parsers
{
    public class IndentParser
    {
        private static string _number = "([１２３４５６７８９０]+)";
        private static Regex _headSpacePattern = new Regex(@"^(　+)(.+)$");
        private static Regex _oneLinePattern = new Regex(@"^［＃(天から)?" + _number + @"字下げ］(.+)$");
        private static Regex _startPattern = new Regex(@"^［＃ここから" + _number + @"字下げ］$");
        private static Regex _startOneTwoPattern = new Regex(@"^［＃ここから" + _number + @"字下げ、折り返して" + _number + @"字下げ］$");
        private static Regex _startZeroTwoPattern = new Regex(@"^［＃ここから改行天付き、折り返して" + _number + @"字下げ］$");
        private static Regex _endPattern = new Regex(@"^［＃ここで字下げ終わり］$");

        private abstract class IndentSetting
        {
            public readonly int TextIndent;
            public readonly int ParagraphIndent;

            public IndentSetting(int textIndent, int paragraphIndent)
            {
                TextIndent = textIndent;
                ParagraphIndent = paragraphIndent;
            }
        }

        private class ManualIndentSetting : IndentSetting
        {
            public ManualIndentSetting(int paragraphIndent)
                : base(0, paragraphIndent)
            {
            }

            public ManualIndentSetting(int textIndent, int paragraphIndent)
                : base(textIndent, paragraphIndent)
            {
            }
        }


        private class NoIndentSetting : IndentSetting
        {
            public NoIndentSetting()
                : base(0, 0)
            {
            }
        }

        private static IndentSetting _noSetting = new NoIndentSetting();
        private IndentSetting _currentIndent = _noSetting;

        public bool IsSetIndent
        {
            get { return _currentIndent != _noSetting; }
        }

        /// <summary>
        /// 行頭字下げ（全角字数）
        /// </summary>
        public int CurrentTextIndent
        {
            get { return _currentIndent.TextIndent; }
        }

        /// <summary>
        /// 段落全体の字下げ（全角字数）
        /// </summary>
        public int CurrentParagraphIndent
        {
            get { return _currentIndent.ParagraphIndent; }
        }

        //NOTE: 入れ子ではな。言い換えると開始～終了はセットではないことに注意。連続する開始は書き換えを意味し、終了はなしに戻る。

        public IEnumerable<string> ReadLines(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                //同行字下げの処理
                {
                    var match = _oneLinePattern.Match(line);
                    if (match.Success)
                    {
                        var indentAmount = ParseFullwidthNumber(match.Groups[2].Value);
                        var stored = _currentIndent;
                        _currentIndent = new ManualIndentSetting(indentAmount);
                        yield return match.Groups[3].Value;
                        _currentIndent = stored;
                        continue;
                    }
                }

                //範囲字下げの開始
                {
                    var match = _startPattern.Match(line);
                    if (match.Success)
                    {
                        var indentAmount = ParseFullwidthNumber(match.Groups[1].Value);
                        _currentIndent = new ManualIndentSetting(indentAmount);
                        continue;
                    }
                }

                //範囲字下げ（折り返しつき）の開始
                {
                    var match = _startOneTwoPattern.Match(line);
                    if (match.Success)
                    {
                        var firstIndent = ParseFullwidthNumber(match.Groups[1].Value);
                        var paragraphIndent = ParseFullwidthNumber(match.Groups[2].Value);
                        _currentIndent = new ManualIndentSetting(firstIndent - paragraphIndent, paragraphIndent);
                        continue;
                    }
                }
                //範囲天付き（折り返しつき）の開始
                {
                    var match = _startZeroTwoPattern.Match(line);
                    if (match.Success)
                    {
                        var paragraphIndent = ParseFullwidthNumber(match.Groups[1].Value);
                        _currentIndent = new ManualIndentSetting(-paragraphIndent, paragraphIndent);
                        continue;
                    }
                }

                //範囲字下げの終了
                if (_endPattern.IsMatch(line))
                {
                    _currentIndent = _noSetting;
                    continue;
                }

                //全角空白による字下げの処理。範囲字下げ内では上書きでなく追加になる。
                //同行に注記による指定がない環境のみ有効
                //字下げなしの環境における一文字分の空白は、段落の目印と見做し、ここでは無視する
                //※段落のスタイルの別のところで指定する
                {
                    var match = _headSpacePattern.Match(line);
                    if (match.Success)
                    {
                        var moreIndentAmount = match.Groups[1].Value.Length;
                        if (IsSetIndent && moreIndentAmount == 1)
                        {
                            yield return match.Groups[2].Value;
                        }
                        else
                        {
                            var stored = _currentIndent;
                            _currentIndent = new ManualIndentSetting(
                                _currentIndent.TextIndent + moreIndentAmount, _currentIndent.ParagraphIndent);
                            yield return match.Groups[2].Value;
                            _currentIndent = stored;
                        }
                        continue;
                    }
                }


                yield return line;
            }
        }

        private static int ParseFullwidthNumber(string s)
        {
            int n = 0;
            foreach (char c in s)
            {
                int i = "０１２３４５６７８９".IndexOf(c);
                if (i == -1) throw new ArgumentOutOfRangeException("全角アラビア数字のみ受け付けます");
                n *= 10;
                n += i;
            }
            return n;
        }

    }
}
