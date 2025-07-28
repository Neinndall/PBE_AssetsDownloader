// PBE_AssetsDownloader/HelpWindow.xaml.cs
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows; // Para Window, MessageBox
using System.Windows.Controls; // Para TabControl, RichTextBox, Button
using System.Windows.Documents; // Para FlowDocument, Paragraph, Run, TextPointer, etc.
using System.Windows.Media; // Para Color, Brushes (equivalente a System.Drawing.Color)
using PBE_AssetsDownloader.Services;
using PBE_AssetsDownloader.Utils;

namespace PBE_AssetsDownloader.UI // Asegúrate de que el namespace sea el correcto
{
  /// <summary>
  /// Interaction logic for HelpWindow.xaml
  /// </summary>
  public partial class HelpWindow : Window
  {
    private readonly LogService _logService;

    private const string EmbeddedChangelogResource = "PBE_AssetsDownloader.changelogs.txt";
    // Asegúrate de que el archivo changelogs.txt esté marcado como "Embedded Resource" en sus propiedades de compilación.

    public HelpWindow(LogService logService)
    {
      InitializeComponent();

      _logService = logService ?? throw new ArgumentNullException(nameof(logService));

      // Cargar textos y aplicar formato al abrir la ventana
      this.Loaded += HelpWindow_Loaded;
    }

    private void HelpWindow_Loaded(object sender, RoutedEventArgs e)
    {
      string aboutText = GetAboutText();
      FormatRichText(richTextBoxAbout, aboutText);

      string changelogText = LoadEmbeddedChangelog();
      FormatRichText(richTextBoxChangelogs, changelogText);
    }

    private string LoadEmbeddedChangelog()
    {
      try
      {
        var assembly = Assembly.GetExecutingAssembly();
        // Construye el nombre completo del recurso.
        // Si el changelogs.txt está en una subcarpeta (ej. 'Resources'), el nombre sería 'PBE_AssetsDownloader.Resources.changelogs.txt'
        // Asegúrate de que el "Build Action" de changelogs.txt sea "Embedded Resource"
        using Stream stream = assembly.GetManifestResourceStream(EmbeddedChangelogResource);
        if (stream == null) return "Changelog resource not found. Check if the 'Build Action' is set to 'Embedded Resource' and the path is correct.";
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
      }
      catch (Exception ex)
      {
        return $"Error reading changelog resource: {ex.Message}";
      }
    }

    private void FormatRichText(RichTextBox box, string content)
    {
      // Limpiar y preparar el FlowDocument del RichTextBox
      box.Document.Blocks.Clear();
      Paragraph currentParagraph = new Paragraph();
      currentParagraph.Margin = new Thickness(4, 0, 0, 0); // Simulate SelectionIndent = 4

      // Añadir una línea en blanco al principio, similar a Environment.NewLine
      currentParagraph.Inlines.Add(new Run(Environment.NewLine));

      foreach (var rawLine in content.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
      {
        var line = rawLine.TrimStart();
        bool isTitle = line.EndsWith(":");
        bool isHeader = line.StartsWith("PBE_AssetsDownloader");

        TextPointer startPointer = currentParagraph.ContentEnd;

        // En WPF, aplicamos el formato a Run (segmentos de texto) y luego los añadimos al Paragraph.
        // O creamos un nuevo Paragraph si el estilo lo requiere.
        if (line.Contains("[NEW]") || line.Contains("[IMPROVEMENTS]") || line.Contains("[BUG FIXES]") || line.Contains("[Major Update]"))
        {
          ApplyStyleWPF(currentParagraph, line, FontWeights.Bold, Brushes.Black, bullet: false);
        }
        else if (isHeader)
        {
          ApplyStyleWPF(currentParagraph, line, FontWeights.Bold, Brushes.DarkBlue, bullet: false);
        }
        else if (isTitle)
        {
          ApplyStyleWPF(currentParagraph, line, FontWeights.Bold, Brushes.Black, bullet: false);
        }
        else if (line.StartsWith("•"))
        {
          ApplyStyleWPF(currentParagraph, line, FontWeights.Normal, Brushes.Black, bullet: true);
        }
        else if (char.IsDigit(line.FirstOrDefault()) && line.Contains("."))
        {
          ApplyStyleWPF(currentParagraph, line, FontWeights.Normal, Brushes.DarkSlateGray, bullet: false);
        }
        else
        {
          ApplyStyleWPF(currentParagraph, line, FontWeights.Normal, Brushes.Black, bullet: false);
        }
      }

      box.Document.Blocks.Add(currentParagraph); // Asegurarse de añadir el último párrafo
    }

    // Helper para aplicar estilos en WPF RichTextBox
    private void ApplyStyleWPF(Paragraph paragraph, string text, FontWeight fontWeight, SolidColorBrush color, bool bullet)
    {
      // Si el párrafo actual ya tiene texto o está formateado de manera diferente
      // y el nuevo texto tiene un estilo diferente (o es un bullet), es mejor crear un nuevo párrafo.
      // Esto es una simplificación; para un control de formato exacto, necesitarías
      // manejar Runs dentro de Inlines más granularmente o incluso crear Paragraphs para cada línea.

      // Para simular el comportamiento de WinForms RichTextBox, donde cada línea se añade y luego se formatea,
      // vamos a añadir el texto y luego aplicar el formato a ese segmento.

      // Crear un nuevo Run para el texto
      Run run = new Run(text + Environment.NewLine);
      run.FontWeight = fontWeight;
      run.Foreground = color;

      if (bullet)
      {
        // Si es un bullet, creamos un ListItem (requiere un ListBlock) o un Paragraph con indentación y un punto.
        // La forma más simple de simular bullets sin ListBlock es con indentación y el carácter •
        // ya que el texto ya comienza con "•".
        // Ajustamos el margen del párrafo para la indentación si es un bullet.
        // Nota: Esto es un poco más complejo si realmente quieres el comportamiento de ListItems.
        // Para simplificar, simplemente añadimos el Run al párrafo actual y confiamos en el "•" en el texto.
        // Si quisieras ListItems reales, la estructura del FlowDocument debería ser un List.
      }

      paragraph.Inlines.Add(run);
    }

    private string GetAboutText()
    {
      return string.Join(Environment.NewLine, new[]
      {
                "Description:",
                "This app is designed to automatically download and manage new assets from League of Legends PBE server updates. " +
                "It helps content creators and developers stay up-to-date with the latest changes and additions to the game.",
                "",
                "Key Features:",
                "1. Automatic detection of new PBE updates.",
                "2. Downloads and organizes new game assets.",
                "3. Supports synchronization with CDTB.",
                "4. Auto-copy functionality for hash files.",
                "5. Back-Ups for hash files.",
                "6. Manually download assets from others days with differences text files.",
                "",
                "How to Use:",
                "1. Configure your desired settings in the Settings menu",
                "2. Select the directories for the old and new hashes files.",
                "3. Enable auto-copy if needed.",
                "4. The tool will automatically check for and download new PBE assets.",
                "",
                "For more information and updates, check the Changelogs section."
            });
    }

    private void buttonClose_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }

    private void buttonReportBug_Click(object sender, RoutedEventArgs e)
    {
      System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
      {
        FileName = "https://github.com/Neinndall/PBE_AssetsDownloader/issues",
        UseShellExecute = true
      });
    }

    private async void buttonCheckUpdates_Click(object sender, RoutedEventArgs e)
    {
      // Asegúrate de que UpdateManager.CheckForUpdatesAsync() exista y sea accesible
      await UpdateManager.CheckForUpdatesAsync();
    }
  }
}
