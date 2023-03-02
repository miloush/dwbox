using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Win32.DWrite;

namespace DWBox
{
    public class TextAnalysisItem
    {
        public char Character { get; set; }
        public string CharacterCode => "U+" + ((int)Character).ToString("X4");
        private string _characterString;
        public string CharacterString
        {
            get { return _characterString ?? Character.ToString(); }
            set { _characterString = value; }
        }

        public ScriptProperties ScriptProperties { get; set; }

        public LineBreakpoint LineBreakpoint { get; set; }

        public int BidiExplicitLevel { get; set; }
        public int BidiResolvedLevel { get; set; }

        public GlyphOrientationAngle GlyphOrientationAngle { get; set;  }
        public int AdjustedBidiLevel { get; set;  }
        public bool IsSideWays { get; set; }
        public bool IsRightToLeft { get; set; }

        public Brush BreakBeforeBrush => GetBreakBrush(LineBreakpoint.BreakConditionBefore);
        public Brush BreakAfterBrush => GetBreakBrush(LineBreakpoint.BreakConditionAfter);

        public Brush BidiExplicitBrush => GetBidiBrush(BidiExplicitLevel);
        public Brush BidiResolvedBrush => GetBidiBrush(BidiResolvedLevel);
        public Brush BidiAdjustedBrush => GetBidiBrush(AdjustedBidiLevel);

        private static Brush GetBidiBrush(int level)
        {
            return level % 2 == 0 ? Brushes.PaleTurquoise : Brushes.PaleGoldenrod;
        }

        private static Brush GetBreakBrush(BreakCondition condition)
        {
            switch (condition)
            {
                case BreakCondition.CanBreak:
                    return Brushes.PaleGreen;
                case BreakCondition.MayNotBreak:
                    return Brushes.PaleGoldenrod;
                case BreakCondition.MustBreak:
                    return Brushes.PaleVioletRed;

                case BreakCondition.Neutral:
                default:
                    return Brushes.White;

            }
        }
    }
}