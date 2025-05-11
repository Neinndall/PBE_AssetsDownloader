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
            bool createBackUpOldHashes, 
            bool onlyCheckDifferences,
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

                // Preparar solicitudes de descarga de assets
                var requests = new Requests(httpClient, directoryCreator);

                // Obtener la ruta de Resources con timestamp
                var resourcesPath = directoryCreator.ResourcesPath;

                // Obtener las extensiones excluidas desde la instancia de AssetDownloader
                var excludedExtensions = assetDownloader.ExcludedExtensions;

                // Comparar hashes (si aplica)
                var hashesManager = new HashesManager(oldHashesDirectory, newHashesDirectory, resourcesPath, excludedExtensions);
                await hashesManager.CompareHashesAsync(); // Siempre se comparan los hashes

                if (onlyCheckDifferences)
                {
                    // Si solo queremos verificar las diferencias, no descargamos recursos ni respaldamos hashes
                    logAction("Only differences check complete. No resources were downloaded or hashes were backed up.");
                    return;  // Salir aquí si solo se verifican las diferencias
                }

                // Descargar recursos (si no es solo verificar diferencias)
                var resourcesDownloader = new Resources(httpClient, directoryCreator); 
                await resourcesDownloader.GetResourcesFiles();

                logAction("Download complete.");
                        
                // Limpiar carpetas vacías dentro de las principales (por ejemplo, plugins y game)
                var directoryCleaner = new DirectoryCleaner(directoryCreator);  // Pasar la instancia de directoryCreator
                directoryCleaner.CleanEmptyDirectories(); // Llamar al método para limpiar las carpetas vacías
                        
                // Manejar respaldo de hashes antiguos
                var backUp = new BackUp(directoryCreator);
                var backupResult = await backUp.HandleBackUpAsync(createBackUpOldHashes);

                // Copiar los nuevos hashes a los antiguos (si aplica)
                var hashCopier = new HashCopier();
                var copyResult = await hashCopier.HandleCopyAsync(autoCopyHashes, newHashesDirectory, oldHashesDirectory);
            }
            catch (Exception ex)
            {
                // Manejo de excepciones global
                Log.Fatal(ex, "An unexpected error has occurred.");
                Log.Fatal(ex, "StackTrace: {0}", ex.StackTrace);
            }
            finally
            {
                // Asegurarse de que los logs se cierren correctamente
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
