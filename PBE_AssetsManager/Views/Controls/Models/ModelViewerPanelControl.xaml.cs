using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PBE_AssetsManager.Views.Models;

namespace PBE_AssetsManager.Views.Controls.Models
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

        public ListBox MeshesListBoxControl => MeshesListBox;
        public ListBox AnimationsListBoxControl => AnimationsListBox;
        public ListBox ModelsListBoxControl => ModelsListBox;

        public ModelViewerPanelControl()
        {
            InitializeComponent();
            AnimationsListBox.SelectionChanged += AnimationsListBox_SelectionChanged;
        }

        private void AnimationsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AnimationsListBox.SelectedItem is string selectedAnimationName)
            {
                AnimationSelected?.Invoke(this, selectedAnimationName);
            }
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
            var openFileDialog = new OpenFileDialog
            {
                Filter = "SKN files (*.skn)|*.skn|All files (*.*)|*.*",
                Title = "Select a SKN File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ModelFileLoaded?.Invoke(this, openFileDialog.FileName);
            }
        }

        private void LoadAnimationButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Animation files (*.anm)|*.anm|All files (*.*)|*.*",
                Title = "Select an Animation File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                AnimationFileLoaded?.Invoke(this, openFileDialog.FileName);
            }
        }
    }
}