using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWBox
{
    public class AppViewModel : INotifyPropertyChanged
    {
        private readonly BoxItemCollection _items = new BoxItemCollection();
        public BoxItemCollection Items => _items;

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

        private void OnPropertyChanged(params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
