using LeagueToolkit.Core.Mesh;
using PBE_AssetsManager.Views.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Collections.ObjectModel;
using LeagueToolkit.Core.Renderer;
using LeagueToolkit.Toolkit;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Windows;

namespace PBE_AssetsManager.Services
{
    public class ModelLoadingService
    {
        private readonly LogService _logService;

        public ModelLoadingService(LogService logService)
        {
            _logService = logService;
        }

        public SceneModel LoadModel(string filePath)
        {
            try
            {
                SkinnedMesh skinnedMesh = SkinnedMesh.ReadFromSimpleSkin(filePath);
                string modelDirectory = Path.GetDirectoryName(filePath);

                if (string.IsNullOrEmpty(modelDirectory))
                {
                    _logService.LogError("Could not determine the model directory.");
                    return null;
                }

                var loadedTextures = new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);
                string[] textureFiles = Directory.GetFiles(modelDirectory, "*.tex", SearchOption.TopDirectoryOnly);
                textureFiles = textureFiles.Concat(Directory.GetFiles(modelDirectory, "*.dds", SearchOption.TopDirectoryOnly)).ToArray();

                foreach (string texPath in textureFiles)
                {
                    BitmapSource loadedTex = LoadTexture(texPath);
                    if (loadedTex != null) 
                    {                      
                        loadedTextures[Path.GetFileNameWithoutExtension(texPath)] = loadedTex;
                    }
                }
                _logService.Log($"Loaded model: {Path.GetFileNameWithoutExtension(filePath)}");
                return CreateSceneModel(skinnedMesh, loadedTextures, Path.GetFileNameWithoutExtension(filePath));
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Failed to load model");
                return null;
            }
        }

        private SceneModel CreateSceneModel(SkinnedMesh skinnedMesh, Dictionary<string, BitmapSource> loadedTextures, string modelName)
        {
            _logService.LogDebug("--- Displaying Model ---");
            _logService.LogDebug($"Available texture keys: {string.Join(", ", loadedTextures.Keys)}");

            var sceneModel = new SceneModel { Name = modelName, SkinnedMesh = skinnedMesh };
            var availableTextureNames = new ObservableCollection<string>(loadedTextures.Keys);

            string defaultTextureKey = loadedTextures.Keys
                .Where(k => k.EndsWith("_tx_cm", StringComparison.OrdinalIgnoreCase))
                .OrderBy(k => k.Length)
                .FirstOrDefault();

            foreach (var rangeObj in skinnedMesh.Ranges)
            {
                string materialName = rangeObj.Material.TrimEnd('\0');
                MeshGeometry3D meshGeometry = new MeshGeometry3D();

                var positions = skinnedMesh.VerticesView.GetAccessor(LeagueToolkit.Core.Memory.VertexElement.POSITION.Name).AsVector3Array();
                var subPositions = new Point3D[rangeObj.VertexCount];
                for(int i = 0; i < rangeObj.VertexCount; i++)
                {
                    var p = positions[rangeObj.StartVertex + i];
                    subPositions[i] = new Point3D(p.X, p.Y, p.Z);
                }
                meshGeometry.Positions = new Point3DCollection(subPositions);

                Int32Collection triangleIndices = new Int32Collection();
                var indices = skinnedMesh.Indices.Slice(rangeObj.StartIndex, rangeObj.IndexCount);
                foreach (var index in indices)
                {
                    triangleIndices.Add((int)index - rangeObj.StartVertex);
                }
                meshGeometry.TriangleIndices = triangleIndices;

                var texCoords = skinnedMesh.VerticesView.GetAccessor(LeagueToolkit.Core.Memory.VertexElement.TEXCOORD_0.Name).AsVector2Array();
                var subTexCoords = new System.Windows.Point[rangeObj.VertexCount];
                for (int i = 0; i < rangeObj.VertexCount; i++)
                {
                    var uv = texCoords[rangeObj.StartVertex + i];
                    subTexCoords[i] = new System.Windows.Point(uv.X, uv.Y);
                }
                meshGeometry.TextureCoordinates = new PointCollection(subTexCoords);

                string initialMatchingKey = FindBestTextureMatch(materialName, loadedTextures.Keys, defaultTextureKey);

                var geometryModel = new GeometryModel3D(meshGeometry, new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Colors.Magenta)));

