using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextComposing.Formatting
{
    internal class UnjustifiedLine : LineBreaking.IUnjustifiedLine<Printing.IPrintableLine>
    {
        private readonly InlineLayoutEngine.IFragment[] _fragmentList;
        private readonly float _penaltyValue;

        public UnjustifiedLine(InlineLayoutEngine.IFragment[] fragmentList, float penaltyValue)
        {
            _fragmentList = (InlineLayoutEngine.IFragment[])fragmentList.Clone();
            _penaltyValue = penaltyValue;
        }

        double LineBreaking.IUnjustifiedLine<Printing.IPrintableLine>.TotalLength
        {
            get { return Sum(x => x.Length); }
        }

        double LineBreaking.IUnjustifiedLine<Printing.IPrintableLine>.TotalStretch
        {
            get { return Sum(x => x.Stretch); }
        }

        double LineBreaking.IUnjustifiedLine<Printing.IPrintableLine>.TotalShrink
        {
            get { return Sum(x => x.Shrink); }
        }

        double LineBreaking.IUnjustifiedLine<Printing.IPrintableLine>.PenaltyValue
        {
            get { return _penaltyValue; }
        }

        private double Sum(Func<InlineLayoutEngine.IFragment, double> f)
        {
            return (from x in _fragmentList select f(x)).
                Aggregate(0.0, (lhs, rhs) => lhs + rhs);
        }

        Printing.IPrintableLine LineBreaking.IUnjustifiedLine<Printing.IPrintableLine>.Justify(double adjustmentRatio)
        {
            //TODO: ルビ配置もここでやり直す！
            var buffer = new List<InlineElement>((int)(_fragmentList.Length * 1.25));
            foreach (var fragment in _fragmentList)
            {
                buffer.Add(fragment.Justify((float)adjustmentRatio));
            }
            var result = new JustifiedLine(buffer);
            return result;
        }

        public override string ToString()
        {
            return String.Join("", from x in _fragmentList select x.ToString());
        }
    }
}
