using DiffPlex.DiffBuilder.Model;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace PBE_AssetsDownloader.UI.Helpers
{
    public static class JsonDiffHelper
    {
        public static (string Text, List<ChangeType> LineTypes) NormalizeTextForAlignment(DiffPaneModel paneModel)
        {
            var lines = new List<string>();
            var lineTypes = new List<ChangeType>();

            foreach (var line in paneModel.Lines)
            {
                lines.Add(line.Type == ChangeType.Imaginary ? "" : line.Text ?? "");
                lineTypes.Add(line.Type);
            }

            return (string.Join("\r\n", lines), lineTypes);
        }

        public static string FormatJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return string.Empty;

            try
            {
                var parsed = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(parsed, Formatting.Indented);
            }
            catch
            {
                return json;
            }
        }
    }
}