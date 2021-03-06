﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Regex = System.Text.RegularExpressions.Regex;
using CC =TextComposing.CharacterClasses;


namespace TextComposing.Formatting
{
    internal class UnjustifiedLine : LineBreaking.IUnjustifiedLine
    {
        private readonly IFragment[] _fragmentList;
        private readonly GlueProperty _endGlue;
        private readonly float _penaltyValue;

        public UnjustifiedLine(IFragment[] fragmentList, GlueProperty endGlue, float penaltyValue)
        {
            _fragmentList = (IFragment[])fragmentList.Clone();
            _endGlue = endGlue;
            _penaltyValue = penaltyValue;
        }

        double LineBreaking.IUnjustifiedLine.TotalLength
        {
            get { return Sum(x => x.Length) + _endGlue.Length; }
        }

        double LineBreaking.IUnjustifiedLine.TotalStretch
        {
            get { return Sum(x => x.Stretch) + _endGlue.Stretch; }
        }
        
        double LineBreaking.IUnjustifiedLine.TotalShrink
        {
            get { return Sum(x => x.Shrink) + _endGlue.Shrink; }
        }

        double LineBreaking.IUnjustifiedLine.PenaltyValue
        {
            get { return _penaltyValue; }
        }

        private double Sum(Func<IFragment, double> f)
        {
            return (from x in _fragmentList select f(x)).
                Aggregate(0.0, (lhs, rhs) => lhs + rhs);
        }

        LineBreaking.IPrintableLine LineBreaking.IUnjustifiedLine.Justify(double adjustmentRatio)
        {
            var buffer = new List<InlineElement>((int)(_fragmentList.Length * 1.25));
            foreach (var fragment in _fragmentList)
            {
                buffer.Add(fragment.Justify((float)adjustmentRatio));
            }
            return new JustifiedLine(buffer);
        }

        public override string ToString()
        {
            return String.Join("", from x in _fragmentList select x.ToString());
        }

        public static IFragment[] LayoutRubyInLine(IFragment[] fragmentList, float endSpaceLength)
        {
            IFragment[] layouted = new IFragment[fragmentList.Length];
            var n = fragmentList.Length;
            for (int i = 0; i < n; ++i)
            {
                IFragment current = fragmentList[i];
                IAdjacentableToFragment before = (i > 0 ? fragmentList[i - 1] : DenyRubyHanging.theInstance);
                IAdjacentableToFragment after = (i < n - 1 ? fragmentList[i + 1] : DenyRubyHanging.theInstance);
                layouted[i] = current.LayoutRuby(before, after);
            }
            return layouted;
        }

        #region fragments
        public interface IAdjacentableToFragment
        {
            float RubyHangingAcceptanceBefore { get; }
            float RubyHangingAcceptanceAfter { get; }
        }

        internal class DenyRubyHanging : IAdjacentableToFragment
        {
            public static readonly IAdjacentableToFragment theInstance = new DenyRubyHanging();

            float IAdjacentableToFragment.RubyHangingAcceptanceBefore
            {
                get { return 0; }
            }

            float IAdjacentableToFragment.RubyHangingAcceptanceAfter
            {
                get { return 0; }
            }
        }

        public interface IFragment : IAdjacentableToFragment
        {
            InlineElement Justify(float adjustmentRatio);
            IFragment LayoutRuby(IAdjacentableToFragment before, IAdjacentableToFragment after);
            double Length { get; }

            double Stretch { get; }

            double Shrink { get; }
        }

        public sealed class JapaneseLetterFragment : IFragment
        {
            public JapaneseLetterFragment(
                GlueProperty beforeGlue,
                float contentLength,
                string content,
                TextEmphasysDot textEmphasysDot)
            {
                BeforeGlue = beforeGlue;
                ContentLength = contentLength;
                Content = content;
                _textEmphasysDot = textEmphasysDot;
            }

            public readonly GlueProperty BeforeGlue;
            public readonly float ContentLength;
            public readonly string Content;
            private readonly TextEmphasysDot _textEmphasysDot;

