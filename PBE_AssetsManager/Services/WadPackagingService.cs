using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LeagueToolkit.Core.Wad;
using PBE_AssetsManager.Info;

namespace PBE_AssetsManager.Services
{
    public class WadPackagingService
    {
        private readonly LogService _logService;

        public WadPackagingService(LogService logService)
        {
            _logService = logService;
        }

        public async Task CreateLeanWadPackageAsync(IEnumerable<SerializableChunkDiff> diffs, string oldPbePath, string newPbePath, string targetOldWadsPath, string targetNewWadsPath)
        {
            var diffsByWad = diffs.GroupBy(d => d.SourceWadFile);

            foreach (var wadGroup in diffsByWad)
            {
                var wadFileRelativePath = wadGroup.Key;
                _logService.LogDebug($"Processing {wadFileRelativePath} for chunk packaging...");

                // --- Handle OLD chunks ---
                string sourceOldWadPath = Path.Combine(oldPbePath, wadFileRelativePath);
                if (File.Exists(sourceOldWadPath))
                {
                    var oldChunksToSave = wadGroup
                        .Where(d => d.Type == ChunkDiffType.Modified || d.Type == ChunkDiffType.Renamed || d.Type == ChunkDiffType.Removed)
                        .Select(d => d.OldPathHash)
                        .Distinct();
                    await SaveChunksFromWadAsync(sourceOldWadPath, targetOldWadsPath, oldChunksToSave);
                }

                // --- Handle NEW chunks ---
                string sourceNewWadPath = Path.Combine(newPbePath, wadFileRelativePath);
                if (File.Exists(sourceNewWadPath))
                {
                    var newChunksToSave = wadGroup
                        .Where(d => d.Type == ChunkDiffType.Modified || d.Type == ChunkDiffType.Renamed || d.Type == ChunkDiffType.New)
                        .Select(d => d.NewPathHash)
                        .Distinct();
                    await SaveChunksFromWadAsync(sourceNewWadPath, targetNewWadsPath, newChunksToSave);
                }
            }
        }

        private async Task SaveChunksFromWadAsync(string sourceWadPath, string targetChunkPath, IEnumerable<ulong> chunkHashes)
        {
            try
            {
                using var sourceWad = new WadFile(sourceWadPath);
                await using var fs = new FileStream(sourceWadPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

                foreach (var hash in chunkHashes)
                {
                    if (sourceWad.Chunks.TryGetValue(hash, out var chunk))
                    {
                        fs.Seek(chunk.DataOffset, SeekOrigin.Begin);
                        byte[] rawChunkData = new byte[chunk.CompressedSize];
                        await fs.ReadAsync(rawChunkData, 0, rawChunkData.Length);

                        string chunkFileName = $"{chunk.PathHash:X16}.chunk";
                        string destChunkPath = Path.Combine(targetChunkPath, chunkFileName);
                        
                        Directory.CreateDirectory(targetChunkPath);
                        await File.WriteAllBytesAsync(destChunkPath, rawChunkData);
                    }
                    else
                    {
                        _logService.LogWarning($"Could not find chunk with hash {hash:X16} in {sourceWadPath}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logService.LogError(ex, $"Failed to save chunks from {sourceWadPath}");
            }
        }
    }
}
