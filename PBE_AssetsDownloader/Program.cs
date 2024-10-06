using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using Serilog;
using System.Windows.Forms;
using Newtonsoft.Json;
using PBE_NewFileExtractor.UI;
using PBE_NewFileExtractor.Utils;
using PBE_NewFileExtractor.Services;

namespace PBE_NewFileExtractor
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

        public static async Task RunExtraction(string newHashesDirectory, string oldHashesDirectory, bool syncHashesWithCDTB, Action<string> logAction)
        {
            // Configurar el logger
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Sink(new DelegateLogSink(logAction))
                .CreateLogger();

            try
            {
                var directoryCreator = new DirectoriesCreator();
                await directoryCreator.CreateAllDirectoriesAsync();

                var httpClient = new HttpClient();
                var requests = new Requests(httpClient, directoryCreator);
                

                var comparator = new FilesComparator(
                    Path.Combine(newHashesDirectory, "hashes.game.txt"),             
                    Path.Combine(oldHashesDirectory, "hashes.game.txt"),
                    Path.Combine(newHashesDirectory, "hashes.lcu.txt"),
                    Path.Combine(oldHashesDirectory, "hashes.lcu.txt"),
                    Path.Combine("Resources", "differences_game.txt"),
                    Path.Combine("Resources", "differences_lcu.txt")
                );

                // Mensajes de comparación
                await comparator.CheckFilesDiffAsync();
                
                // Crear una instancia de HashesManager para comparar hashes
                var hashesManager = new HashesManager(oldHashesDirectory, newHashesDirectory);
                await hashesManager.CompareHashesAsync();

                // Crear una instancia de Resources para descargar los assets
                var resourcesDownloader = new Resources(new HttpClient());
                await resourcesDownloader.DownloadAssetsAsync();

                Log.Information("Download complete.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An unexpected error has occurred.");
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
        
        // Guardar por si acaso
        // public class AppSettings
        // {
        //     public bool syncHashesWithCDTB { get; set; }
        // }
    }
}
