using AssetsManager.Services.Core;
using AssetsManager.Services.Downloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AssetsManager.Views.Controls.Export
{
    public partial class FilterConfigControl : UserControl
    {
        #region Properties
        public LogService LogService { get; set; }
        public ExportService ExportService { get; set; }
        #endregion

        #region Private Fields
        private readonly CheckBox[] _individualCheckboxes;
        #endregion

        #region Constructor
        public FilterConfigControl()
        {
            InitializeComponent();
            _individualCheckboxes = new[] { chkImages, chkAudios, chkPlugins, chkGame };
            
            Loaded += OnControlLoaded;
            SetupCheckboxEvents();
        }
        #endregion

        #region Event Handlers
        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            // Ensure the service is updated with the default values set in XAML.
            UpdateExportService();
        }

        private void SetupCheckboxEvents()
        {
            chkAll.Checked += OnAllCheckedChanged;
            chkAll.Unchecked += OnAllCheckedChanged;

            foreach (var checkbox in _individualCheckboxes)
            {
                checkbox.Checked += OnIndividualCheckedChanged;
                checkbox.Unchecked += OnIndividualCheckedChanged;
            }
        }

        private void OnAllCheckedChanged(object sender, RoutedEventArgs e)
        {
            // When "All" is checked, uncheck the others.
            if (chkAll.IsChecked == true)
            {
                SetIndividualCheckboxes(false);
            }
            // If "All" is unchecked, but no other box is checked, re-check "All".
            else if (!HasAnyIndividualCheckboxSelected())
            {
                chkAll.IsChecked = true;
            }
            
            UpdateExportService();
        }

        private void OnIndividualCheckedChanged(object sender, RoutedEventArgs e)
        {
            // If any individual box is checked, uncheck "All".
            if (HasAnyIndividualCheckboxSelected())
            {
                chkAll.IsChecked = false;
            }
            // If no individual box is checked, re-check "All".
            else
            {
                chkAll.IsChecked = true;
            }

            UpdateExportService();
        }
        #endregion

        #region Core Logic
        private void UpdateExportService()
        {
            if (ExportService == null) return;

            ExportService.SelectedAssetTypes = GetSelectedAssetTypes();
            ExportService.FilterLogic = FilterAssetsByType;
        }

        private List<string> GetSelectedAssetTypes()
        {
            var selectedTypes = new List<string>();

            if (chkAll.IsChecked.GetValueOrDefault())
            {
                selectedTypes.Add("All");
            }
            else
            {
                selectedTypes.AddRange(
                    _individualCheckboxes.Where(cb => cb.IsChecked.GetValueOrDefault())
                                         .Select(cb => cb.Tag as string)
                );
            }

            return selectedTypes;
        }

        private List<string> FilterAssetsByType(IEnumerable<string> lines, List<string> selectedTypes)
        {
            var filteredAndParsedLines = lines
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split(' ').Skip(1).FirstOrDefault())
                .Where(path => path != null)
                .ToList();

            LogService.LogDebug($"FilterAssetsByType: Total parsed lines (before type filter): {filteredAndParsedLines.Count}");

            if (selectedTypes.Any(type => type.Equals("All", StringComparison.OrdinalIgnoreCase)))
                return filteredAndParsedLines.Distinct().ToList();

            var finalFilteredAssets = filteredAndParsedLines
                .Where(path => IsPathMatchingSelectedTypes(path, selectedTypes))
                .Distinct()
                .ToList();

            if (LogService != null) LogService.LogDebug($"FilterAssetsByType: Total assets after type filter and distinct: {finalFilteredAssets.Count}");
            return finalFilteredAssets;
        }
        #endregion

        #region Helpers
        private void SetIndividualCheckboxes(bool value)
        {
            foreach (var checkbox in _individualCheckboxes)
            {
                checkbox.IsChecked = value;
            }
        }

        private bool HasAnyIndividualCheckboxSelected()
        {
            return _individualCheckboxes.Any(cb => cb.IsChecked.GetValueOrDefault());
        }

        private bool IsPathMatchingSelectedTypes(string path, List<string> selectedTypes)
        {
            var typeCheckers = new Dictionary<string, Func<string, bool>>
            {
                { "Images", p => p.EndsWith(".tex", StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith(".dds", StringComparison.OrdinalIgnoreCase) },
                { "Audios", p => p.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) },
                { "Plugins", p => p.StartsWith("plugins/", StringComparison.OrdinalIgnoreCase) },
                { "Game", p => p.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) }
            };

            foreach (var type in selectedTypes)
            {
                if (typeCheckers.TryGetValue(type, out var checker) && checker(path))
                {
                    if (LogService != null) LogService.LogDebug($"FilterAssetsByType: Path '{path}' matched '{type}'.");
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}