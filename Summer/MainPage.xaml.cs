using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Imaging;

namespace Summer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a <see cref="Frame">.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public Helpers.SettingsService AppSettings { get; } = new Helpers.SettingsService();

        public string AppVersion { get; }

        private readonly InkStrokeBuilder _strokeBuilder = new();

        private readonly Helpers.InkShapeRecognizer _shapeRecognizer = new();

        private readonly List<InkStroke> _bufferedShapeStrokes = [];

        private readonly DispatcherTimer _shapeRecognitionTimer = new();

        private bool _shapeRecognitionEnabled = false;

        private bool _isShapeRecognitionRunning = false;

        private bool _hasUnsavedChanges = false;

        private ContentDialog? _closingContentDialog = null;

        public MainPage()
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

            InitializeComponent();

            this.Loaded += (_, _) =>
            {
                Window.Current.SetTitleBar(TitleBarGrid);

                UpdateAppTheme();
                UpdateAppHandednessMode();

                InitializeInk();

                CommonShadow.Receivers.Add(ShadowReceiverGrid);

                UpdateSketchCanvasSize(true);
            };

            this.AppSettings.AppearanceSettingChanged += (_, _) =>
            {
                UpdateAppTheme();
            };

            this.AppSettings.HandednessModeSettingsChanged += (_, _) =>
            {
                UpdateAppHandednessMode();
            };

            ListenWindowSizeChanged();
            ListenWindowActivated();
            ListenWindowCloseRequested();
            ListenCanvasZoomed();
            ListenCanvasSizeChanged();
            ListenInkChanged();
            ListenInkStrokesCollected();
            ListenInkStrokeStarted();

            InitializeShapeRecognizerTimer();

            this.AppVersion = $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}";
        }

        private void ListenWindowSizeChanged()
        {
            Window.Current.CoreWindow.SizeChanged += (_, _) =>
            {
                var view = ApplicationView.GetForCurrentView();
                FullscreenButton?.IsChecked = view.IsFullScreenMode;
            };
        }

        private void ListenWindowActivated()
        {
            Window.Current.Activated += (_, e) =>
            {
                LogoStackPanel?.Opacity = e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated ? 0.7 : 1.0;
            };
        }

        private void ListenWindowCloseRequested()
        {
            Windows.UI.Core.Preview.SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += async (_, args) =>
            {
                var resourceLoader = ResourceLoader.GetForCurrentView();
                bool isCanvasEmpty = SketchCanvas.InkPresenter.StrokeContainer.GetStrokes().Count <= 0;
                if (_hasUnsavedChanges && !isCanvasEmpty)
                {
                    args.Handled = true;

                    if (_closingContentDialog is not null)
                    {
                        return;
                    }

                    _closingContentDialog = new ContentDialog()
                    {
                        XamlRoot = this.XamlRoot,
                        RequestedTheme = this.ActualTheme,
                        Title = resourceLoader.GetString("SaveConfirmTitle"),
                        Content = resourceLoader.GetString("SaveConfirmContent"),
                        PrimaryButtonText = resourceLoader.GetString("ConfirmSaveButtonContent"),
                        SecondaryButtonText = resourceLoader.GetString("DoNotSaveButtonContent"),
                        CloseButtonText = resourceLoader.GetString("CancelSaveButtonContent"),
                        DefaultButton = ContentDialogButton.Close,
                        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                    };

                    try
                    {
                        var result = await _closingContentDialog.ShowAsync();

                        if (result == ContentDialogResult.Primary)
                        {
                            bool saved = await SaveSketchToFileAsync();
                            if (saved)
                            {
                                await ApplicationView.GetForCurrentView().TryConsolidateAsync();
                            }
                        }
                        else if (result == ContentDialogResult.Secondary)
                        {
                            await ApplicationView.GetForCurrentView().TryConsolidateAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine(ex.Message);
                    }
                    finally
                    {
                        _closingContentDialog = null;
                    }
                }
            };
        }

        private void ListenCanvasZoomed()
        {
            SketchCanvasScrollViewer.RegisterPropertyChangedCallback(ScrollViewer.ZoomFactorProperty, (o, d) =>
            {
                try
                {
                    CanvasZoomFactorTextBlock.Text = ((SketchCanvasScrollViewer.ZoomFactor * 100)).ToString("f0");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                }
            });
        }

        private void ListenCanvasSizeChanged()
        {
            CanvasGrid.SizeChanged += (_, _) => UpdateSketchCanvasSize();
        }

        private void ListenInkChanged()
        {
            SketchCanvas.InkPresenter.StrokesCollected += (_, _) =>
            {
                _hasUnsavedChanges = true;
            };
            SketchCanvas.InkPresenter.StrokesErased += (_, _) =>
            {
                _hasUnsavedChanges = true;
            };
        }

        private void ListenInkStrokesCollected()
        {
            SketchCanvas.InkPresenter.StrokesCollected += (_, args) =>
            {
                try
                {
                    if (!_shapeRecognitionEnabled)
                    {
                        return;
                    }

                    if (args.Strokes.Count <= 0)
                    {
                        return;
                    }

                    _shapeRecognitionTimer.Stop();

                    _bufferedShapeStrokes.AddRange(args.Strokes);

                    _shapeRecognitionTimer.Start();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                }
            };
        }

        private void ListenInkStrokeStarted()
        {
            SketchCanvas.InkPresenter.StrokeInput.StrokeStarted += (_, _) =>
            {
                try
                {
                    if (!_shapeRecognitionEnabled)
                    {
                        return;
                    }

                    _shapeRecognitionTimer.Stop();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                }
            };
        }

        private void InitializeInk()
        {
            try
            {
                SketchCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen;

                var drawingAttributes = SketchCanvas.InkPresenter.CopyDefaultDrawingAttributes();
                drawingAttributes.IgnoreTilt = false;
                drawingAttributes.IgnorePressure = false;
                drawingAttributes.FitToCurve = true;
                SketchCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);

                if (SketchToolbar.GetToolButton(InkToolbarTool.BallpointPen) is InkToolbarBallpointPenButton ballpointPen)
                {
                    ballpointPen.SelectedBrushIndex = this.AppSettings.Appearance == 1 ? 1 : 0;
                    SketchToolbar.ActiveTool = ballpointPen;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
        }

        private void InitializeShapeRecognizerTimer()
        {
            _shapeRecognitionTimer.Interval = TimeSpan.FromSeconds(1);
            _shapeRecognitionTimer.Tick += async (_, _) =>
            {
                try
                {
                    _shapeRecognitionTimer.Stop();

                    if (!_shapeRecognitionEnabled)
                    {
                        return;
                    }

                    if (_isShapeRecognitionRunning)
                    {
                        _shapeRecognitionTimer.Start();
                        return;
                    }

                    if (_bufferedShapeStrokes.Count <= 0)
                    {
                        return;
                    }

                    _isShapeRecognitionRunning = true;

                    try
                    {
                        var strokesToAnalyze = new List<InkStroke>(_bufferedShapeStrokes);
                        _bufferedShapeStrokes.Clear();

                        var results = await _shapeRecognizer.AnalyzeAsync(strokesToAnalyze);

                        foreach (var result in results)
                        {
                            ReplaceRecognizedShape(result);
                        }
                    }
                    finally
                    {
                        _isShapeRecognitionRunning = false;
                    }
                }
                catch (Exception ex)
                {
                    _bufferedShapeStrokes.Clear();
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                }
            };
        }

        private void UpdateAppTheme()
        {
            try
            {
                bool isLightTheme = this.AppSettings.Appearance == 0;

                var titleBar = ApplicationView.GetForCurrentView().TitleBar;

                titleBar.BackgroundColor = Colors.Transparent;
                titleBar.InactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonHoverBackgroundColor = isLightTheme ? Windows.UI.Color.FromArgb(10, 0, 0, 0) : Windows.UI.Color.FromArgb(16, 255, 255, 255);
                titleBar.ButtonPressedBackgroundColor = isLightTheme ? Windows.UI.Color.FromArgb(08, 0, 0, 0) : Windows.UI.Color.FromArgb(10, 255, 255, 255);
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

                titleBar.ForegroundColor = isLightTheme ? Colors.Black : Colors.White;
                titleBar.InactiveForegroundColor = Colors.Gray;
                titleBar.ButtonForegroundColor = isLightTheme ? Colors.Black : Colors.White;
                titleBar.ButtonHoverForegroundColor = isLightTheme ? Colors.Black : Colors.White;
                titleBar.ButtonPressedForegroundColor = isLightTheme ? Colors.Black : Colors.White;
                titleBar.ButtonInactiveForegroundColor = Colors.Gray;

                if (Window.Current.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = this.AppSettings.Appearance == 1 ? ElementTheme.Dark : ElementTheme.Light;
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Failed to UpdateAppTheme: {ex}");
            }
        }

        private void UpdateAppHandednessMode()
        {
            try
            {
                if (this.AppSettings.HandednessMode != 1)
                {
                    _ = VisualStateManager.GoToState(this, "RightHandState", false);
                }
                else
                {
                    _ = VisualStateManager.GoToState(this, "LeftHandState", false);
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Failed to UpdateAppHandednessMode: {ex}");
            }
        }

        private void UpdateSketchCanvasSize(bool force = false)
        {
            try
            {
                double viewportWidth = CanvasGrid.ActualWidth;
                double viewportHeight = CanvasGrid.ActualHeight;

                if (viewportWidth <= 0 || viewportHeight <= 0)
                {
                    return;
                }

                if (force || SketchCanvasGrid.Width < viewportWidth)
                {
                    SketchCanvasGrid.Width = viewportWidth;
                }

                if (force || SketchCanvasGrid.Height < viewportHeight)
                {
                    SketchCanvasGrid.Height = viewportHeight;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Failed to UpdateSketchCanvasSize: {ex}");
            }
        }

        private async Task<bool> SaveSketchToFileAsync()
        {
            try
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker
                {
                    SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary
                };

                savePicker.FileTypeChoices.Add("PNG", [".png"]);
                savePicker.SuggestedFileName = "Summer Sketch";

                Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();

                if (file is null)
                {
                    return false;
                }

                CanvasDevice device = CanvasDevice.GetSharedDevice();
                CanvasRenderTarget renderTarget = new(device, (int)Math.Ceiling(SketchCanvasGrid.Width), (int)Math.Ceiling(SketchCanvasGrid.Height), 96);
                using (var ds = renderTarget.CreateDrawingSession())
                {
                    ds.Clear(this.AppSettings.Appearance == 1 ? Color.FromArgb(255, 46, 46, 46) : Colors.White);
                    ds.DrawInk(SketchCanvas.InkPresenter.StrokeContainer.GetStrokes());
                }

                using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                {
                    await renderTarget.SaveAsync(fileStream, CanvasBitmapFileFormat.Png, 1f);
                }

                _hasUnsavedChanges = false;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                return false;
            }
        }

        private void ReplaceRecognizedShape(Helpers.RecognizedShapeResult result)
        {
            try
            {
                if (result is null || result.SourceStrokeIds is null || result.Points is null)
                {
                    return;
                }

                foreach (uint strokeId in result.SourceStrokeIds)
                {
                    var stroke = SketchCanvas.InkPresenter.StrokeContainer.GetStrokeById(strokeId);
                    stroke?.Selected = true;
                }

                if (result.ShapeKind == Helpers.RecognizedShapeKind.Ellipse)
                {
                    if (result.Points.Count > 1)
                    {
                        var stroke = _strokeBuilder.CreateStroke(result.Points);
                        stroke.PointTransform = System.Numerics.Matrix3x2.Identity;
                        stroke.DrawingAttributes = SketchCanvas.InkPresenter.CopyDefaultDrawingAttributes();

                        SketchCanvas.InkPresenter.StrokeContainer.AddStroke(stroke);
                    }
                }
                else if (result.ShapeKind == Helpers.RecognizedShapeKind.Polygon)
                {
                    if (result.Points.Count >= 2)
                    {
                        var drawingAttributes = SketchCanvas.InkPresenter.CopyDefaultDrawingAttributes();

                        for (int i = 0; i < result.Points.Count; i++)
                        {
                            var linePoints = new List<InkPoint>
                            {
                                new(result.Points[i], 0.5f),
                                new(result.Points[(i + 1) % result.Points.Count], 0.5f)
                            };

                            var stroke = _strokeBuilder.CreateStrokeFromInkPoints(linePoints, System.Numerics.Matrix3x2.Identity);
                            stroke.DrawingAttributes = drawingAttributes;

                            SketchCanvas.InkPresenter.StrokeContainer.AddStroke(stroke);
                        }
                    }
                }

                SketchCanvas.InkPresenter.StrokeContainer.DeleteSelected();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
        }

        #region ControlBar

        private void EnterFullscreen()
        {
            var view = ApplicationView.GetForCurrentView();
            if (!view.IsFullScreenMode)
            {
                view.TryEnterFullScreenMode();
                _ = VisualStateManager.GoToState(this, "FullScreenState", false);
            }
        }

        private void ExitFullscreen()
        {
            var view = ApplicationView.GetForCurrentView();
            if (view.IsFullScreenMode)
            {
                view.ExitFullScreenMode();
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
                _ = VisualStateManager.GoToState(this, "NormalState", false);
            }
        }

        private async Task EnterCompactOverlay()
        {
            if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay))
            {
                ViewModePreferences compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                compactOptions.CustomSize = new Windows.Foundation.Size(960, 740);
                bool success = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, compactOptions);
                if (success)
                {
                    _ = VisualStateManager.GoToState(this, "PiPState", false);
                }
            }
        }

        private async Task ExitCompactOverlay()
        {
            bool success = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
            if (success)
            {
                _ = VisualStateManager.GoToState(this, "NormalState", false);
            }
        }

        private void FullscreenButton_Checked(object sender, RoutedEventArgs e)
        {
            EnterFullscreen();
        }

        private void FullscreenButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ExitFullscreen();
        }

        private void TopmostButton_Checked(object sender, RoutedEventArgs e)
        {
            _ = EnterCompactOverlay();
        }

        private void TopmostButton_Unchecked(object sender, RoutedEventArgs e)
        {
            _ = ExitCompactOverlay();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _ = SaveSketchToFileAsync();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsTeachingTip.IsOpen = true;
        }

        private void CloseSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsTeachingTip.IsOpen = false;
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsTeachingTip.IsOpen = false;
        }

        #endregion

        #region FeatureBar

        private void EnableDrawWithHand()
        {
            try
            {
                SketchCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Touch;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
        }

        private void DisableDrawWithHand()
        {
            try
            {
                SketchCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
        }

        private void EnableShapeAnalyzer()
        {
            try
            {
                _shapeRecognitionEnabled = true;
                _shapeRecognizer.Clear();
                _bufferedShapeStrokes.Clear();
                _shapeRecognitionTimer.Stop();
                _isShapeRecognitionRunning = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
        }

        private void DisableShapeAnalyzer()
        {
            try
            {
                _shapeRecognitionEnabled = false;
                _shapeRecognizer.Clear();
                _bufferedShapeStrokes.Clear();
                _shapeRecognitionTimer.Stop();
                _isShapeRecognitionRunning = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
        }

        private async Task<bool> SetPictureBackground()
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker
                {
                    ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                    SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary
                };

                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");

                Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
                if (file is not null)
                {
                    using Windows.Storage.Streams.IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                    var bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(stream);
                    SketchCanvasBackgroundImage.Source = bitmapImage;
                    SketchCanvasBackgroundImage.Visibility = Visibility.Visible;
                }

                return file is not null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                return false;
            }
        }

        private void RemovePictureBackground()
        {
            try
            {
                SketchCanvasBackgroundImage.Visibility = Visibility.Collapsed;
                SketchCanvasBackgroundImage.Source = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
        }

        private void DrawWithHandToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            EnableDrawWithHand();
        }

        private void DrawWithHandToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            DisableDrawWithHand();
        }

        private void ShapeRecognizeToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            EnableShapeAnalyzer();
        }

        private void ShapeRecognizeToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            DisableShapeAnalyzer();
        }

        private async void PictureToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!await SetPictureBackground())
            {
                if (sender is ToggleButton toggleButton)
                {
                    toggleButton.IsChecked = false;
                }
            }
        }

        private void PictureToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            RemovePictureBackground();
        }

        #endregion

        #region StatusBar

        private void ZoomOutCanvas()
        {
            float currentZoom = SketchCanvasScrollViewer.ZoomFactor;
            float zoom = Math.Max(1, (currentZoom - 0.5f));

            double viewportCenterHorizontalOffsetRatio = SketchCanvasScrollViewer.ScrollableWidth <= 0 ? 0.5 : (SketchCanvasScrollViewer.HorizontalOffset + (SketchCanvasScrollViewer.ViewportWidth / 2)) / SketchCanvasScrollViewer.ExtentWidth;
            double horizontalOffset = (zoom * SketchCanvasScrollViewer.ExtentWidth / currentZoom * viewportCenterHorizontalOffsetRatio) - (SketchCanvasScrollViewer.ViewportWidth / 2);

            double viewportCenterVerticalOffsetRatio = SketchCanvasScrollViewer.ScrollableHeight <= 0 ? 0.5 : (SketchCanvasScrollViewer.VerticalOffset + (SketchCanvasScrollViewer.ViewportHeight / 2)) / SketchCanvasScrollViewer.ExtentHeight;
            double verticalOffset = (zoom * SketchCanvasScrollViewer.ExtentHeight / currentZoom * viewportCenterVerticalOffsetRatio) - (SketchCanvasScrollViewer.ViewportHeight / 2);

            SketchCanvasScrollViewer.ChangeView(horizontalOffset, verticalOffset, zoom);
        }

        private void ZoomInCanvas()
        {
            float currentZoom = SketchCanvasScrollViewer.ZoomFactor;
            float zoom = Math.Min(5, (currentZoom + 0.5f));

            double viewportCenterHorizontalOffsetRatio = SketchCanvasScrollViewer.ScrollableWidth <= 0 ? 0.5 : (SketchCanvasScrollViewer.HorizontalOffset + (SketchCanvasScrollViewer.ViewportWidth / 2)) / SketchCanvasScrollViewer.ExtentWidth;
            double horizontalOffset = (zoom * SketchCanvasScrollViewer.ExtentWidth / currentZoom * viewportCenterHorizontalOffsetRatio) - (SketchCanvasScrollViewer.ViewportWidth / 2);

            double viewportCenterVerticalOffsetRatio = SketchCanvasScrollViewer.ScrollableHeight <= 0 ? 0.5 : (SketchCanvasScrollViewer.VerticalOffset + (SketchCanvasScrollViewer.ViewportHeight / 2)) / SketchCanvasScrollViewer.ExtentHeight;
            double verticalOffset = (zoom * SketchCanvasScrollViewer.ExtentHeight / currentZoom * viewportCenterVerticalOffsetRatio) - (SketchCanvasScrollViewer.ViewportHeight / 2);

            SketchCanvasScrollViewer.ChangeView(horizontalOffset, verticalOffset, zoom);
        }

        private void ResetCanvasZoomFactor()
        {
            float currentZoom = SketchCanvasScrollViewer.ZoomFactor;

            double viewportCenterHorizontalOffsetRatio = SketchCanvasScrollViewer.ScrollableWidth <= 0 ? 0.5 : (SketchCanvasScrollViewer.HorizontalOffset + (SketchCanvasScrollViewer.ViewportWidth / 2)) / SketchCanvasScrollViewer.ExtentWidth;
            double horizontalOffset = (1.0 * SketchCanvasScrollViewer.ExtentWidth / currentZoom * viewportCenterHorizontalOffsetRatio) - (SketchCanvasScrollViewer.ViewportWidth / 2);

            double viewportCenterVerticalOffsetRatio = SketchCanvasScrollViewer.ScrollableHeight <= 0 ? 0.5 : (SketchCanvasScrollViewer.VerticalOffset + (SketchCanvasScrollViewer.ViewportHeight / 2)) / SketchCanvasScrollViewer.ExtentHeight;
            double verticalOffset = (1.0 * SketchCanvasScrollViewer.ExtentHeight / currentZoom * viewportCenterVerticalOffsetRatio) - (SketchCanvasScrollViewer.ViewportHeight / 2);

            SketchCanvasScrollViewer.ChangeView(horizontalOffset, verticalOffset, 1.0f);
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomOutCanvas();
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomInCanvas();
        }

        private void ZoomResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetCanvasZoomFactor();
        }

        #endregion
    }
}
