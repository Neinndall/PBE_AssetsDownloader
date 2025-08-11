using PBE_AssetsDownloader.Utils;
using System.Windows.Controls;

namespace PBE_AssetsDownloader.UI.Views.Settings
{
    public partial class GeneralSettingsView : UserControl, ISettingsView
    {
        private AppSettings _appSettings;

        public GeneralSettingsView() 
        {
            InitializeComponent();
        }

        public void ApplySettingsToUI(AppSettings appSettings)
        {
            _appSettings = appSettings;

            checkBoxSyncHashes.IsChecked = _appSettings.SyncHashesWithCDTB;
            checkBoxCheckJsonData.IsChecked = _appSettings.CheckJsonDataUpdates;
            checkBoxAutoCopy.IsChecked = _appSettings.AutoCopyHashes;
            checkBoxCreateBackUp.IsChecked = _appSettings.CreateBackUpOldHashes;
            checkBoxOnlyCheckDifferences.IsChecked = _appSettings.OnlyCheckDifferences;
            checkBoxEnableDiffHistory.IsChecked = _appSettings.EnableDiffHistory;
            checkBoxEnableBackgroundUpdates.IsChecked = _appSettings.EnableBackgroundUpdates;
            comboBoxUpdateFrequency.SelectedItem = _appSettings.BackgroundUpdateFrequency;
        }

        public void SaveSettings()
        {
            if (_appSettings == null) return;
            
            _appSettings.SyncHashesWithCDTB = checkBoxSyncHashes.IsChecked ?? false;
            _appSettings.CheckJsonDataUpdates = checkBoxCheckJsonData.IsChecked ?? false;
            _appSettings.AutoCopyHashes = checkBoxAutoCopy.IsChecked ?? false;
            _appSettings.CreateBackUpOldHashes = checkBoxCreateBackUp.IsChecked ?? false;
            _appSettings.OnlyCheckDifferences = checkBoxOnlyCheckDifferences.IsChecked ?? false;
            _appSettings.EnableDiffHistory = checkBoxEnableDiffHistory.IsChecked ?? false;
            _appSettings.EnableBackgroundUpdates = checkBoxEnableBackgroundUpdates.IsChecked ?? false;
            if (comboBoxUpdateFrequency.SelectedItem is ComboBoxItem item)
            {
                _appSettings.BackgroundUpdateFrequency = int.Parse(item.Content.ToString());
            }
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_appSettings != null)
            {
                ApplySettingsToUI(_appSettings);
            }
        }

        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveSettings();
        }
    }
}
