using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextComposing
{
    public enum PageSize
    {
        A5Portrait,
        A4Landscape
    }

    /// <summary>
    /// レイアウト設定（基本版面）
    /// </summary>
    public class Layout
    {
        public PageSize PageSize { get; private set; }
        /// <summary>
        /// ページの偶奇で余白を左右反転するか
        /// </summary>
        public bool Mirroring { get; private set; }
        public float RightMargin { get; private set; }
        public float TopMargin { get; private set; }
        public float PageNumberRightMargin { get; private set; }
        public float PageNumberTopMargin { get; private set; }
        public float PageHeaderOffset { get; private set; }
        public float FontSize { get; private set; }
        public float Leading { get; private set; }
        public int NumberOfLines { get; private set; }
        public int NumberOfRows { get; private set; }

        public static readonly Layout A5Pocket = new Layout
        {
            PageSize = PageSize.A5Portrait,
            Mirroring = true,
            RightMargin = 32,
            TopMargin = 55,
            PageNumberRightMargin = 32,
            PageNumberTopMargin = 30,
            PageHeaderOffset = 40,
            NumberOfLines = 17,
            NumberOfRows = 40,
            FontSize = 12.0F,
            Leading = 12.0F * 1.6F,
        };

        public static readonly Layout A4Manuscript = new Layout
        {
            PageSize = PageSize.A4Landscape,
            Mirroring = false,
            RightMargin = 70,
            TopMargin = 110,
            PageNumberRightMargin = 40,
            PageNumberTopMargin = 30,
            PageHeaderOffset = 40,
            NumberOfLines = 40,
            NumberOfRows = 30,
            FontSize = 12F,
            Leading = 12F * 1.5F,
        };
    }
}
