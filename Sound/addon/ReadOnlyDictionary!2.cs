using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SystemX.Addon {

    [DebuggerDisplay("Count = {Count}")]
    [ComVisible(false)]
    [DebuggerTypeProxy(typeof(ReadOnlyDictionaryDebugView<,>))]
    public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable {
        private readonly IDictionary<TKey, TValue> backing;

        public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionaryToWrap) {
            if (dictionaryToWrap == null) {
                throw new ArgumentNullException(nameof(dictionaryToWrap));
            }
            this.backing = dictionaryToWrap;
        }

        public void Add(KeyValuePair<TKey, TValue> item) {
            ThrowNotSupportedException();
        }

        public void Add(TKey key, TValue value) {
            ThrowNotSupportedException();
        }

        public void Clear() {
            ThrowNotSupportedException();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) {
            return this.backing.Contains(item);
        }

        public bool ContainsKey(TKey key) {
            return this.backing.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            this.backing.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            return this.backing.GetEnumerator();
        }

        public bool Remove(TKey key) {
            ThrowNotSupportedException();
            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) {
            ThrowNotSupportedException();
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this.backing.GetEnumerator();
        }

        public bool TryGetValue(TKey key, out TValue value) {
            return this.backing.TryGetValue(key, out value);
        }

        public int Count {
            get { return this.backing.Count; }
        }

        public bool IsReadOnly {
            get {
                return true;
            }
        }

        public TValue this[TKey key] {
            get {
                return this.backing[key];
            }
            set {
                ThrowNotSupportedException();
            }
        }

        public ICollection<TKey> Keys {
            get { return new ReadOnlyCollection<TKey>(this.backing.Keys); }
        }

        public ICollection<TValue> Values {
            get { return new ReadOnlyCollection<TValue>(this.backing.Values); }
        }

        private static void ThrowNotSupportedException() {
            throw new NotSupportedException("This Dictionary is read-only");
        }
    }

    internal sealed class ReadOnlyDictionaryDebugView<TKey, TValue> {
        private IDictionary<TKey, TValue> dict;

        public ReadOnlyDictionaryDebugView(ReadOnlyDictionary<TKey, TValue> dictionary) {
            if (dictionary == null) {
                throw new ArgumentNullException(nameof(dictionary));
            }

            this.dict = dictionary;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<TKey, TValue>[] Items {
            get {
                KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[this.dict.Count];
                this.dict.CopyTo(array, 0);
                return array;
            }
        }
    }
}
