using System.Windows.Controls;
using Win32.DWrite;

namespace DWBox
{
    public class BoxDirection
    {
        public ReadingDirection ReadingDirection { get; set; }
        public FlowDirection FlowDirection { get; set; }

        public bool IsVertical => ReadingDirection is ReadingDirection.TopToBottom or ReadingDirection.BottomToTop;
        public string ReadingGroup => IsVertical ? "Vertical" : "Horizontal";
        public Orientation ReadingOrientation => IsVertical ? Orientation.Vertical : Orientation.Horizontal;
        public Orientation FlowOrientation => IsVertical ? Orientation.Horizontal : Orientation.Vertical;

        public ScrollBarVisibility HorizontalScrollBarVisibility => IsVertical ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
        public ScrollBarVisibility VerticalScrollBarVisibility => IsVertical ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;

        public override string ToString()
        {
            return ToString(ReadingDirection) + ", " + ToString(FlowDirection);
        }

        private static string ToString(ReadingDirection direction)
        {
            return direction switch
            {
                ReadingDirection.LeftToRight => "LTR",
                ReadingDirection.RightToLeft => "RTL",
                ReadingDirection.TopToBottom => "TTB",
                ReadingDirection.BottomToTop => "BTT",
                _ => direction.ToString()
            };
        }
        private static string ToString(FlowDirection direction)
        {
            return direction switch
            {
                FlowDirection.LeftToRight => "LTR",
                FlowDirection.RightToLeft => "RTL",
                FlowDirection.TopToBottom => "TTB",
                FlowDirection.BottomToTop => "BTT",
                _ => direction.ToString()
            };
        }
    }
}