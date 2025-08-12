using LibVLCSharp.Shared;
using PBE_AssetsDownloader.Services;
using System;
using System.IO;
using System.Reflection;

namespace PBE_AssetsDownloader.Utils
{
    public static class VlcManager
    {
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        public static void Initialize(LogService logService)
        {
            lock (_lock)
            {
                if (_isInitialized)
                {
                    return;
                }

                try
                {
                    var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var vlcLibDirectory = Path.Combine(appDataPath, "PBE_AssetsDownloader", "libvlc");

                    if (Directory.Exists(vlcLibDirectory))
                    {
                        Core.Initialize(vlcLibDirectory);
                        logService.LogDebug($"LibVLC initialized successfully from {vlcLibDirectory}");
                        _isInitialized = true;
                    }
                    else
                    {
                        logService.LogWarning($"VLC libraries not found at the expected path: {vlcLibDirectory}. Video playback will be disabled.");
                    }
                }
                catch (Exception ex)
                {
                    logService.LogError("Failed to initialize LibVLC. Video playback will be disabled. See application_errors.log for details.");
                    logService.LogCritical(ex, "LibVLC Initialization Failed");
                }
            }
        }
    }
}
