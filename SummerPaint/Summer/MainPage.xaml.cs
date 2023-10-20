using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Controls;
using Summer.Views;
using Windows.ApplicationModel;
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
        private int _sketchPageIndex = 1;

        private string _appVersion = string.Empty;
        private SettingsService _appSettings = new SettingsService();

        public MainPage()
        {
            this.InitializeComponent();
            SetTitleBarArea();
            SwitchAppTheme();
            _appVersion = GetAppVersion();
        }

        private void TabView_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TabView tabView)
            {
                var newItem = CreateNewTab();
                tabView.TabItems.Add(newItem);
                tabView.SelectedItem = newItem;
            }
        }

        private void TabView_AddButtonClick(TabView sender, object args)
        {
            var newItem = CreateNewTab();
            sender.TabItems.Add(newItem);
            sender.SelectedItem = newItem;
        }

        private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            sender.TabItems.Remove(args.Tab);
        }

        private void SketchesTabView_TabItemsChanged(TabView sender, IVectorChangedEventArgs args)
        {
            SketchPlaceholderImage.Opacity = (sender?.TabItems?.Count ?? 0) > 0 ? 0 : 0.1;
        }

        private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SwitchAppTheme();
        }

        private TabViewItem CreateNewTab()
        {
            TabViewItem newItem = new TabViewItem();
            newItem.Header = $"Sketch {_sketchPageIndex}";
            _sketchPageIndex++;
            newItem.IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource() { Symbol = Symbol.Pictures };
            Frame frame = new Frame();
            frame.Navigate(typeof(SketchPage));
            newItem.Content = frame;
            return newItem;
        }

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

        private void OnClickSettings(object sender, RoutedEventArgs e)
        {
            try
            {
                SettingsTeachingTip.IsOpen = true;
            }
            catch { }
        }
    }
}
