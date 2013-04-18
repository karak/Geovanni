using System;
using System.Collections.Generic;

namespace TextComposing.IO.AozoraBunko.Lexers
{
    /// <summary>
    /// 二倍長のかな繰り返し記号（踊り字）の処理
    /// </summary>
    public static class KanaRepeatingMarkParser
    {
        private static readonly UChar UpperHalfChar = new UChar('／');
        private static readonly UChar DownerHalfChar = new UChar('＼');
        private static readonly UChar VoicingChar = new UChar('″');
        private enum State
        {
            Normal,
            UpperHalf,
            UpperHalfVoiced
        }
        public static IEnumerable<UChar> Filter(IEnumerable<UChar> inputStream)
        {
            State state = State.Normal;
            foreach (UChar c in inputStream)
            {
                switch (state)
                {
                    case State.Normal:
                        if (c == UpperHalfChar)
                        {
                            state = State.UpperHalf;
                        }
                        else
                        {
                            yield return c;
                        }
                        break;
                    case State.UpperHalf:
                        if (c == DownerHalfChar)
                        {
                            yield return UChar.FromCodePoint(0x3033);
                            yield return UChar.FromCodePoint(0x3035);
                            state = State.Normal;
                        }
                        else if (c == VoicingChar)
                        {
                            state = State.UpperHalfVoiced;
                        }
                        else
                        {
                            yield return UpperHalfChar;
                            if (c == UpperHalfChar)
                            {
                                state = State.UpperHalf;
                            }
                            else
                            {
                                yield return c;
                                state = State.Normal;
                            }
                        }
                        break;
                    case State.UpperHalfVoiced:
                        if (c == DownerHalfChar)
                        {
                            yield return UChar.FromCodePoint(0x3034);
                            yield return UChar.FromCodePoint(0x3035);
                            state = State.Normal;
                        }
                        else
                        {
                            yield return UpperHalfChar;
                            if (c == UpperHalfChar)
                            {
                                state = State.UpperHalf;
                            }
                            else
                            {
                                yield return c;
                                state = State.Normal;
                            }
                        }
                        break;
                }

            }
        }
    }
}
