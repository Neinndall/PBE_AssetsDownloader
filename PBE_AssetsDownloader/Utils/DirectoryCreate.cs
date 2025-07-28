using System;
using System.IO;
using System.Threading.Tasks;
using PBE_AssetsDownloader.Services;

namespace PBE_AssetsDownloader.Utils
{
  public class DirectoriesCreator
  {
    private readonly LogService _logService;
    
    public string ResourcesPath { get; private set; }
    public string PreviewAssetsPath { get; private set; }
    public string HashesNewPath { get; private set; }
    public string HashesOldsPaths { get; private set; }
    public string JsonCacheNewPath { get; private set; }
    public string JsonCacheOldPath { get; private set; }
    public string SubAssetsDownloadedPath { get; private set; }
    public string BackUpOldHashesPath { get; private set; }


    public DirectoriesCreator(LogService logService)
    {
      _logService = logService;

      string date = DateTime.Now.ToString("dd-M-yyyy.H.mm.ss");
      SubAssetsDownloadedPath = Path.Combine("AssetsDownloaded", date);
      ResourcesPath = Path.Combine("Resources", date);
      
      HashesNewPath = Path.Combine("hashes", "new");
      HashesOldsPaths = Path.Combine("hashes", "olds");
      
      PreviewAssetsPath = Path.Combine("PBE_PreviewAssets");
      JsonCacheNewPath = Path.Combine("json_cache", "new");
      JsonCacheOldPath = Path.Combine("json_cache", "old");
      BackUpOldHashesPath = Path.Combine("hashes", "olds", "BackUp", date);
      
    }

    public Task CreateDirResourcesAsync() => CreateFoldersAsync(ResourcesPath);
    
    public Task CreateDirSubAssetsDownloadedAsync() => CreateFoldersAsync(SubAssetsDownloadedPath);

    public Task CreateBackUpOldHashesAsync() => CreateFoldersAsync(BackUpOldHashesPath);

    public Task CreatePreviewAssetsAsync() => CreateFoldersAsync(PreviewAssetsPath);
    
    public Task CreateDirJsonCacheNewAsync() => CreateFoldersAsync(JsonCacheNewPath);
    
    public Task CreateDirJsonCacheOldAsync() => CreateFoldersAsync(JsonCacheOldPath);
    
    public async Task CreateAllDirectoriesAsync()
    {
        await CreateDirResourcesAsync();
        await CreateDirSubAssetsDownloadedAsync();
    }
    
    public string CreateAssetDirectoryPath(string url, string downloadDirectory)
    {
      string path = new Uri(url).AbsolutePath;

      if (path.StartsWith("/pbe/"))
      {
        path = path.Substring(5); // Eliminar "/pbe/"
      }

      string patternToReplace = "rcp-be-lol-game-data/global/default/";
      if (path.Contains(patternToReplace))
      {
        path = path.Replace(patternToReplace, "rcp-be-lol-game-data/");
      }

      string safePath = path.Replace("/", "");

      foreach (char invalidChar in Path.GetInvalidPathChars())
      {
        safePath = safePath.Replace(invalidChar.ToString(), "_");
      }

      string fullDirectoryPath = Path.Combine(downloadDirectory, safePath);
      string directory = Path.GetDirectoryName(fullDirectoryPath);

      if (!Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      return directory;
    }

    private Task CreateFoldersAsync(string path)
    {
      try
      {
        if (!Directory.Exists(path))
        {
          Directory.CreateDirectory(path);
          _logService.Log($"Directory created successfully at: {path}");
        }
      }
      catch (Exception e)
      {
        _logService.LogError(e, $"Error during directory creation for path: {path}");
      }

      return Task.CompletedTask;
    }
  }
}