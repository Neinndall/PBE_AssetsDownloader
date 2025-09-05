using DiffPlex.DiffBuilder.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PBE_AssetsManager.Views.Helpers
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

        public static Task<string> FormatJsonAsync(object jsonInput)
        {
            if (jsonInput == null) 
                return Task.FromResult(string.Empty);

            return Task.Run(() =>
            {
                try
                {
                    string jsonString = jsonInput is string s ? s : JsonConvert.SerializeObject(jsonInput);
                    var token = JToken.Parse(jsonString);
                    return token.ToString(Formatting.Indented);
                }
                catch (JsonReaderException)
                {
                    return jsonInput.ToString();
                }
                catch (JsonSerializationException)
                {
                    return jsonInput.ToString();
                }
            });
        }
    }
}