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

    internal class ManualParagraphIndentStyle : IParagraphIndentStyle
    {
        private readonly float _textIndent;
        private readonly float _paragraphIndent;

        public ManualParagraphIndentStyle(float textIndent, float paragraphIndent)
        {
            _textIndent = textIndent;
            _paragraphIndent = paragraphIndent;
        }

        float IParagraphIndentStyle.TextIndent(UChar firstLetter)
        {
            return _textIndent + IndentOnSpaceType(firstLetter);
        }

        float IParagraphIndentStyle.ParagraphIndent
        {
            get
            {
                return this._paragraphIndent;
            }
        }

        private static float IndentOnSpaceType(UChar firstLetter)
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
                    return 0F;
            }
        }
    }
}
