using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using Newtonsoft.Json;
using PBE_AssetsDownloader.Info;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.Services;

namespace PBE_AssetsDownloader.UI
{
    public partial class SettingsForm : Form
    {
        private Requests _requests;
        private DirectoriesCreator _directoriesCreator;
        private Status _status;

        public bool syncHashesWithCDTB { get; private set; }
        public bool AutoCopyHashes { get; private set; }
        public bool CreateBackUpOldHashes { get; private set; }

        // Evento para notificar cambios en la configuración
        public event EventHandler SettingsChanged;

        public SettingsForm(bool syncHashesWithCDTB, bool autoCopyHashes, bool CreateBackUpOldHashes, Status status)
        {
            InitializeComponent();
            ApplicationInfos.SetIcon(this);

            var httpClient = new HttpClient();
            _directoriesCreator = new DirectoriesCreator();
            _requests = new Requests(httpClient, _directoriesCreator);
            _status = new Status(AppendLog);

            var settings = LoadSettings();
            this.syncHashesWithCDTB = settings.syncHashesWithCDTB;
            checkBoxSyncHashes.Checked = syncHashesWithCDTB;

            this.AutoCopyHashes = settings.AutoCopyHashes;
            checkBoxAutoCopy.Checked = AutoCopyHashes;
            
            this.CreateBackUpOldHashes = settings.CreateBackUpOldHashes;
            CheckBoxCreateBackUp.Checked = CreateBackUpOldHashes;
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            syncHashesWithCDTB = checkBoxSyncHashes.Checked;
            AutoCopyHashes = checkBoxAutoCopy.Checked;
            CreateBackUpOldHashes = CheckBoxCreateBackUp.Checked;

            var settings = LoadSettings();
            settings.syncHashesWithCDTB = syncHashesWithCDTB;
            settings.AutoCopyHashes = AutoCopyHashes;
            settings.CreateBackUpOldHashes = CreateBackUpOldHashes;

            SaveSettings(settings.syncHashesWithCDTB, settings.lastUpdateHashes);

            if (syncHashesWithCDTB)
            {
                var result = MessageBox.Show("Do you want to sync files now?", "Confirm synchronization", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    AppendLog("Checking for updates on the server...");
                    bool isUpdated = await _status.IsUpdatedAsync();

                    if (!isUpdated)
                    {
                        _status.CheckForUpdates(isUpdated);
                        return;
                    }

                    AppendLog("Starting sync...");
                    await DownloadFiles(syncHashesWithCDTB, settings.lastUpdateHashes);
                    AppendLog("Synchronization completed.");
                }
            }

            SettingsChanged?.Invoke(this, EventArgs.Empty);
            AppendLog("Settings updated.");
        }

        private async Task DownloadFiles(bool syncHashesWithCDTB, string lastUpdateHashes)
        {
            string downloadDirectory = _directoriesCreator.GetHashesNewsDirectoryPath();

            try
            {
                await _requests.DownloadHashesFilesAsync(downloadDirectory, logMessage =>
                {
                    AppendLog(logMessage);
                });

                SaveSettings(syncHashesWithCDTB, lastUpdateHashes);
            }
            catch (Exception ex)
            {
                _status.HandleError(ex);
            }
        }

        private AppSettings LoadSettings()
        {
            const string configFilePath = "config.json";

            if (File.Exists(configFilePath))
            {
                var json = File.ReadAllText(configFilePath);
                return JsonConvert.DeserializeObject<AppSettings>(json);
            }

            return new AppSettings { syncHashesWithCDTB = false, AutoCopyHashes = false, CreateBackUpOldHashes = false };
        }

        private void SaveSettings(bool syncHashesWithCDTB, string lastUpdateHashes = null)
        {
            var settings = LoadSettings();
            settings.syncHashesWithCDTB = syncHashesWithCDTB;
            settings.AutoCopyHashes = AutoCopyHashes;
            settings.CreateBackUpOldHashes = CreateBackUpOldHashes;

            if (!string.IsNullOrEmpty(lastUpdateHashes))
            {
                settings.lastUpdateHashes = lastUpdateHashes;
            }

            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText("config.json", json);
        }

        public void AppendLog(string message)
        {
            if (richTextBoxLogs.InvokeRequired)
            {
                richTextBoxLogs.Invoke(new Action(() => 
                {
                    // Añadir timestamp a cada mensaje
                    string timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
                    richTextBoxLogs.AppendText(timestampedMessage + Environment.NewLine);
                    richTextBoxLogs.SelectionStart = richTextBoxLogs.TextLength;
                    richTextBoxLogs.ScrollToCaret();
                }));
            }
            else
            {
                string timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
                richTextBoxLogs.AppendText(timestampedMessage + Environment.NewLine);
                richTextBoxLogs.SelectionStart = richTextBoxLogs.TextLength;
                richTextBoxLogs.ScrollToCaret();
            }
        }

        public class AppSettings
        {
            public bool syncHashesWithCDTB { get; set; }
            public string lastUpdateHashes { get; set; }
            public bool AutoCopyHashes { get; set; }
            public bool CreateBackUpOldHashes { get; set; }
        }
    }
}