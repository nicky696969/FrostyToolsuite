using Frosty.Controls;
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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DiceUIVectorShapesEditorPlugin
{
    [TemplatePart(Name = PART_Canvas, Type = typeof(Viewbox))]
    [TemplatePart(Name = PART_Outline, Type = typeof(ToggleButton))]
    [TemplatePart(Name = PART_LodComboBox, Type = typeof(ComboBox))]
    [TemplatePart(Name = PART_AssetPropertyGrid, Type = typeof(FrostyPropertyGrid))]
    [TemplatePart(Name = PART_Panel, Type = typeof(FrostyDockablePanel))]
    public class FrostyDiceUIVectorShapeEditor : FrostyAssetEditor
    {
        private const string PART_Canvas = "PART_Canvas";
        private const string PART_Outline = "PART_Outline";
        private const string PART_LodComboBox = "PART_LodComboBox";
        private const string PART_AssetPropertyGrid = "PART_AssetPropertyGrid";
        private const string PART_Panel = "PART_Panel";

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
        public bool OutlineVisible
        {
            get => (bool)GetValue(OutlineVisibleProperty);
            set => SetValue(OutlineVisibleProperty, value);
        }
        #endregion

        private Viewbox canvas;

        private ToggleButton outline;
        private ComboBox lodComboBox;
        private bool firstTimeLoad = true;
        private FrostyPropertyGrid pg;
        private FrostyDockablePanel dp;

        protected bool isDragging;
        private Point clickPosition;
        private TranslateTransform originTT;

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
            dp = GetTemplateChild(PART_Panel) as FrostyDockablePanel;
            dp.MouseLeftButtonDown += ResetPropertyGrid;
            canvas = GetTemplateChild(PART_Canvas) as Viewbox;
            Loaded += FrostySvgImageEditor_Update;
            outline = GetTemplateChild(PART_Outline) as ToggleButton;
            outline.Checked += FrostySvgImageEditor_Update;
            outline.Unchecked += FrostySvgImageEditor_Update;
            lodComboBox = GetTemplateChild(PART_LodComboBox) as ComboBox;
            lodComboBox.SelectionChanged += LodComboBox_SelectionChanged;
            pg = GetTemplateChild(PART_AssetPropertyGrid) as FrostyPropertyGrid;
            pg.OnModified += Pg_OnModified; ;
        }

        private void Pg_OnModified(object sender, ItemModifiedEventArgs e)
        {
            UpdateCanvas();
        }

        private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void Canvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // remove buttons
        }

        private void ResetPropertyGrid(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            pg.SetClass(RootObject);
            UpdateCanvas();
            
            //var draggableControl = sender as Viewbox;
            ////originTT = draggableControl.RenderTransform as TranslateTransform ?? new TranslateTransform();
            //isDragging = true;
            //var grid = canvas.Child as Grid;
            //clickPosition = e.GetPosition(grid.Children[0]);
            //draggableControl.CaptureMouse();
            //var p = new Polygon();
            //p.Points.Add(clickPosition);
            //p.Stroke = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            //grid.Children.Add(p);

            //for (int i = 0; i < grid.Children.Count; i++)
            //{
            //    //foreach (var point in ((dynamic)grid.Children[i]).Points)
            //    //{

            //    //}
            //}
        }

        private void FrostySvgImageEditor_Loaded(object sender, RoutedEventArgs e)
        {
            if (firstTimeLoad)
            {
                UpdateCanvas();

                firstTimeLoad = false;
            }
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

        private void LodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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

            Grid imageGrid = new Grid()
            {
                //Width = vectorShapes.LayoutRect.z - vectorShapes.LayoutRect.x,
                //Height = vectorShapes.LayoutRect.w - vectorShapes.LayoutRect.y,
                Margin = new Thickness(5)
                
            };

            double area = (vectorShapes.LayoutRect.z - vectorShapes.LayoutRect.x) * (vectorShapes.LayoutRect.w - vectorShapes.LayoutRect.y);

            Polygon layoutRect = new Polygon();
            layoutRect.Stroke = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            layoutRect.StrokeThickness = Math.Sqrt(area) / 100;
            if (!OutlineVisible) layoutRect.Stroke = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            layoutRect.Points.Add(new Point(vectorShapes.LayoutRect.x, vectorShapes.LayoutRect.y)); // top left
            layoutRect.Points.Add(new Point(vectorShapes.LayoutRect.z, vectorShapes.LayoutRect.y)); // top right
            layoutRect.Points.Add(new Point(vectorShapes.LayoutRect.z, vectorShapes.LayoutRect.w)); // bottom right
            layoutRect.Points.Add(new Point(vectorShapes.LayoutRect.x, vectorShapes.LayoutRect.w)); // bottom left
            imageGrid.Children.Add(layoutRect);

            foreach (dynamic shape in vectorShapes.Shapes)
            {
                if (shape.Path.Corners.Count != 0)
                {
                    PathFigure p = new PathFigure();

                    p.StartPoint = new Point(shape.Path.Corners[0].Position.x, shape.Path.Corners[0].Position.y);

                    if (shape.Path.Corners.Count > 1)
                    {
                        for (int i = 1; i < shape.Path.Corners.Count; i++)
                        {
                            Point point = new Point(shape.Path.Corners[i].Position.x, shape.Path.Corners[i].Position.y);
                            double radius = shape.Path.Corners[i].Radius;

                            SweepDirection direction;
                            if (radius < 0)
                            {
                                direction = SweepDirection.Clockwise;
                                radius *= -1;
                            }
                            else
                            {
                                direction = SweepDirection.Counterclockwise;
                            }

                            ArcSegment arc = new ArcSegment(point, new Size(radius, radius), 0, false, direction, true);
                            p.Segments.Add(arc);
                        }

                        // Add extra point to fix things if its a fill or outline
                        if ((int)shape.DrawStyle == 1 || (int)shape.DrawStyle == 2)
                        {
                            Point point = new Point(shape.Path.Corners[0].Position.x, shape.Path.Corners[0].Position.y);
                            double radius = shape.Path.Corners[0].Radius;

                            SweepDirection direction;
                            if (radius < 0)
                            {
                                direction = SweepDirection.Clockwise;
                                radius *= -1;
                            }
                            else
                            {
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
                    path.MouseDown += Path_MouseDown;
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

            foreach (dynamic shape in vectorShapes.Shapes)
            {
                int shapeIndex = vectorShapes.Shapes.IndexOf(shape);
                if (lodComboBox.SelectedIndex - 1 == shapeIndex)
                {
                    for (int i = 0; i < shape.Path.Corners.Count; i++)
                    {
                        CornerButton pointButton = GenerateCornerButtons(shape, i, area);

                        imageGrid.Children.Add(pointButton);
                    }
                }
            }

            imageGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
            imageGrid.VerticalAlignment = VerticalAlignment.Stretch;

            canvas.Child = imageGrid;
        }

        public CornerButton GenerateCornerButtons(dynamic shape, int i, double area) {
            Point point = new Point(shape.Path.Corners[i].Position.x, shape.Path.Corners[i].Position.y);
            double scale = Math.Sqrt(Math.Sqrt(area)) / 2;

            CornerButton pointButton = new CornerButton();
            pointButton.HorizontalAlignment = HorizontalAlignment.Left;
            pointButton.VerticalAlignment = VerticalAlignment.Top;
            pointButton.Margin = new Thickness(point.X - (scale / 2), point.Y - (scale / 2), 0, 0);
            pointButton.Padding = new Thickness(0.25);
            pointButton.Height = scale;
            pointButton.Width = scale;
            pointButton.Ebx = shape.Path.Corners[i];
            pointButton.Content = i;
            pointButton.FontSize = scale / 2;
            pointButton.PreviewMouseLeftButtonDown += PointButton_PreviewMouseLeftButtonDown;
            pointButton.PreviewMouseLeftButtonUp += PointButton_PreviewMouseLeftButtonUp;
            pointButton.MouseMove += PointButton_MouseMove;
            pointButton.MouseRightButtonDown += PointButton_MouseRightButtonDown;

            Style buttonStyle = new Style(typeof(Button));
            buttonStyle.BasedOn = (Style)FindResource(typeof(Button));
            buttonStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Black));
            buttonStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));

            //Trigger focusTrigger = new Trigger();
            //focusTrigger.Property = Control.IsFocusedProperty;
            //focusTrigger.Value = true;
            //focusTrigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            //focusTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(50, 50, 50))));

            //buttonStyle.Triggers.Add(focusTrigger);

            Style borderStyle = new Style(typeof(Border));
            borderStyle.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(scale)));
            pointButton.Resources.Add(typeof(Border), borderStyle);

            if (pg.SelectedClass == pointButton.Ebx) {
                buttonStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
                buttonStyle.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(50, 50, 50))));
            }
                
            pointButton.Style = buttonStyle;
            
            return pointButton;
        }

        private void Path_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            Grid grid = canvas.Child as Grid;
            int index = grid.Children.IndexOf((Path)sender);
            lodComboBox.SelectedIndex = index;
        }

        private void PointButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!isDragging) return;
            isDragging = false;
            var draggable = sender as CornerButton;
            Point currentPosition = e.GetPosition(canvas.Child);
            var transform = draggable.RenderTransform as TranslateTransform ?? new TranslateTransform();
            transform.X = originTT.X + (currentPosition.X - clickPosition.X);
            transform.Y = originTT.Y + (currentPosition.Y - clickPosition.Y);
            draggable.Ebx.Position.x += transform.X;
            draggable.Ebx.Position.y += transform.Y;
            UpdateCanvas();
            AssetModified = true;
            InvokeOnAssetModified();
            draggable.ReleaseMouseCapture();
        }

        private void PointButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var draggableControl = sender as CornerButton;
            originTT = draggableControl.RenderTransform as TranslateTransform ?? new TranslateTransform();
            isDragging = true;
            clickPosition = e.GetPosition(canvas.Child);
            draggableControl.CaptureMouse();
        }

        private void PointButton_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isDragging = false;
            var draggable = sender as CornerButton;
            UpdateCanvas();
            draggable.ReleaseMouseCapture();
        }

        private void PointButton_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var draggable = sender as CornerButton;
            if (isDragging && draggable != null)
            {
                Point currentPosition = e.GetPosition(canvas.Child);
                var transform = draggable.RenderTransform as TranslateTransform ?? new TranslateTransform();
                transform.X = originTT.X + (currentPosition.X - clickPosition.X);
                transform.Y = originTT.Y + (currentPosition.Y - clickPosition.Y);
                draggable.Ebx.Position.x += transform.X;
                draggable.Ebx.Position.y += transform.Y;
                UpdateCanvas();
                draggable.RenderTransform = new TranslateTransform(transform.X, transform.Y);
            }
        }

        private void PointButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var draggableControl = sender as CornerButton;
            originTT = draggableControl.RenderTransform as TranslateTransform ?? new TranslateTransform();
            isDragging = true;
            clickPosition = e.GetPosition(canvas.Child);
            draggableControl.CaptureMouse();
        }

        private void PointButton_MouseRightButtonDown(object sender, RoutedEventArgs e)
        {
            pg.SetClass(((CornerButton)sender).Ebx);
            UpdateCanvas();
        }
    }

    public class CornerButton : Button
    {
        public dynamic Ebx { get; set; }
    }
}
