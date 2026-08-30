using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace DWBox
{
    public class AppViewModel : INotifyPropertyChanged
    {
        private readonly BoxItemCollection _items;
        public BoxItemCollection Items => _items;

        public AppViewModel()
        {
            _items = new BoxItemCollection();
            _items.CollectionChanged += OnItemsChanged;
        }

        private void OnItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            HasItems = _items.Count > 0;
        }

        private bool _hasItems = false;
        public bool HasItems
        {
            get { return _hasItems; }
            set
            {
                if (_hasItems != value)
                {
                    _hasItems = value;
                    OnPropertyChanged(nameof(HasItems), nameof(NoItemsVisibility));
                }
            }
        }

        public Visibility NoItemsVisibility => HasItems ? Visibility.Collapsed : Visibility.Visible;

        private bool _isRasterMode = false;
        public bool IsRasterMode
        {
            get { return _isRasterMode; }
            set
            {
                if (_isRasterMode != value)
                {
                    _isRasterMode = value;
                    OnPropertyChanged(nameof(IsRasterMode), nameof(IsVectorMode), nameof(ModeString));
                }
            }
        }
        public bool IsVectorMode
        {
            get { return !_isRasterMode; }
        }
        public string ModeString => _isRasterMode ? "Raster" : "Vector";

        private float _addEmSize;
        public float AddEmSize
        {
            get { return _addEmSize; }
            set
            {
                if (_addEmSize != value)
                {
                    _addEmSize = value;
                    OnPropertyChanged(nameof(AddEmSize));
                }
            }
        }

        private Brush _glyphFill = Brushes.Black;
        private Brush _glyphOutline = null;

        public Brush GlyphFill => _glyphFill;
        public Brush GlyphOutline => _glyphOutline;

        public void SetGlyphBrushes(Brush fill, Brush outline)
        {
            _glyphFill = fill;
            _glyphOutline = outline;

            OnPropertyChanged(nameof(GlyphFill), nameof(GlyphOutline));
        }

        public void Highlight(RenderGlyphDetails details)
        {
            foreach (var item in _items)
                if (item.RenderingElement is DirectWriteElement el)
                    el.Highlight(details);
        }

        private void OnPropertyChanged(params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
