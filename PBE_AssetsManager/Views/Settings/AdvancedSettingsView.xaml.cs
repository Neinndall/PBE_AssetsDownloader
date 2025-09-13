using PBE_AssetsManager.Utils;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsManager.Views.Settings
{
    public partial class AdvancedSettingsView : UserControl
    {
        private AppSettings _settings;

        public AdvancedSettingsView()
        {
            InitializeComponent();
            Loaded += AdvancedSettingsView_Loaded;
        }

        private void AdvancedSettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            // The parent window will pass the settings via ApplySettingsToUI
        }

        public void ApplySettingsToUI(AppSettings settings)
        {
            _settings = settings;
            IntervalUnitComboBox.ItemsSource = new string[] { "Minutes", "Hours", "Days" };
            LoadIntervalSettings(); 
        }

        public void SaveSettings()
        {
            if (_settings == null) return;

            _settings.AssetTrackerTimer = EnableAssetTrackerCheckBox.IsChecked ?? false;
            _settings.CheckPbeStatus = EnablePbeStatusCheckBox.IsChecked ?? false;
            
            if (!int.TryParse(IntervalValueTextBox.Text, out int value) || value < 0)
            {
                // On invalid input, perhaps default to a safe value or do nothing.
                // For now, we'll just not save the interval.
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

            if (totalMinutes > 0 && totalMinutes % 1440 == 0)
            {
                IntervalValueTextBox.Text = (totalMinutes / 1440).ToString();
                IntervalUnitComboBox.SelectedItem = "Days";
            }
            else if (totalMinutes > 0 && totalMinutes % 60 == 0)
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
    }
}