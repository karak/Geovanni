using System;
using System.Collections.Generic;


namespace TextComposing
{
    internal abstract class LazyList<T> : IEnumerable<T>
    {
        public static LazyList<T> New()
        {
            return new LazyList0();
        }

        public static LazyList<T> New(T car)
        {
            return new LazyList1(car);
        }

        public static LazyList<T> New(T car, System.Func<LazyList<T>> cdr)
        {
            return new LazyList2(car, cdr);
        }
        
        protected abstract IEnumerator<T> GetEnumeratorImpl();

        private sealed class LazyList0 : LazyList<T>
        {
            public LazyList0()
            {
            }

            protected override IEnumerator<T> GetEnumeratorImpl()
            {
                yield break;
            }
        }

        private sealed class LazyList1 : LazyList<T>
        {
            private T _car;

            public LazyList1(T car)
            {
                _car = car;
            }

            protected override IEnumerator<T> GetEnumeratorImpl()
            {
                yield return _car;
            }
        }

        private sealed class LazyList2 : LazyList<T>
        {
            private T _car;
            private System.Func<LazyList<T>> _cdr;

            public LazyList2(T car, System.Func<LazyList<T>> cdr)
            {
                _car = car;
                _cdr = cdr;
            }

            protected override IEnumerator<T> GetEnumeratorImpl()
            {
                yield return _car;
                foreach (var x in _cdr())
                    yield return x;
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumeratorImpl();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }
    }
}
