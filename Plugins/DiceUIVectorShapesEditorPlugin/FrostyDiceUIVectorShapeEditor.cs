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
    [TemplatePart(Name = PART_LodComboBox, Type = typeof(ComboBox))]
    public class FrostyDiceUIVectorShapeEditor : FrostyAssetEditor
    {
        private const string PART_Canvas = "PART_Canvas";
        private const string PART_Outline = "PART_Outline";
        private const string PART_LodComboBox = "PART_LodComboBox";

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
        private ComboBox lodComboBox;

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
            lodComboBox = GetTemplateChild(PART_LodComboBox) as ComboBox;
            lodComboBox.SelectionChanged += LodComboBox_SelectionChanged;
        }

        private void FrostySvgImageEditor_Update(object sender, RoutedEventArgs e)
        {
            UpdateCanvas();

            dynamic vectorShapes = RootObject;
            int old = lodComboBox.SelectedIndex;
            lodComboBox.Items.Clear();
            lodComboBox.Items.Add("None");
            for (int i = 0; i < vectorShapes.Shapes.Count; i++)
                lodComboBox.Items.Add(i);
            if (old > (lodComboBox.Items.Count - 1) || old == -1) lodComboBox.SelectedIndex = 0;
            else lodComboBox.SelectedIndex = old;
        }

        private void LodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (lodComboBox.SelectedIndex == -1)
                return;
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
            Grid imageGrid = new Grid() {
                Margin = new Thickness(5)
            };

            Polygon layoutRect = new Polygon();
            layoutRect.Stroke = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            layoutRect.StrokeThickness = 1 ;
            if (!OutlineVisible) layoutRect.Stroke = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            layoutRect.Points.Add(new Point(vectorShapes.LayoutRect.x, vectorShapes.LayoutRect.y)); // top left
            layoutRect.Points.Add(new Point(vectorShapes.LayoutRect.z, vectorShapes.LayoutRect.y)); // top right
            layoutRect.Points.Add(new Point(vectorShapes.LayoutRect.z, vectorShapes.LayoutRect.w)); // bottom right
            layoutRect.Points.Add(new Point(vectorShapes.LayoutRect.x, vectorShapes.LayoutRect.w)); // bottom left
            imageGrid.Children.Add(layoutRect);

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

            foreach (dynamic shape in vectorShapes.Shapes) {
                int shapeIndex = vectorShapes.Shapes.IndexOf(shape);
                if (lodComboBox.SelectedIndex - 1 == shapeIndex) {
                    for (int i = 0; i < shape.Path.Corners.Count; i++) {
                        Point point = new Point(shape.Path.Corners[i].Position.x, shape.Path.Corners[i].Position.y);

                        Button pointButton = new Button();
                        pointButton.HorizontalAlignment = HorizontalAlignment.Left;
                        pointButton.VerticalAlignment = VerticalAlignment.Top;
                        pointButton.Margin = new Thickness(point.X - 1, point.Y - 1, 0, 0);
                        pointButton.Padding = new Thickness(0.25);
                        pointButton.Height = 2;
                        pointButton.Width = 2;

                        pointButton.Content = i;
                        pointButton.FontSize = 1;

                        Style buttonStyle = new Style(typeof(Button));
                        buttonStyle.BasedOn = (Style)FindResource(typeof(Button));
                        buttonStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Black));
                        buttonStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));
                        buttonStyle.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(3)));

                        Trigger focusTrigger = new Trigger();
                        focusTrigger.Property = Control.IsMouseOverProperty;
                        focusTrigger.Value = true;
                        focusTrigger.Setters.Add(new Setter(Control.OpacityProperty, 0.99));

                        buttonStyle.Triggers.Add(focusTrigger);

                        Style borderStyle = new Style(typeof(Border));
                        borderStyle.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(4)));
                        pointButton.Resources.Add(typeof(Border), borderStyle);

                        pointButton.Style = buttonStyle;

                        imageGrid.Children.Add(pointButton);
                    }
                }
            }

            imageGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
            imageGrid.VerticalAlignment = VerticalAlignment.Stretch;

            canvas.Child = imageGrid;
        }
    }
}
