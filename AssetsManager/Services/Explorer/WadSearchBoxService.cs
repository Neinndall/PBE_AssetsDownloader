
using AssetsManager.Views.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AssetsManager.Services.Explorer
{
    public class WadSearchBoxService
    {
        public async Task FilterTreeAsync(ObservableCollection<FileSystemNodeModel> nodes, string searchText)
        {
            await Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    SetVisibility(nodes, true);
                    return;
                }

                FilterNodes(nodes, searchText);
            });
        }

        private bool FilterNodes(IEnumerable<FileSystemNodeModel> nodes, string searchText)
        {
            bool somethingVisibleInThisLevel = false;
            if (nodes == null) return false;

            foreach (var node in nodes)
            {
                if (node.Name == "Loading...")
                {
                    node.IsVisible = false;
                    continue;
                }

                bool selfMatches = node.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                bool childMatches = FilterNodes(node.Children, searchText);

                node.IsVisible = selfMatches || childMatches;

                if (selfMatches)
                {
                    var index = node.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
                    var length = searchText.Length;

                    node.PreMatch = node.Name.Substring(0, index);
                    node.Match = node.Name.Substring(index, length);
                    node.PostMatch = node.Name.Substring(index + length);
                    node.HasMatch = true;
                }
                else
                {
                    node.HasMatch = false;
                    node.PreMatch = null;
                    node.Match = null;
                    node.PostMatch = null;
                }

                if (node.IsVisible)
                {
                    somethingVisibleInThisLevel = true;
                }
            }
            return somethingVisibleInThisLevel;
        }

        private void SetVisibility(IEnumerable<FileSystemNodeModel> nodes, bool isVisible)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                node.IsVisible = isVisible;
                node.HasMatch = false;
                node.PreMatch = null;
                node.Match = null;
                node.PostMatch = null;

                if (!isVisible)
                {
                    node.IsExpanded = false;
                }
                SetVisibility(node.Children, isVisible);
            }
        }
    }
}
