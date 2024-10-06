namespace PBE_NewFileExtractor.UI
{
    partial class SettingsForm
    {
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.CheckBox checkBoxSyncHashes;
        private System.Windows.Forms.TextBox textBoxLogs;

        private void InitializeComponent()
        {   
            this.checkBoxSyncHashes = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.textBoxLogs = new System.Windows.Forms.TextBox();
            
            // 
            // checkBoxSyncHashes
            // 
            this.checkBoxSyncHashes.AutoSize = true;
            this.checkBoxSyncHashes.Location = new System.Drawing.Point(12, 12);
            this.checkBoxSyncHashes.Name = "checkBoxSyncHashes";
            this.checkBoxSyncHashes.Size = new System.Drawing.Size(138, 17);
            this.checkBoxSyncHashes.TabIndex = 0;
            this.checkBoxSyncHashes.Text = "Synchronize Hashes with CDTB";
            this.checkBoxSyncHashes.UseVisualStyleBackColor = true;
            
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(12, 35);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            // 
            // textBoxLogs
            // 
            this.textBoxLogs.Location = new System.Drawing.Point(12, 64);
            this.textBoxLogs.Multiline = true;
            this.textBoxLogs.Name = "textBoxLogs";
            this.textBoxLogs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxLogs.Size = new System.Drawing.Size(260, 185);
            this.textBoxLogs.TabIndex = 2;

            // 
            // SettingsForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.textBoxLogs);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.checkBoxSyncHashes);
            this.Name = "SettingsForm";
            this.Text = "Configuración";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
