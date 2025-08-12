using System;
using System.IO;
using System.Threading.Tasks;
using PBE_AssetsDownloader.Services;

namespace PBE_AssetsDownloader.Utils
{
    public class HashBackUp
    {
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly LogService _logService;

        public HashBackUp(DirectoriesCreator directoriesCreator, LogService logService)
        {
            _directoriesCreator = directoriesCreator;
            _logService = logService;
        }

        public async Task<string> HandleBackUpAsync(bool createBackUp)
        {
            if (createBackUp)
            {
                return await CopyFilesToBackUp();
            }
            else
            {
                return string.Empty;
            }
        }

        public async Task<string> CopyFilesToBackUp()
        {
            try
            {
                await _directoriesCreator.CreateBackUpOldHashesAsync();
                string backupDirectory = _directoriesCreator.BackUpOldHashesPath;                
                var filesToCopy = new[] { "hashes.game.txt", "hashes.lcu.txt" };

                foreach (var fileName in filesToCopy)
                {
                    string sourceFilePath = Path.Combine("hashes", "olds", fileName);
                    string destinationFilePath = Path.Combine(backupDirectory, fileName);

                    if (File.Exists(sourceFilePath))
                    {
                        File.Copy(sourceFilePath, destinationFilePath, true);
                    }
                }
                return backupDirectory;
            }
            catch (Exception ex)
            {
                _logService.LogError("Error occurred while creating HashBackUp. See application_errors.log for details.");
                _logService.LogCritical(ex, "HashBackUp.CopyFilesToBackUp Exception");
                return "Error occurred while creating HashBackUp";
            }
        }
    }
}
