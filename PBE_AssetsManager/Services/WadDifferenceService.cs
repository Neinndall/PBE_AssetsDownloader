using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using BCnEncoder.Shared;
using LeagueToolkit.Core.Meta;
using LeagueToolkit.Core.Meta.Properties;
using LeagueToolkit.Core.Renderer;
using LeagueToolkit.Core.Wad;
using PBE_AssetsManager.Info;
using PBE_AssetsManager.Utils;

namespace PBE_AssetsManager.Services
{
    public class WadDifferenceService
    {
        private readonly HashResolverService _hashResolverService;
        private readonly CustomMessageBoxService _customMessageBoxService;

        public WadDifferenceService(HashResolverService hashResolverService, CustomMessageBoxService customMessageBoxService)
        {
            _hashResolverService = hashResolverService;
            _customMessageBoxService = customMessageBoxService;
        }

        public async Task<(string DataType, object OldData, object NewData, string OldPath, string NewPath)> PrepareDifferenceDataAsync(SerializableChunkDiff diff, string oldPbePath, string newPbePath)
        {
            if (string.IsNullOrEmpty(oldPbePath) || string.IsNullOrEmpty(newPbePath))
            {
                _customMessageBoxService.ShowInfo("Info", "Difference viewing is not available for results loaded from a file.");
                return ("error", null, null, null, null);
            }

            try
            {
                string extension = Path.GetExtension(diff.Path).ToLowerInvariant();
                var textExtensions = new[] { ".json", ".txt", ".lua", ".xml", ".yaml", ".yml", ".ini", ".log" };
                var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tex", ".dds" };
                var binExtensions = new[] { ".bin" };

                byte[] oldData = null;
                byte[] newData = null;

                bool isChunkBased = oldPbePath.Contains("wad_chunks");

                if (isChunkBased)
                {
                    if (diff.OldPathHash != 0)
                    {
                        string oldChunkPath = Path.Combine(oldPbePath, $"{diff.OldPathHash:X16}.chunk");
                        if (File.Exists(oldChunkPath))
                        {
                            byte[] compressedOldData = await File.ReadAllBytesAsync(oldChunkPath);
                            oldData = WadChunkUtils.DecompressChunk(compressedOldData, diff.OldCompressionType);
                        }
                    }

                    if (diff.NewPathHash != 0)
                    {
                        string newChunkPath = Path.Combine(newPbePath, $"{diff.NewPathHash:X16}.chunk");
                        if (File.Exists(newChunkPath))
                        {
                            byte[] compressedNewData = await File.ReadAllBytesAsync(newChunkPath);
                            newData = WadChunkUtils.DecompressChunk(compressedNewData, diff.NewCompressionType);
                        }
                    }
                }
                else
                {
                    if (diff.OldPathHash != 0 && File.Exists(Path.Combine(oldPbePath, diff.SourceWadFile)))
                    {
                        using var oldWad = new WadFile(Path.Combine(oldPbePath, diff.SourceWadFile));
                        if (oldWad.Chunks.TryGetValue(diff.OldPathHash, out WadChunk oldChunk))
                        {
                            using var decompressedChunk = oldWad.LoadChunkDecompressed(oldChunk);
                            oldData = decompressedChunk.Span.ToArray();
                        }
                    }

                    if (diff.NewPathHash != 0 && File.Exists(Path.Combine(newPbePath, diff.SourceWadFile)))
                    {
                        using var newWad = new WadFile(Path.Combine(newPbePath, diff.SourceWadFile));
                        if (newWad.Chunks.TryGetValue(diff.NewPathHash, out WadChunk newChunk))
                        {
                            using var decompressedChunk = newWad.LoadChunkDecompressed(newChunk);
                            newData = decompressedChunk.Span.ToArray();
                        }
                    }
                }

                if (oldData == null && diff.Type != ChunkDiffType.New)
                {
                    _customMessageBoxService.ShowWarning("Warning", "Could not load old version of the file.");
                    return ("error", null, null, null, null);
                }

                if (newData == null && diff.Type != ChunkDiffType.Removed)
                {
                     _customMessageBoxService.ShowWarning("Warning", "Could not load new version of the file.");
                    return ("error", null, null, null, null);
                }

                if (textExtensions.Contains(extension) || binExtensions.Contains(extension))
                {
                    string oldText = null, newText = null;
                    if (oldData != null)
                    {
                        if (binExtensions.Contains(extension))
                        {
                            await _hashResolverService.LoadBinHashesAsync();
                            var options = new JsonSerializerOptions { WriteIndented = true };
                            using var oldStream = new MemoryStream(oldData);
                            var oldBin = new BinTree(oldStream);
                            var oldDict = ConvertBinTreeToDictionary(oldBin, _hashResolverService);
                            oldText = JsonSerializer.Serialize(oldDict, options);
                        }
                        else
                        {
                            oldText = Encoding.UTF8.GetString(oldData);
                        }
                    }

                    if (newData != null)
                    {
                        if (binExtensions.Contains(extension))
                        {
                            await _hashResolverService.LoadBinHashesAsync();
                            var options = new JsonSerializerOptions { WriteIndented = true };
                            using var newStream = new MemoryStream(newData);
                            var newBin = new BinTree(newStream);
                            var newDict = ConvertBinTreeToDictionary(newBin, _hashResolverService);
                            newText = JsonSerializer.Serialize(newDict, options);
                        }
                        else
                        {
                            newText = Encoding.UTF8.GetString(newData);
                        }
                    }
                    
                    return ("json", oldText, newText, diff.OldPath, diff.NewPath);
                }
                else if (imageExtensions.Contains(extension))
                {
                    var oldImage = ToBitmapSource(oldData, extension);
                    var newImage = ToBitmapSource(newData, extension);

                    if (oldImage == null && diff.Type != ChunkDiffType.New) return ("error", null, null, null, null);
                    if (newImage == null && diff.Type != ChunkDiffType.Removed) return ("error", null, null, null, null);

                    return ("image", oldImage, newImage, diff.OldPath, diff.NewPath);
                }
                else
                {
                    _customMessageBoxService.ShowInfo("Info", $"File type '{extension}' is not supported for comparison.");
                    return ("unsupported", null, null, null, null);
                }
            }
            catch (Exception ex)
            {
                _customMessageBoxService.ShowError("Error", $"An error occurred while preparing the diff view: {ex.Message}");
                return ("error", null, null, null, null);
            }
        }

