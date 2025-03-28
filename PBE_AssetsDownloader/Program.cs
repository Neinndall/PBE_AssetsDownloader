using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using Serilog;
using System.Windows.Forms;
using Newtonsoft.Json;
using PBE_AssetsDownloader.UI;
using PBE_AssetsDownloader.Utils;
using PBE_AssetsDownloader.Services;

namespace PBE_AssetsDownloader
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Iniciar el formulario principal
            Application.Run(new MainForm());
        }

        public static async Task RunExtraction(
            string newHashesDirectory, 
            string oldHashesDirectory, 
            bool syncHashesWithCDTB, 
            bool autoCopyHashes, 
            bool CreateBackUpOldHashes, 
            Action<string> logAction)
        {
            // Configurar el logger
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Sink(new DelegateLogSink(logAction))
                .CreateLogger();

            try
            {
                // Crear instancia de DirectoriesCreator y AssetDownloader
                var directoryCreator = new DirectoriesCreator();
                var httpClient = new HttpClient();
                var assetDownloader = new AssetDownloader(httpClient, directoryCreator);

                // Crear todas las carpetas necesarias
                await directoryCreator.CreateAllDirectoriesAsync().ConfigureAwait(false);

                var requests = new Requests(httpClient, directoryCreator);

                // Obtener la ruta de Resources con timestamp
                var resourcesPath = directoryCreator.ResourcesPath;

                // Obtener la lista de extensiones excluidas desde la instancia de AssetDownloader
                var excludedExtensions = assetDownloader.ExcludedExtensions;

                // Crear una instancia de HashesManager para comparar hashes
                var hashesManager = new HashesManager(oldHashesDirectory, newHashesDirectory, resourcesPath, excludedExtensions);
                await hashesManager.CompareHashesAsync();

                // Crear una instancia de Resources para descargar los assets
                var resourcesDownloader = new Resources(httpClient, directoryCreator); 
                await resourcesDownloader.GetResourcesFiles();

                Log.Information("Download complete.");
                
                // Crear una instancia de BackUp para manejar el respaldo de hashes antiguos
                var backUp = new BackUp(directoryCreator);
                var backupResult = await backUp.HandleBackUpAsync(CreateBackUpOldHashes);

                // Crear una instancia de HashCopier para manejar el copiado de nuevos hashes a antiguos
                var hashCopier = new HashCopier();
                var copyResult = await hashCopier.HandleCopyAsync(autoCopyHashes, newHashesDirectory, oldHashesDirectory);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An unexpected error has occurred.");
                Log.Fatal(ex, "StackTrace: {0}", ex.StackTrace);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        // Implementación de un sink personalizado para redirigir los logs a un Action<string>
        public class DelegateLogSink : Serilog.Core.ILogEventSink
        {
            private readonly Action<string> _logAction;

            public DelegateLogSink(Action<string> logAction)
            {
                _logAction = logAction;
            }

            public void Emit(Serilog.Events.LogEvent logEvent)
            {
                _logAction(logEvent.RenderMessage());
            }
        }
    }
}
