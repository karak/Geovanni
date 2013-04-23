using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Regex = System.Text.RegularExpressions.Regex;


namespace TextComposing.Formatting
{
    internal class UnjustifiedLineBuilder
    {
        private IFormatObject[] _objectList;
        private Heading _heading;

        public UnjustifiedLineBuilder(IFormatObject[] objectList, Heading heading)
        {
            _objectList = objectList;
            _heading = heading;
        }

        public LineBreaking.IUnjustifiedLine<Printing.IPrintableLine> CreateLine(LineBreaking.ILineConstraint constraint, InlineStyle style, LineBreaking.IBreakPoint from, LineBreaking.IBreakPoint to, out InlineStyle newStyle)
        {
            IInterletter fromAsJapaneseInterletterspace;
            IInterletter toAsJapaneseInterletterspace;
            StartOfParagraph fromAsStart;
            EndOfParagraph toAsEnd;

            fromAsJapaneseInterletterspace = from as IInterletter;
            toAsJapaneseInterletterspace = to as IInterletter;
            if (fromAsJapaneseInterletterspace != null)
            {
                if (toAsJapaneseInterletterspace != null)
                {
                    return CreateLine(constraint, fromAsJapaneseInterletterspace, toAsJapaneseInterletterspace, style, out newStyle);
                }
                else
                {
                    toAsEnd = to as EndOfParagraph;
                    if (toAsEnd != null)
                    {
                        return CreateLine(constraint, fromAsJapaneseInterletterspace, toAsEnd, style, out newStyle);
                    }
                    else
                    {
                        throw new ArgumentException("type error", "to");
                    }
                }
            }
            else
            {
                fromAsStart = from as StartOfParagraph;
                if (fromAsStart != null)
                {

                    if (toAsJapaneseInterletterspace != null)
                    {
                        return CreateLine(constraint, fromAsStart, toAsJapaneseInterletterspace, style, out newStyle);
                    }
                    else
                    {
                        toAsEnd = to as EndOfParagraph;
                        if (toAsEnd != null)
                        {
                            return CreateLine(constraint, fromAsStart, toAsEnd, style, out newStyle);
                        }
                        else
                        {
                            throw new ArgumentException("type error", "to");
                        }
                    }
                }
                else
                {
                    throw new ArgumentException("type error", "start");
                }
            }
        }

        private LineBreaking.IUnjustifiedLine<Printing.IPrintableLine> CreateLine(LineBreaking.ILineConstraint constraint, StartOfParagraph from, EndOfParagraph to, InlineStyle style, out InlineStyle newStyle)
        {
            return InlineLayoutEngine.Solve(constraint, from.Glue, _objectList, to.Glue, PenaltyValue(to), _heading, style, out newStyle);
        }

        private static int PenaltyValue(LineBreaking.IBreakPoint to)
        {
            return to.Penalty;
        }

        private LineBreaking.IUnjustifiedLine<Printing.IPrintableLine> CreateLine(LineBreaking.ILineConstraint constraint, StartOfParagraph from, IInterletter to, InlineStyle style, out InlineStyle newStyle)
        {
            var toIndex = Array.IndexOf(_objectList, to);
            if (toIndex == -1) throw new ArgumentException("to");
            return InlineLayoutEngine.Solve(constraint, from.Glue, _objectList.Take(toIndex), to.GlueBeforeBreak, PenaltyValue(to), _heading, style, out newStyle);
        }

        private LineBreaking.IUnjustifiedLine<Printing.IPrintableLine> CreateLine(LineBreaking.ILineConstraint constraint, IInterletter from, IInterletter to, InlineStyle style, out InlineStyle newStyle)
        {
            var fromIndex = Array.IndexOf(_objectList, from);
            if (fromIndex == -1) throw new ArgumentException("from");
            var toIndex = Array.IndexOf(_objectList, to);
            if (toIndex == -1) throw new ArgumentException("to");
            return InlineLayoutEngine.Solve(
                constraint, 
                new GlueProperty(from.IndentAfterBreak, 0, 0),
                _objectList.Skip(fromIndex + 1).Take(toIndex - fromIndex - 1), to.GlueBeforeBreak, PenaltyValue(to), _heading, style, out newStyle);
        }

        private LineBreaking.IUnjustifiedLine<Printing.IPrintableLine> CreateLine(LineBreaking.ILineConstraint constraint, IInterletter from, EndOfParagraph to, InlineStyle style, out InlineStyle newStyle)
        {
            var fromIndex = Array.IndexOf(_objectList, from);
            if (fromIndex == -1) throw new ArgumentException("from");
            return InlineLayoutEngine.Solve(
                constraint, 
                new GlueProperty(from.IndentAfterBreak, 0, 0),
                _objectList.Skip(fromIndex + 1), to.Glue, PenaltyValue(to), _heading, style, out newStyle);
        }
    }

}
