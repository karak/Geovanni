using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextComposing.Formatting
{
    /// <summary>
    /// 書式指定オブジェクト
    /// </summary>
    interface IFormatObject
    {
        void Accept(IFormatObjectVisitor visitor);
    }

    interface IFormatObjectVisitor
    {
        void Visit(ParagraphHeadIndent @object);
        void Visit(JapaneseLetter @object);
        void Visit(JapaneseInterletterspace @object);
        void Visit(JapaneseEndOfLineSpace @object);
        void Visit(LatinWord @object);
        void Visit(LatinInterwordSpace @object);
        void Visit(GroupRuby @object);
        void Visit(TextEmphasysDotChange @object);
    }
    
    /// <summary>
    /// Knuth-Plass における Glue 要素の属性
    /// </summary>
    struct GlueProperty
    {
        public GlueProperty(float length, float stretch, float shrink)
        {
            Length = length;
            Stretch = stretch;
            Shrink = shrink;
        }

        public readonly float Length;
        public readonly float Stretch;
        public readonly float Shrink;

        public static GlueProperty operator +(GlueProperty lhs, GlueProperty rhs)
        {
            return new GlueProperty(lhs.Length + rhs.Length, lhs.Stretch + rhs.Stretch, lhs.Shrink + rhs.Shrink);
        }
        public static GlueProperty operator *(GlueProperty lhs, float rhs)
        {
            return new GlueProperty(lhs.Length * rhs, lhs.Stretch * rhs, lhs.Shrink * rhs);
        }
    }

    /// <summary>
    /// 和文一字
    /// </summary>
    class JapaneseLetter : IFormatObject
    {
        public JapaneseLetter(UChar letter, float length)
        {
            Letter = letter;
            Length = length;
        }

        public UChar Letter { get; private set; }

        public float Length { get; private set; }

        void IFormatObject.Accept(IFormatObjectVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return String.Format("\"{0}\":{1:F3}", Letter, Length);
        }
    }

    /// <summary>
    /// 段落先頭行の字下げ
    /// </summary>
    class ParagraphHeadIndent : IFormatObject
    {
        public readonly float Indent;

        public ParagraphHeadIndent(float indent)
        {
            Indent = indent;
        }

        void IFormatObject.Accept(IFormatObjectVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return String.Format("head-indent:{0:F3}", Indent);
        }
    }

    interface IInterletter : IFormatObject, LineBreaking.IBreakPoint
    {
        /// <summary>
        /// 行分割されなかった場合の字間アキ
        /// </summary>
        GlueProperty Glue { get; }

        /// <summary>
        /// ここで行分割された際の前の行末のアキ
        /// </summary>
        GlueProperty GlueBeforeBreak { get; }

        /// <summary>
        /// ここで行分割された際の次の行の（追加）字下げ
        /// </summary>
        float IndentAfterBreak { get; }
    }

    /// <summary>
    /// 和文アキ
    /// </summary>
    class JapaneseInterletterspace : IInterletter
    {
        public JapaneseInterletterspace(
            GlueProperty glue,
            GlueProperty glueBeforeBreak,
            float indentAfterBreak,
            bool isBreakProhibited)
        {
            Glue = glue;
            GlueBeforeBreak = glueBeforeBreak;
            IndentAfterBreak = indentAfterBreak;
            _penalty = isBreakProhibited ? LineBreaking.Constants.PenaltyInfinity : 0;
        }

        /// <summary>
        /// ペナルティ値。禁則適用箇所なら無限大
        /// </summary>
        private int _penalty;

        /// <summary>
        /// 行分割されなかった場合の字間アキ
        /// </summary>
        public GlueProperty Glue { get; private set; }

        /// <summary>
        /// ここで行分割された際の前の行末のアキ
        /// </summary>
        public GlueProperty GlueBeforeBreak { get; private set; }

        /// <summary>
        /// ここで行分割された際の次の行の字下げ
        /// </summary>
        public float IndentAfterBreak { get; private set; }

        bool LineBreaking.IBreakPoint.IsFlagged
        {
            get { return false; }
        }
        
        int LineBreaking.IBreakPoint.Penalty
        {
            get { return _penalty; }
        }

        void IFormatObject.Accept(IFormatObjectVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return String.Format("japanese-interletter-space:{0}/{1},{2}",
                Glue.Length, GlueBeforeBreak.Length, IndentAfterBreak);
        }
    }
    /// <summary>
    /// 和文行末アキ
    /// </summary>
    /// <remarks>
    /// 約物の後ろアキおよびぶら下げに使用。
    /// </remarks>
    class JapaneseEndOfLineSpace : IFormatObject
    {
        private readonly GlueProperty _finalGlue;

        public JapaneseEndOfLineSpace(GlueProperty finalGlue)
        {
            _finalGlue = finalGlue;
        }

        public GlueProperty FinalGlue
        {
            get { return _finalGlue; }
        }

        void IFormatObject.Accept(IFormatObjectVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 欧文一単語
    /// </summary>
    class LatinWord : IFormatObject
    {
        public LatinWord(UString letters, float length)
        {
            Letters = letters;
            Length = length;
        }

        public UString Letters { get; private set; }

        public float Length { get; private set; }

        void IFormatObject.Accept(IFormatObjectVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return String.Format("\"{0}\":{1:F3}", Letters, Length);
        }
    }

    class LatinInterwordSpace : IInterletter
    {
        public LatinInterwordSpace(GlueProperty glue)
        {
            Glue = glue;
        }

        /// <summary>
        /// 行分割されなかった場合の字間アキ
        /// </summary>
        public GlueProperty Glue { get; private set; }

        GlueProperty IInterletter.GlueBeforeBreak { get { return default(GlueProperty); } }
 
        float IInterletter.IndentAfterBreak { get { return 0; } }

        bool LineBreaking.IBreakPoint.IsFlagged
        {
            get { return false; }
        }

        int LineBreaking.IBreakPoint.Penalty
        {
            get { return 0; }
        }

        void IFormatObject.Accept(IFormatObjectVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return String.Format("interword-space-latin:{0}", Glue.Length);
        }
    }

    //TODO:InterletterSpaceBetweenLatinAndJp

    /// <summary>
    /// グループルビ（和文・欧文区別なし）。複合オブジェクト
    /// </summary>
    class GroupRuby : IFormatObject
    {
        private IFormatObject[] _baseObjects;
        private IFormatObject[] _rubyObjects;

        public GroupRuby(IFormatObject[] baseObjects, IFormatObject[] rubyObjects)
        {
            _baseObjects = (IFormatObject[])(baseObjects.Clone());
            _rubyObjects = (IFormatObject[])(rubyObjects.Clone());
        }

        public IFormatObject[] BaseObjects
        {
            get { return _baseObjects; }
        }

        public IFormatObject[] RubyObjects
        {
            get { return _rubyObjects; }
        }

        void IFormatObject.Accept(IFormatObjectVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return String.Format(
                "{0}({1})",
                String.Join(";", Array.ConvertAll(_baseObjects, x => x.ToString())),
                String.Join(";", Array.ConvertAll(_rubyObjects, x => x.ToString()))
            );
        }
    }

    /// <summary>
    /// 圏点指定の切り替え
    /// </summary>
    class TextEmphasysDotChange : IFormatObject
    {
        public readonly TextEmphasysDot Value;

        public TextEmphasysDotChange(TextEmphasysDot value)
        {
            Value = value;
        }

        void IFormatObject.Accept(IFormatObjectVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}