using System.Windows;

namespace PBE_AssetsDownloader.UI.Dialogs
{
    public partial class InputDialog : Window
    {
        public string InputText
        {
            get { return textBoxInput.Text; }
            set { textBoxInput.Text = value; }
        }

        public InputDialog(string title, string question, string defaultAnswer = "")
        {
            InitializeComponent();
            Title = title;
            textBlockQuestion.Text = question;
            InputText = defaultAnswer;
            textBoxInput.Focus();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
