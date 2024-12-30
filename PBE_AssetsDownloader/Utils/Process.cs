using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PBE_AssetsDownloader.Utils
{
    public class ProcessManager
    {
        public static async Task RunProcessAsync(string fileName, string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false, // No usar la shell del sistema
                    CreateNoWindow = true    // No crear ventana para el proceso
                }
            };

            try
            {
                // Iniciar el proceso
                process.Start();

                // Leer la salida estándar y los errores del proceso de manera asíncrona
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                // Esperar a que el proceso termine
                await process.WaitForExitAsync();

                // Comprobar el código de salida del proceso
                if (process.ExitCode != 0)
                {
                    // Lanzar excepción si el proceso terminó con un código de salida distinto de cero
                    throw new Exception($"Process exited with code {process.ExitCode}. Error: {error}");
                }

                // Imprimir la salida estándar del proceso
                Console.WriteLine(output);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.Error.WriteLine($"An error occurred while running the process: {ex.Message}");
                throw; // Rethrow the exception after logging it
            }
        }
    }
}
