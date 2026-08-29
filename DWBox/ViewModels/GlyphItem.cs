using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace DWBox
{
    public class GlyphItem : INotifyPropertyChanged
    {
        private RenderGlyphDetails _details;
        public RenderGlyphDetails Details => _details;

        public GlyphItem(RenderGlyphDetails details)
        {
            _details = details;
        }

        public Thickness OriginMargin => new Thickness(_details.TransformedX, _details.TransformedY, 0, 0);

        private bool _isRunHighlited; 
        private bool _isClusterHighlited;
        private bool _isGlyphHighlighted;

        public bool IsRunHighlighted
        {
            get { return _isRunHighlited; }
            set
            {
                if (_isRunHighlited != value)
                {
                    _isRunHighlited = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsClusterHighlighted
        {
            get { return _isClusterHighlited; }
            set
            {
                if (_isClusterHighlited != value)
                {
                    _isClusterHighlited = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsGlyphHighlighted
        {
            get { return _isGlyphHighlighted; }
            set
            {
                if (_isGlyphHighlighted != value)
                {
                    _isGlyphHighlighted = value;
                    OnPropertyChanged();
                }
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
