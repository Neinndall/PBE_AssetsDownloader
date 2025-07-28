using System.Windows.Forms;
using PBE_AssetsDownloader.Info;

namespace PBE_AssetsDownloader.UI
{
    partial class ProgressForm
    {
        private System.ComponentModel.IContainer components = null;
        private ProgressBar progressBar;
        private Label labelProgress;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            ApplicationInfos.SetIcon(this);
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.labelProgress = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(20, 15);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(260, 25);
            this.progressBar.TabIndex = 0;
            // 
            // labelProgress
            // 
            this.labelProgress.Location = new System.Drawing.Point(20, 45);
            this.labelProgress.Name = "labelProgress";
            this.labelProgress.Size = new System.Drawing.Size(260, 20);
            this.labelProgress.TabIndex = 1;
            this.labelProgress.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ProgressForm
            // 
            this.ClientSize = new System.Drawing.Size(300, 80);
            this.Controls.Add(this.labelProgress);
            this.Controls.Add(this.progressBar);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProgressForm";
            this.Text = "Update";
            this.ResumeLayout(false);
        }
    }
}
