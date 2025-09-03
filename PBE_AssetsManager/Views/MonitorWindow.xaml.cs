using PBE_AssetsManager.Services;
using PBE_AssetsManager.Utils;
using System;
using System.Windows.Controls;
using PBE_AssetsManager.Views.Controls.Monitor;

namespace PBE_AssetsManager.Views
{
    public partial class MonitorWindow : UserControl
    {
        public MonitorWindow(
            MonitorService monitorService, 
            IServiceProvider serviceProvider, 
            DiffViewService diffViewService, 
            AppSettings appSettings, 
            LogService logService, 
            CustomMessageBoxService customMessageBoxService,
            JsonDataService jsonDataService)
        {
            InitializeComponent();
            
            // The main DataContext is the window itself.
            DataContext = this;

            // Inject all necessary dependencies into the FileWatcherControl
            FileWatcherControl.MonitorService = monitorService;
            FileWatcherControl.ServiceProvider = serviceProvider;
            FileWatcherControl.DiffViewService = diffViewService;
            FileWatcherControl.JsonDataService = jsonDataService;
            FileWatcherControl.AppSettings = appSettings;
            FileWatcherControl.LogService = logService;
            FileWatcherControl.CustomMessageBoxService = customMessageBoxService;
            FileWatcherControl.InitializeData();

            // Setup and inject dependencies for HistoryViewControl
            HistoryViewControl.AppSettings = appSettings;
            HistoryViewControl.LogService = logService;
            HistoryViewControl.CustomMessageBoxService = customMessageBoxService;
            HistoryViewControl.DiffViewService = diffViewService;
            HistoryViewControl.LoadHistory();
        }
    }
}