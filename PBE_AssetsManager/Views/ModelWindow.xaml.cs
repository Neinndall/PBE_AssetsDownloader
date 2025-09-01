using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using LeagueToolkit.Core.Animation;
using LeagueToolkit.Core.Mesh;
using Microsoft.Win32;
using System.Diagnostics;
using System.Linq;
using PBE_AssetsManager.Views.Camera;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Views.Models;

namespace PBE_AssetsManager.Views
{
    public partial class ModelWindow : UserControl
    {
        private readonly ModelLoadingService _modelLoadingService;
        private readonly LogService _logService;
        private readonly CustomCameraController _cameraController;
        private SceneModel _sceneModel;

        private readonly Dictionary<string, IAnimationAsset> _animations = new();
        private readonly ObservableCollection<string> _animationNames = new();
        private RigResource _skeleton;

        public ModelWindow(ModelLoadingService modelLoadingService, LogService logService)
        {
            InitializeComponent();
            _modelLoadingService = modelLoadingService;
            _logService = logService;
            _cameraController = new CustomCameraController(ViewportControl.Viewport);

            ViewportControl.LogService = _logService;

            SetupScene();

            PanelControl.AnimationsListBoxControl.ItemsSource = _animationNames;
            PanelControl.AnimationFileLoaded += PanelControl_AnimationFileLoaded;
            PanelControl.ModelFileLoaded += PanelControl_ModelFileLoaded;
        }

        private void SetupScene()
        {
            var ground = SceneElements.CreateGroundPlane(path => _modelLoadingService.LoadTexture(path), _logService.LogError);
            var sky = SceneElements.CreateSidePlanes(path => _modelLoadingService.LoadTexture(path), _logService.LogError);

            ViewportControl.Viewport.Children.Add(ground);
            ViewportControl.Viewport.Children.Add(sky);
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "3D Model Files (*.skn, *.skl)|*.skn;*.skl|All Files|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var extension = Path.GetExtension(openFileDialog.FileName).ToLower();
                if (extension == ".skl")
                {
                    using (var stream = File.OpenRead(openFileDialog.FileName))
                    {
                        _skeleton = new RigResource(stream);
                        ViewportControl.SetSkeleton(_skeleton);
                    }
                    _logService.Log($"Loaded skeleton: {Path.GetFileName(openFileDialog.FileName)}");
                }
                else if (extension == ".skn")
                {
                    string sklFilePath = Path.ChangeExtension(openFileDialog.FileName, ".skl");
                    if (File.Exists(sklFilePath))
                    {
                        using (var stream = File.OpenRead(sklFilePath))
                        {
                            _skeleton = new RigResource(stream);
                            ViewportControl.SetSkeleton(_skeleton);
                        }
                    }
                    
                    _sceneModel = _modelLoadingService.LoadModel(openFileDialog.FileName);
                    if (_sceneModel != null)
                    {
                        EmptyStatePanel.Visibility = Visibility.Collapsed;
                        MainContentGrid.Visibility = Visibility.Visible;
                        ViewportControl.SetModel(_sceneModel);
                        PanelControl.MeshesListBoxControl.ItemsSource = _sceneModel.Parts;
                        
                        ViewportControl.ResetCamera();
                    }
                }
            }
        }

        private void PanelControl_AnimationFileLoaded(object sender, string filePath)
        {
            LoadAnimation(filePath);
        }

        private void PanelControl_ModelFileLoaded(object sender, string filePath)
        {
            LoadModel(filePath);
        }

        private void LoadModel(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            if (extension == ".skn")
            {
                string sklFilePath = Path.ChangeExtension(filePath, ".skl");
                if (File.Exists(sklFilePath))
                {
                    using (var stream = File.OpenRead(sklFilePath))
                    {
                        _skeleton = new RigResource(stream);
                        ViewportControl.SetSkeleton(_skeleton);
                    }
                }
                
                _sceneModel = _modelLoadingService.LoadModel(filePath);
                if (_sceneModel != null)
                {
                    ViewportControl.SetModel(_sceneModel);
                    PanelControl.MeshesListBoxControl.ItemsSource = _sceneModel.Parts;
                    
                    ViewportControl.ResetCamera();
                }
            }
        }

        private void LoadAnimation(string filePath)
        {
            if (_skeleton == null)
            {
                MessageBox.Show("Please load a skeleton (.skl) file first.", "Missing Skeleton", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            using (var stream = File.OpenRead(filePath))
            {
                var animationAsset = AnimationAsset.Load(stream);
                var animationName = Path.GetFileNameWithoutExtension(filePath);

                if (!_animations.ContainsKey(animationName))
                {
                    _animations[animationName] = animationAsset;
                    _animationNames.Add(animationName);
                }
            }
            _logService.Log($"Loaded animation: {Path.GetFileName(filePath)}");
        }

        private void AnimationsListBox_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<string> e)
        {
            var selectedAnimationName = e.NewValue;
            if (_animations.TryGetValue(selectedAnimationName, out var animationAsset))
            {
                _logService.Log($"Animation selected: {selectedAnimationName}");
                ViewportControl.SetAnimation(animationAsset);
            }
        }
    }
}
