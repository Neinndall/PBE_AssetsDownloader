using System;
using System.Windows.Forms;

namespace PBE_AssetsDownloader.UI
{
    partial class ExportForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabAssets;
        private System.Windows.Forms.CheckedListBox clbAssets;
        private System.Windows.Forms.TextBox txtDownloadTargetPath;
        private System.Windows.Forms.TextBox txtDifferencesPath;
        private System.Windows.Forms.Button btnBrowseDownloadTargetPath;
        private System.Windows.Forms.Button btnBrowseDifferencesPath;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.OpenFileDialog openFileDialogDifferences;

        private System.Windows.Forms.Label lblDownloadTargetPath;
        private System.Windows.Forms.Label lblDifferencesPath;

        private System.Windows.Forms.Button btnPreviewAssets;

        private System.Windows.Forms.RichTextBox richTextBoxLogs;

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
            this.components = new System.ComponentModel.Container();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabAssets = new System.Windows.Forms.TabPage();
            this.clbAssets = new System.Windows.Forms.CheckedListBox();
            this.txtDownloadTargetPath = new System.Windows.Forms.TextBox();
            this.txtDifferencesPath = new System.Windows.Forms.TextBox();
            this.btnBrowseDownloadTargetPath = new System.Windows.Forms.Button();
            this.btnBrowseDifferencesPath = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.openFileDialogDifferences = new System.Windows.Forms.OpenFileDialog();

            this.lblDownloadTargetPath = new System.Windows.Forms.Label();
            this.lblDifferencesPath = new System.Windows.Forms.Label();

            this.btnPreviewAssets = new System.Windows.Forms.Button();

            this.richTextBoxLogs = new System.Windows.Forms.RichTextBox();

            this.tabControl.SuspendLayout();
            this.tabAssets.SuspendLayout();
            this.SuspendLayout();

            // tabControl
            this.tabControl.Controls.Add(this.tabAssets);
            this.tabControl.Location = new System.Drawing.Point(20, 20);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(540, 423);  // Ajustado para tener margen
            this.tabControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            // tabAssets
            this.tabAssets.Location = new System.Drawing.Point(4, 24);
            this.tabAssets.Name = "tabAssets";
            this.tabAssets.Size = new System.Drawing.Size(532, 342);  // Ajustado para tener margen también
            this.tabAssets.Text = "Manual Downloads";
            this.tabAssets.UseVisualStyleBackColor = true;

            // clbAssets
            this.clbAssets.FormattingEnabled = true;
            this.clbAssets.Items.AddRange(new object[] { "All", "Images", "Audios", "Plugins", "Game" });
            this.clbAssets.Location = new System.Drawing.Point(13, 13);
            this.clbAssets.Size = new System.Drawing.Size(500, 120);  // Aumentamos el margen lateral
            this.clbAssets.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.clbAssets.MultiColumn = true;
            this.clbAssets.ColumnWidth = 90;
            this.tabAssets.Controls.Add(this.clbAssets);

            // txtDownloadTargetPath
            this.txtDownloadTargetPath.BackColor = System.Drawing.Color.White;
            this.txtDownloadTargetPath.Location = new System.Drawing.Point(13, 160);
            this.txtDownloadTargetPath.Size = new System.Drawing.Size(400, 23);  // Aumentado el margen lateral
            this.txtDownloadTargetPath.ReadOnly = true;
            this.txtDownloadTargetPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.tabAssets.Controls.Add(this.txtDownloadTargetPath);

            // btnPreviewAssets
            this.btnPreviewAssets.Location = new System.Drawing.Point(13, 250); // Mismo Y que btnExport
            this.btnPreviewAssets.Size = new System.Drawing.Size(160, 30);
            this.btnPreviewAssets.Text = "View Assets";
            this.btnPreviewAssets.UseVisualStyleBackColor = true;
            this.btnPreviewAssets.Click += new System.EventHandler(this.btnPreviewAssets_Click);
            this.btnPreviewAssets.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            this.tabAssets.Controls.Add(this.btnPreviewAssets);

