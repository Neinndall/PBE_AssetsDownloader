using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace PBE_AssetsManager.Views.Models
{
    public enum NodeType { RealDirectory, RealFile, WadFile, VirtualDirectory, VirtualFile }

    public class FileSystemNodeModel : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public NodeType Type { get; set; }
        public string FullPath { get; set; } // Real path for RealDirectory/WadFile/RealFile, Virtual path for Virtual items
        public ObservableCollection<FileSystemNodeModel> Children { get; set; }

        // --- Data for WADs and Chunks ---
        public string SourceWadPath { get; set; } // Only for VirtualFile/VirtualDirectory
        public ulong SourceChunkPathHash { get; set; } // Only for VirtualFile

        public string Extension => (Type == NodeType.RealDirectory || Type == NodeType.VirtualDirectory) ? "" : Path.GetExtension(FullPath).ToLowerInvariant();

        private bool _isExpanded;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged(nameof(IsVisible));
                }
            }
        }

        private string _preMatch;
        public string PreMatch
        {
            get { return _preMatch; }
            set
            {
                if (_preMatch != value)
                {
                    _preMatch = value;
                    OnPropertyChanged(nameof(PreMatch));
                }
            }
        }

        private string _match;
        public string Match
        {
            get { return _match; }
            set
            {
                if (_match != value)
                {
                    _match = value;
                    OnPropertyChanged(nameof(Match));
                }
            }
        }

        private string _postMatch;
        public string PostMatch
        {
            get { return _postMatch; }
            set
            {
                if (_postMatch != value)
                {
                    _postMatch = value;
                    OnPropertyChanged(nameof(PostMatch));
                }
            }
        }

        private bool _hasMatch;
        public bool HasMatch
        {
            get { return _hasMatch; }
            set
            {
                if (_hasMatch != value)
                {
                    _hasMatch = value;
                    OnPropertyChanged(nameof(HasMatch));
                }
            }
        }

        // Constructor for real files/directories
        public FileSystemNodeModel(string path)
        {
            FullPath = path;
            Name = Path.GetFileName(path);
            Children = new ObservableCollection<FileSystemNodeModel>();

            if (Directory.Exists(path))
            {
                Type = NodeType.RealDirectory;
                Children.Add(new FileSystemNodeModel()); // Add dummy child for lazy loading
            }
            else
            {
                string lowerPath = path.ToLowerInvariant();
                if (lowerPath.EndsWith(".wad") || lowerPath.EndsWith(".wad.client"))
                {
                    Type = NodeType.WadFile;
                    Children.Add(new FileSystemNodeModel()); // Add dummy child for lazy loading
                }
                else
                {
                    Type = NodeType.RealFile; // It's a real file on the filesystem
                }
            }
        }

        // Constructor for virtual nodes inside a WAD
        public FileSystemNodeModel(string name, bool isDirectory, string virtualPath, string sourceWad)
        {
            Name = name;
            FullPath = virtualPath;
            SourceWadPath = sourceWad;
            Type = isDirectory ? NodeType.VirtualDirectory : NodeType.VirtualFile;
            Children = new ObservableCollection<FileSystemNodeModel>();
        }

        // Private constructor for the dummy node
        private FileSystemNodeModel() 
        {
            Name = "Loading...";
            Children = new ObservableCollection<FileSystemNodeModel>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}