using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CC = TextComposing.CharacterClasses;

namespace TextComposing
{
    /// <summary>
    /// 段落の見た目
    /// </summary>
    public class ParagraphStyle
    {
        /// <summary>
        /// 本文フォントサイズ（ポイント）
        /// </summary>
        public float FontSize;

        /// <summary>
        /// ルビフォントサイズ（本文に対する割合）
        /// </summary>
        public float RubyFontSizeRatio;

        public IParagraphIndentStyle Indent;
    }

    public interface IParagraphIndentStyle
    {
        /// <summary>
        /// 段落先頭行の字下げ（字数）。最初の文字のフォントサイズ基準
        /// </summary>
        float TextIndent(UChar firstLetter);

        /// <summary>
        /// 段落全体の字下げ（字数）。段落指定のフォントサイズ基準
        /// </summary>
        float ParagraphIndent { get; }
    }

    internal class AutoParagraphIndentStyle : IParagraphIndentStyle
    {
        float IParagraphIndentStyle.TextIndent(UChar firstLetter)
        {
            switch (Formatting.SpaceTypeExtension.GetSpaceType(firstLetter))
            {
                case Formatting.SpaceType.Opening:
                    return 0.5F;
                case Formatting.SpaceType.MiddleDots:
                    return 0.25F;
                case Formatting.SpaceType.Closing:
                case Formatting.SpaceType.DividingPunctuation:
                case Formatting.SpaceType.Normal:
                default:
                    if (CC.IsCJKIdeograph(firstLetter) || (CC.IsHiragana(firstLetter) || CC.IsKatakana(firstLetter)))
                        return 1F;
                    else
                        return 0F;
            }
        }

        float IParagraphIndentStyle.ParagraphIndent
        {
            get { return 0F; }
        }
    }

    internal class ManualParagraphIndentStyle : IParagraphIndentStyle
    {
        public ManualParagraphIndentStyle(float textIndent, float paragraphIndent)
        {
            TextIndent = textIndent;
            ParagraphIndent = paragraphIndent;
        }

        public float TextIndent { get; private set; }
        public float ParagraphIndent { get; private set; }

        float IParagraphIndentStyle.TextIndent(UChar firstLetter)
        {
            return TextIndent;
        }
    }
}
