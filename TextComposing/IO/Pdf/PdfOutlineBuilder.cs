using System;
using System.Collections.Generic;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace TextComposing.IO.Pdf
{
    class PdfOutlineBuilder
    {
        private OutlineNode _currentNode = null;
        private int _initialLevel = 0;

        public void Clear()
        {
            _currentNode = null;
            _initialLevel = 0;
        }

        /// <summary>
        /// 新しいアウトラインを登録する
        /// </summary>
        /// <param name="level">階層構造のレベル（1以上）</param>
        /// <param name="title">見出し文字列</param>
        /// <param name="cb">PDFデータ</param>
        /// <returns>生成されたアウトラインのパス</returns>
        public void AppendOutline(int level, UString title, PdfContentByte cb)
        {
            AppendOutlineNode(level, title);
            var path = Path(_currentNode);
            var destination = new PdfDestination(PdfDestination.INDIRECT);
            var added = cb.LocalDestination(path, destination);
        }

        private void AppendOutlineNode(int level, UString title)
        {
            if (level <= 0)
            {
                throw new ArgumentOutOfRangeException("level");
            }
            if (title == null)
            {
                throw new ArgumentNullException("title");
            }

            //initial
            if (_currentNode == null)
            {
                _initialLevel = level;
                _currentNode = new OutlineNode(UString.Empty);
            }

            //TODO: behave initial level is the maximum(top) level

            //update
            var currentLevel = _currentNode.Level;

            if (level > currentLevel)
            {
                for (int i = currentLevel; i < level - 1; ++i)
                {
                    _currentNode = _currentNode.FirstChild(UString.Empty);
                }
                _currentNode = _currentNode.FirstChild(title);
            }
            else
            {
                for (int i = level; i < currentLevel; ++i)
                {
                    _currentNode = _currentNode.Parent;
                }
                _currentNode = _currentNode.NextSibling(title);
            }
        }

        public void GenerateTo(PdfOutline parentOutline)
        {
            if (_currentNode == null) return;

            var startNode = Root.FirstChild();
            Visit(parentOutline, startNode);
        }

        private OutlineNode Root
        {
            get
            {
                if (_currentNode == null) return null;

                OutlineNode node;
                for (node = _currentNode; !node.IsRoot; node = node.Parent)
                {
                }
                return node;
            }
        }

        private void Visit(PdfOutline parentOutline, OutlineNode node)
        {
            if (node == null) return;
            var counterString = Path(node);
            var thisOutline = new PdfOutline(parentOutline, PdfAction.GotoLocalPage(counterString, false), node.Title.ToString());

            Visit(thisOutline, node.FirstChild());
            Visit(parentOutline, node.NextSibling());
        }

        private static string Path(OutlineNode node)
        {

            var counterString = String.Join(".", node.Path);
            return counterString;
        }
    }

    internal class OutlineNode
    {
        private readonly OutlineNode _parent;
        private readonly int _value;
        private readonly UString _title;

        private OutlineNode _firstChild = null;
        private OutlineNode _nextSibling = null;

        public OutlineNode(UString title)
            : this(1, title)
        {
        }

        public OutlineNode(int initialValue, UString title)
            : this(null, initialValue, title)
        {
        }

        private OutlineNode(OutlineNode parent, int initialValue, UString title)
        {
            _parent = parent;
            _value = initialValue;
            _title = title;
        }

        public OutlineNode Parent
        {
            get { return _parent; }
        }

        public OutlineNode FirstChild()
        {
            return _firstChild;
        }

        public OutlineNode FirstChild(UString title)
        {
            if (_firstChild != null) throw new InvalidOperationException();
            return _firstChild = new OutlineNode(this, 1, title);
        }

        public OutlineNode NextSibling()
        {
            return _nextSibling;
        }

        public OutlineNode NextSibling(UString title)
        {
            if (_nextSibling != null) throw new InvalidOperationException();
            return _nextSibling = new OutlineNode(this._parent, this._value + 1, title);
        }

        public bool IsRoot
        {
            get { return _parent == null; }
        }

        public int Level
        {
            get { return (IsRoot ? 0 : _parent.Level + 1); }
        }

        public int[] Path
        {
            get
            {
                int level = this.Level;
                int[] path = new int[level];
                var node = this;
                int i = level; 
                while (i > 0)
                {
                    path[--i] = node._value;
                    node = node.Parent;
                }
                return path;
            }
        }

        public UString Title
        {
            get { return _title; }
        }

        public override string  ToString()
        {
 	         return String.Format("{0} {1}", String.Join(".", this.Path), this.Title);
        }
    }
}
