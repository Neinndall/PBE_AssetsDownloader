using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PBE_AssetsDownloader.Utils;

namespace PBE_AssetsDownloader.Services
{
  public class HashesManager
  {
    // Directories for old and new hashes, and the resources directory
    private readonly string _oldHashesDirectory;
    private readonly string _newHashesDirectory;
    private readonly string _resourcesPath;
    private readonly List<string> _excludedExtensions;
    private readonly LogService _logService;

    // Constructor that receives directories and excluded extensions

    public HashesManager(string oldHashesDirectory, string newHashesDirectory, string resourcesPath, List<string> excludedExtensions, LogService logService)
    {
      _oldHashesDirectory = oldHashesDirectory;
      _newHashesDirectory = newHashesDirectory;
      _resourcesPath = resourcesPath;
      _excludedExtensions = excludedExtensions;
      _logService = logService; // ¡NUEVO: Asignar la instancia de LogService!
    }

    // Main method to compare hashes
    public async Task CompareHashesAsync()
    {
      // Log message indicating that hashes are being compared and filtered
      _logService.Log("Comparing and filtering hashes, please wait...");

      // Define paths for hash files
      string oldGameHashesPath = Path.Combine(_oldHashesDirectory, "hashes.game.txt");
      string oldLcuHashesPath = Path.Combine(_oldHashesDirectory, "hashes.lcu.txt");
      string newGameHashesPath = Path.Combine(_newHashesDirectory, "hashes.game.txt");
      string newLcuHashesPath = Path.Combine(_newHashesDirectory, "hashes.lcu.txt");

      // Asegúrate de que los archivos existan antes de intentar leerlos.
      if (!File.Exists(oldGameHashesPath)) _logService.LogWarning($"Old game hashes file not found: {oldGameHashesPath}");
      if (!File.Exists(oldLcuHashesPath)) _logService.LogWarning($"Old LCU hashes file not found: {oldLcuHashesPath}");
      if (!File.Exists(newGameHashesPath)) _logService.LogWarning($"New game hashes file not found: {newGameHashesPath}");
      if (!File.Exists(newLcuHashesPath)) _logService.LogWarning($"New LCU hashes file not found: {newLcuHashesPath}");

      // Read hash files asynchronously
      var oldGameHashesTask = TryReadAllLinesAsync(oldGameHashesPath);
      var oldLcuHashesTask = TryReadAllLinesAsync(oldLcuHashesPath);
      var newGameHashesTask = TryReadAllLinesAsync(newGameHashesPath);
      var newLcuHashesTask = TryReadAllLinesAsync(newLcuHashesPath);

      // Wait for all reads to complete
      await Task.WhenAll(oldGameHashesTask, oldLcuHashesTask, newGameHashesTask, newLcuHashesTask);

      // Assign the results read from the files
      var oldGameHashes = oldGameHashesTask.Result;
      var oldLcuHashes = oldLcuHashesTask.Result;
      var newGameHashes = newGameHashesTask.Result;
      var newLcuHashes = newLcuHashesTask.Result;

      // Convert old hashes into HashSet for improved lookup performance (O(1) complexity)
      var oldGameHashesSet = new HashSet<string>(oldGameHashes ?? Enumerable.Empty<string>()); // Manejar posibles nulls
      var oldLcuHashesSet = new HashSet<string>(oldLcuHashes ?? Enumerable.Empty<string>()); // Manejar posibles nulls

      // Use Parallel.ForEach to compare hashes in parallel for better performance
      var differencesGame = new ConcurrentBag<string>();
      var differencesLcu = new ConcurrentBag<string>();

      // Compare new hashes with old hashes in parallel (for "game" hashes)
      Parallel.ForEach(newGameHashes ?? Enumerable.Empty<string>(), newHash => // Manejar posibles nulls
      {
        if (!oldGameHashesSet.Contains(newHash))
        {
          differencesGame.Add(newHash);
        }
      });

      // Compare new hashes with old hashes in parallel (for "lcu" hashes)
      Parallel.ForEach(newLcuHashes ?? Enumerable.Empty<string>(), newHash => // Manejar posibles nulls
      {
        if (!oldLcuHashesSet.Contains(newHash))
        {
          differencesLcu.Add(newHash);
        }
      });

      // Call the filtering method for the found differences
      await FilterAndSaveDifferencesAsync(differencesGame.ToList(), differencesLcu.ToList());
    }

