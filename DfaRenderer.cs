using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DfaSimulator
{
    public class DfaRenderer
    {
        private readonly Canvas _canvas;

        public DfaRenderer(Canvas canvas)
        {
            _canvas = canvas;
        }

        public void Refresh(DfaManager manager, string? simActiveStateId, string? simActiveTransitionId, MouseButtonEventHandler transitionClick)
        {
            _canvas.Children.Clear();

            // 1. Draw grid pattern lines
            DrawGrid();

            // 2. Draw Transition Paths
            foreach (var trans in manager.Transitions)
            {
                var fromState = manager.States.FirstOrDefault(s => s.Id == trans.FromId);
                var toState = manager.States.FirstOrDefault(s => s.Id == trans.ToId);
                if (fromState == null || toState == null) continue;

                bool isSelfLoop = trans.FromId == trans.ToId;
                bool isBidirectional = manager.IsBidirectional(trans.FromId, trans.ToId);
                bool isActive = manager.ActiveTransition == trans;
                bool isSimulated = simActiveTransitionId == trans.Id;

                Brush strokeBrush = isActive ? BrushFromHex("#EF4444") : isSimulated ? BrushFromHex("#3B82F6") : BrushFromHex("#64748B");
                double strokeThickness = isActive || isSimulated ? 3.5 : 2;

                if (isSelfLoop)
                {
                    DrawSelfLoop(fromState, trans, strokeBrush, strokeThickness, isSimulated, transitionClick);
                }
                else
                {
                    DrawStandardTransition(fromState, toState, trans, isBidirectional, strokeBrush, strokeThickness, isSimulated, transitionClick);
                }
            }

            // 3. Draw State Circles and Labels
            foreach (var state in manager.States)
            {
                bool isActive = manager.ActiveState == state;
                bool isSimulated = simActiveStateId == state.Id;

                Brush borderBrush = isActive ? BrushFromHex("#EF4444") : isSimulated ? BrushFromHex("#3B82F6") : BrushFromHex(state.BorderColor);
                double borderThickness = isActive ? Math.Max(3, state.BorderWidth + 1) : isSimulated ? Math.Max(3.5, state.BorderWidth + 1.5) : state.BorderWidth;
                Brush fillBrush = BrushFromHex(state.FillColor);

                if (isActive)
                {
                    Ellipse glow = new Ellipse
                    {
                        Width = (state.Radius + 6) * 2,
                        Height = (state.Radius + 6) * 2,
                        Stroke = BrushFromHex("#EF4444"),
                        StrokeThickness = 1.5,
                        StrokeDashArray = new DoubleCollection(new double[] { 3, 2 }),
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(glow, state.X - state.Radius - 6);
                    Canvas.SetTop(glow, state.Y - state.Radius - 6);
                    _canvas.Children.Add(glow);
                }

                if (isSimulated)
                {
                    Ellipse pulse = new Ellipse
                    {
                        Width = (state.Radius + 9) * 2,
                        Height = (state.Radius + 9) * 2,
                        Stroke = BrushFromHex("#3B82F6"),
                        StrokeThickness = 2.5,
                        Opacity = 0.6,
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(pulse, state.X - state.Radius - 9);
                    Canvas.SetTop(pulse, state.Y - state.Radius - 9);
                    _canvas.Children.Add(pulse);
                }

                Ellipse circle = new Ellipse
                {
                    Width = state.Radius * 2,
                    Height = state.Radius * 2,
                    Fill = fillBrush,
                    Stroke = borderBrush,
                    StrokeThickness = borderThickness,
                    Tag = state
                };
                Canvas.SetLeft(circle, state.X - state.Radius);
                Canvas.SetTop(circle, state.Y - state.Radius);
                _canvas.Children.Add(circle);

                if (state.IsAccepting)
                {
                    double innerR = Math.Max(6, state.Radius - 5);
                    Ellipse innerCircle = new Ellipse
                    {
                        Width = innerR * 2,
                        Height = innerR * 2,
                        Stroke = borderBrush,
                        StrokeThickness = isActive ? 2 : isSimulated ? 2.5 : Math.Max(1, state.BorderWidth - 0.5),
                        Fill = Brushes.Transparent,
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(innerCircle, state.X - innerR);
                    Canvas.SetTop(innerCircle, state.Y - innerR);
                    _canvas.Children.Add(innerCircle);
                }

                if (state.IsInitial)
                {
                    double arrowOriginX = state.X - state.Radius - 32;
                    Line startLine = new Line
                    {
                        X1 = arrowOriginX, Y1 = state.Y,
                        X2 = state.X - state.Radius - 2, Y2 = state.Y,
                        Stroke = isSimulated ? BrushFromHex("#3B82F6") : BrushFromHex("#6366F1"),
                        StrokeThickness = 3
                    };
                    _canvas.Children.Add(startLine);

                    Polygon head = new Polygon
                    {
                        Points = new PointCollection(new Point[] {
                            new Point(state.X - state.Radius - 1, state.Y),
                            new Point(state.X - state.Radius - 9, state.Y - 4),
                            new Point(state.X - state.Radius - 9, state.Y + 4)
                        }),
                        Fill = isSimulated ? BrushFromHex("#3B82F6") : BrushFromHex("#6366F1")
                    };
                    _canvas.Children.Add(head);
                }

                TextBlock textBlock = new TextBlock
                {
                    Text = state.Label,
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Foreground = isSimulated ? BrushFromHex("#1E40AF") : BrushFromHex("#334155"),
                    IsHitTestVisible = false
                };
                textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(textBlock, state.X - textBlock.DesiredSize.Width / 2);
                Canvas.SetTop(textBlock, state.Y - textBlock.DesiredSize.Height / 2);
                _canvas.Children.Add(textBlock);
            }
        }

        private void DrawGrid()
        {
            for (int x = 0; x < _canvas.ActualWidth || x < 1200; x += 20)
            {
                Line gridLine = new Line
                {
                    X1 = x, Y1 = 0, X2 = x, Y2 = 800,
                    Stroke = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    StrokeThickness = 1
                };
                _canvas.Children.Add(gridLine);
            }
            for (int y = 0; y < _canvas.ActualHeight || y < 800; y += 20)
            {
                Line gridLine = new Line
                {
                    X1 = 0, Y1 = y, X2 = 1200, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    StrokeThickness = 1
                };
                _canvas.Children.Add(gridLine);
            }
        }

        private void DrawStandardTransition(DfaState from, DfaState to, DfaTransition trans, bool isBidirectional, Brush brush, double thickness, bool isSimulated, MouseButtonEventHandler clickHandler)
        {
            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist == 0) return;

            Point startPt, endPt;
            Point labelPt;
            double angleRad;

            if (isBidirectional)
            {
                double midX = (from.X + to.X) / 2;
                double midY = (from.Y + to.Y) / 2;
                double nx = -dy / dist;
                double ny = dx / dist;

                double ctrlX = midX + nx * 35;
                double ctrlY = midY + ny * 35;

                double dqx1 = ctrlX - from.X;
                double dqy1 = ctrlY - from.Y;
                double d1 = Math.Sqrt(dqx1 * dqx1 + dqy1 * dqy1);
                startPt = new Point(from.X + (dqx1 / d1) * from.Radius, from.Y + (dqy1 / d1) * from.Radius);

                double dqx2 = to.X - ctrlX;
                double dqy2 = to.Y - ctrlY;
                double d2 = Math.Sqrt(dqx2 * dqx2 + dqy2 * dqy2);
                endPt = new Point(to.X - (dqx2 / d2) * to.Radius, to.Y - (dqy2 / d2) * to.Radius);

                PathFigure figure = new PathFigure { StartPoint = startPt };
                figure.Segments.Add(new QuadraticBezierSegment
                {
                    Point1 = new Point(ctrlX, ctrlY),
                    Point2 = endPt
                });
                PathGeometry geometry = new PathGeometry();
                geometry.Figures.Add(figure);

                Path path = new Path
                {
                    Stroke = brush,
                    StrokeThickness = thickness,
                    Data = geometry,
                    Tag = trans
                };
                if (isSimulated) path.StrokeDashArray = new DoubleCollection(new double[] { 4, 2 });
                path.MouseLeftButtonDown += clickHandler;
                _canvas.Children.Add(path);

                labelPt = new Point(
                    0.25 * startPt.X + 0.5 * ctrlX + 0.25 * endPt.X + nx * 14,
                    0.25 * startPt.Y + 0.5 * ctrlY + 0.25 * endPt.Y + ny * 14
                );
                angleRad = Math.Atan2(endPt.Y - ctrlY, endPt.X - ctrlX);
            }
            else
            {
                double startX = from.X + (dx / dist) * from.Radius;
                double startY = from.Y + (dy / dist) * from.Radius;
                double endX = to.X - (dx / dist) * to.Radius;
                double endY = to.Y - (dy / dist) * to.Radius;

                startPt = new Point(startX, startY);
                endPt = new Point(endX, endY);

                Line line = new Line
                {
                    X1 = startPt.X, Y1 = startPt.Y,
                    X2 = endPt.X, Y2 = endPt.Y,
                    Stroke = brush,
                    StrokeThickness = thickness,
                    Tag = trans
                };
                if (isSimulated) line.StrokeDashArray = new DoubleCollection(new double[] { 4, 2 });
                line.MouseLeftButtonDown += clickHandler;
                _canvas.Children.Add(line);

                double nx = -dy / dist;
                double ny = dx / dist;
                labelPt = new Point((startX + endX) / 2 + nx * 14, (startY + endY) / 2 + ny * 14);
                angleRad = Math.Atan2(to.Y - from.Y, to.X - from.X);
            }

            double angleDeg = angleRad * (180 / Math.PI);
            Polygon head = new Polygon
            {
                Points = new PointCollection(new Point[] {
                    new Point(0, 0),
                    new Point(-11, -5),
                    new Point(-8, 0),
                    new Point(-11, 5)
                }),
                Fill = brush,
                RenderTransform = new TransformGroup
                {
                    Children = new TransformCollection(new Transform[] {
                        new RotateTransform(angleDeg),
                        new TranslateTransform(endPt.X, endPt.Y)
                    })
                }
            };
            _canvas.Children.Add(head);

            Border back = new Border
            {
                Background = Brushes.White,
                BorderBrush = brush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(4, 2, 4, 2),
                IsHitTestVisible = false
            };
            back.Child = new TextBlock
            {
                Text = string.Join(",", trans.Symbols),
                FontSize = 10,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex("#1E293B")
            };
            back.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(back, labelPt.X - back.DesiredSize.Width / 2);
            Canvas.SetTop(back, labelPt.Y - back.DesiredSize.Height / 2);
            _canvas.Children.Add(back);
        }

        private void DrawSelfLoop(DfaState state, DfaTransition trans, Brush brush, double thickness, bool isSimulated, MouseButtonEventHandler clickHandler)
        {
            double r = state.Radius;
            double cx = state.X;
            double cy = state.Y;

            double sin30 = 0.5;
            double cos30 = 0.866;
            Point pStart = new Point(cx - r * sin30, cy - r * cos30);
            Point pEnd = new Point(cx + r * sin30, cy - r * cos30);

            Point cp1 = new Point(cx - r * 1.5, cy - r * 2.2);
            Point cp2 = new Point(cx + r * 1.5, cy - r * 2.2);

            PathFigure figure = new PathFigure { StartPoint = pStart };
            figure.Segments.Add(new BezierSegment(cp1, cp2, pEnd, true));
            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            Path path = new Path
            {
                Stroke = brush,
                StrokeThickness = thickness,
                Data = geometry,
                Tag = trans
            };
            if (isSimulated) path.StrokeDashArray = new DoubleCollection(new double[] { 4, 2 });
            path.MouseLeftButtonDown += clickHandler;
            _canvas.Children.Add(path);

            double angleRad = Math.Atan2(pEnd.Y - cp2.Y, pEnd.X - cp2.X);
            double angleDeg = angleRad * (180 / Math.PI);
            Polygon head = new Polygon
            {
                Points = new PointCollection(new Point[] {
                    new Point(0, 0),
                    new Point(-11, -5),
                    new Point(-8, 0),
                    new Point(-11, 5)
                }),
                Fill = brush,
                RenderTransform = new TransformGroup
                {
                    Children = new TransformCollection(new Transform[] {
                        new RotateTransform(angleDeg),
                        new TranslateTransform(pEnd.X, pEnd.Y)
                    })
                }
            };
            _canvas.Children.Add(head);

            Border back = new Border
            {
                Background = Brushes.White,
                BorderBrush = brush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(3, 1, 3, 1),
                IsHitTestVisible = false
            };
            back.Child = new TextBlock
            {
                Text = string.Join(",", trans.Symbols),
                FontSize = 9,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex("#1E293B")
            };
            back.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(back, cx - back.DesiredSize.Width / 2);
            Canvas.SetTop(back, cy - r * 2.1 - back.DesiredSize.Height / 2);
            _canvas.Children.Add(back);
        }

        public Brush BrushFromHex(string hex)
        {
            try
            {
                if (string.IsNullOrEmpty(hex)) return Brushes.Black;
                return (SolidColorBrush)new BrushConverter().ConvertFromString(hex) ?? Brushes.Black;
            }
            catch
            {
                return Brushes.SlateGray;
            }
        }
    }
}
