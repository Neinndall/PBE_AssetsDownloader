using System;
using System.IO;
using System.Threading.Tasks;
using PBE_AssetsDownloader.Services; // Añadimos el using para LogService

namespace PBE_AssetsDownloader.Utils
{
  public class HashCopier
  {
    private readonly LogService _logService;
    private readonly DirectoriesCreator _directoriesCreator;

    public HashCopier(LogService logService, DirectoriesCreator directoriesCreator)
    {
      _logService = logService;
      _directoriesCreator = directoriesCreator;
    }

    public async Task<string> HandleCopyAsync(bool autoCopyHashes)
    {
      if (autoCopyHashes)
      {
        // Iniciar el proceso de copia de hashes
        return await CopyNewHashesToOlds();
      }
      else
      {
        //_logService.Log("AutoCopyHashes is disabled.");
        return string.Empty; // Retornar cadena vacía si autoCopyHashes está deshabilitado
      }
    }

    private async Task<string> CopyNewHashesToOlds()
    {
      // Llamamos a la carpeta de donde cogeremos los hashes para copiar
      string sourcePath = _directoriesCreator.HashesNewPath;
      // Llamamos a la carpeta a donde copiaremos los hashes
      string destinationPath = _directoriesCreator.HashesOldsPaths;

      try
      {
        // Verificar si el directorio de destino existe
        if (!Directory.Exists(destinationPath))
        {
          // Si no existe, crear el directorio de destino
          Directory.CreateDirectory(destinationPath);
          _logService.Log($"Destination directory did not exist and was created: {destinationPath}");
        }

        // Copiar los archivos y subdirectorios desde el directorio fuente al destino, sobrescribiendo si es necesario
        await Task.Run(() => DirectoryCopy(sourcePath, destinationPath, true));

        // Verificar que el directorio de destino existe después de la copia
        if (Directory.Exists(destinationPath))
        {
          _logService.Log("Hashes replaced successfully.");
          return "Hashes replaced successfully.";
        }
        else
        {
          _logService.LogError($"Failed to replace hashes: destination directory not created.");
          return "Failed to replace hashes: destination directory not created.";
        }
      }
      catch (Exception ex)
      {
        _logService.LogError(ex, "An error occurred while copying hashes.");
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
        _logService.LogError(ex, "Error copying directory.");
        throw new InvalidOperationException("Error copying directory", ex);
      }
    }
  }
}
