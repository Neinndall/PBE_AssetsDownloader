
using PBE_AssetsManager.Services.Core;
using PBE_AssetsManager.Services.Versions;
using PBE_AssetsManager.Views.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PBE_AssetsManager.Views.Controls.Monitor
{
    public partial class ManageVersionsControl : UserControl, INotifyPropertyChanged
    {
        public VersionService VersionService { get; set; }
        public LogService LogService { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<VersionFileInfo> _leagueClientVersions;
        public ObservableCollection<VersionFileInfo> LeagueClientVersions
        {
            get { return _leagueClientVersions; }
            set
            {
                _leagueClientVersions = value;
                OnPropertyChanged(nameof(LeagueClientVersions));
            }
        }

        private ObservableCollection<VersionFileInfo> _otherGameVersions;
        public ObservableCollection<VersionFileInfo> OtherGameVersions
        {
            get { return _otherGameVersions; }
            set
            {
                _otherGameVersions = value;
                OnPropertyChanged(nameof(OtherGameVersions));
            }
        }

        public ManageVersionsControl()
        {
            InitializeComponent();
            this.DataContext = this;
            LeagueClientVersions = new ObservableCollection<VersionFileInfo>();
            OtherGameVersions = new ObservableCollection<VersionFileInfo>();
            this.Loaded += ManageVersionsControl_Loaded;
        }

        private async void ManageVersionsControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadVersionFiles();
        }

        private async void FetchVersions_Click(object sender, RoutedEventArgs e)
        {
            if (VersionService != null && LogService != null)
            {
                LogService.Log("User initiated version fetch.");
                await VersionService.FetchAllVersionsAsync();
                await LoadVersionFiles(); // Refresh the lists after fetching
            }
            else
            {
                MessageBox.Show("Services not initialized.", "Error");
            }
        }

        private async Task LoadVersionFiles()
        {
            if (VersionService != null)
            {
                var allFiles = await VersionService.GetVersionFilesAsync();

                LeagueClientVersions.Clear();
                OtherGameVersions.Clear();

                foreach (var file in allFiles.Where(f => f.Category == "league-client"))
                {
                    LeagueClientVersions.Add(file);
                }

                foreach (var file in allFiles.Where(f => f.Category != "league-client"))
                {
                    OtherGameVersions.Add(file);
                }

                LogService.Log($"Loaded {LeagueClientVersions.Count} League Client versions and {OtherGameVersions.Count} other game versions.");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
