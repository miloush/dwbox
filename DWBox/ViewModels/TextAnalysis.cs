using System.Collections.Generic;
using System.Collections.ObjectModel;
using Win32.DWrite;

namespace DWBox
{
    public class TextAnalysis : Collection<TextAnalysisItem>
    {
        //private string _text;
        //private BoxItem _item;

        //public TextAnalysis(string text, BoxItem item)
        //{
        //    _text = text;
        //    _item = item;
        //}

        //public string Name => _item.NameVersion;
        //public float EmSize => _item.RenderingElement?.FontSize ?? 48f;


        public string Text { get; }

        public TextAnalysis(string text)
        {
            Text = text;
        }

        public static TextAnalysis Analyze(string text, ReadingDirection readingDirection = ReadingDirection.LeftToRight, string cultureName = null)
        {
            TextAnalysis items = new TextAnalysis(text);
            for (int i = 0; i < text.Length; i++)
                items.Add(new TextAnalysisItem());

            for (int i = 0; i < text.Length; i++)
            {
                items[i].Character = text[i];
             
                if (char.IsSurrogatePair(text, i))
                {
                    items[i].CharacterString = text.Substring(i, 2);
                    items[i + 1].CharacterString = "";
                }
            }

            var propertiesCache = new Dictionary<int, ScriptProperties>();

            var analyzer = (DWrite.IDWriteTextAnalyzer1)DWriteFactory.Shared.CreateTextAnalyzer();
            var source = new StringAnalysisSource(text, readingDirection, cultureName);

            var sink = new TextAnalysisSink();

            sink.OnScriptAnalysis += (s, e) =>
            {
                if (!propertiesCache.TryGetValue(e.ScriptAnalysis.Script, out ScriptProperties properties))
                    properties = analyzer.GetScriptProperties(e.ScriptAnalysis);

                for (int i = 0; i < e.TextLength; i++)
                    items[e.TextPosition + i].ScriptProperties = properties;
            };

            sink.OnLineBreakpointAnalysis += (s, e) =>
            {
                for (int i = 0; i < e.TextLength; i++)
                    items[e.TextPosition + i].LineBreakpoint = e.LineBreakpoints[i];
            };

            sink.OnBidiLevelAnalysis += (s, e) =>
            {
                for (int i = 0; i < e.TextLength; i++)
                {
                    TextAnalysisItem item = items[e.TextPosition + i];
                    item.BidiExplicitLevel = e.ExplicitLevel;
                    item.BidiResolvedLevel = e.ResolvedLevel;
                }
            };

            sink.OnGlyphOrientationAnalysis += (s, e) =>
            {
                for (int i = 0; i < e.TextLength; i++)
                {
                    TextAnalysisItem item = items[e.TextPosition + i];
                    item.GlyphOrientationAngle = e.GlyphOrientationAngle;
                    item.AdjustedBidiLevel = e.AdjustedBidiLevel;
                    item.IsSideWays = e.IsSideWays;
                    item.IsRightToLeft = e.IsRightToLeft;
                }
            };

            analyzer.AnalyzeScript(source, 0, text.Length, sink);
            analyzer.AnalyzeLineBreakpoints(source, 0, text.Length, sink);
            analyzer.AnalyzeBidi(source, 0, text.Length, sink);
            analyzer.AnalyzeVerticalGlyphOrientation(source, 0, text.Length, sink);

            return items;
        }

    }
}
