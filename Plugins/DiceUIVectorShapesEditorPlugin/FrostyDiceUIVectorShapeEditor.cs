using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DiceUIVectorShapesEditorPlugin
{
    [TemplatePart(Name = PART_Canvas, Type = typeof(Viewbox))]
    [TemplatePart(Name = PART_Outline, Type = typeof(ToggleButton))]
    public class FrostyDiceUIVectorShapeEditor : FrostyAssetEditor
    {
        private const string PART_Canvas = "PART_Canvas";
        private const string PART_Outline = "PART_Outline";

        #region -- GridVisible --
        public static readonly DependencyProperty GridVisibleProperty = DependencyProperty.Register("GridVisible", typeof(bool), typeof(FrostyDiceUIVectorShapeEditor), new FrameworkPropertyMetadata(false));
        public bool GridVisible
        {
            get => (bool)GetValue(GridVisibleProperty);
            set => SetValue(GridVisibleProperty, value);
        }
        #endregion

        #region -- OutlineVisible --
        public static readonly DependencyProperty OutlineVisibleProperty = DependencyProperty.Register("OutlineVisible", typeof(bool), typeof(FrostyDiceUIVectorShapeEditor), new FrameworkPropertyMetadata(true));
        public bool OutlineVisible {
            get => (bool)GetValue(OutlineVisibleProperty);
            set => SetValue(OutlineVisibleProperty, value);
        }
        #endregion

        private Viewbox canvas;
        private ToggleButton outline;

        static FrostyDiceUIVectorShapeEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FrostyDiceUIVectorShapeEditor), new FrameworkPropertyMetadata(typeof(FrostyDiceUIVectorShapeEditor)));
        }

        public FrostyDiceUIVectorShapeEditor(ILogger inLogger)
            : base(inLogger)
        {
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            canvas = GetTemplateChild(PART_Canvas) as Viewbox;
            Loaded += FrostySvgImageEditor_Update;
            OnAssetModified += FrostySvgImageEditor_Update;
            outline = GetTemplateChild(PART_Outline) as ToggleButton;
            outline.Checked += FrostySvgImageEditor_Update;
            outline.Unchecked += FrostySvgImageEditor_Update;
        }

        private void FrostySvgImageEditor_Update(object sender, RoutedEventArgs e)
        {
            UpdateCanvas();
        }

        //public override List<ToolbarItem> RegisterToolbarItems()
        //{
        //    return new List<ToolbarItem>()
        //    {
        //        new ToolbarItem("Export", "Export SVG", "Images/Export.png", new RelayCommand((object state) => { ExportButton_Click(this, new RoutedEventArgs()); })),
        //        new ToolbarItem("Import", "Import SVG", "Images/Import.png", new RelayCommand((object state) => { ImportButton_Click(this, new RoutedEventArgs()); })),
        //    };
        //}

        private void UpdateCanvas()
        {
            dynamic vectorShapes = RootObject;
            Grid imageGrid = new Grid()
            {
                Margin = new Thickness(5)
            };

            Polygon layoutRect = new Polygon();
            layoutRect.Stroke = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            layoutRect.StrokeThickness = (vectorShapes.LayoutRect.z - vectorShapes.LayoutRect.x) / 100;
            if (!OutlineVisible) layoutRect.StrokeThickness = 0;
            layoutRect.Points.Add(new Point(vectorShapes.LayoutRect.x, vectorShapes.LayoutRect.y));
            layoutRect.Points.Add(new Point(vectorShapes.LayoutRect.z, vectorShapes.LayoutRect.y));
            layoutRect.Points.Add(new Point(vectorShapes.LayoutRect.z, vectorShapes.LayoutRect.w));
            layoutRect.Points.Add(new Point(vectorShapes.LayoutRect.x, vectorShapes.LayoutRect.w));

            foreach (dynamic shape in vectorShapes.Shapes) {
                if (shape.Path.Corners.Count != 0) {
                    PathFigure p = new PathFigure();

                    p.StartPoint = new Point(shape.Path.Corners[0].Position.x, shape.Path.Corners[0].Position.y);

                    if (shape.Path.Corners.Count > 1) {
                        for (int i = 1; i < shape.Path.Corners.Count; i++) {
                            Point point = new Point(shape.Path.Corners[i].Position.x, shape.Path.Corners[i].Position.y);
                            double radius = shape.Path.Corners[i].Radius;

                            SweepDirection direction;
                            if (radius < 0) {
                                direction = SweepDirection.Clockwise;
                                radius *= -1;
                            }
                            else {
                                direction = SweepDirection.Counterclockwise;
                            }

                            ArcSegment arc = new ArcSegment(point, new Size(radius, radius), 0, false, direction, true);
                            p.Segments.Add(arc);
                        }

                        // Add extra point to fix things if its a fill or outline
                        if ((int)shape.DrawStyle == 1 || (int)shape.DrawStyle == 2) {
                            Point point = new Point(shape.Path.Corners[0].Position.x, shape.Path.Corners[0].Position.y);
                            double radius = shape.Path.Corners[0].Radius;

                            SweepDirection direction;
                            if (radius < 0) {
                                direction = SweepDirection.Clockwise;
                                radius *= -1;
                            }
                            else {
                                direction = SweepDirection.Counterclockwise;
                            }

                            ArcSegment arcLast = new ArcSegment(point, new Size(radius, radius), 0, false, direction, true);
                            p.Segments.Add(arcLast);
                            p.IsClosed = true;
                        }
                    }

                    PathGeometry geometry = new PathGeometry();
                    geometry.Figures.Add(p);

                    Path path = new Path();
                    path.Data = geometry;

                    if ((int)shape.DrawStyle == 0 || (int)shape.DrawStyle == 1)
                        path.Stroke = new SolidColorBrush(Color.FromScRgb(shape.Alpha, shape.Color.x, shape.Color.y, shape.Color.z));
                    if ((int)shape.DrawStyle == 2)
                        path.Fill = new SolidColorBrush(Color.FromScRgb(shape.Alpha, shape.Color.x, shape.Color.y, shape.Color.z));

                    path.StrokeThickness = shape.LineWidth == 0 ? 1 : shape.LineWidth;
                    path.StrokeLineJoin = (PenLineJoin)shape.Path.Corners[0].CornerType;
                    path.StrokeEndLineCap = (PenLineCap)shape.EndCapType;
                    path.StrokeStartLineCap = (PenLineCap)shape.StartCapType;

                    imageGrid.Children.Add(path);
                }
            }

            imageGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
            imageGrid.VerticalAlignment = VerticalAlignment.Stretch;

            imageGrid.Children.Add(layoutRect);

            canvas.Child = imageGrid;
        }
    }
}
