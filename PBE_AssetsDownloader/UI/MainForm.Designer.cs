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
        private System.Windows.Forms.RichTextBox richTextBoxLogs; // RichTextBox para mostrar los logs
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;

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
            this.labelNewHashes = new System.Windows.Forms.Label();
            this.newHashesTextBox = new System.Windows.Forms.TextBox();
            this.btnSelectNewHashesDirectory = new System.Windows.Forms.Button();
            
            this.labelOldHashes = new System.Windows.Forms.Label();
            this.oldHashesTextBox = new System.Windows.Forms.TextBox();
            this.btnSelectOldHashesDirectory = new System.Windows.Forms.Button();
            
            this.startButton = new System.Windows.Forms.Button();
            this.btnHelp = new System.Windows.Forms.Button();
            this.btnSettings = new System.Windows.Forms.Button();
            this.richTextBoxLogs = new System.Windows.Forms.RichTextBox(); // Inicialización del RichTextBox
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            
            this.SuspendLayout();

            // 
            // labelNewHashes
            // 
            this.labelNewHashes.AutoSize = true;
            this.labelNewHashes.Location = new System.Drawing.Point(10, 50); // Ajustar la ubicación
            this.labelNewHashes.Name = "labelNewHashes";
            this.labelNewHashes.Size = new System.Drawing.Size(226, 17);
            this.labelNewHashes.TabIndex = 0;
            this.labelNewHashes.Text = "Choose your directory new hashes";
            
            // 
            // newHashesTextBox
            // 
            this.newHashesTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.newHashesTextBox.Location = new System.Drawing.Point(10, 70); // Ajustar la ubicación
            this.newHashesTextBox.Name = "newHashesTextBox";
            this.newHashesTextBox.Size = new System.Drawing.Size(690, 20);
            this.newHashesTextBox.TabIndex = 1;
            
            // 
            // btnSelectNewHashesDirectory
            // 
            this.btnSelectNewHashesDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelectNewHashesDirectory.Location = new System.Drawing.Point(705, 70); // Ajustar la ubicación
            this.btnSelectNewHashesDirectory.Name = "btnSelectNewHashesDirectory";
            this.btnSelectNewHashesDirectory.Size = new System.Drawing.Size(75, 26);
            this.btnSelectNewHashesDirectory.TabIndex = 2;
            this.btnSelectNewHashesDirectory.Text = "Browse";
            this.btnSelectNewHashesDirectory.UseVisualStyleBackColor = true;
            this.btnSelectNewHashesDirectory.Click += new System.EventHandler(this.btnSelectNewHashesDirectory_Click);
            
            // 
            // labelOldHashes
            // 
            this.labelOldHashes.AutoSize = true;
            this.labelOldHashes.Location = new System.Drawing.Point(10, 100); // Ajustar la ubicación
            this.labelOldHashes.Name = "labelOldHashes";
            this.labelOldHashes.Size = new System.Drawing.Size(226, 17);
            this.labelOldHashes.TabIndex = 3;
            this.labelOldHashes.Text = "Choose your directory old hashes";
            
            // 
            // oldHashesTextBox
            // 
            this.oldHashesTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.oldHashesTextBox.Location = new System.Drawing.Point(10, 120); // Ajustar la ubicación
            this.oldHashesTextBox.Name = "oldHashesTextBox";
            this.oldHashesTextBox.Size = new System.Drawing.Size(690, 20);
            this.oldHashesTextBox.TabIndex = 4;
            
            // 
            // btnSelectOldHashesDirectory
            // 
            this.btnSelectOldHashesDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelectOldHashesDirectory.Location = new System.Drawing.Point(705, 120); // Ajustar la ubicación
            this.btnSelectOldHashesDirectory.Name = "btnSelectOldHashesDirectory";
            this.btnSelectOldHashesDirectory.Size = new System.Drawing.Size(75, 26);
            this.btnSelectOldHashesDirectory.TabIndex = 5;
            this.btnSelectOldHashesDirectory.Text = "Browse";
            this.btnSelectOldHashesDirectory.UseVisualStyleBackColor = true;
            this.btnSelectOldHashesDirectory.Click += new System.EventHandler(this.btnSelectOldHashesDirectory_Click);
    
            // 
            // richTextBoxLogs
            // 
            this.richTextBoxLogs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom)));
            this.richTextBoxLogs.Location = new System.Drawing.Point(10, 150);
            this.richTextBoxLogs.Name = "richTextBoxLogs";
            this.richTextBoxLogs.Size = new System.Drawing.Size(770, 288);
            this.richTextBoxLogs.TabIndex = 6;
            this.richTextBoxLogs.ReadOnly = true;
            this.richTextBoxLogs.SelectionIndent = 4;
            this.richTextBoxLogs.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBoxLogs.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.richTextBoxLogs.BackColor = System.Drawing.SystemColors.Window;
            this.richTextBoxLogs.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            
            // 
            // startButton
            // 
            this.startButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.startButton.Location = new System.Drawing.Point(10, 12); // Ajustar la ubicación
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(75, 26);
            this.startButton.TabIndex = 7;
            this.startButton.Text = "Start";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            
            // 
            // btnHelp
            //
            this.btnHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnHelp.Location = new System.Drawing.Point(705, 12);
            this.btnHelp.Name = "btnHelp";
            this.btnHelp.Size = new System.Drawing.Size(75, 26);
            this.btnHelp.TabIndex = 8;
            this.btnHelp.Text = "Help";
            this.btnHelp.UseVisualStyleBackColor = true;
            this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
            
            // 
            // btnSettings
            // 
            this.btnSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSettings.Location = new System.Drawing.Point(625, 12);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(75, 26);
            this.btnSettings.TabIndex = 9;
            this.btnSettings.Text = "Settings";
            this.btnSettings.UseVisualStyleBackColor = true;
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);

            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.labelNewHashes);
            this.Controls.Add(this.newHashesTextBox);
            this.Controls.Add(this.btnSelectNewHashesDirectory);
            this.Controls.Add(this.labelOldHashes);
            this.Controls.Add(this.oldHashesTextBox);
            this.Controls.Add(this.btnSelectOldHashesDirectory);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.btnHelp);
            this.Controls.Add(this.btnSettings);
            this.Controls.Add(this.richTextBoxLogs); // Agregar el RichTextBox a los controles del formulario
            this.Name = "MainForm";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}