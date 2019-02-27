// Compositactic - Made in the USA - Indianapolis, IN  - Copyright (c) 2017 Matt J. Crouch

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction, 
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
// subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies
// or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN 
// NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections;
using System;
using System.Linq;
using System.Reflection;

namespace CT
{
    [Serializable]
    public class CompositeDictionary<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged where TValue : Composite
    {
        internal readonly ConcurrentDictionary<TKey, TValue> dictionary;

        public CompositeDictionary()
        {
            _synchronizationContext = AsyncOperationManager.SynchronizationContext;
            dictionary = new ConcurrentDictionary<TKey, TValue>();
            _removedIds = new ConcurrentBag<object>();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly ConcurrentBag<object> _removedIds;
        public IEnumerable<object> RemovedIds => _removedIds;

        [NonSerialized]
        private readonly SynchronizationContext _synchronizationContext;
        private void RaiseEvents()
        {
            _synchronizationContext.Post(s =>
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ICollection<KeyValuePair<TKey, TValue>>.Count)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IDictionary<TKey, TValue>.Keys)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IDictionary<TKey, TValue>.Values)));
            }, null);
        }

        public void Add(TKey key, TValue value)
        {
            var result = dictionary.TryAdd(key, value);

            if (result)
            {
                value.AddEvent(CompositeEventType.Add, value.GetPath(), value);
                RaiseEvents();
            }
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return dictionary.Keys; }
        }

        public bool Remove(TKey key, bool setCompositeStateDeleted)
        {
            if (!setCompositeStateDeleted)
                return Remove(key);

            var valueToRemove = dictionary[key];

            var compositeModelAttribute = valueToRemove
                                        .GetType()
                                        .GetCustomAttribute<CompositeModelAttribute>();

            if (compositeModelAttribute == null)
                throw new MissingMemberException();

            var valueToRemoveModel = valueToRemove.GetType().GetField(compositeModelAttribute.ModelFieldName).GetValue(valueToRemove);
            var valueToRemoveId = valueToRemove.GetType().GetProperty(valueToRemoveModel.GetType().GetCustomAttribute<KeyPropertyAttribute>().PropertyName).GetValue(valueToRemoveModel);

            if (!_removedIds.Contains(valueToRemoveId))
                _removedIds.Add(valueToRemoveId);

            return Remove(key);
        }

        public void ClearRemovedIds()
        {
            _removedIds.Clear();
        }

        public bool Remove(TKey key)
        {
            var result = dictionary.TryRemove(key, out TValue removedItem);

            if (result)
            {
                removedItem.AddEvent(CompositeEventType.Remove, removedItem.GetPath(), removedItem);
                RaiseEvents();
            }

            return result;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values
        {
            get { return dictionary.Values; }
        }

        public int Count
        {
            get { return dictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).IsReadOnly; }
        }

        public TValue this[TKey key]
        {
            get { return dictionary[key]; }
            set
            {
                dictionary[key] = value;
                value.AddEvent(CompositeEventType.Rename, value.GetPath(), key);
                RaiseEvents();
            }
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> newItem)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Add(newItem);
            newItem.Value.AddEvent(CompositeEventType.Add, newItem.Value.GetPath(), newItem);
            RaiseEvents();
        }

        public void Clear()
        {
            dictionary.Clear();
            RaiseEvents();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            var result = dictionary.TryRemove(item.Key, out TValue removedItem);

            if (result)
            {
                removedItem.AddEvent(CompositeEventType.Remove, removedItem.GetPath(), removedItem);
                RaiseEvents();
            }

            return result;
        }
    }
}