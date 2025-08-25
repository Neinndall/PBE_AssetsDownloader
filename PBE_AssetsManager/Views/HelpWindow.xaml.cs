using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PBE_AssetsManager.Views.Help;

namespace PBE_AssetsManager.Views
{
    public partial class HelpWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;

        public HelpWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;

            SetupNavigation();
            // Load initial view
            NavigateToView(_serviceProvider.GetRequiredService<AboutView>());
        }

        private void SetupNavigation()
        {
            NavAbout.Checked += (s, e) => NavigateToView(_serviceProvider.GetRequiredService<AboutView>());
            NavChangelogs.Checked += (s, e) => NavigateToView(_serviceProvider.GetRequiredService<ChangelogsView>());
            NavBugsReport.Checked += (s, e) => NavigateToView(_serviceProvider.GetRequiredService<BugReportsView>());
            NavUpdates.Checked += (s, e) => NavigateToView(_serviceProvider.GetRequiredService<UpdatesView>());
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
