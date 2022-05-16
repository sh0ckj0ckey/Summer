using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

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
        }

        private void SetTitleBar()
        {
            // 设置标题栏样式
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(48, 128, 128, 128);
            titleBar.ButtonPressedBackgroundColor = Color.FromArgb(64, 128, 128, 128);
            titleBar.ButtonForegroundColor = Colors.Black;
            titleBar.ButtonHoverForegroundColor = Colors.Black;
            titleBar.ButtonPressedForegroundColor = Colors.Black;
            titleBar.ButtonInactiveForegroundColor = Colors.Gray;
            titleBar.ForegroundColor = Colors.Black;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                SetTitleBar();

                MainCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Touch;

                var back = new BitmapImage(new System.Uri("ms-appx:///Assets/Background/background.png"));
                BackgroundImage.Source = back;

                UpdateCanvasSize(true);

                InkBarShadow.Receivers.Add(BackgroundImage);
                InkToolBar.Translation += new System.Numerics.Vector3(0, 0, 32);
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
                MainScrollViewer.Height = BackgroundGrid.ActualHeight;
                MainScrollViewer.Width = BackgroundGrid.ActualWidth;

                if (MainGrid.Height < BackgroundGrid.ActualHeight || forceUpdateCavas)
                {
                    MainGrid.Height = BackgroundGrid.ActualHeight;
                }
                if (MainGrid.Width < BackgroundGrid.ActualWidth || forceUpdateCavas)
                {
                    MainGrid.Width = BackgroundGrid.ActualWidth;
                }
            }
            catch { }
        }
    }
}
