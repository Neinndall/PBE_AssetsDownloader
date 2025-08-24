using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using PBE_AssetsManager.Services;

namespace PBE_AssetsManager.UI.Dialogs
{
    public partial class ProgressDetailsWindow : Window
    {
        private readonly LogService _logService;
        private DateTime _startTime;
        private int _totalFiles;

        public string OperationVerb { get; set; } = "Downloading";
        public string WindowTitle { get; set; } // No default value here
        public string HeaderIconKind { get; set; } = "Download";
        public string HeaderText { get; set; } = "Download Details";

        public ProgressDetailsWindow(LogService logService, string windowTitle) // Add windowTitle parameter
        {
            InitializeComponent();
            _logService = logService;
            _startTime = DateTime.Now;

            this.Title = windowTitle; // Set the window title from parameter
            this.WindowTitle = windowTitle; // Also set the property for consistency
            this.DataContext = this; // Set DataContext for binding

            DetailedLogsRichTextBox.Document.Blocks.Clear(); // Clear any default content
        }

        public void UpdateProgress(int completedFiles, int totalFiles, string currentFileName, bool isSuccess, string errorMessage)
        {
            _totalFiles = totalFiles; // Store total files for estimated time calculation
            ProgressSummaryTextBlock.Text = $"{OperationVerb}: {completedFiles} of {totalFiles}";
            CurrentFileTextBlock.Text = $"Current file: {currentFileName}";
            UpdateEstimatedTime(completedFiles, totalFiles);
        }

        private void UpdateEstimatedTime(int completedFiles, int totalFiles)
        {
            if (completedFiles == 0 || totalFiles == 0)
            {
                EstimatedTimeTextBlock.Text = "Estimated time: Calculating...";
                return;
            }

            TimeSpan elapsed = DateTime.Now - _startTime;
            double progress = (double)completedFiles / totalFiles;

            if (progress > 0)
            {
                TimeSpan estimatedTotalTime = TimeSpan.FromSeconds(elapsed.TotalSeconds / progress);
                TimeSpan estimatedRemainingTime = estimatedTotalTime - elapsed;

                // Ensure remaining time is not negative and use robust formatting
                if (estimatedRemainingTime.TotalSeconds < 0)
                {
                    EstimatedTimeTextBlock.Text = "Estimated time remaining: 00:00:00";
                }
                else
                {
                    EstimatedTimeTextBlock.Text = $"Estimated time remaining: {estimatedRemainingTime.ToString(@"hh\:mm\:ss")}";
                }
            }
            else
            {
                EstimatedTimeTextBlock.Text = "Estimated time: Calculating...";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}