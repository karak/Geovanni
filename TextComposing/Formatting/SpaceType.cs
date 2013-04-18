using System;
using CC = TextComposing.CharacterClasses;

namespace TextComposing.Formatting
{
    internal enum SpaceType
    {
        Normal,
        DividingPunctuation, //区切り約物。後ろに全角アキ
        MiddleDots,          //中点。前後に四分アキ
        Opening,             //始まり括弧。前に二分アキ
        Closing              //終わり括弧、句読点類。後ろに二分アキ
    }

    internal static class SpaceTypeExtension
    {
        public static SpaceType GetSpaceType(this UChar letter)
        {
            if (CC.Cl07(letter) || CC.Cl06(letter) || CC.Cl02(letter))
            {
                return SpaceType.Closing;
            }
            else if (CC.Cl01(letter))
            {
                return SpaceType.Opening;
            }
            else if (CC.Cl04(letter))
            {
                return SpaceType.DividingPunctuation;
            }
            else if (CC.Cl05(letter))
            {
                return SpaceType.MiddleDots;
            }
            else
            {
                return SpaceType.Normal;
            }
        }
    }
}
