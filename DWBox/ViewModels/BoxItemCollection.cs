using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Win32.DWrite;

namespace DWBox
{
    public class BoxItemCollection : IEnumerable<BoxItem>
    {
        private readonly Dictionary<string, List<BoxItem>> _itemsDictionary = new Dictionary<string, List<BoxItem>>();
        private readonly ObservableCollection<BoxItem> _items = new ObservableCollection<BoxItem>();

        public void Clear()
        {
            _itemsDictionary.Clear();
            _items.Clear();
        }

        public bool Add(FontSetEntry entry, float emSize)
        {
            BoxItem item = new BoxItem(entry) { EmSize = emSize };

            if (!_itemsDictionary.TryGetValue(entry.FullName, out var items))
                _itemsDictionary[entry.FullName] = items = new List<BoxItem>();

            foreach (BoxItem existingItem in items)
                if (item.FontFace.Equals(existingItem.FontFace))
                    return false;

            items.Add(item);
            _items.Add(item);
            return true;
        }

        public bool Remove(BoxItem item)
        {
            if (_itemsDictionary.TryGetValue(item.Name, out var items))
                items.Remove(item);

            return _items.Remove(item);
        }

        public void Remove(Predicate<BoxItem> predicate)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
                if (predicate(_items[i]))
                {
                    _itemsDictionary.Remove(_items[i].Name);
                    _items.RemoveAt(i);
                }
        }

        public ICollectionView View
        {
            get
            {
                ListCollectionView view = new ListCollectionView(_items);
                view.SortDescriptions.Add(new SortDescription(nameof(BoxItem.Name), ListSortDirection.Ascending));
                view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(BoxItem.TypographicFamilyName)));
                return view;
            }
        }

        public IEnumerator<BoxItem> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