            public InlineText Justify(float adjustmentRatio)
            {
                var offset = BeforeGlue.Length + (adjustmentRatio >= 0 ? BeforeGlue.Stretch * adjustmentRatio : BeforeGlue.Shrink * adjustmentRatio);
                InlineText result = new NormalInlineText(offset + ContentLength, offset, Content);
                if (_textEmphasysDot != TextEmphasysDot.None) {
                    result = result.WithEmphasysDots();
                }
                return result;
            }

            InlineElement IFragment.Justify(float adjustmentRatio)
            {
                return this.Justify(adjustmentRatio);
            }

            public double Length
            {
                get { return ContentLength + BeforeGlue.Length; }
            }

            public double Stretch
            {
                get { return BeforeGlue.Stretch; }
            }

            public double Shrink
            {
                get { return BeforeGlue.Shrink; }
            }

            float IAdjacentableToFragment.RubyHangingAcceptanceBefore
            {
                get
                {
                    if (IsKanji(Content) || IsPunctuation(Content))
                    {
                        return BeforeGlue.Length;
                    }
                    else
                    {
                        return ContentLength / 2 + BeforeGlue.Length;
                    }
                }
            }
            float IAdjacentableToFragment.RubyHangingAcceptanceAfter
            {
                get
                {
                    //TODO: アキにルビをかけた際のツメ
                    if (IsKanji(Content) || IsPunctuation(Content))
                    {
                        return 0;
                    }
                    else
                    {
                        return ContentLength / 2;
                    }
                }
            }

            IFragment IFragment.LayoutRuby(IAdjacentableToFragment before, IAdjacentableToFragment after)
            {
                return this;
            }

            public JapaneseLetterFragment Offset(float lengthIncrease)
            {
                return new JapaneseLetterFragment(
                    new GlueProperty(
                        BeforeGlue.Length + lengthIncrease,
                        BeforeGlue.Stretch,
                        BeforeGlue.Shrink
                    ),
                    ContentLength,
                    Content,
                    _textEmphasysDot
                );
            }

            //TODO: JSReq準拠
            private bool IsKanji(string letter)
            {
                return Regex.IsMatch(letter, @"^\p{IsCJKUnifiedIdeographs}$");
            }

            private bool IsPunctuation(string letter)
            {
                return (letter.Length == 1 &&
                    (CC.Cl01(letter[0]) || CC.Cl02(letter[0]) || CC.Cl03(letter[0]) ||
                     CC.Cl04(letter[0]) || CC.Cl05(letter[0]) || CC.Cl06(letter[0]) ||
                     CC.Cl07(letter[0])));
            }

            public override string ToString()
            {
                return Content;
            }
        }

        internal abstract class FragmentString
        {
            public abstract double Length { get; }
            public abstract double FirstOffset { get; }
            public abstract double ContentLength { get; }
            public abstract double Stretch { get; }
            public abstract double Shrink { get; }
            public abstract InlineText[] Justify(float adjustmentRatio);
        }

        internal class BasicFragmentString : FragmentString
        {
            private JapaneseLetterFragment[] _fragments;

            public BasicFragmentString(JapaneseLetterFragment[] source)
            {
                _fragments = (JapaneseLetterFragment[])(source.Clone());
            }
            
            public override string ToString()
            {
                return String.Join("", from x in _fragments select x.ToString());
            }

            public override double FirstOffset
            {
                get
                {
                    return _fragments[0].BeforeGlue.Length;
                }
            }

            public override double Length
            {
                get
                {
                    return _fragments.Aggregate(0.0, (lhs, rhs) => lhs + rhs.Length);
                }
            }

            public override double ContentLength
            {
                get
                {
                    return _fragments.Aggregate(0.0, (lhs, rhs) => lhs + rhs.ContentLength);
                }
            }

            public override double Stretch
            {
                get
                {
                    return _fragments.Aggregate(0.0, (lhs, rhs) => lhs + rhs.BeforeGlue.Stretch);
                }
            }

