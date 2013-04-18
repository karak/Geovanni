using CC = TextComposing.CharacterClasses;

namespace TextComposing.Formatting
{
    /// <summary>
    /// 禁則判定
    /// </summary>
    internal class WordWrapStrategy
    {
        /// <summary>
        /// 指定の字間で行分割できるか
        /// </summary>
        public bool IsProhibited(UChar proceeding, UChar following)
        {
            return 
                DoViolateLineStartProhibitionRule(following) ||
                DoViolateLineEndProhibitionRule(proceeding) ||
                DoViolateUnbreakableCharactersRule(proceeding, following);
        }

        /// <summary>
        /// 分割禁止
        /// </summary>
        private static bool DoViolateUnbreakableCharactersRule(UChar proceeding, UChar following)
        {
            return CC.Cl08(proceeding, following);
        }

        /// <summary>
        /// 行末禁則
        /// </summary>
        private static bool DoViolateLineEndProhibitionRule(UChar proceeding)
        {
            return CC.Cl01(proceeding);
        }

        /// <summary>
        /// 行頭禁則
        /// </summary>
        private static bool DoViolateLineStartProhibitionRule(UChar following)
        {
            return CC.Cl02(following) || CC.Cl04(following) || CC.Cl06(following) || CC.Cl07(following) || CC.Cl09(following);
        }
    }
}


