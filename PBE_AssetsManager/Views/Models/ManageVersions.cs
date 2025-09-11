using PBE_AssetsManager.Services.Core;
using PBE_AssetsManager.Services.Versions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace PBE_AssetsManager.Views.Models
{
    public class ManageVersions : INotifyPropertyChanged
    {
        private readonly VersionService _versionService;
        private readonly LogService _logService;

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

        private ObservableCollection<VersionFileInfo> _loLGameClientVersions;
        public ObservableCollection<VersionFileInfo> LoLGameClientVersions
        {
            get { return _loLGameClientVersions; }
            set
            {
                _loLGameClientVersions = value;
                OnPropertyChanged(nameof(LoLGameClientVersions));
            }
        }

        private bool _isEsEsSelected = true;
        public bool IsEsEsSelected
        {
            get { return _isEsEsSelected; }
            set
            {
                _isEsEsSelected = value;
                OnPropertyChanged(nameof(IsEsEsSelected));
            }
        }

        private bool _isEsMxSelected = true;
        public bool IsEsMxSelected
        {
            get { return _isEsMxSelected; }
            set
            {
                _isEsMxSelected = value;
                OnPropertyChanged(nameof(IsEsMxSelected));
            }
        }

        private bool _isEnUsSelected = true;
        public bool IsEnUsSelected
        {
            get { return _isEnUsSelected; }
            set
            {
                _isEnUsSelected = value;
                OnPropertyChanged(nameof(IsEnUsSelected));
            }
        }

        public ManageVersions(VersionService versionService, LogService logService)
        {
            _versionService = versionService;
            _logService = logService;
            LeagueClientVersions = new ObservableCollection<VersionFileInfo>();
            LoLGameClientVersions = new ObservableCollection<VersionFileInfo>();
        }

        public async Task LoadVersionFilesAsync()
        {
            if (_versionService != null)
            {
                var allFiles = await _versionService.GetVersionFilesAsync();

                LeagueClientVersions.Clear();
                LoLGameClientVersions.Clear();

                foreach (var file in allFiles.Where(f => f.Category == "league-client"))
                {
                    LeagueClientVersions.Add(file);
                }

                var gameClientCategories = new[] { "lol-game-client" }; // Can be expanded with more categories
                foreach (var file in allFiles.Where(f => gameClientCategories.Contains(f.Category)))
                {
                    LoLGameClientVersions.Add(file);
                }
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