            public override double Shrink
            {
                get
                {
                    return _fragments.Aggregate(0.0, (lhs, rhs) => lhs + rhs.BeforeGlue.Shrink);
                }
            }

            public FragmentString InsertSpace(float lengthIncrease)
            {
                var border = lengthIncrease / (2 * this.Count);
                //1:2:1が体裁がよい。
                //TODO: 先頭と末尾は親文字基準で二分以上あけない（現在のインターフェイスでは情報不足）
                //TODO: 肩ルビ
                return new SpaceInsertedFragmentString(this, border, border * 2, border);
            }

            public FragmentString InsertSpaceInside(float lengthIncrease)
            {
                if (this.Count > 1)
                {
                    var middle = lengthIncrease / (this.Count - 1);
                    return new SpaceInsertedFragmentString(this, 0, middle, 0);
                }
                else
                {
                    var border = lengthIncrease / 2;
                    return new SpaceInsertedFragmentString(this, border, 0, border);
                }
            }

            public BasicFragmentString Offset(float lengthIncrease)
            {
                if (_fragments.Length == 0) throw new InvalidOperationException();

                var newFragments = new JapaneseLetterFragment[_fragments.Length];
                newFragments[0] = _fragments[0].Offset(lengthIncrease);

                for (int i = 1; i < newFragments.Length; ++i)
                    newFragments[i] = _fragments[i];
                return new BasicFragmentString(newFragments);
            }

            public int Count
            {
                get { return _fragments.Length; }
            }

            public override InlineText[] Justify(float adjustmentRatio)
            {
                return Array.ConvertAll(_fragments, x => x.Justify(adjustmentRatio));
            }

            public T[] ConvertAll<T>(System.Converter<JapaneseLetterFragment, T> f)
            {
                return Array.ConvertAll(_fragments, f);
            }

            public delegate T ConverterWithIndex<T>(JapaneseLetterFragment x, int i);

            public T[] ConvertAllWithIndex<T>(ConverterWithIndex<T> f)
            {
                T[] result = new T[_fragments.Length];
                for (int i = 0; i < _fragments.Length; ++i)
                {
                    result[i] = f(_fragments[i], i);
                }
                return result;
            }
        }

        public abstract class RubyFragmentBase : IFragment
        {
            private const double _rubySizeRatio = 0.5;
            protected FragmentString _baseFragments;
            protected FragmentString _rubyFragments;

            internal RubyFragmentBase(FragmentString baseFragments, FragmentString rubyFragments)
            {
                _baseFragments = baseFragments;
                _rubyFragments = rubyFragments;
            }

            InlineElement IFragment.Justify(float adjustmentRatio)
            {
                var adjustedLength = Length + (adjustmentRatio >= 0? Stretch * adjustmentRatio : Shrink * adjustmentRatio);
                var baseJustified = _baseFragments.Justify(adjustmentRatio);
                var rubyJustified = _rubyFragments.Justify(0); //TODO: ルビ側の調整
                return new InlineTextWithRuby(adjustedLength, baseJustified, rubyJustified);
            }

            public double Length
            {
                get { return _baseFragments.Length; }
            }

            public double Stretch
            {
                get { return Math.Min(_baseFragments.Stretch, _rubyFragments.Stretch * _rubySizeRatio); }
            }
            
            public double Shrink
            {
                get { return Math.Min(_baseFragments.Shrink, _rubyFragments.Shrink * _rubySizeRatio); }
            }

            float IAdjacentableToFragment.RubyHangingAcceptanceBefore
            {
                get { return 0; }
            }

            float IAdjacentableToFragment.RubyHangingAcceptanceAfter
            {
                get { return 0; }
            }

            public override string ToString()
            {
                return String.Format("{0}（{1}）",_baseFragments, _rubyFragments);
            }

            public abstract IFragment LayoutRuby(IAdjacentableToFragment before, IAdjacentableToFragment after);
        }

        internal class SpaceInsertedFragmentString : FragmentString
        {
            private BasicFragmentString _inserted;
            private float _appendingSpaceLength;