    // Método auxiliar para leer archivos y manejar errores de forma más elegante
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
        _logService?.LogError(ex, $"Error reading hash file: {path}");
        return Array.Empty<string>();
      }
    }

    // Method that filters and saves differences, removing those that do not meet certain criteria
    public async Task FilterAndSaveDifferencesAsync(List<string> differencesGame, List<string> differencesLcu)
    {
      // Read the old hashes file (game.txt) for comparisons
      string oldHashesPath = Path.Combine(_oldHashesDirectory, "hashes.game.txt");
      // ¡MODIFICADO: Usar TryReadAllLinesAsync!
      var oldHashes = await TryReadAllLinesAsync(oldHashesPath);

      // Create a HashSet with file paths, removing the extension for .tex comparison
      var oldPathsWithoutTex = new HashSet<string>(
          oldHashes
              .Select(line => line.Split(' ')[1]) // Get the file path
              .Where(path => path.EndsWith(".dds")) // Filter by desired extensions (only .dds in this case)
              .Select(path => path[..path.LastIndexOf('.')]) // Remove the extension for comparison
      );

      // Lists to store filtered differences
      var filteredDifferencesGame = new ConcurrentBag<string>();
      var filteredDifferencesLcu = new ConcurrentBag<string>();

      // Process GAME differences in parallel
      Parallel.ForEach(differencesGame, line =>
      {
        try
        {
          var parts = line.Split(' '); // Split the line into parts
          var filePath = parts[1];     // Get the file path
          var extension = Path.GetExtension(filePath); // Get the file extension

          // If the extension is in the excluded list, ignore this file
          if (_excludedExtensions.Contains(extension))
            return;

          // If the path is ignored by custom rules, discard it
          string adjusted = AssetUrlRules.Adjust(filePath);
          if (adjusted == null)
            return;

          // If it's a .tex file, compare it with old paths without extension
          if (filePath.EndsWith(".tex"))
          {
            var baseFile = filePath[..filePath.LastIndexOf('.')]; // Remove the .tex extension
            if (!oldPathsWithoutTex.Contains(baseFile)) // If not found in paths without extension, add it to differences
            {
              filteredDifferencesGame.Add(line);
            }
          }
          else
          {
            filteredDifferencesGame.Add(line); // If it's not a .tex, add it directly
          }
        }
        catch (Exception ex)
        {
          // If an error occurs processing a line, log it
          _logService.LogWarning($"Error filtering GAME line '{line}': {ex.Message}");
        }
      });

      // Process LCU differences in parallel
      Parallel.ForEach(differencesLcu, line =>
      {
        try
        {
          var parts = line.Split(' '); // Split the line into parts
          var filePath = parts[1];     // Get the file path

          // If the path is ignored by custom rules, discard it
          string adjusted = AssetUrlRules.Adjust(filePath);
          if (adjusted != null)
          {
            filteredDifferencesLcu.Add(line);
          }
        }
        catch (Exception ex)
        {
          _logService.LogWarning($"Error filtering LCU line '{line}': {ex.Message}");
        }
      });

      try
      {
        await File.WriteAllLinesAsync(Path.Combine(_resourcesPath, "differences_game.txt"), filteredDifferencesGame);
        _logService.Log($"Filtered differences saved to {Path.Combine(_resourcesPath, "differences_game.txt")}");
      }
      catch (Exception ex)
      {
        _logService.LogError(ex, $"Error saving game differences to {Path.Combine(_resourcesPath, "differences_game.txt")}");
      }

      try
      {
        await File.WriteAllLinesAsync(Path.Combine(_resourcesPath, "differences_lcu.txt"), filteredDifferencesLcu);
        _logService.Log($"Filtered differences saved to {Path.Combine(_resourcesPath, "differences_lcu.txt")}");
      }
      catch (Exception ex)
      {
        _logService.LogError(ex, $"Error saving LCU differences to {Path.Combine(_resourcesPath, "differences_lcu.txt")}");
      }
    }
  }
}
