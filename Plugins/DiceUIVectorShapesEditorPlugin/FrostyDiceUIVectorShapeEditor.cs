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
using System.Windows.Media;
using System.Windows.Shapes;

namespace DiceUIVectorShapesEditorPlugin
{
    [TemplatePart(Name = PART_Canvas, Type = typeof(Viewbox))]
    public class FrostyDiceUIVectorShapeEditor : FrostyAssetEditor
    {
        private const string PART_Canvas = "PART_Canvas";

        #region -- GridVisible --
        public static readonly DependencyProperty GridVisibleProperty = DependencyProperty.Register("GridVisible", typeof(bool), typeof(FrostyDiceUIVectorShapeEditor), new FrameworkPropertyMetadata(false));
        public bool GridVisible
        {
            get => (bool)GetValue(GridVisibleProperty);
            set => SetValue(GridVisibleProperty, value);
        }
        #endregion

        private Viewbox canvas;

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
            layoutRect.Points.Add(new Point(vectorShapes.LayoutRect.x, vectorShapes.LayoutRect.y));
            layoutRect.Points.Add(new Point(vectorShapes.LayoutRect.z, vectorShapes.LayoutRect.y));
            layoutRect.Points.Add(new Point(vectorShapes.LayoutRect.z, vectorShapes.LayoutRect.w));
            layoutRect.Points.Add(new Point(vectorShapes.LayoutRect.x, vectorShapes.LayoutRect.w));

            imageGrid.Children.Add(layoutRect);
            foreach (dynamic shape in vectorShapes.Shapes)
            {
                PathFigure p = new PathFigure();

                p.StartPoint = new Point(shape.Path.Corners[0].Position.x, shape.Path.Corners[0].Position.y);

                if(shape.Path.Corners.Count > 1) {
                    for (int i = 1; i < shape.Path.Corners.Count; i++) {

                        Point pointMinus;
                        if (i == 0)
                            pointMinus = new Point(shape.Path.Corners[0].Position.x, shape.Path.Corners[0].Position.y);
                        else
                            pointMinus = new Point(shape.Path.Corners[i - 1].Position.x, shape.Path.Corners[i - 1].Position.y);


                        Point point = new Point(shape.Path.Corners[i].Position.x, shape.Path.Corners[i].Position.y);
                        Point pointPlus;
                        if (i + 1 == shape.Path.Corners.Count) 
                            pointPlus = new Point(shape.Path.Corners[0].Position.x, shape.Path.Corners[0].Position.y);
                        else 
                            pointPlus = new Point(shape.Path.Corners[i + 1].Position.x, shape.Path.Corners[i + 1].Position.y);
                        
                        Vector v1 = point - pointMinus;
                        Vector v2 = pointPlus - point;
                        double radius = shape.Path.Corners[i].Radius;

                        SweepDirection direction = (Vector.AngleBetween(v1, v2) > 0) ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;

                        ArcSegment arc = new ArcSegment(point, new Size(radius, radius), 0, false, direction, true);
                        p.Segments.Add(arc);
                    }

                    //Add extra point to fix things if its a fill
                    Point pointL = new Point(shape.Path.Corners[0].Position.x, shape.Path.Corners[0].Position.y);
                    Point pointMinusL = new Point(shape.Path.Corners[shape.Path.Corners.Count - 1].Position.x, shape.Path.Corners[shape.Path.Corners.Count - 1].Position.y);
                    Point pointPlusL = new Point(shape.Path.Corners[1].Position.x, shape.Path.Corners[1].Position.y);
                    Vector v1L = pointL - pointMinusL;
                    Vector v2L = pointPlusL - pointL;
                    SweepDirection directionL = (Vector.AngleBetween(v1L, v2L) > 0) ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;
                    ArcSegment arcL = new ArcSegment(pointL, new Size(shape.Path.Corners[0].Radius, shape.Path.Corners[0].Radius), 0, false, directionL, true);
                    if ((int)shape.DrawStyle == 2)
                        p.Segments.Add(arcL);
                }

                PathGeometry geometry = new PathGeometry();
                geometry.Figures.Add(p);

                Path path = new Path();
                path.Data = geometry;

                if ((int)shape.DrawStyle == 0 || (int)shape.DrawStyle == 1)
                    path.Stroke = new SolidColorBrush(Color.FromScRgb(shape.Alpha, shape.Color.x, shape.Color.y, shape.Color.z));
                if ((int)shape.DrawStyle == 2)
                    path.Fill = new SolidColorBrush(Color.FromScRgb(shape.Alpha, shape.Color.x, shape.Color.y, shape.Color.z));

                path.StrokeThickness = shape.LineWidth;
                path.StrokeEndLineCap = (PenLineCap)shape.EndCapType;
                path.StrokeStartLineCap = (PenLineCap)shape.StartCapType;

                imageGrid.Children.Add(path);
            }

            imageGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
            imageGrid.VerticalAlignment = VerticalAlignment.Stretch;

            canvas.Child = imageGrid;
        }
    }
}
