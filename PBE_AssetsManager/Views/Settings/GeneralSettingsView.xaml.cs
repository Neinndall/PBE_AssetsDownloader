using System.Linq;
using System.Windows.Controls;
using PBE_AssetsManager.Utils;

namespace PBE_AssetsManager.Views.Settings
{
    public partial class GeneralSettingsView : UserControl
    {
        private AppSettings _appSettings;

        public GeneralSettingsView() 
        {
            InitializeComponent();
        }

        public void ApplySettingsToUI(AppSettings appSettings)
        {
            _appSettings = appSettings;
            // By setting the DataContext to null and then back to the settings object,
            // we force WPF to re-evaluate all the bindings.
            DataContext = null;
            DataContext = _appSettings;
        }

        public void SaveSettings()
        {   
            // Are now throught bindings isCheck
            if (_appSettings == null) return;
        }
    }
}