            // txtDifferencesPath
            this.txtDifferencesPath.BackColor = System.Drawing.Color.White;
            this.txtDifferencesPath.Location = new System.Drawing.Point(13, 220);
            this.txtDifferencesPath.Size = new System.Drawing.Size(400, 23);  // Aumentado el margen lateral
            this.txtDifferencesPath.ReadOnly = true;
            this.txtDifferencesPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.tabAssets.Controls.Add(this.txtDifferencesPath);

            // btnBrowseDownloadTargetPath
            this.btnBrowseDownloadTargetPath.Location = new System.Drawing.Point(420, 160);
            this.btnBrowseDownloadTargetPath.Size = new System.Drawing.Size(100, 23);  // Ajustado el tamaño
            this.btnBrowseDownloadTargetPath.Text = "Browse";
            this.btnBrowseDownloadTargetPath.Click += new System.EventHandler(this.BtnBrowseDownloadTargetPath_Click);
            this.btnBrowseDownloadTargetPath.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.tabAssets.Controls.Add(this.btnBrowseDownloadTargetPath);

            // btnBrowseDifferencesPath
            this.btnBrowseDifferencesPath.Location = new System.Drawing.Point(420, 220);
            this.btnBrowseDifferencesPath.Size = new System.Drawing.Size(100, 23);  // Ajustado el tamaño
            this.btnBrowseDifferencesPath.Text = "Browse";
            this.btnBrowseDifferencesPath.Click += new System.EventHandler(this.BtnBrowseDifferencesPath_Click);
            this.btnBrowseDifferencesPath.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.tabAssets.Controls.Add(this.btnBrowseDifferencesPath);

            // btnExport
            this.btnExport.Location = new System.Drawing.Point(180, 250); // Justo a la derecha del otro
            this.btnExport.Size = new System.Drawing.Size(340, 30);  // Reduce para que entre en la fila
            this.btnExport.Text = "Download Selected Assets";
            this.btnExport.Click += new System.EventHandler(this.BtnDownloadSelectedAssets_Click);
            this.btnExport.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.tabAssets.Controls.Add(this.btnExport);

            // lblDownloadTargetPath
            this.lblDownloadTargetPath.Text = "Select the folder where to save the downloaded assets!";
            this.lblDownloadTargetPath.Location = new System.Drawing.Point(13, 140);
            this.lblDownloadTargetPath.Size = new System.Drawing.Size(400, 20);  // Aumentado el margen lateral
            this.lblDownloadTargetPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.tabAssets.Controls.Add(this.lblDownloadTargetPath);

            // lblDifferencesPath
            this.lblDifferencesPath.Text = "Select where the differences txt files are located!";
            this.lblDifferencesPath.Location = new System.Drawing.Point(13, 200);
            this.lblDifferencesPath.Size = new System.Drawing.Size(400, 20);  // Aumentado el margen lateral
            this.lblDifferencesPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.tabAssets.Controls.Add(this.lblDifferencesPath);

            // richTextBoxLogs
            this.richTextBoxLogs.Multiline = true;
            this.richTextBoxLogs.ScrollBars = RichTextBoxScrollBars.Vertical;
            this.richTextBoxLogs.Location = new System.Drawing.Point(13, 290);
            this.richTextBoxLogs.Size = new System.Drawing.Size(507, 50);  // Ajustado el tamaño y margen
            this.richTextBoxLogs.BorderStyle = BorderStyle.FixedSingle;
            this.richTextBoxLogs.BackColor = System.Drawing.SystemColors.Window;
            this.richTextBoxLogs.ReadOnly = true;
            this.richTextBoxLogs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.tabAssets.Controls.Add(this.richTextBoxLogs);

            // ExportForm
            this.ClientSize = new System.Drawing.Size(580, 460);  // Ajustado el tamaño para tener margen
            this.Controls.Add(this.tabControl);
            this.Name = "ExportForm";
            this.Text = "Export Assets";
            this.tabControl.ResumeLayout(false);
            this.tabAssets.ResumeLayout(false);
            this.tabAssets.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}