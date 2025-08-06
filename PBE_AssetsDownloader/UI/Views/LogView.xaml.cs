using System.Windows.Controls;
using System.Windows;
using Material.Icons;
using System;
using Material.Icons.WPF;
using System.Windows.Media.Animation;

namespace PBE_AssetsDownloader.UI.Views
{
  public partial class LogView : UserControl
  {
    // Propiedades públicas para que MainWindow pueda acceder a los controles internos
    public RichTextBox LogRichTextBox => richTextBoxLogs;
    public ScrollViewer LogScrollViewerControl => LogScrollViewer;

    // Dependency Property for Progress Message
    public static readonly DependencyProperty ProgressMessageProperty =
        DependencyProperty.Register("ProgressMessage", typeof(string), typeof(LogView), new PropertyMetadata(string.Empty, OnProgressMessageChanged));

    public string ProgressMessage
    {
      get { return (string)GetValue(ProgressMessageProperty); }
      set { SetValue(ProgressMessageProperty, value); }
    }

    // Dependency Property for Progress Visibility
    public static readonly DependencyProperty IsProgressVisibleProperty =
        DependencyProperty.Register("IsProgressVisible", typeof(bool), typeof(LogView), new PropertyMetadata(false, OnIsProgressVisibleChanged));

    public bool IsProgressVisible
    {
      get { return (bool)GetValue(IsProgressVisibleProperty); }
      set { SetValue(IsProgressVisibleProperty, value); }
    }

    // Dependency Property for Progress Icon Kind
    public static readonly DependencyProperty ProgressIconKindProperty =
        DependencyProperty.Register("ProgressIconKind", typeof(MaterialIconKind), typeof(LogView), new PropertyMetadata(MaterialIconKind.Loading));

    public MaterialIconKind ProgressIconKind
    {
      get { return (MaterialIconKind)GetValue(ProgressIconKindProperty); }
      set { SetValue(ProgressIconKindProperty, value); }
    }

    // Event to request progress details window
    public event Action ProgressDetailsRequested;

    private Storyboard _spinningIconAnimationStoryboard;

    public LogView()
    {
      InitializeComponent();
      // No need for SetBinding here, DP callbacks handle it.
    }

    private static void OnProgressMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (LogView)d;
        // No longer updating a TextBlock in the LogView for progress summary
        // control.ProgressMessageTextBlock.Text = (string)e.NewValue;
    }

    private static void OnIsProgressVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (LogView)d;
        bool isVisible = (bool)e.NewValue;
        control.ProgressSummaryButton.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed; // Control ProgressSummaryButton directly

        if (isVisible)
        {
            // Get the storyboard and set target when it becomes visible
            if (control._spinningIconAnimationStoryboard == null)
            {
                var originalStoryboard = (Storyboard)control.FindResource("SpinningIconAnimation");
                if (originalStoryboard != null)
                {
                    control._spinningIconAnimationStoryboard = originalStoryboard.Clone(); // Always clone
                    Storyboard.SetTarget(control._spinningIconAnimationStoryboard, control.ProgressIcon);
                }
            }
            control._spinningIconAnimationStoryboard?.Begin();
        }
        else
        {
            control._spinningIconAnimationStoryboard?.Stop();
            control._spinningIconAnimationStoryboard = null; // Clear the reference when stopped
        }
    }

    private void ProgressSummaryButton_Click(object sender, RoutedEventArgs e)
    {
        ProgressDetailsRequested?.Invoke();
    }
  }

  public class BooleanToVisibilityConverter : System.Windows.Data.IValueConverter
  {
      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
          return (bool)value ? Visibility.Visible : Visibility.Collapsed;
      }

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
          return (Visibility)value == Visibility.Visible;
      }
  }
}