            public SpaceInsertedFragmentString(BasicFragmentString baseString,
                float first, float middle, float last)
            {
                _inserted = new BasicFragmentString(baseString.ConvertAllWithIndex(
                    (x, i) => PrependSpace(x, i == 0 ? first : middle)));
                _appendingSpaceLength = last;
            }

            private static JapaneseLetterFragment PrependSpace(JapaneseLetterFragment source, float length)
            {
                return source.Offset(length);
            }

            public override double FirstOffset
            {
                get { return _inserted.FirstOffset; }
            }

            public override double Length
            {
                get { return _inserted.Length + _appendingSpaceLength; }
            }

            public override double ContentLength
            {
                get { return _inserted.ContentLength; }
            }

            public override double Stretch
            {
                get { return _inserted.Stretch; }
            }

            public override double Shrink
            {
                get { return _inserted.Shrink; }
            }

            public override InlineText[] Justify(float adjustmentRatio)
            {
                return _inserted.Justify(adjustmentRatio);
                //ATTENTION: appending space length would be lost
            }

            public override string ToString()
            {
                return _inserted.ToString();
            }
        }

        public sealed class LayoutedRubyFragment : RubyFragmentBase
        {
            internal LayoutedRubyFragment(FragmentString baseFragments, FragmentString rubyFragments)
                :base(baseFragments, rubyFragments)
            {
            }

            public override IFragment LayoutRuby(IAdjacentableToFragment before, IAdjacentableToFragment after)
            {
                throw new InvalidOperationException("This has already been layouted!");
            }
        }

        public sealed class RubyFragment : RubyFragmentBase
        {
            internal RubyFragment(JapaneseLetterFragment[] baseFragments, JapaneseLetterFragment[] rubyFragments)
               :base(new BasicFragmentString(baseFragments), new BasicFragmentString(rubyFragments).Offset(0))
            {
            }

            public override IFragment LayoutRuby(IAdjacentableToFragment before, IAdjacentableToFragment after)
            {
                var baseBasic = (BasicFragmentString)_baseFragments;
                var rubyBasic = (BasicFragmentString)_rubyFragments;
                float beforeAcceptance = before.RubyHangingAcceptanceAfter;
                float afterAcceptance = after.RubyHangingAcceptanceBefore;

                //NOTE: 中。肩付きは未対応
                //TODO: 同じ文字に前後両方からかかった場合のアキ（処理のレベルを上げる必要！）
                float overLength = (float)(
                    (rubyBasic.Length - rubyBasic.FirstOffset) -
                    (baseBasic.Length - baseBasic.FirstOffset));
                float alignment = (float)(baseBasic.FirstOffset - rubyBasic.FirstOffset);
                if (overLength > 0)
                {
                    var halfOverLength = overLength / 2;
                    if (beforeAcceptance >= halfOverLength && afterAcceptance >= halfOverLength)
                    {
                        //前後ともに掛け
                        return new LayoutedRubyFragment(baseBasic, rubyBasic.Offset(alignment - halfOverLength));
                    }
                    else if (afterAcceptance >= halfOverLength)
                    {
                        //前にアキ、後ろは掛け
                        var layoutedBase = baseBasic.Offset(halfOverLength);
                        return new LayoutedRubyFragment(layoutedBase, rubyBasic.Offset(alignment));
                    }
                    else
                    {
                        //TODO: 前のみ掛け。ただし後ろのみ掛けより優先順位は下。実装は構造変更が必要（最後の letter の length 伸ばす）
                        //それ以外は内部にアキ。
                        //今のところ掛けかつアキはしていない。
                        var layoutedBase = baseBasic.InsertSpaceInside(overLength);
                        return new LayoutedRubyFragment(layoutedBase, rubyBasic.Offset(alignment));
                    }
                }
                else
                {
                    var alignedRuby = rubyBasic.Offset(alignment);
                    return new LayoutedRubyFragment(baseBasic, alignedRuby.InsertSpace(-overLength));
                }
            }
        }
        #endregion
    }

