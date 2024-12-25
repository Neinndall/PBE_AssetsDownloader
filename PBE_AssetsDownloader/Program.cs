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

        public static async Task<string> RunExtraction(string newHashesDirectory, string oldHashesDirectory, bool syncHashesWithCDTB, bool autoCopyHashes, Action<string> logAction)
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

                // Crear instancia de FilesComparator
                var comparator = new FilesComparator();
                comparator.HashesComparator(newHashesDirectory, oldHashesDirectory, resourcesPath);

                // Mensajes de comparación
                await comparator.CheckFilesDiffAsync();

                // Crear una instancia de HashesManager para comparar hashes
                var hashesManager = new HashesManager(oldHashesDirectory, newHashesDirectory, resourcesPath);
                await hashesManager.CompareHashesAsync();

                // Crear una instancia de Resources para descargar los assets
                var resourcesDownloader = new Resources(httpClient, directoryCreator); // Pasar directoryCreator aquí
                await resourcesDownloader.DownloadAssetsAsync();

                Log.Information("Download complete.");

                // Crear una instancia de HashCopier para hacer el copiar de hashes si está activado
                if (autoCopyHashes)
                {
                    var hashCopier = new HashCopier();
                    return await hashCopier.CopyNewHashesToOlds(newHashesDirectory, oldHashesDirectory);
                }

                return "Hashes were not replaced because autoCopyHashes is disabled.";
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An unexpected error has occurred.");
                Log.Fatal(ex, "StackTrace: {0}", ex.StackTrace);
                return "An error occurred during the process.";
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