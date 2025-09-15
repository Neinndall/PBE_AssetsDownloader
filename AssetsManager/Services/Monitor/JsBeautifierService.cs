using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUglify;
using NUglify.JavaScript;

namespace AssetsManager.Services.Monitor
{
    public sealed class JsBeautifierService : IDisposable
    {
        private readonly CodeSettings _beautifySettings;
        private bool _disposed = false;

        public JsBeautifierService()
        {
            // Configuración optimizada para embellecimiento
            _beautifySettings = new CodeSettings
            {
                MinifyCode = false,
                OutputMode = OutputMode.MultipleLines,
                Indent = "  ",
                LocalRenaming = LocalRenaming.KeepAll,
                PreserveFunctionNames = true,
                PreserveImportantComments = true,
                RemoveUnneededCode = false,
                StripDebugStatements = false,
                CollapseToLiteral = false,
                ReorderScopeDeclarations = false,
                RemoveFunctionExpressionNames = false
            };
        }

        /// <summary>
        /// Embellece código JavaScript de forma asíncrona.
        /// </summary>
        public Task<string> BeautifyAsync(string jsContent)
        {
            if (string.IsNullOrWhiteSpace(jsContent))
                return Task.FromResult(string.Empty);

            return Task.Run(() =>
            {
                try
                {
                    // Paso 1: Intentar embellecer el código directamente con NUglify.
                    var beautified = Uglify.Js(jsContent, _beautifySettings);
                    
                    if (!beautified.HasErrors && !string.IsNullOrEmpty(beautified.Code))
                    {
                        return beautified.Code;
                    }
                    
                    // Fallback: Si NUglify falla, usar la beautificación básica manual.
                    return ApplyBasicBeautification(jsContent);
                }
                catch
                {
                    // Fallback final para cualquier excepción inesperada.
                    return ApplyBasicBeautification(jsContent);
                }
            });
        }

        

        /// <summary>
        /// Beautificación básica y rápida cuando NUglify falla
        /// </summary>
        private string ApplyBasicBeautification(string jsContent)
        {
            if (string.IsNullOrWhiteSpace(jsContent))
                return string.Empty;

            var result = jsContent;
            
            // Transformaciones básicas y rápidas
            result = Regex.Replace(result, @";(?!\s*[\r\n])", ";\n");              // ; + nueva línea
            result = Regex.Replace(result, @"\{(?!\s*[\r\n])", "{\n");             // { + nueva línea  
            result = Regex.Replace(result, @"\}(?!\s*[,;\r\n])", "}\n");           // } + nueva línea
            result = Regex.Replace(result, @",(?=\s*[\w""])", ",\n");              // , + nueva línea
            result = Regex.Replace(result, @"(\w+)=function\(", "$1 = function("); // espacios en funciones
            
            // Aplicar indentación básica
            return ApplySimpleIndentation(result);
        }

        /// <summary>
        /// Indentación simple y eficiente
        /// </summary>
        private string ApplySimpleIndentation(string jsContent)
        {
            var lines = jsContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new StringBuilder(jsContent.Length + (lines.Length * 4));
            var indentLevel = 0;
            const int MAX_INDENT_LEVEL = 6; // Máximo 6 niveles = 12 espacios

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                // Reducir indentación ANTES de mostrar líneas de cierre
                if (IsClosingLine(trimmed))
                {
                    indentLevel = Math.Max(0, indentLevel - 1);
                }

                // Aplicar indentación: mínimo 1, máximo 4 niveles
                var currentIndent = Math.Max(1, Math.Min(indentLevel, MAX_INDENT_LEVEL)) * 2;
                result.AppendLine(new string(' ', currentIndent) + trimmed);

                // Aumentar indentación DESPUÉS de mostrar líneas de apertura
                if (IsOpeningLine(trimmed))
                {
                    indentLevel++;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Determina si una línea es de cierre
        /// </summary>
        private bool IsClosingLine(string line)
        {
            return line.StartsWith("}") || 
                   line.StartsWith("]") || 
                   line.StartsWith(");") ||
                   line.StartsWith("})") ||
                   line == "}," ||
                   line == "]," ||
                   line == ");";
        }

        /// <summary>
        /// Determina si una línea es de apertura
        /// </summary>
        private bool IsOpeningLine(string line)
        {
            return line.EndsWith("{") || 
                   line.EndsWith("[") ||
                   line.EndsWith("({") ||
                   (line.Contains("function") && line.EndsWith("{")) ||
                   line.EndsWith("=> {");
        }

        

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}