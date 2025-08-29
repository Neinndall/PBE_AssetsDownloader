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

                // Now, sort the children within the rootVirtualNode's Children ObservableCollection
                // and return them. The TreeView will update based on this sorted list.
                var sortedChildren = rootVirtualNode.Children
                    .OrderBy(c => c.Type == NodeType.VirtualDirectory ? 0 : 1)
                    .ThenBy(c => c.Name)
                    .ToList(); // Create a new sorted list

                // Clear the existing (unsorted) children and add the sorted ones
                rootVirtualNode.Children.Clear();
                foreach (var child in sortedChildren)
                {
                    rootVirtualNode.Children.Add(child);
                }

                return sortedChildren; // Return the sorted list
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