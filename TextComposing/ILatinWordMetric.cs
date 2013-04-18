using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextComposing
{
    public interface ILatinWordMetric
    {
        /// <summary>
        /// 欧文の単語の長さを測る（単位はPoint）。
        /// フォント依存。現在フォントは一種類という前提。
        /// </summary>
        float MeasureText(string latinText);
    }

}
