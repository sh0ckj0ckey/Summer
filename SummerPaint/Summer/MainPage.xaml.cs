using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace Summer
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool _recShapes = false;
        InkAnalyzer analyzerShape = new InkAnalyzer();
        IReadOnlyList<InkStroke> strokesShape = null;
        InkAnalysisResult resultShape = null;

        public MainPage()
        {
            this.InitializeComponent();
            SetTitleBarArea();
            SwitchAppTheme();
            _appVersion = GetAppVersion();

            CommonShadow.Receivers.Add(ShadowReceiverGrid);
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
                SketchScrollViewer.Height = CanvasGrid.ActualHeight;
                SketchScrollViewer.Width = CanvasGrid.ActualWidth;

                if (SketchGrid.Height < CanvasGrid.ActualHeight || forceUpdateCavas)
                {
                    SketchGrid.Height = CanvasGrid.ActualHeight;
                }
                if (SketchGrid.Width < CanvasGrid.ActualWidth || forceUpdateCavas)
                {
                    SketchGrid.Width = CanvasGrid.ActualWidth;
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
            // ShapesCanvas.Children.Add(ellipse);
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
            // ShapesCanvas.Children.Add(polygon);
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

        /// <summary>
        /// 设置应用程序的标题栏区域
        /// </summary>
        private void SetTitleBarArea()
        {
            try
            {
                var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
                coreTitleBar.ExtendViewIntoTitleBar = true;

                // 设置为可拖动区域
                Window.Current.SetTitleBar(AppTitleBar);

                var titleBar = ApplicationView.GetForCurrentView().TitleBar;

                titleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;
                titleBar.ButtonInactiveForegroundColor = Windows.UI.Colors.Gray;

                // 当窗口激活状态改变时，注册一个handler
                Window.Current.Activated += (s, e) =>
                {
                    try
                    {
                        if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
                            AppTitleLogo.Opacity = 0.7;
                        else
                            AppTitleLogo.Opacity = 1.0;
                    }
                    catch { }
                };
            }
            catch { }
        }

        /// <summary>
        /// 切换应用程序的主题
        /// </summary>
        private void SwitchAppTheme()
        {
            try
            {
                // 设置标题栏颜色
                bool isLight = _appSettings.AppearanceIndex == 0;

                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

                if (isLight)
                {
                    titleBar.ButtonForegroundColor = Colors.Black;
                    titleBar.ButtonHoverForegroundColor = Colors.Black;
                    titleBar.ButtonPressedForegroundColor = Colors.Black;
                    titleBar.ButtonHoverBackgroundColor = new Color() { A = 8, R = 0, G = 0, B = 0 };
                    titleBar.ButtonPressedBackgroundColor = new Color() { A = 16, R = 0, G = 0, B = 0 };
                }
                else
                {
                    titleBar.ButtonForegroundColor = Colors.White;
                    titleBar.ButtonHoverForegroundColor = Colors.White;
                    titleBar.ButtonPressedForegroundColor = Colors.White;
                    titleBar.ButtonHoverBackgroundColor = new Color() { A = 16, R = 255, G = 255, B = 255 };
                    titleBar.ButtonPressedBackgroundColor = new Color() { A = 24, R = 255, G = 255, B = 255 };
                }

                // 设置应用程序颜色
                if (Window.Current.Content is FrameworkElement rootElement)
                {
                    if (_appSettings.AppearanceIndex == 1)
                    {
                        rootElement.RequestedTheme = ElementTheme.Dark;
                    }
                    else
                    {
                        rootElement.RequestedTheme = ElementTheme.Light;
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 小窗置顶
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnCheckTopmost(object sender, RoutedEventArgs e)
        {
            if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay))
            {
                ViewModePreferences compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                compactOptions.CustomSize = new Windows.Foundation.Size(960, 740);
                _ = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, compactOptions);
            }
        }

        /// <summary>
        /// 取消小窗置顶
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnUncheckTopmost(object sender, RoutedEventArgs e)
        {
            _ = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
        }

        #region Settings

        // 应用程序版本号
        private string _appVersion = string.Empty;

        // 设置
        private SettingsService _appSettings = SettingsService.Instance;

        /// <summary>
        /// 点击打开设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickSettings(object sender, RoutedEventArgs e)
        {
            try
            {
                SettingsTeachingTip.IsOpen = true;
            }
            catch { }
        }

        /// <summary>
        /// 点击切换主题
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SwitchAppTheme();
        }

        /// <summary>
        /// 获取应用程序的版本号
        /// </summary>
        /// <returns></returns>
        private string GetAppVersion()
        {
            try
            {
                Package package = Package.Current;
                PackageId packageId = package.Id;
                PackageVersion version = packageId.Version;
                return string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
            }
            catch (Exception) { }
            return "";
        }

        #endregion

        private void OnClickFullscreen(object sender, RoutedEventArgs e)
        {

        }

        private void OnClickImportPicture(object sender, RoutedEventArgs e)
        {

        }

        private void OnClickAbout(object sender, RoutedEventArgs e)
        {

        }
    }
}
