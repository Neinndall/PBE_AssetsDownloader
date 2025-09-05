using PBE_AssetsManager.Utils;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsManager.Views.Settings
{
    public partial class AdvancedSettingsView : UserControl
    {
        private AppSettings _settings;
        private bool _isLoaded = false;

        public AdvancedSettingsView()
        {
            InitializeComponent();
            Loaded += AdvancedSettingsView_Loaded;
        }

        private void AdvancedSettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            _settings = AppSettings.LoadSettings();
            
            IntervalUnitComboBox.ItemsSource = new string[] { "Minutes", "Hours", "Days" };

            EnableAssetTrackerCheckBox.IsChecked = _settings.CheckAssetUpdates;

            LoadIntervalSettings();

            _isLoaded = true;
        }

        private void LoadIntervalSettings()
        {
            int totalMinutes = _settings.AssetTrackerFrequency;

            if (totalMinutes <= 0)
            {
                IntervalValueTextBox.Text = "0";
                IntervalUnitComboBox.SelectedItem = "Minutes";
                return;
            }

            if (totalMinutes % 1440 == 0) // 60 * 24
            {
                IntervalValueTextBox.Text = (totalMinutes / 1440).ToString();
                IntervalUnitComboBox.SelectedItem = "Days";
            }
            else if (totalMinutes % 60 == 0)
            {
                IntervalValueTextBox.Text = (totalMinutes / 60).ToString();
                IntervalUnitComboBox.SelectedItem = "Hours";
            }
            else
            {
                IntervalValueTextBox.Text = totalMinutes.ToString();
                IntervalUnitComboBox.SelectedItem = "Minutes";
            }
        }

        private void SaveInterval()
        {
            if (!_isLoaded) return;

            if (!int.TryParse(IntervalValueTextBox.Text, out int value) || value < 0)
            {
                return;
            }

            string selectedUnit = IntervalUnitComboBox.SelectedItem as string;
            if (selectedUnit == null) return;

            int totalMinutes = 0;
            switch (selectedUnit)
            {
                case "Days":
                    totalMinutes = value * 1440;
                    break;
                case "Hours":
                    totalMinutes = value * 60;
                    break;
                case "Minutes":
                    totalMinutes = value;
                    break;
            }

            _settings.AssetTrackerFrequency = totalMinutes;
            AppSettings.SaveSettings(_settings);
        }

        private void Interval_SettingChanged(object sender, RoutedEventArgs e)
        {
            SaveInterval();
        }

        private void EnableAssetTracker_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            _settings.CheckAssetUpdates = EnableAssetTrackerCheckBox.IsChecked ?? false;
            AppSettings.SaveSettings(_settings);
        }
    }
}