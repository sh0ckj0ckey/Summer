using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Storage;

namespace Summer.Helpers
{
    public partial class SettingsService : ObservableObject
    {
        private const string Settings_Appearance = "AppearanceIndex";
        private const string Settings_HandednessMode = "HandednessMode";

        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        public event EventHandler<int>? AppearanceSettingChanged = null;

        public event EventHandler<int>? HandednessModeSettingsChanged = null;

        private int _appearance = -1;

        private int _handednessMode = -1;

        public int Appearance
        {
            get
            {
                try
                {
                    if (_appearance < 0)
                    {
                        if (_localSettings.Values[Settings_Appearance] is null)
                        {
                            _appearance = 0;
                        }
                        else if (_localSettings.Values[Settings_Appearance].ToString() == "0")
                        {
                            _appearance = 0;
                        }
                        else if (_localSettings.Values[Settings_Appearance].ToString() == "1")
                        {
                            _appearance = 1;
                        }
                        else
                        {
                            _appearance = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex);
                }

                return _appearance;
            }
            set
            {
                SetProperty(ref _appearance, value);
                _localSettings.Values[Settings_Appearance] = _appearance;
                this.AppearanceSettingChanged?.Invoke(this, _appearance);
            }
        }

        public int HandednessMode
        {
            get
            {
                try
                {
                    if (_handednessMode < 0)
                    {
                        if (_localSettings.Values[Settings_HandednessMode] is null)
                        {
                            _handednessMode = 0;
                        }
                        else if (_localSettings.Values[Settings_HandednessMode].ToString() == "0")
                        {
                            _handednessMode = 0;
                        }
                        else if (_localSettings.Values[Settings_HandednessMode].ToString() == "1")
                        {
                            _handednessMode = 1;
                        }
                        else
                        {
                            _handednessMode = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex);
                }

                return _handednessMode;
            }
            set
            {
                SetProperty(ref _handednessMode, value);
                _localSettings.Values[Settings_HandednessMode] = _handednessMode;
                this.HandednessModeSettingsChanged?.Invoke(this, _handednessMode);
            }
        }
    }
}
