using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using AssetsManager.Views.Models;

namespace AssetsManager.Views.Controls.Models
{
    /// <summary>
    /// Interaction logic for ModelViewerPanelControl.xaml
    /// </summary>
    public partial class ModelViewerPanelControl : UserControl
    {
        public event EventHandler<string> AnimationFileLoaded;
        public event EventHandler<string> ModelFileLoaded;
        public event EventHandler<string> AnimationSelected;
        public event EventHandler<SceneModel> ModelDeleted;
        public event EventHandler AnimationStopRequested;

        public ListBox MeshesListBoxControl => MeshesListBox;
        public ListBox AnimationsListBoxControl => AnimationsListBox;
        public ListBox ModelsListBoxControl => ModelsListBox;

        public ModelViewerPanelControl()
        {
            InitializeComponent();
        }

        private void DeleteModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SceneModel modelToDelete)
            {
                ModelDeleted?.Invoke(this, modelToDelete);
            }
        }

        private void LoadModelButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new CommonOpenFileDialog
            {
                Filters = { new CommonFileDialogFilter("SKN files", "*.skn"), new CommonFileDialogFilter("All files", "*.*") },
                Title = "Select a SKN File"
            };

            if (openFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ModelFileLoaded?.Invoke(this, openFileDialog.FileName);
            }
        }

        private void LoadAnimationButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new CommonOpenFileDialog
            {
                Filters = { new CommonFileDialogFilter("Animation files", "*.anm"), new CommonFileDialogFilter("All files", "*.*") },
                Title = "Select Animation Files",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                foreach (string fileName in openFileDialog.FileNames)
                {
                    AnimationFileLoaded?.Invoke(this, fileName);
                }
            }
        }
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string animationName)
            {
                AnimationsListBox.SelectedItem = animationName;
                AnimationSelected?.Invoke(this, animationName);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            AnimationStopRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}