using System;
using System.Threading.Tasks;
using PBE_AssetsManager.Utils;

namespace PBE_AssetsManager.Services
{
    public class ExtractionService
    {
        private readonly HashesManager _hashesManager;
        private readonly Resources _resources;
        private readonly DirectoryCleaner _directoryCleaner;
        private readonly HashBackUp _hashBackUp;
        private readonly HashCopier _hashCopier;
        private readonly LogService _logService;
        private readonly DirectoriesCreator _directoriesCreator;
        private readonly AppSettings _appSettings;

        public ExtractionService(
            HashesManager hashesManager,
            Resources resources,
            DirectoryCleaner directoryCleaner,
            HashBackUp hashBackUp,
            HashCopier hashCopier,
            LogService logService,
            DirectoriesCreator directoriesCreator,
            AppSettings appSettings)
        {
            _hashesManager = hashesManager;
            _resources = resources;
            _directoryCleaner = directoryCleaner;
            _hashBackUp = hashBackUp;
            _hashCopier = hashCopier;
            _logService = logService;
            _directoriesCreator = directoriesCreator;
            _appSettings = appSettings;
        }

        public async Task ExecuteAsync()
        {
            try
            {
                await _directoriesCreator.CreateAllDirectoriesAsync();
                await _hashesManager.CompareHashesAsync();
                await _resources.GetResourcesFiles();
                _directoryCleaner.CleanEmptyDirectories();
                await _hashBackUp.HandleBackUpAsync(_appSettings.CreateBackUpOldHashes);
                await _hashCopier.HandleCopyAsync(_appSettings.AutoCopyHashes);
                
                // No pondremos ningun log aqui
            }
            catch (Exception ex)
            {
                _logService.LogError("A critical error occurred during the extraction process. See application_errors.log for details.");
                _logService.LogCritical(ex, "ExtractionService.ExecuteAsync Exception");
            }
        }
    }
}