                var modelPart = new ModelPart
                {
                    Name = string.IsNullOrEmpty(materialName) ? "Default" : materialName,
                    Visual = new ModelVisual3D(),
                    AllTextures = loadedTextures,
                    AvailableTextureNames = availableTextureNames,
                    SelectedTextureName = initialMatchingKey,
                    Geometry = geometryModel
                };

                modelPart.Visual.Content = geometryModel;
                modelPart.UpdateMaterial();

                sceneModel.Parts.Add(modelPart);
                sceneModel.RootVisual.Children.Add(modelPart.Visual);
            }
            _logService.LogDebug("--- Finished displaying model ---");
            return sceneModel;
        }

        public BitmapSource LoadTexture(string textureFilePath)
        {
            try
            {
                string extension = Path.GetExtension(textureFilePath);
                using (Stream resourceStream = textureFilePath.StartsWith("pack://application:")
                    ? Application.GetResourceStream(new Uri(textureFilePath)).Stream
                    : File.OpenRead(textureFilePath))
                {
                    if (resourceStream == null) { return null; }

                    if (extension.Equals(".tex", StringComparison.OrdinalIgnoreCase) || extension.Equals(".dds", StringComparison.OrdinalIgnoreCase))
                    {
                        Texture tex = Texture.Load(resourceStream);
                        if (tex.Mips.Length > 0)
                        {
                            Image<Rgba32> imageSharp = tex.Mips[0].ToImage();
                            using (MemoryStream ms = new MemoryStream())
                            {
                                imageSharp.SaveAsBmp(ms);
                                ms.Position = 0;
                                BitmapImage bitmapImage = new BitmapImage();
                                bitmapImage.BeginInit();
                                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                bitmapImage.StreamSource = ms;
                                bitmapImage.EndInit();
                                bitmapImage.Freeze();
                                return bitmapImage;
                            }
                        }
                        return null;
                    }
                    else
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.UriSource = new Uri(textureFilePath, UriKind.Absolute);
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();
                        return bitmapImage;
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Failed to load texture");
                return null;
            }
        }

        private string FindBestTextureMatch(string materialName, IEnumerable<string> availableTextureKeys, string defaultTextureKey)
        {
            // 1. Exact match (highest priority)
            string bestMatch = availableTextureKeys.FirstOrDefault(key => key.Equals(materialName, StringComparison.OrdinalIgnoreCase));
            if (bestMatch != null) return bestMatch;

            // 2. Keyword-based scoring match (the new generalized logic)
            var materialKeywords = materialName.ToLower().Split('_', '-', ' ');

            string bestScoringMatch = null;
            int bestScore = 0;

            foreach (string key in availableTextureKeys)
            {
                string lowerKey = key.ToLower();
                int currentScore = 0;

                foreach (string keyword in materialKeywords)
                {
                    if (string.IsNullOrWhiteSpace(keyword)) continue;
                    if (lowerKey.Contains(keyword))
                    {
                        currentScore++;
                    }
                }

                if (currentScore > bestScore)
                {
                    bestScore = currentScore;
                    bestScoringMatch = key;
                }
                // Tie-breaking: if scores are equal, prefer the shorter key name as it's likely more specific
                else if (currentScore > 0 && currentScore == bestScore)
                {
                    if (bestScoringMatch == null || key.Length < bestScoringMatch.Length)
                    {
                        bestScoringMatch = key;
                    }
                }
            }

            if (bestScoringMatch != null)
            {
                return bestScoringMatch;
            }

            // Fallback
            return defaultTextureKey;
        }
    }
}
