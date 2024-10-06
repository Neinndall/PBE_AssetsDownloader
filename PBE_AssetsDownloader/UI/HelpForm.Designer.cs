namespace PBE_NewFileExtractor.UI
{
    partial class HelpForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnAbout;
        private System.Windows.Forms.Button btnChangelogs;
        private System.Windows.Forms.Panel contentPanel; // Panel para mostrar el contenido

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
            this.btnAbout = new System.Windows.Forms.Button();
            this.btnChangelogs = new System.Windows.Forms.Button();
            this.contentPanel = new System.Windows.Forms.Panel(); // Inicialización del panel
            this.SuspendLayout();
            
            // 
            // btnAbout
            // 
            this.btnAbout.Location = new System.Drawing.Point(12, 40);
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.Size = new System.Drawing.Size(100, 30); // Tamaño ajustado
            this.btnAbout.TabIndex = 0;
            this.btnAbout.Text = "About";
            this.btnAbout.UseVisualStyleBackColor = true;
            this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
            
            // 
            // btnChangelogs
            // 
            this.btnChangelogs.Location = new System.Drawing.Point(12, 80);
            this.btnChangelogs.Name = "btnChangelogs";
            this.btnChangelogs.Size = new System.Drawing.Size(100, 30); // Tamaño ajustado
            this.btnChangelogs.TabIndex = 1;
            this.btnChangelogs.Text = "Changelogs";
            this.btnChangelogs.UseVisualStyleBackColor = true;
            this.btnChangelogs.Click += new System.EventHandler(this.btnChangelogs_Click);
            
            // 
            // contentPanel
            // 
            this.contentPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.contentPanel.AutoScroll = true; // Habilitar el desplazamiento automático
            this.contentPanel.Location = new System.Drawing.Point(120, 12);
            this.contentPanel.Name = "contentPanel";
            this.contentPanel.Size = new System.Drawing.Size(680, 360); // Tamaño ajustado
            this.contentPanel.TabIndex = 2;
            
            // 
            // HelpForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(820, 400); // Tamaño ajustado del formulario
            this.Controls.Add(this.contentPanel); // Agregar el panel a los controles del formulario
            this.Controls.Add(this.btnChangelogs);
            this.Controls.Add(this.btnAbout);
            this.Name = "HelpForm";
            this.Text = "Help"; // El título se actualizará por ApplicationInfos.SetInfo
            this.ResumeLayout(false);
        }
    }
}
