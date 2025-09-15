using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AssetsManager.Services.Explorer;

namespace AssetsManager.Views.Models
{
    public class FilePreviewerViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<PinnedFileViewModel> PinnedFiles { get; set; }

        private PinnedFileViewModel _selectedFile;
        public PinnedFileViewModel SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (_selectedFile != value)
                {
                    if (_selectedFile != null) _selectedFile.IsSelected = false;
                    _selectedFile = value;
                    if (_selectedFile != null) _selectedFile.IsSelected = true;
                    OnPropertyChanged(nameof(SelectedFile));
                }
            }
        }

        public FilePreviewerViewModel()
        {
            PinnedFiles = new ObservableCollection<PinnedFileViewModel>();
        }

        public void PinFile(FileSystemNodeModel node)
        {
            if (node == null || PinnedFiles.Any(x => x.Node == node)) return;

            var newPinnedFile = new PinnedFileViewModel(node);
            PinnedFiles.Add(newPinnedFile);
            SelectedFile = newPinnedFile;
        }

        public void UnpinFile(PinnedFileViewModel fileToUnpin)
        {
            if (fileToUnpin == null) return;

            int index = PinnedFiles.IndexOf(fileToUnpin);
            if (index != -1)
            {
                PinnedFiles.RemoveAt(index);
                if (SelectedFile == fileToUnpin)
                {
                    SelectedFile = PinnedFiles.Count > 0 ? PinnedFiles[System.Math.Max(0, index - 1)] : null;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
