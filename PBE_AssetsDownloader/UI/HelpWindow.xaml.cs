using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Material.Icons;
using PBE_AssetsDownloader.Services;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.UI.Views.HelpViews;

namespace PBE_AssetsDownloader.UI
{
    public partial class HelpWindow : Window
    {
        private readonly LogService _logService;

        private readonly AboutView _aboutView;
        private readonly ChangelogsView _changelogsView;
        private readonly BugReportsView _bugReportsView;
        private readonly UpdatesView _updatesView;

        public HelpWindow(LogService logService)
        {
            InitializeComponent();
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));

            _aboutView = new AboutView();
            _changelogsView = new ChangelogsView();
            _bugReportsView = new BugReportsView();
            _updatesView = new UpdatesView();

            NavAbout.Checked += (s, e) => NavigateToView(_aboutView);
            NavChangelogs.Checked += (s, e) => NavigateToView(_changelogsView);
            NavBugsReport.Checked += (s, e) => NavigateToView(_bugReportsView);
            NavUpdates.Checked += (s, e) => NavigateToView(_updatesView);

            // Load initial view
            NavigateToView(_aboutView);

            // Initialize views that need dependencies
            _changelogsView.ApplySettingsToUI(_logService);
        }

        private void NavigateToView(object view)
        {
            HelpContentArea.Content = view;
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}