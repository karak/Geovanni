using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextComposing
{
    /// <summary>
    /// 圏点設定
    /// </summary>
    public enum TextEmphasysDot
    {
        /// <summary>
        /// なし
        /// </summary>
        None,
        /// <summary>
        /// ごま
        /// </summary>
        Sesami
    }

    /// <summary>
    /// 行内の文字列スタイル設定
    /// </summary>
    public class InlineStyle
    {
        private TextEmphasysDot _textEmphasysDot;

        public TextEmphasysDot TextEmphasysDot
        {
            get { return _textEmphasysDot; }
            set { _textEmphasysDot = value; }
        }

        public InlineStyle()
        {
            _textEmphasysDot = TextEmphasysDot.None;
        }

        public InlineStyle Clone()
        {
            return new InlineStyle
            {
                TextEmphasysDot = this.TextEmphasysDot
            };
        }
    }
}
