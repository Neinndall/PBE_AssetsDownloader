using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System;

namespace PBE_AssetsDownloader.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        
        // Controles del formulario
        private System.Windows.Forms.Label labelNewHashes;
        private System.Windows.Forms.TextBox newHashesTextBox;
        private System.Windows.Forms.Button btnSelectNewHashesDirectory;
        
        private System.Windows.Forms.Label labelOldHashes;
        private System.Windows.Forms.Button btnSelectOldHashesDirectory;
        private System.Windows.Forms.TextBox oldHashesTextBox;

        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Button btnHelp;
        private System.Windows.Forms.Button btnSettings;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.RichTextBox richTextBoxLogs; // RichTextBox para mostrar los logs

        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private ToolTip toolTip; // Informacion

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            labelNewHashes = new Label();
            newHashesTextBox = new TextBox();
            btnSelectNewHashesDirectory = new Button();
            labelOldHashes = new Label();
            oldHashesTextBox = new TextBox();
            btnSelectOldHashesDirectory = new Button();
            startButton = new Button();
            btnHelp = new Button();
            btnSettings = new Button();
            btnExport = new Button();
            richTextBoxLogs = new RichTextBox();
            folderBrowserDialog = new FolderBrowserDialog();
            toolTip = new ToolTip();
            SuspendLayout();
            
            // labelNewHashes
            labelNewHashes.AutoSize = true;
            labelNewHashes.Location = new Point(9, 47); // Ajustar la ubicación
            labelNewHashes.Name = "labelNewHashes";
            labelNewHashes.Size = new Size(177, 15);
            labelNewHashes.TabIndex = 0;
            labelNewHashes.Text = "Choose your new hash directory";
            Controls.Add(labelNewHashes);

            // newHashesTextBox
            newHashesTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            newHashesTextBox.Location = new Point(9, 66); // Ajustar la ubicación
            newHashesTextBox.Name = "newHashesTextBox";
            newHashesTextBox.Size = new Size(604, 23);
            newHashesTextBox.TabIndex = 1;
            Controls.Add(newHashesTextBox);

            // btnSelectNewHashesDirectory
            btnSelectNewHashesDirectory.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSelectNewHashesDirectory.Location = new Point(617, 66); // Ajustar la ubicación
            btnSelectNewHashesDirectory.Name = "btnSelectNewHashesDirectory";
            btnSelectNewHashesDirectory.Size = new Size(66, 23);
            btnSelectNewHashesDirectory.TabIndex = 2;
            btnSelectNewHashesDirectory.Text = "Browse";
            btnSelectNewHashesDirectory.UseVisualStyleBackColor = true;
            btnSelectNewHashesDirectory.Click += btnSelectNewHashesDirectory_Click;
            Controls.Add(btnSelectNewHashesDirectory);

            // labelOldHashes
            labelOldHashes.AutoSize = true;
            labelOldHashes.Location = new Point(9, 94); // Ajustar la ubicación
            labelOldHashes.Name = "labelOldHashes";
            labelOldHashes.Size = new Size(183, 15);
            labelOldHashes.TabIndex = 3;
            labelOldHashes.Text = "Choose your old hashes directory";
            Controls.Add(labelOldHashes);

            // oldHashesTextBox
            oldHashesTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            oldHashesTextBox.Location = new Point(9, 112); // Ajustar la ubicación
            oldHashesTextBox.Name = "oldHashesTextBox";
            oldHashesTextBox.Size = new Size(604, 23);
            oldHashesTextBox.TabIndex = 4;
            Controls.Add(oldHashesTextBox);

            // btnSelectOldHashesDirectory
            btnSelectOldHashesDirectory.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSelectOldHashesDirectory.Location = new Point(617, 112); // Ajustar la ubicación
            btnSelectOldHashesDirectory.Name = "btnSelectOldHashesDirectory";
            btnSelectOldHashesDirectory.Size = new Size(66, 23);
            btnSelectOldHashesDirectory.TabIndex = 5;
            btnSelectOldHashesDirectory.Text = "Browse";
            btnSelectOldHashesDirectory.UseVisualStyleBackColor = true;
            btnSelectOldHashesDirectory.Click += btnSelectOldHashesDirectory_Click;
            Controls.Add(btnSelectOldHashesDirectory);
            
            // richTextBoxLogs
            richTextBoxLogs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            richTextBoxLogs.BackColor = SystemColors.Window;
            richTextBoxLogs.BorderStyle = BorderStyle.FixedSingle;
            richTextBoxLogs.Font = new Font("Segoe UI", 9F);
            richTextBoxLogs.Location = new Point(9, 141); // Ajustar la ubicación
            richTextBoxLogs.Name = "richTextBoxLogs";
            richTextBoxLogs.ReadOnly = true;
            richTextBoxLogs.ScrollBars = RichTextBoxScrollBars.Vertical;
            richTextBoxLogs.Size = new Size(674, 270);
            richTextBoxLogs.TabIndex = 6;
            richTextBoxLogs.Text = "";
            Controls.Add(richTextBoxLogs);

            // startButton
            this.startButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            startButton.Location = new Point(617, 11); // Ajustar la ubicación
            startButton.Name = "startButton";
            startButton.Size = new Size(66, 24);
            startButton.TabIndex = 7;
            startButton.Text = "Start";
            // startButton.FlatStyle = FlatStyle.System; // Tipos: Standar, Flat, System, Popup
            startButton.UseVisualStyleBackColor = true;
            startButton.Click += startButton_Click; 
            toolTip.SetToolTip(startButton, "Start the download process");
            Controls.Add(startButton);
            
            // btnHelp
            this.btnHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            btnHelp.Location = new Point(153, 11);
            btnHelp.Name = "btnHelp";
            btnHelp.Size = new Size(66, 24);
            btnHelp.TabIndex = 8;
            btnHelp.Text = "Help";
            // btnHelp.FlatStyle = FlatStyle.Popup;
            btnHelp.UseVisualStyleBackColor = true;
            btnHelp.Click += btnHelp_Click;
            toolTip.SetToolTip(btnHelp, "Help and support");
            Controls.Add(btnHelp);

            // btnSettings
            this.btnSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            btnSettings.Location = new Point(81, 11);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new Size(66, 24);
            btnSettings.TabIndex = 9;
            btnSettings.Text = "Settings";
            // btnSettings.FlatStyle = FlatStyle.System;
            btnSettings.UseVisualStyleBackColor = true;
            btnSettings.Click += btnSettings_Click;
            toolTip.SetToolTip(btnSettings, "Configure your settings");
            Controls.Add(btnSettings);
            
            // btnExport
            this.btnExport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            btnExport.Location = new Point(9, 11);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(66, 24);
            btnExport.TabIndex = 10;
            btnExport.Text = "Export";
            // btnExport.FlatStyle = FlatStyle.System;
            btnExport.UseVisualStyleBackColor = true;
            btnExport.Click += btnExport_Click;
            toolTip.SetToolTip(btnExport, "Export manually downloaded assets");
            Controls.Add(btnExport);

            // MainForm
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(700, 422);
            Name = "MainForm";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}