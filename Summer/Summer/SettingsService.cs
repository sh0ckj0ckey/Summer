using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Storage;

namespace Summer
{
    public class SettingsService : ObservableObject
    {
        private static Lazy<SettingsService> _lazyVM = new Lazy<SettingsService>(() => new SettingsService());
        public static SettingsService Instance => _lazyVM.Value;

        private const string SETTING_NAME_APPEARANCEINDEX = "AppearanceIndex";
        private const string SETTING_NAME_CANVASINDEX = "CanvasIndex";
        private const string SETTING_NAME_HANDMODEINDEX = "HandModeIndex";

        private ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        private int _appearanceIndex = -1;

        private int _canvasIndex = -1;

        private int _handModeIndex = -1;

        /// <summary>
        /// 设置的应用程序的主题 0-Light 1-Dark
        /// </summary>
        public int AppearanceIndex
        {
            get
            {
                try
                {
                    if (_appearanceIndex < 0)
                    {
                        if (_localSettings.Values[SETTING_NAME_APPEARANCEINDEX] == null)
                        {
                            _appearanceIndex = 0;
                        }
                        else if (_localSettings.Values[SETTING_NAME_APPEARANCEINDEX]?.ToString() == "0")
                        {
                            _appearanceIndex = 0;
                        }
                        else if (_localSettings.Values[SETTING_NAME_APPEARANCEINDEX]?.ToString() == "1")
                        {
                            _appearanceIndex = 1;
                        }
                        else
                        {
                            _appearanceIndex = 0;
                        }
                    }
                }
                catch { }
                if (_appearanceIndex < 0) _appearanceIndex = 0;
                return _appearanceIndex < 0 ? 0 : _appearanceIndex;
            }
            set
            {
                SetProperty(ref _appearanceIndex, value);
                ApplicationData.Current.LocalSettings.Values[SETTING_NAME_APPEARANCEINDEX] = _appearanceIndex;
            }
        }

        /// <summary>
        /// 设置画板底图纹理 0-纸质 1-纯色 2-透明
        /// </summary>
        public int CanvasIndex
        {
            get
            {
                try
                {
                    if (_canvasIndex < 0)
                    {
                        if (_localSettings.Values[SETTING_NAME_CANVASINDEX] == null)
                        {
                            _canvasIndex = 0;
                        }
                        else if (_localSettings.Values[SETTING_NAME_CANVASINDEX]?.ToString() == "0")
                        {
                            _canvasIndex = 0;
                        }
                        else if (_localSettings.Values[SETTING_NAME_CANVASINDEX]?.ToString() == "1")
                        {
                            _canvasIndex = 1;
                        }
                        else if (_localSettings.Values[SETTING_NAME_CANVASINDEX]?.ToString() == "2")
                        {
                            _canvasIndex = 2;
                        }
                        else
                        {
                            _canvasIndex = 0;
                        }
                    }
                }
                catch { }
                if (_canvasIndex < 0) _canvasIndex = 0;
                return _canvasIndex < 0 ? 0 : _canvasIndex;
            }
            set
            {
                SetProperty(ref _canvasIndex, value);
                ApplicationData.Current.LocalSettings.Values[SETTING_NAME_CANVASINDEX] = _canvasIndex;
            }
        }

        /// <summary>
        /// 设置的应用程序的左右手模式 0-右手 1-左手
        /// </summary>
        public int HandModeIndex
        {
            get
            {
                try
                {
                    if (_handModeIndex < 0)
                    {
                        if (_localSettings.Values[SETTING_NAME_HANDMODEINDEX] == null)
                        {
                            _handModeIndex = 0;
                        }
                        else if (_localSettings.Values[SETTING_NAME_HANDMODEINDEX]?.ToString() == "0")
                        {
                            _handModeIndex = 0;
                        }
                        else if (_localSettings.Values[SETTING_NAME_HANDMODEINDEX]?.ToString() == "1")
                        {
                            _handModeIndex = 1;
                        }
                        else
                        {
                            _handModeIndex = 0;
                        }
                    }
                }
                catch { }
                if (_handModeIndex < 0) _handModeIndex = 0;
                return _handModeIndex < 0 ? 0 : _handModeIndex;
            }
            set
            {
                SetProperty(ref _handModeIndex, value);
                ApplicationData.Current.LocalSettings.Values[SETTING_NAME_HANDMODEINDEX] = _handModeIndex;
            }
        }
    }
}
