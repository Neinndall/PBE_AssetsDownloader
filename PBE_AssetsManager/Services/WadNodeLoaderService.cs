using PBE_AssetsManager.Services;
using PBE_AssetsManager.Views.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeagueToolkit.Core.Wad;

namespace PBE_AssetsManager.Services
{
    public class WadNodeLoaderService
    {
        private readonly HashResolverService _hashResolverService;

        public WadNodeLoaderService(HashResolverService hashResolverService)
        {
            _hashResolverService = hashResolverService;
        }

        public async Task<List<FileSystemNodeModel>> LoadChildrenAsync(FileSystemNodeModel wadNode)
        {
            var childrenToAdd = await Task.Run(() =>
            {
                var rootVirtualNode = new FileSystemNodeModel(wadNode.Name, true, wadNode.FullPath, wadNode.FullPath);
                using (var wadFile = new WadFile(wadNode.FullPath))
                {
                    foreach (var chunk in wadFile.Chunks.Values)
                    {
                        string virtualPath = _hashResolverService.ResolveHash(chunk.PathHash);
                        if (!string.IsNullOrEmpty(virtualPath) && virtualPath != chunk.PathHash.ToString("x16"))
                        {
                            AddNodeToVirtualTree(rootVirtualNode, virtualPath, wadNode.FullPath, chunk.PathHash);
                        }
                    }
                }
                return rootVirtualNode.Children
                    .OrderBy(c => c.Type == NodeType.VirtualDirectory ? 0 : 1)
                    .ThenBy(c => c.Name)
                    .ToList();
            });

            return childrenToAdd;
        }

        private void AddNodeToVirtualTree(FileSystemNodeModel root, string virtualPath, string wadPath, ulong chunkHash)
        {
            string[] parts = virtualPath.Replace('\\', '/').Split('/');
            var currentNode = root;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                var dirName = parts[i];
                var childDir = currentNode.Children.FirstOrDefault(c => c.Name.Equals(dirName, System.StringComparison.OrdinalIgnoreCase) && c.Type == NodeType.VirtualDirectory);
                if (childDir == null)
                {
                    var newVirtualPath = string.Join("/", parts.Take(i + 1));
                    childDir = new FileSystemNodeModel(dirName, true, newVirtualPath, wadPath);
                    currentNode.Children.Add(childDir);
                }
                currentNode = childDir;
            }

            var fileNode = new FileSystemNodeModel(parts.Last(), false, virtualPath, wadPath)
            {
                SourceChunkPathHash = chunkHash
            };
            currentNode.Children.Add(fileNode);
        }
    }
}