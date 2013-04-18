using CC = TextComposing.CharacterClasses;

namespace TextComposing.Formatting
{
    /// <summary>
    /// 字送りおよびアキ
    /// </summary>
    internal class AdvancingStrategy
    {
        public float _standardStretchJPByZw = 0.2F;
        public float _standardShrinkJPByZw = 0F;
        private IParagraphIndentStyle _indentStyle;

        public AdvancingStrategy(IParagraphIndentStyle indentStyle)
        {
            _indentStyle = indentStyle;
        }

        public GlueProperty InterletterGlueJP(UChar letterBefore, float zwSizeBefore, UChar letterAfter, float zwSizeAfter)
        {
            //最優先は分離禁止。
            if (CC.Cl08(letterBefore, letterAfter))
            {
                return new GlueProperty(0, 0, 0);
            }
            else
            {
                //それ以外の場合について検査
                float zwSpaceSize;
                float baseSize;
                InterletterSpaceJP(letterBefore, zwSizeBefore, letterAfter, zwSizeAfter, out zwSpaceSize, out baseSize);
                return new GlueProperty(zwSpaceSize, _standardStretchJPByZw, zwSpaceSize + _standardShrinkJPByZw) * baseSize;
            }
        }

        private void InterletterSpaceJP(UChar letterBefore, float zwSizeBefore, UChar letterAfter, float zwSizeAfter, out float zwSpaceSize, out float baseSize)
        {
            var beforeSpaceType = letterBefore.GetSpaceType();
            var afterSpaceType = letterAfter.GetSpaceType();

            switch (beforeSpaceType)
            {
                case SpaceType.Closing:
                    switch (afterSpaceType)
                    {
                        case SpaceType.Closing:
                        case SpaceType.DividingPunctuation:
                        case SpaceType.MiddleDots:
                            zwSpaceSize = 0F;
                            baseSize = zwSizeBefore;
                            break;
                        case SpaceType.Opening:
                        case SpaceType.Normal:
                        default:
                            zwSpaceSize = 0.5F;
                            baseSize = zwSizeBefore;
                            break;
                    }
                    break;
                case SpaceType.DividingPunctuation:
                    switch (afterSpaceType)
                    {
                        case SpaceType.Closing:
                        case SpaceType.DividingPunctuation:
                            zwSpaceSize = 0F;
                            baseSize = zwSizeBefore;
                            break;
                        case SpaceType.MiddleDots:
                            zwSpaceSize = 0.25F;
                            baseSize = zwSizeAfter;
                            break;
                        case SpaceType.Opening:
                            zwSpaceSize = 0.5F;
                            baseSize = zwSizeAfter;
                            break;
                        case SpaceType.Normal:
                        default:
                            zwSpaceSize = 1.0F;
                            baseSize = zwSizeBefore;
                            break;
                    }
                    break;
                case SpaceType.MiddleDots:
                    switch (afterSpaceType)
                    {
                        case SpaceType.Opening:
                            zwSpaceSize = 0.5F;
                            baseSize = zwSizeAfter;
                            break;
                        case SpaceType.Closing:
                        case SpaceType.DividingPunctuation:
                        case SpaceType.MiddleDots:
                            zwSpaceSize = 0F;
                            baseSize = zwSizeBefore;
                            break;
                        case SpaceType.Normal:
                        default:
                            zwSpaceSize = 0.25F;
                            baseSize = zwSizeBefore;
                            break;
                    }
                    break;
                case SpaceType.Opening:
                    switch (afterSpaceType)
                    {
                        case SpaceType.Opening:
                        case SpaceType.Closing:
                        case SpaceType.DividingPunctuation:
                        case SpaceType.MiddleDots:
                        case SpaceType.Normal:
                        default:
                            zwSpaceSize = 0F;
                            baseSize = zwSizeBefore;
                            break;
                    }
                    break;
                case SpaceType.Normal:
                default:
                    switch (afterSpaceType)
                    {
                        case SpaceType.Opening:
                            zwSpaceSize = 0.5F;
                            baseSize = zwSizeAfter;
                            break;
                        case SpaceType.MiddleDots:
                            zwSpaceSize = 0.25F;
                            baseSize = zwSizeAfter;
                            break;
                        case SpaceType.Closing:
                        case SpaceType.DividingPunctuation:
                        case SpaceType.Normal:
                        default:
                            zwSpaceSize = 0F;
                            baseSize = zwSizeBefore;
                            break;
                    }
                    break;
            }
        }

        public GlueProperty LineTailGlueJP(UChar letter, float zwSize)
        {
            if (CC.Cl07(letter) || CC.Cl06(letter) || CC.Cl02(letter))
            {
                var toCancel = LengthJPByZw(letter, zwSize);
                return new GlueProperty(-toCancel, toCancel + 0.5F * zwSize, 0F);
                //TODO: 行末は指定のアキかベタ組かどちらか。またほかのアキより優先的に詰める。データ型と UnjustfiedLine のデータ構造変更。
                //TODO: 前項目の修正はぶら下げも実現できるように
            }
            else if (CC.Cl05(letter))
            {
                var length = 0.25F * zwSize;
                return new GlueProperty(length, 0, length);
            }
            else
            {
                return new GlueProperty();
            }
        }

        //行頭アキはゼロと仮定
        public float LengthJPByZw(UChar letter, float zwSize)
        {
            return new GlyphMetric(letter, zwSize).VerticalSize;
        }

        // TODO: スタイルに移動
        /// <summary>
        /// 段落頭字下げ
        /// </summary>
        public float FirstLineIndent(UChar letter, float zwSize)
        {
            return (_indentStyle.ParagraphIndent + _indentStyle.TextIndent(letter)) * zwSize;
        }

        public float FollowingLineIndent(UChar letter, float zwSize)
        {
            return _indentStyle.ParagraphIndent * zwSize;
        }
    }
}
