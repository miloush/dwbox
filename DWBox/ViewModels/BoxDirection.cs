using System.Windows;
using System.Windows.Controls;
using Win32.DWrite;
using FlowDirection = Win32.DWrite.FlowDirection;

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

        public Dock BoxHeaderDock => IsVertical ? Dock.Left : Dock.Top;
        public double BoxHeaderAngle => IsVertical ? 90 : 0;
        public double BoxHeaderStackedAngle => IsVertical ? -90 : 0;

        public VerticalAlignment BoxOverlayVerticalAlignment => IsVertical ? VerticalAlignment.Stretch : VerticalAlignment.Bottom;
        public HorizontalAlignment BoxOverlayHorizontalAlignment => IsVertical ? HorizontalAlignment.Right : HorizontalAlignment.Stretch;

        public ExpandDirection ExpandDirection => IsVertical ? ExpandDirection.Right : ExpandDirection.Down;

        public Visibility HorizontalVisibility => IsVertical ? Visibility.Collapsed : Visibility.Visible;
        public Visibility VerticalVisibility => IsVertical ? Visibility.Visible : Visibility.Collapsed;

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