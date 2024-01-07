using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace Summer.Views
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SketchPage : Page
    {
        private bool _recShapes = false;

        InkAnalyzer analyzerShape = new InkAnalyzer();
        IReadOnlyList<InkStroke> strokesShape = null;
        InkAnalysisResult resultShape = null;

        public SketchPage()
        {
            this.InitializeComponent();

            CommonShadow.Receivers.Add(SketchScrollViewer);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                SketchCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen;
                UpdateCanvasSize(true);

                SketchCanvas.InkPresenter.StrokeInput.StrokeEnded += OnStrokeEnded;
            }
            catch { }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                SketchCanvas.InkPresenter.StrokeInput.StrokeEnded -= OnStrokeEnded;
            }
            catch { }
        }

        private void BackgroundGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                UpdateCanvasSize(false);
            }
            catch { }
        }

        private void UpdateCanvasSize(bool forceUpdateCavas)
        {
            try
            {
                SketchScrollViewer.Height = RootGrid.ActualHeight;
                SketchScrollViewer.Width = RootGrid.ActualWidth;

                if (SketchGrid.Height < RootGrid.ActualHeight || forceUpdateCavas)
                {
                    SketchGrid.Height = RootGrid.ActualHeight;
                }
                if (SketchGrid.Width < RootGrid.ActualWidth || forceUpdateCavas)
                {
                    SketchGrid.Width = RootGrid.ActualWidth;
                }
            }
            catch { }
        }

        private void OnCheckDrawWithHand(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton)
            {
                SketchCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Touch;
            }
        }

        private void OnUncheckDrawWithHand(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton)
            {
                SketchCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen;
            }
        }

        private void OnCheckShapeRec(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton)
            {
                _recShapes = true;
            }
        }

        private void OnUncheckShapeRec(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton)
            {
                _recShapes = false;
            }
        }

        private async void OnStrokeEnded(InkStrokeInput sender, PointerEventArgs args)
        {
            await Task.Delay(600);
            if (_recShapes)
            {
                strokesShape = SketchCanvas.InkPresenter.StrokeContainer.GetStrokes();

                if (strokesShape.Count > 0)
                {
                    analyzerShape.AddDataForStrokes(strokesShape);

                    resultShape = await analyzerShape.AnalyzeAsync();

                    if (resultShape.Status == InkAnalysisStatus.Updated)
                    {
                        var drawings = analyzerShape.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing);

                        foreach (var drawing in drawings)
                        {
                            var shape = (InkAnalysisInkDrawing)drawing;
                            if (shape.DrawingKind == InkAnalysisDrawingKind.Drawing)
                            {
                                // Catch and process unsupported shapes (lines and so on) here.
                            }
                            else
                            {
                                // Process recognized shapes here.
                                if (shape.DrawingKind == InkAnalysisDrawingKind.Circle || shape.DrawingKind == InkAnalysisDrawingKind.Ellipse)
                                {
                                    DrawEllipse(shape);
                                }
                                else
                                {
                                    DrawPolygon(shape);
                                }
                                foreach (var strokeId in shape.GetStrokeIds())
                                {
                                    var stroke = SketchCanvas.InkPresenter.StrokeContainer.GetStrokeById(strokeId);
                                    stroke.Selected = true;
                                }
                            }
                            analyzerShape.RemoveDataForStrokes(shape.GetStrokeIds());
                        }
                        SketchCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                    }
                }
            }
        }

        private void DrawEllipse(InkAnalysisInkDrawing shape)
        {
            var points = shape.Points;
            Ellipse ellipse = new Ellipse();
            ellipse.Width = Math.Sqrt((points[0].X - points[2].X) * (points[0].X - points[2].X) +
                 (points[0].Y - points[2].Y) * (points[0].Y - points[2].Y));
            ellipse.Height = Math.Sqrt((points[1].X - points[3].X) * (points[1].X - points[3].X) +
                 (points[1].Y - points[3].Y) * (points[1].Y - points[3].Y));

            var rotAngle = Math.Atan2(points[2].Y - points[0].Y, points[2].X - points[0].X);
            RotateTransform rotateTransform = new RotateTransform();
            rotateTransform.Angle = rotAngle * 180 / Math.PI;
            rotateTransform.CenterX = ellipse.Width / 2.0;
            rotateTransform.CenterY = ellipse.Height / 2.0;

            TranslateTransform translateTransform = new TranslateTransform();
            translateTransform.X = shape.Center.X - ellipse.Width / 2.0;
            translateTransform.Y = shape.Center.Y - ellipse.Height / 2.0;

            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(rotateTransform);
            transformGroup.Children.Add(translateTransform);
            ellipse.RenderTransform = transformGroup;

            var brush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(255, 91, 40, 0));
            ellipse.Stroke = brush;
            ellipse.StrokeThickness = 2;
            ShapesCanvas.Children.Add(ellipse);
        }

        private void DrawPolygon(InkAnalysisInkDrawing shape)
        {
            var points = shape.Points;
            Polygon polygon = new Polygon();

            foreach (var point in points)
            {
                polygon.Points.Add(point);
            }

            var brush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(255, 91, 40, 0));
            polygon.Stroke = brush;
            polygon.StrokeThickness = 2;
            ShapesCanvas.Children.Add(polygon);
        }

        private async void OnClickSave(object sender, RoutedEventArgs e)
        {
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            savePicker.FileTypeChoices.Add("PNG", new List<string>() { ".png" });
            savePicker.SuggestedFileName = "Summer Sketch";

            Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                CanvasDevice device = CanvasDevice.GetSharedDevice();
                CanvasRenderTarget renderTarget = new CanvasRenderTarget(device, (int)SketchCanvas.ActualWidth, (int)SketchCanvas.ActualHeight, 96);
                using (var ds = renderTarget.CreateDrawingSession())
                {
                    ds.Clear(SettingsService.Instance.AppearanceIndex == 1 ? Color.FromArgb(255, 46, 46, 46) : Colors.White);
                    ds.DrawInk(SketchCanvas.InkPresenter.StrokeContainer.GetStrokes());
                }

                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await renderTarget.SaveAsync(fileStream, CanvasBitmapFileFormat.Png, 1f);
                }
            }
        }
    }
}
