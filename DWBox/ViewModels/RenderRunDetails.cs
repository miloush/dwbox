namespace DWBox
{
    public class RenderRunDetails
    {
        public int Index { get; set; }
        public float BaselineOriginX { get; set; }
        public float BaselineOriginY { get; set; }
        public double OrientationAngle { get; set; }

        public string LocaleName { get; set; }
        public string Text { get; set; }
        public int TextPosition { get; set; }

        public string FontName { get; set; }
        public int BidiLevel { get; set; }
    }
}
