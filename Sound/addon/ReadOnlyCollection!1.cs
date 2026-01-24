namespace SystemX.Addon {
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public sealed class ReadOnlyCollection<T> : ICollection<T>, IEnumerable<T>, ICollection, IEnumerable {
        private IEnumerable<T> backing;

        public ReadOnlyCollection(IEnumerable<T> backing) {
            if (backing == null) {
                throw new ArgumentNullException(nameof(backing));
            }
            this.backing = backing;
        }

        public void Add(T item) {
            ThrowInvalidOperationException();
        }

        public void Clear() {
            ThrowInvalidOperationException();
        }

        public bool Contains(T item) {
            if (this.backing is ICollection<T>) {
                return this.BackingCollection.Contains(item);
            }
            return this.BackingCollection.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex) {
            if (array == null) {
                throw new ArgumentNullException(nameof(array));
            }
            ICollection<T> backing = this.backing as ICollection<T>;
            if (backing != null) {
                backing.CopyTo(array, arrayIndex);
            } else {
                int index = arrayIndex;
                foreach (T local in this.backing) {
                    array[index] = local;
                    index++;
                }
            }
        }

        public IEnumerator<T> GetEnumerator() {
            return this.backing.GetEnumerator();
        }

        public bool Remove(T item) {
            ThrowInvalidOperationException();
            return false;
        }

        void ICollection.CopyTo(Array array, int index) {
            if (array == null) {
                throw new ArgumentNullException(nameof(array));
            }
            int num = index;
            foreach (T local in this.backing) {
                array.SetValue(local, num);
                num++;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this.backing.GetEnumerator();
        }

        private ICollection<T> BackingCollection {
            get {
                ICollection<T> backing = this.backing as ICollection<T>;
                if (backing == null) {
                    backing = new List<T>(this.backing);
                    this.backing = backing;
                }
                return backing;
            }
        }

        public int Count {
            get {
                return this.BackingCollection.Count;
            }
        }

        public bool IsReadOnly {
            get {
                return true;
            }
        }

        bool ICollection.IsSynchronized {
            get {
                return false;
            }
        }

        object ICollection.SyncRoot {
            get {
                return this;
            }
        }

        private static void ThrowInvalidOperationException() {
            throw new InvalidOperationException("This Collection is read-only");
        }
    }
}

