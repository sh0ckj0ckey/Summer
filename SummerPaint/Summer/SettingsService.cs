using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Storage;

namespace Summer
{
    public class SettingsService : ObservableObject
    {
        private const string SETTING_NAME_APPEARANCEINDEX = "AppearanceIndex";
        private const string SETTING_NAME_ENABLE_DRAW_WITH_HAND = "EnableDrawWithHand";
        private const string SETTING_NAME_ENABLE_SHAPE_REC = "EnableShapeRecog";

        private ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        // 设置的应用程序的主题 0-Light 1-Dark
        private int _appearanceIndex = -1;
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

        // 是否手指绘画
        private bool? _enableDrawWithHand = null;
        public bool EnableDrawWithHand
        {
            get
            {
                try
                {
                    if (_enableDrawWithHand is null)
                    {
                        if (_localSettings.Values[SETTING_NAME_ENABLE_SHAPE_REC] == null)
                        {
                            _enableDrawWithHand = true;
                        }
                        else if (_localSettings.Values[SETTING_NAME_ENABLE_SHAPE_REC]?.ToString() == "True")
                        {
                            _enableDrawWithHand = true;
                        }
                        else
                        {
                            _enableDrawWithHand = false;
                        }
                    }
                }
                catch { }
                if (_enableDrawWithHand is null) _enableDrawWithHand = true;
                return _enableDrawWithHand != false;
            }
            set
            {
                SetProperty(ref _enableDrawWithHand, value);
                ApplicationData.Current.LocalSettings.Values[SETTING_NAME_ENABLE_SHAPE_REC] = _enableDrawWithHand;
            }
        }

        // 是否识别形状
        private bool? _enableShapeRecog = null;
        public bool EnableShapeRecog
        {
            get
            {
                try
                {
                    if (_enableShapeRecog is null)
                    {
                        if (_localSettings.Values[SETTING_NAME_ENABLE_SHAPE_REC] == null)
                        {
                            _enableShapeRecog = true;
                        }
                        else if (_localSettings.Values[SETTING_NAME_ENABLE_SHAPE_REC]?.ToString() == "True")
                        {
                            _enableShapeRecog = true;
                        }
                        else
                        {
                            _enableShapeRecog = false;
                        }
                    }
                }
                catch { }
                if (_enableShapeRecog is null) _enableShapeRecog = true;
                return _enableShapeRecog != false;
            }
            set
            {
                SetProperty(ref _enableShapeRecog, value);
                ApplicationData.Current.LocalSettings.Values[SETTING_NAME_ENABLE_SHAPE_REC] = _enableShapeRecog;
            }
        }
    }
}
