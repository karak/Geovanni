using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TextComposing.LineBreaking
{
    /// <summary>
    /// 公開定数
    /// </summary>
    public static class Constants
    {
        public const int PenaltyInfinity = 1000;
        public const int PenaltyToForceBreak = -PenaltyInfinity;
        public const double RatioInfinity = 1000;
    }

    /// <summary>
    /// 段落を囲む枠。今のところ行単位で引くことが前提。
    /// </summary>
    public interface IFrameModel
    {
        /// <summary>
        /// ある行を挟んでいる長さを得る
        /// </summary>
        /// <param name="lineNumber">行番号（1から始まる）</param>
        float LengthOf(int lineNumber);
    }

    public interface ILineConstraint
    {
        float SuitableLength { get; }
    }

    internal class LineConstraint : ILineConstraint
    {
        public float SuitableLength { get; set; }
    }

    /// <summary>
    /// Knuth-Plassアルゴリズムによるソルバ。
    /// </summary>
    /// <typeparam name="TLine">完成した（字間調整後の）行</typeparam>
    /// <typeparam name="TState">計算中の状態</typeparam>
    public class Solver<TLine, TState> where TState : class, new()
    {
        private readonly double _tolerance = 1;
        private Evaluator<TLine> _evaluator = new Evaluator<TLine>();

        public TLine[] Layout(IEnumerable<IParagraphModel<TLine, TState>> paragraphs, IFrameModel frame)
        {
            var buffer = new List<TLine>(1024);
            foreach (var p in paragraphs)
            {
                buffer.AddRange(Layout(p, frame));
            }
            return buffer.ToArray();
        }

        public IEnumerable<TLine> Layout(IParagraphModel<TLine, TState> paragraph, IFrameModel frame)
        {
            var track = ComputeBreakPoints(paragraph, frame);
            if (track.Count == 0)
            {
                return new TLine[] { ForceLayoutInOneLine(paragraph, frame).Justify(1) };
            }
            else
            {
                return from x in track select x.CreateLine();
            }
        }

        private static IUnjustifiedLine<TLine> ForceLayoutInOneLine(IParagraphModel<TLine, TState> paragraph, IFrameModel frame)
        {
            var length = frame.LengthOf(1);
            var constraint = new LineConstraint { SuitableLength = length };
            var style = new TState();
            var ignore = new TState();
            var unjustifiedLine = paragraph.CreateLine(constraint, paragraph.StartPoint, paragraph.EndPoint, style, out ignore);
            return unjustifiedLine;
        }

        private List<ActiveNode> ComputeBreakPoints(IParagraphModel<TLine, TState> paragraph, IFrameModel frame)
        {
            var startNode = ActiveNode.CreateStartNode(paragraph.StartPoint);
            var storedActiveNodeList = new LinkedList<ActiveNode>();
            storedActiveNodeList.AddLast(startNode);
            foreach (var breakPoint in paragraph.FeasibleBreakPoints)
            {
                var breakPointNodes = new HashSet<ActiveNode>();
                var activeNodeNode = storedActiveNodeList.First;
                while (activeNodeNode != null)
                {
                    ActiveNode activeNode = activeNodeNode.Value;
                    bool doDeactivate;
                    ActiveNode nextNode = TryBreakHere(paragraph, frame, breakPoint, activeNode, out doDeactivate);
                    if (nextNode != null)
                    {
                        bool betterThanTheOthers = AddBetterActiveNode(breakPointNodes, nextNode);
#if TRACE_TRY_BREAK
                        Console.WriteLine("#ACTIVATE    | {0}", nextNode);
                        if (betterThanTheOthers)
                        {
                            Console.WriteLine("#...BETTER   |");
                        }
#endif
                    }
                    var next = activeNodeNode.Next;
                    if (doDeactivate)
                    {
                        storedActiveNodeList.Remove(activeNodeNode);
#if TRACE_TRY_BREAK
                        if (nextNode != null)
                        {
                            Console.WriteLine("#ACCEPT  From| {0:12} | To:{1:12}", activeNode, nextNode);
                        }
                        else
                        {
                            Console.WriteLine("#DECLINE From| {0:12}", activeNode);
                        }
#endif
                    }
                    activeNodeNode = next;
                }
                MergeActiveNodes(storedActiveNodeList, breakPointNodes);
            }

            var endedNodes = (from x in storedActiveNodeList where x.Point == paragraph.EndPoint select x);
            if (IsNotEmpty(endedNodes))
            {
                var bestNode = FindTheWorstDemerits(endedNodes);
                storedActiveNodeList.Clear();
                return bestNode.TrackFromStartToHere();
            }
            else
            {
                //失敗した場合は一行におさめる
                var line = ForceLayoutInOneLine(paragraph, frame);
                return new List<ActiveNode>
                {
                    ActiveNode.CreateBreakNode(paragraph.EndPoint, line, FitnessClass.Tight/*dummy*/, null, 0.0, 0.0, startNode)
                };
            }
        }

        private static bool IsNotEmpty(IEnumerable<ActiveNode> endedNodes)
        {
            var e = endedNodes.GetEnumerator();
            return !e.MoveNext();
        }

        private static ActiveNode FindTheWorstDemerits(IEnumerable<ActiveNode> nodes)
        {
            return nodes.Aggregate((lhs, rhs) => (lhs.IsBetterThan(rhs) ? lhs : rhs));
        }

        private ActiveNode TryBreakHere(
            IParagraphModel<TLine, TState> paragraph, IFrameModel frame,
            IBreakPoint breakPoint, ActiveNode prevNode,
            out bool doDeactivate)
        {
            var nextLineNumber = prevNode.LineNumber + 1;
            var suitableLength = frame.LengthOf(nextLineNumber);
            var constraint = new LineConstraint { SuitableLength = suitableLength };
            var newStyle = new TState();
            var line = paragraph.CreateLine(constraint, prevNode.Point, breakPoint, prevNode.Style, out newStyle);
            double ratio = _evaluator.ComputeAdjustmentRatio(line, suitableLength);

            doDeactivate = (ratio < -1 || _evaluator.IsForcedBreakPoint(breakPoint));

            if (-1 <= ratio && ratio <= _tolerance)
            {
                var fitnessClass = _evaluator.ComputeFitnessClass(ratio);
                var prevIsFlagged = prevNode.Point.IsFlagged;
                var prevFitnessClass = prevNode.FitnessClass;
                var demerits = _evaluator.ComputeDemerits(breakPoint, ratio, fitnessClass, prevIsFlagged, prevFitnessClass);
                return ActiveNode.CreateBreakNode(breakPoint, line, fitnessClass, newStyle, ratio, demerits, prevNode);
            }
            else
            {
                return null;
            }
        }

        private static bool AddBetterActiveNode(HashSet<ActiveNode> breakPointNodes, ActiveNode newNode)
        {
            var competitor = breakPointNodes.FirstOrDefault(x => x.FitnessClass == newNode.FitnessClass);
            if (competitor == null)
            {
                breakPointNodes.Add(newNode);
                return true;
            }
            else if (newNode.IsBetterThan(competitor))
            {
                breakPointNodes.Remove(competitor);
                breakPointNodes.Add(newNode);
                return true;
            }
            else
            {
                return false;
            }
        }
        
        private static void MergeActiveNodes(LinkedList<ActiveNode> storedActiveNodeList, HashSet<ActiveNode> breakPointNodes)
        {

            var breakPointBuffer = new List<ActiveNode>(breakPointNodes);
            breakPointBuffer.Sort(ActiveNode.CompareDemerits);
            breakPointBuffer.ForEach(x => storedActiveNodeList.AddLast(x));
            breakPointBuffer.Clear();
        }

        #region private class definition
        private class ActiveNode
        {
            public IBreakPoint Point { get; private set; }
            public FitnessClass FitnessClass { get; private set; }
            public TState Style { get; private set; }
            private double Ratio { get; set; }
            private double TotalDemerits { get; set; }
            public int LineNumber { get; private set; }
            private IUnjustifiedLine<TLine> LineBeforeHere { get; set; }
            private ActiveNode PrevNode { get; set; }

            private bool IsStartNode
            {
                get { return PrevNode == null; }
            }

            public static ActiveNode CreateStartNode(IBreakPoint startPoint)
            {
                return new ActiveNode
                {
                    Point = startPoint,
                    FitnessClass = LineBreaking.FitnessClass.VeryTight,
                    Style = new TState(),
                    Ratio = 0.0,
                    TotalDemerits = 0.0,
                    LineNumber = 0,
                    PrevNode = null,
                    LineBeforeHere = null
                };
            }

            public static ActiveNode CreateBreakNode(IBreakPoint here, IUnjustifiedLine<TLine> lineBeforeHere, FitnessClass fitnessClass, TState style, double ratio, double demerits, ActiveNode prevNode)
            {
                return new ActiveNode
                {
                    Point = here,
                    FitnessClass = fitnessClass,
                    Style = style,
                    Ratio = ratio,
                    TotalDemerits = demerits + prevNode.TotalDemerits,
                    LineNumber = prevNode.LineNumber + 1,
                    PrevNode = prevNode,
                    LineBeforeHere = lineBeforeHere
                };
            }

            public bool IsBetterThan(ActiveNode other)
            {
                return CompareDemerits(this, other) < 0;
            }

            public static int CompareDemerits(ActiveNode lhs, ActiveNode rhs)
            {
                //NOTE: break is always better than start
                if (!lhs.IsStartNode)
                {
                    if (!rhs.IsStartNode)
                    {
                        //break v.s. break
                        double difference = lhs.TotalDemerits - rhs.TotalDemerits;
                        if (difference > 0) return 1;
                        else if (difference < 0) return -1;
                        else return 0;
                    }
                    else
                    {
                        //break v.s. start
                        return -1;
                    }
                }
                else
                {
                    if (!rhs.IsStartNode)
                    {
                        //start v.s. break
                        return +1;
                    }
                    else
                    {
                        //start v.s. start
                        return 0;
                    }
                }
            }

            public TLine CreateLine()
            {
                return LineBeforeHere.Justify(Ratio);
            }

            public List<ActiveNode> TrackFromStartToHere()
            {
                List<ActiveNode> accum = new List<ActiveNode>(64);
                TrackFromStartToHereWithAccum(accum);
                return accum;
            }

            private void TrackFromStartToHereWithAccum(List<ActiveNode> accum)
            {
                if (this.PrevNode == null) return;

                this.PrevNode.TrackFromStartToHereWithAccum(accum);
                accum.Add(this);
            }

            public override string ToString()
            {
                var text = (LineBeforeHere != null ? LineBeforeHere.ToString() : "");
                if (text.Length > 10)
                {
                    text = (text.Substring(0, 3) + "..." + text.Substring(text.Length - 6));
                }
                return String.Format("{0:D3}: {1:F3}; {2}",
                    LineNumber,
                    TotalDemerits,
                    text);
            }
        }
        #endregion
    }

    /// <summary>
    /// 適応度クラス
    /// </summary>
    internal enum FitnessClass
    {
        VeryTight = 0,
        Tight = 1,
        Loose = 2,
        VeryLoose = 3,
        Max = 4
    }

    /// <summary>
    /// Solver から評価関数の計算を委譲される
    /// </summary>
    /// <typeparam name="TJustifiedLine"></typeparam>
    internal class Evaluator<TJustifiedLine>
    {
        private double _lineDemerit = 1;
        private double _flaggedDemerit = 100;
        private double _fitnessDemerit = 100;

        public bool IsForcedBreakPoint(IBreakPoint breakPoint)
        {
            return breakPoint.Penalty == Constants.PenaltyToForceBreak;
        }

        public double ComputeAdjustmentRatio(IUnjustifiedLine<TJustifiedLine> line, double requiredLength)
        {
            double underLength = requiredLength - line.TotalLength;

            if (underLength > 0)
            {
                var stretch = line.TotalStretch;
                if (stretch > 0)
                    return underLength / stretch;
                else
                    return Constants.RatioInfinity;
            }
            else if (underLength < 0)
            {
                var shrink = line.TotalShrink;
                if (shrink > 0)
                    return (double)underLength / (double)shrink;
                else
                    return Constants.RatioInfinity;
            }
            else
            {
                return 0;
            }
        }

        public FitnessClass ComputeFitnessClass(double ratio)
        {
            if (ratio < -.5)
                return FitnessClass.VeryTight;
            else if (ratio <= 0.5)
                return FitnessClass.Tight;
            else if (ratio <= 1)
                return FitnessClass.Loose;
            else
                return FitnessClass.VeryLoose;
        }

        public double ComputeDemerits(
            IBreakPoint point,
            double ratio, 
            FitnessClass fitnessClass, 
            bool prevIsFlagged,
            FitnessClass prevFitnessClass)
        {
            double demerits = ComputeDemerits(point.Penalty, ratio, IsForcedBreakPoint(point));

            //continuous flagged
            if (point.IsFlagged && prevIsFlagged)
                demerits += _flaggedDemerit;

            //unmatched fitness
            //TODO: let fitnessDemerit is 0 when prevNode is head.
            if (Math.Abs(fitnessClass - prevFitnessClass) > 1)
                demerits += _fitnessDemerit;

            return demerits;
        }

        private double ComputeDemerits(int penalty, double ratio, bool forcedBreak)
        {
            double badness = 100 * Cubic(Math.Abs(ratio));
            double demerits;
            if (penalty >= 0)
                //positive penalty or no penalty element
                demerits = Square(_lineDemerit + badness) + penalty;
            else if (!forcedBreak)
                //negative penalty
                demerits = Square(_lineDemerit + badness) - Square(penalty);
            else
                //force break
                demerits = Square(_lineDemerit + badness);
            return demerits;
        }

        private double Square(double x) { return x * x; }
        private double Cubic(double x) { return x * x * x; }
    }
}
