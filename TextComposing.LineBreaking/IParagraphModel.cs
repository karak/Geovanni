using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextComposing.LineBreaking
{
    /// <summary>
    /// Knuth-Plass に基づく段落モデル
    /// </summary>
    public interface IParagraphModel<TLine, TState>
    {
        /// <summary>
        /// 開始点を得る。
        /// </summary>
        IBreakPoint StartPoint { get; }

        /// <summary>
        /// 終了点を得る。
        /// </summary>
        IBreakPoint EndPoint { get; }

        /// <summary>
        /// （開始点を除き、終了点を含む）妥当な分割点を逐次列挙する。
        /// </summary>
        IEnumerable<IBreakPoint> FeasibleBreakPoints { get; }

        /// <summary>
        /// 指定範囲を含んだ行（字間調整前）を作成する。
        /// </summary>
        /// <param name="constraint">行の制約</param>
        /// <param name="from">行直前の分割点</param>
        /// <param name="to">行直後の分割点</param>
        /// <param name="currentState">現在の計算状態</param>
        /// <param name="newState">処理後の計算状態</param>
        /// <remarks>
        /// <paramref name="constraint"/>はオリジナルのKnuth-Plassにはないが、行内の最適配置の算出に必要（ルビなど）
        /// </remarks>
        IUnjustifiedLine<TLine> CreateLine(ILineConstraint constraint, IBreakPoint from, IBreakPoint to, TState currentState, out TState newState);
    }

    /// <summary>
    /// 字間調整前の一行を表す。
    /// </summary>
    /// <typeparam name="TLine">字間調整によって生成される行</typeparam>
    public interface IUnjustifiedLine<TLine>
    {
        /// <summary>
        /// Knuth-Plass における総行長。
        /// </summary>
        double TotalLength { get; }

        /// <summary>
        /// Knuth-Plass における行内伸張量。
        /// </summary>
        double TotalStretch { get; }

        /// <summary>
        /// Knuth-Plass における行内収縮量。
        /// </summary>
        double TotalShrink { get; }

        /// <summary>
        /// Knuth-Plass における行分割罰則値。
        /// </summary>
        double PenaltyValue { get; }

        /// <summary>
        /// 字間調整を行う。
        /// </summary>
        /// <param name="adjustmentRatio">Knuth-Plass における調整比率</param>
        /// <returns>字間調整結果</returns>
        TLine Justify(double adjustmentRatio);
    }


    /// <summary>
    /// 分割点を表す。
    /// </summary>
    public interface IBreakPoint
    {
        /// <summary>
        /// Knuth-Plass におけるフラグ（行末にフラグが連続するとNG。ハイフネーション用）
        /// </summary>
        bool IsFlagged { get; }

        /// <summary>
        /// Knuth-Plass における罰則値。
        /// </summary>
        int Penalty { get; }
    }
}
