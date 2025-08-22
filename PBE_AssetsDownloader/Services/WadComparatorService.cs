using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Threading.Tasks;
using LeagueToolkit.Core.Wad;

namespace PBE_AssetsDownloader.Services
{
    public enum ChunkDiffType
    {
        New,
        Removed,
        Modified,
        Renamed
    }

    public class ChunkDiff
    {
        public ChunkDiffType Type { get; set; }
        public WadChunk OldChunk { get; set; }
        public WadChunk NewChunk { get; set; }
        public string OldPath { get; set; }
        public string NewPath { get; set; }
    }

    public class WadComparatorService
    {
        private readonly HashResolverService _hashResolverService;
        private readonly LogService _logService;

        public WadComparatorService(HashResolverService hashResolverService, LogService logService)
        {
            _hashResolverService = hashResolverService;
            _logService = logService;
        }

        public async Task<List<ChunkDiff>> CompareWadsAsync(string oldPbeDir, string newPbeDir)
        {
            _logService.Log("Starting WAD comparison...");

            await _hashResolverService.LoadHashesAsync();

            var oldGameWadDir = Path.Combine(oldPbeDir, "Game", "DATA", "FINAL");
            var newGameWadDir = Path.Combine(newPbeDir, "Game", "DATA", "FINAL");

            var oldPluginsWadDir = Path.Combine(oldPbeDir, "Plugins");
            var newPluginsWadDir = Path.Combine(newPbeDir, "Plugins");

            var allDiffs = new List<ChunkDiff>();

            try
            {
                var gameWadDiffs = await CompareWadDirectoriesAsync(oldGameWadDir, newGameWadDir);
                allDiffs.AddRange(gameWadDiffs);

                var pluginsWadDiffs = await CompareWadDirectoriesAsync(oldPluginsWadDir, newPluginsWadDir);
                allDiffs.AddRange(pluginsWadDiffs);

                _logService.Log($"WAD comparison finished. Found {allDiffs.Count} differences.");
            }
            catch (Exception ex)
            {
                _logService.LogError($"An error occurred during WAD comparison: {ex.Message}");
            }

            return allDiffs;
        }

        private async Task<List<ChunkDiff>> CompareWadDirectoriesAsync(string oldWadDir, string newWadDir)
        {
            var allDiffs = new List<ChunkDiff>();

            if (!Directory.Exists(oldWadDir))
            {
                _logService.LogWarning($"Old WAD directory not found: {oldWadDir}");
                return allDiffs;
            }

            if (!Directory.Exists(newWadDir))
            {
                _logService.LogWarning($"New WAD directory not found: {newWadDir}");
                return allDiffs;
            }

            var oldWadFiles = Directory.GetFiles(oldWadDir, "*.wad.client", SearchOption.AllDirectories);
            var newWadFiles = Directory.GetFiles(newWadDir, "*.wad.client", SearchOption.AllDirectories);

            foreach (var oldWadFile in oldWadFiles)
            {
                // Calculate the relative path of the old WAD file to its base directory
                var relativePath = Path.GetRelativePath(oldWadDir, oldWadFile);
                // Construct the full path for the new WAD file using the new base directory and the relative path
                var newWadFileFullPath = Path.Combine(newWadDir, relativePath);

                if (File.Exists(newWadFileFullPath))
                {
                    _logService.Log($"Comparing {relativePath}...");
                    using var oldWad = new WadFile(oldWadFile);
                    using var newWad = new WadFile(newWadFileFullPath);

                    var diffs = await CollectDiffsAsync(oldWad, newWad);
                    _logService.Log($"Found {diffs.Count} differences in {relativePath}.");
                    allDiffs.AddRange(diffs);
                }
                else
                {
                    _logService.LogWarning($"New WAD file not found: {newWadFileFullPath}. Skipping comparison for this file.");
                }
            }

            return allDiffs;
        }

        private async Task<List<ChunkDiff>> CollectDiffsAsync(WadFile oldWad, WadFile newWad)
        {
            var diffs = new List<ChunkDiff>();

            var oldChunks = oldWad.Chunks.ToDictionary(c => c.Key, c => c.Value);
            var newChunks = newWad.Chunks.ToDictionary(c => c.Key, c => c.Value);

            var oldChunkChecksums = await GetChunkChecksumsAsync(oldWad, oldChunks.Values);
            var newChunkChecksums = await GetChunkChecksumsAsync(newWad, newChunks.Values);

            // Removed and Modified
            foreach (var oldChunk in oldChunks.Values)
            {
                var oldPath = _hashResolverService.ResolveHash(oldChunk.PathHash);
                if (!newChunks.ContainsKey(oldChunk.PathHash))
                {
                    diffs.Add(new ChunkDiff { Type = ChunkDiffType.Removed, OldChunk = oldChunk, OldPath = oldPath });
                }
                else
                {
                    var newChunk = newChunks[oldChunk.PathHash];
                    if (oldChunkChecksums[oldChunk.PathHash] != newChunkChecksums[newChunk.PathHash])
                    {
                        var newPath = _hashResolverService.ResolveHash(newChunk.PathHash);
                        diffs.Add(new ChunkDiff { Type = ChunkDiffType.Modified, OldChunk = oldChunk, NewChunk = newChunk, OldPath = oldPath, NewPath = newPath });
                    }
                }
            }

            // New and Renamed
            foreach (var newChunk in newChunks.Values)
            {
                if (!oldChunks.ContainsKey(newChunk.PathHash))
                {
                    var newPath = _hashResolverService.ResolveHash(newChunk.PathHash);
                    var oldChecksum = oldChunkChecksums.FirstOrDefault(c => c.Value == newChunkChecksums[newChunk.PathHash]);
                    if (oldChecksum.Key != 0)
                    {
                        var oldPath = _hashResolverService.ResolveHash(oldChecksum.Key);
                        diffs.Add(new ChunkDiff { Type = ChunkDiffType.Renamed, OldChunk = oldChunks[oldChecksum.Key], NewChunk = newChunk, OldPath = oldPath, NewPath = newPath });
                    }
                    else
                    {
                        diffs.Add(new ChunkDiff { Type = ChunkDiffType.New, NewChunk = newChunk, NewPath = newPath });
                    }
                }
            }

            return diffs;
        }

        private async Task<Dictionary<ulong, ulong>> GetChunkChecksumsAsync(WadFile wadFile, IEnumerable<WadChunk> chunks)
        {
            var checksums = new Dictionary<ulong, ulong>();

            await Task.Run(() =>
            {
                foreach (var chunk in chunks)
                {
                    using var decompressedChunk = wadFile.LoadChunkDecompressed(chunk);
                    var checksum = System.IO.Hashing.XxHash64.HashToUInt64(decompressedChunk.Span);
                    checksums[chunk.PathHash] = checksum;
                }
            });

            return checksums;
        }
    }
}