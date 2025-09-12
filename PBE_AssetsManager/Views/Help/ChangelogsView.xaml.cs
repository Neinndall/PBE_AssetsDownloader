using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using Material.Icons;
using PBE_AssetsManager.Services;
using PBE_AssetsManager.Services.Core;

namespace PBE_AssetsManager.Views.Help
{
    public class ChangelogVersion
    {
        public string Version { get; set; }
        public List<ChangeGroup> Groups { get; set; } = new List<ChangeGroup>();
    }

    public class ChangeGroup
    {
        public string Title { get; set; }
        public MaterialIconKind Icon { get; set; }
        public SolidColorBrush IconColor { get; set; }
        public List<string> Changes { get; set; } = new List<string>();
    }

    public partial class ChangelogsView : UserControl
    {
        private readonly LogService _logService;

        public ChangelogsView(LogService logService)
        {
            InitializeComponent();
            _logService = logService;
            LoadChangelogs();
        }

        private void LoadChangelogs()
        {
            try
            {
                var changelogText = LoadEmbeddedChangelog();
                var changelogData = ParseChangelog(changelogText);
                ChangelogItemsControl.ItemsSource = changelogData;
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Failed to load or parse changelog.txt.");
            }
        }

        private string LoadEmbeddedChangelog()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "PBE_AssetsManager.changelogs.txt";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return string.Empty;
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private List<ChangelogVersion> ParseChangelog(string text)
        {
            var versions = new List<ChangelogVersion>();
            var versionBlocks = text.Split(
                new[] { ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>" },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var block in versionBlocks)
            {
                var lines = block.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 0) continue;

                // Primera línea: nombre y versión (ej: PBE_AssetsManager - League of Legends | v2.2.0.0)
                var version = new ChangelogVersion { Version = lines[0].Trim() };
                ChangeGroup currentGroup = null;

                foreach (var line in lines.Skip(1))
                {
                    var trimmedLine = line.Trim();

                    // Detectar títulos de secciones
                    bool isGroupTitle =
                        trimmedLine.StartsWith("[") ||
                        trimmedLine.Equals("New Features", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.Equals("Improvements", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.Equals("Changes", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.Equals("Bug Fixes", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.Equals("Notes", StringComparison.OrdinalIgnoreCase);

                    // Detectar subtítulos de update (Major, Medium, Hotfix)
                    bool isUpdateTitle =
                        trimmedLine.Equals("MAJOR UPDATE", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.Equals("MEDIUM UPDATE", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.Equals("HOTFIX UPDATE", StringComparison.OrdinalIgnoreCase);

                    if (isGroupTitle)
                    {
                        var title = trimmedLine.Trim('[', ']');
                        currentGroup = new ChangeGroup { Title = title };
                        version.Groups.Add(currentGroup);

                        (currentGroup.Icon, currentGroup.IconColor) = title switch
                        {
                            "New Features" => (MaterialIconKind.Star, new SolidColorBrush(Colors.Gold)),
                            "Improvements" => (MaterialIconKind.Flash, new SolidColorBrush(Colors.LightBlue)),
                            "Changes" => (MaterialIconKind.Build, new SolidColorBrush(Colors.LightGreen)),
                            "Bug Fixes" => (MaterialIconKind.Bug, new SolidColorBrush(Colors.OrangeRed)),
                            "Notes" => (MaterialIconKind.NotebookOutline, new SolidColorBrush(Colors.Purple)),
                            _ => (MaterialIconKind.Pencil, (SolidColorBrush)System.Windows.Application.Current.FindResource("TextSecondary"))
                        };
                    }
                    else if (isUpdateTitle)
                    {
                        // Puedes decidir si lo guardas como "grupo especial" o en otra propiedad
                        currentGroup = new ChangeGroup { Title = trimmedLine };
                        version.Groups.Add(currentGroup);

                        (currentGroup.Icon, currentGroup.IconColor) = trimmedLine.ToUpper() switch
                        {
                            "MAJOR UPDATE" => (MaterialIconKind.Rocket, new SolidColorBrush(Colors.Orange)),
                            "MEDIUM UPDATE" => (MaterialIconKind.Update, new SolidColorBrush(Colors.CornflowerBlue)),
                            "HOTFIX UPDATE" => (MaterialIconKind.Fire, new SolidColorBrush(Colors.Red)),
                            _ => (MaterialIconKind.Pencil, (SolidColorBrush)System.Windows.Application.Current.FindResource("TextSecondary"))
                        };
                    }
                    else if (currentGroup != null && !string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        // Línea normal (item de changelog)
                        currentGroup.Changes.Add(trimmedLine);
                    }
                }

                versions.Add(version);
            }
            return versions;
        }


    }
}