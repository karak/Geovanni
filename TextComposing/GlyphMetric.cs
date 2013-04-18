using System;
using CC = TextComposing.CharacterClasses;

namespace TextComposing
{
    /// <summary>
    /// 日本語のグリフの測長
    /// </summary>
    public class GlyphMetric
    {
        private UChar _letter;
        private float _zwSize;

        public GlyphMetric(UChar letter, float zwSize)
        {
            _letter = letter;
            _zwSize = zwSize;
        }

        /// <summary>
        /// 字幅
        /// </summary>
        public float VerticalSize
        {
            get
            {
                if (CC.Cl01(_letter) || CC.Cl02(_letter) ||
                    CC.Cl05(_letter) || CC.Cl06(_letter) || CC.Cl07(_letter))
                {
                    return _zwSize / 2;
                }
                else if (CC.Cl03(_letter))
                {
                    return _zwSize / 4;
                }
                else
                {
                    return _zwSize;
                }
            }
        }

        /// <summary>
        /// 等幅の縦書きフォントにおいて、グリフ内の描画開始位置がどれだけ下方向にずれているか
        /// </summary>
        public float VerticalOffset
        {
            get
            {
                if (CC.Cl01(_letter))
                {
                    return _zwSize;
                }
                else if (CC.Cl05(_letter))
                {
                    return _zwSize / 2;
                }
                else
                {
                    return 0F;
                }
            }
        }
    }
}