    internal class UnjustifiedLineBuilder
    {

        #region
        private class Context : IFormatObjectVisitor
        {
            private List<UnjustifiedLine.IFragment> _buffer;
            private GlueProperty _accumulatedGlue;
            private InlineStyle _currentStyle;

            public Context(InlineStyle style, int capacity)
            {
                _buffer = new List<UnjustifiedLine.IFragment>(capacity);
                _accumulatedGlue = default(GlueProperty);
                _currentStyle = style;
            }

            public void PutLetter(JapaneseLetter @object)
            {
                var newFragment = new UnjustifiedLine.JapaneseLetterFragment(
                    _accumulatedGlue,
                    @object.Length,
                    @object.Letter,
                    _currentStyle.TextEmphasysDot);
                _buffer.Add(newFragment);
                _accumulatedGlue = default(GlueProperty);
            }

            public void PutGroupRuby(GroupRuby @object)
            {
                var baseFragments = GenerateFragments(_currentStyle, @object.BaseObjects, _accumulatedGlue);
                _accumulatedGlue = default(GlueProperty);
                var rubyFragments = GenerateFragments(_currentStyle, @object.RubyObjects, default(GlueProperty));
                _buffer.Add(new UnjustifiedLine.RubyFragment(baseFragments, rubyFragments));
            }

            private static UnjustifiedLine.JapaneseLetterFragment[] GenerateFragments(InlineStyle style, IFormatObject[] objects, GlueProperty startGlue)
            {
                Context baseContext = new Context(style, objects.Length);
                baseContext.PutGlue(startGlue);
                foreach (var x in objects)
                {
                    baseContext.Put(x);
                }
                try
                {
                    return Array.ConvertAll(baseContext.Buffer, x => (UnjustifiedLine.JapaneseLetterFragment)x);
                }
                catch (Exception ex)
                {
                    throw new NotImplementedException("texts both with formatting and with ruby is not supported", ex);
                }
            }

            public UnjustifiedLine.IFragment[] Buffer
            {
                get { return _buffer.ToArray(); }
            }

            public GlueProperty AccumulatedGlue
            {
                get { return _accumulatedGlue; }
            }

            public void PutGlue(GlueProperty glue)
            {
                _accumulatedGlue += glue;
            }

            public void Put(IFormatObject @object)
            {
                @object.Accept(this);
            }

            public void PutTextEmphasysDotChange(TextEmphasysDotChange dot)
            {
                _currentStyle.TextEmphasysDot = dot.Value;
            }

            void IFormatObjectVisitor.Visit(ParagraphHeadIndent @object)
            {
                PutGlue(new GlueProperty(@object.Indent, 0, 0));
            }

            void IFormatObjectVisitor.Visit(JapaneseLetter @object)
            {
                PutLetter(@object);
            }

            void IFormatObjectVisitor.Visit(JapaneseInterletterspace @object)
            {
                PutGlue(@object.Glue);
            }

            void IFormatObjectVisitor.Visit(GroupRuby @object)
            {
                PutGroupRuby(@object);
            }

            void IFormatObjectVisitor.Visit(TextEmphasysDotChange @object)
            {
                PutTextEmphasysDotChange(@object);
            }
        }
        #endregion

        public UnjustifiedLine Build(
            GlueProperty startGlue,
            IEnumerable<IFormatObject> objectList,
            GlueProperty endGlue,
            float penaltyValue,
            InlineStyle style,
            out InlineStyle newStyle)
        {
            var styleClone = style.Clone();
            Context context = new Context(styleClone, 64);
            context.PutGlue(startGlue);
            foreach (var obj in objectList)
            {
                context.Put(obj);
            }
            context.PutGlue(endGlue);
            var buffer = context.Buffer.ToArray();
            //TODO: この処理が同じオブジェクト列に対して繰り返し呼ばれる。もっと前段階に運搬
            var rubyLayoutedBuffer = UnjustifiedLine.LayoutRubyInLine(buffer, endGlue.Length);
            newStyle = styleClone;
            return new UnjustifiedLine(rubyLayoutedBuffer, context.AccumulatedGlue, penaltyValue);
        }
    }

