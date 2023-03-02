using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Win32;

namespace DWBox
{
    //public class FontFaceReferenceCollection : IndexedSetCollection<DirectWrite.IDWriteFontSet, DirectWrite.IDWriteFontFaceReference>
    //{
    //    protected override int GetSetCount(DirectWrite.IDWriteFontSet set) => set.GetFontCount();
    //    protected override DirectWrite.IDWriteFontFaceReference GetItem(DirectWrite.IDWriteFontSet set, int index) => set.GetFontFaceReference(index);
    //    protected override bool FindItem(DirectWrite.IDWriteFontSet set, DirectWrite.IDWriteFontFaceReference item, out int index) => set.FindFontFaceReference(item, out index);

    //    public string GetProperty(int index, DirectWrite.FontPropertyId propertyId, string locale)
    //    {
    //        DirectWrite.IDWriteLocalizedStrings localizesdStrings = Set.GetPropertyValues(Items[index].Index, propertyId, out bool exists);
    //        if (!exists)
    //            return null;


    //    }
    //}

    public abstract class IndexedSetCollection<TSet, TItem> : INotifyCollectionChanged, IReadOnlyList<TItem>
    {
        protected class IndexedReference
        {
            public int Index;
            public TItem Item;
            public TSet Set;

            public IndexedReference(TSet set, TItem item, int index)
            {
                Set = set;
                Item = item;
                Index = index;
            }
        }

        private TSet _lastSet;
        protected TSet Set => _lastSet;

        private readonly List<IndexedReference> _items = new List<IndexedReference>();
        protected IList<IndexedReference> Items => _items;

        protected abstract int GetSetCount(TSet set);
        protected abstract TItem GetItem(TSet set, int index);
        protected abstract bool FindItem(TSet set, TItem item, out int index);

        public void UpdateWith(TSet set)
        {
            int count = GetSetCount(set);
            for (int i = 0; i < count; i++)
            {
                TItem item = GetItem(set, i);
                if (_lastSet == null || !FindItem(_lastSet, item, out int lastIndex))
                {
                    _items.Add(new IndexedReference(set, item, i));
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _items.Count - 1));
                }
                else
                {
                    IndexedReference reference = _items.Find(ixref => ReferenceEquals(ixref.Set, set) && ixref.Index == lastIndex); // PERF: replace with dictionary
                    reference.Set = set;
                    reference.Index = i;
                }
            }

            for (int i = _items.Count - 1; i >= 0; i--)
                if (!ReferenceEquals(_items[i], set))
                {
                    IndexedReference reference = _items[i];
                    _items.RemoveAt(i);
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, reference, i));
                }

            _lastSet = set;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public TItem this[int index] => _items[index].Item;
        public int Count => _items.Count;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<TItem> GetEnumerator()
        {
            foreach (IndexedReference ixref in _items)
                yield return ixref.Item;
        }
    }

}
