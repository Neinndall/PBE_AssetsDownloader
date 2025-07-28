using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using System.Net.Http;
using System.Reflection;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;

using PBE_AssetsDownloader.UI.Controls;
using PBE_AssetsDownloader.Info;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.Services;

namespace PBE_AssetsDownloader.UI
{
    public partial class MainWindow : Window
    {
        private readonly LogService _logService;
        private readonly HttpClient _httpClient;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly Requests _requests;
        private readonly Status _status;
        private readonly AssetDownloader _assetDownloader;
        private readonly AppSettings _appSettings;

        private HomeWindow _homeViewInstance;
        private ExportWindow _exportViewInstance;
                
        public MainWindow(
            LogService logService, 
            HttpClient httpClient, 
            DirectoriesCreator directoriesCreator, 
            Requests requests, 
            Status status, 
            AssetDownloader assetDownloader,
            AppSettings appSettings)
            
        {
            InitializeComponent();
    
            _logService = logService;
            
            // LOG PARA EXCLUSIVAMENTE MAINWINDOW (EXPORT Y HOME)
            _logService.SetLogOutput(LogView.LogRichTextBox, LogView.LogScrollViewerControl);
                        
            _httpClient = httpClient;
            _directoriesCreator = directoriesCreator;
            _requests = requests;
            _status = status; // Asignamos la instancia de Status que ya recibimos
            _assetDownloader = assetDownloader;
            _appSettings = appSettings; // ✅ usar la instancia inyectada

            _homeViewInstance = new HomeWindow(
                _logService,
                _httpClient,
                _directoriesCreator,
                _requests,
                _status,
                _assetDownloader,
                _appSettings
            );

            _exportViewInstance = new ExportWindow(
                _logService,
                _httpClient,
                _directoriesCreator,
                _assetDownloader
            );

            // ✅ NUEVO: Conectar el evento de navegación del Sidebar
            Sidebar.NavigationRequested += OnSidebarNavigationRequested;
            LoadHomeView();

            var configLogs = new (bool enabled, string message)[]
            {
                (_appSettings.SyncHashesWithCDTB, "Sync enabled on startup."),
                (_appSettings.AutoCopyHashes, "Automatically replace old hashes enabled."),
                (_appSettings.CreateBackUpOldHashes, "Backup old hashes enabled."),
                (_appSettings.OnlyCheckDifferences, "Check only differences enabled."),
                (_appSettings.CheckJsonDataUpdates, "Check json files enabled.")
            };

            foreach (var (enabled, message) in configLogs)
            {
                if (enabled) _logService.Log(message);
            }

            if (_appSettings.SyncHashesWithCDTB)
            {
                _ = _status.SyncHashesIfNeeds(_appSettings.SyncHashesWithCDTB);
            }

            // Comprobar actualizaciones de datos JSON si está habilitado
            if (_appSettings.CheckJsonDataUpdates)
            {
                _ = _status.CheckJsonDataUpdatesAsync();
            }

            _ = UpdateManager.CheckForUpdatesAsync(false);
        }

        // ✅ NUEVO: Método que maneja la navegación desde el Sidebar
        private void OnSidebarNavigationRequested(string viewTag)
        {
            switch (viewTag)
            {
                case "Home":
                    LoadHomeView();
                    break;
                case "Export":
                    LoadExportView();
                    break;
                case "Settings":
                    btnSettings_Click(null, null);
                    break;
                case "Help":
                    btnHelp_Click(null, null);
                    break;
                default:
                    break;
            }
        }

        // ✅ MANTENIDO: Por si tienes otros botones que usen este método
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            // El 'sender' del evento que nos llega es el botón original que se pulsó.
            var button = e.OriginalSource as Button;
            if (button == null) return;

            string viewTag = button.Tag as string;
            OnSidebarNavigationRequested(viewTag);
        }

        private void LoadHomeView()
        {
            MainContentArea.Content = _homeViewInstance;
        }

        private void LoadExportView()
        {
            MainContentArea.Content = _exportViewInstance;
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = new HelpWindow(_logService);
            helpWindow.ShowDialog();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(
                new LogService(),
                _httpClient,
                _directoriesCreator,
                _requests,
                _status,
                _appSettings
            );
            
            settingsWindow.SettingsChanged += OnSettingsChanged;
            settingsWindow.ShowDialog();
        }
        
        // Metodo para actualizar en Home las rutas de hashes predeterminadas
        private void OnSettingsChanged(object sender, EventArgs e)
        {
            _homeViewInstance?.UpdateSettings(_appSettings);
        }
    }
}