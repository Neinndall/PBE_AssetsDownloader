using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PBE_AssetsManager.Utils;

namespace PBE_AssetsManager.Services
{
  public class HashesManager
  {
    private readonly AppSettings _appSettings;
    private readonly DirectoriesCreator _directoriesCreator;
    private readonly LogService _logService;

    public HashesManager(AppSettings appSettings, DirectoriesCreator directoriesCreator, LogService logService)
    {
      _appSettings = appSettings;
      _directoriesCreator = directoriesCreator;
      _logService = logService;
    }

    public async Task CompareHashesAsync()
    {
      _logService.Log("Comparing and filtering hashes, please wait...");

      string oldGameHashesPath = Path.Combine(_appSettings.OldHashesPath, "hashes.game.txt");
      string oldLcuHashesPath = Path.Combine(_appSettings.OldHashesPath, "hashes.lcu.txt");
      string newGameHashesPath = Path.Combine(_appSettings.NewHashesPath, "hashes.game.txt");
      string newLcuHashesPath = Path.Combine(_appSettings.NewHashesPath, "hashes.lcu.txt");

      var oldGameHashesTask = TryReadAllLinesAsync(oldGameHashesPath);
      var oldLcuHashesTask = TryReadAllLinesAsync(oldLcuHashesPath);
      var newGameHashesTask = TryReadAllLinesAsync(newGameHashesPath);
      var newLcuHashesTask = TryReadAllLinesAsync(newLcuHashesPath);

      await Task.WhenAll(oldGameHashesTask, oldLcuHashesTask, newGameHashesTask, newLcuHashesTask);

      var oldGameHashesSet = new HashSet<string>(oldGameHashesTask.Result);
      var oldLcuHashesSet = new HashSet<string>(oldLcuHashesTask.Result);
      var newGameHashes = newGameHashesTask.Result;
      var newLcuHashes = newLcuHashesTask.Result;

      var differencesGame = new ConcurrentBag<string>();
      Parallel.ForEach(newGameHashes, newHash =>
      {
        if (!oldGameHashesSet.Contains(newHash))
        {
          differencesGame.Add(newHash);
        }
      });

      var differencesLcu = new ConcurrentBag<string>();
      Parallel.ForEach(newLcuHashes, newHash =>
      {
        if (!oldLcuHashesSet.Contains(newHash))
        {
          differencesLcu.Add(newHash);
        }
      });

      await FilterAndSaveDifferencesAsync(differencesGame.ToList(), differencesLcu.ToList());
    }

    private async Task<string[]> TryReadAllLinesAsync(string path)
    {
      try
      {
        return await File.ReadAllLinesAsync(path);
      }
      catch (FileNotFoundException)
      {
        _logService.LogWarning($"Hash file not found, treating as empty: {path}");
        return Array.Empty<string>();
      }
      catch (Exception ex)
      {
        _logService.LogError(ex, $"Error reading hash file: {path}");
        return Array.Empty<string>();
      }
    }

    public async Task FilterAndSaveDifferencesAsync(List<string> differencesGame, List<string> differencesLcu)
    {
      string oldHashesPath = Path.Combine(_appSettings.OldHashesPath, "hashes.game.txt");
      var oldHashes = await TryReadAllLinesAsync(oldHashesPath);

      var oldPathsWithoutTex = new HashSet<string>(
          oldHashes
              .Select(line => line.Split(' ')[1])
              .Where(path => path.EndsWith(".dds"))
              .Select(path => path[..path.LastIndexOf('.')])
      );

      var filteredDifferencesGame = new ConcurrentBag<string>();
      Parallel.ForEach(differencesGame, line =>
      {
        try
        {
          var parts = line.Split(' ');
          if (parts.Length < 2) return;
          var filePath = parts[1];

          // The filtering logic is now fully centralized in AssetUrlRules.Adjust
          if (AssetUrlRules.Adjust(filePath) == null)
            return;

          if (filePath.EndsWith(".tex"))
          {
            var baseFile = filePath[..filePath.LastIndexOf('.')];
            if (!oldPathsWithoutTex.Contains(baseFile))
            {
              filteredDifferencesGame.Add(line);
            }
          }
          else
          {
            filteredDifferencesGame.Add(line);
          }
        }
        catch (Exception ex)
        {
            _logService.LogError(ex, $"Error filtering GAME line '{line}'");
        }
      });

      var filteredDifferencesLcu = new ConcurrentBag<string>();
      Parallel.ForEach(differencesLcu, line =>
      {
        try
        {
          var parts = line.Split(' ');
          if (parts.Length < 2) return;
          var filePath = parts[1];

          // Use the same centralized filtering logic for LCU assets for consistency
          if (AssetUrlRules.Adjust(filePath) != null)
          {
            filteredDifferencesLcu.Add(line);
          }
        }
        catch (Exception ex)
        {
            _logService.LogError(ex, $"Error filtering LCU line '{line}'");
        }
      });

      try
      {
        await File.WriteAllLinesAsync(Path.Combine(_directoriesCreator.ResourcesPath, "differences_game.txt"), filteredDifferencesGame);
        _logService.Log($"Filtered differences saved to {Path.Combine(_directoriesCreator.ResourcesPath, "differences_game.txt")}");
      }
      catch (Exception ex)
      {
        _logService.LogError(ex, "Error saving game differences");
      }

      try
      {
        await File.WriteAllLinesAsync(Path.Combine(_directoriesCreator.ResourcesPath, "differences_lcu.txt"), filteredDifferencesLcu);
        _logService.Log($"Filtered differences saved to {Path.Combine(_directoriesCreator.ResourcesPath, "differences_lcu.txt")}");
      }
      catch (Exception ex)
      {
        _logService.LogError(ex, "Error saving LCU differences");
      }
    }
  }
}
