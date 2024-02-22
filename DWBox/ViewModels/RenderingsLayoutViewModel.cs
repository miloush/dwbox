using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace DWBox
{
    public class RenderingsLayoutViewModel
    {
        public string Name { get; set; }
        public ImageSource Icon { get; set;  }
        public ItemsPanelTemplate ItemsPanelTemplate { get; set; }
        public ScrollBarVisibility HorizontalScrollBarVisibility { get; set; }
        public ScrollBarVisibility VerticalScrollBarVisibility { get; set; }
    }
}
