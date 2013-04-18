using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextComposing
{
    /// <summary>
    /// JLReq の文字クラス
    /// </summary>
    ///http://www.w3.org/TR/jlreq/ja/#about_character_classes
    ///<remarks>
    ///全角文字（FULLWIDTH ...）の対応する文字も加えている
    /// </remarks>
    public class CharacterClasses
    {
        /// <summary>
        /// 始め括弧類
        /// </summary>
        public static bool Cl01(UChar ch)
        {
            return Cl0X(1, ch);
        }
        
        /// <summary>
        /// 終わり括弧類
        /// </summary>
        public static bool Cl02(UChar ch)
        {
            return Cl0X(2, ch);
        }
        
        /// <summary>
        /// ハイフン類
        /// </summary>
        public static bool Cl03(UChar ch)
        {
            return Cl0X(3, ch);
        }
        
        /// <summary>
        /// 区切り約物（疑問符、感嘆符類）
        /// </summary>
        public static bool Cl04(UChar ch)
        {
            return Cl0X(4, ch);
        }
        
        /// <summary>
        /// 中点類
        /// </summary>
        public static bool Cl05(UChar ch)
        {
            return Cl0X(5, ch);
        }

        /// <summary>
        /// 句点類
        /// </summary>
        public static bool Cl06(UChar ch)
        {
            return Cl0X(6, ch);
        }
        /// <summary>
        /// 読点類
        /// </summary>
        public static bool Cl07(UChar ch)
        {
            return Cl0X(7, ch);
        }

        /// <summary>
        /// 分離禁止
        /// </summary>
        public static bool Cl08(UChar proceeding, UChar following)
        {
            switch (proceeding.CodePoint)
            {
                case 0x2014:
                case 0x2026:
                case 0x2025:
                    return proceeding == following;
                case 0x3033:
                case 0x3034:
                    return following.CodePoint == 0x3035;
                default:
                    return false;
            }
        }

        public static bool Cl08(UChar nonCosinderPairing)
        {
            switch (nonCosinderPairing.CodePoint)
            {
                case 0x2014:
                case 0x2026:
                case 0x2025:
                case 0x3033:
                case 0x3034:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 繰返し記号
        /// </summary>
        public static bool Cl09(UChar ch)
        {
            return Cl0X(9, ch);
        }

        /// <summary>
        /// 長音記号
        /// </summary>
        public static bool Cl10(UChar ch)
        {
            return Cl0X(10, ch);
        }

        /// <summary>
        /// 小書きの仮名
        /// </summary>
        public static bool Cl11(UChar ch)
        {
            return Cl0X(11, ch);
            //NOTE: equivalent to "IsSmallKatakana(c) || IsSmallHiragana(c)"
        }

        /// <summary>
        /// 平仮名（小書き除く）
        /// </summary>
        public static bool Cl15(UChar ch)
        {
            return IsLargeHiragana(ch);
        }

        /// <summary>
        /// 片仮名（小書き除く）
        /// </summary>
        public static bool Cl16(UChar ch)
        {
            return IsLargeKatakana(ch);
        }

#region hiragana
        public static bool IsHiragana(UChar ch)
        {
            return InRange(ch.CodePoint, 0x3040, 0x309F);
            //か゚	<304B, 309A>などは省く
        }

        public static bool IsLargeHiragana(UChar ch)
        {
            return IsHiragana(ch) && !IsSmallHiragana(ch);
        }

        private static readonly int[] smallHiraganas = new int[] { 0x3041, 0x3043, 0x3045, 0x3047, 0x3049, 0x3063, 0x3083, 0x3085, 0x3087, 0x308E, 0x3095, 0x3096 };
        public static bool IsSmallHiragana(UChar ch)
        {
            return Array.IndexOf(smallHiraganas, ch.CodePoint) != -1;
        }
#endregion

#region Katakana
        public static bool IsKatakana(UChar ch)
        {
            return IsBasicKatakana(ch) || IsBasicSmallKatakana(ch) || IsExSmallKatakana(ch);
        }

        public static bool IsLargeKatakana(UChar ch)
        {
            return IsBasicKatakana(ch) && !IsBasicSmallKatakana(ch);
        }

        private static bool IsSmallKatakana(UChar ch)
        {
            return IsBasicSmallKatakana(ch) || IsExSmallKatakana(ch);
        }

        private static bool IsBasicKatakana(UChar ch)
        {
            return InRange(ch.CodePoint, 0x30A0, 0x30FF);
        }

        private static readonly int[] basicSmallKatakanas = new int[] { 0x30A1,0x30A3,0x30A5,0x30A7,0x30A9,0x30C3,0x30E3,0x30E5,0x30E7,0x30EE };
        private static bool IsBasicSmallKatakana(UChar ch)
        {
            return Array.IndexOf(basicSmallKatakanas, ch.CodePoint) != -1;
        }

        private static bool IsExSmallKatakana(UChar ch)
        {
            return InRange(ch.CodePoint, 0x30F5, 0x31FF);
            //ㇷ゚	<31F7, 309A>は省く
        }
#endregion
        /// <summary>
        /// 漢字など
        /// </summary>
        /// <remarks>
        /// 不完全
        /// </remarks>
        public static bool Cl19(UChar ch)
        {
            return IsCJKIdeograph(ch) || IsLatin(ch);
        }

        /// <summary>
        /// CJK統合漢字かどうか
        /// </summary>
        public static bool IsCJKIdeograph(UChar ch)
        {
            var code = ch.CodePoint;
            return
                //CJK Unified Ideographs Extension A
                InRange(code, 0x3400, 0x4DBF) ||
                //CJK Unified Ideographs
                InRange(code, 0x4E00, 0x9FFF) ||
                //CJK Compatibility Ideographs
                InRange(code, 0xF900, 0xFAFF) ||
                //CJK Unified Ideographs Extension B-D, and CJK Compatibility Ideographs Supplement
                InRange(code, 0x20000, 0x2FA1F);
        }

        /// <summary>
        /// 広義のラテン文字かどうか
        /// </summary>
        public static bool IsLatin(UChar ch)
        {
            var code = ch.CodePoint;
            return
                InRange(code, 0x0041, 0x005a) || //Basic Latin uppercase letters
                InRange(code, 0x0061, 0x007a) || //Basic Latin lowercase letters
                InRange(code, 0x0080, 0x00FF) || //Latin Latin-1 Supplement
                InRange(code, 0x0100, 0x017F) || //Latin Extended-A
                InRange(code, 0x0180, 0x024F) || //Latin Extended-B
                InRange(code, 0xA720, 0xA7FF)    //Latin Extended-D
                ;
        }

        /// <summary>
        ///合印中の文字（cl-20）
        /// </summary>
        public static bool Cl20(UChar ch)
        {
            return false;
        }

        private static readonly int[] cl28 = new int[] { 0x0028, 0x3014, 0x005B };
        /// <summary>
        /// 割注始め括弧類（cl-28）
        /// </summary>
        public static bool Cl28(UChar ch)
        {
            return Array.IndexOf(cl28, ch) != -1;
        }

        private static readonly int[] cl29 = new int[] { 0x0029, 0x3015, 0x005D };
        /// <summary>
        /// 割注終わり括弧類（cl-29）
        /// </summary>
        public static bool Cl29(UChar ch)
        {
            return Array.IndexOf(cl29, ch) != -1;
        }

        private static bool Cl0X(int number, UChar ch)
        {
            return classes[number - 1].Contains(ch.CodePoint);
        }

        private static bool InRange(int x, int low, int high)
        {
            return low <= x && x <= high;
        }

        private static int[][] classes = new int[][]
        {
            //TODO: add fullwidth compatible to all
            new int[] { 0x2018,0x201c,0x0028,0x3014,0x005b,0x007b,0x3008,0x300a,0x300c,0x300E,0x3010,0x2985,0x3018,0x3016,0x00ab,0x301d,
                        0xFF08,0xFF3B,0xFF5F}, //fullwidth
            new int[] { 0x2019,0x201D,0x0029,0x3015,0x005D,0x007D,0x3009,0x300B,0x300D,0x300F,0x3011,0x2986,0x3019,0x3017,0x00BB,0x301F,
                0xFF09,0xFF3D,0xFF60 }, //fullwidth
            new int[] { 0x2010,0x301C,0x30A0,0x2013 },
            new int[] { 0x0021,0x003F,0x203C,0x2047,0x2048,0x2049,
                0xFF01,0xFF1F }, //fullwidth
            new int[] { 0x30FB,0x003A,0x003B },
            new int[] { 0x3002,0x002E },
            new int[] { 0x3001,0x002C },
            null, //cl-08 分離禁止文字。分離禁止規則が特殊（組み合わせ）なのでこうは記さない。
            new int[] { 0x30FD,0x30FE,0x309D,0x3005,0x303B },
            new int[] { 0x30FC }, //cl-10 長音記号
            new int[] { 0x3041,0x3043,0x3045,0x3047,0x3049,0x30A1,0x30A3,0x30A5,0x30A7,0x30A9,0x3063,0x3083,0x3085,0x3087,0x308E,0x3095,0x3096,0x30C3,0x30E3,0x30E5,0x30E7,0x30EE,0x30F5,0x30F6,0x31F0,0x31F1,0x31F2,0x31F3,0x31F4,0x31F5,0x31F6,0x31F7,0x31F8,0x31F9,0x31FA,0x31FB,0x31FC,0x31FD,0x31FE,0x31FF } //cl-11「小書き半濁点付き片仮名フ」はサロゲートペアが使われるので扱わないものとする。
        };
    }
}