        private BitmapSource ToBitmapSource(byte[] data, string extension)
        {
            if (data == null || data.Length == 0) return null;

            if (extension == ".tex" || extension == ".dds")
            {
                using (var stream = new MemoryStream(data))
                {
                    var texture = LeagueToolkit.Core.Renderer.Texture.Load(stream);
                    if (texture.Mips.Length == 0) return null;

                    var mainMip = texture.Mips[0];
                    var width = mainMip.Width;
                    var height = mainMip.Height;

                    if (mainMip.Span.TryGetSpan(out Span<ColorRgba32> pixelSpan))
                    {
                        var pixelByteSpan = MemoryMarshal.AsBytes(pixelSpan);
                        return BitmapSource.Create(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, pixelByteSpan.ToArray(), width * 4);
                    }

                    return null; // Should not happen with how Texture is created
                }
            }
            else
            {
                using (var stream = new MemoryStream(data))
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                    return bitmapImage;
                }
            }
        }

        private object ConvertPropertyToValue(BinTreeProperty prop, HashResolverService hashResolver)
        {
            if (prop == null) return null;
            return prop.Type switch
            {
                BinPropertyType.String => ((BinTreeString)prop).Value,
                BinPropertyType.Hash => hashResolver.ResolveBinHashGeneral(((BinTreeHash)prop).Value),
                BinPropertyType.I8 => ((BinTreeI8)prop).Value,
                BinPropertyType.U8 => ((BinTreeU8)prop).Value,
                BinPropertyType.I16 => ((BinTreeI16)prop).Value,
                BinPropertyType.U16 => ((BinTreeU16)prop).Value,
                BinPropertyType.I32 => ((BinTreeI32)prop).Value,
                BinPropertyType.U32 => ((BinTreeU32)prop).Value,
                BinPropertyType.I64 => ((BinTreeI64)prop).Value,
                BinPropertyType.U64 => ((BinTreeU64)prop).Value,
                BinPropertyType.F32 => ((BinTreeF32)prop).Value,
                BinPropertyType.Bool => ((BinTreeBool)prop).Value,
                BinPropertyType.BitBool => ((BinTreeBitBool)prop).Value,
                BinPropertyType.Vector2 => ((BinTreeVector2)prop).Value,
                BinPropertyType.Vector3 => ((BinTreeVector3)prop).Value,
                BinPropertyType.Vector4 => ((BinTreeVector4)prop).Value,
                BinPropertyType.Matrix44 => ((BinTreeMatrix44)prop).Value,
                BinPropertyType.Color => ((BinTreeColor)prop).Value,
                BinPropertyType.ObjectLink => hashResolver.ResolveBinHashGeneral(((BinTreeObjectLink)prop).Value),
                BinPropertyType.WadChunkLink => hashResolver.ResolveHash(((BinTreeWadChunkLink)prop).Value),
                BinPropertyType.Container => ((BinTreeContainer)prop).Elements.Select(p => ConvertPropertyToValue(p, hashResolver)).ToList(),
                BinPropertyType.UnorderedContainer => ((BinTreeUnorderedContainer)prop).Elements.Select(p => ConvertPropertyToValue(p, hashResolver)).ToList(),
                BinPropertyType.Struct => ((BinTreeStruct)prop).Properties.ToDictionary(kvp => hashResolver.ResolveBinHashGeneral(kvp.Key), kvp => ConvertPropertyToValue(kvp.Value, hashResolver)),
                BinPropertyType.Embedded => ((BinTreeEmbedded)prop).Properties.ToDictionary(kvp => hashResolver.ResolveBinHashGeneral(kvp.Key), kvp => ConvertPropertyToValue(kvp.Value, hashResolver)),
                BinPropertyType.Optional => ConvertPropertyToValue(((BinTreeOptional)prop).Value, hashResolver),
                BinPropertyType.Map => ((BinTreeMap)prop).ToDictionary(kvp => ConvertPropertyToValue(kvp.Key, hashResolver), kvp => ConvertPropertyToValue(kvp.Value, hashResolver)),
                _ => new Dictionary<string, object> { { "Type", prop.Type }, { "NameHash", hashResolver.ResolveBinHashGeneral(prop.NameHash) } }
            };
        }

        private Dictionary<string, object> ConvertBinTreeToDictionary(BinTree binTree, HashResolverService hashResolver)
        {
            var dict = new Dictionary<string, object>
            {
                ["IsOverride"] = binTree.IsOverride,
                ["Dependencies"] = binTree.Dependencies.Select(depHashString => {
                    if (uint.TryParse(depHashString, System.Globalization.NumberStyles.HexNumber, null, out uint depHashUint))
                    {
                        return hashResolver.ResolveBinHashGeneral(depHashUint);
                    }
                    return depHashString;
                }).ToList(),
                ["Objects"] = binTree.Objects.ToDictionary(
                    kvp => hashResolver.ResolveBinHashGeneral(kvp.Key),
                    kvp => new Dictionary<string, object>
                    {
                        ["Type"] = hashResolver.ResolveBinType(kvp.Value.ClassHash),
                        ["Path"] = hashResolver.ResolveBinHashGeneral(kvp.Value.PathHash),
                        ["Properties"] = kvp.Value.Properties.ToDictionary(
                            propKvp => hashResolver.ResolveBinHashGeneral(propKvp.Key),
                            propKvp => ConvertPropertyToValue(propKvp.Value, hashResolver)
                        )
                    }
                )
            };
            return dict;
        }
    }
}