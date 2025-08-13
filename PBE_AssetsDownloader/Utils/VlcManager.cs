using LibVLCSharp.Shared;
using PBE_AssetsDownloader.Services;
using System;
using System.IO;
using System.Linq;
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

                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var vlcLibDirectory = Path.Combine(appDataPath, "PBE_AssetsDownloader", "libvlc");

                try
                {
                    if (!Directory.Exists(vlcLibDirectory) || !Directory.EnumerateFileSystemEntries(vlcLibDirectory).Any())
                    {
                        logService.LogDebug("VLC libraries not found or directory is empty. Extracting from embedded resources...");
                        ExtractVlcLibraries(vlcLibDirectory, logService);
                    }

                    Core.Initialize(vlcLibDirectory);
                    logService.LogDebug($"LibVLC initialized successfully from {vlcLibDirectory}");
                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    logService.LogError("Failed to initialize LibVLC. Video playback will be disabled. See application_errors.log for details.");
                    logService.LogCritical(ex, "LibVLC Initialization Failed");
                }
            }
        }

        private static void ExtractVlcLibraries(string destinationDirectory, LogService logService)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNamePrefix = $"{assembly.GetName().Name}.Resources.libvlc.";

            var vlcResources = assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(resourceNamePrefix))
                .ToList();

            if (!vlcResources.Any())
            {
                logService.LogWarning("No embedded VLC library resources found. Video playback will be disabled.");
                return;
            }

            logService.LogDebug($"Found {vlcResources.Count} embedded VLC files to extract.");

            foreach (var resourceName in vlcResources)
            {
                var relativePath = resourceName.Substring(resourceNamePrefix.Length);

                // Reconstruct the path correctly, handling dots in filenames.
                // This assumes file extensions use a single dot (e.g., .dll), which is safe for VLC libs.
                var pathParts = relativePath.Split('.');
                if (pathParts.Length < 2) continue; // Skip invalid or unexpected resource names

                var fileName = $"{pathParts[pathParts.Length - 2]}.{pathParts[pathParts.Length - 1]}";
                var directoryParts = pathParts.Take(pathParts.Length - 2).ToArray();
                var relativeDirectory = Path.Combine(directoryParts);
                var finalRelativePath = Path.Combine(relativeDirectory, fileName);

                var destinationPath = Path.Combine(destinationDirectory, finalRelativePath);

                try
                {
                    var destinationFolder = Path.GetDirectoryName(destinationPath);
                    if (destinationFolder != null && !Directory.Exists(destinationFolder))
                    {
                        Directory.CreateDirectory(destinationFolder);
                    }

                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null)
                        {
                            logService.LogWarning($"Could not find embedded resource stream for {resourceName}");
                            continue;
                        }
                        using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logService.LogError($"Failed to extract VLC file: {destinationPath}. See application_errors.log for details.");
                    logService.LogCritical(ex, $"VLC File Extraction Failed for: {resourceName}");
                }
            }
            logService.LogDebug("Successfully extracted VLC libraries.");
        }
    }
}
