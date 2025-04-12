using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Imaging;

namespace Summer
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            SetTitleBarArea();
            SwitchAppTheme();
            SwitchAppHand();
            LoadAppVersion();

            CommonShadow.Receivers.Add(ShadowReceiverGrid);

            App.OnWindowSizeChanged += OnWindowSizeChanged;

            RegisterClosingConfirm();
        }

        #region 应用程序相关

        /// <summary>
        /// 保存提示对话框是否已经打开
        /// </summary>
        private bool _exitConfirmDialogShowing = false;

        /// <summary>
        /// 是否有墨迹没有保存
        /// </summary>
        private bool _someInkNotSaved = false;

        /// <summary>
        /// 添加关闭应用程序的保存提示
        /// </summary>
        private void RegisterClosingConfirm()
        {
            try
            {
                var resourceLoader = ResourceLoader.GetForCurrentView();

                Windows.UI.Core.Preview.SystemNavigationManagerPreview.GetForCurrentView().CloseRequested +=
                    async (sender, args) =>
                    {
                        bool isCanvasEmpty = SketchCanvas.InkPresenter.StrokeContainer.GetStrokes().Count <= 0;
                        if (_someInkNotSaved && !isCanvasEmpty)
                        {
                            args.Handled = true;
                            if (!_exitConfirmDialogShowing)
                            {
                                _exitConfirmDialogShowing = true;

                                var result = await new ContentDialog
                                {
                                    XamlRoot = this.XamlRoot,
                                    RequestedTheme = this.ActualTheme,
                                    Title = resourceLoader.GetString("SaveConfirmTitle"),
                                    Content = resourceLoader.GetString("SaveConfirmContent"),
                                    PrimaryButtonText = resourceLoader.GetString("ConfirmSaveButton"),
                                    SecondaryButtonText = resourceLoader.GetString("DonotSaveButton"),
                                    CloseButtonText = resourceLoader.GetString("CancelSaveButton"),
                                    DefaultButton = ContentDialogButton.Close,
                                    Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                                }.ShowAsync();

                                if (result == ContentDialogResult.Primary)
                                {
                                    SaveSketchToFile();
                                }
                                else if (result == ContentDialogResult.Secondary)
                                {
                                    //Application.Current.Exit();
                                    await ApplicationView.GetForCurrentView().TryConsolidateAsync();
                                }

                                _exitConfirmDialogShowing = false;
                            }
                        }
                    };
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
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
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                    }
                };
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
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
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 切换应用程序的左右手
        /// </summary>
        private void SwitchAppHand()
        {
            try
            {
                bool isRightHand = _appSettings.HandModeIndex != 1;

                if (isRightHand)
                {
                    AppTitleLogo.HorizontalAlignment = HorizontalAlignment.Right;
                    AppBottomBar.HorizontalAlignment = HorizontalAlignment.Left;
                    Grid.SetColumn(RightFeatureBar, 2);
                    Grid.SetColumn(LeftFeatureBar, 0);
                }
                else
                {
                    AppTitleLogo.HorizontalAlignment = HorizontalAlignment.Left;
                    AppBottomBar.HorizontalAlignment = HorizontalAlignment.Right;
                    Grid.SetColumn(RightFeatureBar, 0);
                    Grid.SetColumn(LeftFeatureBar, 2);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                SketchCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen;

                // 默认选择圆珠笔，并根据主题切换黑白墨水
                if (SketchToolbar.GetToolButton(InkToolbarTool.BallpointPen) is InkToolbarBallpointPenButton ballpointPen)
                {
                    ballpointPen.SelectedBrushIndex = _appSettings.AppearanceIndex == 1 ? 1 : 0;
                    SketchToolbar.ActiveTool = ballpointPen;
                }

                UpdateCanvasSize(true);

                SketchCanvas.InkPresenter.StrokesCollected += OnCollectedStrokes;
                SketchCanvas.InkPresenter.StrokesErased += OnErasedStrokes;

                SketchScrollViewer.RegisterPropertyChangedCallback(ScrollViewer.ZoomFactorProperty, (o, d) =>
                {
                    try
                    {
                        CanvasZoomFactorTextBlock.Text = ((SketchScrollViewer.ZoomFactor * 100)).ToString("f0");
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void BackgroundGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                UpdateCanvasSize(false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
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
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        #endregion

        #region 画布

        /// <summary>
        /// 是否开启形状识别
        /// </summary>
        private bool _shapesRecognitionEnabled = false;

        /// <summary>
        /// 形状识别器
        /// </summary>
        private readonly InkAnalyzer _shapesAnalyzer = new InkAnalyzer();

        private readonly InkStrokeBuilder _strokeBuilder = new InkStrokeBuilder();

        /// <summary>
        /// 每次绘画完成，标记为未保存，并分析形状
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void OnCollectedStrokes(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            try
            {
                _someInkNotSaved = true;

                if (!_shapesRecognitionEnabled)
                {
                    return;
                }

                await Task.Delay(600);

                if (args.Strokes.Count > 0)
                {
                    _shapesAnalyzer.AddDataForStrokes(args.Strokes);

                    var resultShape = await _shapesAnalyzer.AnalyzeAsync();

                    if (resultShape.Status == InkAnalysisStatus.Updated)
                    {
                        var drawings = _shapesAnalyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing);

                        foreach (var drawing in drawings)
                        {
                            var shape = (InkAnalysisInkDrawing)drawing;

                            if (shape.DrawingKind != InkAnalysisDrawingKind.Drawing)
                            {
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

                            _shapesAnalyzer.RemoveDataForStrokes(shape.GetStrokeIds());
                        }

                        SketchCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 每次擦除绘画，标记为未保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnErasedStrokes(InkPresenter sender, InkStrokesErasedEventArgs args)
        {
            try
            {
                _someInkNotSaved = true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 清除所有墨迹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickClear(object sender, RoutedEventArgs e)
        {
            SketchCanvas?.InkPresenter?.StrokeContainer?.Clear();
        }

        /// <summary>
        /// 绘制多边形
        /// </summary>
        /// <param name="shape"></param>
        private void DrawPolygon(InkAnalysisInkDrawing shape)
        {
            try
            {
                var strokes = SketchCanvas.InkPresenter.StrokeContainer.GetStrokes();
                System.Numerics.Matrix3x2 matr = strokes[0].PointTransform;
                List<InkPoint> inkPointsList = new List<InkPoint>();
                foreach (var item in shape.Points)
                {
                    var intpoint = new InkPoint(new Point(item.X, item.Y), 0.5f);
                    inkPointsList.Add(intpoint);
                }

                for (int i = 0; i < inkPointsList.Count; i++)
                {
                    List<InkPoint> lineInkPoints = new List<InkPoint>
                    {
                        inkPointsList[i],
                        inkPointsList[(i + 1) % inkPointsList.Count]
                    };

                    InkStroke stroke = _strokeBuilder.CreateStrokeFromInkPoints(lineInkPoints, matr);
                    stroke.DrawingAttributes = SketchCanvas.InkPresenter.CopyDefaultDrawingAttributes();

                    SketchCanvas.InkPresenter.StrokeContainer.AddStroke(stroke);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// 绘制圆形
        /// </summary>
        /// <param name="shape"></param>
        private void DrawEllipse(InkAnalysisInkDrawing shape)
        {
            try
            {
                var (center, a, b, rotation) = CalculateEllipseParameters(shape.Points.ToArray());

                var points = GenerateEllipsePoints(center, a, b, rotation);

                InkStroke stroke = _strokeBuilder.CreateStroke(points);
                stroke.PointTransform = Matrix3x2.Identity;
                stroke.DrawingAttributes = SketchCanvas.InkPresenter.CopyDefaultDrawingAttributes();

                SketchCanvas.InkPresenter.StrokeContainer.AddStroke(stroke);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// 椭圆参数计算核心方法
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private (Point center, double a, double b, double rotation) CalculateEllipseParameters(Point[] points)
        {
            // 寻找最长轴
            var maxDistance = 0.0;
            Point p1 = points[0], p2 = points[1];

            foreach (var pair in Combinations(points))
            {
                var dist = Distance(pair.Item1, pair.Item2);
                if (dist > maxDistance)
                {
                    maxDistance = dist;
                    p1 = pair.Item1;
                    p2 = pair.Item2;
                }
            }

            // 计算中心点
            var center = new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);

            // 计算长半轴和旋转角度
            var a = maxDistance / 2;
            var rotation = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);

            // 计算短半轴（使用投影法）
            var b = 0.0;
            foreach (var p in points.Except(new[] { p1, p2 }))
            {
                // 坐标旋转补偿
                var translatedX = p.X - center.X;
                var translatedY = p.Y - center.Y;
                var rotatedX = translatedX * Math.Cos(-rotation) - translatedY * Math.Sin(-rotation);
                var rotatedY = translatedX * Math.Sin(-rotation) + translatedY * Math.Cos(-rotation);

                var currentB = Math.Abs(rotatedY);
                if (currentB > b) b = currentB;
            }

            return (center, a, b, rotation);
        }

        /// <summary>
        /// 生成椭圆点
        /// </summary>
        /// <param name="center"></param>
        /// <param name="a">长半轴（椭圆长轴的一半长度）</param>
        /// <param name="b">短半轴（椭圆短轴的一半长度）</param>
        /// <param name="rotation"></param>
        /// <param name="segments">采样分段数</param>
        /// <returns></returns>
        private List<Point> GenerateEllipsePoints(Point center, double a, double b, double rotation, int segments = 72)
        {
            var points = new List<Point>();

            for (int i = 0; i <= segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;

                // 标准椭圆坐标
                var x = a * Math.Cos(angle);
                var y = b * Math.Sin(angle);

                // 应用旋转
                var rotatedX = x * Math.Cos(rotation) - y * Math.Sin(rotation);
                var rotatedY = x * Math.Sin(rotation) + y * Math.Cos(rotation);

                // 平移至中心点
                points.Add(new Point(center.X + rotatedX, center.Y + rotatedY));
            }

            // 强制闭合
            points.Add(points[0]);
            return points;
        }

        /// <summary>
        /// 辅助方法：两点距离计算
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private double Distance(Point a, Point b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

        /// <summary>
        /// 辅助方法：生成所有两两组合
        /// </summary>
        /// <param name="points"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        private IEnumerable<Tuple<Point, Point>> Combinations(Point[] points)
        {
            for (int i = 0; i < points.Length; i++)
                for (int j = i + 1; j < points.Length; j++)
                    yield return Tuple.Create(points[i], points[j]);
        }

        #endregion

        #region 功能栏

        /// <summary>
        /// 启用手指触摸绘画
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCheckDrawWithHand(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ToggleButton)
                {
                    SketchCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Touch;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 关闭手指绘画
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUncheckDrawWithHand(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ToggleButton)
                {
                    SketchCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 开启形状识别
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCheckShapeRec(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ToggleButton)
                {
                    _shapesRecognitionEnabled = true;
                    _shapesAnalyzer.ClearDataForAllStrokes();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 关闭形状识别
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUncheckShapeRec(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ToggleButton)
                {
                    _shapesRecognitionEnabled = false;
                    _shapesAnalyzer.ClearDataForAllStrokes();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 开启底图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnCheckPictureBackground(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");

                Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(stream);
                        PictureBackgroundImage.Source = bitmapImage;
                        PictureBackgroundImage.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    if (sender is ToggleButton toggleButton)
                    {
                        toggleButton.IsChecked = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 关闭底图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUncheckPictureBackground(object sender, RoutedEventArgs e)
        {
            try
            {
                PictureBackgroundImage.Visibility = Visibility.Collapsed;
                PictureBackgroundImage.Source = null;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 保存到文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickSave(object sender, RoutedEventArgs e)
        {
            SaveSketchToFile();
        }

        /// <summary>
        /// 将草图保存到图片文件
        /// </summary>
        private async void SaveSketchToFile()
        {
            try
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

                    _someInkNotSaved = false;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 重置画布缩放
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickResetCanvasZoom(object sender, RoutedEventArgs e)
        {
            float currentZoom = SketchScrollViewer.ZoomFactor;

            double viewportCenterHorizontalOffsetRatio = SketchScrollViewer.ScrollableWidth <= 0 ? 0.5 : (SketchScrollViewer.HorizontalOffset + (SketchScrollViewer.ViewportWidth / 2)) / SketchScrollViewer.ExtentWidth;
            double horizontalOffset = (1.0 * SketchScrollViewer.ExtentWidth / currentZoom * viewportCenterHorizontalOffsetRatio) - (SketchScrollViewer.ViewportWidth / 2);

            double viewportCenterVerticalOffsetRatio = SketchScrollViewer.ScrollableHeight <= 0 ? 0.5 : (SketchScrollViewer.VerticalOffset + (SketchScrollViewer.ViewportHeight / 2)) / SketchScrollViewer.ExtentHeight;
            double verticalOffset = (1.0 * SketchScrollViewer.ExtentHeight / currentZoom * viewportCenterVerticalOffsetRatio) - (SketchScrollViewer.ViewportHeight / 2);

            SketchScrollViewer.ChangeView(horizontalOffset, verticalOffset, 1.0f);
        }

        /// <summary>
        /// 缩小画布缩放
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickCanvasZoomOut(object sender, RoutedEventArgs e)
        {
            float currentZoom = SketchScrollViewer.ZoomFactor;
            float zoom = Math.Max(1, (currentZoom - 0.5f));

            double viewportCenterHorizontalOffsetRatio = SketchScrollViewer.ScrollableWidth <= 0 ? 0.5 : (SketchScrollViewer.HorizontalOffset + (SketchScrollViewer.ViewportWidth / 2)) / SketchScrollViewer.ExtentWidth;
            double horizontalOffset = (zoom * SketchScrollViewer.ExtentWidth / currentZoom * viewportCenterHorizontalOffsetRatio) - (SketchScrollViewer.ViewportWidth / 2);

            double viewportCenterVerticalOffsetRatio = SketchScrollViewer.ScrollableHeight <= 0 ? 0.5 : (SketchScrollViewer.VerticalOffset + (SketchScrollViewer.ViewportHeight / 2)) / SketchScrollViewer.ExtentHeight;
            double verticalOffset = (zoom * SketchScrollViewer.ExtentHeight / currentZoom * viewportCenterVerticalOffsetRatio) - (SketchScrollViewer.ViewportHeight / 2);

            SketchScrollViewer.ChangeView(horizontalOffset, verticalOffset, zoom);
        }

        /// <summary>
        /// 增大画布缩放
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickCanvasZoomIn(object sender, RoutedEventArgs e)
        {
            float currentZoom = SketchScrollViewer.ZoomFactor;
            float zoom = Math.Min(5, (currentZoom + 0.5f));

            double viewportCenterHorizontalOffsetRatio = SketchScrollViewer.ScrollableWidth <= 0 ? 0.5 : (SketchScrollViewer.HorizontalOffset + (SketchScrollViewer.ViewportWidth / 2)) / SketchScrollViewer.ExtentWidth;
            double horizontalOffset = (zoom * SketchScrollViewer.ExtentWidth / currentZoom * viewportCenterHorizontalOffsetRatio) - (SketchScrollViewer.ViewportWidth / 2);

            double viewportCenterVerticalOffsetRatio = SketchScrollViewer.ScrollableHeight <= 0 ? 0.5 : (SketchScrollViewer.VerticalOffset + (SketchScrollViewer.ViewportHeight / 2)) / SketchScrollViewer.ExtentHeight;
            double verticalOffset = (zoom * SketchScrollViewer.ExtentHeight / currentZoom * viewportCenterVerticalOffsetRatio) - (SketchScrollViewer.ViewportHeight / 2);

            SketchScrollViewer.ChangeView(horizontalOffset, verticalOffset, zoom);
        }

        #endregion

        #region 设置栏

        /// <summary>
        /// 应用程序窗口尺寸变化，更新全屏按钮状态
        /// </summary>
        private void OnWindowSizeChanged()
        {
            FullscreenButton.IsChecked = ApplicationView.GetForCurrentView().IsFullScreenMode;
        }

        /// <summary>
        /// 全屏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCheckFullscreen(object sender, RoutedEventArgs e)
        {
            var view = ApplicationView.GetForCurrentView();
            if (!view.IsFullScreenMode)
            {
                view.TryEnterFullScreenMode();
                VisualStateManager.GoToState(this, "FullScreenState", false);
            }
        }

        /// <summary>
        /// 退出全屏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUncheckFullscreen(object sender, RoutedEventArgs e)
        {
            var view = ApplicationView.GetForCurrentView();
            if (view.IsFullScreenMode)
            {
                view.ExitFullScreenMode();
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
                VisualStateManager.GoToState(this, "NormalState", false);
            }
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
                VisualStateManager.GoToState(this, "PiPState", false);
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
            VisualStateManager.GoToState(this, "NormalState", false);
        }

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
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 点击关闭设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickCloseSettings(object sender, RoutedEventArgs e)
        {
            try
            {
                SettingsTeachingTip.IsOpen = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        #endregion

        #region 设置

        /// <summary>
        /// 设置
        /// </summary>
        private readonly SettingsService _appSettings = SettingsService.Instance;

        /// <summary>
        /// 应用程序版本号
        /// </summary>
        private string _appVersion = string.Empty;

        /// <summary>
        /// 点击切换主题
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SwitchAppTheme();
            SettingsTeachingTip.IsOpen = false;
        }

        /// <summary>
        /// 点击切换左右手模式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHandModeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SwitchAppHand();
            SettingsTeachingTip.IsOpen = false;
        }

        /// <summary>
        /// 获取应用程序的版本号
        /// </summary>
        /// <returns></returns>
        private void LoadAppVersion()
        {
            try
            {
                Package package = Package.Current;
                PackageId packageId = package.Id;
                PackageVersion version = packageId.Version;
                _appVersion = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        #endregion

    }
}
