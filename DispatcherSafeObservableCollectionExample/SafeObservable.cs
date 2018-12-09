using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Windows.Threading;

namespace DispatcherSafeObservableCollectionExample
{
    public class SafeObservable<T> : IList<T>, INotifyCollectionChanged
    {
        private readonly IList<T> collection = new List<T>();
        private readonly Dispatcher dispatcher;
        private readonly ReaderWriterLock sync = new ReaderWriterLock();

        public SafeObservable()
        {
            dispatcher = Dispatcher.CurrentDispatcher;
        }

        public void Add(T item)
        {
            if (Thread.CurrentThread == dispatcher.Thread)
                DoAdd(item);
            else
                dispatcher.BeginInvoke((Action) (() => { DoAdd(item); }));
        }

        public void Clear()
        {
            if (Thread.CurrentThread == dispatcher.Thread)
                DoClear();
            else
                dispatcher.BeginInvoke((Action) (DoClear));
        }

        public bool Contains(T item)
        {
            sync.AcquireReaderLock(Timeout.Infinite);
            var result = collection.Contains(item);
            sync.ReleaseReaderLock();
            return result;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            sync.AcquireWriterLock(Timeout.Infinite);
            collection.CopyTo(array, arrayIndex);
            sync.ReleaseWriterLock();
        }

        public int Count
        {
            get
            {
                sync.AcquireReaderLock(Timeout.Infinite);
                var result = collection.Count;
                sync.ReleaseReaderLock();
                return result;
            }
        }

        public bool IsReadOnly => collection.IsReadOnly;

        public bool Remove(T item)
        {
            if (Thread.CurrentThread == dispatcher.Thread) return DoRemove(item);

            var op = dispatcher.BeginInvoke(
                new Func<T, bool>(DoRemove), item);
            if (op.Result == null)
                return false;
            return (bool) op.Result;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return collection.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            sync.AcquireReaderLock(Timeout.Infinite);
            var result = collection.IndexOf(item);
            sync.ReleaseReaderLock();
            return result;
        }

        public void Insert(int index, T item)
        {
            if (Thread.CurrentThread == dispatcher.Thread)
                DoInsert(index, item);
            else
                dispatcher.BeginInvoke((Action) (() => { DoInsert(index, item); }));
        }

        public void RemoveAt(int index)
        {
            if (Thread.CurrentThread == dispatcher.Thread)
                DoRemoveAt(index);
            else
                dispatcher.BeginInvoke((Action) (() => { DoRemoveAt(index); }));
        }

        public T this[int index]
        {
            get
            {
                sync.AcquireReaderLock(Timeout.Infinite);
                var result = collection[index];
                sync.ReleaseReaderLock();
                return result;
            }
            set
            {
                sync.AcquireWriterLock(Timeout.Infinite);
                if (collection.Count == 0 || collection.Count <= index)
                {
                    sync.ReleaseWriterLock();
                    return;
                }

                collection[index] = value;
                sync.ReleaseWriterLock();
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void DoAdd(T item)
        {
            sync.AcquireWriterLock(Timeout.Infinite);
            collection.Add(item);
            CollectionChanged?.Invoke(this,
    new NotifyCollectionChangedEventArgs(
        NotifyCollectionChangedAction.Add, item));
            sync.ReleaseWriterLock();
        }

        private void DoClear()
        {
            sync.AcquireWriterLock(Timeout.Infinite);
            collection.Clear();
            CollectionChanged?.Invoke(this,
    new NotifyCollectionChangedEventArgs(
        NotifyCollectionChangedAction.Reset));
            sync.ReleaseWriterLock();
        }

        private bool DoRemove(T item)
        {
            sync.AcquireWriterLock(Timeout.Infinite);
            var index = collection.IndexOf(item);
            if (index == -1)
            {
                sync.ReleaseWriterLock();
                return false;
            }

            var result = collection.Remove(item);
            if (result)
                CollectionChanged?.Invoke(this, new
                    NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Reset));
            sync.ReleaseWriterLock();
            return result;
        }

        private void DoInsert(int index, T item)
        {
            sync.AcquireWriterLock(Timeout.Infinite);
            collection.Insert(index, item);
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add, item, index));
            sync.ReleaseWriterLock();
        }

        private void DoRemoveAt(int index)
        {
            sync.AcquireWriterLock(Timeout.Infinite);
            if (collection.Count == 0 || collection.Count <= index)
            {
                sync.ReleaseWriterLock();
                return;
            }

            collection.RemoveAt(index);
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Reset));
            sync.ReleaseWriterLock();
        }
    }
}