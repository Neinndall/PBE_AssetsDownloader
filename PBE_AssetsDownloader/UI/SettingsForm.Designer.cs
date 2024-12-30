namespace PBE_AssetsDownloader.UI
{
    partial class SettingsForm
    {
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.CheckBox checkBoxSyncHashes;
        private System.Windows.Forms.CheckBox checkBoxAutoCopy;
        private System.Windows.Forms.RichTextBox richTextBoxLogs; // Cambiado a RichTextBox

        private void InitializeComponent()
        {
            this.checkBoxSyncHashes = new System.Windows.Forms.CheckBox();
            this.checkBoxAutoCopy = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.richTextBoxLogs = new System.Windows.Forms.RichTextBox(); // Inicialización del RichTextBox

            this.SuspendLayout();

            // 
            // checkBoxSyncHashes
            // 
            this.checkBoxSyncHashes.AutoSize = true;
            this.checkBoxSyncHashes.Location = new System.Drawing.Point(12, 12);
            this.checkBoxSyncHashes.Name = "checkBoxSyncHashes";
            this.checkBoxSyncHashes.Size = new System.Drawing.Size(250, 24);
            this.checkBoxSyncHashes.TabIndex = 0;
            this.checkBoxSyncHashes.Text = "Synchronize Hashes with CDTB";
            this.checkBoxSyncHashes.UseVisualStyleBackColor = true;

            // 
            // checkBoxAutoCopy
            // 
            this.checkBoxAutoCopy.AutoSize = true;
            this.checkBoxAutoCopy.Location = new System.Drawing.Point(12, 30);
            this.checkBoxAutoCopy.Name = "checkBoxAutoCopy";
            this.checkBoxAutoCopy.Size = new System.Drawing.Size(250, 24);
            this.checkBoxAutoCopy.TabIndex = 1;
            this.checkBoxAutoCopy.Text = "Automatically copy new hashes to old";
            this.checkBoxAutoCopy.UseVisualStyleBackColor = true;

            // 
            // richTextBoxLogs
            // 
            this.richTextBoxLogs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom)));
            this.richTextBoxLogs.Location = new System.Drawing.Point(12, 72);
            this.richTextBoxLogs.Name = "richTextBoxLogs";
            this.richTextBoxLogs.Size = new System.Drawing.Size(260, 150);
            this.richTextBoxLogs.TabIndex = 2;
            this.richTextBoxLogs.ReadOnly = true;
            this.richTextBoxLogs.SelectionIndent = 4; // Esto mueve el texto hacia la derecha
            this.richTextBoxLogs.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBoxLogs.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.richTextBoxLogs.BackColor = System.Drawing.SystemColors.Window;
            this.richTextBoxLogs.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.richTextBoxLogs.Padding = new System.Windows.Forms.Padding(5);

            // 
            // btnSave
            // 
            this.btnSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            this.btnSave.Location = new System.Drawing.Point(12, 230);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            // 
            // SettingsForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.checkBoxAutoCopy);
            this.Controls.Add(this.checkBoxSyncHashes);
            this.Controls.Add(this.richTextBoxLogs);
            this.Controls.Add(this.btnSave);
            this.Name = "SettingsForm";
            this.Text = "Settings";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}