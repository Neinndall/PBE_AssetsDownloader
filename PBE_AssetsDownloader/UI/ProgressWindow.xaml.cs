// PBE_AssetsDownloader/ProgressWindow.xaml.cs

using System;
using System.Windows; // For Window, RoutedEventArgs
using System.Windows.Controls; // For ProgressBar, TextBlock (WPF equivalent of Label)

namespace PBE_AssetsDownloader.UI
{
  /// <summary>
  /// Interaction logic for ProgressWindow.xaml
  /// </summary>
  public partial class ProgressWindow : Window
  {
    public ProgressWindow()
    {
      InitializeComponent();
      // Optional: Set initial text/progress here if needed, or rely on SetProgress
      // this.Loaded += (s, e) => SetProgress(0, "Starting operation...");
    }

    /// <summary>
    /// Safely updates the progress bar and associated text in the window.
    /// </summary>
    /// <param name="progress">The progress value to set (0-100).</param>
    /// <param name="labelText">The text to display in the label (optional).</param>
    public void SetProgress(int progress, string labelText = "")
    {
      try
      {
        // In WPF, we use Dispatcher.Invoke or Dispatcher.BeginInvoke
        // to update UI elements from a non-UI thread.
        // CheckAccess() tells us if we're on the UI thread.
        if (!Dispatcher.CheckAccess())
        {
          // If we are on a different thread, invoke the method on the UI thread
          // Dispatcher.Invoke is synchronous, Dispatcher.BeginInvoke is asynchronous.
          // For progress updates, Invoke is usually fine unless performance is an issue
          // or you want non-blocking updates.
          Dispatcher.Invoke(new Action<int, string>(SetProgress), progress, labelText);
        }
        else
        {
          // If we are already on the UI thread, update the controls directly
          progressBar.Value = Math.Max(0, Math.Min(progress, 100));

          // Update the TextBlock (WPF's equivalent of Label)
          textBlockProgress.Text = labelText;
        }
      }
      catch (InvalidOperationException ex) when (ex.Message.Contains("Dispatcher processing has been suspended"))
      {
        // This can happen if the window is being closed rapidly
        // while an update is trying to be dispatched.
        // It's the WPF equivalent of ObjectDisposedException for UI elements.
        // We handle the error silently as the form is no longer available.
      }
      catch (Exception)
      {
        // Catch any other unexpected exceptions during UI update
        // For a progress window, silent handling might be acceptable.
      }
    }
  }
}