    internal class LineFactory
    {
        private UnjustifiedLineBuilder _lineBuilder = new UnjustifiedLineBuilder();
        private IFormatObject[] _objectList;

        public LineFactory(IFormatObject[] objectList)
        {
            _objectList = objectList;
        }


        public LineBreaking.IUnjustifiedLine CreateLine(InlineStyle style, LineBreaking.IBreakPoint from, LineBreaking.IBreakPoint to, out InlineStyle newStyle)
        {
            JapaneseInterletterspace fromAsJapaneseInterletterspace;
            JapaneseInterletterspace toAsJapaneseInterletterspace;
            StartOfParagraph fromAsStart;
            EndOfParagraph toAsEnd;

            fromAsJapaneseInterletterspace = from as JapaneseInterletterspace;
            toAsJapaneseInterletterspace = to as JapaneseInterletterspace;
            if (fromAsJapaneseInterletterspace != null)
            {
                if (toAsJapaneseInterletterspace != null)
                {
                    return CreateLine(fromAsJapaneseInterletterspace, toAsJapaneseInterletterspace, style, out newStyle);
                }
                else
                {
                    toAsEnd = to as EndOfParagraph;
                    if (toAsEnd != null)
                    {
                        return CreateLine(fromAsJapaneseInterletterspace, toAsEnd, style, out newStyle);
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
                        return CreateLine(fromAsStart, toAsJapaneseInterletterspace, style, out newStyle);
                    }
                    else
                    {
                        toAsEnd = to as EndOfParagraph;
                        if (toAsEnd != null)
                        {
                            return CreateLine(fromAsStart, toAsEnd, style, out newStyle);
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

        private LineBreaking.IUnjustifiedLine CreateLine(StartOfParagraph from, EndOfParagraph to, InlineStyle style, out InlineStyle newStyle)
        {
            return _lineBuilder.Build(from.Glue, _objectList, to.Glue, PenaltyValue(to), style, out newStyle);
        }

        private static int PenaltyValue(LineBreaking.IBreakPoint to)
        {
            return to.Penalty;
        }

        private LineBreaking.IUnjustifiedLine CreateLine(StartOfParagraph from, JapaneseInterletterspace to, InlineStyle style, out InlineStyle newStyle)
        {
            var toIndex = Array.IndexOf(_objectList, to);
            if (toIndex == -1) throw new ArgumentException("to");
            return _lineBuilder.Build(from.Glue, _objectList.Take(toIndex), to.GlueBeforeBreak, PenaltyValue(to), style, out newStyle);
        }

        private LineBreaking.IUnjustifiedLine CreateLine(JapaneseInterletterspace from, JapaneseInterletterspace to, InlineStyle style, out InlineStyle newStyle)
        {
            var fromIndex = Array.IndexOf(_objectList, from);
            if (fromIndex == -1) throw new ArgumentException("from");
            var toIndex = Array.IndexOf(_objectList, to);
            if (toIndex == -1) throw new ArgumentException("to");
            return _lineBuilder.Build(
                new GlueProperty(from.IndentAfterBreak, 0, 0),
                _objectList.Skip(fromIndex + 1).Take(toIndex - fromIndex - 1), to.GlueBeforeBreak, PenaltyValue(to), style, out newStyle);
        }

        private LineBreaking.IUnjustifiedLine CreateLine(JapaneseInterletterspace from, EndOfParagraph to, InlineStyle style, out InlineStyle newStyle)
        {
            var fromIndex = Array.IndexOf(_objectList, from);
            if (fromIndex == -1) throw new ArgumentException("from");
            return _lineBuilder.Build(
                new GlueProperty(from.IndentAfterBreak, 0, 0),
                _objectList.Skip(fromIndex + 1), to.Glue, PenaltyValue(to), style, out newStyle);
        }
    }

}
