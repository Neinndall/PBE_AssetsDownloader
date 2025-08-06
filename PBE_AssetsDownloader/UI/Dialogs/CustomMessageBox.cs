using PBE_AssetsDownloader.UI.Dialogs;
using System.Windows;

namespace PBE_AssetsDownloader.UI.Dialogs
{
    public static class CustomMessageBox
    {
        public static bool? ShowYesNo(string title, string message, Window owner = null, CustomMessageBoxIcon icon = CustomMessageBoxIcon.Question)
        {
            var dialog = new ConfirmationDialog(title, message, CustomMessageBoxButtons.YesNo, icon)
            {
                Owner = owner
            };
            return dialog.ShowDialog();
        }

        public static void ShowInfo(string title, string message, Window owner = null, CustomMessageBoxIcon icon = CustomMessageBoxIcon.Info)
        {
            var dialog = new ConfirmationDialog(title, message, CustomMessageBoxButtons.OK, icon)
            {
                Owner = owner
            };
            dialog.ShowDialog();
        }
    }
}
