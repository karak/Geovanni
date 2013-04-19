using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CC = TextComposing.CharacterClasses;

namespace TextComposing.IO.AozoraBunko.Lexers
{
     /// <summary>
     /// 字上符（ダイアクリティカルマーク）付きのアルファベットを置換する
     /// </summary>
    public static class AccentNotationParser
    {
        private static UChar nul = new UChar('\0');
        private static UChar lf = new UChar('\n');
        private static UChar open = new UChar('〔');
        private static UChar close = new UChar('〕');
        private static UChar circumflex = new UChar('^');
        private static UChar tilde = new UChar('~');
        private static UChar acute = new UChar('\'');
        private static UChar atmark = new UChar('@');

        public static IEnumerable<UChar> Filter(IEnumerable<UChar> inputStream)
        {
            bool enabled = false;
            UChar prev = nul;

            foreach (var c in inputStream)
            {
                if (enabled)
                {
                    if (c == close)
                    {
                        if (prev != nul)
                        {
                            yield return prev;
                            prev = nul;
                        }
                        enabled = false;
                    }
                    else if (c == lf)
                    {
                        if (prev != nul)
                        {
                            yield return prev;
                            prev = nul;
                        }
                        yield return lf;
                        enabled = false;
                    }
                    else if (c == circumflex)
                    {
                        switch (prev.CodePoint)
                        {
                            /* circumflex */
                            case 0x0041: //'A'
                                yield return UChar.FromCodePoint(0x00C2);
                                break;
                            case 0x0061: //'a'
                                yield return UChar.FromCodePoint(0x00E2);
                                break;
                            case 0x0043: //'C'
                                yield return UChar.FromCodePoint(0x0108);
                                break;
                            case 0x0063: //'c'
                                yield return UChar.FromCodePoint(0x0109);
                                break;
                            case 0x0045: //'E'
                                yield return UChar.FromCodePoint(0x00CA);
                                break;
                            case 0x0065: //'e'
                                yield return UChar.FromCodePoint(0x00EA);
                                break;
                            case 0x0047: //'G'
                                yield return UChar.FromCodePoint(0x011C);
                                break;
                            case 0x0067: //'g'
                                yield return UChar.FromCodePoint(0x011D);
                                break;
                            case 0x0048: //'H'
                                yield return UChar.FromCodePoint(0x0124);
                                break;
                            case 0x0068: //'h'
                                yield return UChar.FromCodePoint(0x0125);
                                break;
                            case 0x0049: //'I'
                                yield return UChar.FromCodePoint(0x00CE);
                                break;
                            case 0x0069: //'i'
                                yield return UChar.FromCodePoint(0x00EE);
                                break;
                            case 0x004A: //'J'
                                yield return UChar.FromCodePoint(0x0134);
                                break;
                            case 0x006A: //'j'
                                yield return UChar.FromCodePoint(0x0135);
                                break;
                            case 0x004E: //'N'
                                yield return UChar.FromCodePoint(0x004E);
                                yield return UChar.FromCodePoint(0x0302);
                                break;
                            case 0x006E: //'n'
                                yield return UChar.FromCodePoint(0x006E);
                                yield return UChar.FromCodePoint(0x0302);
                                break;
                            case 0x004F: //'O'
                                yield return UChar.FromCodePoint(0x00D4);
                                break;
                            case 0x006F: //'o'
                                yield return UChar.FromCodePoint(0x00F4);
                                break;
                            case 0x0053: //'S'
                                yield return UChar.FromCodePoint(0x015C);
                                break;
                            case 0x0073: //'s'
                                yield return UChar.FromCodePoint(0x015D);
                                break;
                            case 0x0057: //'W'
                                yield return UChar.FromCodePoint(0x0174);
                                break;
                            case 0x0077: //'w'
                                yield return UChar.FromCodePoint(0x0175);
                                break;
                            case 0x0059: //'Y'
                                yield return UChar.FromCodePoint(0x0176);
                                break;
                            case 0x0079: //'y'
                                yield return UChar.FromCodePoint(0x0177);
                                break;
                            case 0x005A: //'Z'
                                yield return UChar.FromCodePoint(0x1E90);
                                break;
                            case 0x007A: //'z'
                                yield return UChar.FromCodePoint(0x1E91);
                                break;
                            default:
                                yield return prev;
                                yield return c;
                                break;
                        };
                        prev = nul;
                    }
                    else if (c == acute)
                    {
                        switch (prev.CodePoint)
                        {
                            /* acute */
                            case 0x0041: //'A'
                                yield return UChar.FromCodePoint(0x00C1);
                                break;
                            case 0x0061: //'a'
                                yield return UChar.FromCodePoint(0x00E1);
                                break;
                            case 0x0045: //'E'
                                yield return UChar.FromCodePoint(0x00C9);
                                break;
                            case 0x0065: //'e'
                                yield return UChar.FromCodePoint(0x00E9);
                                break;
                            case 0x0049: //'I'
                                yield return UChar.FromCodePoint(0x00CD);
                                break;
                            case 0x0069: //'i'
                                yield return UChar.FromCodePoint(0x00ED);
                                break;
                            case 0x004F: //'O'
                                yield return UChar.FromCodePoint(0x00D3);
                                break;
                            case 0x006F: //'o'
                                yield return UChar.FromCodePoint(0x00F3);
                                break;
                            case 0x0055: //'U'
                                yield return UChar.FromCodePoint(0x00DA);
                                break;
                            case 0x0075: //'u'
                                yield return UChar.FromCodePoint(0x00FA);
                                break;
                            default:
                                yield return prev;
                                yield return c;
                                break;
                        };
                        prev = nul;
                    }
                    else if (c == tilde)
                    {
                        switch (prev.CodePoint)
                        {
                            /* tilde */
                            case 0x0041: //'A'
                                yield return UChar.FromCodePoint(0x00C3);
                                break;
                            case 0x0061: //'a'
                                yield return UChar.FromCodePoint(0x00E3);
                                break;
                            case 0x004E: //'N'
                                yield return UChar.FromCodePoint(0x00D1);
                                break;
                            case 0x006E: //'n'
                                yield return UChar.FromCodePoint(0x00F1);
                                break;
                            case 0x004F: //'O'
                                yield return UChar.FromCodePoint(0x00D5);
                                break;
                            case 0x006F: //'o'
                                yield return UChar.FromCodePoint(0x00F5);
                                break;
                            default:
                                yield return prev;
                                yield return c;
                                break;
                        };
                        prev = nul;
                    }
                    else if (c == atmark && prev.CodePoint == 0x003F)
                    {
                        yield return UChar.FromCodePoint(0x00BF);
                        prev = nul;
                    }
                    else
                    {
                        if (prev != nul)
                        {
                            yield return prev;
                        }
                        prev = c;
                    }
                }
                else
                {
                    if (c == open)
                    {
                        enabled = true;
                    }
                    else
                    {
                        yield return c;
                    }
                }
            }
        }
    }
}
