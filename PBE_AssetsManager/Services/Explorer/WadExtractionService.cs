using LeagueToolkit.Core.Wad;
using PBE_AssetsManager.Services.Comparator;
using PBE_AssetsManager.Services.Core;
using PBE_AssetsManager.Utils;
using PBE_AssetsManager.Views.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PBE_AssetsManager.Services.Explorer
{
    public class WadExtractionService
    {
        private readonly LogService _logService;
        private readonly WadNodeLoaderService _wadNodeLoaderService;

        public WadExtractionService(LogService logService, WadNodeLoaderService wadNodeLoaderService)
        {
            _logService = logService;
            _wadNodeLoaderService = wadNodeLoaderService;
        }

        public async Task ExtractNodeAsync(FileSystemNodeModel node, string destinationPath)
        {
            switch (node.Type)
            {
                case NodeType.VirtualFile:
                    await ExtractVirtualFileAsync(node, destinationPath);
                    break;
                case NodeType.RealFile:
                    ExtractRealFile(node, destinationPath);
                    break;
                case NodeType.VirtualDirectory:
                case NodeType.WadFile:
                    await ExtractVirtualDirectoryAsync(node, destinationPath);
                    break;
                case NodeType.RealDirectory:
                    await ExtractRealDirectoryAsync(node, destinationPath);
                    break;
            }
        }

        private async Task ExtractVirtualDirectoryAsync(FileSystemNodeModel dirNode, string destinationPath)
        {
            string newDirPath = Path.Combine(destinationPath, dirNode.Name);
            Directory.CreateDirectory(newDirPath);

            // If children are not loaded (i.e., it's the dummy node), load them.
            if (dirNode.Children.Count == 1 && dirNode.Children[0].Name == "Loading...")
            {
                var loadedChildren = await _wadNodeLoaderService.LoadChildrenAsync(dirNode);
                dirNode.Children.Clear(); // Remove dummy node
                foreach(var child in loadedChildren)
                {
                    dirNode.Children.Add(child);
                }
            }

            // Now, recursively call ExtractNodeAsync on the actual children.
            foreach (var childNode in dirNode.Children)
            {
                await ExtractNodeAsync(childNode, newDirPath);
            }
        }

        private async Task ExtractRealDirectoryAsync(FileSystemNodeModel dirNode, string destinationPath)
        {
            string newDirPath = Path.Combine(destinationPath, dirNode.Name);
            Directory.CreateDirectory(newDirPath);

            var subDirectories = Directory.GetDirectories(dirNode.FullPath);
            foreach (var dirPath in subDirectories)
            {
                var childNode = new FileSystemNodeModel(dirPath);
                await ExtractNodeAsync(childNode, newDirPath);
            }

            var files = Directory.GetFiles(dirNode.FullPath);
            foreach (var filePath in files)
            {
                var childNode = new FileSystemNodeModel(filePath);
                await ExtractNodeAsync(childNode, newDirPath);
            }
        }

        private void ExtractRealFile(FileSystemNodeModel fileNode, string destinationPath)
        {
            try
            {
                string destFilePath = Path.Combine(destinationPath, fileNode.Name);
                File.Copy(fileNode.FullPath, destFilePath, true);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, $"Failed to extract real file: {fileNode.FullPath}");
            }
        }

        private Task ExtractVirtualFileAsync(FileSystemNodeModel fileNode, string destinationPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    string destFilePath = Path.Combine(destinationPath, fileNode.Name);

                    using var wadFile = new WadFile(fileNode.SourceWadPath);

                    if (!wadFile.Chunks.TryGetValue(fileNode.SourceChunkPathHash, out var chunk))
                    {
                        _logService.LogWarning($"Chunk with hash {fileNode.SourceChunkPathHash:x16} not found in {fileNode.SourceWadPath}");
                        return;
                    }

                    using var decompressedData = wadFile.LoadChunkDecompressed(chunk);

                    File.WriteAllBytes(destFilePath, decompressedData.Span.ToArray());
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex, $"Failed to extract virtual file: {fileNode.FullPath}");
                }
            });
        }
    }
}
