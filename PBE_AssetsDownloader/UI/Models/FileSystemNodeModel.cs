using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace PBE_AssetsDownloader.UI.Models
{
    public class FileSystemNodeModel : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public string Extension { get; set; }
        public ObservableCollection<FileSystemNodeModel> Children { get; set; }

        public FileSystemNodeModel(string path)
        {
            FullPath = path;
            Name = Path.GetFileName(path);
            IsDirectory = Directory.Exists(path);
            Extension = Path.GetExtension(path).ToLowerInvariant();
            Children = new ObservableCollection<FileSystemNodeModel>();

            if (IsDirectory)
            {
                LoadChildren();
            }
        }

        private void LoadChildren()
        {
            try
            {
                var directories = Directory.GetDirectories(FullPath).Select(dir => new FileSystemNodeModel(dir));
                foreach (var dir in directories)
                {
                    Children.Add(dir);
                }

                var files = Directory.GetFiles(FullPath).Select(file => new FileSystemNodeModel(file));
                foreach (var file in files)
                {
                    Children.Add(file);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore folders we can't access
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}