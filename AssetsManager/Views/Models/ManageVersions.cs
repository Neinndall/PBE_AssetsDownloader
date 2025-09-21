using AssetsManager.Services.Core;
using AssetsManager.Services.Versions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AssetsManager.Views.Models
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

        public ObservableCollection<LocaleOption> AvailableLocales { get; set; }

        public ManageVersions(VersionService versionService, LogService logService)
        {
            _versionService = versionService;
            _logService = logService;
            LeagueClientVersions = new ObservableCollection<VersionFileInfo>();
            LoLGameClientVersions = new ObservableCollection<VersionFileInfo>();
            AvailableLocales = new ObservableCollection<LocaleOption>
            {
                new LocaleOption { Code = "es_ES", IsSelected = false },
                new LocaleOption { Code = "es_MX", IsSelected = false },
                new LocaleOption { Code = "en_US", IsSelected = false },
                new LocaleOption { Code = "tr_TR", IsSelected = false }
            };
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

        public void DeleteVersions(IEnumerable<VersionFileInfo> versionsToDelete)
        {
            if (versionsToDelete == null || !versionsToDelete.Any()) return;

            if (_versionService.DeleteVersionFiles(versionsToDelete))
            {
                foreach (var versionFile in versionsToDelete.ToList()) // ToList() to avoid issues with modifying collection while iterating
                {
                    if (LeagueClientVersions.Contains(versionFile))
                    {
                        LeagueClientVersions.Remove(versionFile);
                    }
                    else if (LoLGameClientVersions.Contains(versionFile))
                    {
                        LoLGameClientVersions.Remove(versionFile);
                    }
                }
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
