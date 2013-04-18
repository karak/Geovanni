using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StringInfo = System.Globalization.StringInfo;

namespace TextComposing
{
    public class UStringBuilder
    {
        private StringBuilder _builder;

        public UStringBuilder()
        {
            _builder = new StringBuilder();
        }

        public int Length
        {
            get { return _builder.Length; }
        }

        public UStringBuilder(int capacity)
        {
            _builder = new StringBuilder(capacity);
        }

        public void Append(UChar ch)
        {
            _builder.Append(ch.ToString());
        }

        public void Append(UString str)
        {
            _builder.Append(str.String);
        }

        public void Clear()
        {
            _builder.Clear();
        }

        public UString ToUString()
        {
            return new UString(_builder.ToString());
        }

        public override string ToString()
        {
            return _builder.ToString();
        }
    }

    public class UString : IEnumerable<UChar>
    {
        private StringInfo _info;

        public UString(string source)
        {
            _info = new StringInfo(source);
        }

        public UString(IEnumerable<UChar> source)
        {
            var builder = new StringBuilder(128);
            foreach (var ch in source)
                builder.Append(ch.ToString());
            _info = new StringInfo(builder.ToString());
        }

        public int Length
        {
            get { return _info.LengthInTextElements; }
        }

        public string String
        {
            get { return _info.String; }
        }

        public override string ToString()
        {
            return _info.String;
        }

        public UChar[] ToArray()
        {
            return Enumerable.ToArray(this);
        }

        public IEnumerator<UChar> GetEnumerator()
        {
            var n = _info.LengthInTextElements;
            for (int i = 0; i < n; ++i)
                yield return new UChar(_info.SubstringByTextElements(i, 1));
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public override bool Equals(object value)
        {
            var casted = value as UString;
            if (Object.ReferenceEquals(casted, null)) return false;
            else return this.Equals(casted);
        }

        public bool Equals(UString value)
        {
            return _info.Equals(value);
        }

        public override int GetHashCode()
        {
            return _info.GetHashCode();
        }
    }

    /// <summary>
    /// Unicode上の一文字（サロゲートペア含む）。
    /// 内部表現は UTF32-LE
    /// </summary>
    public struct UChar
    {
        private int _codePoint;

        public UChar(string character)
        {
            if (char.IsHighSurrogate(character[0]))
            {
                if (character.Length != 2) throw new ArgumentException();
                if (!char.IsLowSurrogate(character[1])) throw new ArgumentException();
            }
            else
            {
                if (character.Length != 1) throw new ArgumentException();
            }
            _codePoint = char.ConvertToUtf32(character, 0);
        }

        public UChar(char character)
        {
            _codePoint = char.ConvertToUtf32(character.ToString(), 0);
        }

        internal UChar(int codePoint)
        {
            _codePoint = codePoint;
        }

        public static UChar FromCodePoint(int code)
        {
            return new UChar(code);
        }

        public UString ToUString()
        {
            return new UString(this.ToString());
        }
        
        public int CodePoint
        {
            get { return _codePoint; }
        }

        public override bool Equals(object value)
        {
            var casted = value as UChar?;
            if (!casted.HasValue) return false;
            else return this == casted.Value;
        }

        public bool Equals(UChar value)
        {
            return this == value;
        }
        
        public static bool operator ==(UChar lhs, UChar rhs)
        {
            return lhs._codePoint == rhs._codePoint;
        }
        public static bool operator !=(UChar lhs, UChar rhs)
        {
            return lhs._codePoint != rhs._codePoint;
        }
        public static bool operator <(UChar lhs, UChar rhs)
        {
            return lhs._codePoint < rhs._codePoint;
        }
        public static bool operator >(UChar lhs, UChar rhs)
        {
            return lhs._codePoint > rhs._codePoint;
        }
        public static bool operator <=(UChar lhs, UChar rhs)
        {
            return lhs._codePoint <= rhs._codePoint;
        }
        public static bool operator >=(UChar lhs, UChar rhs)
        {
            return lhs._codePoint >= rhs._codePoint;
        }

        public override int GetHashCode()
        {
            return _codePoint;
        }

        public override string ToString()
        {
            return char.ConvertFromUtf32(_codePoint);
        }
    }
}
