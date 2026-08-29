using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Win32.D2D1;
using Win32.DWrite;

namespace DWBox
{
    public class StreamGeometrySink : DWrite.IDWriteGeometrySink
    {
        private PathGeometry _geometry = new();
        private PathFigure _figure;
        private bool _stroked = true;

        public PathGeometry Geometry => _geometry;

        public void SetFillMode(FillMode fillMode)
        {
            _geometry.FillRule = fillMode == FillMode.Alternate ? FillRule.EvenOdd : FillRule.Nonzero;
        }

        public void SetSegmentFlags(Win32.D2D1.PathSegment vertexFlags)
        {
            _stroked = (vertexFlags & Win32.D2D1.PathSegment.ForceUnstroked) == 0;
        }

        public void BeginFigure(Point2F startPoint, FigureBegin figureBegin)
        {
            _figure = new PathFigure();
            _figure.StartPoint = (Point)startPoint;
            _figure.IsFilled = figureBegin == FigureBegin.Filled;
        }

        public void AddLines(Point2F[] points, int pointsCount)
        {
            if (pointsCount == 1)
                _figure.Segments.Add(new LineSegment((Point)points[0], _stroked));
            else
                _figure.Segments.Add(new PolyLineSegment(points.Select(p => (Point)p), _stroked));
        }

        public void AddBeziers(Win32.D2D1.BezierSegment[] beziers, int beziersCount)
        {
            for (int i = 0; i < beziersCount; i++)
                _figure.Segments.Add((System.Windows.Media.BezierSegment)beziers[i]);
        }

        public void EndFigure(FigureEnd figureEnd)
        {
            _figure.IsClosed = figureEnd == FigureEnd.Closed;
            _geometry.Figures.Add(_figure);
        }

        public void Close()
        {
            _geometry.Freeze();
        }
    }
}
