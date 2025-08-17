using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using Material.Icons;
using PBE_AssetsDownloader.Services;

namespace PBE_AssetsDownloader.UI.Views.Help
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
                _logService.LogError("Failed to load or parse changelog.txt. See application_errors.log for details.");
                _logService.LogCritical(ex, "ChangelogsView.LoadChangelogs Exception");
            }
        }

        private string LoadEmbeddedChangelog()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "PBE_AssetsDownloader.changelogs.txt";
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
            var versionBlocks = text.Split(new[] { ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var block in versionBlocks)
            {
                var lines = block.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 0) continue;

                var version = new ChangelogVersion { Version = lines[0].Trim() };
                ChangeGroup currentGroup = null;

                foreach (var line in lines.Skip(1))
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("["))
                    {
                        var title = trimmedLine.Trim('[', ']');
                        currentGroup = new ChangeGroup { Title = title };
                        version.Groups.Add(currentGroup);

                        (currentGroup.Icon, currentGroup.IconColor) = title switch
                        {
                            "NEW" => (MaterialIconKind.Star, new SolidColorBrush(Colors.Gold)),
                            "IMPROVEMENTS" => (MaterialIconKind.RocketLaunch, new SolidColorBrush(Colors.LightBlue)),
                            "BUG FIXES" => (MaterialIconKind.Bug, new SolidColorBrush(Colors.OrangeRed)),
                            "CHANGES" => (MaterialIconKind.Palette, new SolidColorBrush(Colors.LightGreen)),
                            _ => (MaterialIconKind.Pencil, (SolidColorBrush)System.Windows.Application.Current.FindResource("TextSecondary"))
                        };
                    }
                    else if (currentGroup != null && !string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        currentGroup.Changes.Add(trimmedLine);
                    }
                }
                versions.Add(version);
            }
            return versions;
        }
    }
}