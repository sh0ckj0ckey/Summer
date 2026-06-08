using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using WinRT;

namespace Summer.Helpers
{
    public enum RecognizedShapeKind
    {
        None,
        Ellipse,
        Polygon
    }

    public sealed class RecognizedShapeResult
    {
        public RecognizedShapeKind ShapeKind { get; init; } = RecognizedShapeKind.None;

        public IReadOnlyList<uint> SourceStrokeIds { get; init; } = [];

        public IReadOnlyList<Point> Points { get; init; } = [];
    }

    public partial class InkShapeRecognizer
    {
        private readonly InkAnalyzer _analyzer = new();

        public void Clear()
        {
            _analyzer.ClearDataForAllStrokes();
        }

        public async Task<IReadOnlyList<RecognizedShapeResult>> AnalyzeAsync(IReadOnlyList<InkStroke> strokes)
        {
            var results = new List<RecognizedShapeResult>();

            if (strokes is null || strokes.Count <= 0)
            {
                return results;
            }

            _analyzer.ClearDataForAllStrokes();

            try
            {
                _analyzer.AddDataForStrokes(strokes);

                var analysisResult = await _analyzer.AnalyzeAsync();

                if (analysisResult.Status != InkAnalysisStatus.Updated)
                {
                    return results;
                }

                var drawings = _analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing);

                foreach (var node in drawings)
                {
                    InkAnalysisInkDrawing drawing;

                    try
                    {
                        drawing = node.As<InkAnalysisInkDrawing>();
                    }
                    catch
                    {
                        continue;
                    }

                    if (drawing is null)
                    {
                        continue;
                    }

                    if (drawing.DrawingKind == InkAnalysisDrawingKind.Drawing)
                    {
                        continue;
                    }

                    if (drawing.DrawingKind == InkAnalysisDrawingKind.Circle ||
                        drawing.DrawingKind == InkAnalysisDrawingKind.Ellipse)
                    {
                        var ellipsePoints = BuildEllipsePoints([.. drawing.Points]);

                        results.Add(new RecognizedShapeResult
                        {
                            ShapeKind = RecognizedShapeKind.Ellipse,
                            SourceStrokeIds = [.. drawing.GetStrokeIds()],
                            Points = ellipsePoints
                        });
                    }
                    else
                    {
                        results.Add(new RecognizedShapeResult
                        {
                            ShapeKind = RecognizedShapeKind.Polygon,
                            SourceStrokeIds = [.. drawing.GetStrokeIds()],
                            Points = [.. drawing.Points]
                        });
                    }
                }

                return results;
            }
            finally
            {
                _analyzer.ClearDataForAllStrokes();
            }
        }

        private static List<Point> BuildEllipsePoints(Point[] points)
        {
            if (points is null || points.Length < 2)
            {
                return [];
            }

            var (center, a, b, rotation) = CalculateEllipseParameters(points);
            return GenerateEllipsePoints(center, a, b, rotation);
        }

        private static (Point center, double a, double b, double rotation) CalculateEllipseParameters(Point[] points)
        {
            // Find the longest axis by searching for the farthest pair of points.
            double maxDistance = 0.0;
            Point p1 = points[0];
            Point p2 = points.Length > 1 ? points[1] : points[0];

            foreach (var pair in Combinations(points))
            {
                double distance = Distance(pair.Item1, pair.Item2);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    p1 = pair.Item1;
                    p2 = pair.Item2;
                }
            }

            // Compute the center point of the ellipse.
            var center = new Point((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0);

            // Compute the semi-major axis length and the rotation angle.
            double a = maxDistance / 2.0;
            double rotation = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);

            // Compute the semi-minor axis length using projection after rotation compensation.
            double b = 0.0;
            foreach (var point in points)
            {
                if (point.Equals(p1) || point.Equals(p2))
                {
                    continue;
                }

                // Translate the point to the ellipse center and rotate it back to the local axis system.
                double translatedX = point.X - center.X;
                double translatedY = point.Y - center.Y;
                double rotatedY = translatedX * Math.Sin(-rotation) + translatedY * Math.Cos(-rotation);

                double currentB = Math.Abs(rotatedY);
                if (currentB > b)
                {
                    b = currentB;
                }
            }

            if (b <= 0)
            {
                b = a;
            }

            return (center, a, b, rotation);
        }

        private static List<Point> GenerateEllipsePoints(Point center, double a, double b, double rotation, int segments = 256)
        {
            var points = new List<Point>(segments + 2);

            for (int i = 0; i <= segments; i++)
            {
                double angle = 2.0 * Math.PI * i / segments;

                // Calculate the point on the standard ellipse before rotation
                double x = a * Math.Cos(angle);
                double y = b * Math.Sin(angle);

                // Apply rotation to the ellipse point
                double rotatedX = x * Math.Cos(rotation) - y * Math.Sin(rotation);
                double rotatedY = x * Math.Sin(rotation) + y * Math.Cos(rotation);

                // Translate the rotated point to the ellipse center
                points.Add(new Point(center.X + rotatedX, center.Y + rotatedY));
            }

            if (points.Count > 0)
            {
                // Explicitly close the ellipse by repeating the first point
                points.Add(points[0]);
            }

            return points;
        }

        private static double Distance(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static IEnumerable<Tuple<Point, Point>> Combinations(Point[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                for (int j = i + 1; j < points.Length; j++)
                {
                    yield return Tuple.Create(points[i], points[j]);
                }
            }
        }
    }
}
