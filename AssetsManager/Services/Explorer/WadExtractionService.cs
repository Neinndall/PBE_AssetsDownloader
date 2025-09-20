using LeagueToolkit.Core.Wad;
using AssetsManager.Services.Comparator;
using AssetsManager.Services.Core;
using AssetsManager.Utils;
using AssetsManager.Views.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using BCnEncoder.Shared;
using System.Runtime.InteropServices;
using LeagueToolkit.Core.Renderer;

namespace AssetsManager.Services.Explorer
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
                if (Path.GetExtension(fileNode.Name).Equals(".tex", StringComparison.OrdinalIgnoreCase))
                {
                    string pngFileName = Path.ChangeExtension(fileNode.Name, ".png");
                    string destFilePath = Path.Combine(destinationPath, pngFileName);
                        
                    byte[] fileData = File.ReadAllBytes(fileNode.FullPath);
                    ConvertTexToPng(fileData, destFilePath);
                }
                else
                {                    string destFilePath = Path.Combine(destinationPath, fileNode.Name);
                    File.Copy(fileNode.FullPath, destFilePath, true);
                }
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
                    using var wadFile = new WadFile(fileNode.SourceWadPath);

                    if (!wadFile.Chunks.TryGetValue(fileNode.SourceChunkPathHash, out var chunk))
                    {
                        _logService.LogWarning($"Chunk with hash {fileNode.SourceChunkPathHash:x16} not found in {fileNode.SourceWadPath}");
                        return;
                    }

                    using var decompressedDataOwner = wadFile.LoadChunkDecompressed(chunk);
                    var decompressedData = decompressedDataOwner.Span;

                    if (Path.GetExtension(fileNode.Name).Equals(".tex", StringComparison.OrdinalIgnoreCase))
                    {
                        string pngFileName = Path.ChangeExtension(fileNode.Name, ".png");
                        string destFilePath = Path.Combine(destinationPath, pngFileName);
                        ConvertTexToPng(decompressedData.ToArray(), destFilePath);
                    }
                    else
                    {
                        string destFilePath = Path.Combine(destinationPath, fileNode.Name);
                        File.WriteAllBytes(destFilePath, decompressedData.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex, $"Failed to extract virtual file: {fileNode.FullPath}");
                }
            });
        }

        private void ConvertTexToPng(byte[] texData, string destinationPngPath)
        {
            using var stream = new MemoryStream(texData);
            var texture = Texture.Load(stream);
            if (texture.Mips.Length > 0)
            {
                var mainMip = texture.Mips[0];
                var width = mainMip.Width;
                var height = mainMip.Height;
                if (mainMip.Span.TryGetSpan(out Span<ColorRgba32> pixelSpan))
                {
                    var pixelBytes = MemoryMarshal.AsBytes(pixelSpan).ToArray();
                    // Swap R and B channels
                    for (int i = 0; i < pixelBytes.Length; i += 4)
                    {
                        var r = pixelBytes[i];
                        var b = pixelBytes[i + 2];
                        pixelBytes[i] = b;
                        pixelBytes[i + 2] = r;
                    }
                    var bmp = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixelBytes, width * 4);
                    bmp.Freeze();
                    
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bmp));

                    using (var fileStream = new FileStream(destinationPngPath, FileMode.Create))
                    {
                        encoder.Save(fileStream);
                    }
                }
            }
        }
    }
}