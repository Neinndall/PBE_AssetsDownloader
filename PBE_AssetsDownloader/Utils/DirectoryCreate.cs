using System;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace PBE_AssetsDownloader.Utils
{
    public class DirectoriesCreator
    {
        public string ResourcesPath { get; private set; }
        public string SubAssetsDownloadedPath { get; }

        public DirectoriesCreator()
        {
            SubAssetsDownloadedPath = Path.Combine("AssetsDownloaded", DateTime.Now.ToString("dd-M-yyyy"));
            ResourcesPath = Path.Combine("Resources", DateTime.Now.ToString("dd-M-yyyy"));
        }

        public Task CreateDirResourcesAsync() => CreateFoldersAsync(ResourcesPath);

        public Task CreateDirAssetsDownloadedAsync() => CreateFoldersAsync(SubAssetsDownloadedPath);

        public Task CreateHashesNewDirectoryAsync() => CreateFoldersAsync(Path.Combine("hashes", "new"));

        public Task CreateBackUpOldHashesAsync() => CreateFoldersAsync(Path.Combine("hashes", "olds", "BackUp", DateTime.Now.ToString("dd-M-yyyy")));

        public string GetHashesNewsDirectoryPath() => Path.Combine("hashes", "new");

        public string GetBackUpOldHashesPath() => Path.Combine("hashes", "olds", "BackUp", DateTime.Now.ToString("dd-M-yyyy"));

        public async Task CreateAllDirectoriesAsync()
        {
            await CreateDirResourcesAsync();
            await CreateDirAssetsDownloadedAsync();
            await CreateHashesNewDirectoryAsync();
        }

        public string CreateAssetTypeDirectory(string finalExtension, string fileName, string url)
        {
            string assetTypeFolder = finalExtension switch
            {
                ".png" or ".jpg" or ".jpeg" => GetImageFolder(fileName, url),
                ".webm" or ".mp4" => Path.Combine(SubAssetsDownloadedPath, "Videos"),
                ".ogg" or ".mp3" or ".wav" => Path.Combine(SubAssetsDownloadedPath, "Audios"),
                ".skn" or ".skl" or ".scb" or ".anm" => Path.Combine(SubAssetsDownloadedPath, "Meshes"),
                ".json" or ".txt" => Path.Combine(SubAssetsDownloadedPath, "Text Files"),
                ".bin" => Path.Combine(SubAssetsDownloadedPath, "Bin Files"),
                ".svg" => Path.Combine(SubAssetsDownloadedPath, "Svgs"),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(assetTypeFolder) && !Directory.Exists(assetTypeFolder))
            {
                Directory.CreateDirectory(assetTypeFolder);
                Log.Information("Directory created for asset type: {0}", assetTypeFolder);
            }

            return assetTypeFolder;
        }

        private string GetImageFolder(string fileName, string url)
        {
            if (url.Contains("/summoneremotes/") || fileName.Contains(".accessories") || fileName.Contains("_accessories") || fileName.Contains("Inventory.TFT") || fileName.Contains("_inventory"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Emotes");
            
            if (url.Contains("/profile-icons/"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Icons");
            
            if (url.Contains("/ux") && (url.Contains("/teamicons/")))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Others Icons");
            
            if (url.Contains("/champion-chroma-images/"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Chromas");
            
            if (url.Contains("/loadingscreen/"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "LoadingScreen");
            
            if (url.Contains("/hud/") && (fileName.Contains("circle") || fileName.Contains("square")))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Hud");
            
            if (fileName.Contains("loadscreen_") || fileName.Contains("loadscreen"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Skins", "LoadScreen");
            
            if (url.Contains("/tft") && (fileName.Contains("splash_tile") || fileName.Contains("splash_uncentered") || fileName.Contains("splash_centered")))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Skins", "SplashArts", "TFT");
            
            if (url.Contains("plugins") && (fileName.Contains("splash_centered") || fileName.Contains("splash_tile") || fileName.Contains("splash_uncentered")))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Skins", "SplashArts");
            
            if (url.Contains("/skins/") && url.Contains("/particles/"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Skins", "Particles");
            
            if (url.Contains("/skins/") && (url.Contains("skinfeaturespreview")))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Skins", "Exalted");
            
            if (url.Contains("/skins/") && (fileName.Contains("2x_") || fileName.Contains("4x_") || fileName.Contains("_skin") || fileName.Contains("_base")))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Skins");
            
            if (url.Contains("/regalia/"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Banners");
            
            if (url.Contains("/maps/") && (url.Contains("/kitpieces") && url.Contains("/textures/")))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Maps", "Textures");
            
            if (url.Contains("/maps/") && url.Contains("/kitpieces"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Maps");
            
            if (url.Contains("/maps/") && (url.Contains("/srs/") || fileName.Contains("_env_update") || fileName.Contains("_env")) || url.Contains("/terrainpaint/"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Maps");
            
            if (url.Contains("levels/"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Maps");
            
            if (url.Contains("/maps/") && url.Contains("/mapgeometry/"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Maps");
            
            if (url.Contains("tft"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "TFT");
                
            if (url.Contains("/maps/particles/"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Maps", "Particles");
            
            if (url.Contains("/shared/particles/"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Shared", "Particles");
            
            if (url.Contains("/augments/") && url.Contains("/icons/"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Augments");
            
            if (url.Contains("/augments/") && url.Contains("/statanvil/"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Augments", "Statanvil");
            
            if (url.Contains("/companions/"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Companions");
            
            if (url.Contains("/loot/"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Loot");
            
            if (url.Contains("/seasons/"))
                return Path.Combine(SubAssetsDownloadedPath, "Images", "Seasons");
            
            // Si ninguna de las condiciones anteriores se cumple, clasificar como "Unclassified"
            return Path.Combine(SubAssetsDownloadedPath, "Images", "Unclassified");
        }

        private static Task CreateFoldersAsync(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    Log.Information("Directory created: {0}", path);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Error during directory creation: {0}", path);
            }

            return Task.CompletedTask;
        }
    }
}