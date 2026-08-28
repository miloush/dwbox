using Win32.DWrite;

namespace DWBox
{
    public class BoxDirection
    {
        public ReadingDirection ReadingDirection { get; set; }
        public FlowDirection FlowDirection { get; set; }

        public bool IsVertical => ReadingDirection is ReadingDirection.TopToBottom or ReadingDirection.BottomToTop;
        public string FlowGroup => IsVertical ? "Vertical" : "Horizontal";

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