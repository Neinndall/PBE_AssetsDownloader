using PBE_AssetsManager.Services;
using PBE_AssetsManager.Services.Core;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace PBE_AssetsManager.Views.Dialogs
{
    public partial class JsonDiffWindow : Window
    {
        private readonly Storyboard _loadingAnimation;

        public JsonDiffWindow(CustomMessageBoxService customMessageBoxService)
        {
            InitializeComponent();
            JsonDiffControl.CustomMessageBoxService = customMessageBoxService;
            JsonDiffControl.ComparisonFinished += (sender, success) =>
            {
                if (success)
                {
                    Close();
                }
            };

            var originalStoryboard = (Storyboard)this.TryFindResource("SpinningIconAnimation");
            if (originalStoryboard != null)
            {
                _loadingAnimation = originalStoryboard.Clone();
                Storyboard.SetTarget(_loadingAnimation, ProgressIcon);
            }
        }

        public void ShowLoading(bool show)
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (show)
            {
                _loadingAnimation?.Begin();
            }
            else
            {
                _loadingAnimation?.Stop();
            }
        }

        public async Task LoadAndDisplayDiffAsync(string oldText, string newText, string oldFileName, string newFileName)
        {
            await JsonDiffControl.LoadAndDisplayDiffAsync(oldText, newText, oldFileName, newFileName);
            ShowLoading(false);
        }

        public async Task LoadAndDisplayDiffAsync(string oldFilePath, string newFilePath)
        {
            await JsonDiffControl.LoadAndDisplayDiffAsync(oldFilePath, newFilePath);
            ShowLoading(false);
        }
    }
}