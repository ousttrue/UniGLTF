using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;


namespace UniGLTF
{
    public struct StringSegment : IEnumerable<Char>
    {
        public string Value;
        public int Offset;
        public int Count;

        public char this[int index]
        {
            get
            {
                if (index >= Count) throw new ArgumentOutOfRangeException();
                return Value[Offset + index];
            }
        }

        public StringSegment(string value) : this(value, 0, value.Length) { }
        public StringSegment(string value, int offset) : this(value, offset, value.Length - offset) { }
        public StringSegment(string value, int offset, int count)
        {
            Value = value;
            Offset = offset;
            Count = count;
        }

        public bool IsMatch(string str)
        {
            if (Count != str.Length) return false;
            return Value.Substring(Offset, Count) == str;
        }

        public override string ToString()
        {
            return Value.Substring(Offset, Count);
        }

        public IEnumerator<char> GetEnumerator()
        {
            return Value.Skip(Count).Take(Count).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public StringSegment Take(int n)
        {
            if (n > Count) throw new ArgumentOutOfRangeException();
            return new StringSegment(Value, Offset, n);
        }

        public StringSegment Skip(int n)
        {
            if (n > Count) throw new ArgumentOutOfRangeException();
            return new StringSegment(Value, Offset + n, Count - n);
        }

        public bool TrySearch(Func<Char, bool> pred, out int pos)
        {
            pos = 0;
            for (; pos < Count; ++pos)
            {
                if (pred(this[pos]))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
