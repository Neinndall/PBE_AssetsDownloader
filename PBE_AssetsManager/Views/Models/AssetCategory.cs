using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PBE_AssetsManager.Views.Models
{
    public class AssetCategory : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string BaseUrl { get; set; }
        public string Extension { get; set; }
        public long Start { get; set; }
        public long LastValid { get; set; }
        public List<long> FoundUrls { get; set; } = new List<long>();
        public List<long> FailedUrls { get; set; } = new List<long>();
        public Dictionary<long, string> FoundUrlOverrides { get; set; } = new Dictionary<long, string>();

        private bool _hasNewAssets;
        public bool HasNewAssets
        {
            get => _hasNewAssets;
            set
            {
                _hasNewAssets = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
