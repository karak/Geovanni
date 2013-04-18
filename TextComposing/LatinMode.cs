using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CC = TextComposing.CharacterClasses;

namespace TextComposing
{
    internal enum Lang
    {
        Undef,
        Japanese,
        Latin
    }

    /// <summary>
    /// 和欧混植。現在はまだユーティリティ
    /// </summary>
    internal class LatinMode
    {
        private Lang _lang = Lang.Undef;

        private UStringBuilder _latinBufer = new UStringBuilder(32);
        
        public class LangChangeEventArgs : EventArgs
        {
            public LangChangeEventArgs(Lang @new, Lang old)
            {
                New = @new;
                Old = old;
            }

            public readonly Lang New;
            public readonly Lang Old;
        }

        public class FlushEventArgs : EventArgs
        {
            public FlushEventArgs(UString latinText)
            {
                LatinText = latinText;
            }

            public readonly UString LatinText;
        }

        public event EventHandler<FlushEventArgs> Flush;
        public event EventHandler<LangChangeEventArgs> BeforeLangChange;

        private static Lang JudgeLang(UChar letter)
        {
            if (CC.IsLatin(letter) || new UString(":;.,-!?\"' ").Contains(letter)) //TODO: 半角空白はここでやるべきか？
            {
                return Lang.Latin;
            }
            else if (CC.IsCJKIdeograph(letter) || CC.IsHiragana(letter) || CC.IsKatakana(letter))
            {
                return Lang.Japanese;
            }
            else
            {
                return Lang.Undef;
            }
        }

        public void Send(UChar letter)
        {
            var oldLang = _lang;
            var newLang = JudgeLang(letter);

            if (oldLang != newLang && oldLang != Lang.Undef && newLang != Lang.Undef)
            {
                if (BeforeLangChange != null)
                {
                    BeforeLangChange(this, new LangChangeEventArgs(newLang, oldLang));
                }
            }
            
            _lang = newLang;

            if (newLang == Lang.Latin)
            {
                _latinBufer.Append(letter);
            }
            else
            {
                DoFlush();
            }
        }

        public Lang CurrentLang { get { return _lang; } }

        public void ForceFlush()
        {
            DoFlush();
        }

        private void DoFlush()
        {
            if (_latinBufer.Length == 0) return;

            var text = _latinBufer.ToUString();
            _latinBufer.Clear();

            if (Flush != null)
            {
                Flush(this, new FlushEventArgs(text));
            }
        }
    }
}
