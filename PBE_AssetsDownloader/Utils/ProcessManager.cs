// PBE_AssetsDownloader/Utils/ProcessManager.cs

using System;
using System.Diagnostics; // This namespace is fully compatible with WPF
using System.Threading.Tasks;
using Serilog; // Añadimos el using para Serilog

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
                    UseShellExecute = false, // Do not use the system shell
                    CreateNoWindow = true    // Do not create a window for the process
                }
            };

            try
            {
                // Start the process
                process.Start();

                // Read the standard output and error of the process asynchronously
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                // Wait for the process to exit
                await process.WaitForExitAsync();

                // Check the process exit code
                if (process.ExitCode != 0)
                {
                    // Throw an exception if the process exited with a non-zero exit code
                    Serilog.Log.Error($"Process exited with code {process.ExitCode}. Error: {error}");
                    throw new Exception($"Process exited with code {process.ExitCode}. Error: {error}");
                }

                // Print the standard output of the process
                Serilog.Log.Information($"Process output: {output}");
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Serilog.Log.Error(ex, $"An error occurred while running the process: {ex.Message}");
                throw; // Rethrow the exception after logging it
            }
        }
    }
}