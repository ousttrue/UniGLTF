using System;
using System.Linq;
using System.Collections.Generic;


namespace UniGLTF
{
    public static partial class ArraySegmentExtensions
    {
        public static IEnumerable<T> ToEnumerable<T>(this ArraySegment<T> self)
        {
            return self.Array.Skip(self.Offset).Take(self.Count);
        }

        public static void Set<T>(this ArraySegment<T> self, int index, T value)
        {
            if (index < 0 || index >= self.Count)
            {
                throw new ArgumentOutOfRangeException();
            }
            self.Array[self.Offset + index] = value;
        }

        public static T Get<T>(this ArraySegment<T> self, int index)
        {
            if (index < 0 || index >= self.Count)
            {
                throw new ArgumentOutOfRangeException();
            }
            return self.Array[self.Offset + index];
        }

        public static ArraySegment<T> Advance<T>(this ArraySegment<T> self, Int32 n)
        {
            return new ArraySegment<T>(self.Array, self.Offset + n, self.Count - n);
        }

        public static ArraySegment<T> Take<T>(this ArraySegment<T> self, Int32 n)
        {
            return new ArraySegment<T>(self.Array, self.Offset, n);
        }

        public static T[] TakeReversedArray<T>(this ArraySegment<T> self, Int32 n)
        {
            var array = new T[n];
            var x = n - 1;
            for (int i = 0; i < n; ++i, --x)
            {
                array[i] = self.Get(x);
            }
            return array;
        }
    }

    public static partial class ArraySegmentExtensions
    {
        #region JSON

        #endregion
    }
}
