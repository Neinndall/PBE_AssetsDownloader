using System;
using System.IO;
using System.Threading.Tasks;
using PBE_AssetsDownloader.Services;

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
                return await CopyNewHashesToOlds();
            }
            else
            {
                return string.Empty;
            }
        }

        private async Task<string> CopyNewHashesToOlds()
        {
            string sourcePath = _directoriesCreator.HashesNewPath;
            string destinationPath = _directoriesCreator.HashesOldsPaths;

            try
            {
                await Task.Run(() => DirectoryCopy(sourcePath, destinationPath, true));

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
                _logService.LogError("An error occurred while copying hashes. See application_errors.log for details.");
                _logService.LogCritical(ex, "HashCopier.CopyNewHashesToOlds Exception");
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

                Directory.CreateDirectory(destDirName);

                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    string tempPath = Path.Combine(destDirName, file.Name);
                    file.CopyTo(tempPath, true);
                }

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
                _logService.LogError("Error copying directory. See application_errors.log for details.");
                _logService.LogCritical(ex, $"HashCopier.DirectoryCopy Exception from '{sourceDirName}' to '{destDirName}'");
                throw new InvalidOperationException("Error copying directory", ex);
            }
        }
    }
}
