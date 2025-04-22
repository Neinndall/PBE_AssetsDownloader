using System;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace PBE_AssetsDownloader.Utils
{
    public class HashCopier
    {
        public async Task<string> HandleCopyAsync(bool autoCopyHashes, string sourcePath, string destinationPath)
        {
            if (autoCopyHashes)
            {
                // Iniciar el proceso de copia de hashes
                return await CopyNewHashesToOlds(sourcePath, destinationPath);
            }
            else
            {
                Log.Information("AutoCopyHashes is disabled.");
                return string.Empty; // Retornar cadena vacía si autoCopyHashes está deshabilitado
            }
        }

        public async Task<string> CopyNewHashesToOlds(string sourcePath, string destinationPath)
        {
            // Verificar si el directorio fuente existe
            if (!Directory.Exists(sourcePath))
            {
                Log.Error("Source directory does not exist: {0}", sourcePath);
                return "Hashes were not replaced because the source directory does not exist.";
            }

            try
            {
                // Verificar si el directorio de destino existe
                if (!Directory.Exists(destinationPath))
                {
                    // Si no existe, crear el directorio de destino
                    Directory.CreateDirectory(destinationPath);
                    Log.Information("Destination directory did not exist and was created: {0}", destinationPath);
                }

                // Copiar los archivos y subdirectorios desde el directorio fuente al destino, sobrescribiendo si es necesario
                await Task.Run(() => DirectoryCopy(sourcePath, destinationPath, true));

                // Verificar que el directorio de destino existe después de la copia
                if (Directory.Exists(destinationPath))
                {
                    Log.Information("Hashes replaced successfully.");
                    return "Hashes replaced successfully.";
                }
                else
                {
                    Log.Error("Destination directory was not created: {0}", destinationPath);
                    return "Failed to replace hashes: destination directory not created.";
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while copying hashes.");
                return "An error occurred while copying hashes.";
            }
        }

        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(sourceDirName);
                if (!dir.Exists)
                    throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDirName}");

                // Crear el directorio de destino si no existe
                Directory.CreateDirectory(destDirName);

                // Copiar los archivos
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    string tempPath = Path.Combine(destDirName, file.Name);
                    file.CopyTo(tempPath, true); // Sobrescribir archivos si ya existen
                }

                // Copiar subdirectorios si se permite
                if (copySubDirs)
                {
                    DirectoryInfo[] subdirs = dir.GetDirectories();
                    foreach (DirectoryInfo subdir in subdirs)
                    {
                        string tempPath = Path.Combine(destDirName, subdir.Name);
                        DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error copying directory.");
                throw new InvalidOperationException("Error copying directory", ex);
            }
        }
    }
}
