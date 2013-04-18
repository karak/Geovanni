using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TextComposing.Formatting
{
    internal class StartOfParagraph : LineBreaking.IBreakPoint
    {
        private readonly static GlueProperty FixedZero = default(GlueProperty);

        /// <summary>
        /// 何もしない
        /// </summary>
        public readonly GlueProperty Glue = FixedZero;

        public override string ToString()
        {
            return "start-of-paragraph";
        }

        bool LineBreaking.IBreakPoint.IsFlagged
        {
            get { return false; }
        }

        int LineBreaking.IBreakPoint.Penalty
        {
            get { return LineBreaking.Constants.PenaltyInfinity; }
        }
    }

    internal class EndOfParagraph : LineBreaking.IBreakPoint
    {
        private const float LengthInfinity = float.MaxValue / 1024;
        private readonly static GlueProperty FromZeroToInfinity = new GlueProperty(0F, LengthInfinity, 0F);

        /// <summary>
        /// 無限に伸張する
        /// </summary>
        public readonly GlueProperty Glue = FromZeroToInfinity;

        public override string ToString()
        {
            return "end-of-paragraph";
        }

        bool LineBreaking.IBreakPoint.IsFlagged
        {
            get { return false; }
        }

        int LineBreaking.IBreakPoint.Penalty
        {
            get { return LineBreaking.Constants.PenaltyToForceBreak; }
        }
    }

    internal class ParagraphModel : LineBreaking.IParagraphModel<Printing.IPrintableLine, InlineStyle>
    {
        private static readonly LineBreaking.IBreakPoint _startFlyweight = new StartOfParagraph();
        private static readonly LineBreaking.IBreakPoint _endFlyweight = new EndOfParagraph();

        private IFormatObject[] _objectList;

        public ParagraphModel(IFormatObject[] objectList)
        {
            _objectList = (IFormatObject[])(objectList.Clone());
#if false
            foreach (var o in _objectList)
                Console.Write(o.ToString() + "<>");
            Console.Write("\n");
#endif
        }

        LineBreaking.IBreakPoint LineBreaking.IParagraphModel<Printing.IPrintableLine, InlineStyle>.StartPoint
        {
            get { return _startFlyweight; }
        }

        LineBreaking.IBreakPoint LineBreaking.IParagraphModel<Printing.IPrintableLine, InlineStyle>.EndPoint
        {
            get { return _endFlyweight; }
        }

        IEnumerable<LineBreaking.IBreakPoint> LineBreaking.IParagraphModel<Printing.IPrintableLine, InlineStyle>.FeasibleBreakPoints
        {
            get
            {
                foreach (var obj in _objectList.Skip(1))
                {
                    var point = obj as LineBreaking.IBreakPoint;
                    if (point != null) yield return point;
                }
                yield return _endFlyweight;
            }
        }

        LineBreaking.IUnjustifiedLine<Printing.IPrintableLine> LineBreaking.IParagraphModel<Printing.IPrintableLine, InlineStyle>.CreateLine(LineBreaking.ILineConstraint constraint, LineBreaking.IBreakPoint from, LineBreaking.IBreakPoint to, InlineStyle style, out InlineStyle newStyle)
        {
            var factory = new UnjustifiedLineBuilder(_objectList);
            return factory.CreateLine(constraint, style, from, to, out newStyle);
        }
    }
}

